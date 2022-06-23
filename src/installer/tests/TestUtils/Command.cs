// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public class Command
    {
        private StringWriter _stdOutCapture;
        private StringWriter _stdErrCapture;

        private bool _running = false;

        public Process Process { get; }

        // Priority order of runnable suffixes to look for and run
        private static readonly string[] RunnableSuffixes = OperatingSystem.IsWindows()
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
            if (OperatingSystem.IsWindows())
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
                    ProcessData(args.Data, _stdOutCapture);
                };
            }

            if (Process.StartInfo.RedirectStandardError)
            {
                Process.ErrorDataReceived += (sender, args) =>
                {
                    ProcessData(args.Data, _stdErrCapture);
                };
            }

            Process.EnableRaisingEvents = true;

            ReportExecBegin();

            // Retry if we hit ETXTBSY due to Linux race
            // https://github.com/dotnet/runtime/issues/58964
            for (int i = 0; ; i++)
            {
                try
                {
                    Process.Start();
                    break;
                }
                catch (Win32Exception e) when (i < 4 && e.Message.Contains("Text file busy"))
                {
                    Thread.Sleep(i * 20);
                }
            }

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

        /// <summary>
        /// Wait for the command to exit and dispose of the underlying process.
        /// </summary>
        /// <param name="expectedToFail">Whether or not the command is expected to fail (non-zero exit code)</param>
        /// <param name="timeoutMilliseconds">Time in milliseconds to wait for the command to exit</param>
        /// <returns>Result of the command</returns>
        public CommandResult WaitForExit(bool expectedToFail, int timeoutMilliseconds = Timeout.Infinite)
        {
           ReportExecWaitOnExit();

            int exitCode;
            if (!Process.WaitForExit(timeoutMilliseconds))
            {
                exitCode = -1;
            }
            else
            {
                exitCode = Process.ExitCode;
            }

            ReportExecEnd(exitCode, expectedToFail);

            Process.Dispose();

            return new CommandResult(
                Process.StartInfo,
                exitCode,
                _stdOutCapture?.GetStringBuilder()?.ToString(),
                _stdErrCapture?.GetStringBuilder()?.ToString());
        }

        /// <summary>
        /// Execute the command and wait for it to exit.
        /// </summary>
        /// <param name="expectedToFail">Whether or not the command is expected to fail (non-zero exit code)</param>
        /// <returns>Result of the command</returns>
        public CommandResult Execute(bool expectedToFail)
        {
            // Clear out any enabling of dump creation if failure is expected
            if (expectedToFail)
            {
                EnvironmentVariable("COMPlus_DbgEnableMiniDump", null);
                EnvironmentVariable("DOTNET_DbgEnableMiniDump", null);
            }

            Start();
            return WaitForExit(expectedToFail);
        }

        public Command WorkingDirectory(string projectDirectory)
        {
            Process.StartInfo.WorkingDirectory = projectDirectory;
            return this;
        }

        public Command EnvironmentVariable(string name, string value)
        {
            if (value == null)
            {
                value = "";
            }

            Process.StartInfo.Environment[name] = value;

            return this;
        }

        public Command RemoveEnvironmentVariable(string name)
        {
            Process.StartInfo.Environment.Remove(name);
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
            BuildReporter.BeginSection("EXEC", FormatProcessInfo(Process.StartInfo, includeWorkingDirectory: false));
        }

        private void ReportExecWaitOnExit()
        {
            BuildReporter.SectionComment("EXEC", $"Waiting for process {Process.Id} to exit...");
        }

        private void ReportExecEnd(int exitCode, bool fExpectedToFail)
        {
            bool success = exitCode == 0;
            string msgExpectedToFail = "";

            if (fExpectedToFail)
            {
                success = !success;
                msgExpectedToFail = "failed as expected and ";
            }

            var message = $"{FormatProcessInfo(Process.StartInfo, includeWorkingDirectory: !success)} {msgExpectedToFail}exited with {exitCode}";

            BuildReporter.EndSection(
                "EXEC",
                success ? message.Green() : message.Red().Bold(),
                success);
        }

        private void ThrowIfRunning([CallerMemberName] string memberName = null)
        {
            if (_running)
            {
                throw new InvalidOperationException($"Unable to invoke {memberName} after the command has been run");
            }
        }

        private void ProcessData(string data, StringWriter capture)
        {
            if (data == null)
            {
                return;
            }

            if (capture != null)
            {
                capture.WriteLine(data);
            }
        }
    }
}
