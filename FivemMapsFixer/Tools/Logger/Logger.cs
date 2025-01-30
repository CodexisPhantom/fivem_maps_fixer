using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace FivemMapsFixer.Tools;

public static class Logger
{
    private static readonly Queue<string> Logs = [];
    private static readonly string LogFile;
    private static readonly string LogFolder;
    private static readonly object Locker = new();

    static Logger()
    {
        LogFolder = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        LogFile = Path.Combine(LogFolder,DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")+".log");
        AppDomain.CurrentDomain.ProcessExit += (_,_) => OnExitLogger();
    }

    private static void OnExitLogger()
    {
        Log(LogSeverity.INFO, LogType.APPLICATION, "Application is closing");
        WriteToFile();
    }
    
    public static void Log(LogSeverity severity, LogType type, string message)
    {
        lock (Locker)
        {
            ConsoleColor defaultColor = Console.ForegroundColor;
            StringBuilder sb = new();
            Log(sb, DateTime.Now.ToString(CultureInfo.InvariantCulture));

            string logPrefix = severity switch
            {
                LogSeverity.INFO => " [INFO]",
                LogSeverity.WARNING => " [WARNING]",
                LogSeverity.ERROR => " [ERROR]",
                _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null)
            };

            Console.ForegroundColor = severity switch
            {
                LogSeverity.INFO => ConsoleColor.Green,
                LogSeverity.WARNING => ConsoleColor.Yellow,
                LogSeverity.ERROR => ConsoleColor.Red,
                _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null)
            };

            Log(sb, logPrefix);
            Console.ForegroundColor = defaultColor;
            Log(sb, " ("+type+") ");
            Log(sb, message + "\n");
            AddLogToFile(sb.ToString());
            Console.ResetColor();
        }
    }

    private static void Log(StringBuilder sb,string log)
    {
        Console.WriteLine(log);
        sb.Append(log);
    }

    private static void AddLogToFile(string newLog,int minLogs = 10)
    {
        Logs.Enqueue(newLog);
        if(Logs.Count < minLogs) return;
        WriteToFile();
    }

    private static void WriteToFile()
    {
        lock (Locker)
        {
            if (!Directory.Exists(LogFolder)) { Directory.CreateDirectory(LogFolder); }
            FileStream fileStream = File.Open(LogFile, FileMode.Append,FileAccess.Write);
            while (Logs.Count > 0)
            {
                string log = Logs.Dequeue();
                byte[] info = new UTF8Encoding(true).GetBytes(log + "\n");
                fileStream.Write(info, 0, info.Length);
            }
            fileStream.Close();
        }
    }
}