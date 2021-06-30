// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

#nullable enable

namespace Wasm.Build.Tests
{
    public class Command
    {
        private StringBuilder _extraArgsBuilder = new();
        private string _label;

        protected string _command;

        public Process? CurrentProcess { get; private set; }

        public Dictionary<string, string> Environment { get; } = new Dictionary<string, string>();

        public event DataReceivedEventHandler? ErrorDataReceived;

        public event DataReceivedEventHandler? OutputDataReceived;

        public string? WorkingDirectory { get; set; }

        public Command(string command, string label="")
        {
            _command = command;
            _label = label;
        }

        public Command WithWorkingDirectory(string dir)
        {
            WorkingDirectory = dir;
            return this;
        }

        public Command WithEnvironmentVariable(string key, string value)
        {
            Environment[key] = value;
            return this;
        }

        public Command WithEnvironmentVariables(IDictionary<string, string>? extraEnvVars)
        {
            if (extraEnvVars != null)
            {
                foreach ((string key, string value) in extraEnvVars)
                    Environment[key] = value;
            }

            return this;
        }

        public virtual CommandResult Execute(string args = "")
        {
            return Task.Run(async () => await ExecuteAsync(args)).Result;
        }

        public async virtual Task<CommandResult> ExecuteAsync(string args = "")
        {
            var resolvedCommand = _command;
            string fullArgs = GetFullArgs(args);
            Console.WriteLine($"[{_label}] Executing - {resolvedCommand} {fullArgs} - {WorkingDirectoryInfo()}");
            return await ExecuteAsyncInternal(resolvedCommand, fullArgs);
        }

        public virtual CommandResult ExecuteWithCapturedOutput(string args = "")
        {
            var resolvedCommand = _command;
            string fullArgs = GetFullArgs(args);
            Console.WriteLine($"[{_label}] Executing (Captured Output) - {resolvedCommand} {fullArgs} - {WorkingDirectoryInfo()}");
            return Task.Run(async () => await ExecuteAsyncInternal(resolvedCommand, fullArgs)).Result;
        }

        protected virtual string GetFullArgs(string args) => $"{args} {_extraArgsBuilder}";

        private async Task<CommandResult> ExecuteAsyncInternal(string executable, string args)
        {
            var output = new List<String>();
            CurrentProcess = CreateProcess(executable, args);
            CurrentProcess.ErrorDataReceived += (s, e) =>
            {
                if (e.Data == null)
                    return;

                output.Add($"[{_label}] {e.Data}");
                var handler = ErrorDataReceived;
                if (handler != null)
                {
                    handler(s, e);
                }
            };

            CurrentProcess.OutputDataReceived += (s, e) =>
            {
                if (e.Data == null)
                    return;

                output.Add($"[{_label}] {e.Data}");
                var handler = OutputDataReceived;
                if (handler != null)
                {
                    handler(s, e);
                }
            };

            var completionTask = CurrentProcess.StartAndWaitForExitAsync();
            CurrentProcess.BeginOutputReadLine();
            CurrentProcess.BeginErrorReadLine();
            await completionTask;

            CurrentProcess.WaitForExit();
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

            AddEnvironmentVariablesTo(psi);
            AddWorkingDirectoryTo(psi);
            var process = new Process
            {
                StartInfo = psi
            };

            process.EnableRaisingEvents = true;
            return process;
        }

        public Command WithExtraArgs(string extraArgs)
        {
            _extraArgsBuilder.Append($" {extraArgs}");
            return this;
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
