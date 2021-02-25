// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    /// <summary>Provides access to local and remote processes and enables you to start and stop local system processes.</summary>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// [!INCLUDE[remarks](~/includes/remarks/System.Diagnostics/Process/Process.md)]
    /// ]]></format></remarks>
    /// <altmember cref="O:System.Diagnostics.Process.Start"/>
    /// <altmember cref="System.Diagnostics.ProcessStartInfo"/>
    /// <altmember cref="System.Diagnostics.Process.CloseMainWindow"/>
    /// <altmember cref="O:System.Diagnostics.Process.Kill"/>
    /// <altmember cref="System.Diagnostics.ProcessThread"/>
    /// <related type="ExternalDocumentation" href="https://code.msdn.microsoft.com/windowsdesktop/Using-the-NET-Process-Class-d70597ef">Using the .NET Process Class</related>
    public partial class Process : IDisposable
    {
        private bool _haveMainWindow;
        private IntPtr _mainWindowHandle;
        private string? _mainWindowTitle;

        private bool _haveResponding;
        private bool _responding;

        private bool StartCore(ProcessStartInfo startInfo)
        {
            return startInfo.UseShellExecute
                ? StartWithShellExecuteEx(startInfo)
                : StartWithCreateProcess(startInfo);
        }

        private unsafe bool StartWithShellExecuteEx(ProcessStartInfo startInfo)
        {
            if (!string.IsNullOrEmpty(startInfo.UserName) || startInfo.Password != null)
                throw new InvalidOperationException(SR.CantStartAsUser);

            if (startInfo.RedirectStandardInput || startInfo.RedirectStandardOutput || startInfo.RedirectStandardError)
                throw new InvalidOperationException(SR.CantRedirectStreams);

            if (startInfo.StandardInputEncoding != null)
                throw new InvalidOperationException(SR.StandardInputEncodingNotAllowed);

            if (startInfo.StandardErrorEncoding != null)
                throw new InvalidOperationException(SR.StandardErrorEncodingNotAllowed);

            if (startInfo.StandardOutputEncoding != null)
                throw new InvalidOperationException(SR.StandardOutputEncodingNotAllowed);

            if (startInfo._environmentVariables != null)
                throw new InvalidOperationException(SR.CantUseEnvVars);

            string arguments = startInfo.BuildArguments();

            fixed (char* fileName = startInfo.FileName.Length > 0 ? startInfo.FileName : null)
            fixed (char* verb = startInfo.Verb.Length > 0 ? startInfo.Verb : null)
            fixed (char* parameters = arguments.Length > 0 ? arguments : null)
            fixed (char* directory = startInfo.WorkingDirectory.Length > 0 ? startInfo.WorkingDirectory : null)
            {
                Interop.Shell32.SHELLEXECUTEINFO shellExecuteInfo = new Interop.Shell32.SHELLEXECUTEINFO()
                {
                    cbSize = (uint)sizeof(Interop.Shell32.SHELLEXECUTEINFO),
                    lpFile = fileName,
                    lpVerb = verb,
                    lpParameters = parameters,
                    lpDirectory = directory,
                    fMask = Interop.Shell32.SEE_MASK_NOCLOSEPROCESS | Interop.Shell32.SEE_MASK_FLAG_DDEWAIT
                };

                if (startInfo.ErrorDialog)
                    shellExecuteInfo.hwnd = startInfo.ErrorDialogParentHandle;
                else
                    shellExecuteInfo.fMask |= Interop.Shell32.SEE_MASK_FLAG_NO_UI;

                shellExecuteInfo.nShow = startInfo.WindowStyle switch
                {
                    ProcessWindowStyle.Hidden => Interop.Shell32.SW_HIDE,
                    ProcessWindowStyle.Minimized => Interop.Shell32.SW_SHOWMINIMIZED,
                    ProcessWindowStyle.Maximized => Interop.Shell32.SW_SHOWMAXIMIZED,
                    _ => Interop.Shell32.SW_SHOWNORMAL,
                };
                ShellExecuteHelper executeHelper = new ShellExecuteHelper(&shellExecuteInfo);
                if (!executeHelper.ShellExecuteOnSTAThread())
                {
                    int error = executeHelper.ErrorCode;
                    if (error == 0)
                    {
                        error = GetShellError(shellExecuteInfo.hInstApp);
                    }

                    switch (error)
                    {
                        case Interop.Errors.ERROR_BAD_EXE_FORMAT:
                        case Interop.Errors.ERROR_EXE_MACHINE_TYPE_MISMATCH:
                            throw new Win32Exception(error, SR.InvalidApplication);
                        case Interop.Errors.ERROR_CALL_NOT_IMPLEMENTED:
                            // This happens on Windows Nano
                            throw new PlatformNotSupportedException(SR.UseShellExecuteNotSupported);
                        default:
                            throw new Win32Exception(error);
                    }
                }

                if (shellExecuteInfo.hProcess != IntPtr.Zero)
                {
                    SetProcessHandle(new SafeProcessHandle(shellExecuteInfo.hProcess));
                    return true;
                }
            }

            return false;
        }

        private int GetShellError(IntPtr error)
        {
            switch ((long)error)
            {
                case Interop.Shell32.SE_ERR_FNF:
                    return Interop.Errors.ERROR_FILE_NOT_FOUND;
                case Interop.Shell32.SE_ERR_PNF:
                    return Interop.Errors.ERROR_PATH_NOT_FOUND;
                case Interop.Shell32.SE_ERR_ACCESSDENIED:
                    return Interop.Errors.ERROR_ACCESS_DENIED;
                case Interop.Shell32.SE_ERR_OOM:
                    return Interop.Errors.ERROR_NOT_ENOUGH_MEMORY;
                case Interop.Shell32.SE_ERR_DDEFAIL:
                case Interop.Shell32.SE_ERR_DDEBUSY:
                case Interop.Shell32.SE_ERR_DDETIMEOUT:
                    return Interop.Errors.ERROR_DDE_FAIL;
                case Interop.Shell32.SE_ERR_SHARE:
                    return Interop.Errors.ERROR_SHARING_VIOLATION;
                case Interop.Shell32.SE_ERR_NOASSOC:
                    return Interop.Errors.ERROR_NO_ASSOCIATION;
                case Interop.Shell32.SE_ERR_DLLNOTFOUND:
                    return Interop.Errors.ERROR_DLL_NOT_FOUND;
                default:
                    return (int)(long)error;
            }
        }

        internal unsafe class ShellExecuteHelper
        {
            private readonly Interop.Shell32.SHELLEXECUTEINFO* _executeInfo;
            private bool _succeeded;
            private bool _notpresent;

            public ShellExecuteHelper(Interop.Shell32.SHELLEXECUTEINFO* executeInfo)
            {
                _executeInfo = executeInfo;
            }

            private void ShellExecuteFunction()
            {
                try
                {
                    if (!(_succeeded = Interop.Shell32.ShellExecuteExW(_executeInfo)))
                        ErrorCode = Marshal.GetLastWin32Error();
                }
                catch (EntryPointNotFoundException)
                {
                    _notpresent = true;
                }
            }

            public bool ShellExecuteOnSTAThread()
            {
                // ShellExecute() requires STA in order to work correctly.

                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    ThreadStart threadStart = new ThreadStart(ShellExecuteFunction);
                    Thread executionThread = new Thread(threadStart)
                    {
                        IsBackground = true,
                        Name = ".NET Process STA"
                    };
                    executionThread.SetApartmentState(ApartmentState.STA);
                    executionThread.Start();
                    executionThread.Join();
                }
                else
                {
                    ShellExecuteFunction();
                }

                if (_notpresent)
                    throw new PlatformNotSupportedException(SR.UseShellExecuteNotSupported);

                return _succeeded;
            }

            public int ErrorCode { get; private set; }
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
                Debug.Assert(error == 0, $"Failed GetWindowTextLengthW(): { new Win32Exception(error).Message }");
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
                Debug.Assert(error == 0, $"Failed GetWindowTextW(): { new Win32Exception(error).Message }");
            }
