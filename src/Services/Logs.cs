using System.Diagnostics;
using System.IO;

namespace stroevkaUpdate.Services
{
    public static class Log
    {
        private static readonly object _lock = new();
        private static readonly string _path;

        static Log()
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, $"stroevka_{DateTime.Now:yyyy-MM-dd}.log");

            // Очищаем файл при каждом запуске
            File.WriteAllText(_path, string.Empty);

            Trace.Listeners.Add(new TextWriterTraceListener(_path));
            Trace.AutoFlush = true;
        }

        public static void Write(string message)
        {
            try
            {
                lock (_lock)
                {
                    var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                    Trace.WriteLine(line);
                }
            }
            catch { /* молча */ }
        }

        public static void Mark(string stage, long elapsedMs, long deltaMs = 0)
        {
            var delta = deltaMs > 0 ? $" (+{deltaMs,5} ms)" : "";
            Write($"[{elapsedMs,6} ms]{delta} {stage}");
        }
    }
}

