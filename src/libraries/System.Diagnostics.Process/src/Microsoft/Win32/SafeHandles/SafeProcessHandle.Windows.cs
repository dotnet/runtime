// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        protected override bool ReleaseHandle()
        {
            return Interop.Kernel32.CloseHandle(handle);
        }

        private static Func<ProcessStartInfo, SafeProcessHandle>? s_startWithShellExecute;

        internal static unsafe SafeProcessHandle StartCore(ProcessStartInfo startInfo, SafeFileHandle? stdinHandle, SafeFileHandle? stdoutHandle, SafeFileHandle? stderrHandle)
        {
            if (startInfo.UseShellExecute)
                return s_startWithShellExecute!(startInfo);

            // See knowledge base article Q190351 for an explanation of the following code.  Noteworthy tricky points:
            //    * The handles are duplicated as inheritable before they are passed to CreateProcess so
            //      that the child process can use them

            var commandLine = new ValueStringBuilder(stackalloc char[256]);
            ProcessUtils.BuildCommandLine(startInfo, ref commandLine);

            Interop.Kernel32.STARTUPINFO startupInfo = default;
            Interop.Kernel32.PROCESS_INFORMATION processInfo = default;
            Interop.Kernel32.SECURITY_ATTRIBUTES unused_SecAttrs = default;
            SafeProcessHandle procSH = new SafeProcessHandle();

            // Inheritable copies of the child handles for CreateProcess
            SafeFileHandle? inheritableStdinHandle = null;
            SafeFileHandle? inheritableStdoutHandle = null;
            SafeFileHandle? inheritableStderrHandle = null;

            // Take a global lock to synchronize all redirect pipe handle creations and CreateProcess
            // calls. We do not want one process to inherit the handles created concurrently for another
            // process, as that will impact the ownership and lifetimes of those handles now inherited
            // into multiple child processes.

            ProcessUtils.s_processStartLock.EnterWriteLock();
            try
            {
                startupInfo.cb = sizeof(Interop.Kernel32.STARTUPINFO);

                if (stdinHandle is not null || stdoutHandle is not null || stderrHandle is not null)
                {
                    Debug.Assert(stdinHandle is not null && stdoutHandle is not null && stderrHandle is not null, "All or none of the standard handles must be provided.");

                    ProcessUtils.DuplicateAsInheritableIfNeeded(stdinHandle, ref inheritableStdinHandle);
                    ProcessUtils.DuplicateAsInheritableIfNeeded(stdoutHandle, ref inheritableStdoutHandle);
                    ProcessUtils.DuplicateAsInheritableIfNeeded(stderrHandle, ref inheritableStderrHandle);

                    startupInfo.hStdInput = (inheritableStdinHandle ?? stdinHandle).DangerousGetHandle();
                    startupInfo.hStdOutput = (inheritableStdoutHandle ?? stdoutHandle).DangerousGetHandle();
                    startupInfo.hStdError = (inheritableStderrHandle ?? stderrHandle).DangerousGetHandle();

                    // If STARTF_USESTDHANDLES is not set, the new process will inherit the standard handles.
                    startupInfo.dwFlags = Interop.Advapi32.StartupInfoOptions.STARTF_USESTDHANDLES;
                }

                if (startInfo.WindowStyle != ProcessWindowStyle.Normal)
                {
                    startupInfo.wShowWindow = (short)ProcessUtils.GetShowWindowFromWindowStyle(startInfo.WindowStyle);
                    startupInfo.dwFlags |= Interop.Advapi32.StartupInfoOptions.STARTF_USESHOWWINDOW;
                }

                // set up the creation flags parameter
                int creationFlags = 0;
                if (startInfo.CreateNoWindow) creationFlags |= Interop.Advapi32.StartupInfoOptions.CREATE_NO_WINDOW;
                if (startInfo.CreateNewProcessGroup) creationFlags |= Interop.Advapi32.StartupInfoOptions.CREATE_NEW_PROCESS_GROUP;

                // set up the environment block parameter
                string? environmentBlock = null;
                if (startInfo._environmentVariables != null)
                {
                    creationFlags |= Interop.Advapi32.StartupInfoOptions.CREATE_UNICODE_ENVIRONMENT;
                    environmentBlock = ProcessUtils.GetEnvironmentVariablesBlock(startInfo._environmentVariables!);
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
                    if (startInfo.LoadUserProfile && startInfo.UseCredentialsForNetworkingOnly)
                    {
                        throw new ArgumentException(SR.CantEnableConflictingLogonFlags, nameof(startInfo));
                    }
                    else if (startInfo.LoadUserProfile)
                    {
                        logonFlags = Interop.Advapi32.LogonFlags.LOGON_WITH_PROFILE;
                    }
                    else if (startInfo.UseCredentialsForNetworkingOnly)
                    {
                        logonFlags = Interop.Advapi32.LogonFlags.LOGON_NETCREDENTIALS_ONLY;
                    }

                    commandLine.NullTerminate();
                    fixed (char* passwordInClearTextPtr = startInfo.PasswordInClearText ?? string.Empty)
                    fixed (char* environmentBlockPtr = environmentBlock)
                    fixed (char* commandLinePtr = &commandLine.GetPinnableReference())
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
                    commandLine.NullTerminate();
                    fixed (char* environmentBlockPtr = environmentBlock)
                    fixed (char* commandLinePtr = &commandLine.GetPinnableReference())
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
                        : Interop.Kernel32.GetMessage(errorCode);

                    throw ProcessUtils.CreateExceptionForErrorStartingProcess(nativeErrorMessage, errorCode, startInfo.FileName, workingDirectory);
                }
            }
            catch
            {
                procSH.Dispose();
                throw;
            }
            finally
            {
                // Only dispose duplicated handles, not the original handles passed by the caller.
                // When the handle was invalid or already inheritable, no duplication was needed.
                inheritableStdinHandle?.Dispose();
                inheritableStdoutHandle?.Dispose();
                inheritableStderrHandle?.Dispose();

                ProcessUtils.s_processStartLock.ExitWriteLock();

                commandLine.Dispose();
            }

            Debug.Assert(!procSH.IsInvalid);
            procSH.ProcessId = (int)processInfo.dwProcessId;
            return procSH;
        }

        private static unsafe SafeProcessHandle StartWithShellExecute(ProcessStartInfo startInfo)
        {
            if (!string.IsNullOrEmpty(startInfo.UserName) || startInfo.Password != null)
                throw new InvalidOperationException(SR.CantStartAsUser);

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

                shellExecuteInfo.nShow = ProcessUtils.GetShowWindowFromWindowStyle(startInfo.WindowStyle);

                bool succeeded = false;
                int lastError = 0;
                nuint executeInfoAddress = (nuint)(&shellExecuteInfo); // cast to nuint to allow delegate capture; safe because Join() keeps this stack frame alive for the thread's lifetime

                void ShellExecuteFunction()
                {
                    try
                    {
                        if (!(succeeded = Interop.Shell32.ShellExecuteExW((Interop.Shell32.SHELLEXECUTEINFO*)executeInfoAddress)))
                            lastError = Marshal.GetLastWin32Error();
                    }
                    catch (EntryPointNotFoundException)
                    {
                        lastError = Interop.Errors.ERROR_CALL_NOT_IMPLEMENTED;
                    }
                }

                // ShellExecute() requires STA in order to work correctly.
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                {
                    Thread executionThread = new Thread(ShellExecuteFunction)
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

                if (!succeeded)
                {
                    int errorCode = lastError;
                    if (errorCode == 0)
                    {
                        errorCode = GetShellError(shellExecuteInfo.hInstApp);
                    }

                    switch (errorCode)
                    {
                        case Interop.Errors.ERROR_CALL_NOT_IMPLEMENTED:
                            // This happens on Windows Nano
                            throw new PlatformNotSupportedException(SR.UseShellExecuteNotSupported);
                        default:
                            string nativeErrorMessage = errorCode == Interop.Errors.ERROR_BAD_EXE_FORMAT || errorCode == Interop.Errors.ERROR_EXE_MACHINE_TYPE_MISMATCH
                                ? SR.InvalidApplication
                                : Interop.Kernel32.GetMessage(errorCode);

                            throw ProcessUtils.CreateExceptionForErrorStartingProcess(nativeErrorMessage, errorCode, startInfo.FileName, startInfo.WorkingDirectory);
                    }
                }

                // From https://learn.microsoft.com/windows/win32/api/shellapi/ns-shellapi-shellexecuteinfow:
                // "In some cases, such as when execution is satisfied through a DDE conversation, no handle will be returned."
                // Process.Start will return false if the handle is invalid.
                return new SafeProcessHandle(shellExecuteInfo.hProcess);

                static int GetShellError(IntPtr error) =>
                    (long)error switch
                    {
                        Interop.Shell32.SE_ERR_FNF => Interop.Errors.ERROR_FILE_NOT_FOUND,
                        Interop.Shell32.SE_ERR_PNF => Interop.Errors.ERROR_PATH_NOT_FOUND,
                        Interop.Shell32.SE_ERR_ACCESSDENIED => Interop.Errors.ERROR_ACCESS_DENIED,
                        Interop.Shell32.SE_ERR_OOM => Interop.Errors.ERROR_NOT_ENOUGH_MEMORY,
                        Interop.Shell32.SE_ERR_DDEFAIL or
                        Interop.Shell32.SE_ERR_DDEBUSY or
                        Interop.Shell32.SE_ERR_DDETIMEOUT => Interop.Errors.ERROR_DDE_FAIL,
                        Interop.Shell32.SE_ERR_SHARE => Interop.Errors.ERROR_SHARING_VIOLATION,
                        Interop.Shell32.SE_ERR_NOASSOC => Interop.Errors.ERROR_NO_ASSOCIATION,
                        Interop.Shell32.SE_ERR_DLLNOTFOUND => Interop.Errors.ERROR_DLL_NOT_FOUND,
                        _ => (int)(long)error,
                    };
            }
        }

        private int GetProcessIdCore() => Interop.Kernel32.GetProcessId(this);

        private bool SignalCore(PosixSignal signal)
        {
            // On Windows, only SIGKILL is supported, mapped to TerminateProcess.
            if (signal != PosixSignal.SIGKILL)
            {
                throw new PlatformNotSupportedException();
            }

            if (!Interop.Kernel32.TerminateProcess(this, -1))
            {
                int errorCode = Marshal.GetLastWin32Error();

                // Return false if the process has already exited.
                if (errorCode == Interop.Errors.ERROR_ACCESS_DENIED &&
                    Interop.Kernel32.GetExitCodeProcess(this, out int exitCode) &&
                    exitCode != Interop.Kernel32.HandleOptions.STILL_ACTIVE)
                {
                    return false;
                }

                throw new Win32Exception(errorCode);
            }

            return true;
        }
    }
}
