// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

        internal static SafeProcessHandle StartCore(ProcessStartInfo startInfo, SafeFileHandle? stdinHandle, SafeFileHandle? stdoutHandle, SafeFileHandle? stderrHandle)
        {
            return startInfo.UseShellExecute
                ? StartWithShellExecuteEx(startInfo)
                : StartWithCreateProcess(startInfo, stdinHandle, stdoutHandle, stderrHandle);
        }

        private static unsafe SafeProcessHandle StartWithShellExecuteEx(ProcessStartInfo startInfo)
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
                ShellExecuteHelper executeHelper = new ShellExecuteHelper(&shellExecuteInfo);
                if (!executeHelper.ShellExecuteOnSTAThread())
                {
                    int errorCode = executeHelper.ErrorCode;
                    if (errorCode == 0)
                    {
                        errorCode = ShellExecuteHelper.GetShellError(shellExecuteInfo.hInstApp);
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
            }
        }

        /// <summary>Starts the process using the supplied start info.</summary>
        private static unsafe SafeProcessHandle StartWithCreateProcess(ProcessStartInfo startInfo, SafeFileHandle? stdinHandle, SafeFileHandle? stdoutHandle, SafeFileHandle? stderrHandle)
        {
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

            System.Collections.Generic.IList<System.Runtime.InteropServices.SafeHandle>? inheritedHandles = startInfo.InheritedHandles;
            bool hasInheritedHandles = inheritedHandles is not null;

            // When InheritedHandles is set, we use PROC_THREAD_ATTRIBUTE_HANDLE_LIST to restrict inheritance.
            // For that, we need a reader lock (concurrent starts with different explicit lists are safe).
            // When InheritedHandles is not set, we use the existing approach with a writer lock.
            if (hasInheritedHandles)
            {
                ProcessUtils.s_processStartLock.EnterReadLock();
            }
            else
            {
                // Take a global lock to synchronize all redirect pipe handle creations and CreateProcess
                // calls. We do not want one process to inherit the handles created concurrently for another
                // process, as that will impact the ownership and lifetimes of those handles now inherited
                // into multiple child processes.
                ProcessUtils.s_processStartLock.EnterWriteLock();
            }
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

                if (hasInheritedHandles)
                {
                    if (startInfo.UserName.Length != 0)
                    {
                        // CreateProcessWithLogonW does not support PROC_THREAD_ATTRIBUTE_HANDLE_LIST.
                        throw new InvalidOperationException(SR.InheritedHandlesNotSupportedWithCredentials);
                    }

                    // Use STARTUPINFOEX with PROC_THREAD_ATTRIBUTE_HANDLE_LIST to restrict inheritance
                    // to only the explicitly specified handles.
                    retVal = StartWithCreateProcessEx(startInfo, ref startupInfo, ref processInfo,
                        ref unused_SecAttrs, inheritedHandles!, inheritableStdinHandle, inheritableStdoutHandle,
                        inheritableStderrHandle, stdinHandle, stdoutHandle, stderrHandle,
                        commandLine, creationFlags, environmentBlock, workingDirectory, out errorCode);
                }
                else if (startInfo.UserName.Length != 0)
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

                if (hasInheritedHandles)
                {
                    ProcessUtils.s_processStartLock.ExitReadLock();
                }
                else
                {
                    ProcessUtils.s_processStartLock.ExitWriteLock();
                }

                commandLine.Dispose();
            }

            Debug.Assert(!procSH.IsInvalid);
            procSH.ProcessId = (int)processInfo.dwProcessId;
            return procSH;
        }

        /// <summary>
        /// Starts the process using STARTUPINFOEX and PROC_THREAD_ATTRIBUTE_HANDLE_LIST to restrict
        /// handle inheritance to only the explicitly specified handles.
        /// </summary>
        private static unsafe bool StartWithCreateProcessEx(
            ProcessStartInfo startInfo,
            ref Interop.Kernel32.STARTUPINFO startupInfo,
            ref Interop.Kernel32.PROCESS_INFORMATION processInfo,
            ref Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs,
            System.Collections.Generic.IList<System.Runtime.InteropServices.SafeHandle> inheritedHandles,
            SafeFileHandle? inheritableStdinHandle,
            SafeFileHandle? inheritableStdoutHandle,
            SafeFileHandle? inheritableStderrHandle,
            SafeFileHandle? stdinHandle,
            SafeFileHandle? stdoutHandle,
            SafeFileHandle? stderrHandle,
            ValueStringBuilder commandLine,
            int creationFlags,
            string? environmentBlock,
            string? workingDirectory,
            out int errorCode)
        {
            errorCode = 0;
            void* attributeListBuffer = null;
            Interop.Kernel32.LPPROC_THREAD_ATTRIBUTE_LIST attributeList = default;
            System.Runtime.InteropServices.SafeHandle?[]? handlesToRelease = null;

            // Determine the maximum number of handles: up to 3 stdio + all user-provided handles
            int stdioCount = (stdinHandle is not null ? 1 : 0)
                + (stdoutHandle is not null ? 1 : 0)
                + (stderrHandle is not null ? 1 : 0);
            int maxHandleCount = stdioCount + inheritedHandles.Count;
            IntPtr* handlesToInherit = (IntPtr*)NativeMemory.Alloc((nuint)Math.Max(1, maxHandleCount), (nuint)sizeof(IntPtr));

            try
            {
                int handleCount = 0;

                // Add the effective stdio handles (already made inheritable via DuplicateAsInheritableIfNeeded)
                if (stdinHandle is not null)
                {
                    IntPtr h = (inheritableStdinHandle ?? stdinHandle).DangerousGetHandle();
                    if (h != IntPtr.Zero && h != new IntPtr(-1))
                        handlesToInherit[handleCount++] = h;
                }
                if (stdoutHandle is not null)
                {
                    IntPtr h = (inheritableStdoutHandle ?? stdoutHandle).DangerousGetHandle();
                    if (h != IntPtr.Zero && h != new IntPtr(-1))
                    {
                        // Avoid duplicates
                        bool isDuplicate = false;
                        for (int i = 0; i < handleCount; i++)
                        {
                            if (handlesToInherit[i] == h) { isDuplicate = true; break; }
                        }
                        if (!isDuplicate)
                            handlesToInherit[handleCount++] = h;
                    }
                }
                if (stderrHandle is not null)
                {
                    IntPtr h = (inheritableStderrHandle ?? stderrHandle).DangerousGetHandle();
                    if (h != IntPtr.Zero && h != new IntPtr(-1))
                    {
                        bool isDuplicate = false;
                        for (int i = 0; i < handleCount; i++)
                        {
                            if (handlesToInherit[i] == h) { isDuplicate = true; break; }
                        }
                        if (!isDuplicate)
                            handlesToInherit[handleCount++] = h;
                    }
                }

                // Add and prepare the user-provided InheritedHandles
                PrepareHandleAllowList(inheritedHandles, handlesToInherit, ref handleCount, ref handlesToRelease);

                // Create the attribute list
                nuint size = 0;
                Interop.Kernel32.LPPROC_THREAD_ATTRIBUTE_LIST emptyList = default;
                Interop.Kernel32.InitializeProcThreadAttributeList(emptyList, 1, 0, ref size);

                attributeListBuffer = NativeMemory.Alloc(size);
                attributeList.AttributeList = (IntPtr)attributeListBuffer;

                if (!Interop.Kernel32.InitializeProcThreadAttributeList(attributeList, 1, 0, ref size))
                {
                    errorCode = Marshal.GetLastWin32Error();
                    return false;
                }

                if (!Interop.Kernel32.UpdateProcThreadAttribute(
                    attributeList,
                    0,
                    (IntPtr)Interop.Kernel32.PROC_THREAD_ATTRIBUTE_HANDLE_LIST,
                    handlesToInherit,
                    (nuint)(handleCount * sizeof(IntPtr)),
                    null,
                    0))
                {
                    errorCode = Marshal.GetLastWin32Error();
                    return false;
                }

                Interop.Kernel32.STARTUPINFOEX startupInfoEx = default;
                startupInfoEx.StartupInfo = startupInfo;
                startupInfoEx.StartupInfo.cb = sizeof(Interop.Kernel32.STARTUPINFOEX);
                startupInfoEx.lpAttributeList = attributeList;

                creationFlags |= Interop.Kernel32.EXTENDED_STARTUPINFO_PRESENT;

                commandLine.NullTerminate();
                fixed (char* environmentBlockPtr = environmentBlock)
                fixed (char* commandLinePtr = &commandLine.GetPinnableReference())
                {
                    bool retVal = Interop.Kernel32.CreateProcess(
                        null,
                        commandLinePtr,
                        ref secAttrs,
                        ref secAttrs,
                        true,
                        creationFlags,
                        environmentBlockPtr,
                        workingDirectory,
                        ref startupInfoEx,
                        ref processInfo);
                    if (!retVal)
                        errorCode = Marshal.GetLastWin32Error();
                    return retVal;
                }
            }
            finally
            {
                NativeMemory.Free(handlesToInherit);

                if (attributeListBuffer != null)
                {
                    Interop.Kernel32.DeleteProcThreadAttributeList(attributeList);
                    NativeMemory.Free(attributeListBuffer);
                }

                if (handlesToRelease is not null)
                {
                    CleanupHandles(handlesToRelease);
                }
            }
        }

        private static unsafe void PrepareHandleAllowList(
            System.Collections.Generic.IList<System.Runtime.InteropServices.SafeHandle> inheritedHandles,
            IntPtr* handlesToInherit,
            ref int handleCount,
            ref System.Runtime.InteropServices.SafeHandle?[]? handlesToRelease)
        {
            handlesToRelease = new System.Runtime.InteropServices.SafeHandle[inheritedHandles.Count];
            int handleIndex = 0;

            foreach (System.Runtime.InteropServices.SafeHandle handle in inheritedHandles)
            {
                if (handle is null || handle.IsInvalid || handle.IsClosed)
                    continue;

                // Prevent handle from being disposed while we use the raw pointer.
                // DangerousAddRef must be called before DangerousGetHandle to avoid a race
                // where the handle is closed between the null check and the pointer read.
                bool refAdded = false;
                try
                {
                    handle.DangerousAddRef(ref refAdded);
                    IntPtr handlePtr = handle.DangerousGetHandle();

                    // Check if this handle is already in the list (avoid duplicates)
                    bool isDuplicate = false;
                    for (int i = 0; i < handleCount; i++)
                    {
                        if (handlesToInherit[i] == handlePtr)
                        {
                            isDuplicate = true;
                            break;
                        }
                    }

                    if (!isDuplicate)
                    {
                        // Enable inheritance on this handle so the child process can use it
                        if (!Interop.Kernel32.SetHandleInformation(
                            handle,
                            Interop.Kernel32.HandleFlags.HANDLE_FLAG_INHERIT,
                            Interop.Kernel32.HandleFlags.HANDLE_FLAG_INHERIT))
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }

                        handlesToRelease[handleIndex++] = handle;
                        handlesToInherit[handleCount++] = handlePtr;
                        refAdded = false; // transferred ownership to handlesToRelease
                    }
                }
                finally
                {
                    if (refAdded)
                        handle.DangerousRelease();
                }
            }
        }

        private static void CleanupHandles(System.Runtime.InteropServices.SafeHandle?[] handlesToRelease)
        {
            foreach (System.Runtime.InteropServices.SafeHandle? safeHandle in handlesToRelease)
            {
                if (safeHandle is null)
                {
                    break;
                }

                safeHandle.DangerousRelease();

                // Remove the inheritance flag so they are not unintentionally inherited by
                // other processes started after this point.
                if (!Interop.Kernel32.SetHandleInformation(
                    safeHandle,
                    Interop.Kernel32.HandleFlags.HANDLE_FLAG_INHERIT,
                    0))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }

        private int GetProcessIdCore() => Interop.Kernel32.GetProcessId(this);
    }
}
