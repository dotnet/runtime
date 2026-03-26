// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Class:  SafeProcessHandle
**
** A wrapper for a process handle
**
**
===========================================================*/

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        protected override bool ReleaseHandle()
        {
            return Interop.Kernel32.CloseHandle(handle);
        }

        private static SafeProcessHandle StartCore(System.Diagnostics.ProcessStartInfo startInfo, SafeFileHandle? stdinHandle, SafeFileHandle? stdoutHandle, SafeFileHandle? stderrHandle)
        {
            return startInfo.UseShellExecute
                ? StartWithShellExecuteEx(startInfo)
                : StartWithCreateProcess(startInfo, stdinHandle, stdoutHandle, stderrHandle);
        }

        private static unsafe SafeProcessHandle StartWithShellExecuteEx(System.Diagnostics.ProcessStartInfo startInfo)
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

                shellExecuteInfo.nShow = System.Diagnostics.ProcessUtils.GetShowWindowFromWindowStyle(startInfo.WindowStyle);
                System.Diagnostics.ShellExecuteHelper executeHelper = new System.Diagnostics.ShellExecuteHelper(&shellExecuteInfo);
                if (!executeHelper.ShellExecuteOnSTAThread())
                {
                    int errorCode = executeHelper.ErrorCode;
                    if (errorCode == 0)
                    {
                        errorCode = System.Diagnostics.ProcessUtils.GetShellError(shellExecuteInfo.hInstApp);
                    }

                    switch (errorCode)
                    {
                        case Interop.Errors.ERROR_CALL_NOT_IMPLEMENTED:
                            // This happens on Windows Nano
                            throw new PlatformNotSupportedException(SR.UseShellExecuteNotSupported);
                        default:
                            string nativeErrorMessage = errorCode == Interop.Errors.ERROR_BAD_EXE_FORMAT || errorCode == Interop.Errors.ERROR_EXE_MACHINE_TYPE_MISMATCH
                                ? SR.InvalidApplication
                                : System.Diagnostics.ProcessUtils.GetErrorMessage(errorCode);

                            throw System.Diagnostics.ProcessUtils.CreateExceptionForErrorStartingProcess(nativeErrorMessage, errorCode, startInfo.FileName, startInfo.WorkingDirectory);
                    }
                }

                if (shellExecuteInfo.hProcess != IntPtr.Zero)
                {
                    return new SafeProcessHandle(shellExecuteInfo.hProcess);
                }
            }

            return new SafeProcessHandle();
        }

        private static unsafe SafeProcessHandle StartWithCreateProcess(System.Diagnostics.ProcessStartInfo startInfo, SafeFileHandle? stdinHandle, SafeFileHandle? stdoutHandle, SafeFileHandle? stderrHandle)
        {
            // See knowledge base article Q190351 for an explanation of the following code.  Noteworthy tricky points:
            //    * The handles are duplicated as inheritable before they are passed to CreateProcess so
            //      that the child process can use them

            var commandLine = new ValueStringBuilder(stackalloc char[256]);
            System.Diagnostics.ProcessUtils.BuildCommandLine(startInfo, ref commandLine);

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

                    System.Diagnostics.ProcessUtils.DuplicateAsInheritableIfNeeded(stdinHandle, ref inheritableStdinHandle);
                    System.Diagnostics.ProcessUtils.DuplicateAsInheritableIfNeeded(stdoutHandle, ref inheritableStdoutHandle);
                    System.Diagnostics.ProcessUtils.DuplicateAsInheritableIfNeeded(stderrHandle, ref inheritableStderrHandle);

                    startupInfo.hStdInput = (inheritableStdinHandle ?? stdinHandle).DangerousGetHandle();
                    startupInfo.hStdOutput = (inheritableStdoutHandle ?? stdoutHandle).DangerousGetHandle();
                    startupInfo.hStdError = (inheritableStderrHandle ?? stderrHandle).DangerousGetHandle();

                    // If STARTF_USESTDHANDLES is not set, the new process will inherit the standard handles.
                    startupInfo.dwFlags = Interop.Advapi32.StartupInfoOptions.STARTF_USESTDHANDLES;
                }

                if (startInfo.WindowStyle != System.Diagnostics.ProcessWindowStyle.Normal)
                {
                    startupInfo.wShowWindow = (short)System.Diagnostics.ProcessUtils.GetShowWindowFromWindowStyle(startInfo.WindowStyle);
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
                    environmentBlock = System.Diagnostics.ProcessUtils.GetEnvironmentVariablesBlock(startInfo._environmentVariables!);
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
                        : System.Diagnostics.ProcessUtils.GetErrorMessage(errorCode);

                    throw System.Diagnostics.ProcessUtils.CreateExceptionForErrorStartingProcess(nativeErrorMessage, errorCode, startInfo.FileName, workingDirectory);
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

            if (procSH.IsInvalid)
            {
                procSH.Dispose();
                return new SafeProcessHandle();
            }

            return procSH;
        }
    }
}
