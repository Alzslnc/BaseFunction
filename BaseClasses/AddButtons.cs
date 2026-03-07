using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AppCore = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using AppSystemVariableChangedEventArgs = Autodesk.AutoCAD.ApplicationServices.SystemVariableChangedEventArgs;

//Использование в плагинах

//public class ExampleRibbon : IExtensionApplication
//{
//    public void Initialize()
//    {
//        StartEvents startEvents = new StartEvents();

//        startEvents.Buttons.Add(new Button("Панель", "Вкладка", new List<ButtonCommand>
//            {
//                //кнопка
//                new ButtonCommand("Команда1", "Название кнопки1",
//                "Описание"),
//                //если в одной кнопке несколько команд то добавлять их в список
//                //new ButtonCommand("Команда2", "Название кнопки2",
//                //"Описание"),
//            }
//            ));

//        startEvents.Initialize();
//    }
//    public void Terminate() { ControlVersionClass.Terminate();}
//}

namespace BaseFunction
{
    public static class ControlVersionClass
    {
        static ControlVersionClass()
        {
            Load();
        }
        public static void Save()
        {

            ControlVersion controlVersion = new ControlVersion() { VersionDatas = VersionDatas };
            FoldersClass folders = new FoldersClass() { Folders = Folders };
            BaseXMLClass.SetSerialisationResult(SavePath, controlVersion);
            BaseXMLClass.SetSerialisationResult(SavePath2, folders);
        }
        public static void Load()
        {
            if (!File.Exists(SavePath) || !File.Exists(SavePath2)) return;
            if (BaseXMLClass.GetSerialisationResult(SavePath, typeof(ControlVersion)) is ControlVersion data)
            {
                VersionDatas = data.VersionDatas;
            }
            if (BaseXMLClass.GetSerialisationResult(SavePath2, typeof(FoldersClass)) is FoldersClass folders)
            {
                Folders = folders.Folders;
            }
        }
        public static void Terminate()
        {
            try
            {
                //if (File.Exists(SavePath)) File.Delete(SavePath);
                //if (File.Exists(SavePath2)) File.Delete(SavePath2);
            } catch { }
        }

