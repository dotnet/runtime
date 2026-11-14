// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace SuperPmi;

[Flags]
public enum LogLevel : uint
{
    None = 0,
    Error = 0x01,       // Internal fatal errors that are non-recoverable
    Warning = 0x02,     // Internal conditions that are unusual, but not serious
    Missing = 0x04,     // Failures due to missing JIT-EE details
    Issue = 0x08,       // Issues found with the JIT, e.g. asm diffs, asserts
    Info = 0x10,        // Notifications/summaries, e.g. 'Loaded 5  Jitted 4  FailedCompile 1'
    Verbose = 0x20,     // Status messages, e.g. 'Jit startup took 151.12ms'
    Debug = 0x40,       // Detailed output that's only useful for SuperPMI debugging
    PassThroughStdout = 0x80,
    PassThroughStderr = 0x100,

    Default = Debug - 1, // Everything except debug
    All = 0xffffffff,
}

// Console and log file output. stdout is buffered; Shutdown() (or Flush()) must be called before exiting.
public static class Logger
{
    private static readonly object s_lock = new object();
    private static LogLevel s_logLevel = LogLevel.Default;
    private static FileStream s_logFile;
    private static string s_logFilePath;

    public static TextWriter Out { get; } = ConsoleWriter(new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false), 1 << 16) { AutoFlush = false });
    public static TextWriter Error { get; } = ConsoleWriter(new StreamWriter(Console.OpenStandardError(), new UTF8Encoding(false)) { AutoFlush = true });

    // stdout and stderr are in text mode, which writes "\n" as "\r\n" on Windows.
    private static TextWriter ConsoleWriter(StreamWriter writer)
        => OperatingSystem.IsWindows() ? new TextModeWriter(writer) : writer;

    private sealed class TextModeWriter : TextWriter
    {
        private readonly TextWriter _inner;

        public TextModeWriter(TextWriter inner) => _inner = inner;

        public override Encoding Encoding => _inner.Encoding;

        public override void Write(char value)
        {
            if (value == '\n')
                _inner.Write('\r');
            _inner.Write(value);
        }

        public override void Write(string value) => _inner.Write(value?.Replace("\n", "\r\n"));

        public override void Write(char[] buffer, int index, int count) => Write(new string(buffer, index, count));

        public override void Flush() => _inner.Flush();
    }

    public static LogLevel GetLogLevel() => s_logLevel;

    public static void SetLogLevel(LogLevel level) => s_logLevel = level;

    public static void Flush() => Out.Flush();

    public static void Shutdown()
    {
        Out.Flush();
        CloseLogFile();
    }

    public static void OpenLogFile(string logFilePath)
    {
        if (s_logFile != null || logFilePath == null)
            return;

        try
        {
            s_logFile = new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Delete);
            s_logFilePath = logFilePath;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Error.WriteLine($"WARNING: [Logger::OpenLogFile] Failed to open log file '{logFilePath}'. GetLastError()={ErrorHandling.LastError(e)}");
        }
    }

    public static void CloseLogFile()
    {
        if (s_logFile == null)
            return;

        bool empty = s_logFile.Length == 0;
        s_logFile.Dispose();
        s_logFile = null;

        // Avoid polluting the file system with empty log files.
        if (empty)
        {
            try
            {
                File.Delete(s_logFilePath);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Error.WriteLine($"WARNING: [Logger::CloseLogFile] remove failed. GetLastError()={ErrorHandling.LastError(e)}");
            }
        }
        s_logFilePath = null;
    }

    // Each character enables a level; 'q' (quiet) overrides everything, then 'a' (all).
    public static LogLevel ParseLogLevelString(string specifier)
    {
        if (specifier.Contains('q'))
            return LogLevel.None;
        if (specifier.Contains('a'))
            return LogLevel.All;

        LogLevel mask = LogLevel.None;
        if (specifier.Contains('e')) mask |= LogLevel.Error;
        if (specifier.Contains('w')) mask |= LogLevel.Warning;
        if (specifier.Contains('m')) mask |= LogLevel.Missing;
        if (specifier.Contains('i')) mask |= LogLevel.Issue;
        if (specifier.Contains('n')) mask |= LogLevel.Info;
        if (specifier.Contains('v')) mask |= LogLevel.Verbose;
        if (specifier.Contains('d')) mask |= LogLevel.Debug;
        return mask;
    }

    public static void LogError(string msg, [CallerMemberName] string function = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        => Log(LogLevel.Error, msg, function, file, line);

    public static void LogWarning(string msg, [CallerMemberName] string function = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        => Log(LogLevel.Warning, msg, function, file, line);

    public static void LogMissing(string msg, [CallerMemberName] string function = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        => Log(LogLevel.Missing, msg, function, file, line);

    public static void LogIssue(string msg, [CallerMemberName] string function = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        => Log(LogLevel.Issue, msg, function, file, line);

    public static void LogInfo(string msg, [CallerMemberName] string function = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        => Log(LogLevel.Info, msg, function, file, line);

    public static void LogVerbose(string msg, [CallerMemberName] string function = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        => Log(LogLevel.Verbose, msg, function, file, line);

    public static void LogDebug(string msg, [CallerMemberName] string function = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        => Log(LogLevel.Debug, msg, function, file, line);

    public static void LogPassThroughStdout(string msg) => Log(LogLevel.PassThroughStdout, msg, "", "", 0);

    public static void LogPassThroughStderr(string msg) => Log(LogLevel.PassThroughStderr, msg, "", "", 0);

    public static void LogExceptionMessage(uint code, string msg, [CallerMemberName] string function = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        => Log(LogLevel.Debug, "Exception thrown: " + msg, function, file, line);

    public static bool IsEnabled(LogLevel level)
        => level is LogLevel.PassThroughStdout or LogLevel.PassThroughStderr || (level & s_logLevel) != 0;

    public static void Log(LogLevel level, string msg, string function, string file, int line)
    {
        if (!IsEnabled(level))
            return;

        (string levelStr, bool toStderr) = level switch
        {
            LogLevel.Error => ("ERROR", true),
            LogLevel.Warning => ("WARNING", true),
            LogLevel.Missing => ("MISSING", false),
            LogLevel.Issue => ("ISSUE", false),
            LogLevel.Info => ("INFO", false),
            LogLevel.Verbose => ("VERBOSE", false),
            LogLevel.Debug => ("DEBUG", false),
            LogLevel.PassThroughStdout => ("STDOUT", false),
            LogLevel.PassThroughStderr => ("STDERR", true),
            _ => ("INVALID_LOGLEVEL", false),
        };

        lock (s_lock)
        {
            TextWriter dest = toStderr ? Error : Out;
            if (toStderr)
                Out.Flush();

            if (level < LogLevel.Info)
                dest.Write(levelStr + ": ");
            dest.Write(msg);
            dest.Write('\n');

            if (s_logFile != null)
            {
                string timeStr = OperatingSystem.IsWindows() ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : "";
                byte[] entry = Encoding.UTF8.GetBytes($"{timeStr} - {function} [{Path.GetFileName(file)}:{line}] - {levelStr} - {msg}\r\n");
                s_logFile.Write(entry);
                s_logFile.Flush();
            }
        }
    }
}
