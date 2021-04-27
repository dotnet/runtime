// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class ProcessParameters
{
    /// <summary>
    /// Maximum time for CPAOT / Crossgen compilation.
    /// </summary>
    public const int DefaultIlcTimeout = 10 * 60 * 1000;

    /// <summary>
    /// Increase compilation timeout for composite builds.
    /// </summary>
    public const int DefaultIlcCompositeTimeout = 30 * 60 * 1000;

    /// <summary>
    /// Test execution timeout.
    /// </summary>
    public const int DefaultExeTimeout = 5 * 60 * 1000;

    /// <summary>
    /// Test execution timeout under GC stress mode.
    /// </summary>
    public const int DefaultExeTimeoutGCStress = 2000 * 1000;

    public string ProcessPath;
    public string Arguments;
    public Dictionary<string, string> EnvironmentOverrides = new Dictionary<string, string>();
    public string LogPath;
    public int TimeoutMilliseconds;
    public int ExpectedExitCode;
    public IEnumerable<string> InputFileNames;
    public string OutputFileName;
    public long CompilationCostHeuristic;
    public bool CollectJittedMethods;
    public IEnumerable<string> MonitorModules;
    public IEnumerable<string> MonitorFolders;
}

public abstract class ProcessConstructor
{
    public abstract ProcessParameters Construct();
}

public class ProcessInfo
{
    public ProcessConstructor Constructor;
    public ProcessParameters Parameters;

    public bool Finished;
    public bool Succeeded;
    public bool TimedOut;
    public int DurationMilliseconds;
    public int ExitCode;
    public Dictionary<string, HashSet<string>> JittedMethods;

    public bool IsEmpty => Parameters == null;

    public bool Crashed => ExitCode < -1000 * 1000;

    public ProcessInfo(ProcessConstructor constructor)
    {
        Constructor = constructor;
    }

    public bool Construct()
    {
        Parameters = Constructor.Construct();
        Constructor = null;
        return Parameters != null;
    }
}

public class ProcessRunner : IDisposable
{
    public const int StateIdle = 0;
    public const int StateRunning = 1;
    public const int StateFinishing = 2;

    public const int TimeoutExitCode = -103;

    private readonly ProcessInfo _processInfo;

    private readonly AutoResetEvent _processExitEvent;

    private readonly int _processIndex;

    private readonly int _processCount;

    private Process _process;

    private ReadyToRunJittedMethods _jittedMethods;

    private readonly Stopwatch _stopwatch;

    /// <summary>
    /// This is actually a boolean flag but we're using int to let us use CPU-native interlocked exchange.
    /// </summary>
    private int _state;

    private volatile TextWriter _logWriter;

    private CancellationTokenSource _cancellationTokenSource;

    private readonly DataReceivedEventHandler _outputHandler;

    private readonly DataReceivedEventHandler _errorHandler;

    private readonly StringBuilder _outputCapture;

    public ProcessRunner(ProcessInfo processInfo, int processIndex, int processCount, ReadyToRunJittedMethods jittedMethods, AutoResetEvent processExitEvent)
    {
        _processInfo = processInfo;
        _processIndex = processIndex;
        _processCount = processCount;
        _jittedMethods = jittedMethods;
        _processExitEvent = processExitEvent;

        _cancellationTokenSource = new CancellationTokenSource();

        _stopwatch = new Stopwatch();
        _stopwatch.Start();
        _state = StateIdle;

        _logWriter = new StreamWriter(_processInfo.Parameters.LogPath);

        if (_processInfo.Parameters.ProcessPath.IndexOf(' ') >= 0)
        {
            _logWriter.Write($"\"{_processInfo.Parameters.ProcessPath}\"");
        }
        else
        {
            _logWriter.Write(_processInfo.Parameters.ProcessPath);
        }
        _logWriter.Write(' ');
        _logWriter.WriteLine(_processInfo.Parameters.Arguments);
        _logWriter.WriteLine("<<<<");

        ProcessStartInfo psi = new ProcessStartInfo()
        {
            FileName = _processInfo.Parameters.ProcessPath,
            Arguments = _processInfo.Parameters.Arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (KeyValuePair<string, string> environmentOverride in _processInfo.Parameters.EnvironmentOverrides)
        {
            psi.EnvironmentVariables[environmentOverride.Key] = environmentOverride.Value;
        }

        _process = new Process();
        _process.StartInfo = psi;
        _process.EnableRaisingEvents = true;
        _process.Exited += new EventHandler(ExitEventHandler);

        Interlocked.Exchange(ref _state, StateRunning);

        _process.Start();
        if (_processInfo.Parameters.CollectJittedMethods)
        {
            _jittedMethods.AddProcessMapping(_processInfo, _process);
        }

        _outputCapture = new StringBuilder();

        _outputHandler = new DataReceivedEventHandler(StandardOutputEventHandler);
        _process.OutputDataReceived += _outputHandler;
        _process.BeginOutputReadLine();

        _errorHandler = new DataReceivedEventHandler(StandardErrorEventHandler);
        _process.ErrorDataReceived += _errorHandler;
        _process.BeginErrorReadLine();

        Task.Run(TimeoutWatchdog);
    }

    public void Dispose()
    {
        CleanupProcess();
        CleanupLogWriter();
    }

    private void TimeoutWatchdog()
    {
        try
        {
            CancellationTokenSource source = _cancellationTokenSource;
            if (source != null)
            {
                Task.Delay(_processInfo.Parameters.TimeoutMilliseconds, source.Token).Wait();
                StopProcessAtomic();
            }
        }
        catch (AggregateException ae) when (ae.InnerException is TaskCanceledException)
        {
            // Ignore cancellation
        }
    }

    private void CleanupProcess()
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
        }

