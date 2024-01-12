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

        // public virtual CommandResult Execute(params string[] args)
        // {
        //     return Task.Run(async () => await ExecuteAsync(args)).Result;
        // }

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
                CurrentProcess.Kill(entireProcessTree: true);
                _testOutput.WriteLine("ToolCommand.Dispose back from calling Kill");
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
            DataReceivedEventHandler logStdErr = (s, e) =>
            {
                if (e.Data == null)
                    return;

                string msg = $"[{_label}] {e.Data}";
                output.Add(msg);
                _testOutput.WriteLine(msg);
                ErrorDataReceived?.Invoke(s, e);
            };

            DataReceivedEventHandler logStdOut = (s, e) =>
            {
                if (e.Data == null)
                    return;

                string msg = $"[{_label}] {e.Data}";
                output.Add(msg);
                _testOutput.WriteLine(msg);
                OutputDataReceived?.Invoke(s, e);
            };

            if (!CurrentProcess.Start())
                throw new ArgumentException("No CurrentProcess was started: CurrentProcess.Start() returned false.");

            try
            {
                //DataReceivedEventHandler logStdErr = (sender, e) => LogData($"[{label}-stderr]", e.Data);
                //DataReceivedEventHandler logStdOut = (sender, e) => LogData($"[{label}]", e.Data);

                CurrentProcess.ErrorDataReceived += logStdErr;
                CurrentProcess.OutputDataReceived += logStdOut;
                CurrentProcess.BeginOutputReadLine();
                CurrentProcess.BeginErrorReadLine();

                using CancellationTokenSource cts = new();
                int timeoutMs = 5*60*1000;
                cts.CancelAfter(timeoutMs);

                _testOutput.WriteLine($"calling CurrentProcess.WaitForExitAsync with timeout: {timeoutMs}");
                try {
                    await CurrentProcess.WaitForExitAsync(cts.Token);
                } catch (TaskCanceledException tce) {
                    _testOutput.WriteLine($"CurrentProcess.WaitForExitAsync timed out{tce}");
                }
                _testOutput.WriteLine($"back from calling CurrentProcess.WaitForExitAsync.. somehow");

                if (cts.IsCancellationRequested)
                {
                    // CurrentProcess didn't exit
                    _testOutput.WriteLine($"CurrentProcess.WaitForExitAsync timed out, attemping to kill it");
                    CurrentProcess.Kill(entireProcessTree: true);
                    _testOutput.WriteLine($"back from CurrentProcess.kill");
                    //lock (syncObj)
                    //{
                    RemoveNullTerminator(output);
                    var lastLines = output.TakeLast(20);
                    throw new XunitException($"CurrentProcess timed out. Last 20 lines of output:{System.Environment.NewLine}{string.Join(System.Environment.NewLine, lastLines)}");
                    //}
                }

                // this will ensure that all the async event handling has completed
                // and should be called after CurrentProcess.WaitForExit(int)
                // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.CurrentProcess.waitforexit?view=net-5.0#System_Diagnostics_CurrentProcess_WaitForExit_System_Int32_
                _testOutput.WriteLine($"calling CurrentProcess.WaitForExit");
                CurrentProcess.WaitForExit();
                _testOutput.WriteLine($"back from calling CurrentProcess.WaitForExit");

                CurrentProcess.ErrorDataReceived -= logStdErr;
                CurrentProcess.OutputDataReceived -= logStdOut;
                _testOutput.WriteLine($"cancelling err/out");
                CurrentProcess.CancelErrorRead();
                CurrentProcess.CancelOutputRead();

                //lock (syncObj)
                //{
                    var exitCode = CurrentProcess.ExitCode;
                    RemoveNullTerminator(output);
                    //return (CurrentProcess.ExitCode, outputBuilder.ToString().Trim('\r', '\n'));
                    // return (CurrentProcess.ExitCode, string.Join(System.Environment.NewLine, output));
                //}
                // RemoveNullTerminator(output);

                return new CommandResult(
                    CurrentProcess.StartInfo,
                    CurrentProcess.ExitCode,
                    string.Join(System.Environment.NewLine, output));
            }
            catch (Exception ex) when (ex is not XunitException)
            {
                _testOutput.WriteLine($"-- exception -- {ex}");
                throw;
            }


            //_testOutput.WriteLine($"Calling StartAndWaitForExitAsync for {executable} {args}");
            //var completionTask = CurrentProcess.StartAndWaitForExitAsync(_testOutput);
            //CurrentProcess.BeginOutputReadLine();
            //CurrentProcess.BeginErrorReadLine();
            //_testOutput.WriteLine($"Waiting on the task returned from .. StartAndWaitForExitAsync for 5mins, on process: {CurrentProcess.HasExited}, {executable} {args}, task: {completionTask.Status}");

            //await Task.WhenAny(completionTask, Task.Delay(TimeSpan.FromMinutes(5))).ConfigureAwait(false);

            ////if (!completionTask.Wait(TimeSpan.FromMinutes(5)))
            //if (!completionTask.IsCompleted)
            //{
                //_testOutput.WriteLine($"** process task timed out, hasexited: {CurrentProcess.HasExited}, task status: {completionTask.Status}, trying to kill");
                //CurrentProcess.Kill
            //}
            //_testOutput.WriteLine("back from waiting on the task returned from .. StartAndWaitForExitAsync");

        }

        private Process CreateProcess(string executable, string args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = args,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
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
