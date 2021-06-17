// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Security;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Diagnostics
{
    public partial class Process : IDisposable
    {
        private static volatile bool s_initialized;
        private static readonly object s_initializedGate = new object();
        private static readonly ReaderWriterLockSlim s_processStartLock = new ReaderWriterLockSlim();

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

        [CLSCompliant(false)]
        [SupportedOSPlatform("windows")]
        public static Process Start(string fileName, string userName, SecureString password, string domain)
        {
            throw new PlatformNotSupportedException(SR.ProcessStartWithPasswordAndDomainNotSupported);
        }

        [CLSCompliant(false)]
        [SupportedOSPlatform("windows")]
        public static Process Start(string fileName, string arguments, string userName, SecureString password, string domain)
        {
            throw new PlatformNotSupportedException(SR.ProcessStartWithPasswordAndDomainNotSupported);
        }

        /// <summary>Terminates the associated process immediately.</summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("tvos")]
        public void Kill()
        {
            if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
            {
                throw new PlatformNotSupportedException();
            }

            EnsureState(State.HaveId);

            // Check if we know the process has exited. This avoids us targetting another
            // process that has a recycled PID. This only checks our internal state, the Kill call below
            // activly checks if the process is still alive.
            if (GetHasExited(refresh: false))
            {
                return;
            }

            int killResult = Interop.Sys.Kill(_processId, Interop.Sys.Signals.SIGKILL);
            if (killResult != 0)
            {
                Interop.Error error = Interop.Sys.GetLastError();

                // Don't throw if the process has exited.
                if (error == Interop.Error.ESRCH)
                {
                    return;
                }

                throw new Win32Exception(); // same exception as on Windows
            }
        }

        private bool GetHasExited(bool refresh)
            => GetWaitState().GetExited(out _, refresh);

        private List<Exception>? KillTree()
        {
            List<Exception>? exceptions = null;
            KillTree(ref exceptions);
            return exceptions;
        }

        private void KillTree(ref List<Exception>? exceptions)
        {
            // If the process has exited, we can no longer determine its children.
            // If we know the process has exited, stop already.
            if (GetHasExited(refresh: false))
            {
                return;
            }

            // Stop the process, so it won't start additional children.
            // This is best effort: kill can return before the process is stopped.
            int stopResult = Interop.Sys.Kill(_processId, Interop.Sys.Signals.SIGSTOP);
            if (stopResult != 0)
            {
                Interop.Error error = Interop.Sys.GetLastError();
                // Ignore 'process no longer exists' error.
                if (error != Interop.Error.ESRCH)
                {
                    (exceptions ??= new List<Exception>()).Add(new Win32Exception());
                }
                return;
            }

            IReadOnlyList<Process> children = GetChildProcesses();

            int killResult = Interop.Sys.Kill(_processId, Interop.Sys.Signals.SIGKILL);
            if (killResult != 0)
            {
                Interop.Error error = Interop.Sys.GetLastError();
                // Ignore 'process no longer exists' error.
                if (error != Interop.Error.ESRCH)
                {
                    (exceptions ??= new List<Exception>()).Add(new Win32Exception());
                }
            }

            foreach (Process childProcess in children)
            {
                childProcess.KillTree(ref exceptions);
                childProcess.Dispose();
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
            // Initialize WaitStateHolder for non-child processes
            GetWaitState();
        }

        /// <devdoc>
        ///     Make sure we are watching for a process exit.
        /// </devdoc>
        /// <internalonly/>
        private void EnsureWatchingForExit()
        {
            if (!_watchingForExit)
            {
                lock (this)
                {
                    if (!_watchingForExit)
                    {
                        Debug.Assert(_waitHandle == null);
                        Debug.Assert(_registeredWaitHandle == null);
                        Debug.Assert(Associated, "Process.EnsureWatchingForExit called with no associated process");
                        _watchingForExit = true;
                        try
                        {
                            _waitHandle = new ProcessWaitHandle(GetWaitState());
                            _registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(_waitHandle,
                                new WaitOrTimerCallback(CompletionCallback), _waitHandle, -1, true);
                        }
                        catch
                        {
                            _waitHandle?.Dispose();
                            _waitHandle = null;
                            _watchingForExit = false;
                            throw;
                        }
                    }
                }
            }
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
                _output?.EOF.GetAwaiter().GetResult();
                _error?.EOF.GetAwaiter().GetResult();
            }

            return exited;
        }

        /// <summary>Gets the main module for the associated process.</summary>
        public ProcessModule? MainModule
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
            _exited = GetWaitState().GetExited(out exitCode, refresh: true);
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
                EnsureState(State.HaveNonExitedId);

                int pri = 0;
                int errno = Interop.Sys.GetPriority(Interop.Sys.PriorityWhich.PRIO_PROCESS, _processId, out pri);
                if (errno != 0) // Interop.Sys.GetPriority returns GetLastWin32Error()
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
                EnsureState(State.HaveNonExitedId);

                int pri = 0; // Normal
                switch (value)
                {
                    case ProcessPriorityClass.RealTime: pri = -19; break;
                    case ProcessPriorityClass.High: pri = -11; break;
                    case ProcessPriorityClass.AboveNormal: pri = -6; break;
                    case ProcessPriorityClass.BelowNormal: pri = 10; break;
                    case ProcessPriorityClass.Idle: pri = 19; break;
                    default:
                        Debug.Assert(value == ProcessPriorityClass.Normal, "Input should have been validated by caller");
                        break;
                }

                int result = Interop.Sys.SetPriority(Interop.Sys.PriorityWhich.PRIO_PROCESS, _processId, pri);
                if (result == -1)
                {
                    throw new Win32Exception(); // match Windows exception
                }
            }
        }

        /// <summary>Checks whether the argument is a direct child of this process.</summary>
        private bool IsParentOf(Process possibleChildProcess)
        {
            try
            {
                return Id == possibleChildProcess.ParentProcessId;
            }
            catch (Exception e) when (IsProcessInvalidException(e))
            {
                return false;
            }
        }

        private bool Equals(Process process)
        {
            try
            {
                return Id == process.Id;
            }
            catch (Exception e) when (IsProcessInvalidException(e))
            {
                return false;
            }
        }

        partial void ThrowIfExited(bool refresh)
        {
            // Don't allocate a ProcessWaitState.Holder unless we're refreshing.
            if (_waitStateHolder == null && !refresh)
            {
                return;
            }

            if (GetHasExited(refresh))
            {
                throw new InvalidOperationException(SR.Format(SR.ProcessHasExited, _processId.ToString()));
            }
        }

        /// <summary>
        /// Gets a short-term handle to the process, with the given access.  If a handle exists,
        /// then it is reused.  If the process has exited, it throws an exception.
        /// </summary>
        private SafeProcessHandle GetProcessHandle()
        {
            if (_haveProcessHandle)
            {
                ThrowIfExited(refresh: true);

                return _processHandle!;
            }

            EnsureState(State.HaveNonExitedId | State.IsLocal);
            return new SafeProcessHandle(_processId, GetSafeWaitHandle());
        }

        /// <summary>
        /// Starts the process using the supplied start info.
        /// With UseShellExecute option, we'll try the shell tools to launch it(e.g. "open fileName")
        /// </summary>
        /// <param name="startInfo">The start info with which to start the process.</param>
        private bool StartCore(ProcessStartInfo startInfo)
        {
            if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
            {
                throw new PlatformNotSupportedException();
            }

            EnsureInitialized();

            string? filename;
            string[] argv;

            if (startInfo.UseShellExecute)
            {
                if (startInfo.RedirectStandardInput || startInfo.RedirectStandardOutput || startInfo.RedirectStandardError)
                {
                    throw new InvalidOperationException(SR.CantRedirectStreams);
                }
            }

            int stdinFd = -1, stdoutFd = -1, stderrFd = -1;
            string[] envp = CreateEnvp(startInfo);
            string? cwd = !string.IsNullOrWhiteSpace(startInfo.WorkingDirectory) ? startInfo.WorkingDirectory : null;

            bool setCredentials = !string.IsNullOrEmpty(startInfo.UserName);
            uint userId = 0;
            uint groupId = 0;
            uint[]? groups = null;
            if (setCredentials)
            {
                (userId, groupId, groups) = GetUserAndGroupIds(startInfo);
            }

            // .NET applications don't echo characters unless there is a Console.Read operation.
            // Unix applications expect the terminal to be in an echoing state by default.
            // To support processes that interact with the terminal (e.g. 'vi'), we need to configure the
            // terminal to echo. We keep this configuration as long as there are children possibly using the terminal.
            bool usesTerminal = !(startInfo.RedirectStandardInput &&
                                  startInfo.RedirectStandardOutput &&
                                  startInfo.RedirectStandardError);

            if (startInfo.UseShellExecute)
            {
                string verb = startInfo.Verb;
                if (verb != string.Empty &&
                    !string.Equals(verb, "open", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Win32Exception(Interop.Errors.ERROR_NO_ASSOCIATION);
                }

                // On Windows, UseShellExecute of executables and scripts causes those files to be executed.
                // To achieve this on Unix, we check if the file is executable (x-bit).
                // Some files may have the x-bit set even when they are not executable. This happens for example
                // when a Windows filesystem is mounted on Linux. To handle that, treat it as a regular file
                // when exec returns ENOEXEC (file format cannot be executed).
                bool isExecuting = false;
                filename = ResolveExecutableForShellExecute(startInfo.FileName, cwd);
                if (filename != null)
                {
                    argv = ParseArgv(startInfo);

                    isExecuting = ForkAndExecProcess(filename, argv, envp, cwd,
                        startInfo.RedirectStandardInput, startInfo.RedirectStandardOutput, startInfo.RedirectStandardError,
                        setCredentials, userId, groupId, groups,
                        out stdinFd, out stdoutFd, out stderrFd, usesTerminal,
                        throwOnNoExec: false); // return false instead of throwing on ENOEXEC
                }

                // use default program to open file/url
                if (!isExecuting)
                {
                    filename = GetPathToOpenFile();
                    argv = ParseArgv(startInfo, filename, ignoreArguments: true);

                    ForkAndExecProcess(filename, argv, envp, cwd,
                        startInfo.RedirectStandardInput, startInfo.RedirectStandardOutput, startInfo.RedirectStandardError,
                        setCredentials, userId, groupId, groups,
                        out stdinFd, out stdoutFd, out stderrFd, usesTerminal);
                }
            }
            else
            {
                filename = ResolvePath(startInfo.FileName);
                argv = ParseArgv(startInfo);
                if (Directory.Exists(filename))
                {
                    throw new Win32Exception(SR.DirectoryNotValidAsInput);
                }

                ForkAndExecProcess(filename, argv, envp, cwd,
                    startInfo.RedirectStandardInput, startInfo.RedirectStandardOutput, startInfo.RedirectStandardError,
                    setCredentials, userId, groupId, groups,
                    out stdinFd, out stdoutFd, out stderrFd, usesTerminal);
            }

            // Configure the parent's ends of the redirection streams.
            // We use UTF8 encoding without BOM by-default(instead of Console encoding as on Windows)
            // as there is no good way to get this information from the native layer
            // and we do not want to take dependency on Console contract.
            if (startInfo.RedirectStandardInput)
            {
                Debug.Assert(stdinFd >= 0);
                _standardInput = new StreamWriter(OpenStream(stdinFd, PipeDirection.Out),
                    startInfo.StandardInputEncoding ?? Encoding.Default, StreamBufferSize)
                { AutoFlush = true };
            }
            if (startInfo.RedirectStandardOutput)
            {
                Debug.Assert(stdoutFd >= 0);
                _standardOutput = new StreamReader(OpenStream(stdoutFd, PipeDirection.In),
                    startInfo.StandardOutputEncoding ?? Encoding.Default, true, StreamBufferSize);
            }
            if (startInfo.RedirectStandardError)
            {
                Debug.Assert(stderrFd >= 0);
                _standardError = new StreamReader(OpenStream(stderrFd, PipeDirection.In),
                    startInfo.StandardErrorEncoding ?? Encoding.Default, true, StreamBufferSize);
            }

            return true;
        }

        private bool ForkAndExecProcess(
            string? filename, string[] argv, string[] envp, string? cwd,
            bool redirectStdin, bool redirectStdout, bool redirectStderr,
            bool setCredentials, uint userId, uint groupId, uint[]? groups,
            out int stdinFd, out int stdoutFd, out int stderrFd,
            bool usesTerminal, bool throwOnNoExec = true)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new Win32Exception(Interop.Error.ENOENT.Info().RawErrno);
            }

            // Lock to avoid races with OnSigChild
            // By using a ReaderWriterLock we allow multiple processes to start concurrently.
            s_processStartLock.EnterReadLock();
            try
            {
                if (usesTerminal)
                {
                    ConfigureTerminalForChildProcesses(1);
                }

                int childPid;

                // Invoke the shim fork/execve routine.  It will create pipes for all requested
                // redirects, fork a child process, map the pipe ends onto the appropriate stdin/stdout/stderr
                // descriptors, and execve to execute the requested process.  The shim implementation
                // is used to fork/execve as executing managed code in a forked process is not safe (only
                // the calling thread will transfer, thread IDs aren't stable across the fork, etc.)
                int errno = Interop.Sys.ForkAndExecProcess(
                    filename, argv, envp, cwd,
                    redirectStdin, redirectStdout, redirectStderr,
                    setCredentials, userId, groupId, groups,
                    out childPid,
                    out stdinFd, out stdoutFd, out stderrFd);

                if (errno == 0)
                {
                    // Ensure we'll reap this process.
                    // note: SetProcessId will set this if we don't set it first.
                    _waitStateHolder = new ProcessWaitState.Holder(childPid, isNewChild: true, usesTerminal);

                    // Store the child's information into this Process object.
                    Debug.Assert(childPid >= 0);
                    SetProcessId(childPid);
                    SetProcessHandle(new SafeProcessHandle(_processId, GetSafeWaitHandle()));

                    return true;
                }
                else
                {
                    if (!throwOnNoExec &&
                        new Interop.ErrorInfo(errno).Error == Interop.Error.ENOEXEC)
                    {
                        return false;
                    }

                    throw new Win32Exception(errno);
                }
            }
            finally
            {
                s_processStartLock.ExitReadLock();

                if (_waitStateHolder == null && usesTerminal)
                {
                    // We failed to launch a child that could use the terminal.
                    s_processStartLock.EnterWriteLock();
                    ConfigureTerminalForChildProcesses(-1);
                    s_processStartLock.ExitWriteLock();
                }
            }
        }

        /// <summary>Finalizable holder for the underlying shared wait state object.</summary>
        private ProcessWaitState.Holder? _waitStateHolder;

        /// <summary>Size to use for redirect streams and stream readers/writers.</summary>
        private const int StreamBufferSize = 4096;

        /// <summary>Converts the filename and arguments information from a ProcessStartInfo into an argv array.</summary>
        /// <param name="psi">The ProcessStartInfo.</param>
        /// <param name="resolvedExe">Resolved executable to open ProcessStartInfo.FileName</param>
        /// <param name="ignoreArguments">Don't pass ProcessStartInfo.Arguments</param>
        /// <returns>The argv array.</returns>
        private static string[] ParseArgv(ProcessStartInfo psi, string? resolvedExe = null, bool ignoreArguments = false)
        {
            if (string.IsNullOrEmpty(resolvedExe) &&
                (ignoreArguments || (string.IsNullOrEmpty(psi.Arguments) && !psi.HasArgumentList)))
            {
                return new string[] { psi.FileName };
            }

            var argvList = new List<string>();
            if (!string.IsNullOrEmpty(resolvedExe))
            {
                argvList.Add(resolvedExe);
                if (resolvedExe.Contains("kfmclient"))
                {
                    argvList.Add("openURL"); // kfmclient needs OpenURL
                }
            }

            argvList.Add(psi.FileName);

            if (!ignoreArguments)
            {
                if (!string.IsNullOrEmpty(psi.Arguments))
                {
                    ParseArgumentsIntoList(psi.Arguments, argvList);
                }
                else if (psi.HasArgumentList)
                {
                    argvList.AddRange(psi.ArgumentList);
                }
            }
            return argvList.ToArray();
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

        private static string? ResolveExecutableForShellExecute(string filename, string? workingDirectory)
        {
            // Determine if filename points to an executable file.
            // filename may be an absolute path, a relative path or a uri.

            string? resolvedFilename = null;
            // filename is an absolute path
            if (Path.IsPathRooted(filename))
            {
                if (File.Exists(filename))
                {
                    resolvedFilename = filename;
                }
            }
            // filename is a uri
            else if (Uri.TryCreate(filename, UriKind.Absolute, out Uri? uri))
            {
                if (uri.IsFile && uri.Host == "" && File.Exists(uri.LocalPath))
                {
                    resolvedFilename = uri.LocalPath;
                }
            }
            // filename is relative
            else
            {
                // The WorkingDirectory property specifies the location of the executable.
                // If WorkingDirectory is an empty string, the current directory is understood to contain the executable.
                workingDirectory = workingDirectory != null ? Path.GetFullPath(workingDirectory) :
                                                              Directory.GetCurrentDirectory();
                string filenameInWorkingDirectory = Path.Combine(workingDirectory, filename);
                // filename is a relative path in the working directory
                if (File.Exists(filenameInWorkingDirectory))
                {
                    resolvedFilename = filenameInWorkingDirectory;
                }
                // find filename on PATH
                else
                {
                    resolvedFilename = FindProgramInPath(filename);
                }
            }

            if (resolvedFilename == null)
            {
                return null;
            }

            if (Interop.Sys.Access(resolvedFilename, Interop.Sys.AccessMode.X_OK) == 0)
            {
                return resolvedFilename;
            }
            else
            {
                return null;
            }
        }

        /// <summary>Resolves a path to the filename passed to ProcessStartInfo. </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>The resolved path. It can return null in case of URLs.</returns>
        private static string? ResolvePath(string filename)
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
            string? path = Environment.ProcessPath;
            if (path != null)
            {
                try
                {
                    path = Path.Combine(Path.GetDirectoryName(path)!, filename);
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
            return FindProgramInPath(filename);
        }

        /// <summary>
        /// Gets the path to the program
        /// </summary>
        /// <param name="program"></param>
        /// <returns></returns>
        private static string? FindProgramInPath(string program)
        {
            string path;
            string? pathEnvVar = Environment.GetEnvironmentVariable("PATH");
            if (pathEnvVar != null)
            {
                var pathParser = new StringParser(pathEnvVar, ':', skipEmpty: true);
                while (pathParser.MoveNext())
                {
                    string subPath = pathParser.ExtractCurrent();
                    path = Path.Combine(subPath, program);
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
            }
            return null;
        }

        private static long s_ticksPerSecond;

        /// <summary>Convert a number of "jiffies", or ticks, to a TimeSpan.</summary>
        /// <param name="ticks">The number of ticks.</param>
        /// <returns>The equivalent TimeSpan.</returns>
        internal static TimeSpan TicksToTimeSpan(double ticks)
        {
            long ticksPerSecond = Volatile.Read(ref s_ticksPerSecond);
            if (ticksPerSecond == 0)
            {
                // Look up the number of ticks per second in the system's configuration,
                // then use that to convert to a TimeSpan
                ticksPerSecond = Interop.Sys.SysConf(Interop.Sys.SysConfName._SC_CLK_TCK);
                if (ticksPerSecond <= 0)
                {
                    throw new Win32Exception();
                }

                Volatile.Write(ref s_ticksPerSecond, ticksPerSecond);
            }

            return TimeSpan.FromSeconds(ticks / (double)ticksPerSecond);
        }

        /// <summary>Opens a stream around the specified file descriptor and with the specified access.</summary>
        /// <param name="fd">The file descriptor.</param>
        /// <param name="direction">The pipe direction.</param>
        /// <returns>The opened stream.</returns>
        private static Stream OpenStream(int fd, PipeDirection direction)
        {
            Debug.Assert(fd >= 0);
            return new AnonymousPipeClientStream(direction, new SafePipeHandle((IntPtr)fd, ownsHandle: true));
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
            // Iterate through all of the characters in the argument string.
            for (int i = 0; i < arguments.Length; i++)
            {
                while (i < arguments.Length && (arguments[i] == ' ' || arguments[i] == '\t'))
                    i++;

                if (i == arguments.Length)
                    break;

                results.Add(GetNextArgument(arguments, ref i));
            }
        }

        private static string GetNextArgument(string arguments, ref int i)
        {
            var currentArgument = new ValueStringBuilder(stackalloc char[256]);
            bool inQuotes = false;

            while (i < arguments.Length)
            {
                // From the current position, iterate through contiguous backslashes.
                int backslashCount = 0;
                while (i < arguments.Length && arguments[i] == '\\')
                {
                    i++;
                    backslashCount++;
                }

                if (backslashCount > 0)
                {
                    if (i >= arguments.Length || arguments[i] != '"')
                    {
                        // Backslashes not followed by a double quote:
                        // they should all be treated as literal backslashes.
                        currentArgument.Append('\\', backslashCount);
                    }
                    else
                    {
                        // Backslashes followed by a double quote:
                        // - Output a literal slash for each complete pair of slashes
                        // - If one remains, use it to make the subsequent quote a literal.
                        currentArgument.Append('\\', backslashCount / 2);
                        if (backslashCount % 2 != 0)
                        {
                            currentArgument.Append('"');
                            i++;
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
                    if (inQuotes && i < arguments.Length - 1 && arguments[i + 1] == '"')
                    {
                        // Two consecutive double quotes inside an inQuotes region should result in a literal double quote
                        // (the parser is left in the inQuotes region).
                        // This behavior is not part of the spec of code:ParseArgumentsIntoList, but is compatible with CRT
                        // and .NET Framework.
                        currentArgument.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }

                    i++;
                    continue;
                }

                // If this is a space/tab and we're not in quotes, we're done with the current
                // argument, it should be added to the results and then reset for the next one.
                if ((c == ' ' || c == '\t') && !inQuotes)
                {
                    break;
                }

                // Nothing special; add the character to the current argument.
                currentArgument.Append(c);
                i++;
            }

            return currentArgument.ToString();
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

        private SafeWaitHandle GetSafeWaitHandle()
            => GetWaitState().EnsureExitedEvent().GetSafeWaitHandle();

        private static (uint userId, uint groupId, uint[] groups) GetUserAndGroupIds(ProcessStartInfo startInfo)
        {
            Debug.Assert(!string.IsNullOrEmpty(startInfo.UserName));

            (uint? userId, uint? groupId) = GetUserAndGroupIds(startInfo.UserName);

            Debug.Assert(userId.HasValue == groupId.HasValue, "userId and groupId both need to have values, or both need to be null.");
            if (!userId.HasValue)
            {
                throw new Win32Exception(SR.Format(SR.UserDoesNotExist, startInfo.UserName));
            }

            uint[]? groups = Interop.Sys.GetGroupList(startInfo.UserName, groupId!.Value);
            if (groups == null)
            {
                throw new Win32Exception(SR.Format(SR.UserGroupsCannotBeDetermined, startInfo.UserName));
            }

            return (userId.Value, groupId.Value, groups);
        }

        private static unsafe (uint? userId, uint? groupId) GetUserAndGroupIds(string userName)
        {
            Interop.Sys.Passwd? passwd;
            // First try with a buffer that should suffice for 99% of cases.
            // Note: on CentOS/RedHat 7.1 systems, getpwnam_r returns 'user not found' if the buffer is too small
            // see https://bugs.centos.org/view.php?id=7324
            const int BufLen = Interop.Sys.Passwd.InitialBufferSize;
            byte* stackBuf = stackalloc byte[BufLen];
            if (TryGetPasswd(userName, stackBuf, BufLen, out passwd))
            {
                if (passwd == null)
                {
                    return (null, null);
                }
                return (passwd.Value.UserId, passwd.Value.GroupId);
            }

            // Fallback to heap allocations if necessary, growing the buffer until
            // we succeed.  TryGetPasswd will throw if there's an unexpected error.
            int lastBufLen = BufLen;
            while (true)
            {
                lastBufLen *= 2;
                byte[] heapBuf = new byte[lastBufLen];
                fixed (byte* buf = &heapBuf[0])
                {
                    if (TryGetPasswd(userName, buf, heapBuf.Length, out passwd))
                    {
                        if (passwd == null)
                        {
                            return (null, null);
                        }
                        return (passwd.Value.UserId, passwd.Value.GroupId);
                    }
                }
            }
        }

        private static unsafe bool TryGetPasswd(string name, byte* buf, int bufLen, out Interop.Sys.Passwd? passwd)
        {
            // Call getpwnam_r to get the passwd struct
            Interop.Sys.Passwd tempPasswd;
            int error = Interop.Sys.GetPwNamR(name, out tempPasswd, buf, bufLen);

            // If the call succeeds, give back the passwd retrieved
            if (error == 0)
            {
                passwd = tempPasswd;
                return true;
            }

            // If the current user's entry could not be found, give back null,
            // but still return true as false indicates the buffer was too small.
            if (error == -1)
            {
                passwd = null;
                return true;
            }

            var errorInfo = new Interop.ErrorInfo(error);

            // If the call failed because the buffer was too small, return false to
            // indicate the caller should try again with a larger buffer.
            if (errorInfo.Error == Interop.Error.ERANGE)
            {
                passwd = null;
                return false;
            }

            // Otherwise, fail.
            throw new Win32Exception(errorInfo.RawErrno, errorInfo.GetErrorMessage());
        }

        public IntPtr MainWindowHandle => IntPtr.Zero;

        private bool CloseMainWindowCore() => false;

        public string MainWindowTitle => string.Empty;

        public bool Responding => true;

        private bool WaitForInputIdleCore(int milliseconds) => throw new InvalidOperationException(SR.InputIdleUnkownError);

        private static unsafe void EnsureInitialized()
        {
            if (s_initialized)
            {
                return;
            }

            lock (s_initializedGate)
            {
                if (!s_initialized)
                {
                    if (!Interop.Sys.InitializeTerminalAndSignalHandling())
                    {
                        throw new Win32Exception();
                    }

                    // Register our callback.
                    Interop.Sys.RegisterForSigChld(&OnSigChild);

                    s_initialized = true;
                }
            }
        }

        [UnmanagedCallersOnly]
        private static void OnSigChild(int reapAll)
        {
            // Lock to avoid races with Process.Start
            s_processStartLock.EnterWriteLock();
            try
            {
                ProcessWaitState.CheckChildren(reapAll != 0);
            }
            finally
            {
                s_processStartLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// This method is called when the number of child processes that are using the terminal changes.
        /// It updates the terminal configuration if necessary.
        /// </summary>
        internal static void ConfigureTerminalForChildProcesses(int increment)
        {
            ConfigureTerminalForChildProcessesInner(increment);
        }

        static partial void ConfigureTerminalForChildProcessesInner(int increment);
    }
}
