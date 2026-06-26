using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BaseFunction
{
    public static class ControlVersionClass
    {
        static ControlVersionClass()
        {
            Load();
            FirstLoad = true;
        }
        public static void Save()
        {
            ControlVersion controlVersion = new ControlVersion() { VersionDatas = VersionDatas, OpenTime = OpenTime };
            FoldersClass folders = new FoldersClass() { Folders = Folders };
            BaseXMLClass.SetSerialisationResult(SavePath, controlVersion);
            BaseXMLClass.SetSerialisationResult(SavePath2, folders);
        }
        public static void Load()
        {
            if (!File.Exists(SavePath) || !File.Exists(SavePath2)) return;
            if (BaseXMLClass.GetSerialisationResult(SavePath, typeof(ControlVersion)) is ControlVersion data)
            {
                if ((OpenTime - data.OpenTime).TotalSeconds < 100 || !FirstLoad)
                {
                    VersionDatas = data.VersionDatas;
                }
                else Save();
            }
            if (BaseXMLClass.GetSerialisationResult(SavePath2, typeof(FoldersClass)) is FoldersClass folders)
            {
                if ((OpenTime - folders.OpenTime).TotalSeconds < 100 || !FirstLoad)
                {
                    Folders = folders.Folders;
                }
                else Save();
            }
        }
        public static void Terminate()
        {
            try
            {
            }
            catch { }
        }
        private static readonly object Lock = new object();
        private static string LoadedData { get; set; } = string.Empty;
        private static readonly string SavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ControlVersion.xml");
        private static readonly string SavePath2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Folders.xml");
        private static bool _ch = false;
        private static bool FirstLoad = true;
        public class ControlVersion
        {
            public ControlVersion() { }
            public List<VersionData> VersionDatas { get; set; } = new List<VersionData>();
            public DateTime OpenTime { get; set; } = DateTime.MinValue;
        }
        public class FoldersClass
        {
            public FoldersClass() { }
            public List<string> Folders { get; set; } = new List<string>();
            public DateTime OpenTime { get; set; } = DateTime.MinValue;
        }
        public class VersionData
        {
            public string Name;
            public DateTime Date;
        }
        public static List<VersionData> VersionDatas { get; set; } = new List<VersionData>();
        public static DateTime OpenTime { get; set; } = DateTime.UtcNow;
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
            if (_ch) return;
            _ch = true;
            try
            {
                using (Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument.LockDocument())
                {
                    LoadedData = string.Empty;

                    ControlVersionClass.Load();

                    Task task = Task.Factory.StartNew(() => { GetGitData(); });

                    Window window = null;

                    Task task2 = Task.Factory.StartNew(() =>
                    {
                        int timer = 0;

                        while (timer++ < 20 || !task.IsCompleted)
                        {
                            System.Threading.Thread.Sleep(500);
                        }

                        if (window != null)
                        {
                            try
                            {
                                window.Dispatcher.Invoke(() => window.Close());
                            }
                            catch { }
                        }
                    });

                    window = CreateWindow();
                    window.ShowDialog();

                    System.Threading.Thread.Sleep(500);

                    lock (Lock)
                    {
                        if (string.IsNullOrEmpty(LoadedData)) LoadedData = "Не удалось проверить.";
                    }

                    System.Windows.MessageBox.Show(LoadedData);
                }
            }
            finally { _ch = false; }
        }

        private static Window CreateWindow()
        {
            Grid rootGrid = new Grid();

            TextBlock textBlock = new TextBlock() { Margin = new Thickness(5), Text = "Идет проверка данных", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Height = 20 };

            rootGrid.Children.Add(textBlock);

            return new Window { WindowStyle = WindowStyle.None, WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = rootGrid, Width = 400, Height = 200, MaxHeight = 200, MaxWidth = 400 };
        }

        private static void GetGitData()
        {
            string report = "";

            using (HttpClient client = new HttpClient() { Timeout = TimeSpan.FromSeconds(8) })
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

                }
                catch (System.Exception ex)
                {
                    report = ex.Message;
                }

                lock (Lock)
                {
                    LoadedData = report;
                }
            }
        }
    }
}
