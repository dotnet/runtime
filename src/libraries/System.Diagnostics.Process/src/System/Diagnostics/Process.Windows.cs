// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    public partial class Process : IDisposable
    {
        private bool _haveMainWindow;
        private IntPtr _mainWindowHandle;
        private string? _mainWindowTitle;

        private bool _haveResponding;
        private bool _responding;

        private bool _signaled;

        [CLSCompliant(false)]
        [SupportedOSPlatform("windows")]
        public static Process? Start(string fileName, string userName, SecureString password, string domain)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(fileName);
            startInfo.UserName = userName;
            startInfo.Password = password;
            startInfo.Domain = domain;
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
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public void Kill()
        {
            using (SafeProcessHandle handle = GetProcessHandle(Interop.Advapi32.ProcessOptions.PROCESS_TERMINATE | Interop.Advapi32.ProcessOptions.PROCESS_QUERY_LIMITED_INFORMATION, throwIfExited: false))
            {
                // If the process has exited, the handle is invalid.
                if (handle.IsInvalid)
                    return;

                handle.Kill();
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
        partial void CloseCore();

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
                    return;
                }

                if (!ProcessManager.HasExited(handle, ref _signaled, out int localExitCode))
                    return;

                _exited = true;
                _exitCode = localExitCode;
            }
        }

        /// <summary>Gets the time that the associated process exited.</summary>
        private DateTime ExitTimeCore
        {
            get { return GetProcessTimes().ExitTime; }
        }

        /// <summary>Gets the amount of time the process has spent running code inside the operating system core.</summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan PrivilegedProcessorTime
        {
            get => IsCurrentProcess ? Environment.CpuUsage.PrivilegedTime : GetProcessTimes().PrivilegedProcessorTime;
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
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan TotalProcessorTime
        {
            get => IsCurrentProcess ? Environment.CpuUsage.TotalTime : GetProcessTimes().TotalProcessorTime;
        }

        /// <summary>
        /// Gets the amount of time the associated process has spent running code
        /// inside the application portion of the process (not the operating system core).
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public TimeSpan UserProcessorTime
        {
            get => IsCurrentProcess ? Environment.CpuUsage.UserTime : GetProcessTimes().UserProcessorTime;
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

        private static ConsoleEncoding GetEncoding(int codePage)
        {
            Encoding enc = EncodingHelper.GetSupportedConsoleEncoding(codePage);
            return new ConsoleEncoding(enc); // ensure encoding doesn't output a preamble
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
                hToken?.Dispose();
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

        private static FileStream OpenStream(SafeFileHandle handle, FileAccess access) => new(handle, access, StreamBufferSize, handle.IsAsync);

        private static ConsoleEncoding GetStandardInputEncoding() => GetEncoding((int)Interop.Kernel32.GetConsoleCP());

        private static ConsoleEncoding GetStandardOutputEncoding() => GetEncoding((int)Interop.Kernel32.GetConsoleOutputCP());

        private bool StartCore(ProcessStartInfo startInfo, SafeFileHandle? stdinHandle, SafeFileHandle? stdoutHandle, SafeFileHandle? stderrHandle)
        {
            SafeProcessHandle startedProcess = SafeProcessHandle.StartCore(startInfo, stdinHandle, stdoutHandle, stderrHandle);

            if (startedProcess.IsInvalid)
            {
                Debug.Assert(startInfo.UseShellExecute);
                return false;
            }

            SetProcessHandle(startedProcess);
            if (!startInfo.UseShellExecute)
            {
                SetProcessId(startedProcess.ProcessId);
            }
            return true;
        }

        private string GetMainWindowTitle()
        {
            IntPtr handle = MainWindowHandle;
            if (handle == IntPtr.Zero)
                return string.Empty;

            int length = Interop.User32.GetWindowTextLengthW(handle);

            if (length == 0)
            {
#if DEBUG
                // We never used to throw here, want to surface possible mistakes on our part
                int error = Marshal.GetLastWin32Error();
                Debug.Assert(error == 0, $"Failed GetWindowTextLengthW(): {Marshal.GetPInvokeErrorMessage(error)}");
#endif
                return string.Empty;
            }

            length++; // for null terminator, which GetWindowTextLengthW does not include in the length
            Span<char> title = length <= 256 ? stackalloc char[256] : new char[length];
            unsafe
            {
                fixed (char* titlePtr = title)
                {
                    length = Interop.User32.GetWindowTextW(handle, titlePtr, title.Length); // returned length does not include null terminator
                }
            }
#if DEBUG
            if (length == 0)
            {
                // We never used to throw here, want to surface possible mistakes on our part
                int error = Marshal.GetLastWin32Error();
                Debug.Assert(error == 0, $"Failed GetWindowTextW(): {Marshal.GetPInvokeErrorMessage(error)}");
            }
#endif
            return title.Slice(0, length).ToString();
        }

        public IntPtr MainWindowHandle
        {
            get
            {
                if (!_haveMainWindow)
                {
                    EnsureState(State.IsLocal | State.HaveId);
                    _mainWindowHandle = ProcessManager.GetMainWindowHandle(_processId);

                    _haveMainWindow = _mainWindowHandle != IntPtr.Zero;
                }
                return _mainWindowHandle;
            }
        }

        private bool CloseMainWindowCore()
        {
            const int GWL_STYLE = -16; // Retrieves the window styles.
            const int WS_DISABLED = 0x08000000; // WindowStyle disabled. A disabled window cannot receive input from the user.
            const int WM_CLOSE = 0x0010; // WindowMessage close.

            IntPtr mainWindowHandle = MainWindowHandle;
            if (mainWindowHandle == (IntPtr)0)
            {
                return false;
            }

            int style = Interop.User32.GetWindowLong(mainWindowHandle, GWL_STYLE);
            if ((style & WS_DISABLED) != 0)
            {
                return false;
            }

            Interop.User32.PostMessageW(mainWindowHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            return true;
        }

        public string MainWindowTitle => _mainWindowTitle ??= GetMainWindowTitle();

        private bool IsRespondingCore()
        {
            const int WM_NULL = 0x0000;
            const int SMTO_ABORTIFHUNG = 0x0002;

            IntPtr mainWindow = MainWindowHandle;
            if (mainWindow == (IntPtr)0)
            {
                return true;
            }

            IntPtr result;
            unsafe
            {
                return Interop.User32.SendMessageTimeout(mainWindow, WM_NULL, IntPtr.Zero, IntPtr.Zero, SMTO_ABORTIFHUNG, 5000, &result) != (IntPtr)0;
            }
        }

        public bool Responding
        {
            get
            {
                if (!_haveResponding)
                {
                    _responding = IsRespondingCore();
                    _haveResponding = true;
                }

                return _responding;
            }
        }

        private bool WaitForInputIdleCore(int milliseconds)
        {
            bool idle;
            using (SafeProcessHandle handle = GetProcessHandle(Interop.Advapi32.ProcessOptions.SYNCHRONIZE | Interop.Advapi32.ProcessOptions.PROCESS_QUERY_INFORMATION))
            {
                int ret = Interop.User32.WaitForInputIdle(handle, milliseconds);
                switch (ret)
                {
                    case Interop.Kernel32.WAIT_OBJECT_0:
                        idle = true;
                        break;
                    case Interop.Kernel32.WAIT_TIMEOUT:
                        idle = false;
                        break;
                    default:
                        throw new InvalidOperationException(SR.InputIdleUnknownError);
                }
            }
            return idle;
        }

        /// <summary>Checks whether the argument is a direct child of this process.</summary>
        /// <remarks>
        /// A child process is a process which has this process's id as its parent process id and which started after this process did.
        /// </remarks>
        private bool IsParentOf(Process possibleChild)
        {
            // Use non-throwing helpers to avoid first-chance exceptions during enumeration.
            // This is critical for performance when a debugger is attached.
            if (!TryGetStartTime(out DateTime myStartTime) ||
                !possibleChild.TryGetStartTime(out DateTime childStartTime) ||
                !possibleChild.TryGetParentProcessId(out int childParentId))
            {
                return false;
            }

            return myStartTime < childStartTime && Id == childParentId;
        }

        /// <summary>
        /// Try to get the process start time without throwing exceptions.
        /// </summary>
        private bool TryGetStartTime(out DateTime startTime)
        {
            startTime = default;
            using (SafeProcessHandle handle = GetProcessHandle(Interop.Advapi32.ProcessOptions.PROCESS_QUERY_LIMITED_INFORMATION, false))
            {
                if (handle.IsInvalid)
                {
                    return false;
                }

                ProcessThreadTimes processTimes = new ProcessThreadTimes();
                if (!Interop.Kernel32.GetProcessTimes(handle,
                    out processTimes._create, out processTimes._exit,
                    out processTimes._kernel, out processTimes._user))
                {
                    return false;
                }

                startTime = processTimes.StartTime;
                return true;
            }
        }

        /// <summary>
        /// Try to get the parent process ID without throwing exceptions.
        /// </summary>
        private unsafe bool TryGetParentProcessId(out int parentProcessId)
        {
            parentProcessId = 0;
            using (SafeProcessHandle handle = GetProcessHandle(Interop.Advapi32.ProcessOptions.PROCESS_QUERY_LIMITED_INFORMATION, false))
            {
                if (handle.IsInvalid)
                {
                    return false;
                }

                Interop.NtDll.PROCESS_BASIC_INFORMATION info;
                if (Interop.NtDll.NtQueryInformationProcess(handle, Interop.NtDll.ProcessBasicInformation, &info, (uint)sizeof(Interop.NtDll.PROCESS_BASIC_INFORMATION), out _) != 0)
                {
                    return false;
                }

                parentProcessId = (int)info.InheritedFromUniqueProcessId;
                return true;
            }
        }

        /// <summary>
        /// Get the process's parent process id.
        /// </summary>
        private unsafe int ParentProcessId
        {
            get
            {
                using (SafeProcessHandle handle = GetProcessHandle(Interop.Advapi32.ProcessOptions.PROCESS_QUERY_INFORMATION))
                {
                    Interop.NtDll.PROCESS_BASIC_INFORMATION info;

                    if (Interop.NtDll.NtQueryInformationProcess(handle, Interop.NtDll.ProcessBasicInformation, &info, (uint)sizeof(Interop.NtDll.PROCESS_BASIC_INFORMATION), out _) != 0)
                        throw new Win32Exception(SR.ProcessInformationUnavailable);

                    return (int)info.InheritedFromUniqueProcessId;
                }
            }
        }

        private bool Equals(Process process)
        {
            // Check IDs first since they're cheap to compare and will fail most of the time.
            if (Id != process.Id)
            {
                return false;
            }

            // Use non-throwing helper to avoid first-chance exceptions during enumeration.
            if (!TryGetStartTime(out DateTime myStartTime) ||
                !process.TryGetStartTime(out DateTime otherStartTime))
            {
                return false;
            }

            return myStartTime == otherStartTime;
        }

        private List<Exception>? KillTree()
        {
            // The process's structures will be preserved as long as a handle is held pointing to them, even if the process exits or
            // is terminated. A handle is held here to ensure a stable reference to the process during execution.
            using (SafeProcessHandle handle = GetProcessHandle(Interop.Advapi32.ProcessOptions.PROCESS_QUERY_LIMITED_INFORMATION, throwIfExited: false))
            {
                // If the process has exited, the handle is invalid.
                if (handle.IsInvalid)
                    return null;

                return KillTree(handle);
            }
        }

        private List<Exception>? KillTree(SafeProcessHandle handle)
        {
            Debug.Assert(!handle.IsInvalid);

            List<Exception>? exceptions = null;

            try
            {
                // Kill the process, so that no further children can be created.
                //
                // This method can return before stopping has completed. Down the road, could possibly wait for termination to complete before continuing.
                Kill();
            }
            catch (Win32Exception e)
            {
                (exceptions ??= new List<Exception>()).Add(e);
            }

            List<(Process Process, SafeProcessHandle Handle)> children = GetProcessHandlePairs((thisProcess, otherProcess) => thisProcess.IsParentOf(otherProcess));
            try
            {
                foreach ((Process Process, SafeProcessHandle Handle) child in children)
                {
                    List<Exception>? exceptionsFromChild = child.Process.KillTree(child.Handle);
                    if (exceptionsFromChild != null)
                    {
                        (exceptions ??= new List<Exception>()).AddRange(exceptionsFromChild);
                    }
                }
            }
            finally
            {
                foreach ((Process Process, SafeProcessHandle Handle) child in children)
                {
                    child.Process.Dispose();
                    child.Handle.Dispose();
                }
            }

            return exceptions;
        }

        private List<(Process Process, SafeProcessHandle Handle)> GetProcessHandlePairs(Func<Process, Process, bool> predicate)
        {
            var results = new List<(Process Process, SafeProcessHandle Handle)>();

            foreach (Process p in GetProcesses())
            {
                SafeProcessHandle h = SafeGetHandle(p);
                if (!h.IsInvalid && predicate(this, p))
                {
                    results.Add((p, h));
                }
                else
                {
                    p.Dispose();
                    h.Dispose();
                }
            }

            return results;

            static SafeProcessHandle SafeGetHandle(Process process)
            {
                try
                {
                    return process.GetProcessHandle(Interop.Advapi32.ProcessOptions.PROCESS_QUERY_LIMITED_INFORMATION, false);
                }
                catch (Win32Exception)
                {
                    return SafeProcessHandle.InvalidHandle;
                }
            }
        }
    }
}
