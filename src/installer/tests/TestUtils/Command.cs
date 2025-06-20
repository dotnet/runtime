// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public class Command
    {
        private StringWriter _stdOutCapture;
        private StringWriter _stdErrCapture;

        private bool _disableDumps = false;
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

        public Command DisableDumps()
        {
            _disableDumps = true;
            RemoveEnvironmentVariable("COMPlus_DbgEnableMiniDump");
            RemoveEnvironmentVariable("DOTNET_DbgEnableMiniDump");
            return this;
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

        public Command Start([CallerMemberName] string caller = "")
        {
            ThrowIfRunning();
            _running = true;

            if (_disableDumps && (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()))
            {
                // Replace double quoted arguments with single quotes.
                // We only want to replace non-escaped quotes - that is, ones not preceded by a backslash
                // or preceded by an even number of backslashes.
                string args = System.Text.RegularExpressions.Regex.Replace(
                    Process.StartInfo.Arguments,
                    @"((?:^|[^\\])(?:\\\\)*)""",
                    m => m.Value.Substring(0, m.Value.Length - 1) + "'"
                );

                // Explicitly set the core file size to 0 before launching the process in the same shell
                Process.StartInfo.Arguments = $"-c \"ulimit -c 0 && exec {Process.StartInfo.FileName} {args}\"";
                Process.StartInfo.FileName = "/bin/sh";
            }

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

            ReportExec(caller);

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
        /// <param name="timeoutMilliseconds">Time in milliseconds to wait for the command to exit</param>
        /// <returns>Result of the command</returns>
        public CommandResult WaitForExit(int timeoutMilliseconds = Timeout.Infinite, [CallerMemberName] string caller = "")
        {
            ReportWaitOnExit(caller);

            int exitCode;
            if (!Process.WaitForExit(timeoutMilliseconds))
            {
                exitCode = -1;
            }
            else
            {
                exitCode = Process.ExitCode;
            }

            ReportExit(exitCode, caller);
            int pid = Process.Id;
            Process.Dispose();

            return new CommandResult(
                Process.StartInfo,
                pid,
                exitCode,
                _stdOutCapture?.GetStringBuilder()?.ToString(),
                _stdErrCapture?.GetStringBuilder()?.ToString());
        }

        /// <summary>
        /// Execute the command and wait for it to exit.
        /// </summary>
        /// <returns>Result of the command</returns>
        public CommandResult Execute([CallerMemberName] string caller = "")
        {
            Start(caller);
            return WaitForExit(caller: caller);
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

        public Command CaptureStdOut(Encoding? stdOutEncoding = null)
        {
            ThrowIfRunning();
            Process.StartInfo.RedirectStandardOutput = true;
            Process.StartInfo.StandardOutputEncoding = stdOutEncoding;
            _stdOutCapture = new StringWriter();
            return this;
        }

        public Command CaptureStdErr(Encoding? stdErrEncoding = null)
        {
            ThrowIfRunning();
            Process.StartInfo.RedirectStandardError = true;
            Process.StartInfo.StandardErrorEncoding = stdErrEncoding;
            _stdErrCapture = new StringWriter();
            return this;
        }

        private string FormatProcessInfo(ProcessStartInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.Arguments))
            {
                return info.FileName;
            }

            return $"{info.FileName} {info.Arguments}";
        }

        private static DateTime _initialTime = DateTime.Now;

        private string GetFormattedTime()
        {
            const string TimeSpanFormat = @"hh\:mm\:ss\.fff";
            return (DateTime.Now - _initialTime).ToString(TimeSpanFormat);
        }

        private void ReportExec(string testName)
        {
            Console.WriteLine(
                $"""
                [EXEC] [{GetFormattedTime()}] [{testName}]
                       {FormatProcessInfo(Process.StartInfo)}
                """);

        }

        private void ReportWaitOnExit(string testName)
        {
            Console.WriteLine(
                $"""
                [WAIT] [{GetFormattedTime()}] [{testName}]
                       PID: {Process.Id} - {FormatProcessInfo(Process.StartInfo)}
                """);

        }

        private void ReportExit(int exitCode, string testName)
        {
            Console.WriteLine(
                $"""
                [EXIT] [{GetFormattedTime()}] [{testName}]
                       PID: {Process.Id} - Exit code: 0x{exitCode:x} - {FormatProcessInfo(Process.StartInfo)}
                """);
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
