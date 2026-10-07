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
            DateTime fileDate = File.GetLastWriteTime(exePath);
            dateStr = fileDate.ToString("dd-MM-yy");
            targetName = $"stroevka 27 {dateStr}";

            Log.Write($"Найден файл обновления от {dateStr}: {exePath}");
            var res = MessageBox.Show(
                $"Найден файл с обновлением от {dateStr}.\n\n" +
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

            RunStandaloneCopy(folder);
        }

        // -------------------------------------------------------------
        // Автономный режим: копирование + ярлык
        // -------------------------------------------------------------
        private void RunStandaloneCopy(string folder)
        {
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
                    Log.Write($"Копируем {folder} -> {targetFolder}");

                    // Если каталог с таким именем уже есть — в "last" (без удаления)
                    if (Directory.Exists(targetFolder))
                    {
                        string backup = targetFolder + " last";
                        if (Directory.Exists(backup))
                        {
                            Log.Write($"Удаляем прежний бэкап: {backup}");
                            Directory.Delete(backup, true);
                        }
                        Log.Write($"Существующий каталог -> {backup}");
                        Directory.Move(targetFolder, backup);
                    }

                    Log.Write("Копирование файлов новой версии...");
                    CopyDirectory(folder, targetFolder, true, bgw);
                    Log.Write("Копирование завершено.");

                    // Ярлык на новую версию с датой в имени
                    string newExe = Path.Combine(targetFolder, "stroevkaI.exe");
                    string shortcutName = $"stroevka 27 {dateStr}.lnk";
                    ReplaceShortcut(shortcutName, newExe, targetFolder);

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
                this.Height += splitContainer1.Panel2.Height;   // растянуть
            }
        }

        private void HideBottomPanel()
        {
            if (!splitContainer1.Panel2Collapsed)
            {
                this.Height -= splitContainer1.Panel2.Height;   // сжать
                splitContainer1.Panel2Collapsed = true;
            }
        }

    }
}