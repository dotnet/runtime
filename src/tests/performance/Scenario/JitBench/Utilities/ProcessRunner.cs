using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JitBench
{
    /// <summary>
    /// Executes a process and logs the output
    /// </summary>
    /// <remarks>
    /// The intended lifecycle is:
    ///   a) Create a new ProcessRunner
    ///   b) Use the various WithXXX methods to modify the configuration of the process to launch
    ///   c) await RunAsync() to start the process and wait for it to terminate. Configuration
    ///      changes are no longer possible
    ///   d) While waiting for RunAsync(), optionally call Kill() one or more times. This will expedite 
    ///      the termination of the process but there is no guarantee the process is terminated by
    ///      the time Kill() returns.
    ///      
    ///   Although the entire API of this type has been designed to be thread-safe, its typical that
    ///   only calls to Kill() and property getters invoked within the logging callbacks will be called
    ///   asynchronously.
    /// </remarks>
    public class ProcessRunner
    {
        // All of the locals might accessed from multiple threads and need to read/written under
        // the _lock. We also use the lock to synchronize property access on the process object.
        //
        // Be careful not to cause deadlocks by calling the logging callbacks with the lock held.
        // The logger has its own lock and it will hold that lock when it calls into property getters
        // on this type.
        object _lock = new object();

        List<IProcessLogger> _loggers;
        Process _p;
        DateTime _startTime;
        TimeSpan _timeout;
        ITestOutputHelper _traceOutput;
        int? _expectedExitCode;
        TaskCompletionSource<Process> _waitForProcessStartTaskSource;
        Task<int> _waitForExitTask;
        Task _timeoutProcessTask;
        Task _readStdOutTask;
        Task _readStdErrTask;
        CancellationTokenSource _cancelSource;
        private string _replayCommand;
        private KillReason? _killReason;

        public ProcessRunner(string exePath, string arguments, string replayCommand = null)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = exePath;
            psi.Arguments = arguments;
            psi.UseShellExecute = false;
            psi.RedirectStandardInput = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;

            lock (_lock)
            {
                _p = new Process();
                _p.StartInfo = psi;
                _p.EnableRaisingEvents = false;
                _loggers = new List<IProcessLogger>();
                _timeout = TimeSpan.FromMinutes(60);
                _cancelSource = new CancellationTokenSource();
                _killReason = null;
                _waitForProcessStartTaskSource = new TaskCompletionSource<Process>();
                Task<Process> startTask = _waitForProcessStartTaskSource.Task;
                
                // unfortunately we can't use the default Process stream reading because it only returns full lines and we have scenarios
                // that need to receive the output before the newline character is written
                _readStdOutTask = startTask.ContinueWith(t =>
                {
                    ReadStreamToLoggers(_p.StandardOutput, ProcessStream.StandardOut, _cancelSource.Token);
                }, 
                _cancelSource.Token, TaskContinuationOptions.LongRunning, TaskScheduler.Default);

                _readStdErrTask = startTask.ContinueWith(t =>
                {
                    ReadStreamToLoggers(_p.StandardError, ProcessStream.StandardError, _cancelSource.Token);
                }, 
                _cancelSource.Token, TaskContinuationOptions.LongRunning, TaskScheduler.Default);

                _timeoutProcessTask = startTask.ContinueWith(t =>
                {
                    Task.Delay(_timeout, _cancelSource.Token).ContinueWith(t2 => Kill(KillReason.TimedOut), TaskContinuationOptions.NotOnCanceled);
                },
                _cancelSource.Token, TaskContinuationOptions.LongRunning, TaskScheduler.Default);

                _waitForExitTask = InternalWaitForExit(startTask, _readStdOutTask, _readStdErrTask);
                
                if (replayCommand == null)
                {
                    _replayCommand = ExePath + " " + Arguments;
                }
                else
                {
                    _replayCommand = replayCommand;
                }
            }
        }
        
        public string ReplayCommand
        {
            get { lock (_lock) { return _replayCommand; } }
        }

        public ProcessRunner WithEnvironmentVariable(string key, string value)
        {
            lock (_lock)
            {
                _p.StartInfo.Environment[key] = value;
            }
            return this;
        }

        public ProcessRunner WithEnvironment(IDictionary<string,string> environmentVariables)
        {
            lock (_lock)
            {
                if(environmentVariables != null)
                {
                    foreach (KeyValuePair<string, string> kv in environmentVariables)
                    {
                        _p.StartInfo.Environment[kv.Key] = kv.Value;
                    }    
                }
            }
            return this;
        }

        public ProcessRunner WithWorkingDirectory(string workingDirectory)
        {
            lock (_lock)
            {
                _p.StartInfo.WorkingDirectory = workingDirectory;
            }
            return this;
        }

        public ProcessRunner WithLog(IProcessLogger logger)
        {
            lock (_lock)
            {
                _loggers.Add(logger);
            }
            return this;
        }

        public ProcessRunner WithLog(ITestOutputHelper output)
        {
            lock (_lock)
            {
                _loggers.Add(new TestOutputProcessLogger(output));
            }
            return this;
        }

        public ProcessRunner WithDiagnosticTracing(ITestOutputHelper traceOutput)
        {
            lock (_lock)
            {
                _traceOutput = traceOutput;
            }
            return this;
        }

        public IProcessLogger[] Loggers
        {
            get { lock (_lock) { return _loggers.ToArray(); } }
        }

        public ProcessRunner WithTimeout(TimeSpan timeout)
        {
            lock (_lock)
            {
                _timeout = timeout;
            }
            return this;
        }

        public ProcessRunner WithExpectedExitCode(int expectedExitCode)
        {
            lock (_lock)
            {
                _expectedExitCode = expectedExitCode;
            }
            return this;
        }

        public string ExePath
        {
            get { lock (_lock) { return _p.StartInfo.FileName; } }
        }

        public string Arguments
        {
            get { lock (_lock) { return _p.StartInfo.Arguments; } }
        }

        public string WorkingDirectory
        {
            get { lock (_lock) { return _p.StartInfo.WorkingDirectory; } }
        }

        public int ProcessId
        {
            get { lock (_lock) { return _p.Id; } }
        }

        public Dictionary<string,string> EnvironmentVariables
        {
            get { lock (_lock) { return new Dictionary<string, string>(_p.StartInfo.Environment); } }
        }

        public bool IsStarted
        {
            get { lock (_lock) { return _waitForProcessStartTaskSource.Task.IsCompleted; } }
        }

        public DateTime StartTime
        {
            get { lock (_lock) { return _startTime; } }
        }

        public int ExitCode
        {
            get { lock (_lock) { return _p.ExitCode; } }
        }

        public void StandardInputWriteLine(string line)
        {
            IProcessLogger[] loggers = null;
            StreamWriter inputStream = null;
            lock (_lock)
            {
                loggers = _loggers.ToArray();
                inputStream = _p.StandardInput;
            }
            foreach (IProcessLogger logger in loggers)
            {
                logger.WriteLine(this, line, ProcessStream.StandardIn);
            }
            inputStream.WriteLine(line);
        }

        public Task<int> Run()
        {
            Start();
            return WaitForExit();
        }

        public Task<int> WaitForExit()
        {
            lock (_lock)
            {
                return _waitForExitTask;
            }
        }

        public ProcessRunner Start()
        {
            Process p = null;
            lock (_lock)
            {
                p = _p;
            }
            // this is safe to call on multiple threads, it only launches the process once
            bool started = p.Start();

            IProcessLogger[] loggers = null;
            lock (_lock)
            {
                // only the first thread to get here will initialize this state
                if (!_waitForProcessStartTaskSource.Task.IsCompleted)
                {
                    loggers = _loggers.ToArray();
                    _startTime = DateTime.Now;
                    _waitForProcessStartTaskSource.SetResult(_p);
                }
            }

            // only the first thread that entered the lock above will run this
            if (loggers != null)
            {
                foreach (IProcessLogger logger in loggers)
                {
                    logger.ProcessStarted(this);
                }
            }

            return this;
        }

        private void ReadStreamToLoggers(StreamReader reader, ProcessStream stream, CancellationToken cancelToken)
        {
            IProcessLogger[] loggers = Loggers;

            // for the best efficiency we want to read in chunks, but if the underlying stream isn't
            // going to timeout partial reads then we have to fall back to reading one character at a time
            int readChunkSize = 1;
            if (reader.BaseStream.CanTimeout)
            {
                readChunkSize = 1000;
            }

            char[] buffer = new char[readChunkSize];
            bool lastCharWasCarriageReturn = false;
            do
            {
                int charsRead = 0;
                int lastStartLine = 0;
                charsRead = reader.ReadBlock(buffer, 0, readChunkSize);

                // this lock keeps the standard out/error streams from being intermixed
                lock (loggers)
                {
                    for (int i = 0; i < charsRead; i++)
                    {
                        // eat the \n after a \r, if any
                        bool isNewLine = buffer[i] == '\n';
                        bool isCarriageReturn = buffer[i] == '\r';
                        if (lastCharWasCarriageReturn && isNewLine)
                        {
                            lastStartLine++;
                            lastCharWasCarriageReturn = false;
                            continue;
                        }
                        lastCharWasCarriageReturn = isCarriageReturn;
                        if (isCarriageReturn || isNewLine)
                        {
                            string line = new string(buffer, lastStartLine, i - lastStartLine);
                            lastStartLine = i + 1;
                            foreach (IProcessLogger logger in loggers)
                            {
                                logger.WriteLine(this, line, stream);
                            }
                        }
                    }

                    // flush any fractional line
                    if (charsRead > lastStartLine)
                    {
                        string line = new string(buffer, lastStartLine, charsRead - lastStartLine);
                        foreach (IProcessLogger logger in loggers)
                        {
                            logger.Write(this, line, stream);
                        }
                    }
                }
            }
            while (!reader.EndOfStream && !cancelToken.IsCancellationRequested);
        }

        public void Kill(KillReason reason = KillReason.Unknown)
        {
            IProcessLogger[] loggers = null;
            Process p = null;
            lock (_lock)
            {
                if (_waitForExitTask.IsCompleted)
                {
                    return;
                }
                if (_killReason.HasValue)
                {
                    return;
                }
                _killReason = reason;
                if (!_p.HasExited)
                {
                    p = _p;
                }

                loggers = _loggers.ToArray();
                _cancelSource.Cancel();
            }

            if (p != null)
            {
                // its possible the process could exit just after we check so
                // we still have to handle the InvalidOperationException that
                // can be thrown.
                try
                {
                    p.Kill();
                }
                catch (InvalidOperationException) { }
            }

            foreach (IProcessLogger logger in loggers)
            {
                logger.ProcessKilled(this, reason);
            }
        }

        private async Task<int> InternalWaitForExit(Task<Process> startProcessTask, Task stdOutTask, Task stdErrTask)
        {
            DebugTrace("starting InternalWaitForExit");
            Process p = await startProcessTask;
            DebugTrace("InternalWaitForExit {0} '{1}'", p.Id, _replayCommand);

            Task processExit = Task.Factory.StartNew(() =>
            {
                DebugTrace("starting Process.WaitForExit {0}", p.Id);
                p.WaitForExit();
                DebugTrace("ending Process.WaitForExit {0}", p.Id);
            },
            TaskCreationOptions.LongRunning);

            DebugTrace("awaiting process {0} exit, stdOut, and stdErr", p.Id);
            await Task.WhenAll(processExit, stdOutTask, stdErrTask);
            DebugTrace("await process {0} exit, stdOut, and stdErr complete", p.Id);

            foreach (IProcessLogger logger in Loggers)
            {
                logger.ProcessExited(this);
            }

            lock (_lock)
            {
                if (_expectedExitCode.HasValue && p.ExitCode != _expectedExitCode.Value)
                {
                    throw new Exception("Process returned exit code " + p.ExitCode + ", expected " + _expectedExitCode.Value + Environment.NewLine +
                                        "Command Line: " + ReplayCommand + Environment.NewLine +
                                        "Working Directory: " + WorkingDirectory);
                }
                DebugTrace("InternalWaitForExit {0} returning {1}", p.Id, p.ExitCode);
                return p.ExitCode;
            }
        }

        private void DebugTrace(string format, params object[] args)
        {
            lock (_lock)
            {
                if (_traceOutput != null)
                {
                    string message = string.Format("TRACE: " + format, args);
                    _traceOutput.WriteLine(message);
                }
            }
        }
    }
}
