using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using stroevkaUpdate.Services;
using Wsh = IWshRuntimeLibrary;

namespace stroevkaUpdate
{
    public partial class Form1 : Form
    {
        #region Поля

        bool fromStroevka = false;
        string fromStroevkaDir = null;
        private int _panel2Height = -1;
        private int _normalFormHeight = -1;


        const string SourcePath10 = @"\\10.37.128.210\temp\СПТ\stroevka 27";
        const string SourcePath192 = @"\\192.168.3.75\stroevka 27";

        string sourcePath;          // для автономного режима — каталог на сервере
        string serverFolder;        // для режима из stroevka — папка новой версии на сервере
        string destinationRoot = @"D:\";
        string dateStr;             // "dd-MM-yy"
        string targetName;          // "stroevka 27 dd-mm-yy"
        string oldVersionFolder;    // только в режиме из stroevka

        int totalFiles;        // всего файлов в источнике
        int copiedFiles;       // скопировано

        BackgroundWorker bgw = new BackgroundWorker();
        #endregion

        public Form1()
        {
            InitializeComponent();
            // Запоминаем высоты, пока Panel2 ещё видима в дизайнере
            _panel2Height = splitContainer1.Panel2.Height;
            _normalFormHeight = this.Height - _panel2Height - splitContainer1.SplitterWidth;

            // Сразу прячем Panel2 и подгоняем форму
            splitContainer1.Panel2Collapsed = true;
            this.Height = _normalFormHeight;
        }

        // -------------------------------------------------------------
        // Запуск
        // -------------------------------------------------------------
        private static string CleanArg(string s)
                => string.IsNullOrEmpty(s) ? s : s.Trim('"', ' ', '\t');
        private void Form1_Load(object sender, EventArgs e)
        {
            Log.Write("=== Запуск модуля обновления ===");
            InitUi();
        }

