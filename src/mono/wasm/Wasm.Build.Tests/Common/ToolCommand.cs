// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

        public ToolCommand WithTimeout(TimeSpan timeout)
        {
            Timeout = timeout;
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

        public virtual CommandResult Execute(params string[] args)
        {
            return Task.Run(async () => await ExecuteAsync(args)).Result;
        }

        public async virtual Task<CommandResult> ExecuteAsync(params string[] args)
        {
            var resolvedCommand = _command;
            string fullArgs = GetFullArgs(args);
            _testOutput.WriteLine($"[{_label}] Executing - {resolvedCommand} {fullArgs} {WorkingDirectoryInfo()}");
            return await ExecuteAsyncInternal(resolvedCommand, fullArgs);
        }

        public virtual CommandResult ExecuteWithCapturedOutput(params string[] args)
        {
            var resolvedCommand = _command;
            string fullArgs = GetFullArgs(args);
            _testOutput.WriteLine($"[{_label}] Executing (Captured Output) - {resolvedCommand} {fullArgs} - {WorkingDirectoryInfo()}");
            return Task.Run(async () => await ExecuteAsyncInternal(resolvedCommand, fullArgs)).Result;
        }

        public virtual void Dispose()
        {
            if (CurrentProcess is not null && !CurrentProcess.HasExited)
            {
                CurrentProcess.Kill(entireProcessTree: true);
                CurrentProcess.Dispose();
                CurrentProcess = null;
            }
        }

        protected virtual string GetFullArgs(params string[] args) => string.Join(" ", args);

        private async Task<CommandResult> ExecuteAsyncInternal(string path, string args)
        {
            _testOutput.WriteLine($"Running {path} {args}");
            _testOutput.WriteLine($"WorkingDirectory: {WorkingDirectory}");
            StringBuilder outputBuilder = new();
            object syncObj = new();

            var psi = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments = args,
            };

            if (WorkingDirectory == null || !Directory.Exists(WorkingDirectory))
                throw new Exception($"Working directory {WorkingDirectory} not found");

            if (!string.IsNullOrEmpty(WorkingDirectory))
                psi.WorkingDirectory = WorkingDirectory;

            psi.Environment["DOTNET_MULTILEVEL_LOOKUP"] = "0";
            psi.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";

            // runtime repo sets this, which interferes with the tests
            psi.RemoveEnvironmentVariables("MSBuildSDKsPath");
            AddEnvironmentVariablesTo(psi);
            AddWorkingDirectoryTo(psi);

            CurrentProcess = new();
            CurrentProcess.StartInfo = psi;
            CurrentProcess.EnableRaisingEvents = true;

            // AutoResetEvent resetEvent = new (false);
            // process.Exited += (_, _) => { _testOutput.WriteLine ($"- exited called"); resetEvent.Set(); };

            if (!CurrentProcess.Start())
                throw new ArgumentException("No process was started: process.Start() return false.");

            try
            {
                DataReceivedEventHandler logStdErr = (sender, e) => LogData($"[{_label}-stderr]", e, ErrorDataReceived);
                DataReceivedEventHandler logStdOut = (sender, e) => LogData($"[{_label}]", e, OutputDataReceived);

                CurrentProcess.ErrorDataReceived += logStdErr;
                CurrentProcess.OutputDataReceived += logStdOut;
                CurrentProcess.BeginOutputReadLine();
                CurrentProcess.BeginErrorReadLine();

                using CancellationTokenSource cts = new();
                cts.CancelAfter((int)Timeout.TotalMilliseconds);

                await CurrentProcess.WaitForExitAsync(cts.Token);

                if (cts.IsCancellationRequested)
                {
                    // process didn't exit
                    CurrentProcess.Kill(entireProcessTree: true);
                    lock (syncObj)
                    {
                        var lastLines = outputBuilder.ToString().Split('\r', '\n').TakeLast(20);
                        throw new XunitException($"Process timed out. Last 20 lines of output:{System.Environment.NewLine}{string.Join(System.Environment.NewLine, lastLines)}");
                    }
                }

                // this will ensure that all the async event handling has completed
                // and should be called after process.WaitForExit(int)
                // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.waitforexit?view=net-5.0#System_Diagnostics_Process_WaitForExit_System_Int32_
                CurrentProcess.WaitForExit();

                CurrentProcess.ErrorDataReceived -= logStdErr;
                CurrentProcess.OutputDataReceived -= logStdOut;
                CurrentProcess.CancelErrorRead();
                CurrentProcess.CancelOutputRead();

                lock (syncObj)
                {
                    var exitCode = CurrentProcess.ExitCode;
                    return new CommandResult(
                        this.CurrentProcess.StartInfo,
                        this.CurrentProcess.ExitCode,
                        outputBuilder.ToString().Trim('\r', '\n'));
                }
            }
            catch (Exception ex)
            {
                _testOutput.WriteLine($"-- exception -- {ex}");
                throw;
            }

            void LogData(string label, DataReceivedEventArgs args, DataReceivedEventHandler? handler)
            {
                lock (syncObj)
                {
                    if (args.Data != null)
                    {
                        _testOutput.WriteLine($"[{label}] {args.Data}");
                    }
                    outputBuilder.AppendLine($"[{label}] {args.Data}");
                }
                handler?.Invoke(this, args);
            }
        }

