// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit.Abstractions;

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

        private async Task<CommandResult> ExecuteAsyncInternal(string executable, string args)
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
