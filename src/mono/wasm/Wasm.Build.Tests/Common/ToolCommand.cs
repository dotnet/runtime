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
    public class ToolCommand : IDisposable
    {
        private string _label;
        protected ITestOutputHelper _testOutput;

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

        // public virtual CommandResult Execute(params string[] args)
        // {
        //     return Task.Run(async () => await ExecuteAsync(args)).Result;
        // }
        //

        public async virtual Task<CommandResult> ExecuteAsync(params string[] args)
        {
            var resolvedCommand = _command;
            string fullArgs = GetFullArgs(args);
            _testOutput.WriteLine($"[{_label}] Executing - {resolvedCommand} {fullArgs} {WorkingDirectoryInfo()}");
            return await ExecuteAsyncInternal(resolvedCommand, fullArgs);
        }

        public virtual Task<CommandResult> ExecuteWithCapturedOutputAsync(params string[] args)
        {
            var resolvedCommand = _command;
            string fullArgs = GetFullArgs(args);
            _testOutput.WriteLine($"[{_label}] Executing (Captured Output) - {resolvedCommand} {fullArgs} - {WorkingDirectoryInfo()}");
            return ExecuteAsyncInternal(resolvedCommand, fullArgs);
        }

        public virtual void Dispose()
        {
            _testOutput.WriteLine("ToolCommand.Dispose ENTER");
            if (CurrentProcess is not null && !CurrentProcess.HasExited)
            {
                _testOutput.WriteLine("ToolCommand.Dispose calling Kill");
                CurrentProcess.Kill();//entireProcessTree: true);
                _testOutput.WriteLine($"ToolCommand.Dispose back from calling Kill, hasexited: {CurrentProcess.HasExited}, and calling waitforexit");
                CurrentProcess.WaitForExit();
                _testOutput.WriteLine($"ToolCommand.Dispose back from calling waitforexit, hasexited: {CurrentProcess.HasExited}");
                CurrentProcess.Dispose();
                CurrentProcess = null;
            }
        }

        protected virtual string GetFullArgs(params string[] args) => string.Join(" ", args);

        private async Task<CommandResult> ExecuteAsyncInternal(string executable, string args)
        {
            _testOutput.WriteLine($"ToolCommand.ExecuteAsyncInternal ENTER: {executable} {args}");
            var output = new List<string>();
            CurrentProcess = CreateProcess(executable, args);
            CurrentProcess.EnableRaisingEvents = true;
            CurrentProcess.Exited += (_, e) => _testOutput.WriteLine($"Exited raised for {executable} {args}");
            if (!CurrentProcess.Start())
                throw new ArgumentException("No CurrentProcess was started: CurrentProcess.Start() returned false.");

            object syncObj = new();
            int pid = CurrentProcess.Id;
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

                using CancellationTokenSource cts = new();
                cts.CancelAfter((int)Timeout.TotalMilliseconds);

                _testOutput.WriteLine($"[{pid}] calling CurrentProcess.WaitForExitAsync, exited: {CurrentProcess.HasExited}");
                try {
                    await CurrentProcess.WaitForExitAsync(cts.Token);
                } catch (TaskCanceledException) {
                    _testOutput.WriteLine($"[{pid}] [{DateTime.Now}] CurrentProcess is null: {CurrentProcess is null}");
                    _testOutput.WriteLine($"[{pid}] CurrentProcess.WaitForExitAsync timed out, exited: {CurrentProcess?.HasExited}");
                    CurrentProcess?.Refresh();
                    DumpProcess($"timed out", pid);

                    _testOutput.WriteLine($"[{pid}] CurrentProcess.WaitForExitAsync timed out, attemping to kill it, process-is-null: {CurrentProcess is null} hasExited: {CurrentProcess!.HasExited}");
                    CurrentProcess.Kill();//entireProcessTree: true);
                    _testOutput.WriteLine($"[{pid}] back from CurrentProcess.kill, exited: {CurrentProcess.HasExited}");
                    DumpProcess($"After killing, and calling waitforexit", pid);
                    CurrentProcess.WaitForExit();
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
                _testOutput.WriteLine($"[{pid}] calling CurrentProcess.WaitForExit, hasExited: {CurrentProcess!.HasExited}");
                CurrentProcess!.WaitForExit();
                _testOutput.WriteLine($"[{pid}] back from calling CurrentProcess.WaitForExit: {CurrentProcess.HasExited}");

                CurrentProcess.ErrorDataReceived -= logStdErr;
                CurrentProcess.OutputDataReceived -= logStdOut;
                _testOutput.WriteLine($"[{pid}] cancelling err/out");
                CurrentProcess.CancelErrorRead();
                CurrentProcess.CancelOutputRead();

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
                _testOutput.WriteLine($"[{pid}] -- exception -- {ex}");
                throw;
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
