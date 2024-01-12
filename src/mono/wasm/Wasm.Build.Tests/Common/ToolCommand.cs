// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests
{
    public class ToolCommand : IAsyncDisposable
    {
        private string _label;
        protected ITestOutputHelper _testOutput;
        private TaskCompletionSource<bool> _exited = new();
        private CancellationTokenSource _cancelRequested = new();
        private CancellationTokenSource _timeoutCts = new();
        private CancellationTokenSource _linkedCts = new();
        private int pid = -1;

        protected string _command;

        public Process? CurrentProcess { get; private set; }

        public Dictionary<string, string> Environment { get; } = new Dictionary<string, string>();

        public event DataReceivedEventHandler? ErrorDataReceived;

        public event DataReceivedEventHandler? OutputDataReceived;

        public string? WorkingDirectory { get; set; }
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);

        public ToolCommand(string command, ITestOutputHelper testOutput, string label="")
        {
            _command = command;
            _testOutput = testOutput;
            _label = label;
        }

        public ToolCommand WithWorkingDirectory(string dir)
        {
            WorkingDirectory = dir;
            return this;
        }

        public ToolCommand WithEnvironmentVariable(string key, string value)
        {
            Environment[key] = value;
            return this;
        }

        public ToolCommand WithEnvironmentVariables(IDictionary<string, string>? extraEnvVars)
        {
            if (extraEnvVars != null)
            {
                foreach ((string key, string value) in extraEnvVars)
                    Environment[key] = value;
            }

            return this;
        }

        public ToolCommand WithOutputDataReceived(Action<string?> handler)
        {
            OutputDataReceived += (_, args) => handler(args.Data);
            return this;
        }

        public ToolCommand WithErrorDataReceived(Action<string?> handler)
        {
            ErrorDataReceived += (_, args) => handler(args.Data);
            return this;
        }

        public ToolCommand WithTimeout(TimeSpan timeout)
        {
            Timeout = timeout;
            return this;
        }

        public virtual Task<CommandResult> ExecuteAsync(params string[] args)
        {
            var resolvedCommand = _command;
            string fullArgs = GetFullArgs(args);
            _testOutput.WriteLine($"[{_label}] Executing - {resolvedCommand} {fullArgs} {WorkingDirectoryInfo()}");
            return ExecuteAsyncInternal(resolvedCommand, fullArgs);
        }

        public virtual Task<CommandResult> ExecuteWithCapturedOutputAsync(params string[] args)
        {
            var resolvedCommand = _command;
            string fullArgs = GetFullArgs(args);
            _testOutput.WriteLine($"[{_label}] Executing (Captured Output) - {resolvedCommand} {fullArgs} - {WorkingDirectoryInfo()}");
            return ExecuteAsyncInternal(resolvedCommand, fullArgs);
        }

        public virtual async ValueTask DisposeAsync()
        {
            _testOutput.WriteLine($"[{pid}] ToolCommand.Dispose ENTER, cancel'ing cancelRequested");
            _cancelRequested.Cancel();
            if (CurrentProcess is null)
                return;

            try
            {
                if (!CurrentProcess.HasExited)
                {
                    _testOutput.WriteLine($"[{pid}] ToolCommand.Dispose has not exited, so calling Kill, exited: {_exited.Task.Status}");
                    CurrentProcess.Kill(entireProcessTree: true);
                    _testOutput.WriteLine($"[{pid}] ToolCommand.Dispose back from calling Kill, hasexited: {CurrentProcess.HasExited}");
                }
                _testOutput.WriteLine($"[{pid}] ToolCommand.Dispose waiting on _exited: {_exited.Task.Status}");
                await _exited.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                _testOutput.WriteLine($"[{pid}] ToolCommand.Dispose back from waiting on exited event, hasexited: {CurrentProcess.HasExited}");
            }
            catch (Exception ex)
            {
                _testOutput.WriteLine($"ToolCommand.Dispose failed with {ex}, but ignoring this as the caller does not care!");
            }
            CurrentProcess.Dispose();
            CurrentProcess = null;
        }

        protected virtual string GetFullArgs(params string[] args) => string.Join(" ", args);

        private async Task<CommandResult> ExecuteAsyncInternal(string executable, string args)
        {
            _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancelRequested.Token, _timeoutCts.Token);
            _timeoutCts.CancelAfter((int)Timeout.TotalMilliseconds);

            _testOutput.WriteLine($"ToolCommand.ExecuteAsyncInternal ENTER: {executable} {args}");
            var output = new List<string>();
            CurrentProcess = CreateProcess(executable, args);
            CurrentProcess.EnableRaisingEvents = true;
            CurrentProcess.Exited += (_, e) => { _testOutput.WriteLine($"[{pid}] Exited raised for {executable} {args}"); _exited.TrySetResult(true); };
            if (!CurrentProcess.Start())
                throw new ArgumentException("No CurrentProcess was started: CurrentProcess.Start() returned false.");

            object syncObj = new();
            pid = CurrentProcess.Id;
            DataReceivedEventHandler logStdErr = (s, e) =>
            {
                if (e.Data == null)
                    return;

                string msg = $"[{pid}] [{_label}] {e.Data}";
                lock (syncObj)
                    output.Add(msg);
                _testOutput.WriteLine(msg);
                ErrorDataReceived?.Invoke(s, e);
            };

            DataReceivedEventHandler logStdOut = (s, e) =>
            {
                if (e.Data == null)
                    return;

                string msg = $"[{pid}] [{_label}] {e.Data}";
                lock (syncObj)
                    output.Add(msg);
                _testOutput.WriteLine(msg);
                OutputDataReceived?.Invoke(s, e);
            };

            try
            {
                _testOutput.WriteLine($"[{pid}] [{DateTime.Now}] Started CurrentProcess for {executable} {args}");
                CurrentProcess.ErrorDataReceived += logStdErr;
                CurrentProcess.OutputDataReceived += logStdOut;
                CurrentProcess.BeginOutputReadLine();
                CurrentProcess.BeginErrorReadLine();

                _testOutput.WriteLine($"[{pid}] calling CurrentProcess.WaitForExitAsync, exited: {CurrentProcess.HasExited}");
                try {
                    await CurrentProcess.WaitForExitAsync(_linkedCts.Token);
                } catch (TaskCanceledException) {
                    _testOutput.WriteLine($"[{pid}] [{DateTime.Now}] CurrentProcess is null: {CurrentProcess is null}");
                    if (_cancelRequested.IsCancellationRequested)
                    {
                        _testOutput.WriteLine($"[{pid}] CurrentProcess.WaitForExitAsync cancelled, cancelRequested, returning");
                        if (CurrentProcess is not null && !CurrentProcess.HasExited)
                        {
                            _testOutput.WriteLine($"[{pid}] cancelling err/out");
                            CurrentProcess.ErrorDataReceived -= logStdErr;
                            CurrentProcess.OutputDataReceived -= logStdOut;
                            CurrentProcess.CancelErrorRead();
                            CurrentProcess.CancelOutputRead();
                        }
                        throw new Exception($"IGNORE this exception.. it's expected because the toolcommand is being cancelled");
                    }
                    _testOutput.WriteLine($"[{pid}] CurrentProcess.WaitForExitAsync timed out, exited: {CurrentProcess?.HasExited}");
                    CurrentProcess?.Refresh();
                    DumpProcess($"timed out", pid);

                    _testOutput.WriteLine($"[{pid}] CurrentProcess.WaitForExitAsync timed out, attemping to kill it, process-is-null: {CurrentProcess is null} hasExited: {CurrentProcess!.HasExited}");
                    CurrentProcess.Kill(entireProcessTree: true);
                    _testOutput.WriteLine($"[{pid}] back from CurrentProcess.kill, exited: {CurrentProcess.HasExited}");
                    DumpProcess($"After killing, and waiting for exited event", pid);
                    await _exited.Task.WaitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                    _testOutput.WriteLine($"[{pid}] back from CurrentProcess.WaitForExit, exited: {CurrentProcess.HasExited}");
                    lock (syncObj)
                    {
                        RemoveNullTerminator(output);
                        // var lastLines = output.TakeLast(20);
                        throw new XunitException($"[{pid}] CurrentProcess timed out.{System.Environment.NewLine}----------------{System.Environment.NewLine}{string.Join(System.Environment.NewLine, output)}--------------------");
                    }
                }
                _testOutput.WriteLine($"[{pid}] [{DateTime.Now}] back from calling CurrentProcess.WaitForExitAsync{pid}");

                // this will ensure that all the async event handling has completed
                // and should be called after CurrentProcess.WaitForExit(int)
                // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.CurrentProcess.waitforexit?view=net-5.0#System_Diagnostics_CurrentProcess_WaitForExit_System_Int32_
                _testOutput.WriteLine($"[{pid}] going to wait on exited event, current-is-null: {CurrentProcess is null}, hasExited: {CurrentProcess?.HasExited}, _exited: {_exited.Task.IsCompleted}");
                await _exited.Task.ConfigureAwait(false);
                _testOutput.WriteLine($"[{pid}] back from waiting on exited event, null: {CurrentProcess is null}: hasexited: {CurrentProcess?.HasExited}, _exited: {_exited.Task.IsCompleted}");

                CurrentProcess!.ErrorDataReceived -= logStdErr;
                CurrentProcess!.OutputDataReceived -= logStdOut;
                _testOutput.WriteLine($"[{pid}] cancelling err/out");
                CurrentProcess!.CancelErrorRead();
                CurrentProcess!.CancelOutputRead();

                lock (syncObj)
                {
                    var exitCode = CurrentProcess.ExitCode;
                    RemoveNullTerminator(output);
                    //return (CurrentProcess.ExitCode, outputBuilder.ToString().Trim('\r', '\n'));
                    // return (CurrentProcess.ExitCode, string.Join(System.Environment.NewLine, output));
                    return new CommandResult(
                        CurrentProcess.StartInfo,
                        CurrentProcess.ExitCode,
                        string.Join(System.Environment.NewLine, output));
                }

            }
            catch (Exception ex) when (ex is not XunitException)
            {
                _testOutput.WriteLine($"[{pid}] -- exception -- {ex}. CurrentProcess is null: {CurrentProcess is null}, hasExited: {CurrentProcess?.HasExited}, _exited: {_exited.Task.Status}, _cancelRequested: {_cancelRequested.IsCancellationRequested}, _timeoutCts: {_timeoutCts.IsCancellationRequested}, _linkedCts: {_linkedCts.IsCancellationRequested}");
                if (!_cancelRequested.IsCancellationRequested)
                    throw;
                else
                    throw new Exception($"IGNORE this exception.. it's expected because the toolcommand is being cancelled: {ex.Message}", ex);
            }
        }

        private void DumpProcess(string message, int pid)
        {
            try
            {
                Process process = Process.GetProcessById(pid);
                _testOutput.WriteLine($"[{pid}] DumpProcess: {message}: {process.HasExited}");
            } catch (Exception ex)
            {
                _testOutput.WriteLine($"Could not GetProcessById for {pid}: {ex.Message}");
            }
        }

        private Process CreateProcess(string executable, string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = args,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                // RedirectStandardInput = true,
                UseShellExecute = false
            };

            psi.Environment["DOTNET_MULTILEVEL_LOOKUP"] = "0";
            psi.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";

            // runtime repo sets this, which interferes with the tests
            psi.RemoveEnvironmentVariables("MSBuildSDKsPath");
            AddEnvironmentVariablesTo(psi);
            AddWorkingDirectoryTo(psi);
            var process = new Process
            {
                StartInfo = psi
            };

            process.EnableRaisingEvents = true;
            return process;
        }

        private string WorkingDirectoryInfo()
        {
            if (WorkingDirectory == null)
            {
                return "";
            }

            return $" in pwd {WorkingDirectory}";
        }

        private void RemoveNullTerminator(List<string> strings)
        {
            var count = strings.Count;

            if (count < 1)
            {
                return;
            }

            if (strings[count - 1] == null)
            {
                strings.RemoveAt(count - 1);
            }
        }

        private void AddEnvironmentVariablesTo(ProcessStartInfo psi)
        {
            foreach (var item in Environment)
            {
                _testOutput.WriteLine($"\t[{item.Key}] = {item.Value}");
                psi.Environment[item.Key] = item.Value;
            }
        }

        private void AddWorkingDirectoryTo(ProcessStartInfo psi)
        {
            if (!string.IsNullOrWhiteSpace(WorkingDirectory))
            {
                psi.WorkingDirectory = WorkingDirectory;
            }
        }
    }
}