        // In ETW collection mode, the disposal is carried out in ReadyToRunJittedMethods
        // as we need to keep the process alive for the entire lifetime of the trace event
        // session, otherwise PID's may get recycled and we couldn't reliably back-translate
        // them into the logical process executions.
        if (_process != null && !_processInfo.Parameters.CollectJittedMethods)
        {
            _process.CancelOutputRead();
            _process.CancelErrorRead();

            _process.OutputDataReceived -= _outputHandler;
            _process.ErrorDataReceived -= _errorHandler;

            _process.Dispose();
            _process = null;
        }
    }

    private void CleanupLogWriter()
    {
        TextWriter logWriter = _logWriter;
        if (logWriter != null)
        {
            lock (logWriter)
            {
                _logWriter = null;
                logWriter.Dispose();
            }
        }
    }

    private void ExitEventHandler(object sender, EventArgs eventArgs)
    {
        StopProcessAtomic();
    }

    private void StopProcessAtomic()
    {
        if (Interlocked.CompareExchange(ref _state, StateFinishing, StateRunning) == StateRunning)
        {
            _cancellationTokenSource.Cancel();
            _processInfo.DurationMilliseconds = (int)_stopwatch.ElapsedMilliseconds;

            _processExitEvent?.Set();
        }
    }

    private void WriteLog(string message)
    {
        TextWriter logWriter = _logWriter;

        if (logWriter != null)
        {
            lock (logWriter)
            {
                if (_logWriter != null)
                {
                    // The logWriter was not destroyed yet
                    _logWriter.WriteLine(message);
                }
            }
        }
    }

    private void StandardOutputEventHandler(object sender, DataReceivedEventArgs eventArgs)
    {
        string data = eventArgs?.Data;
        if (!string.IsNullOrEmpty(data))
        {
            WriteLog(data);
            _outputCapture.AppendLine("  " + data);
        }
    }

    private void StandardErrorEventHandler(object sender, DataReceivedEventArgs eventArgs)
    {
        string data = eventArgs?.Data;
        if (!string.IsNullOrEmpty(data))
        {
            WriteLog(data);
            _outputCapture.AppendLine("!! " + data);
        }
    }

    public bool IsAvailable(ref int progressIndex, ref int failureCount)
    {
        if (_state != StateFinishing)
        {
            return _state == StateIdle;
        }

        string processSpec;
        if (!string.IsNullOrEmpty(_processInfo.Parameters.Arguments))
        {
            processSpec = Path.GetFileName(_processInfo.Parameters.ProcessPath) + " " + _processInfo.Parameters.Arguments;
        }
        else
        {
            processSpec = _processInfo.Parameters.ProcessPath;
        }

        _processInfo.TimedOut = !_process.WaitForExit(0);
        if (_processInfo.TimedOut)
        {
            KillProcess();
        }
        _processInfo.ExitCode = (_processInfo.TimedOut ? TimeoutExitCode : _process.ExitCode);
        _processInfo.Succeeded = (!_processInfo.TimedOut && _processInfo.ExitCode == _processInfo.Parameters.ExpectedExitCode);
        WriteLog(">>>>");

        if (!_processInfo.Succeeded)
        {
            failureCount++;
        }

        string linePrefix = $"{_processIndex} / {_processCount} ({(++progressIndex * 100 / _processCount)}%, {failureCount} failed): ";

        if (_processInfo.Succeeded)
        {
            string successMessage = linePrefix + $"succeeded in {_processInfo.DurationMilliseconds} msecs";

            WriteLog(successMessage);

            Console.WriteLine(successMessage + $": {processSpec}");
            _processInfo.Succeeded = true;
        }
        else
        {
            string failureMessage;
            if (_processInfo.TimedOut)
            {
                failureMessage = linePrefix + $"timed out in {_processInfo.DurationMilliseconds} msecs";
            }
            else
            {
                failureMessage = linePrefix + $"failed in {_processInfo.DurationMilliseconds} msecs, exit code {_processInfo.ExitCode}";
                if (_processInfo.ExitCode < 0)
                {
                    failureMessage += $" = 0x{_processInfo.ExitCode:X8}";
                }
                failureMessage += $", expected {_processInfo.Parameters.ExpectedExitCode}";
            }

            WriteLog(failureMessage);

            Console.Error.WriteLine(failureMessage + $": {processSpec}");
            Console.Error.Write(_outputCapture.ToString());
        }

        CleanupProcess();

        _processInfo.Finished = true;

        lock (_logWriter)
        {
            _logWriter.Flush();
        }

        CleanupLogWriter();

        _state = StateIdle;
        return true;
    }

    /// <summary>
    /// Kills process execution. This may be called from a different thread.
    /// </summary>
    private void KillProcess()
    {
        try
        {
            _process?.Kill();
        }
        catch (Exception)
        {
            // Silently ignore exceptions during this call to Kill as
            // the process may have exited in the meantime.
        }
    }
}
