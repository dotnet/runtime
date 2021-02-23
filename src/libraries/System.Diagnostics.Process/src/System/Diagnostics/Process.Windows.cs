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
        private static readonly object s_createProcessLock = new object();

        /// <summary>Creates an array of new <see cref="System.Diagnostics.Process" /> components and associates them with all the process resources on a remote computer that share the specified process name.</summary>
        /// <param name="processName">The friendly name of the process.</param>
        /// <param name="machineName">The name of a computer on the network.</param>
        /// <returns>An array of type <see cref="System.Diagnostics.Process" /> that represents the process resources running the specified application or file.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// [!INCLUDE[remarks](~/includes/remarks/System.Diagnostics/Process/GetProcessesByName_String_String.md)]
        /// ]]></format></remarks>
        /// <exception cref="System.ArgumentException">The <paramref name="machineName" /> parameter syntax is invalid. It might have length zero (0).</exception>
        /// <exception cref="System.ArgumentNullException">The <paramref name="machineName" /> parameter is <see langword="null" />.</exception>
        /// <exception cref="System.PlatformNotSupportedException">The operating system platform does not support this operation on remote computers.</exception>
        /// <exception cref="System.InvalidOperationException">The attempt to connect to <paramref name="machineName" /> has failed.
        /// -or-
        /// There are problems accessing the performance counter APIs used to get process information. This exception is specific to Windows NT, Windows 2000, and Windows XP.</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">A problem occurred accessing an underlying system API.</exception>
        /// <altmember cref="System.Diagnostics.Process.ProcessName"/>
        /// <altmember cref="System.Diagnostics.Process.MachineName"/>
        /// <altmember cref="System.Diagnostics.Process.GetProcessById(int,string)"/>
        /// <altmember cref="O:System.Diagnostics.Process.GetProcesses"/>
        /// <altmember cref="System.Diagnostics.Process.GetCurrentProcess"/>
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

        /// <summary>Starts a process resource by specifying the name of an application, a user name, a password, and a domain and associates the resource with a new <see cref="System.Diagnostics.Process" /> component.</summary>
        /// <param name="fileName">The name of an application file to run in the process.</param>
        /// <param name="userName">The user name to use when starting the process.</param>
        /// <param name="password">A <see cref="System.Security.SecureString" /> that contains the password to use when starting the process.</param>
        /// <param name="domain">The domain to use when starting the process.</param>
        /// <returns>A new <see cref="System.Diagnostics.Process" /> that is associated with the process resource, or <see langword="null" /> if no process resource is started. Note that a new process that's started alongside already running instances of the same process will be independent from the others. In addition, Start may return a non-null Process with its <see cref="System.Diagnostics.Process.HasExited" /> property already set to <see langword="true" />. In this case, the started process may have activated an existing instance of itself and then exited.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// [!INCLUDE[remarks](~/includes/remarks/System.Diagnostics/Process/Start_String_String_SecureString_String.md)]
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">No file name was specified.</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">There was an error in opening the associated file.
        /// -or-
        /// The file specified in the <paramref name="fileName" /> could not be found.</exception>
        /// <exception cref="System.ObjectDisposedException">The process object has already been disposed.</exception>
        /// <exception cref="System.PlatformNotSupportedException">This member is not supported on Linux or macOS (.NET Core only).</exception>
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

        /// <summary>Starts a process resource by specifying the name of an application, a set of command-line arguments, a user name, a password, and a domain and associates the resource with a new <see cref="System.Diagnostics.Process" /> component.</summary>
        /// <param name="fileName">The name of an application file to run in the process.</param>
        /// <param name="arguments">Command-line arguments to pass when starting the process.</param>
        /// <param name="userName">The user name to use when starting the process.</param>
        /// <param name="password">A <see cref="System.Security.SecureString" /> that contains the password to use when starting the process.</param>
        /// <param name="domain">The domain to use when starting the process.</param>
        /// <returns>A new <see cref="System.Diagnostics.Process" /> that is associated with the process resource, or <see langword="null" /> if no process resource is started. Note that a new process that's started alongside already running instances of the same process will be independent from the others. In addition, Start may return a non-null Process with its <see cref="System.Diagnostics.Process.HasExited" /> property already set to <see langword="true" />. In this case, the started process may have activated an existing instance of itself and then exited.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// [!INCLUDE[remarks](~/includes/remarks/System.Diagnostics/Process/Start_String_String_String_SecureString_String.md)]
        /// ]]></format></remarks>
        /// <exception cref="System.InvalidOperationException">No file name was specified.</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">An error occurred when opening the associated file.
        /// -or-
        /// The file specified in the <paramref name="fileName" /> could not be found.
        /// -or-
        /// The sum of the length of the arguments and the length of the full path to the associated file exceeds 2080. The error message associated with this exception can be one of the following: "The data area passed to a system call is too small." or "Access is denied."</exception>
        /// <exception cref="System.ObjectDisposedException">The process object has already been disposed.</exception>
        /// <exception cref="System.PlatformNotSupportedException">This member is not supported on Linux or macOS (.NET Core only).</exception>
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

        /// <summary>Puts a <see cref="System.Diagnostics.Process" /> component in state to interact with operating system processes that run in a special mode by enabling the native property <see langword="SeDebugPrivilege" /> on the current thread.</summary>
        /// <remarks>Some operating system processes run in a special mode. Attempting to read properties of or attach to these processes is not possible unless you have called <see cref="System.Diagnostics.Process.EnterDebugMode" /> on the component. Call <see cref="System.Diagnostics.Process.LeaveDebugMode" /> when you no longer need access to these processes that run in special mode.</remarks>
        /// <altmember cref="System.Diagnostics.Process.LeaveDebugMode"/>
        public static void EnterDebugMode()
        {
            SetPrivilege(Interop.Advapi32.SeDebugPrivilege, (int)Interop.Advapi32.SEPrivileges.SE_PRIVILEGE_ENABLED);
        }

        /// <summary>Takes a <see cref="System.Diagnostics.Process" /> component out of the state that lets it interact with operating system processes that run in a special mode.</summary>
        /// <remarks>Some operating system processes run in a special mode. Attempting to read properties of or attach to these processes is not possible unless you have called <see cref="System.Diagnostics.Process.EnterDebugMode" /> on the component. Call <see cref="System.Diagnostics.Process.LeaveDebugMode" /> when you no longer need access to these processes that run in special mode.</remarks>
        /// <altmember cref="System.Diagnostics.Process.EnterDebugMode"/>
        public static void LeaveDebugMode()
        {
            SetPrivilege(Interop.Advapi32.SeDebugPrivilege, 0);
        }

        /// <summary>Immediately stops the associated process.</summary>
        /// <exception cref="System.ComponentModel.Win32Exception">The associated process could not be terminated.</exception>
        /// <exception cref="System.NotSupportedException">You are attempting to call <see cref="O:System.Diagnostics.Process.Kill" /> for a process that is running on a remote computer. The method is available only for processes running on the local computer.</exception>
        /// <exception cref="System.InvalidOperationException">There is no process associated with this <see cref="System.Diagnostics.Process" /> object.</exception>
        /// <altmember cref="System.Environment.Exit(int)"/>
        /// <altmember cref="System.Diagnostics.Process.CloseMainWindow"/>
        /// <altmember cref="O:System.Diagnostics.Process.Start"/>
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
                if (_output != null && milliseconds == Timeout.Infinite)
                    _output.WaitUntilEOF();

                if (_error != null && milliseconds == Timeout.Infinite)
                    _error.WaitUntilEOF();

                handle?.Dispose();
            }
        }

        /// <summary>Gets the main module for the associated process.</summary>
        /// <value>The <see cref="System.Diagnostics.ProcessModule" /> that was used to start the process.</value>
        /// <remarks>A process module represents a.dll or .exe file that is loaded into a particular process. The <see cref="System.Diagnostics.Process.MainModule" /> property lets you view information about the executable used to start the process, including the module name, file name, and module memory details.</remarks>
        /// <exception cref="System.NotSupportedException">You are trying to access the <see cref="System.Diagnostics.Process.MainModule" /> property for a process that is running on a remote computer. This property is available only for processes that are running on the local computer.</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">A 32-bit process is trying to access the modules of a 64-bit process.</exception>
        /// <exception cref="System.InvalidOperationException">The process <see cref="System.Diagnostics.Process.Id" /> is not available.
        /// -or-
        /// The process has exited.</exception>
        /// <altmember cref="System.Diagnostics.Process.Modules"/>
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

        /// <summary>Gets the privileged processor time for this process.</summary>
        /// <value>A <see cref="System.TimeSpan" /> that indicates the amount of time that the process has spent running code inside the operating system core.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// ## Examples
        /// The following example starts an instance of Notepad. The example then retrieves and displays various properties of the associated process. The example detects when the process exits, and displays the process's exit code.
        /// [!code-cpp[Diag_Process_MemoryProperties64#1](~/samples/snippets/cpp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CPP/source.cpp#1)]
        /// [!code-csharp[Diag_Process_MemoryProperties64#1](~/samples/snippets/csharp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CS/source.cs#1)]
        /// [!code-vb[Diag_Process_MemoryProperties64#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Diag_Process_MemoryProperties64/VB/source.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.NotSupportedException">You are attempting to access the <see cref="System.Diagnostics.Process.PrivilegedProcessorTime" /> property for a process that is running on a remote computer. This property is available only for processes that are running on the local computer.</exception>
        /// <altmember cref="System.Diagnostics.Process.UserProcessorTime"/>
        /// <altmember cref="System.Diagnostics.Process.PrivilegedProcessorTime"/>
        public TimeSpan PrivilegedProcessorTime
        {
            get { return GetProcessTimes().PrivilegedProcessorTime; }
        }

        /// <summary>Gets the time the associated process was started.</summary>
        internal DateTime StartTimeCore
        {
            get { return GetProcessTimes().StartTime; }
        }

        /// <summary>Gets the total processor time for this process.</summary>
        /// <value>A <see cref="System.TimeSpan" /> that indicates the amount of time that the associated process has spent utilizing the CPU. This value is the sum of the <see cref="System.Diagnostics.Process.UserProcessorTime" /> and the <see cref="System.Diagnostics.Process.PrivilegedProcessorTime" />.</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// ## Examples
        /// The following example starts an instance of Notepad. The example then retrieves and displays various properties of the associated process. The example detects when the process exits, and displays the process's exit code.
        /// [!code-cpp[Diag_Process_MemoryProperties64#1](~/samples/snippets/cpp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CPP/source.cpp#1)]
        /// [!code-csharp[Diag_Process_MemoryProperties64#1](~/samples/snippets/csharp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CS/source.cs#1)]
        /// [!code-vb[Diag_Process_MemoryProperties64#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Diag_Process_MemoryProperties64/VB/source.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.NotSupportedException">You are attempting to access the <see cref="System.Diagnostics.Process.TotalProcessorTime" /> property for a process that is running on a remote computer. This property is available only for processes that are running on the local computer.</exception>
        /// <altmember cref="System.Diagnostics.Process.UserProcessorTime"/>
        /// <altmember cref="System.Diagnostics.Process.PrivilegedProcessorTime"/>
        public TimeSpan TotalProcessorTime
        {
            get { return GetProcessTimes().TotalProcessorTime; }
        }

        /// <summary>Gets the user processor time for this process.</summary>
        /// <value>A <see cref="System.TimeSpan" /> that indicates the amount of time that the associated process has spent running code inside the application portion of the process (not inside the operating system core).</value>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// ## Examples
        /// The following example starts an instance of Notepad. The example then retrieves and displays various properties of the associated process. The example detects when the process exits, and displays the process's exit code.
        /// [!code-cpp[Diag_Process_MemoryProperties64#1](~/samples/snippets/cpp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CPP/source.cpp#1)]
        /// [!code-csharp[Diag_Process_MemoryProperties64#1](~/samples/snippets/csharp/VS_Snippets_CLR/Diag_Process_MemoryProperties64/CS/source.cs#1)]
        /// [!code-vb[Diag_Process_MemoryProperties64#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/Diag_Process_MemoryProperties64/VB/source.vb#1)]
        /// ]]></format></remarks>
        /// <exception cref="System.NotSupportedException">You are attempting to access the <see cref="System.Diagnostics.Process.UserProcessorTime" /> property for a process that is running on a remote computer. This property is available only for processes that are running on the local computer.</exception>
        /// <altmember cref="System.Diagnostics.Process.UserProcessorTime"/>
        /// <altmember cref="System.Diagnostics.Process.PrivilegedProcessorTime"/>
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
                        if (errorCode == Interop.Errors.ERROR_BAD_EXE_FORMAT || errorCode == Interop.Errors.ERROR_EXE_MACHINE_TYPE_MISMATCH)
                        {
                            throw new Win32Exception(errorCode, SR.InvalidApplication);
                        }
                        throw new Win32Exception(errorCode);
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
            // get the keys
            string[] keys = new string[sd.Count];
            sd.Keys.CopyTo(keys, 0);

            // sort both by the keys
            // Windows 2000 requires the environment block to be sorted by the key
            // It will first converting the case the strings and do ordinal comparison.

            // We do not use Array.Sort(keys, values, IComparer) since it is only supported
            // in System.Runtime contract from 4.20.0.0 and Test.Net depends on System.Runtime 4.0.10.0
            // we workaround this by sorting only the keys and then lookup the values form the keys.
            Array.Sort(keys, StringComparer.OrdinalIgnoreCase);

            // create a list of null terminated "key=val" strings
            StringBuilder stringBuff = new StringBuilder();
            for (int i = 0; i < sd.Count; ++i)
            {
                stringBuff.Append(keys[i]);
                stringBuff.Append('=');
                stringBuff.Append(sd[keys[i]]);
                stringBuff.Append('\0');
            }
            // an extra null at the end that indicates end of list will come from the string.
            return stringBuff.ToString();
        }
    }
}
