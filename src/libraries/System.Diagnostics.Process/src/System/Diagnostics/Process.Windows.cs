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
        private string? _processName;

        /// <summary>
        /// Creates an array of <see cref="Process"/> components that are associated with process resources on a
        /// remote computer. These process resources share the specified process name.
        /// </summary>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static Process[] GetProcessesByName(string? processName, string machineName)
        {
            bool isRemoteMachine = ProcessManager.IsRemoteMachine(machineName);

            ProcessInfo[] processInfos = ProcessManager.GetProcessInfos(processName, machineName);
            Process[] processes = new Process[processInfos.Length];

            for (int i = 0; i < processInfos.Length; i++)
            {
                ProcessInfo processInfo = processInfos[i];
                processes[i] = new Process(machineName, isRemoteMachine, processInfo.ProcessId, processInfo);
            }

            return processes;
        }

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
            _processName = null;
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

        private bool _signaled;

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

        /// <summary>Gets the friendly name of the process.</summary>
        public string ProcessName
        {
            get
            {
                if (_processName == null)
                {
                    // If we already have the name via a populated ProcessInfo
                    // then use that one.
                    if (_processInfo?.ProcessName != null)
                    {
                        _processName = _processInfo!.ProcessName;
                    }
                    else
                    {
                        // Ensure that the process is not yet exited
                        EnsureState(State.HaveNonExitedId);
                        _processName = ProcessManager.GetProcessName(_processId, _machineName);

                        // Fallback to slower ProcessInfo implementation if optimized way did not return a
                        // process name (e.g. in case of missing permissions for Non-Admin users)
                        if (_processName == null)
                        {
                            EnsureState(State.HaveProcessInfo);
                            _processName = _processInfo!.ProcessName;
                        }
                    }
                }

                return _processName;
            }
        }
    }
}
