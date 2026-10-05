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


        const string SourcePath10 = @"\\10.37.128.210\temp\stroevka 27";
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
            var args = Environment.GetCommandLineArgs();

            // Режим --do-update (второй инстанс из %TEMP%)
            if (args.Length >= 4 && args[1] == "--do-update")
            {
                oldVersionFolder = CleanArg(args[2]).TrimEnd(Path.DirectorySeparatorChar);
                serverFolder = CleanArg(args[3]).TrimEnd(Path.DirectorySeparatorChar);
                destinationRoot = args.Length > 4
                                   ? CleanArg(args[4]).TrimEnd(Path.DirectorySeparatorChar)
                                   : @"D:";
                Log.Write($"Режим --do-update");
                Log.Write($"  old    = {oldVersionFolder}");
                Log.Write($"  server = {serverFolder}");
                Log.Write($"  dst    = {destinationRoot}");
                Thread.Sleep(1200);
                BeginFullUpdate();
                return;
            }

            // Режим --from-stroevka (запущены из stroevkaI)
            if (args.Length >= 3 && args[1] == "--from-stroevka")
            {
                fromStroevka = true;
                fromStroevkaDir = args[2].TrimEnd(Path.DirectorySeparatorChar);
                Log.Write($"Режим --from-stroevka, old={fromStroevkaDir}");
            }
            else
            {
                // fallback: если запущены из папки версии — тоже считаем «из stroevka»
                string currentDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
                string currentName = Path.GetFileName(currentDir);
                if (currentName.StartsWith("stroevka 27 ", StringComparison.OrdinalIgnoreCase))
                {
                    fromStroevka = true;
                    fromStroevkaDir = currentDir;
                    Log.Write($"Режим определён по имени папки: {currentDir}");
                }
                else
                {
                    Log.Write("Автономный режим (аргументы не заданы, имя папки не совпало)");
                }
            }

            InitUi();
        }

        //private void Form1_Load(object sender, EventArgs e)
        //{
        //    Log.Write("=== Запуск модуля обновления ===");

        //    var args = Environment.GetCommandLineArgs();

        //    // режим «из stroevka» (запуск из главной программы)
        //    if (args.Length >= 3 && args[1] == "--from-stroevka")
        //    {
        //        oldVersionFolder = args[2];
        //        Log.Write($"Режим --from-stroevka, old={oldVersionFolder}");
        //        fromStroevka = true;
        //        InitUi();  // покажем UI и дадим пользователю нажать «Обновить»
        //        return;
        //    }


        //    // Режим перезапуска из %TEMP%: --do-update "<oldFolder>" "<serverFolder>" "<dst>"
        //    if (args.Length >= 4 && args[1] == "--do-update")
        //    {
        //        oldVersionFolder = args[2];
        //        serverFolder = args[3];
        //        destinationRoot = args.Length > 4 ? args[4] : @"D:\";

        //        Log.Write($"Режим --do-update");
        //        Log.Write($"  old    = {oldVersionFolder}");
        //        Log.Write($"  server = {serverFolder}");
        //        Log.Write($"  dst    = {destinationRoot}");

        //        // Дадим родителю время освободить папку
        //        Thread.Sleep(1200);

        //        BeginFullUpdate();
        //        return;
        //    }

        //    // Обычный UI
        //    InitUi();
        //}

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

            // 1. Ищем папку с самым свежим stroevkaI.exe
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

            // 2. Спрашиваем
            Log.Write($"Найден файл обновления от {dateStr}: {exePath}");
            var res = MessageBox.Show(
                $"Найден файл с обновлением от {dateStr}.\n\n" +
                $"Каталог новой версии: {targetName}\n\n" +
                "Обновить программу?",
                "Обновление строевки",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);

            if (res != DialogResult.OK)
            {
                Log.Write("Пользователь отказался от обновления.");
                return;
            }

            // 3. Определяем, запущены ли мы из папки версии stroevka
            string currentDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            string currentName = Path.GetFileName(currentDir);
            bool fromVersionFolder = currentName.StartsWith(
                "stroevka 27 ", StringComparison.OrdinalIgnoreCase);




            if (fromStroevka && !string.IsNullOrEmpty(fromStroevkaDir))
            {
                oldVersionFolder = fromStroevkaDir;
                serverFolder = folder;
                Log.Write($"Режим: обновление из stroevka. Old={oldVersionFolder}. Перезапуск из %TEMP%.");
                RelaunchFromTemp(oldVersionFolder, folder, destinationRoot);
            }
            else
            {
                Log.Write("Режим: автономный. Копирование без перезапуска.");
                RunStandaloneCopy(folder);
            }
            //if (fromVersionFolder)
            //{
            //    // Режим обновления из stroevka
            //    oldVersionFolder = currentDir;
            //    serverFolder = folder;
            //    Log.Write("Режим: обновление из stroevka. Перезапуск из %TEMP%.");
            //    RelaunchFromTemp(currentDir, folder, destinationRoot);
            //}
            //else
            //{
            //    // Автономный режим
            //    Log.Write("Режим: автономный. Копирование без перезапуска.");
            //    RunStandaloneCopy(folder);
            //}
        }

        // -------------------------------------------------------------
        // Автономный режим: копирование + ярлык
        // -------------------------------------------------------------
        private void RunStandaloneCopy(string folder)
        {
            btnUpdate.Enabled = false;
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
                    Log.Write($"Автономно: копируем {folder} -> {targetFolder}");

                    // 1. Старая папка, в которой уже лежит работающая версия
                    //    (в автономном режиме это destinationRoot\targetName,
                    //     но может быть и старая папка с другой датой)
                    string actualOldFolder = null;

                    // 1a. Если в целевом каталоге уже лежит версия — её надо сохранить
                    if (Directory.Exists(targetFolder))
                    {
                        // сначала поищем внутри старый exe
                        string oldExeInside = Path.Combine(targetFolder, "stroevkaI.exe");
                        if (File.Exists(oldExeInside))
                        {
                            // переименовываем целевую папку в "… last"
                            string backup = targetFolder + " last";
                            if (Directory.Exists(backup))
                            {
                                Log.Write($"Удаляем прежний бэкап: {backup}");
                                Directory.Delete(backup, true);
                            }
                            Log.Write($"Существующий каталог -> {backup}");
                            Directory.Move(targetFolder, backup);
                            actualOldFolder = backup;
                        }
                        else
                        {
                            // пустой/битый каталог — просто удалим
                            Log.Write($"Каталог {targetFolder} без stroevkaI.exe — удаляем");
                            Directory.Delete(targetFolder, true);
                        }
                    }

                    // 2. Копирование новой версии
                    Log.Write("Копирование файлов новой версии...");
                    CopyDirectory(folder, targetFolder, true, bgw);
                    Log.Write("Копирование завершено.");

                    // 3. Ярлык на новую версию
                    string newExe = Path.Combine(targetFolder, "stroevkaI.exe");
                    ReplaceShortcut("stroevka 27.lnk", newExe, targetFolder);

                    // 4. Ярлык на предыдущую версию (если она была сохранена)
                    string lastShortcut = null;
                    if (actualOldFolder != null)
                    {
                        string oldExePath = Path.Combine(actualOldFolder, "stroevkaI.exe");
                        if (File.Exists(oldExePath))
                        {
                            lastShortcut = "stroevka 27 last.lnk";
                            Log.Write($"Создаём last-ярлык: Target={oldExePath} WorkDir={actualOldFolder}");
                            ReplaceShortcut(lastShortcut, oldExePath, actualOldFolder);
                        }
                    }

                    e.Result = new { Target = targetFolder, Last = lastShortcut };
                }
                catch (Exception ex) { e.Result = ex; }
            };

            bgw.RunWorkerCompleted += (s, e) =>
            {
                btnUpdate.Enabled = true;
                progressBar1.Visible = false;

                if (e.Result is Exception ex)
                {
                    Log.Write($"ОШИБКА: {ex.Message}");
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                dynamic r = e.Result;
                string tgt = (string)r.Target;
                string last = (string)r.Last;

                string msg =
                    $"Программа обновлена до версии от {dateStr}.\n\n" +
                    $"Каталог: {tgt}\n";

                if (!string.IsNullOrEmpty(last))
                    msg += $"Старая версия может быть запущена с помощью ярлыка:\n    \"{last}\"";
                else
                    msg += "Предыдущая версия не сохранилась (нечего было бэкапить).";

                Log.Write($"Автономное обновление завершено: {tgt}, last={last}");
                MessageBox.Show(msg, "Обновление завершено",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            bgw.RunWorkerAsync();
        }

        // -------------------------------------------------------------
        // Режим из stroevka: копируем себя в %TEMP% и перезапускаемся
        // -------------------------------------------------------------
        // -------------------------------------------------------------
        // Режим из stroevka: копируем себя в %TEMP% и перезапускаемся
        // -------------------------------------------------------------
        private void RelaunchFromTemp(string currentDir, string folder, string dst)
        {
            // Страховка: убираем завершающий '\' и случайные кавычки
            currentDir = currentDir.TrimEnd(Path.DirectorySeparatorChar).Trim('"', ' ');
            folder = folder.Trim('"', ' ');
            dst = dst.TrimEnd(Path.DirectorySeparatorChar).Trim('"', ' ');

            string tempDir = Path.Combine(Path.GetTempPath(), "stroevkaUpdate");
            Directory.CreateDirectory(tempDir);

            // Копируем свои exe/dll/json
            foreach (var pattern in new[] { "stroevkaUpdate*", "Interop.*.dll" })
                foreach (var f in Directory.GetFiles(currentDir, pattern))
                    try { File.Copy(f, Path.Combine(tempDir, Path.GetFileName(f)), true); } catch { }

            // На всякий случай — сам exe
            try
            {
                string myExe = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(myExe))
                    File.Copy(myExe, Path.Combine(tempDir, Path.GetFileName(myExe)), true);
            }
            catch { }

            string tempExe = Path.Combine(tempDir, "stroevkaUpdate.exe");
            Log.Write($"Перезапуск из {tempExe} с параметрами --do-update");
            Log.Write($"  currentDir = {currentDir}");
            Log.Write($"  folder     = {folder}");
            Log.Write($"  dst        = {dst}");

            var psi = new ProcessStartInfo
            {
                FileName = tempExe,
                UseShellExecute = false
            };
            psi.ArgumentList.Add("--do-update");
            psi.ArgumentList.Add(currentDir);
            psi.ArgumentList.Add(folder);
            psi.ArgumentList.Add(dst);

            Process.Start(psi);

            Application.Exit();
        }

        // -------------------------------------------------------------
        // Полная процедура (из %TEMP%)
        // -------------------------------------------------------------
        private void BeginFullUpdate()
        {
            btnUpdate.Enabled = false;
            PrepareProgress(serverFolder);   // <-- счёт файлов

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
                try { PerformFullUpdate(); }
                catch (Exception ex)
                {
                    Log.Write($"ОШИБКА: {ex.Message}");
                    e.Result = ex;
                }
            };

            bgw.RunWorkerCompleted += (s, e) =>
            {
                if (e.Result is Exception ex)
                    MessageBox.Show($"Ошибка обновления: {ex.Message}", "Ошибка",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            };

            bgw.RunWorkerAsync();
        }

        private void PerformFullUpdate()
        {
            string serverExe = Path.Combine(serverFolder, "stroevkaI.exe");
            if (!File.Exists(serverExe))
                throw new Exception("Не найден stroevkaI.exe: " + serverExe);

            dateStr = File.GetLastWriteTime(serverExe).ToString("dd-MM-yy");
            targetName = $"stroevka 27 {dateStr}";
            targetName = targetName.Trim();
            string targetFolder = Path.Combine(destinationRoot, targetName);

            Log.Write($"Full update:");
            Log.Write($"  serverFolder  = {serverFolder}");
            Log.Write($"  oldVersionDir = {oldVersionFolder}");
            Log.Write($"  targetFolder  = {targetFolder}");

            bool sameName = string.Equals(
                Path.GetFileName(oldVersionFolder), targetName,
                StringComparison.OrdinalIgnoreCase);

            // 1. Старая папка (та, из которой запущено обновление)
            string actualOldFolder = oldVersionFolder;

            // 1a. Если её имя совпадает с целевым — сначала в "last"
            if (sameName && Directory.Exists(oldVersionFolder))
            {
                string oldBackup = oldVersionFolder + " last";
                if (Directory.Exists(oldBackup))
                {
                    Log.Write($"Удаляем прежний бэкап: {oldBackup}");
                    Directory.Delete(oldBackup, true);
                }
                Log.Write($"Переименовываем старую версию: {oldVersionFolder} -> {oldBackup}");
                Directory.Move(oldVersionFolder, oldBackup);
                actualOldFolder = oldBackup;
            }
            else
            {
                Log.Write($"Переименование старой версии не требуется (имя = {Path.GetFileName(oldVersionFolder)})");
            }

            // 2. Если целевая папка уже существует — в "last"
            if (Directory.Exists(targetFolder))
            {
                string backup = targetFolder + " last";
                if (Directory.Exists(backup))
                {
                    Log.Write($"Удаляем прежний бэкап: {backup}");
                    Directory.Delete(backup, true);
                }
                Log.Write($"Существующий целевой каталог -> {backup}");
                Directory.Move(targetFolder, backup);
            }

            // 3. Копирование
            Log.Write("Копирование файлов новой версии...");
            CopyDirectory(serverFolder, targetFolder, true, bgw);
            Log.Write("Копирование завершено.");

            // 4. Основной ярлык ? новая версия
            string newExe = Path.Combine(targetFolder, "stroevkaI.exe");
            ReplaceShortcut("stroevka 27.lnk", newExe, targetFolder);

            // 5. Ярлык "last" ? старая версия
            //    Проверяем фактическое существование папки, а не assumed
            string oldExePath = null;
            string finalOldFolder = null;

            if (Directory.Exists(actualOldFolder))
            {
                string candidate = Path.Combine(actualOldFolder, "stroevkaI.exe");
                if (File.Exists(candidate))
                {
                    oldExePath = candidate;
                    finalOldFolder = actualOldFolder;
                }
            }

            // fallback: возможно, папка переименовалась иначе
            if (oldExePath == null && Directory.Exists(oldVersionFolder))
            {
                string candidate = Path.Combine(oldVersionFolder, "stroevkaI.exe");
                if (File.Exists(candidate))
                {
                    oldExePath = candidate;
                    finalOldFolder = oldVersionFolder;
                }
            }

            string lastShortcut = "stroevka 27 last.lnk";

            if (oldExePath != null)
            {
                Log.Write($"Создаём last-ярлык: Target={oldExePath} WorkDir={finalOldFolder}");
                ReplaceShortcut(lastShortcut, oldExePath, finalOldFolder);
            }
            else
            {
                Log.Write($"Старый exe не найден, ярлык 'last' не создан.");
                lastShortcut = "(старая версия недоступна)";
            }

            // 6. Убиваем stroevkaI
            KillProcess("stroevkaI");
            Thread.Sleep(1500);

            // 7. Запускаем новую версию
            Log.Write($"Запуск новой версии: {newExe}");
            Process.Start(new ProcessStartInfo
            {
                FileName = newExe,
                WorkingDirectory = targetFolder
            });

            // 8. Финальное сообщение
            Log.Write($"Обновление завершено. Старая версия: \"{lastShortcut}\"");
            MessageBox.Show(
                $"Программа обновлена до версии от {dateStr}.\n\n" +
                $"Старая версия может быть запущена с помощью ярлыка:\n" +
                $"    \"{lastShortcut}\"",
                "Обновление завершено",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void KillProcess(string name)
        {
            foreach (var p in Process.GetProcessesByName(name).ToList())
            {
                try
                {
                    Log.Write($"Останавливаем процесс: {name} (PID {p.Id})");
                    p.Kill();
                }
                catch (Exception ex)
                {
                    Log.Write($"Не удалось остановить {name}: {ex.Message}");
                }
            }
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

    }
}