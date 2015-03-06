// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            int errno = Interop.libc.kill(_processId, Interop.libc.Signals.SIGKILL);
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

        /// <summary>
        /// Instructs the Process component to wait the specified number of milliseconds for the associated process to exit.
        /// </summary>
        public bool WaitForExitCore(int milliseconds)
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
            get { throw new PlatformNotSupportedException(); }
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

        /// <summary>Gets the amount of time the process has spent running code inside the operating system core.</summary>
        public TimeSpan PrivilegedProcessorTime
        {
            get
            {
                return TicksToTimeSpan(GetStat().stime);
            }
        }

        /// <summary>Gets the time the associated process was started.</summary>
        public DateTime StartTime
        {
            get
            {
                return BootTimeToDateTime(GetStat().starttime);
            }
        }

        /// <summary>
        /// Gets the amount of time the associated process has spent utilizing the CPU.
        /// It is the sum of the <see cref='System.Diagnostics.Process.UserProcessorTime'/> and
        /// <see cref='System.Diagnostics.Process.PrivilegedProcessorTime'/>.
        /// </summary>
        public TimeSpan TotalProcessorTime
        {
            get
            {
                Interop.procfs.ParsedStat stat = GetStat();
                return TicksToTimeSpan(stat.utime + stat.stime);
            }
        }

        /// <summary>
        /// Gets the amount of time the associated process has spent running code
        /// inside the application portion of the process (not the operating system core).
        /// </summary>
        public TimeSpan UserProcessorTime
        {
            get
            {
                return TicksToTimeSpan(GetStat().utime);
            }
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

                int pri = Interop.libc.getpriority(Interop.libc.PriorityWhich.PRIO_PROCESS, _processId);
                if (pri == -1 && Marshal.GetLastWin32Error() != 0)
                {
                    throw new Win32Exception(); // match Windows exception
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

                int result = Interop.libc.setpriority(Interop.libc.PriorityWhich.PRIO_PROCESS, _processId, pri);
                if (result == -1)
                {
                    throw new Win32Exception(); // match Windows exception
                }
            }
        }

        /// <summary>
        /// Gets or sets which processors the threads in this process can be scheduled to run on.
        /// </summary>
        private unsafe IntPtr ProcessorAffinityCore
        {
            get
            {
                EnsureState(State.HaveId);

                Interop.libc.cpu_set_t set = default(Interop.libc.cpu_set_t);
                if (Interop.libc.sched_getaffinity(_processId, (IntPtr)sizeof(Interop.libc.cpu_set_t), &set) != 0)
                {
                    throw new Win32Exception(); // match Windows exception
                }

                ulong bits = 0;
                int maxCpu = IntPtr.Size == 4 ? 32 : 64;
                for (int cpu = 0; cpu < maxCpu; cpu++)
                {
                    if (Interop.libc.CPU_ISSET(cpu, &set))
                        bits |= (1u << cpu);
                }
                return (IntPtr)bits;
            }
            set
            {
                EnsureState(State.HaveId);

                Interop.libc.cpu_set_t set = default(Interop.libc.cpu_set_t);

                long bits = (long)value;
                int maxCpu = IntPtr.Size == 4 ? 32 : 64;
                for (int cpu = 0; cpu < maxCpu; cpu++)
                {
                    if ((bits & (cpu << 1)) != 0)
                        Interop.libc.CPU_SET(cpu, &set);
                }

                if (Interop.libc.sched_setaffinity(_processId, (IntPtr)sizeof(Interop.libc.cpu_set_t), &set) != 0)
                {
                    throw new Win32Exception(); // match Windows exception
                }
            }
        }

        /// <summary>Gets the ID of the current process.</summary>
        private static int GetCurrentProcessId()
        {
            return Interop.libc.getpid();
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

        /// <summary>
        /// Make sure we have obtained the min and max working set limits.
        /// </summary>
        private void GetWorkingSetLimits(out IntPtr minWorkingSet, out IntPtr maxWorkingSet)
        {
            minWorkingSet = IntPtr.Zero; // no defined limit available
            ulong rsslim = GetStat().rsslim;

            // rsslim is a ulong, but maxWorkingSet is an IntPtr, so we need to cap rsslim
            // at the max size of IntPtr.  This often happens when there is no configured 
            // rsslim other than ulong.MaxValue, which without these checks would show up
            // as a maxWorkingSet == -1.
            switch (IntPtr.Size)
            {
                case 4:
                    if (rsslim > int.MaxValue)
                        rsslim = int.MaxValue;
                    break;
                case 8:
                    if (rsslim > long.MaxValue)
                        rsslim = long.MaxValue;
                    break;
            }

            maxWorkingSet = (IntPtr)rsslim;
        }

        /// <summary>Sets one or both of the minimum and maximum working set limits.</summary>
        /// <param name="newMin">The new minimum working set limit, or null not to change it.</param>
        /// <param name="newMax">The new maximum working set limit, or null not to change it.</param>
        /// <param name="resultingMin">The resulting minimum working set limit after any changes applied.</param>
        /// <param name="resultingMax">The resulting maximum working set limit after any changes applied.</param>
        private void SetWorkingSetLimitsCore(IntPtr? newMin, IntPtr? newMax, out IntPtr resultingMin, out IntPtr resultingMax)
        {
            // RLIMIT_RSS with setrlimit not supported on Linux > 2.4.30.
            throw new PlatformNotSupportedException();
        }

        /// <summary>Starts the process using the supplied start info.</summary>
        /// <param name="startInfo">The start info with which to start the process.</param>
        private bool StartCore(ProcessStartInfo startInfo)
        {
            // Resolve the path to the specified file name
            string executablePath = ResolvePath(startInfo.FileName);

            // TODO: Fix stream redirection.
            // If streams are to be redirected, create a pipe for each that is.
            // int[0] is the read end of the pipe, int[1] is the write end.
            int[] stdinPipeFds = startInfo.RedirectStandardInput ? CreatePipe() : null;
            int[] stdoutPipeFds = startInfo.RedirectStandardOutput ? CreatePipe() : null;
            int[] stderrPipeFds = startInfo.RedirectStandardError ? CreatePipe() : null;

            // Fork the process.  In the child process, pid will be 0.
            // In the parent process, pid will be the child's process ID.
            int pid = Interop.libc.fork();
            if (pid == -1) // fork error
            {
                throw new Win32Exception();
            }

            if (pid == 0) // child process
            {
                // If we're redirecting streams, configure stdin, stdout, stderr
                if (stdinPipeFds != null)
                {
                    Redirect(stdinPipeFds[Interop.libc.ReadEndOfPipe], Interop.libc.FileDescriptors.STDIN_FILENO);
                    Interop.libc.close(stdinPipeFds[Interop.libc.WriteEndOfPipe]); // close copy of parent's write end
                }
                if (stdoutPipeFds != null)
                {
                    Redirect(stdoutPipeFds[Interop.libc.WriteEndOfPipe], Interop.libc.FileDescriptors.STDOUT_FILENO);
                    Interop.libc.close(stdoutPipeFds[Interop.libc.ReadEndOfPipe]); // close copy of parent's read end
                }
                if (stderrPipeFds != null)
                {
                    Redirect(stderrPipeFds[Interop.libc.WriteEndOfPipe], Interop.libc.FileDescriptors.STDERR_FILENO);
                    Interop.libc.close(stderrPipeFds[Interop.libc.ReadEndOfPipe]); // close copy of parent's read end
                }

                // Set the current working directory based on the caller's request
                if (!string.IsNullOrWhiteSpace(startInfo.WorkingDirectory))
                    Directory.SetCurrentDirectory(startInfo.WorkingDirectory);

                // Parse the list of arguments and environment variables
                string[] argv = CreateArgv(startInfo);
                string[] envp = CreateEnvp(startInfo);

                // Execute. If this succeeds, it won't return.
                if (Interop.libc.execve(executablePath, argv, envp) != 0)
                {
                    throw new Win32Exception();
                }

                Debug.Fail("execve should never return when successful");
                return false; 
            }

            // We're the parent process.  Store the child's information into this Process object.
            SetProcessHandle(new SafeProcessHandle(pid));
            SetProcessId(pid);

            // Configure the parent's ends of the redirection streams.
            if (stdinPipeFds != null)
            {
                Interop.libc.close(stdinPipeFds[Interop.libc.ReadEndOfPipe]); // close copy of child's stdin
                _standardInput = new StreamWriter(
                    OpenStream(stdinPipeFds[Interop.libc.WriteEndOfPipe], FileAccess.Write),
                    Encoding.UTF8, StreamBufferSize) { AutoFlush = true };
            }
            if (stdoutPipeFds != null)
            {
                Interop.libc.close(stdoutPipeFds[Interop.libc.WriteEndOfPipe]); // close copy of child's stdout
                _standardOutput = new StreamReader(
                    OpenStream(stdoutPipeFds[Interop.libc.ReadEndOfPipe], FileAccess.Read),
                    startInfo.StandardOutputEncoding ?? Encoding.UTF8, true, StreamBufferSize);
            }
            if (stderrPipeFds != null)
            {
                Interop.libc.close(stderrPipeFds[Interop.libc.WriteEndOfPipe]); // close copy of child's stderr
                _standardError = new StreamReader(
                    OpenStream(stderrPipeFds[Interop.libc.ReadEndOfPipe], FileAccess.Read),
                    startInfo.StandardErrorEncoding ?? Encoding.UTF8, true, StreamBufferSize);
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
        private static string[] CreateArgv(ProcessStartInfo psi)
        {
            string argv0 = psi.FileName; // pass filename instead of resolved path as argv[0], to match what caller supplied
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
            throw new Win32Exception(Interop.Errors.ENOENT);
        }

        /// <summary>Gets the path to the current executable, or null if it could not be retrieved.</summary>
        private static string GetExePath()
        {
            // Determine the maximum size of a path
            int maxPath = -1;
            Interop.libc.GetPathConfValue(ref maxPath, Interop.libc.PathConfNames._PC_PATH_MAX, Interop.libc.DEFAULT_PC_PATH_MAX);

            // Start small with a buffer allocation, and grow only up to the max path
            for (int pathLen = 256; pathLen < maxPath; pathLen *= 2)
            {
                // Read from procfs the symbolic link to this process' executable
                byte[] buffer = new byte[pathLen + 1]; // +1 for null termination
                int resultLength = (int)Interop.libc.readlink(Interop.procfs.SelfExeFilePath, buffer, (IntPtr)pathLen);

                // If we got one, null terminate it (readlink doesn't do this) and return the string
                if (resultLength > 0)
                {
                    buffer[resultLength] = (byte)'\0';
                    return Encoding.UTF8.GetString(buffer, 0, resultLength);
                }

                // If the buffer was too small, loop around again and try with a larger buffer.
                // Otherwise, bail.
                if (resultLength == 0 || Marshal.GetLastWin32Error() != Interop.Errors.ENAMETOOLONG)
                {
                    break;
                }
            }

            // Could not get a path
            return null;
        }

        /// <summary>Computes a time based on a number of ticks since boot.</summary>
        /// <param name="ticksAfterBoot">The number of ticks since boot.</param>
        /// <returns>The converted time.</returns>
        internal static DateTime BootTimeToDateTime(ulong ticksAfterBoot)
        {
            // Read procfs to determine the system's uptime, aka how long ago it booted
            string uptimeStr = File.ReadAllText(Interop.procfs.ProcUptimeFilePath, Encoding.UTF8);
            int spacePos = uptimeStr.IndexOf(' ');
            double uptime;
            if (spacePos < 1 || !double.TryParse(uptimeStr.Substring(0, spacePos), out uptime))
            {
                throw new Win32Exception();
            }

            // Use the uptime and the current time to determine the absolute boot time
            DateTime bootTime = DateTime.UtcNow - TimeSpan.FromSeconds(uptime);

            // And use that to determine the absolute time for ticksStartedAfterBoot
            DateTime dt = bootTime + TicksToTimeSpan(ticksAfterBoot);

            // The return value is expected to be in the local time zone.
            // It is converted here (rather than starting with DateTime.Now) to avoid DST issues.
            return dt.ToLocalTime();
        }

        /// <summary>Convert a number of "jiffies", or ticks, to a TimeSpan.</summary>
        /// <param name="ticks">The number of ticks.</param>
        /// <returns>The equivalent TimeSpan.</returns>
        internal static TimeSpan TicksToTimeSpan(double ticks)
        {
            // Look up the number of ticks per second in the system's configuration,
            // then use that to convert to a TimeSpan
            int ticksPerSecond = Interop.libc.sysconf(Interop.libc.SysConfNames._SC_CLK_TCK);
            if (ticksPerSecond <= 0)
            {
                throw new Win32Exception();
            }
            return TimeSpan.FromSeconds(ticks / (double)ticksPerSecond);
        }

        /// <summary>
        /// Duplicates <paramref name="pipeToUse"/> on to <paramref name="fdToOverwrite"/>.
        /// </summary>
        /// <param name="pipeToUse">The file descriptor to use in place of <paramref name="fdToOverwrite"/>.</param>
        /// <param name="fdToOverwrite">The file descriptor to be overwritten.</param>
        private static void Redirect(int pipeToUse, int fdToOverwrite)
        {
            Debug.Assert(pipeToUse >= 0 && fdToOverwrite >= 0);
            Debug.Assert(pipeToUse != fdToOverwrite);

            // Duplicate the file descriptor.  We may need to retry if the I/O is interrupted.
            int result;
            while ((result = Interop.libc.dup2(pipeToUse, fdToOverwrite)) != 0)
            {
                int errno = Marshal.GetLastWin32Error();
                if (errno != Interop.Errors.EBUSY && errno != Interop.Errors.EINTR)
                {
                    throw new Win32Exception(errno);
                }
            }
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

        /// <summary>Reads the stats information for this process from the procfs file system.</summary>
        private Interop.procfs.ParsedStat GetStat()
        {
            EnsureState(State.HaveId);
            return Interop.procfs.ReadStatFile(_processId);
        }

        /// <summary>Creates an anonymous pipe, returning the file descriptors for the two ends of the pipe.</summary>
        /// <returns>An array containing the read end (int[0]) and the write end (int[1]) of the pipe.</returns>
        private static unsafe int[] CreatePipe()
        {
            var fds = new int[2];
            fixed (int* fdsptr = fds)
            {
                if (Interop.libc.pipe(fdsptr) != 0)
                {
                    throw new Win32Exception();
                }
            }
            Debug.Assert(fds[0] >= 0 && fds[1] >= 0 && fds[0] != fds[1]);
            return fds;
        }

        /// <summary>Parses a command-line argument string into a list of arguments.</summary>
        /// <param name="arguments">The argument string.</param>
        /// <param name="results">The list into which the component arguments should be stored.</param>
        private static void ParseArgumentsIntoList(string arguments, List<string> results)
        {
            var currentArgument = new StringBuilder();
            bool inQuotes = false;

            // Iterate through all of the characters in the argument string
            for (int i = 0; i < arguments.Length; i++)
            {
                char c = arguments[i];

                // If this is an escaped double-quote, just add a '"' to the current
                // argument and then skip past it in the input.
                if (c == '\\' && i < arguments.Length - 1 && arguments[i + 1] == '"')
                {
                    currentArgument.Append('"');
                    i++;
                    continue;
                }

                // If this is a double quote, track whether we're inside of quotes or not.
                // Anything within quotes will be treated as a single argument, even if
                // it contains spaces.
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                // If this is a space and we're not in quotes, we're done with the current
                // argument, and if we've built up any characters in the current argument,
                // it should be added to the results and then reset for the next one.
                if (c == ' ' && !inQuotes)
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

    }
}
