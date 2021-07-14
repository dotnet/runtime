// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Threading;

namespace System.Diagnostics
{
    public partial class Process : IDisposable
    {
        private static readonly object s_createProcessLock = new object();

        /// <summary>
        /// Creates an array of <see cref="Process"/> components that are associated with process resources on a
        /// remote computer. These process resources share the specified process name.
        /// </summary>
        public static Process[] GetProcessesByName(string? processName, string machineName)
        {
            if (processName == null)
            {
                processName = string.Empty;
            }

            Process[] procs = GetProcesses(machineName);
            var list = new List<Process>();

            for (int i = 0; i < procs.Length; i++)
            {
                if (string.Equals(processName, procs[i].ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(procs[i]);
                }
                else
                {
                    procs[i].Dispose();
                }
            }

            return list.ToArray();
        }

        [CLSCompliant(false)]
        [SupportedOSPlatform("windows")]
        public static Process? Start(string fileName, string userName, SecureString password, string domain)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(fileName);
            startInfo.UserName = userName;
            startInfo.Password = password;
            startInfo.Domain = domain;
            startInfo.UseShellExecute = false;
            return Start(startInfo);
        }

        [CLSCompliant(false)]
        [SupportedOSPlatform("windows")]
        public static Process? Start(string fileName, string arguments, string userName, SecureString password, string domain)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(fileName, arguments);
            startInfo.UserName = userName;
            startInfo.Password = password;
            startInfo.Domain = domain;
            startInfo.UseShellExecute = false;
            return Start(startInfo);
        }

        /// <summary>
        /// Puts a Process component in state to interact with operating system processes that run in a
        /// special mode by enabling the native property SeDebugPrivilege on the current thread.
        /// </summary>
        public static void EnterDebugMode()
        {
            SetPrivilege(Interop.Advapi32.SeDebugPrivilege, (int)Interop.Advapi32.SEPrivileges.SE_PRIVILEGE_ENABLED);
        }

        /// <summary>
        /// Takes a Process component out of the state that lets it interact with operating system processes
        /// that run in a special mode.
        /// </summary>
        public static void LeaveDebugMode()
        {
            SetPrivilege(Interop.Advapi32.SeDebugPrivilege, 0);
        }

        /// <summary>Terminates the associated process immediately.</summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("tvos")]
        public void Kill()
        {
            using (SafeProcessHandle handle = GetProcessHandle(Interop.Advapi32.ProcessOptions.PROCESS_TERMINATE | Interop.Advapi32.ProcessOptions.PROCESS_QUERY_LIMITED_INFORMATION, throwIfExited: false))
            {
                // If the process has exited, the handle is invalid.
                if (handle.IsInvalid)
                    return;

                if (!Interop.Kernel32.TerminateProcess(handle, -1))
                {
                    // Capture the exception
                    var exception = new Win32Exception();

                    // Don't throw if the process has exited.
                    if (exception.NativeErrorCode == Interop.Errors.ERROR_ACCESS_DENIED &&
                        Interop.Kernel32.GetExitCodeProcess(handle, out int localExitCode) && localExitCode != Interop.Kernel32.HandleOptions.STILL_ACTIVE)
                    {
                        return;
                    }

                    throw exception;
                }
            }
        }

        /// <summary>Discards any information about the associated process.</summary>
        private void RefreshCore()
        {
            _signaled = false;
            _haveMainWindow = false;
            _mainWindowTitle = null;
            _haveResponding = false;
        }

