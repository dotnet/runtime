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
        private bool isDisposed = false;
        private string _label;
        protected ITestOutputHelper _testOutput;

        protected string _command;

        public Process? CurrentProcess { get; private set; }

        public Dictionary<string, string> Environment { get; } = new Dictionary<string, string>();

        // Per-line callbacks used by ExecuteAsyncInternal. Wired via WithOutputDataReceived /
        // WithErrorDataReceived. Replaces the older `event DataReceivedEventHandler` pair that
        // were tied to the pre-net11 Process.BeginOutputReadLine pattern.
        private Action<string?>? _onOutputLine;
        private Action<string?>? _onErrorLine;

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
            _onOutputLine += handler;
            return this;
        }

        public ToolCommand WithErrorDataReceived(Action<string?> handler)
        {
            _onErrorLine += handler;
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
            if (isDisposed)
                return;
            if (CurrentProcess is not null && !CurrentProcess.HasExited)
            {
                CurrentProcess.Kill(entireProcessTree: true);
                CurrentProcess.Dispose();
                CurrentProcess = null;
            }
            isDisposed = true;
        }

        protected virtual string GetFullArgs(params string[] args) => string.Join(" ", args);

        private async Task<CommandResult> ExecuteAsyncInternal(string executable, string args)
        {
            var output = new List<string>();

            // Capture the process in a local. Dispose() can run concurrently — e.g. a test's
            // `using` scope ending while a long-running server process is still executing (see
            // BrowserRunner) — and it kills the process and sets the CurrentProcess field to null.
            // Reading the field after that point would throw NullReferenceException, so all
            // subsequent member access goes through the local instead.
            Process process = CurrentProcess = CreateProcess(executable, args);

            try
            {
                process.Start();

                // Process.ReadAllLinesAsync (added in .NET 11) yields each redirected stdout/stderr
                // line as it arrives and completes once both streams have hit EOF. The streams
                // close when the child process closes its pipe handles — typically (but not always)
                // observable before Exited fires. We still call WaitForExitAsync after the loop so
                // process.ExitCode is safe to read.
                await foreach (ProcessOutputLine line in process.ReadAllLinesAsync().ConfigureAwait(false))
                {
                    if (isDisposed)
                        break;

                    string msg = $"[{_label}] {line.Content}";
                    output.Add(msg);
                    _testOutput.WriteLine(msg);

                    if (line.StandardError)
                        _onErrorLine?.Invoke(line.Content);
                    else
                        _onOutputLine?.Invoke(line.Content);
                }

                if (isDisposed)
                {
                    // The command was disposed while still running, so Dispose() has already killed
                    // the process. There is no meaningful exit code to report and touching the
                    // disposed process would throw, so return the output collected so far.
                    RemoveNullTerminator(output);
                    return new CommandResult(process.StartInfo, exitCode: -1, string.Join(System.Environment.NewLine, output));
                }

                await process.WaitForExitAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // If process start fails, the `Process` object is in a state "don't touch me"
                // (calling almost everything results in "No process associated with this object"),
                // therefore we just set it to null to avoid hiding the root exception.
                CurrentProcess = null;

                _testOutput.WriteLine($"[{_label}] Exception running command: {ex}");
                throw;
            }

            RemoveNullTerminator(output);

            return new CommandResult(
                process.StartInfo,
                process.ExitCode,
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
