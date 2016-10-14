// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System.Diagnostics
{
    public partial class Process : IDisposable
    {
        private static readonly UTF8Encoding s_utf8NoBom =
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        /// <summary>
        /// Puts a Process component in state to interact with operating system processes that run in a 
        /// special mode by enabling the native property SeDebugPrivilege on the current thread.
        /// </summary>
        public static void EnterDebugMode()
        {
            // Nop.
        }

        /// <summary>
        /// Takes a Process component out of the state that lets it interact with operating system processes 
        /// that run in a special mode.
        /// </summary>
        public static void LeaveDebugMode()
        {
            // Nop.
        }

        /// <summary>Stops the associated process immediately.</summary>
        public void Kill()
        {
            EnsureState(State.HaveId);
            int errno = Interop.Sys.Kill(_processId, Interop.Sys.Signals.SIGKILL);
            if (errno != 0)
            {
                throw new Win32Exception(errno); // same exception as on Windows
            }
        }

        /// <summary>Discards any information about the associated process.</summary>
        private void RefreshCore()
        {
            // Nop.  No additional state to reset.
        }

        /// <summary>Additional logic invoked when the Process is closed.</summary>
        private void CloseCore()
        {
            if (_waitStateHolder != null)
            {
                _waitStateHolder.Dispose();
                _waitStateHolder = null;
            }
        }

        /// <summary>Additional configuration when a process ID is set.</summary>
        partial void ConfigureAfterProcessIdSet()
        {
            // Make sure that we configure the wait state holder for this process object, which we can only do once we have a process ID.
            Debug.Assert(_haveProcessId, $"{nameof(ConfigureAfterProcessIdSet)} should only be called once a process ID is set");
            GetWaitState(); // lazily initializes the wait state
        }

        /// <summary>
        /// Instructs the Process component to wait the specified number of milliseconds for the associated process to exit.
        /// </summary>
        private bool WaitForExitCore(int milliseconds)
        {
            bool exited = GetWaitState().WaitForExit(milliseconds);
            Debug.Assert(exited || milliseconds != Timeout.Infinite);

            if (exited && milliseconds == Timeout.Infinite) // if we have a hard timeout, we cannot wait for the streams
            {
                if (_output != null)
                {
                    _output.WaitUtilEOF();
                }
                if (_error != null)
                {
                    _error.WaitUtilEOF();
                }
            }

            return exited;
        }

        /// <summary>Gets the main module for the associated process.</summary>
        public ProcessModule MainModule
        {
            get
            {
                ProcessModuleCollection pmc = Modules;
                return pmc.Count > 0 ? pmc[0] : null;
            }
        }

        /// <summary>Checks whether the process has exited and updates state accordingly.</summary>
        private void UpdateHasExited()
        {
            int? exitCode;
            _exited = GetWaitState().GetExited(out exitCode);
            if (_exited && exitCode != null)
            {
                _exitCode = exitCode.Value;
            }
        }

        /// <summary>Gets the time that the associated process exited.</summary>
        private DateTime ExitTimeCore
        {
            get { return GetWaitState().ExitTime; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the associated process priority
        /// should be temporarily boosted by the operating system when the main window
        /// has focus.
        /// </summary>
        private bool PriorityBoostEnabledCore
        {
            get { return false; } //Nop
            set { } // Nop
        }

        /// <summary>
        /// Gets or sets the overall priority category for the associated process.
        /// </summary>
        private ProcessPriorityClass PriorityClassCore
        {
            // This mapping is relatively arbitrary.  0 is normal based on the man page,
            // and the other values above and below are simply distributed evenly.
            get
            {
                EnsureState(State.HaveId);

                int pri = 0;
                int errno = Interop.Sys.GetPriority(Interop.Sys.PriorityWhich.PRIO_PROCESS, _processId, out pri);
                if (errno != 0)
                {
                    throw new Win32Exception(errno); // match Windows exception
                }

                Debug.Assert(pri >= -20 && pri <= 20);
                return 
                    pri < -15 ? ProcessPriorityClass.RealTime :
                    pri < -10 ? ProcessPriorityClass.High :
                    pri < -5 ? ProcessPriorityClass.AboveNormal :
                    pri == 0 ? ProcessPriorityClass.Normal :
                    pri <= 10 ? ProcessPriorityClass.BelowNormal :
                    ProcessPriorityClass.Idle;
            }
            set
            {
                int pri;
                switch (value)
                {
                    case ProcessPriorityClass.RealTime: pri = -19; break;
                    case ProcessPriorityClass.High: pri = -11; break;
                    case ProcessPriorityClass.AboveNormal: pri = -6; break;
                    case ProcessPriorityClass.Normal: pri = 0; break;
                    case ProcessPriorityClass.BelowNormal: pri = 10; break;
                    case ProcessPriorityClass.Idle: pri = 19; break;
                    default: throw new Win32Exception(); // match Windows exception
                }

                int result = Interop.Sys.SetPriority(Interop.Sys.PriorityWhich.PRIO_PROCESS, _processId, pri);
                if (result == -1)
                {
                    throw new Win32Exception(); // match Windows exception
                }
            }
        }

        /// <summary>Gets the ID of the current process.</summary>
        private static int GetCurrentProcessId()
        {
            return Interop.Sys.GetPid();
        }

        /// <summary>
        /// Gets a short-term handle to the process, with the given access.  If a handle exists,
        /// then it is reused.  If the process has exited, it throws an exception.
        /// </summary>
        private SafeProcessHandle GetProcessHandle()
        {
            if (_haveProcessHandle)
            {
                if (GetWaitState().HasExited)
                {
                    throw new InvalidOperationException(SR.Format(SR.ProcessHasExited, _processId.ToString(CultureInfo.CurrentCulture)));
                }
                return _processHandle;
            }

            EnsureState(State.HaveId | State.IsLocal);
            return new SafeProcessHandle(_processId);
        }

        /// <summary>Starts the process using the supplied start info.</summary>
        /// <param name="startInfo">The start info with which to start the process.</param>
        private bool StartCore(ProcessStartInfo startInfo)
        {
            string filename;
            string[] argv;

            if (startInfo.UseShellExecute)
            {
                if (startInfo.RedirectStandardInput || startInfo.RedirectStandardOutput || startInfo.RedirectStandardError)
                {
                    throw new InvalidOperationException(SR.CantRedirectStreams);
                }

                const string ShellPath = "/bin/sh";

                filename = ShellPath;
                argv = new string[3] { ShellPath, "-c", startInfo.FileName + " " + startInfo.Arguments};
            }
            else
            {
                filename = ResolvePath(startInfo.FileName);
                argv = ParseArgv(startInfo);
            }

            string[] envp = CreateEnvp(startInfo);
            string cwd = !string.IsNullOrWhiteSpace(startInfo.WorkingDirectory) ? startInfo.WorkingDirectory : null;

            // Invoke the shim fork/execve routine.  It will create pipes for all requested
            // redirects, fork a child process, map the pipe ends onto the appropriate stdin/stdout/stderr
            // descriptors, and execve to execute the requested process.  The shim implementation
            // is used to fork/execve as executing managed code in a forked process is not safe (only
            // the calling thread will transfer, thread IDs aren't stable across the fork, etc.)
            int childPid, stdinFd, stdoutFd, stderrFd;
            Interop.Sys.ForkAndExecProcess(
                filename, argv, envp, cwd,
                startInfo.RedirectStandardInput, startInfo.RedirectStandardOutput, startInfo.RedirectStandardError,
                out childPid,
                out stdinFd, out stdoutFd, out stderrFd);

            // Store the child's information into this Process object.
            Debug.Assert(childPid >= 0);
            SetProcessId(childPid);
            SetProcessHandle(new SafeProcessHandle(childPid));

            // Configure the parent's ends of the redirection streams.
            // We use UTF8 encoding without BOM by-default(instead of Console encoding as on Windows)
            // as there is no good way to get this information from the native layer
            // and we do not want to take dependency on Console contract.
            if (startInfo.RedirectStandardInput)
            {
                Debug.Assert(stdinFd >= 0);
                _standardInput = new StreamWriter(OpenStream(stdinFd, FileAccess.Write),
                    s_utf8NoBom, StreamBufferSize) { AutoFlush = true };
            }
            if (startInfo.RedirectStandardOutput)
            {
                Debug.Assert(stdoutFd >= 0);
                _standardOutput = new StreamReader(OpenStream(stdoutFd, FileAccess.Read),
                    startInfo.StandardOutputEncoding ?? s_utf8NoBom, true, StreamBufferSize);
            }
            if (startInfo.RedirectStandardError)
            {
                Debug.Assert(stderrFd >= 0);
                _standardError = new StreamReader(OpenStream(stderrFd, FileAccess.Read),
                    startInfo.StandardErrorEncoding ?? s_utf8NoBom, true, StreamBufferSize);
            }

            return true;
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        /// <summary>Finalizable holder for the underlying shared wait state object.</summary>
        private ProcessWaitState.Holder _waitStateHolder;

        /// <summary>Size to use for redirect streams and stream readers/writers.</summary>
        private const int StreamBufferSize = 4096;

        /// <summary>Converts the filename and arguments information from a ProcessStartInfo into an argv array.</summary>
        /// <param name="psi">The ProcessStartInfo.</param>
        /// <returns>The argv array.</returns>
        private static string[] ParseArgv(ProcessStartInfo psi)
        {
            string argv0 = psi.FileName; // pass filename (instead of resolved path) as argv[0], to match what caller supplied
            if (string.IsNullOrEmpty(psi.Arguments))
            {
                return new string[] { argv0 };
            }
            else
            {
                var argvList = new List<string>();
                argvList.Add(argv0);
                ParseArgumentsIntoList(psi.Arguments, argvList);
                return argvList.ToArray();
            }
        }

        /// <summary>Converts the environment variables information from a ProcessStartInfo into an envp array.</summary>
        /// <param name="psi">The ProcessStartInfo.</param>
        /// <returns>The envp array.</returns>
        private static string[] CreateEnvp(ProcessStartInfo psi)
        {
            var envp = new string[psi.Environment.Count];
            int index = 0;
            foreach (var pair in psi.Environment)
            {
                envp[index++] = pair.Key + "=" + pair.Value;
            }
            return envp;
        }

        /// <summary>Resolves a path to the filename passed to ProcessStartInfo.</summary>
        /// <param name="filename">The filename.</param>
        /// <returns>The resolved path.</returns>
        private static string ResolvePath(string filename)
        {
            // Follow the same resolution that Windows uses with CreateProcess:
            // 1. First try the exact path provided
            // 2. Then try the file relative to the executable directory
            // 3. Then try the file relative to the current directory
            // 4. then try the file in each of the directories specified in PATH
            // Windows does additional Windows-specific steps between 3 and 4,
            // and we ignore those here.

            // If the filename is a complete path, use it, regardless of whether it exists.
            if (Path.IsPathRooted(filename))
            {
                // In this case, it doesn't matter whether the file exists or not;
                // it's what the caller asked for, so it's what they'll get
                return filename;
            }

            // Then check the executable's directory
            string path = GetExePath();
            if (path != null)
            {
                try
                {
                    path = Path.Combine(Path.GetDirectoryName(path), filename);
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
                catch (ArgumentException) { } // ignore any errors in data that may come from the exe path
            }

            // Then check the current directory
            path = Path.Combine(Directory.GetCurrentDirectory(), filename);
            if (File.Exists(path))
            {
                return path;
            }

            // Then check each directory listed in the PATH environment variables
            string pathEnvVar = Environment.GetEnvironmentVariable("PATH");
            if (pathEnvVar != null)
            {
                var pathParser = new StringParser(pathEnvVar, ':', skipEmpty: true);
                while (pathParser.MoveNext())
                {
                    string subPath = pathParser.ExtractCurrent();
                    path = Path.Combine(subPath, filename);
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
            }

            // Could not find the file
            throw new Win32Exception(Interop.Error.ENOENT.Info().RawErrno);
        }

        /// <summary>Convert a number of "jiffies", or ticks, to a TimeSpan.</summary>
        /// <param name="ticks">The number of ticks.</param>
        /// <returns>The equivalent TimeSpan.</returns>
        internal static TimeSpan TicksToTimeSpan(double ticks)
        {
            // Look up the number of ticks per second in the system's configuration,
            // then use that to convert to a TimeSpan
            long ticksPerSecond = Interop.Sys.SysConf(Interop.Sys.SysConfName._SC_CLK_TCK);
            if (ticksPerSecond <= 0)
            {
                throw new Win32Exception();
            }
            return TimeSpan.FromSeconds(ticks / (double)ticksPerSecond);
        }

        /// <summary>Computes a time based on a number of ticks since boot.</summary>
        /// <param name="timespanAfterBoot">The timespan since boot.</param>
        /// <returns>The converted time.</returns>
        internal static DateTime BootTimeToDateTime(TimeSpan timespanAfterBoot)
        {
            // Use the uptime and the current time to determine the absolute boot time. This implementation is relying on the 
            // implementation detail that Stopwatch.GetTimestamp() uses a value based on time since boot.
            DateTime bootTime = DateTime.UtcNow - TimeSpan.FromSeconds(Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);

            // And use that to determine the absolute time for timespan.
            DateTime dt = bootTime + timespanAfterBoot;

            // The return value is expected to be in the local time zone.
            // It is converted here (rather than starting with DateTime.Now) to avoid DST issues.
            return dt.ToLocalTime();
        }

        /// <summary>Opens a stream around the specified file descriptor and with the specified access.</summary>
        /// <param name="fd">The file descriptor.</param>
        /// <param name="access">The access mode.</param>
        /// <returns>The opened stream.</returns>
        private static FileStream OpenStream(int fd, FileAccess access)
        {
            Debug.Assert(fd >= 0);
            return new FileStream(
                new SafeFileHandle((IntPtr)fd, ownsHandle: true), 
                access, StreamBufferSize, isAsync: false);
        }

        /// <summary>Parses a command-line argument string into a list of arguments.</summary>
        /// <param name="arguments">The argument string.</param>
        /// <param name="results">The list into which the component arguments should be stored.</param>
        /// <remarks>
        /// This follows the rules outlined in "Parsing C++ Command-Line Arguments" at 
        /// https://msdn.microsoft.com/en-us/library/17w5ykft.aspx.
        /// </remarks>
        private static void ParseArgumentsIntoList(string arguments, List<string> results)
        {
            var currentArgument = new StringBuilder();
            bool inQuotes = false;

            // Iterate through all of the characters in the argument string.
            for (int i = 0; i < arguments.Length; i++)
            {
                // From the current position, iterate through contiguous backslashes.
                int backslashCount = 0;
                for (; i < arguments.Length && arguments[i] == '\\'; i++, backslashCount++) ;
                if (backslashCount > 0)
                {
                    if (i >= arguments.Length || arguments[i] != '"')
                    {
                        // Backslashes not followed by a double quote:
                        // they should all be treated as literal backslashes.
                        currentArgument.Append('\\', backslashCount);
                        i--;
                    }
                    else
                    {
                        // Backslashes followed by a double quote:
                        // - Output a literal slash for each complete pair of slashes
                        // - If one remains, use it to make the subsequent quote a literal.
                        currentArgument.Append('\\', backslashCount / 2);
                        if (backslashCount % 2 == 0)
                        {
                            i--;
                        }
                        else
                        {
                            currentArgument.Append('"');
                        }
                    }
                    continue;
                }

                char c = arguments[i];

                // If this is a double quote, track whether we're inside of quotes or not.
                // Anything within quotes will be treated as a single argument, even if
                // it contains spaces.
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                // If this is a space/tab and we're not in quotes, we're done with the current
                // argument, and if we've built up any characters in the current argument,
                // it should be added to the results and then reset for the next one.
                if ((c == ' ' || c == '\t') && !inQuotes)
                {
                    if (currentArgument.Length > 0)
                    {
                        results.Add(currentArgument.ToString());
                        currentArgument.Clear();
                    }
                    continue;
                }

                // Nothing special; add the character to the current argument.
                currentArgument.Append(c);
            }

            // If we reach the end of the string and we still have anything in our current
            // argument buffer, treat it as an argument to be added to the results.
            if (currentArgument.Length > 0)
            {
                results.Add(currentArgument.ToString());
            }
        }

        /// <summary>Gets the wait state for this Process object.</summary>
        private ProcessWaitState GetWaitState()
        {
            if (_waitStateHolder == null)
            {
                EnsureState(State.HaveId);
                _waitStateHolder = new ProcessWaitState.Holder(_processId);
            }
            return _waitStateHolder._state;
        }

        private bool IsRespondingCore()
        {
            return true;
        }
        private string GetMainWindowTitle()
        {
            return string.Empty;
        }
        private bool CloseMainWindowCore()
        {
            return false;
        }
        private bool WaitForInputIdleCore(int milliseconds)
        {
            throw new InvalidOperationException(SR.InputIdleUnkownError);
        }
    }
}