        /// <summary>Additional logic invoked when the Process is closed.</summary>
        private void CloseCore()
        {
            // Nop
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
                        Debug.Assert(Associated, "Process.EnsureWatchingForExit called with no associated process");
                        _watchingForExit = true;
                        try
                        {
                            _waitHandle = new Interop.Kernel32.ProcessWaitHandle(GetOrOpenProcessHandle());
                            _registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(_waitHandle,
                                new WaitOrTimerCallback(CompletionCallback), _waitHandle, -1, true);
                        }
                        catch
                        {
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
            SafeProcessHandle? handle = null;
            try
            {
                handle = GetProcessHandle(Interop.Advapi32.ProcessOptions.SYNCHRONIZE, false);
                if (handle.IsInvalid)
                    return true;

                using (Interop.Kernel32.ProcessWaitHandle processWaitHandle = new Interop.Kernel32.ProcessWaitHandle(handle))
                {
                    return _signaled = processWaitHandle.WaitOne(milliseconds);
                }
            }
            finally
            {
                // If we have a hard timeout, we cannot wait for the streams
                if (milliseconds == Timeout.Infinite)
                {
                    _output?.EOF.GetAwaiter().GetResult();
                    _error?.EOF.GetAwaiter().GetResult();
                }

                handle?.Dispose();
            }
        }

        /// <summary>Gets the main module for the associated process.</summary>
        public ProcessModule? MainModule
        {
            get
            {
                // We only return null if we couldn't find a main module. This could be because
                // the process hasn't finished loading the main module (most likely).
                // On NT, the first module is the main module.
                EnsureState(State.HaveId | State.IsLocal);
                return NtProcessManager.GetFirstModule(_processId);
            }
        }

        /// <summary>Checks whether the process has exited and updates state accordingly.</summary>
        private void UpdateHasExited()
        {
            using (SafeProcessHandle handle = GetProcessHandle(
                Interop.Advapi32.ProcessOptions.PROCESS_QUERY_LIMITED_INFORMATION | Interop.Advapi32.ProcessOptions.SYNCHRONIZE, false))
            {
                if (handle.IsInvalid)
                {
                    _exited = true;
                }
                else
                {
                    int localExitCode;

                    // Although this is the wrong way to check whether the process has exited,
                    // it was historically the way we checked for it, and a lot of code then took a dependency on
                    // the fact that this would always be set before the pipes were closed, so they would read
                    // the exit code out after calling ReadToEnd() or standard output or standard error. In order
                    // to allow 259 to function as a valid exit code and to break as few people as possible that
                    // took the ReadToEnd dependency, we check for an exit code before doing the more correct
                    // check to see if we have been signaled.
                    if (Interop.Kernel32.GetExitCodeProcess(handle, out localExitCode) && localExitCode != Interop.Kernel32.HandleOptions.STILL_ACTIVE)
                    {
                        _exitCode = localExitCode;
                        _exited = true;
                    }
                    else
                    {
                        // The best check for exit is that the kernel process object handle is invalid,
                        // or that it is valid and signaled.  Checking if the exit code != STILL_ACTIVE
                        // does not guarantee the process is closed,
                        // since some process could return an actual STILL_ACTIVE exit code (259).
                        if (!_signaled) // if we just came from WaitForExit, don't repeat
                        {
                            using (var wh = new Interop.Kernel32.ProcessWaitHandle(handle))
                            {
                                _signaled = wh.WaitOne(0);
                            }
                        }
                        if (_signaled)
                        {
                            if (!Interop.Kernel32.GetExitCodeProcess(handle, out localExitCode))
                                throw new Win32Exception();

                            _exitCode = localExitCode;
                            _exited = true;
                        }
                    }
                }
            }
        }

        /// <summary>Gets the time that the associated process exited.</summary>
        private DateTime ExitTimeCore
        {
            get { return GetProcessTimes().ExitTime; }
        }

        /// <summary>Gets the amount of time the process has spent running code inside the operating system core.</summary>
        public TimeSpan PrivilegedProcessorTime
        {
            get { return GetProcessTimes().PrivilegedProcessorTime; }
        }

        /// <summary>Gets the time the associated process was started.</summary>
        internal DateTime StartTimeCore
        {
            get { return GetProcessTimes().StartTime; }
        }

        /// <summary>
        /// Gets the amount of time the associated process has spent utilizing the CPU.
        /// It is the sum of the <see cref='System.Diagnostics.Process.UserProcessorTime'/> and
        /// <see cref='System.Diagnostics.Process.PrivilegedProcessorTime'/>.
        /// </summary>
        public TimeSpan TotalProcessorTime
        {
            get { return GetProcessTimes().TotalProcessorTime; }
        }

        /// <summary>
        /// Gets the amount of time the associated process has spent running code
        /// inside the application portion of the process (not the operating system core).
        /// </summary>
        public TimeSpan UserProcessorTime
        {
            get { return GetProcessTimes().UserProcessorTime; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the associated process priority
        /// should be temporarily boosted by the operating system when the main window
        /// has focus.
        /// </summary>
        private bool PriorityBoostEnabledCore
        {
            get
            {
                using (SafeProcessHandle handle = GetProcessHandle(Interop.Advapi32.ProcessOptions.PROCESS_QUERY_INFORMATION))
                {
                    bool disabled;
                    if (!Interop.Kernel32.GetProcessPriorityBoost(handle, out disabled))
                    {
                        throw new Win32Exception();
                    }
                    return !disabled;
                }
            }
            set
            {
                using (SafeProcessHandle handle = GetProcessHandle(Interop.Advapi32.ProcessOptions.PROCESS_SET_INFORMATION))
                {
                    if (!Interop.Kernel32.SetProcessPriorityBoost(handle, !value))
                        throw new Win32Exception();
                }
            }
        }

        /// <summary>
        /// Gets or sets the overall priority category for the associated process.
        /// </summary>
        private ProcessPriorityClass PriorityClassCore
        {
            get
            {
                using (SafeProcessHandle handle = GetProcessHandle(Interop.Advapi32.ProcessOptions.PROCESS_QUERY_INFORMATION))
                {
                    int value = Interop.Kernel32.GetPriorityClass(handle);
                    if (value == 0)
                    {
                        throw new Win32Exception();
                    }
                    return (ProcessPriorityClass)value;
                }
            }
            set
            {
                using (SafeProcessHandle handle = GetProcessHandle(Interop.Advapi32.ProcessOptions.PROCESS_SET_INFORMATION))
                {
                    if (!Interop.Kernel32.SetPriorityClass(handle, (int)value))
                        throw new Win32Exception();
                }
            }
        }

        /// <summary>
        /// Gets or sets which processors the threads in this process can be scheduled to run on.
        /// </summary>
        private IntPtr ProcessorAffinityCore
        {
            get
            {
                using (SafeProcessHandle handle = GetProcessHandle(Interop.Advapi32.ProcessOptions.PROCESS_QUERY_INFORMATION))
                {
                    IntPtr processAffinity, systemAffinity;
                    if (!Interop.Kernel32.GetProcessAffinityMask(handle, out processAffinity, out systemAffinity))
                        throw new Win32Exception();
                    return processAffinity;
                }
            }
            set
            {
                using (SafeProcessHandle handle = GetProcessHandle(Interop.Advapi32.ProcessOptions.PROCESS_SET_INFORMATION))
                {
                    if (!Interop.Kernel32.SetProcessAffinityMask(handle, value))
                        throw new Win32Exception();
                }
            }
        }

        /// <summary>
        /// Gets a short-term handle to the process, with the given access.  If a handle exists,
        /// then it is reused.  If the process has exited, it throws an exception.
        /// </summary>
        private SafeProcessHandle GetProcessHandle()
        {
            return GetProcessHandle(Interop.Advapi32.ProcessOptions.PROCESS_ALL_ACCESS);
        }

        /// <summary>Get the minimum and maximum working set limits.</summary>
        private void GetWorkingSetLimits(out IntPtr minWorkingSet, out IntPtr maxWorkingSet)
        {
            using (SafeProcessHandle handle = GetProcessHandle(Interop.Advapi32.ProcessOptions.PROCESS_QUERY_INFORMATION))
            {
                int ignoredFlags;
                if (!Interop.Kernel32.GetProcessWorkingSetSizeEx(handle, out minWorkingSet, out maxWorkingSet, out ignoredFlags))
                    throw new Win32Exception();
            }
        }

        /// <summary>Sets one or both of the minimum and maximum working set limits.</summary>
        /// <param name="newMin">The new minimum working set limit, or null not to change it.</param>
        /// <param name="newMax">The new maximum working set limit, or null not to change it.</param>
        /// <param name="resultingMin">The resulting minimum working set limit after any changes applied.</param>
        /// <param name="resultingMax">The resulting maximum working set limit after any changes applied.</param>
        private void SetWorkingSetLimitsCore(IntPtr? newMin, IntPtr? newMax, out IntPtr resultingMin, out IntPtr resultingMax)
        {
            using (SafeProcessHandle handle = GetProcessHandle(Interop.Advapi32.ProcessOptions.PROCESS_QUERY_INFORMATION | Interop.Advapi32.ProcessOptions.PROCESS_SET_QUOTA))
            {
                IntPtr min, max;
                int ignoredFlags;
                if (!Interop.Kernel32.GetProcessWorkingSetSizeEx(handle, out min, out max, out ignoredFlags))
                {
                    throw new Win32Exception();
                }

                if (newMin.HasValue)
                {
                    min = newMin.Value;
                }
                if (newMax.HasValue)
                {
                    max = newMax.Value;
                }

                if ((long)min > (long)max)
                {
                    if (newMin != null)
                    {
                        throw new ArgumentException(SR.BadMinWorkset);
                    }
                    else
                    {
                        throw new ArgumentException(SR.BadMaxWorkset);
                    }
                }

                // We use SetProcessWorkingSetSizeEx which gives an option to follow
                // the max and min value even in low-memory and abundant-memory situations.
                // However, we do not use these flags to emulate the existing behavior
                if (!Interop.Kernel32.SetProcessWorkingSetSizeEx(handle, min, max, 0))
                {
                    throw new Win32Exception();
                }

                // The value may be rounded/changed by the OS, so go get it
                if (!Interop.Kernel32.GetProcessWorkingSetSizeEx(handle, out min, out max, out ignoredFlags))
                {
                    throw new Win32Exception();
                }

                resultingMin = min;
                resultingMax = max;
            }
        }

        /// <summary>Starts the process using the supplied start info.</summary>
        /// <param name="startInfo">The start info with which to start the process.</param>
        private unsafe bool StartWithCreateProcess(ProcessStartInfo startInfo)
        {
            // See knowledge base article Q190351 for an explanation of the following code.  Noteworthy tricky points:
            //    * The handles are duplicated as non-inheritable before they are passed to CreateProcess so
            //      that the child process can not close them
            //    * CreateProcess allows you to redirect all or none of the standard IO handles, so we use
            //      GetStdHandle for the handles that are not being redirected

            var commandLine = new ValueStringBuilder(stackalloc char[256]);
            BuildCommandLine(startInfo, ref commandLine);

            Interop.Kernel32.STARTUPINFO startupInfo = default;
            Interop.Kernel32.PROCESS_INFORMATION processInfo = default;
            Interop.Kernel32.SECURITY_ATTRIBUTES unused_SecAttrs = default;
            SafeProcessHandle procSH = new SafeProcessHandle();

            // handles used in parent process
            SafeFileHandle? parentInputPipeHandle = null;
            SafeFileHandle? childInputPipeHandle = null;
            SafeFileHandle? parentOutputPipeHandle = null;
            SafeFileHandle? childOutputPipeHandle = null;
            SafeFileHandle? parentErrorPipeHandle = null;
            SafeFileHandle? childErrorPipeHandle = null;

            // Take a global lock to synchronize all redirect pipe handle creations and CreateProcess
            // calls. We do not want one process to inherit the handles created concurrently for another
            // process, as that will impact the ownership and lifetimes of those handles now inherited
            // into multiple child processes.
            lock (s_createProcessLock)
            {
                try
                {
                    startupInfo.cb = sizeof(Interop.Kernel32.STARTUPINFO);

                    // set up the streams
                    if (startInfo.RedirectStandardInput || startInfo.RedirectStandardOutput || startInfo.RedirectStandardError)
                    {
                        if (startInfo.RedirectStandardInput)
                        {
                            CreatePipe(out parentInputPipeHandle, out childInputPipeHandle, true);
                        }
                        else
                        {
                            childInputPipeHandle = new SafeFileHandle(Interop.Kernel32.GetStdHandle(Interop.Kernel32.HandleTypes.STD_INPUT_HANDLE), false);
                        }

                        if (startInfo.RedirectStandardOutput)
                        {
                            CreatePipe(out parentOutputPipeHandle, out childOutputPipeHandle, false);
                        }
                        else
                        {
                            childOutputPipeHandle = new SafeFileHandle(Interop.Kernel32.GetStdHandle(Interop.Kernel32.HandleTypes.STD_OUTPUT_HANDLE), false);
                        }

                        if (startInfo.RedirectStandardError)
                        {
                            CreatePipe(out parentErrorPipeHandle, out childErrorPipeHandle, false);
                        }
                        else
                        {
                            childErrorPipeHandle = new SafeFileHandle(Interop.Kernel32.GetStdHandle(Interop.Kernel32.HandleTypes.STD_ERROR_HANDLE), false);
                        }

                        startupInfo.hStdInput = childInputPipeHandle.DangerousGetHandle();
                        startupInfo.hStdOutput = childOutputPipeHandle.DangerousGetHandle();
                        startupInfo.hStdError = childErrorPipeHandle.DangerousGetHandle();

                        startupInfo.dwFlags = Interop.Advapi32.StartupInfoOptions.STARTF_USESTDHANDLES;
                    }

                    // set up the creation flags parameter
                    int creationFlags = 0;
                    if (startInfo.CreateNoWindow) creationFlags |= Interop.Advapi32.StartupInfoOptions.CREATE_NO_WINDOW;

                    // set up the environment block parameter
                    string? environmentBlock = null;
                    if (startInfo._environmentVariables != null)
                    {
                        creationFlags |= Interop.Advapi32.StartupInfoOptions.CREATE_UNICODE_ENVIRONMENT;
                        environmentBlock = GetEnvironmentVariablesBlock(startInfo._environmentVariables!);
                    }

                    string? workingDirectory = startInfo.WorkingDirectory;
                    if (workingDirectory.Length == 0)
                    {
                        workingDirectory = null;
                    }

                    bool retVal;
                    int errorCode = 0;

                    if (startInfo.UserName.Length != 0)
                    {
                        if (startInfo.Password != null && startInfo.PasswordInClearText != null)
                        {
                            throw new ArgumentException(SR.CantSetDuplicatePassword);
                        }

                        Interop.Advapi32.LogonFlags logonFlags = (Interop.Advapi32.LogonFlags)0;
                        if (startInfo.LoadUserProfile)
                        {
                            logonFlags = Interop.Advapi32.LogonFlags.LOGON_WITH_PROFILE;
                        }

                        fixed (char* passwordInClearTextPtr = startInfo.PasswordInClearText ?? string.Empty)
                        fixed (char* environmentBlockPtr = environmentBlock)
                        fixed (char* commandLinePtr = &commandLine.GetPinnableReference(terminate: true))
                        {
                            IntPtr passwordPtr = (startInfo.Password != null) ?
                                Marshal.SecureStringToGlobalAllocUnicode(startInfo.Password) : IntPtr.Zero;

                            try
                            {
                                retVal = Interop.Advapi32.CreateProcessWithLogonW(
                                    startInfo.UserName,
                                    startInfo.Domain,
                                    (passwordPtr != IntPtr.Zero) ? passwordPtr : (IntPtr)passwordInClearTextPtr,
                                    logonFlags,
                                    null,            // we don't need this since all the info is in commandLine
                                    commandLinePtr,
                                    creationFlags,
                                    (IntPtr)environmentBlockPtr,
                                    workingDirectory,
                                    ref startupInfo,        // pointer to STARTUPINFO
                                    ref processInfo         // pointer to PROCESS_INFORMATION
                                );
                                if (!retVal)
                                    errorCode = Marshal.GetLastWin32Error();
                            }
                            finally
                            {
                                if (passwordPtr != IntPtr.Zero)
                                    Marshal.ZeroFreeGlobalAllocUnicode(passwordPtr);
                            }
                        }
                    }
                    else
                    {
                        fixed (char* environmentBlockPtr = environmentBlock)
                        fixed (char* commandLinePtr = &commandLine.GetPinnableReference(terminate: true))
                        {
                            retVal = Interop.Kernel32.CreateProcess(
                                null,                // we don't need this since all the info is in commandLine
                                commandLinePtr,      // pointer to the command line string
                                ref unused_SecAttrs, // address to process security attributes, we don't need to inherit the handle
                                ref unused_SecAttrs, // address to thread security attributes.
                                true,                // handle inheritance flag
                                creationFlags,       // creation flags
                                (IntPtr)environmentBlockPtr, // pointer to new environment block
                                workingDirectory,    // pointer to current directory name
                                ref startupInfo,     // pointer to STARTUPINFO
                                ref processInfo      // pointer to PROCESS_INFORMATION
                            );
                            if (!retVal)
                                errorCode = Marshal.GetLastWin32Error();
                        }
                    }

                    if (processInfo.hProcess != IntPtr.Zero && processInfo.hProcess != new IntPtr(-1))
                        Marshal.InitHandle(procSH, processInfo.hProcess);
                    if (processInfo.hThread != IntPtr.Zero && processInfo.hThread != new IntPtr(-1))
                        Interop.Kernel32.CloseHandle(processInfo.hThread);

                    if (!retVal)
                    {
                        string nativeErrorMessage = errorCode == Interop.Errors.ERROR_BAD_EXE_FORMAT || errorCode == Interop.Errors.ERROR_EXE_MACHINE_TYPE_MISMATCH
                            ? SR.InvalidApplication
                            : GetErrorMessage(errorCode);

                        throw CreateExceptionForErrorStartingProcess(nativeErrorMessage, errorCode, startInfo.FileName, workingDirectory);
                    }
                }
                finally
                {
                    childInputPipeHandle?.Dispose();
                    childOutputPipeHandle?.Dispose();
                    childErrorPipeHandle?.Dispose();
                }
            }

            if (startInfo.RedirectStandardInput)
            {
                Encoding enc = startInfo.StandardInputEncoding ?? GetEncoding((int)Interop.Kernel32.GetConsoleCP());
                _standardInput = new StreamWriter(new FileStream(parentInputPipeHandle!, FileAccess.Write, 4096, false), enc, 4096);
                _standardInput.AutoFlush = true;
            }
            if (startInfo.RedirectStandardOutput)
            {
                Encoding enc = startInfo.StandardOutputEncoding ?? GetEncoding((int)Interop.Kernel32.GetConsoleOutputCP());
                _standardOutput = new StreamReader(new FileStream(parentOutputPipeHandle!, FileAccess.Read, 4096, false), enc, true, 4096);
            }
            if (startInfo.RedirectStandardError)
            {
                Encoding enc = startInfo.StandardErrorEncoding ?? GetEncoding((int)Interop.Kernel32.GetConsoleOutputCP());
                _standardError = new StreamReader(new FileStream(parentErrorPipeHandle!, FileAccess.Read, 4096, false), enc, true, 4096);
            }

            commandLine.Dispose();

            if (procSH.IsInvalid)
                return false;

            SetProcessHandle(procSH);
            SetProcessId((int)processInfo.dwProcessId);
            return true;
        }

        private static Encoding GetEncoding(int codePage)
        {
            Encoding enc = EncodingHelper.GetSupportedConsoleEncoding(codePage);
            return new ConsoleEncoding(enc); // ensure encoding doesn't output a preamble
        }

        private bool _signaled;

        private static void BuildCommandLine(ProcessStartInfo startInfo, ref ValueStringBuilder commandLine)
        {
            // Construct a StringBuilder with the appropriate command line
            // to pass to CreateProcess.  If the filename isn't already
            // in quotes, we quote it here.  This prevents some security
            // problems (it specifies exactly which part of the string
            // is the file to execute).
            ReadOnlySpan<char> fileName = startInfo.FileName.AsSpan().Trim();
            bool fileNameIsQuoted = fileName.Length > 0 && fileName[0] == '\"' && fileName[fileName.Length - 1] == '\"';
            if (!fileNameIsQuoted)
            {
                commandLine.Append('"');
            }

            commandLine.Append(fileName);

            if (!fileNameIsQuoted)
            {
                commandLine.Append('"');
            }

            startInfo.AppendArgumentsTo(ref commandLine);
        }

        /// <summary>Gets timing information for the current process.</summary>
        private ProcessThreadTimes GetProcessTimes()
        {
            using (SafeProcessHandle handle = GetProcessHandle(Interop.Advapi32.ProcessOptions.PROCESS_QUERY_LIMITED_INFORMATION, false))
            {
                if (handle.IsInvalid)
                {
                    throw new InvalidOperationException(SR.Format(SR.ProcessHasExited, _processId.ToString()));
                }

                ProcessThreadTimes processTimes = new ProcessThreadTimes();
                if (!Interop.Kernel32.GetProcessTimes(handle,
                    out processTimes._create, out processTimes._exit,
                    out processTimes._kernel, out processTimes._user))
                {
                    throw new Win32Exception();
                }

                return processTimes;
            }
        }

        private static unsafe void SetPrivilege(string privilegeName, int attrib)
        {
            // this is only a "pseudo handle" to the current process - no need to close it later
            SafeTokenHandle? hToken = null;

            try
            {
                // get the process token so we can adjust the privilege on it.  We DO need to
                // close the token when we're done with it.
                if (!Interop.Advapi32.OpenProcessToken(Interop.Kernel32.GetCurrentProcess(), Interop.Kernel32.HandleOptions.TOKEN_ADJUST_PRIVILEGES, out hToken))
                {
                    throw new Win32Exception();
                }

                if (!Interop.Advapi32.LookupPrivilegeValue(null, privilegeName, out Interop.Advapi32.LUID luid))
                {
                    throw new Win32Exception();
                }

                Interop.Advapi32.TOKEN_PRIVILEGE tp;
                tp.PrivilegeCount = 1;
                tp.Privileges.Luid = luid;
                tp.Privileges.Attributes = (uint)attrib;

                Interop.Advapi32.AdjustTokenPrivileges(hToken, false, &tp, 0, null, null);

                // AdjustTokenPrivileges can return true even if it failed to
                // set the privilege, so we need to use GetLastError
                if (Marshal.GetLastWin32Error() != Interop.Errors.ERROR_SUCCESS)
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                if (hToken != null)
                {
                    hToken.Dispose();
                }
            }
        }

        /// <devdoc>
        ///     Gets a short-term handle to the process, with the given access.
        ///     If a handle is stored in current process object, then use it.
        ///     Note that the handle we stored in current process object will have all access we need.
        /// </devdoc>
        /// <internalonly/>
        private SafeProcessHandle GetProcessHandle(int access, bool throwIfExited = true)
        {
            if (_haveProcessHandle)
            {
                if (throwIfExited)
                {
                    // Since haveProcessHandle is true, we know we have the process handle
                    // open with at least SYNCHRONIZE access, so we can wait on it with
                    // zero timeout to see if the process has exited.
                    using (Interop.Kernel32.ProcessWaitHandle waitHandle = new Interop.Kernel32.ProcessWaitHandle(_processHandle!))
                    {
                        if (waitHandle.WaitOne(0))
                        {
                            throw new InvalidOperationException(_haveProcessId ?
                                SR.Format(SR.ProcessHasExited, _processId.ToString()) :
                                SR.ProcessHasExitedNoId);
                        }
                    }
                }

                // If we dispose of our contained handle we'll be in a bad state. .NET Framework dealt with this
                // by doing a try..finally around every usage of GetProcessHandle and only disposed if
                // it wasn't our handle.
                return new SafeProcessHandle(_processHandle!.DangerousGetHandle(), ownsHandle: false);
            }
            else
            {
                EnsureState(State.HaveId | State.IsLocal);
                SafeProcessHandle handle = ProcessManager.OpenProcess(_processId, access, throwIfExited);
                if (throwIfExited && (access & Interop.Advapi32.ProcessOptions.PROCESS_QUERY_INFORMATION) != 0)
                {
                    if (Interop.Kernel32.GetExitCodeProcess(handle, out _exitCode) && _exitCode != Interop.Kernel32.HandleOptions.STILL_ACTIVE)
                    {
                        throw new InvalidOperationException(SR.Format(SR.ProcessHasExited, _processId.ToString()));
                    }
                }
                return handle;
            }
        }

        private static void CreatePipeWithSecurityAttributes(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, ref Interop.Kernel32.SECURITY_ATTRIBUTES lpPipeAttributes, int nSize)
        {
            bool ret = Interop.Kernel32.CreatePipe(out hReadPipe, out hWritePipe, ref lpPipeAttributes, nSize);
            if (!ret || hReadPipe.IsInvalid || hWritePipe.IsInvalid)
            {
                throw new Win32Exception();
            }
        }

        // Using synchronous Anonymous pipes for process input/output redirection means we would end up
        // wasting a worker threadpool thread per pipe instance. Overlapped pipe IO is desirable, since
        // it will take advantage of the NT IO completion port infrastructure. But we can't really use
        // Overlapped I/O for process input/output as it would break Console apps (managed Console class
        // methods such as WriteLine as well as native CRT functions like printf) which are making an
        // assumption that the console standard handles (obtained via GetStdHandle()) are opened
        // for synchronous I/O and hence they can work fine with ReadFile/WriteFile synchronously!
        private void CreatePipe(out SafeFileHandle parentHandle, out SafeFileHandle childHandle, bool parentInputs)
        {
            Interop.Kernel32.SECURITY_ATTRIBUTES securityAttributesParent = default;
            securityAttributesParent.bInheritHandle = Interop.BOOL.TRUE;

            SafeFileHandle? hTmp = null;
            try
            {
                if (parentInputs)
                {
                    CreatePipeWithSecurityAttributes(out childHandle, out hTmp, ref securityAttributesParent, 0);
                }
                else
                {
                    CreatePipeWithSecurityAttributes(out hTmp,
                                                          out childHandle,
                                                          ref securityAttributesParent,
                                                          0);
                }
                // Duplicate the parent handle to be non-inheritable so that the child process
                // doesn't have access. This is done for correctness sake, exact reason is unclear.
                // One potential theory is that child process can do something brain dead like
                // closing the parent end of the pipe and there by getting into a blocking situation
                // as parent will not be draining the pipe at the other end anymore.
                IntPtr currentProcHandle = Interop.Kernel32.GetCurrentProcess();
                if (!Interop.Kernel32.DuplicateHandle(currentProcHandle,
                                                     hTmp,
                                                     currentProcHandle,
                                                     out parentHandle,
                                                     0,
                                                     false,
                                                     Interop.Kernel32.HandleOptions.DUPLICATE_SAME_ACCESS))
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                if (hTmp != null && !hTmp.IsInvalid)
                {
                    hTmp.Dispose();
                }
            }
        }

        private static string GetEnvironmentVariablesBlock(IDictionary<string, string> sd)
        {
            // https://docs.microsoft.com/en-us/windows/win32/procthread/changing-environment-variables
            // "All strings in the environment block must be sorted alphabetically by name. The sort is
            //  case-insensitive, Unicode order, without regard to locale. Because the equal sign is a
            //  separator, it must not be used in the name of an environment variable."

            var keys = new string[sd.Count];
            sd.Keys.CopyTo(keys, 0);
            Array.Sort(keys, StringComparer.OrdinalIgnoreCase);

            // Join the null-terminated "key=val\0" strings
            var result = new StringBuilder(8 * keys.Length);
            foreach (string key in keys)
            {
                result.Append(key).Append('=').Append(sd[key]).Append('\0');
            }

            return result.ToString();
        }

        private static string GetErrorMessage(int error) => Interop.Kernel32.GetMessage(error);
    }
}