        private static readonly string SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ControlVersion.xml");
        private static readonly string SavePath2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Folders.xml");
        public class ControlVersion
        {
            public ControlVersion() { }
            public List<VersionData> VersionDatas { get; set; } = new List<VersionData>();
        }
        public class FoldersClass
        {
            public FoldersClass() { }
            public List<string> Folders { get; set; } = new List<string>();
        }
        public class VersionData
        {
            public string Name;
            public DateTime Date;
        }
        public static List<VersionData> VersionDatas { get; set; } = new List<VersionData>();
        public static List<string> Folders { get; set; } = new List<string>();
        public static void OpenFolder()
        {
            Load();
            foreach (string path in Folders)
            {
                if (Directory.Exists(path))
                {
                    Process.Start("explorer", "\"" + path + "\"");
                }
            }           
        }
        public static void CheckVersion()
        {
            using (Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
            {

                ControlVersionClass.Load();

                string report = "";

                //string nnn = Path.Combine($"https://raw.githubusercontent.com/Alzslnc/AcadPlugins/main/");


                using (HttpClient client = new HttpClient())
                {
                    // GitHub API требует User-Agent
                    client.DefaultRequestHeaders.Add("User-Agent", "C# App");

                    List<string> actual = new List<string>();
                    List<string> toUpdate = new List<string>();
                    List<string> notInstalled = new List<string>();

                    try
                    {
                        string json = client.GetStringAsync($"https://api.github.com/repos/Alzslnc/AcadPlugins/contents/").Result.Replace("[", "").Replace("]", "").Replace("{", "").Replace("}}", "}");

                        string[] docs = json.Split('}');

                        foreach (string doc in docs)
                        {
                            if (doc.Length < 20) continue;

                            string folderName = "";
                            string name = "";
                            string path = "";
                            string size = "";

                            string fNamePath = "\"name\":\"";
                            int start = doc.IndexOf(fNamePath) + fNamePath.Length;
                            if (start >= 0)
                            {
                                while (doc[start] != '"')
                                {
                                    folderName += doc[start++];

                                }
                                folderName = folderName.Replace(".zip", "");
                                name = folderName.Replace(".bundle", "");
                            }
                            if (string.IsNullOrEmpty(folderName)) continue;
                            if (string.IsNullOrEmpty(name)) continue;


                            fNamePath = "\"download_url\":\"";
                            start = doc.IndexOf(fNamePath) + fNamePath.Length;
                            if (start >= 0)
                            {
                                while (doc[start] != '"')
                                {
                                    path += doc[start++];
                                }
                            }

                            if (string.IsNullOrEmpty(name)) continue;

                            fNamePath = "\"size\":";
                            start = doc.IndexOf(fNamePath) + fNamePath.Length;
                            if (start >= 0)
                            {
                                while (doc[start] != ',')
                                {
                                    size += doc[start++];
                                }
                            }

                            if (string.IsNullOrEmpty(size)) continue;

                            VersionData versionData = ControlVersionClass.VersionDatas.FirstOrDefault(x => x.Name.Contains(folderName));

                            if (versionData == null)
                            {
                                notInstalled.Add(name);
                            }
                            else
                            {
                                try
                                {
                                    if (uint.TryParse(size, out uint sizeL) && sizeL > 2048)
                                    {
                                        client.DefaultRequestHeaders.Range = new RangeHeaderValue(sizeL - 2048, sizeL);
                                    }

                                    byte[] archiveTail = client.GetByteArrayAsync(path).Result;
                                    Stream fs = null;
                                    try
                                    {
                                        fs = new MemoryStream(archiveTail);
                                        // 1. Ищем EOCD, чтобы найти начало Центрального Каталога
                                        fs.Seek(Math.Max(0, fs.Length - 1024), SeekOrigin.Begin);
                                        byte[] eocdBuf = new byte[1024];
                                        fs.Read(eocdBuf, 0, eocdBuf.Length);

                                        int eocdPos = -1;
                                        for (int i = eocdBuf.Length - 4; i >= 0; i--)
                                        {
                                            if (BitConverter.ToUInt32(eocdBuf, i) == 0x06054B50) { eocdPos = i; break; }
                                        }

                                        if (eocdPos == -1) return;

                                        // 2. Читаем кол-во записей и смещение каталога
                                        ushort totalEntries = BitConverter.ToUInt16(eocdBuf, eocdPos + 10);
                                        uint cdOffset = BitConverter.ToUInt32(eocdBuf, eocdPos + 16);

                                        DateTime dt = DateTime.MinValue;

                                        uint newCdoff = cdOffset + 2048;

                                        if (newCdoff < sizeL)
                                        {
                                            client.DefaultRequestHeaders.Range = new RangeHeaderValue(cdOffset, sizeL);
                                            archiveTail = client.GetByteArrayAsync(path).Result;
                                            fs = new MemoryStream(archiveTail);
                                            newCdoff = 0;
                                        }
                                        else
                                        {
                                            newCdoff -= sizeL;
                                        }
                                        // 3. Переходим к каталогу и читаем данные каждого файла

                                        fs.Seek(newCdoff, SeekOrigin.Begin);
                                        for (int i = 0; i < totalEntries; i++)
                                        {
                                            byte[] h = new byte[46]; // Фиксированная часть заголовка (46 байт)
                                            fs.Read(h, 0, 46);

                                            if (BitConverter.ToUInt32(h, 0) != 0x02014B50) break;

                                            // Извлекаем метаданные файла
                                            uint crc32 = BitConverter.ToUInt32(h, 16);
                                            uint compSize = BitConverter.ToUInt32(h, 20);
                                            uint uncompSize = BitConverter.ToUInt32(h, 24);
                                            ushort nLen = BitConverter.ToUInt16(h, 28); // Длина имени
                                            ushort eLen = BitConverter.ToUInt16(h, 30); // Длина доп. полей
                                            ushort cLen = BitConverter.ToUInt16(h, 32); // Длина комментария файла
                                            uint localHeaderOffset = BitConverter.ToUInt32(h, 42); // Смещение данных

                                            // Читаем имя файла
                                            byte[] nameBuf = new byte[nLen];
                                            fs.Read(nameBuf, 0, nLen);
                                            string fileName = Encoding.UTF8.GetString(nameBuf);

                                            string shortName = new FileInfo(versionData.Name).Name;
                                            if (fileName.Contains(shortName) && !fileName.Contains("config"))
                                            {
                                                // Извлекаем сырые значения из массива заголовка h
                                                ushort dosTime = BitConverter.ToUInt16(h, 12);
                                                ushort dosDate = BitConverter.ToUInt16(h, 14);

                                                // Распаковываем биты даты
                                                int year = ((dosDate & 0xFE00) >> 9) + 1980;
                                                int month = (dosDate & 0x01E0) >> 5;
                                                int day = dosDate & 0x1F;

                                                // Распаковываем биты времени
                                                int hour = (dosTime & 0xF800) >> 11;
                                                int minute = (dosTime & 0x07E0) >> 5;
                                                int second = (dosTime & 0x1F) * 2; // ZIP хранит секунды с шагом в 2 сек.

                                                try
                                                {
                                                    dt = new DateTime(year, month, day, hour, minute, second);
                                                }
                                                catch
                                                {
                                                }

                                                break;
                                            }

                                            // Пропускаем доп. поля и комментарий файла, чтобы попасть на следующую запись
                                            fs.Seek(eLen + cLen, SeekOrigin.Current);
                                        }

                                        if (dt == DateTime.MinValue) continue;
                                        else if (dt > versionData.Date)
                                        {
                                            toUpdate.Add(name);
                                        }
                                        else
                                        {
                                            actual.Add(name);
                                        }
                                    }
                                    finally
                                    {
                                        fs?.Dispose();
                                    }                                   
                                }
                                catch
                                {
                                }
                            }
                        }

                        if (actual.Count > 0)
                        {
                            report += $" {Environment.NewLine}Версия актуальна:{Environment.NewLine}";
                            foreach (string s in actual) { report += s + Environment.NewLine; }
                        }
                        if (toUpdate.Count > 0)
                        {
                            report += $" {Environment.NewLine}Есть новая версия:{Environment.NewLine}";
                            foreach (string s in toUpdate) { report += s + Environment.NewLine; }
                        }
                        if (notInstalled.Count > 0)
                        {
                            report += $" {Environment.NewLine}Программа не установлена:{Environment.NewLine}";
                            foreach (string s in notInstalled) { report += s + Environment.NewLine; }
                        }
                        System.Windows.MessageBox.Show(report);

                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine($"Ошибка: {ex.Message}");
                    }
                }
            }
        }
    }
    public static class RenameTabClass
    {        
        static RenameTabClass()
        {
            Load();
        }
        [CommandMethod("RenameTab")]
        public static void Rename()
        {

            Editor editor = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;

            Load();

            Dictionary<string, string> newTab = Tabs.ToDictionary(x => x.Key, x => x.Value);

            if (Tabs.Count == 0)
            {
                editor.WriteMessage("\nВкладки для переименования отсутствуют");
                return;
            }

            LoadList(editor);

            while (BaseFunction.BaseGetObjectClass.TryGetKeywords(out string result, new List<string> { "Вкладки", "Переименовать", "Сохранить", "Отменить" }, "\nВыберите пункт из списка"))
            {
                switch (result) 
                {
                    case "Вкладки": 
                        {
                            LoadList(editor);
                            break;
                        }
                    case "Переименовать":
                        {
                            if (BaseGetObjectClass.TryGetIntFromUser(out int numResult, 1, 1, newTab.Count, "Выберите номер вкладки для переименования"))
                            {
                                PromptResult stringResult = editor.GetString(new PromptStringOptions($"Выберите новое название вкладки {newTab.ElementAt(numResult - 1).Key}")
                                {
                                    UseDefaultValue = true,
                                    DefaultValue = newTab.ElementAt(numResult - 1).Value,
                                });
                                if (stringResult.Status == PromptStatus.OK && !string.IsNullOrEmpty(stringResult.StringResult))
                                {
                                    newTab[newTab.ElementAt(numResult - 1).Key] = stringResult.StringResult;                               
                                }
                            }
                            else
                            {
                                editor.WriteMessage("\nИндекс вкладки за пределами возможных значений");
                            }                               

                            break;
                        }
                    case "Сохранить":
                        {
                            Tabs = newTab.ToDictionary(x => x.Key, x => x.Value);
                            Save();
                            return;
                        }
                    case "Отменить":
                        {                           
                            return;
                        }
                }
            }
            Save();
        }
        public static void RenameTabsOnLoad(List<Button> Buttons)
        {
            try
            {
                Load();
                for (int i = 0; i < Buttons.Count; i++)
                {
                    if (!RenameTabClass.Tabs.ContainsKey(Buttons[i].RibbonTabName))
                    {
                        RenameTabClass.Tabs.Add(Buttons[i].RibbonTabName, Buttons[i].RibbonTabName);
                        RenameTabClass.Save();
                    }

                    else
                    {
                        Buttons[i].RibbonTabName = RenameTabClass.Tabs[Buttons[i].RibbonTabName];
                        Buttons[i].RibbonTabId = Buttons[i].RibbonTabName + "_Id";
                    }
                }
            }
            catch { }
        }
        private static void LoadList(Editor ed)
        {
            ed.WriteMessage("\nСписок панелей для переименования:");
            int i = 1;
            foreach (KeyValuePair<string, string> key in Tabs)
            {
                ed.WriteMessage($"\n{i++}: {key.Key} / {key.Value}");
            }
        }
        public static void Save()
        {
            if (Tabs.Count == 0) return;
            TabsSaveData tabsSaveData = new TabsSaveData(Tabs);
            BaseXMLClass.SetSerialisationResult(SavePath, tabsSaveData);
        }
        public static void Load()
        {
            if (!File.Exists(SavePath)) return;
            if (BaseXMLClass.GetSerialisationResult(SavePath, typeof(TabsSaveData)) is TabsSaveData data)
            {
                foreach (TabData tabData in data.TabDatas)
                { 
                    if (Tabs.ContainsKey(tabData.Name)) Tabs[tabData.Name] = tabData.NewName;
                    else Tabs.Add(tabData.Name, tabData.NewName);
                }
            }
        }