#if false
        {
            var output = new List<string>();
            CurrentProcess = CreateProcess(executable, args);
            CurrentProcess.ErrorDataReceived += (s, e) =>
            {
                if (e.Data == null)
                    return;

                string msg = $"[{_label}] {e.Data}";
                output.Add(msg);
                _testOutput.WriteLine(msg);
                ErrorDataReceived?.Invoke(s, e);
            };

            CurrentProcess.OutputDataReceived += (s, e) =>
            {
                if (e.Data == null)
                    return;

                string msg = $"[{_label}] {e.Data}";
                output.Add(msg);
                _testOutput.WriteLine(msg);
                OutputDataReceived?.Invoke(s, e);
            };

            var completionTask = CurrentProcess.StartAndWaitForExitAsync();
            CurrentProcess.BeginOutputReadLine();
            CurrentProcess.BeginErrorReadLine();
            await completionTask;

            RemoveNullTerminator(output);

            return new CommandResult(
                CurrentProcess.StartInfo,
                CurrentProcess.ExitCode,
                string.Join(System.Environment.NewLine, output));
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
#endif

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
    /*

        public static async Task<(int exitCode, string buildOutput)> RunProcessAsync(string path,
                                         ITestOutputHelper _testOutput,
                                         string args = "",
                                         IDictionary<string, string>? envVars = null,
                                         string? workingDir = null,
                                         string? label = null,
                                         int? timeoutMs = null)
        {
            _testOutput.WriteLine($"Running {path} {args}");
            _testOutput.WriteLine($"WorkingDirectory: {workingDir}");
            StringBuilder outputBuilder = new();
            object syncObj = new();

            var processStartInfo = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments = args,
            };

            if (workingDir == null || !Directory.Exists(workingDir))
                throw new Exception($"Working directory {workingDir} not found");

            if (workingDir != null)
                processStartInfo.WorkingDirectory = workingDir;

            if (envVars != null)
            {
                if (envVars.Count > 0)
                    _testOutput.WriteLine("Setting environment variables for execution:");

                foreach (KeyValuePair<string, string> envVar in envVars)
                {
                    processStartInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
                    _testOutput.WriteLine($"\t{envVar.Key} = {envVar.Value}");
                }

                // runtime repo sets this, which interferes with the tests
                processStartInfo.RemoveEnvironmentVariables("MSBuildSDKsPath");
            }

            Process process = new();
            process.StartInfo = processStartInfo;
            process.EnableRaisingEvents = true;

            // AutoResetEvent resetEvent = new (false);
            // process.Exited += (_, _) => { _testOutput.WriteLine ($"- exited called"); resetEvent.Set(); };

            if (!process.Start())
                throw new ArgumentException("No process was started: process.Start() return false.");

            try
            {
                DataReceivedEventHandler logStdErr = (sender, e) => LogData($"[{label}-stderr]", e.Data);
                DataReceivedEventHandler logStdOut = (sender, e) => LogData($"[{label}]", e.Data);

                process.ErrorDataReceived += logStdErr;
                process.OutputDataReceived += logStdOut;
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using CancellationTokenSource cts = new();
                cts.CancelAfter(timeoutMs ?? s_defaultPerTestTimeoutMs);

                await process.WaitForExitAsync(cts.Token);

                if (cts.IsCancellationRequested)
                {
                    // process didn't exit
                    process.Kill(entireProcessTree: true);
                    lock (syncObj)
                    {
                        var lastLines = outputBuilder.ToString().Split('\r', '\n').TakeLast(20);
                        throw new XunitException($"Process timed out. Last 20 lines of output:{Environment.NewLine}{string.Join(Environment.NewLine, lastLines)}");
                    }
                }

                // this will ensure that all the async event handling has completed
                // and should be called after process.WaitForExit(int)
                // https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.waitforexit?view=net-5.0#System_Diagnostics_Process_WaitForExit_System_Int32_
                process.WaitForExit();

                process.ErrorDataReceived -= logStdErr;
                process.OutputDataReceived -= logStdOut;
                process.CancelErrorRead();
                process.CancelOutputRead();

                lock (syncObj)
                {
                    var exitCode = process.ExitCode;
                    return (process.ExitCode, outputBuilder.ToString().Trim('\r', '\n'));
                }
            }
            catch (Exception ex)
            {
                _testOutput.WriteLine($"-- exception -- {ex}");
                throw;
            }

            void LogData(string label, string? message)
            {
                lock (syncObj)
                {
                    if (message != null)
                    {
                        _testOutput.WriteLine($"{label} {message}");
                    }
                    outputBuilder.AppendLine($"{label} {message}");
                }
            }
        }
*/
}
