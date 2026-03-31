// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
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