        public class TabsSaveData
        { 
            public TabsSaveData() { }
            public TabsSaveData(Dictionary<string, string> data)
            {
                foreach (var key in data)
                {
                    TabDatas.Add(new TabData { Name = key.Key, NewName = key.Value });
                }
            }
            public List<TabData> TabDatas { get; set; } = new List<TabData>();        
        }

        public struct TabData
        { 
            public string Name;
            public string NewName;
        }
        
        public static Dictionary<string, string> Tabs = new Dictionary<string, string>();
        private static string SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RenameTabData.xml");  
    }
    public static class SpecialCommandsClass
    {
        public static void CreateSpecialButtons(List<Button> Buttons)
        {

            List<string> ribTabNames = new List<string>();

            foreach (Button button in Buttons)
            {
                if (!ribTabNames.Contains(button.RibbonTabName)) ribTabNames.Add(button.RibbonTabName);
            }

            foreach (string ribTabName in ribTabNames)
            {
                Buttons.Add(new Button(ribTabName, "О программе", new List<ButtonCommand> { new ButtonCommand("О программе", "О программе", "Описание"), }));
                Buttons.Add(new Button(ribTabName, "О программе", new List<ButtonCommand> { new ButtonCommand("Проверить обновления", "Проверить обновления", "Показывает наличие обновлений."), }));
                Buttons.Add(new Button(ribTabName, "О программе", new List<ButtonCommand> { new ButtonCommand("Открыть репозиторий", "Открыть репозиторий", "Место хранения последних версий программ."), }));
                Buttons.Add(new Button(ribTabName, "О программе", new List<ButtonCommand> { new ButtonCommand("Открыть папку с плагинами", "Открыть папку с плагинами", "Открывает папку, откуда были запущены плагины."), }));
            }
        }
        public static bool SpecialCommands(string name)
        {
            if (name == "О программе")
            {
                System.Windows.MessageBox.Show("Все вопросы можно направить по адресу alzslnc@gmail.com");
            }
            else if (name == "Открыть репозиторий")
            {
                Process.Start(new ProcessStartInfo("https://github.com/Alzslnc/AcadPlugins") { UseShellExecute = true });
            }
            else if (name == "Открыть папку с плагинами")
            {
                ControlVersionClass.OpenFolder();
            }
            else if (name == "Проверить обновления")
            {
                ControlVersionClass.CheckVersion();
            }
            else return false;
            return true;
        }
    }
    internal class StartEvents
    {
        private bool Initialized { get; set; } = false;
        private bool NeedUpdRibbonDetected { get; set; } = false;        
        public List<Button> Buttons { get; private set; } = new List<Button>();
        /// <summary>
        /// Инициализация
        /// </summary>
        public void Initialize()
        {
            if (Buttons.Count == 0) return;

            GetVersion();

            RenameTabClass.RenameTabsOnLoad(Buttons);

            SpecialCommandsClass.CreateSpecialButtons(Buttons);           

            if (!Initialized)
            {
                Initialized = true;

                AppCore.Idle += Application_Idle_RibbonUpdate;
                AppCore.SystemVariableChanged += App_SysVarChanged_RibbonUpdate;
            }
        }
        private void GetVersion()
        {            
            FileInfo fileInfo = new FileInfo(this.GetType().Assembly.Location);
            ControlVersionClass.Load();

            ControlVersionClass.VersionData versionData = ControlVersionClass.VersionDatas.FirstOrDefault(x => x.Name == fileInfo.FullName);
            if (versionData == null)
            { 
                versionData = new ControlVersionClass.VersionData() { Name = fileInfo.FullName};
                ControlVersionClass.VersionDatas.Add(versionData);
            }
            versionData.Date = fileInfo.LastWriteTime;
            DirectoryInfo directory = fileInfo.Directory;
            while (directory.FullName.Contains(".bundle")) directory = directory.Parent;
            if (directory != null && !ControlVersionClass.Folders.Contains(directory.FullName)) ControlVersionClass.Folders.Add(directory.FullName);

            ControlVersionClass.Save();
        }
        private void App_SysVarChanged_RibbonUpdate (object sender, AppSystemVariableChangedEventArgs e)
        {
            if (!NeedUpdRibbonDetected
                && 
                (e.Name.Equals("WSCURRENT",
                StringComparison.OrdinalIgnoreCase)
                || e.Name.Equals("RIBBONSTATE",
                StringComparison.OrdinalIgnoreCase)))
            {
                NeedUpdRibbonDetected = true;
                AppCore.Idle += Application_Idle_RibbonUpdate;
            }
        }
        private void Application_Idle_RibbonUpdate(object sender, EventArgs e)
        {
            RibbonControl ribbon = ComponentManager.Ribbon;
            if (ribbon != null)
            {
                AppCore.Idle -= Application_Idle_RibbonUpdate;
                NeedUpdRibbonDetected = false;
                CreateRibbonTab(ribbon);
            }
        }       
        private void CreateRibbonTab(RibbonControl ribCntrl)
        {
            try
            {             
                foreach (Button buttonData in Buttons)
                {
                    if (string.IsNullOrEmpty(buttonData.RibbonTabId) ||
                        string.IsNullOrEmpty(buttonData.RibbonTabName) ||
                        string.IsNullOrEmpty(buttonData.RibbonPanelName) ||
                        buttonData.ButtonCommands.Count == 0) continue;

                    RibbonTab ribTab = null;
                    // добавляем свою вкладку
                    foreach (RibbonTab tab in ribCntrl.Tabs)
                    {
                        if (tab.Id.Equals(buttonData.RibbonTabId))
                        {
                            ribTab = tab;
                            break;
                        }
                    }
                    if (ribTab == null)
                    {                        
                        ribTab = new RibbonTab
                        {
                            
                            Title = buttonData.RibbonTabName, // Заголовок вкладки
                            Id = buttonData.RibbonTabId // Идентификатор вкладки
                        };
                        ribCntrl.Tabs.Add(ribTab); // Добавляем вкладку в ленту
                    }
                    // добавляем содержимое в свою вкладку (одну панель)
                    AddExampleContent(ribTab, buttonData);
                    // Делаем вкладку активной (не желательно, ибо неудобно)
                    //ribTab.IsActive = true;
                    // Обновляем ленту (если делаете вкладку активной, то необязательно)
                    ribCntrl.UpdateLayout();
                }
            }
            catch { }            
        }
        /// <summary>
        /// Строим панели и кнопки если требуется
        /// </summary>
        private void AddExampleContent(RibbonTab ribTab, Button buttonData)
        {
            RibbonPanel ribPanel = null;       
            foreach (RibbonPanel panel in ribTab.Panels)
            {
                if (panel.Source.Title.Equals(buttonData.RibbonPanelName))
                {
                    ribPanel = panel;
                    break;
                }
            }
            if (ribPanel == null)
            {
                RibbonPanelSource ribSourcePanel = new RibbonPanelSource
                {
                    Title = buttonData.RibbonPanelName
                };
                ribPanel = new RibbonPanel
                {
                    Source = ribSourcePanel
                };
                List<string> names = new List<string>() { buttonData.RibbonPanelName };
                foreach (RibbonPanel panel in ribTab.Panels) names.Add(panel.Source.Title);
                names.Sort();
                names.Remove("О программе");
                names.Add("О программе");
                ribTab.Panels.Insert(names.IndexOf(buttonData.RibbonPanelName), ribPanel);
            }

            foreach (RibbonItem item in ribPanel.Source.Items)
            {
                if (item is RibbonSplitButton splitButton)
                {
                    if (buttonData.ButtonCommands.Count > 1 && splitButton.Text.ToString() == buttonData.ButtonCommands[0].Name) return;
                }
                else if (item is RibbonButton button)
                { 
                    if (buttonData.ButtonCommands.Count == 1 && button.CommandParameter.ToString() == buttonData.ButtonCommands[0].Command) return;                
                }
            }

            if (buttonData.ButtonCommands.Count == 1) ribPanel.Source.Items.Add(CreateButton(buttonData.ButtonCommands[0]));
            else if (buttonData.ButtonCommands.Count > 1) ribPanel.Source.Items.Add(CreateSplitButton(buttonData.ButtonCommands));

            if (ribPanel.Source.Items.Count == 5) ribPanel.Source.Items.Add(new RibbonPanelBreak());
            else ribPanel.Source.Items.Add(new RibbonRowBreak());
        }
        /// <summary>
        /// создание кнопки
        /// </summary>
        private RibbonButton CreateButton(ButtonCommand buttonTextData)
        {
            RibbonToolTip tt = new RibbonToolTip
            {
                IsHelpEnabled = false,
                Title = buttonTextData.Name,
                Command = buttonTextData.Command,
                Content = buttonTextData.Content
            };
            RibbonButton ribBtn = new RibbonButton
            {
                CommandParameter = buttonTextData.Command,
                Text = buttonTextData.Name,
                Name = buttonTextData.Name,
                CommandHandler = new RibbonCommandHandler(),
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Size = RibbonItemSize.Standard,
                ShowImage = false,
                ShowText = true,
                ToolTip = tt
            };
            return ribBtn;
        }
        /// <summary>
        /// создание общей кнопки
        /// </summary>
        private RibbonSplitButton CreateSplitButton(List<ButtonCommand> buttonTextDatas)
        {
            RibbonSplitButton risSplitBtn = new RibbonSplitButton
            {
                Text = buttonTextDatas[0].Name,
                Orientation = System.Windows.Controls.Orientation.Vertical,
                Size = RibbonItemSize.Standard,
                ShowImage = false,
                ShowText = true,
                ListButtonStyle = Autodesk.Private.Windows.RibbonListButtonStyle.SplitButton,
                ResizeStyle = RibbonItemResizeStyles.NoResize,
                ListStyle = RibbonSplitButtonListStyle.List
            };
            foreach (ButtonCommand buttonTextData in buttonTextDatas)
            {
                risSplitBtn.Items.Add(CreateButton(buttonTextData));
            }
            risSplitBtn.Current = risSplitBtn.Items.First();
            return risSplitBtn;
        }        
        private class RibbonCommandHandler : System.Windows.Input.ICommand
        {
            public bool CanExecute(object parameter)
            {
                return true;
            }
            public event EventHandler CanExecuteChanged;
            public void Execute(object parameter)
            {
                if (parameter is RibbonButton button)
                {
                    Autodesk.AutoCAD.Internal.Utils.SetFocusToDwgView();

                    if (!SpecialCommandsClass.SpecialCommands(button.Name))
                    {
                        Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.
                            SendStringToExecute(button.CommandParameter + " ", true, false, true);
                    }
                }
            }      
            
        }
    }
    public class Button
    {
        public Button(string ribbonTabName, string ribbonPanelName)
        {
            RibbonTabName = ribbonTabName;
            RibbonTabId = ribbonTabName + "_Id";
            RibbonPanelName = ribbonPanelName;
        }
        public Button(string ribbonTabName, string ribbonPanelName, List<ButtonCommand> buttonCommnands)
        {
            RibbonTabName = ribbonTabName;
            RibbonTabId = ribbonTabName + "_Id";
            RibbonPanelName = ribbonPanelName;
            ButtonCommands.AddRange(buttonCommnands);
        }
        public Button(string ribbonTabName, string ribbonPanelName, ButtonCommand buttonCommand)
        {
            RibbonTabName = ribbonTabName;
            RibbonTabId = ribbonTabName + "_Id";
            RibbonPanelName = ribbonPanelName;
            ButtonCommands.Add(buttonCommand);
        }
        public string RibbonTabName { get; set; } = string.Empty;
        public string RibbonTabId { get; set; } = string.Empty;
        public string RibbonPanelName { get; set; } = string.Empty;
        public List<ButtonCommand> ButtonCommands { get; set; } = new List<ButtonCommand>();
    }
    public class ButtonCommand
    {
        public ButtonCommand(string command, string name, string content)
        { 
            Command = command;
            Name = name;
            Content = content;
        }        
        public string Command { get; set; } = string.Empty; 
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    } 
}