#endif
            return title.Slice(0, length).ToString();
        }

        /// <summary>Gets the window handle of the main window of the associated process.</summary>
        /// <value>The system-generated window handle of the main window of the associated process.</value>
        /// <remarks>The main window is the window opened by the process that currently has the focus (the <see langword="System.Windows.Forms.Form.TopLevel" /> form). You must use the <see cref="System.Diagnostics.Process.Refresh" /> method to refresh the <see cref="System.Diagnostics.Process" /> object to get the most up to date main window handle if it has changed. In general, because the window handle is cached, use <see cref="System.Diagnostics.Process.Refresh" /> beforehand to guarantee that you'll retrieve the current handle.
        /// You can get the <see cref="System.Diagnostics.Process.MainWindowHandle" /> property only for processes that are running on the local computer. The <see cref="System.Diagnostics.Process.MainWindowHandle" /> property is a value that uniquely identifies the window that is associated with the process.
        /// A process has a main window associated with it only if the process has a graphical interface. If the associated process does not have a main window, the <see cref="System.Diagnostics.Process.MainWindowHandle" /> value is zero. The value is also zero for processes that have been hidden, that is, processes that are not visible in the taskbar. This can be the case for processes that appear as icons in the notification area, at the far right of the taskbar.
        /// If you have just started a process and want to use its main window handle, consider using the <see cref="O:System.Diagnostics.Process.WaitForInputIdle" /> method to allow the process to finish starting, ensuring that the main window handle has been created. Otherwise, an exception will be thrown.</remarks>
        /// <exception cref="System.InvalidOperationException">The <see cref="System.Diagnostics.Process.MainWindowHandle" /> is not defined because the process has exited.</exception>
        /// <exception cref="System.NotSupportedException">You are trying to access the <see cref="System.Diagnostics.Process.MainWindowHandle" /> property for a process that is running on a remote computer. This property is available only for processes that are running on the local computer.</exception>
        /// <altmember cref="System.Diagnostics.Process.MainWindowTitle"/>
        /// <altmember cref="System.Diagnostics.Process.MainModule"/>
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

        /// <summary>Gets the caption of the main window of the process.</summary>
        /// <value>The main window title of the process.</value>
        /// <remarks>A process has a main window associated with it only if the process has a graphical interface. If the associated process does not have a main window (so that <see cref="System.Diagnostics.Process.MainWindowHandle" /> is zero), or if the system can't determine that there's a main window (such as may be the case on some Unix platforms), <see cref="System.Diagnostics.Process.MainWindowTitle" /> is an empty string ("").
        /// If you have just started a process and want to use its main window title, consider using the <see cref="O:System.Diagnostics.Process.WaitForInputIdle" /> method to allow the process to finish starting, ensuring that the main window handle has been created. Otherwise, the system throws an exception.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  The main window is the window that currently has the focus; note that this might not be the primary window for the process. You must use the <xref:System.Diagnostics.Process.Refresh%2A> method to refresh the <xref:System.Diagnostics.Process> object to get the most up to date main window handle if it has changed.
        /// ]]></format></remarks>
        /// <example>The following example starts an instance of Notepad and retrieves the caption of the main window of the process.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-cpp[process_MainWindowTitle#1](~/samples/snippets/cpp/VS_Snippets_CLR/Process_MainWindowTitle/CPP/process_mainwindowtitle.cpp#1)]
        /// [!code-csharp[process_MainWindowTitle#1](~/samples/snippets/csharp/VS_Snippets_CLR/Process_MainWindowTitle/CS/process_mainwindowtitle.cs#1)]
        /// [!code-vb[process_MainWindowTitle#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Process_MainWindowTitle/VB/process_mainwindowtitle.vb#1)]
        /// ]]></format></example>
        /// <exception cref="System.InvalidOperationException">The <see cref="System.Diagnostics.Process.MainWindowTitle" /> property is not defined because the process has exited.</exception>
        /// <exception cref="System.NotSupportedException">You are trying to access the <see cref="System.Diagnostics.Process.MainWindowTitle" /> property for a process that is running on a remote computer. This property is available only for processes that are running on the local computer.</exception>
        public string MainWindowTitle
        {
            get
            {
                if (_mainWindowTitle == null)
                {
                    _mainWindowTitle = GetMainWindowTitle();
                }

                return _mainWindowTitle;
            }
        }

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
            return Interop.User32.SendMessageTimeout(mainWindow, WM_NULL, IntPtr.Zero, IntPtr.Zero, SMTO_ABORTIFHUNG, 5000, out result) != (IntPtr)0;
        }

        /// <summary>Gets a value indicating whether the user interface of the process is responding.</summary>
        /// <value><see langword="true" /> if the user interface of the associated process is responding to the system; otherwise, <see langword="false" />.</value>
        /// <remarks>The value returned by this property represents the most recently refreshed status. To get the most up to date status, you need to call <see cref="System.Diagnostics.Process.Refresh" /> method first.
        /// If a process has a user interface, the <see cref="System.Diagnostics.Process.Responding" /> property contacts the user interface to determine whether the process is responding to user input. If the interface does not respond immediately, the <see cref="System.Diagnostics.Process.Responding" /> property returns <see langword="false" />. Use this property to determine whether the interface of the associated process has stopped responding.
        /// If the process does not have a <see cref="System.Diagnostics.Process.MainWindowHandle" />, this property returns <see langword="true" />.</remarks>
        /// <example>The following example starts an instance of Notepad. The example then retrieves and displays various properties of the associated process. The example detects when the process exits, and displays the process's exit code.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-cpp[Diag_Process_MemoryProperties64#1](~/samples/snippets/cpp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CPP/source.cpp#1)]
        /// [!code-csharp[Diag_Process_MemoryProperties64#1](~/samples/snippets/csharp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CS/source.cs#1)]
        /// [!code-vb[Diag_Process_MemoryProperties64#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Diag_Process_MemoryProperties64/VB/source.vb#1)]
        /// ]]></format></example>
        /// <exception cref="System.InvalidOperationException">There is no process associated with this <see cref="System.Diagnostics.Process" /> object.</exception>
        /// <exception cref="System.NotSupportedException">You are attempting to access the <see cref="System.Diagnostics.Process.Responding" /> property for a process that is running on a remote computer. This property is available only for processes that are running on the local computer.</exception>
        /// <altmember cref="System.Diagnostics.Process.MainWindowHandle"/>
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
                        throw new InvalidOperationException(SR.InputIdleUnkownError);
                }
            }
            return idle;
        }

        /// <summary>Checks whether the argument is a direct child of this process.</summary>
        /// <remarks>
        /// A child process is a process which has this process's id as its parent process id and which started after this process did.
        /// </remarks>
        private bool IsParentOf(Process possibleChild) =>
            StartTime < possibleChild.StartTime
            && Id == possibleChild.ParentProcessId;

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

        private bool Equals(Process process) =>
            Id == process.Id
            && StartTime == process.StartTime;

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

            List<(Process Process, SafeProcessHandle Handle)> children = GetProcessHandlePairs(p => SafePredicateTest(() => IsParentOf(p)));
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

        private List<(Process Process, SafeProcessHandle Handle)> GetProcessHandlePairs(Func<Process, bool> predicate)
        {
            var results = new List<(Process Process, SafeProcessHandle Handle)>();

            foreach (Process p in GetProcesses())
            {
                SafeProcessHandle h = SafeGetHandle(p);
                if (!h.IsInvalid)
                {
                    if (predicate(p))
                    {
                        results.Add((p, h));
                    }
                    else
                    {
                        p.Dispose();
                        h.Dispose();
                    }
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
