// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.DotNet.InternalAbstractions;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public class Command
    {
        private StringWriter _stdOutCapture;
        private StringWriter _stdErrCapture;

        private Action<string> _stdOutForward;
        private Action<string> _stdErrForward;

        private Action<string> _stdOutHandler;
        private Action<string> _stdErrHandler;

        private bool _running = false;
        private bool _quietBuildReporter = false;

        public Process Process { get; }

        // Priority order of runnable suffixes to look for and run
        private static readonly string[] RunnableSuffixes = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                                         ? new string[] { ".exe", ".cmd", ".bat" }
                                                         : new string[] { string.Empty };

        private Command(string executable, string args)
        {
            // Set the things we need
            var psi = new ProcessStartInfo()
            {
                FileName = executable,
                Arguments = args
            };

            Process = new Process()
            {
                StartInfo = psi
            };
        }

        public static Command Create(string executable, params string[] args)
        {
            return Create(executable, ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(args));
        }

        public static Command Create(string executable, IEnumerable<string> args)
        {
            return Create(executable, ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(args));
        }

        public static Command Create(string executable, string args)
        {
            ResolveExecutablePath(ref executable, ref args);

            return new Command(executable, args);
        }

        private static void ResolveExecutablePath(ref string executable, ref string args)
        {
            foreach (string suffix in RunnableSuffixes)
            {
                var fullExecutable = Path.GetFullPath(Path.Combine(
                                        AppContext.BaseDirectory, executable + suffix));

                if (File.Exists(fullExecutable))
                {
                    executable = fullExecutable;

                    // In priority order we've found the best runnable extension, so break.
                    break;
                }
            }

            // On Windows, we want to avoid using "cmd" if possible (it mangles the colors, and a bunch of other things)
            // So, do a quick path search to see if we can just directly invoke it
            var useCmd = ShouldUseCmd(executable);

            if (useCmd)
            {
                var comSpec = System.Environment.GetEnvironmentVariable("ComSpec");

                // cmd doesn't like "foo.exe ", so we need to ensure that if
                // args is empty, we just run "foo.exe"
                if (!string.IsNullOrEmpty(args))
                {
                    executable = (executable + " " + args).Replace("\"", "\\\"");
                }
                args = $"/C \"{executable}\"";
                executable = comSpec;
            }
        }

        private static bool ShouldUseCmd(string executable)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var extension = Path.GetExtension(executable);
                if (!string.IsNullOrEmpty(extension))
                {
                    return !string.Equals(extension, ".exe", StringComparison.Ordinal);
                }
                else if (executable.Contains(Path.DirectorySeparatorChar))
                {
                    // It's a relative path without an extension
                    if (File.Exists(executable + ".exe"))
                    {
                        // It refers to an exe!
                        return false;
                    }
                }
                else
                {
                    // Search the path to see if we can find it 
                    foreach (var path in System.Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator))
                    {
                        var candidate = Path.Combine(path, executable + ".exe");
                        if (File.Exists(candidate))
                        {
                            // We found an exe!
                            return false;
                        }
                    }
                }

                // It's a non-exe :(
                return true;
            }

            // Non-windows never uses cmd
            return false;
        }

        public Command Environment(IDictionary<string, string> env)
        {
            if (env == null)
            {
                return this;
            }

            foreach (var item in env)
            {
                Process.StartInfo.Environment[item.Key] = item.Value;
            }
            return this;
        }

        public Command Environment(string key, string value)
        {
            Process.StartInfo.Environment[key] = value;
            return this;
        }

        public Command QuietBuildReporter()
        {
            _quietBuildReporter = true;
            return this;
        }

        public CommandResult Execute()
        {
            return Execute(false);
        }

        public Command Start()
        {
            ThrowIfRunning();
            _running = true;

            if (Process.StartInfo.RedirectStandardOutput)
            {
                Process.OutputDataReceived += (sender, args) =>
                {
                    ProcessData(args.Data, _stdOutCapture, _stdOutForward, _stdOutHandler);
                };
            }

            if (Process.StartInfo.RedirectStandardError)
            {
                Process.ErrorDataReceived += (sender, args) =>
                {
                    ProcessData(args.Data, _stdErrCapture, _stdErrForward, _stdErrHandler);
                };
            }

            Process.EnableRaisingEvents = true;

            ReportExecBegin();

            Process.Start();

            if (Process.StartInfo.RedirectStandardOutput)
            {
                Process.BeginOutputReadLine();
            }

            if (Process.StartInfo.RedirectStandardError)
            {
                Process.BeginErrorReadLine();
            }

            return this;
        }

        public CommandResult WaitForExit(bool fExpectedToFail)
        {
            ReportExecWaitOnExit();

            Process.WaitForExit();

            var exitCode = Process.ExitCode;

            ReportExecEnd(exitCode, fExpectedToFail);

            return new CommandResult(
                Process.StartInfo,
                exitCode,
                _stdOutCapture?.GetStringBuilder()?.ToString(),
                _stdErrCapture?.GetStringBuilder()?.ToString());
        }

        public CommandResult Execute(bool fExpectedToFail)
        {
            Start();
            return WaitForExit(fExpectedToFail);
        }

        public Command WorkingDirectory(string projectDirectory)
        {
            Process.StartInfo.WorkingDirectory = projectDirectory;
            return this;
        }

        public Command WithUserProfile(string userprofile)
        {
            string userDir;
            if (RuntimeEnvironment.OperatingSystemPlatform == Platform.Windows)
            {
                userDir = "USERPROFILE";
            }
            else
            {
                userDir = "HOME";
            }

            Process.StartInfo.Environment[userDir] = userprofile;
            return this;
        }

        public Command WithGlobalLocation(string global)
        {
            if (RuntimeEnvironment.OperatingSystemPlatform == Platform.Windows)
            {
                throw new NotImplementedException("Global location override needs test improvements for Windows");
            }
            else
            {
                throw new NotSupportedException("Global location override not supported for Linux");
            }
        }

        public Command EnvironmentVariable(string name, string value)
        {
            if (value != null)
            {
                Process.StartInfo.Environment[name] = value;
            }

            return this;
        }

        public Command CaptureStdOut()
        {
            ThrowIfRunning();
            Process.StartInfo.RedirectStandardOutput = true;
            _stdOutCapture = new StringWriter();
            return this;
        }

        public Command CaptureStdErr()
        {
            ThrowIfRunning();
            Process.StartInfo.RedirectStandardError = true;
            _stdErrCapture = new StringWriter();
            return this;
        }

        public Command ForwardStdOut(TextWriter to = null)
        {
            ThrowIfRunning();
            Process.StartInfo.RedirectStandardOutput = true;
            if (to == null)
            {
                _stdOutForward = Reporter.Output.WriteLine;
            }
            else
            {
                _stdOutForward = to.WriteLine;
            }
            return this;
        }

        public Command ForwardStdErr(TextWriter to = null)
        {
            ThrowIfRunning();
            Process.StartInfo.RedirectStandardError = true;
            if (to == null)
            {
                _stdErrForward = Reporter.Error.WriteLine;
            }
            else
            {
                _stdErrForward = to.WriteLine;
            }
            return this;
        }

        public Command OnOutputLine(Action<string> handler)
        {
            ThrowIfRunning();
            Process.StartInfo.RedirectStandardOutput = true;
            if (_stdOutHandler != null)
            {
                throw new InvalidOperationException("Already handling stdout!");
            }
            _stdOutHandler = handler;
            return this;
        }

        public Command OnErrorLine(Action<string> handler)
        {
            ThrowIfRunning();
            Process.StartInfo.RedirectStandardError = true;
            if (_stdErrHandler != null)
            {
                throw new InvalidOperationException("Already handling stderr!");
            }
            _stdErrHandler = handler;
            return this;
        }

        private string FormatProcessInfo(ProcessStartInfo info, bool includeWorkingDirectory)
        {
            string prefix = includeWorkingDirectory ?
                $"{info.WorkingDirectory}> {info.FileName}" :
                info.FileName;

            if (string.IsNullOrWhiteSpace(info.Arguments))
            {
                return prefix;
            }

            return prefix + " " + info.Arguments;
        }

        private void ReportExecBegin()
        {
            if (!_quietBuildReporter)
            {
                BuildReporter.BeginSection("EXEC", FormatProcessInfo(Process.StartInfo, includeWorkingDirectory: false));
            }
        }

        private void ReportExecWaitOnExit()
        {
            if (!_quietBuildReporter)
            {
                BuildReporter.SectionComment("EXEC", $"Waiting for process {Process.Id} to exit...");
            }
        }

        private void ReportExecEnd(int exitCode, bool fExpectedToFail)
        {
            if (!_quietBuildReporter)
            {
                bool success = exitCode == 0;
                string msgExpectedToFail = "";

                if (fExpectedToFail) {
                    success = !success;
                    msgExpectedToFail = "failed as expected and ";
                }

                var message = $"{FormatProcessInfo(Process.StartInfo, includeWorkingDirectory: !success)} {msgExpectedToFail}exited with {exitCode}";

                BuildReporter.EndSection(
                    "EXEC",
                    success ? message.Green() : message.Red().Bold(),
                    success);
            }
        }

        private void ThrowIfRunning([CallerMemberName] string memberName = null)
        {
            if (_running)
            {
                throw new InvalidOperationException($"Unable to invoke {memberName} after the command has been run");
            }
        }

        private void ProcessData(string data, StringWriter capture, Action<string> forward, Action<string> handler)
        {
            if (data == null)
            {
                return;
            }

            if (capture != null)
            {
                capture.WriteLine(data);
            }

            if (forward != null)
            {
                forward(data);
            }

            if (handler != null)
            {
                handler(data);
            }
        }
    }
}