        private void InitUi()
        {
            cmbDisk.Items.Clear();
            foreach (var d in DriveInfo.GetDrives())
                if (d.DriveType == DriveType.Fixed) cmbDisk.Items.Add(d.Name);

            if (cmbDisk.Items.Contains(@"D:\")) cmbDisk.SelectedItem = @"D:\";
            else if (cmbDisk.Items.Count > 0) cmbDisk.SelectedIndex = 0;

            sourcePath = DetectSourceByIp();
            if (sourcePath == null)
            {
                Log.Write("ОШИБКА: не удалось определить источник по IP.");
                MessageBox.Show(
                    "Не удалось определить доступный каталог-источник по IP-адресу компьютера.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            Log.Write($"Источник: {sourcePath}");
            cataloglabel.Text = $"Источник: {sourcePath}";
        }

        // -------------------------------------------------------------
        // Кнопка "Обновить программу"
        // -------------------------------------------------------------
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(sourcePath))
            {
                MessageBox.Show("Источник не выбран.", "Ошибка");
                return;
            }
            if (cmbDisk.SelectedItem == null)
            {
                MessageBox.Show("Выберите диск для копирования.", "Ошибка");
                return;
            }
            destinationRoot = cmbDisk.SelectedItem.ToString()
                                     .TrimEnd(Path.DirectorySeparatorChar);

            string folder = FindLatestVersionFolder();
            if (folder == null)
            {
                MessageBox.Show("Не найден stroevkaI.exe на сервере.",
                                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string exePath = Path.Combine(folder, "stroevkaI.exe");
            DateTime serverDate = File.GetLastWriteTime(exePath);

            // Имя папки — по времени копирования, а не по времени изменения exe.
            // Тогда повторное копирование той же версии не будет пытаться
            // переименовать уже существующую (занятую) папку.
            DateTime now = DateTime.Now;
            dateStr = now.ToString("dd-MM-yy");
            string timeStr = now.ToString("HH-mm");
            targetName = $"stroevka 27 {dateStr} {timeStr}";

            // --- проверка: не старая ли это версия ---
            string localExe = FindCurrentLocalExe();
            if (localExe != null)
            {
                DateTime localDate = File.GetLastWriteTime(localExe);
                Log.Write($"Сервер: {serverDate:dd-MM-yy HH-mm:ss}");
                Log.Write($"Локально: {localDate:dd-MM-yy HH-mm:ss}  ({localExe})");

                if (serverDate <= localDate)
                {
                    var ans = MessageBox.Show(
                        $"На сервере версия от {serverDate:dd-MM-yy HH-mm}.\n" +
                        $"Локально — от {localDate:dd-MM-yy HH-mm}.\n\n" +
                        "Похоже, у вас уже эта версия или новее.\n" +
                        "Всё равно скопировать обновление?",
                        "Версия не новее",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Warning);

                    if (ans != DialogResult.OK)
                    {
                        Log.Write("Пользователь отказался копировать устаревшую версию.");
                        return;
                    }
                }
            }


            Log.Write($"Найден файл обновления от {dateStr}: {exePath}");
            var res = MessageBox.Show(
                $"Найден файл с обновлением от {serverDate:dd-MM-yy HH-mm}.\n\n" +
                $"Каталог новой версии: {targetName}\n\n" +
                "Скопировать обновление?",
                "Обновление строевки",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);

            if (res != DialogResult.OK)
            {
                Log.Write("Пользователь отказался от обновления.");
                return;
            }

            RunStandaloneCopy(folder, destinationRoot);
        }

        // -------------------------------------------------------------
        // Автономный режим: копирование + ярлык
        // -------------------------------------------------------------
        private void RunStandaloneCopy(string folder, string destinationRoot)
        {

            // Гарантируем, что путь — абсолютный и с завершающим слешем
            if (!destinationRoot.EndsWith(Path.DirectorySeparatorChar.ToString()))
                destinationRoot += Path.DirectorySeparatorChar;

            btnUpdate.Enabled = false;
            ShowBottomPanel();
            PrepareProgress(folder);

            bgw = new BackgroundWorker();
            bgw.WorkerReportsProgress = true;

            bgw.ProgressChanged += (s, e) =>
            {
                progressBar1.Value = e.ProgressPercentage;
                progressStatusLabel1.Text = e.ProgressPercentage + " %";
                if (e.UserState != null)
                    progressStatusLabel2.Text = "Файл: " + e.UserState;
            };

            bgw.DoWork += (s, e) =>
            {
                try
                {
                    string targetFolder = Path.Combine(destinationRoot, targetName);


                    if (Directory.Exists(targetFolder))
                    {
                        // Совпадение в пределах одной минуты — добавляем секунды
                        string unique = $"{targetName} {DateTime.Now:ss}";
                        targetFolder = Path.Combine(destinationRoot, unique);
                        Log.Write($"Каталог занят, используем: {targetFolder}");
                    }
                    Log.Write($"Копируем {folder} -> {targetFolder}");
                    CopyDirectory(folder, targetFolder, true, bgw);

                    Log.Write("Копирование завершено.");

                    // Ярлык на новую версию с датой в имени
                    string newExe = Path.Combine(targetFolder, "stroevkaI.exe");
                    string shortcutName = $"{targetName}.lnk";
                    ReplaceShortcut(shortcutName, newExe, targetFolder);

                    // Оставляем только две последние версии
                    CleanupOldVersions(destinationRoot, targetFolder);

                    e.Result = new { Target = targetFolder, Shortcut = shortcutName };
                }
                catch (Exception ex) { e.Result = ex; }
            };

            bgw.RunWorkerCompleted += (s, e) =>
            {
                btnUpdate.Enabled = true;
                HideBottomPanel();

                if (e.Result is Exception ex)
                {
                    Log.Write($"ОШИБКА: {ex.Message}");
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                dynamic r = e.Result;
                string tgt = (string)r.Target;
                string shc = (string)r.Shortcut;

                string msg =
                    $"Обновление сохранено в каталоге:\n" +
                    $"    {tgt}\n\n" +
                    $"Создан ярлык:\n" +
                    $"    {shc}\n\n" +
                    $"Можете запустить новую версию с помощью этого ярлыка.";

                Log.Write($"Обновление завершено: {tgt}, ярлык {shc}");
                MessageBox.Show(msg, "Обновление сохранено",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Форму НЕ закрываем — оператор закроет сам
            };

            bgw.RunWorkerAsync();
        }


        // -------------------------------------------------------------
        // Вспомогательные
        // -------------------------------------------------------------
        private string DetectSourceByIp()
        {
            try
            {
                var ips = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                bool has10 = false, has192 = false;
                foreach (var ip in ips)
                {
                    string s = ip.ToString();
                    if (s.StartsWith("10.")) has10 = true;
                    if (s.StartsWith("192.168.")) has192 = true;
                }
                if (has10) return SourcePath10;
                if (has192) return SourcePath192;
            }
            catch (Exception ex) { Log.Write($"Ошибка определения IP: {ex.Message}"); }
            return null;
        }

        private string FindLatestVersionFolder()
        {
            if (string.IsNullOrEmpty(sourcePath) || !Directory.Exists(sourcePath))
                return null;

            Log.Write($"Поиск stroevkaI.exe в {sourcePath}");
            string latestExe = null;
            DateTime latestTime = DateTime.MinValue;

            foreach (var subDir in Directory.GetDirectories(sourcePath))
            {
                string exePath = Path.Combine(subDir, "stroevkaI.exe");
                if (!File.Exists(exePath)) continue;

                var t = File.GetLastWriteTime(exePath);
                Log.Write($"  найден: {exePath}  ({t:dd-MM-yy HH:mm:ss})");

                if (t > latestTime) { latestTime = t; latestExe = exePath; }
            }

            if (latestExe == null) return null;
            Log.Write($"Выбран: {latestExe}  ({latestTime:dd-MM-yy HH:mm:ss})");
            return Path.GetDirectoryName(latestExe);
        }

        //private void CopyDirectory(string src, string dst, bool recursive, BackgroundWorker worker)
        //{
        //    Directory.CreateDirectory(dst);
        //    var dir = new DirectoryInfo(src);

        //    foreach (var file in dir.GetFiles())
        //    {
        //        string target = Path.Combine(dst, file.Name);
        //        file.CopyTo(target, true);
        //        worker.ReportProgress(0, file.FullName);
        //    }
        //    if (recursive)
        //        foreach (var sub in dir.GetDirectories())
        //            CopyDirectory(sub.FullName, Path.Combine(dst, sub.Name), true, worker);
        //}

        private void CopyDirectory(string src, string dst, bool recursive, BackgroundWorker worker)
        {
            Directory.CreateDirectory(dst);
            var dir = new DirectoryInfo(src);

            foreach (var file in dir.GetFiles())
            {
                string target = Path.Combine(dst, file.Name);
                file.CopyTo(target, true);

                copiedFiles++;
                int percent = totalFiles > 0
                    ? (int)((long)copiedFiles * 100 / totalFiles)
                    : 0;
                if (percent > 100) percent = 100;

                worker.ReportProgress(percent, file.FullName);
            }

            if (recursive)
                foreach (var sub in dir.GetDirectories())
                    CopyDirectory(sub.FullName, Path.Combine(dst, sub.Name), true, worker);
        }

        private void ReplaceShortcut(string shortcutName, string exePath, string workingDir)
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string path = Path.Combine(desktop, shortcutName);
            if (File.Exists(path)) File.Delete(path);
            CreateShortcut(shortcutName, exePath, workingDir);
        }

        private void CreateShortcut(string shortcutName, string exePath, string workingDir)
        {
            if (!File.Exists(exePath))
                throw new FileNotFoundException("Не найден exe: " + exePath);

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string path = Path.Combine(desktop, shortcutName);

            var shell = new Wsh.WshShell();
            var shortcut = (Wsh.IWshShortcut)shell.CreateShortcut(path);
            shortcut.TargetPath = exePath;
            shortcut.WorkingDirectory = workingDir;
            shortcut.Description = "stroevka 27";
            shortcut.Save();

            Log.Write($"Создан ярлык: {path} -> {exePath}");
        }


        /// <summary>
        /// Настраивает прогрессбар и подготавливает счётчики.
        /// </summary>
        private void PrepareProgress(string sourceFolder)
        {
            totalFiles = Directory.EnumerateFiles(sourceFolder, "*", SearchOption.AllDirectories).Count();
            copiedFiles = 0;

            Log.Write($"Всего файлов для копирования: {totalFiles}");

            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;
            progressBar1.Visible = true;

            progressStatusLabel1.Text = "0 %";
            progressStatusLabel2.Text = "Файл: ";
        }


        private void ShowBottomPanel()
        {
            if (splitContainer1.Panel2Collapsed)
            {
                splitContainer1.Panel2Collapsed = false;
                this.Height = _normalFormHeight + _panel2Height + splitContainer1.SplitterWidth;
            }
        }


        private void HideBottomPanel()
        {
            if (!splitContainer1.Panel2Collapsed)
            {
                splitContainer1.Panel2Collapsed = true;
                this.Height = _normalFormHeight;
            }
        }
        /// <summary>
        /// Возвращает путь к stroevkaI.exe установленной (локальной) версии.
        /// Сначала смотрит в AppContext.BaseDirectory (там, откуда запущен модуль),
        /// иначе ищет самую свежую папку "stroevka 27 *" в destinationRoot.
        /// </summary>
        private string FindCurrentLocalExe()
        {
            // 1. Папка, из которой запущен сам stroevkaUpdate
            string here = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            string hereExe = Path.Combine(here, "stroevkaI.exe");
            if (File.Exists(hereExe))
            {
                Log.Write($"Локальная версия (текущая папка): {hereExe}");
                return hereExe;
            }

            // 2. Самая свежая папка stroevka 27 * в destinationRoot
            if (!Directory.Exists(destinationRoot)) return null;

            string latestExe = null;
            DateTime latestTime = DateTime.MinValue;

            foreach (var subDir in Directory.GetDirectories(destinationRoot, "stroevka 27 *"))
            {
                string exePath = Path.Combine(subDir, "stroevkaI.exe");
                if (!File.Exists(exePath)) continue;

                var t = File.GetLastWriteTime(exePath);
                if (t > latestTime) { latestTime = t; latestExe = exePath; }
            }

            if (latestExe != null)
                Log.Write($"Локальная версия (поиск): {latestExe} ({latestTime:dd-MM-yy HH-mm})");

            return latestExe;
        }
        /// <summary>
        /// Оставляет только две самые свежие папки "stroevka 27 *" в destinationRoot.
        /// Папки, которые не удалось удалить (заняты процессом), пропускаются.
        /// </summary>
        private void CleanupOldVersions(string destinationRoot, string keepNewFolder)
        {
            try
            {
                if (!Directory.Exists(destinationRoot))
                {
                    Log.Write($"CleanupOldVersions: каталог не существует: {destinationRoot}");
                    return;
                }

                // Нормализуем путь
                if (!destinationRoot.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    destinationRoot += Path.DirectorySeparatorChar;

                var dirs = Directory.GetDirectories(destinationRoot)
                                    .Select(d => new DirectoryInfo(d))
                                    .Where(d => d.Name.StartsWith("stroevka 27 ", StringComparison.OrdinalIgnoreCase))
                                    .OrderByDescending(d => d.LastWriteTime)
                                    .ToList();

                Log.Write($"CleanupOldVersions: найдено {dirs.Count} папок в {destinationRoot}");
                foreach (var d in dirs)
                    Log.Write($"  {d.Name}  ({d.LastWriteTime:dd-MM-yy HH-mm-ss})");

                var keep = new List<string>();

                string newFull = Path.GetFullPath(keepNewFolder).TrimEnd(Path.DirectorySeparatorChar);
                keep.Add(newFull);

                var previous = dirs.FirstOrDefault(d =>
                    !string.Equals(d.FullName.TrimEnd(Path.DirectorySeparatorChar),
                                   newFull, StringComparison.OrdinalIgnoreCase));

                if (previous != null)
                {
                    keep.Add(previous.FullName.TrimEnd(Path.DirectorySeparatorChar));
                    Log.Write($"Сохраняем предыдущую: {previous.FullName}");
                }

                foreach (var d in dirs)
                {
                    string full = d.FullName.TrimEnd(Path.DirectorySeparatorChar);

                    if (keep.Any(k => string.Equals(k, full, StringComparison.OrdinalIgnoreCase)))
                    {
                        Log.Write($"  оставляем: {full}");
                        continue;
                    }

                    try
                    {
                        Log.Write($"  удаляем:   {full}");
                        Directory.Delete(full, true);
                    }
                    catch (Exception ex)
                    {
                        Log.Write($"  НЕ удалось удалить {full}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write($"CleanupOldVersions error: {ex.Message}");
            }
        }
    }
}