// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

            Interop.Kernel32.STARTUPINFOEX startupInfoEx = default;
            Interop.Kernel32.PROCESS_INFORMATION processInfo = default;
            Interop.Kernel32.SECURITY_ATTRIBUTES unused_SecAttrs = default;
            SafeProcessHandle procSH = new SafeProcessHandle();

            // Inheritable copies of the child handles for CreateProcess
            SafeFileHandle? inheritableStdinHandle = null;
            SafeFileHandle? inheritableStdoutHandle = null;
            SafeFileHandle? inheritableStderrHandle = null;

            IList<SafeHandle>? inheritedHandles = startInfo.InheritedHandles;
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

            void* attributeListBuffer = null;
            SafeHandle?[]? handlesToRelease = null;
            IntPtr* handlesToInherit = null;

            try
            {
                startupInfoEx.StartupInfo.cb = sizeof(Interop.Kernel32.STARTUPINFOEX);

                if (stdinHandle is not null || stdoutHandle is not null || stderrHandle is not null)
                {
                    Debug.Assert(stdinHandle is not null && stdoutHandle is not null && stderrHandle is not null, "All or none of the standard handles must be provided.");

                    ProcessUtils.DuplicateAsInheritableIfNeeded(stdinHandle, ref inheritableStdinHandle);
                    ProcessUtils.DuplicateAsInheritableIfNeeded(stdoutHandle, ref inheritableStdoutHandle);
                    ProcessUtils.DuplicateAsInheritableIfNeeded(stderrHandle, ref inheritableStderrHandle);

                    startupInfoEx.StartupInfo.hStdInput = (inheritableStdinHandle ?? stdinHandle).DangerousGetHandle();
                    startupInfoEx.StartupInfo.hStdOutput = (inheritableStdoutHandle ?? stdoutHandle).DangerousGetHandle();
                    startupInfoEx.StartupInfo.hStdError = (inheritableStderrHandle ?? stderrHandle).DangerousGetHandle();

                    // If STARTF_USESTDHANDLES is not set, the new process will inherit the standard handles.
                    startupInfoEx.StartupInfo.dwFlags = Interop.Advapi32.StartupInfoOptions.STARTF_USESTDHANDLES;
                }

                if (startInfo.WindowStyle != ProcessWindowStyle.Normal)
                {
                    startupInfoEx.StartupInfo.wShowWindow = (short)ProcessUtils.GetShowWindowFromWindowStyle(startInfo.WindowStyle);
                    startupInfoEx.StartupInfo.dwFlags |= Interop.Advapi32.StartupInfoOptions.STARTF_USESHOWWINDOW;
                }

                // set up the creation flags parameter
                int creationFlags = hasInheritedHandles ? Interop.Kernel32.EXTENDED_STARTUPINFO_PRESENT : 0;
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

                // When InheritedHandles is set, build a PROC_THREAD_ATTRIBUTE_HANDLE_LIST to restrict
                // inheritance to only the explicitly specified handles.
                int handleCount = 0;
                if (hasInheritedHandles)
                {
                    int maxHandleCount = 3 + inheritedHandles!.Count;
                    handlesToInherit = (IntPtr*)NativeMemory.Alloc((nuint)maxHandleCount, (nuint)sizeof(IntPtr));

                    // Add the effective stdio handles (already made inheritable via DuplicateAsInheritableIfNeeded)
                    AddHandleToInheritList(inheritableStdinHandle ?? stdinHandle, handlesToInherit, ref handleCount);
                    AddHandleToInheritList(inheritableStdoutHandle ?? stdoutHandle, handlesToInherit, ref handleCount);
                    AddHandleToInheritList(inheritableStderrHandle ?? stderrHandle, handlesToInherit, ref handleCount);

                    PrepareHandleAllowList(inheritedHandles, handlesToInherit, ref handleCount, ref handlesToRelease);
                    BuildProcThreadAttributeList(handlesToInherit, handleCount, ref attributeListBuffer);
                }

                startupInfoEx.lpAttributeList = attributeListBuffer;

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
                        IntPtr secureStringPtr = (startInfo.Password != null) ? Marshal.SecureStringToGlobalAllocUnicode(startInfo.Password) : IntPtr.Zero;
                        IntPtr passwordPtr = (startInfo.Password != null) ? secureStringPtr : (IntPtr)passwordInClearTextPtr;

                        SafeTokenHandle? tokenHandle = null;
                        try
                        {
                            // CreateProcessWithLogonW does not support STARTUPINFOEX.
                            // CreateProcessWithTokenW docs mention STARTUPINFOEX, but they don't mention
                            // that EXTENDED_STARTUPINFO_PRESENT is not supported anyway.
                            // So when we have to use EXTENDED_STARTUPINFO_PRESENT, we use CreateProcessAsUserW.
                            // In contrary to CreateProcessWithLogonW, CreateProcessAsUserW requires privilege (SE_INCREASE_QUOTA_NAME).
                            // That is why we use it only for the new, optional features.
                            if ((creationFlags & Interop.Kernel32.EXTENDED_STARTUPINFO_PRESENT) != 0)
                            {
                                // Map LogonFlags (used by CreateProcessWithLogonW) to LOGON32_LOGON_* constants
                                // required by LogonUser's dwLogonType parameter.
                                int logonType = logonFlags == Interop.Advapi32.LogonFlags.LOGON_NETCREDENTIALS_ONLY
                                    ? Interop.Advapi32.LOGON32_LOGON_NEW_CREDENTIALS
                                    : Interop.Advapi32.LOGON32_LOGON_INTERACTIVE;

                                if (!Interop.Advapi32.LogonUser(startInfo.UserName, startInfo.Domain, passwordPtr, logonType, 0, out tokenHandle))
                                {
                                    errorCode = Marshal.GetLastWin32Error();
                                    retVal = false;
                                }
                                else
                                {
                                    retVal = Interop.Kernel32.CreateProcessAsUser(
                                        tokenHandle,         // token representing the user
                                        null,                // we don't need this since all the info is in commandLine
                                        commandLinePtr,      // pointer to the command line string
                                        ref unused_SecAttrs, // address to process security attributes, we don't need to inherit the handle
                                        ref unused_SecAttrs, // address to thread security attributes.
                                        true,                // handle inheritance flag
                                        creationFlags,       // creation flags
                                        environmentBlockPtr, // pointer to new environment block
                                        workingDirectory,    // pointer to current directory name
                                        &startupInfoEx,      // pointer to STARTUPINFOEX
                                        &processInfo         // pointer to PROCESS_INFORMATION
                                    );
                                    if (!retVal)
                                        errorCode = Marshal.GetLastWin32Error();
                                }
                            }
                            else
                            {
                                Interop.Kernel32.STARTUPINFO startupInfo = startupInfoEx.StartupInfo;
                                startupInfo.cb = sizeof(Interop.Kernel32.STARTUPINFO);

                                retVal = Interop.Advapi32.CreateProcessWithLogonW(
                                    startInfo.UserName,
                                    startInfo.Domain,
                                    passwordPtr,
                                    logonFlags,
                                    null,            // we don't need this since all the info is in commandLine
                                    commandLinePtr,
                                    creationFlags,
                                    (IntPtr)environmentBlockPtr,
                                    workingDirectory,
                                    &startupInfo,
                                    &processInfo
                                );
                            }
                            if (!retVal)
                                errorCode = Marshal.GetLastWin32Error();
                        }
                        finally
                        {
                            if (secureStringPtr != IntPtr.Zero)
                                Marshal.ZeroFreeGlobalAllocUnicode(secureStringPtr);

                            tokenHandle?.Dispose();
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
                            environmentBlockPtr, // pointer to new environment block
                            workingDirectory,    // pointer to current directory name
                            &startupInfoEx,      // pointer to STARTUPINFOEX
                            &processInfo         // pointer to PROCESS_INFORMATION
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

                NativeMemory.Free(handlesToInherit);

                if (attributeListBuffer != null)
                {
                    Interop.Kernel32.DeleteProcThreadAttributeList(attributeListBuffer);
                    NativeMemory.Free(attributeListBuffer);
                }

                if (handlesToRelease is not null)
                {
                    CleanupHandles(handlesToRelease);
                }

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

        /// <summary>
        /// Adds a handle to the inherit list if it is valid and not already present.
        /// </summary>
        private static unsafe void AddHandleToInheritList(SafeFileHandle? handle, IntPtr* handlesToInherit, ref int handleCount)
        {
            if (handle is null || handle.IsInvalid)
            {
                return;
            }

            IntPtr h = handle.DangerousGetHandle();
            for (int i = 0; i < handleCount; i++)
            {
                if (handlesToInherit[i] == h)
                {
                    return; // already in list
                }
            }
            handlesToInherit[handleCount++] = h;
        }

        /// <summary>
        /// Creates and populates a PROC_THREAD_ATTRIBUTE_LIST with a PROC_THREAD_ATTRIBUTE_HANDLE_LIST entry.
        /// </summary>
        private static unsafe void BuildProcThreadAttributeList(
            IntPtr* handlesToInherit,
            int handleCount,
            ref void* attributeListBuffer)
        {
            nuint size = 0;
            int attributeCount = handleCount > 0 ? 1 : 0;
            Interop.Kernel32.InitializeProcThreadAttributeList(null, attributeCount, 0, ref size);

            attributeListBuffer = NativeMemory.Alloc(size);

            if (!Interop.Kernel32.InitializeProcThreadAttributeList(attributeListBuffer, attributeCount, 0, ref size))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            if (handleCount > 0 && !Interop.Kernel32.UpdateProcThreadAttribute(
                attributeListBuffer,
                0,
                (IntPtr)Interop.Kernel32.PROC_THREAD_ATTRIBUTE_HANDLE_LIST,
                handlesToInherit,
                (nuint)(handleCount * sizeof(IntPtr)),
                null,
                0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        private static unsafe void PrepareHandleAllowList(
            IList<SafeHandle> inheritedHandles,
            IntPtr* handlesToInherit,
            ref int handleCount,
            ref SafeHandle?[]? handlesToRelease)
        {
            handlesToRelease = new SafeHandle[inheritedHandles.Count];
            int handleIndex = 0;

            foreach (SafeHandle handle in inheritedHandles)
            {
                if (handle is null || handle.IsInvalid || handle.IsClosed)
                {
                    continue;
                }

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

                        // Transfer ref ownership to handlesToRelease; CleanupHandles will release it.
                        handlesToRelease[handleIndex++] = handle;
                        handlesToInherit[handleCount++] = handlePtr;
                        refAdded = false; // ownership transferred — don't release in finally
                    }
                }
                finally
                {
                    if (refAdded)
                    {
                        // AddRef succeeded but ownership was not transferred (duplicate or not added).
                        handle.DangerousRelease();
                    }
                }
            }
        }

        private static void CleanupHandles(SafeHandle?[] handlesToRelease)
        {
            foreach (SafeHandle? safeHandle in handlesToRelease)
            {
                if (safeHandle is null)
                {
                    break;
                }

                try
                {
                    // Remove the inheritance flag so they are not unintentionally inherited by
                    // other processes started after this point.
                    // Ignore failures — the handle may have been closed by the time we get here.
                    Interop.Kernel32.SetHandleInformation(
                        safeHandle,
                        Interop.Kernel32.HandleFlags.HANDLE_FLAG_INHERIT,
                        0);
                }
                finally
                {
                    safeHandle.DangerousRelease();
                }
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
