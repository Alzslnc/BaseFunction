using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace BaseFunction
{
    public class ControlVersionClass
    {
        [CommandMethod("OpenRepositoryPage")]
        public void OpenRepositoryPage()
        {
            Process.Start(new ProcessStartInfo("https://github.com/Alzslnc/AcadPlugins") { UseShellExecute = true });
        }
        [CommandMethod("CheckPluginVersion")]
        public void CheckPluginVersion()
        {
            List<MetaData> datas = GetOurPlugins();
            List<MetaData> gitDatas;
            try
            {
                gitDatas = GetGitDatasProcess();

                Autodesk.AutoCAD.ApplicationServices.Application.ShowModalWindow(CreateWindow(GetResultString(datas, gitDatas), true));

                //System.Windows.MessageBox.Show(GetResultString(datas, gitDatas));
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        [CommandMethod("OpenPluginsFolder")]
        public static void OpenPluginsFolder()
        {
            List<MetaData> datas = GetOurPlugins();

            if (datas.Count == 0) return;

            foreach (string path in datas.Select(x => x.Path).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (Directory.Exists(path))
                {
                    Process.Start("explorer", "\"" + path + "\"");
                }
            }
        }
        private static List<MetaData> GetGitDatasProcess()
        {
            List<MetaData> result = new List<MetaData>();

            Window window = CreateWindow();
            window.Owner = System.Windows.Application.Current?.MainWindow;
            bool isCheckingFinished = false; // Флаг: завершился ли фоновый поток?

            // 1. Запрещаем закрытие окна (через Alt+F4 или крестик), пока идет проверка
            window.Closing += (sender, e) =>
            {
                if (!isCheckingFinished)
                {
                    e.Cancel = true; // Отменяем закрытие окна
                }
            };

            // 2. Логика запуска и автоматического закрытия
            window.Loaded += (sender, args) =>
            {
                Task.Run(() =>
                {
                    try
                    {
                        result = GetGitData();
                    }
                    finally
                    {
                        isCheckingFinished = true; // Разрешаем закрытие окна
                                                   // Закрываем окно через Dispatcher
                        window.Dispatcher.Invoke(() => window.Close());
                    }
                });
            };

            // Блокируем AutoCAD модальным окном
            window.ShowDialog();

            return result;
        }
        private static string GetResultString(List<MetaData> datas, List<MetaData> gitDatas)
        {
            // Если с Гита ничего не пришло, сразу возвращаем ошибку
            if (gitDatas == null || gitDatas.Count == 0)
            {
                return "Не удалось получить данные с сервера обновлений.";
            }

            List<string> actual = new List<string>();
            List<string> toUpdate = new List<string>();
            List<string> notInstalled = new List<string>();

            foreach (var gitPlugin in gitDatas)
            {
                // Отрезаем расширение (.dll или .bundle), чтобы выводить пользователю только чистое имя
                string displayName = System.IO.Path.GetFileNameWithoutExtension(gitPlugin.Name);

                string displayName2 = System.IO.Path.GetFileName(gitPlugin.Name);
                // Ищем локальный плагин по совпадению имени файла
                var localPlugin = datas.FirstOrDefault(x => x.Name.Equals(displayName2, StringComparison.OrdinalIgnoreCase));
                              
                if (localPlugin == null)
                {
                    notInstalled.Add($" - {displayName}");
                }
                else
                {
                    // Вычисляем разницу во времени между локальным файлом и файлом в ZIP
                    TimeSpan timeDiff = gitPlugin.Date - localPlugin.Date;

                    // Если файл на Гите новее локального больше чем на 2 секунды (учитывая DOS-точность)
                    if (timeDiff.TotalSeconds > 2)
                    {
                        string localDateStr = localPlugin.Date.ToString("dd.MM.yyyy HH:mm");
                        string gitDateStr = gitPlugin.Date.ToString("dd.MM.yyyy HH:mm");

                        toUpdate.Add($" - {displayName} (установлен: {localDateStr}, доступно: {gitDateStr})");
                    }
                    else
                    {
                        actual.Add($" - {displayName}");
                    }
                }
            }

            // Собираем финальную строку отчета
            StringBuilder sb = new StringBuilder();

            if (toUpdate.Count > 0)
            {
                sb.AppendLine("Доступны обновления:");
                foreach (string s in toUpdate) sb.AppendLine(s);
                sb.AppendLine(); // Пустая строка-разделитель
            }

            if (actual.Count > 0)
            {
                sb.AppendLine("Версии актуальны:");
                foreach (string s in actual) sb.AppendLine(s);
                sb.AppendLine();
            }

            if (notInstalled.Count > 0)
            {
                sb.AppendLine("Доступные, но не установленные плагины:");
                foreach (string s in notInstalled) sb.AppendLine(s);
            }

            // Если вдруг все списки пустые (маловероятно, но для безопасности)
            if (sb.Length == 0)
            {
                return "Плагины не найдены.";
            }

            return sb.ToString().TrimEnd(); // Убираем лишние переносы строк в самом конце
        }
        private static List<MetaData> GetOurPlugins()
        {
            List<MetaData> result = new List<MetaData>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location)) continue;
                    // Ищем тип по точному названию класса контроля версий   
                    if (assembly.GetType(typeof(ControlVersionClass).FullName) != null)
                    {
                        result.Add(new MetaData(assembly));
                    }
                }
                catch { }
            }

            return result;
        }
        private static Window CreateWindow(string text = "", bool okButton = false)
        {
            // 1. Проверяем, используется ли дефолтный текст
            bool isDefaultText = string.IsNullOrEmpty(text);

            // 2. Создаем сетку для разметки
            Grid rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            // 3. Настраиваем текстовый блок с динамическим выравниванием
            TextBlock textBlock = new TextBlock()
            {
                Text = isDefaultText ? "Идет проверка данных" : text,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = isDefaultText ? HorizontalAlignment.Center : HorizontalAlignment.Left,
                TextWrapping = TextWrapping.NoWrap,
                Margin = new Thickness(15, 15, 15, okButton ? 20 : 15)
            };
            Grid.SetRow(textBlock, 0);
            rootGrid.Children.Add(textBlock);

            // 4. Создаем окно с автоматическим размером под контент
            Window window = new Window
            {
                WindowStyle = WindowStyle.None,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                Content = rootGrid
            };

            // 5. Добавляем кнопку ОК, используя полное пространство имен для предотвращения конфликтов
            if (okButton)
            {
                System.Windows.Controls.Button btnOk = new System.Windows.Controls.Button()
                {
                    Content = "OK",
                    Width = 75,
                    Height = 23,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 0, 15, 15)
                };
                btnOk.Click += (s, e) => window.Close();

                Grid.SetRow(btnOk, 1);
                rootGrid.Children.Add(btnOk);
            }

            // 6. Привязываем к главному окну AutoCAD через Win32 Handle
            try
            {
                IntPtr acadMainWindowHandle = Autodesk.AutoCAD.ApplicationServices.Application.MainWindow.Handle;
                WindowInteropHelper helper = new WindowInteropHelper(window);
                helper.Owner = acadMainWindowHandle;
            }
            catch 
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            return window;
        }
        private static List<MetaData> GetGitData()
        {
            List<MetaData> result = new List<MetaData>();

            string appVersion = Autodesk.AutoCAD.ApplicationServices.Application.Version.Major < 25
                ? "2021"
                : (Autodesk.AutoCAD.ApplicationServices.Application.Version.Major >= 27
                ? "2027"
                : "2025");

            using (HttpClient client = new HttpClient() { Timeout = TimeSpan.FromSeconds(8) })
            {
                // GitHub API требует User-Agent
                client.DefaultRequestHeaders.Add("User-Agent", "C# App");

                List<string> actual = new List<string>();
                List<string> toUpdate = new List<string>();
                List<string> notInstalled = new List<string>();

                try
                {
                    string json = client.GetStringAsync($"https://api.github.com/repos/Alzslnc/AcadPlugins/contents/").Result;

                    int index = 0;

                    while (true)
                    {
                        // Ищем каждый конкретный файл по его имени/расширению
                        int bundlePos = json.IndexOf(".bundle", index);
                        if (bundlePos == -1) break;

                        // Находим границы именно этого JSON-объекта (от { до })
                        int startObject = json.LastIndexOf("{", bundlePos);
                        int endObject = json.IndexOf("}", bundlePos);

                        if (startObject == -1 || endObject == -1) break;

                        // Вырезаем только строку одного конкретного плагина
                        string doc = json.Substring(startObject, endObject - startObject);

                        // Смещаем индекс для следующего поиска
                        index = endObject + 1;

                        if (doc.Length < 20 || !doc.Contains(".bundle")) continue;

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

                        try
                        {
                            if (uint.TryParse(size, out uint sizeL) && sizeL > 2048)
                            {
                                client.DefaultRequestHeaders.Range = new RangeHeaderValue(sizeL - 2048, sizeL);
                            }

                            byte[] archiveTail = client.GetByteArrayAsync(path).Result;
                            MemoryStream fs = null;
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

                                if (eocdPos == -1) continue;

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

                                    if (fileName.EndsWith($"{appVersion}.dll"))
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
                                            result.Add(new MetaData { Name = fileName, Date = dt });
                                        }
                                        catch
                                        {
                                        }
                                    }
                                    // Пропускаем доп. поля и комментарий файла, чтобы попасть на следующую запись
                                    fs.Seek(eLen + cLen, SeekOrigin.Current);
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
                catch
                {
                }

                return result;
            }
        }

        private class MetaData
        {
            public MetaData(Assembly assembly)
            {

                FileInfo fileInfo = new FileInfo(assembly.Location);

                //полное имя сборки
                Name = fileInfo.Name;

                //устанавливаем время записи файла
                Date = fileInfo.LastWriteTime;

                //сначала устанавливаем место именно содержащую плагин папку
                DirectoryInfo directory = fileInfo.Directory;
                Path = directory.FullName;

                //если плагин в бандл папке то местом выбираем папку с бандл папкой
                while (directory != null && directory.FullName.Contains(".bundle")) directory = directory.Parent;
                if (directory != null) Path = directory.FullName;
            }
            public MetaData()
            {
            }
            public string Name { get; set; }
            public DateTime Date { get; set; }
            public string Path { get; set; }
        }
    }
}
