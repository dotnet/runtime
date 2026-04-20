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
        // Static job object used for KillOnParentExit functionality.
        // All child processes with KillOnParentExit=true are assigned to this job.
        // The job handle is intentionally never closed - it should live for the
        // lifetime of the process. When this process exits, the job object is destroyed
        // by the OS, which terminates all child processes in the job.
        private static readonly Lazy<Interop.Kernel32.SafeJobHandle> s_killOnParentExitJob = new(CreateKillOnParentExitJob);

        protected override bool ReleaseHandle()
        {
            return Interop.Kernel32.CloseHandle(handle);
        }

        private static unsafe Interop.Kernel32.SafeJobHandle CreateKillOnParentExitJob()
        {
            Interop.Kernel32.JOBOBJECT_EXTENDED_LIMIT_INFORMATION limitInfo = default;
            limitInfo.BasicLimitInformation.LimitFlags = Interop.Kernel32.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

            Interop.Kernel32.SafeJobHandle jobHandle = Interop.Kernel32.CreateJobObjectW(IntPtr.Zero, IntPtr.Zero);
            if (jobHandle.IsInvalid || !Interop.Kernel32.SetInformationJobObject(
                jobHandle,
                Interop.Kernel32.JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation,
                ref limitInfo,
                (uint)sizeof(Interop.Kernel32.JOBOBJECT_EXTENDED_LIMIT_INFORMATION)))
            {
                int error = Marshal.GetLastWin32Error();
                jobHandle.Dispose();
                throw new Win32Exception(error);
            }

            return jobHandle;
        }

        private static Func<ProcessStartInfo, SafeProcessHandle>? s_startWithShellExecute;

        internal static unsafe SafeProcessHandle StartCore(ProcessStartInfo startInfo, SafeFileHandle? stdinHandle, SafeFileHandle? stdoutHandle,
            SafeFileHandle? stderrHandle, SafeHandle[]? inheritedHandles = null)
        {
            if (startInfo.UseShellExecute)
            {
                // Nulls are allowed only for ShellExecute.
                Debug.Assert(stdinHandle is null && stdoutHandle is null && stderrHandle is null, "All of the standard handles must be null for ShellExecute.");
                return s_startWithShellExecute!(startInfo);
            }

            Debug.Assert(stdinHandle is not null && stdoutHandle is not null && stderrHandle is not null, "All of the standard handles must be provided.");

            // See knowledge base article Q190351 for an explanation of the following code.  Noteworthy tricky points:
            //    * The handles are duplicated as inheritable before they are passed to CreateProcess so
            //      that the child process can use them

            ValueStringBuilder commandLine = new(stackalloc char[256]);
            ProcessUtils.BuildCommandLine(startInfo, ref commandLine);

            Interop.Kernel32.STARTUPINFOEX startupInfoEx = default;
            Interop.Kernel32.PROCESS_INFORMATION processInfo = default;
            Interop.Kernel32.SECURITY_ATTRIBUTES unused_SecAttrs = default;
            SafeProcessHandle procSH = new SafeProcessHandle();

            // Inheritable copies of the child handles for CreateProcess
            bool stdinRefAdded = false, stdoutRefAdded = false, stderrRefAdded = false;
            bool restrictInheritedHandles = inheritedHandles is not null;
            bool killOnParentExit = startInfo.KillOnParentExit;
            bool logon = !string.IsNullOrEmpty(startInfo.UserName);

            // When InheritedHandles is set, we use PROC_THREAD_ATTRIBUTE_HANDLE_LIST to restrict inheritance
            // or pass bInheritHandles=false when there are no valid handles to inherit.
            // For that, we need a reader lock (concurrent starts with different explicit lists are safe).
            // When InheritedHandles is not set, we use the existing approach with a writer lock.
            if (restrictInheritedHandles)
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
            IntPtr* jobHandles = null;

            try
            {
                startupInfoEx.StartupInfo.cb = sizeof(Interop.Kernel32.STARTUPINFO);

                ProcessUtils.DuplicateAsInheritableIfNeeded(stdinHandle, ref startupInfoEx.StartupInfo.hStdInput, ref stdinRefAdded);
                ProcessUtils.DuplicateAsInheritableIfNeeded(stdoutHandle, ref startupInfoEx.StartupInfo.hStdOutput, ref stdoutRefAdded);
                ProcessUtils.DuplicateAsInheritableIfNeeded(stderrHandle, ref startupInfoEx.StartupInfo.hStdError, ref stderrRefAdded);

                // If STARTF_USESTDHANDLES is not set, the new process will inherit the standard handles.
                startupInfoEx.StartupInfo.dwFlags = Interop.Advapi32.StartupInfoOptions.STARTF_USESTDHANDLES;

                if (startInfo.WindowStyle != ProcessWindowStyle.Normal)
                {
                    startupInfoEx.StartupInfo.wShowWindow = (short)ProcessUtils.GetShowWindowFromWindowStyle(startInfo.WindowStyle);
                    startupInfoEx.StartupInfo.dwFlags |= Interop.Advapi32.StartupInfoOptions.STARTF_USESHOWWINDOW;
                }

                // set up the creation flags parameter
                int creationFlags = 0;
                if (startInfo.CreateNoWindow) creationFlags |= Interop.Advapi32.StartupInfoOptions.CREATE_NO_WINDOW;
                if (startInfo.CreateNewProcessGroup) creationFlags |= Interop.Advapi32.StartupInfoOptions.CREATE_NEW_PROCESS_GROUP;
                if (startInfo.StartDetached) creationFlags |= Interop.Advapi32.StartupInfoOptions.DETACHED_PROCESS;

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

                // By default, all handles are inherited.
                bool bInheritHandles = true;

                // Extended Startup Info can be configured only for the non-logon path
                if (!logon)
                {
                    if (ConfigureExtendedStartupInfo(inheritedHandles, killOnParentExit,
                        in startupInfoEx, ref attributeListBuffer,
                        ref handlesToInherit, ref handlesToRelease, ref bInheritHandles,
                        ref jobHandles))
                    {
                        creationFlags |= Interop.Kernel32.EXTENDED_STARTUPINFO_PRESENT;
                        startupInfoEx.StartupInfo.cb = sizeof(Interop.Kernel32.STARTUPINFOEX);
                        startupInfoEx.lpAttributeList = attributeListBuffer;
                    }
                }

                bool retVal;
                int errorCode = 0;

                if (logon)
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

                    // CreateProcessWithLogonW does not support STARTUPINFOEX. CreateProcessWithTokenW docs mention STARTUPINFOEX,
                    // but they don't mention that EXTENDED_STARTUPINFO_PRESENT is not supported anyway.
                    // CreateProcessAsUserW supports both, but it's too restrictive and simply different than CreateProcessWithLogonW in many ways.
                    Debug.Assert(!restrictInheritedHandles, "Inheriting handles is not supported when starting with alternate credentials.");
                    Debug.Assert(startupInfoEx.StartupInfo.cb == sizeof(Interop.Kernel32.STARTUPINFO));
                    Debug.Assert((creationFlags & Interop.Kernel32.EXTENDED_STARTUPINFO_PRESENT) == 0);

                    // When KillOnParentExit is set and we use CreateProcessWithLogonW (which doesn't support
                    // PROC_THREAD_ATTRIBUTE_JOB_LIST), we create the process suspended, assign it to the job,
                    // then resume it.
                    if (killOnParentExit)
                    {
                        creationFlags |= Interop.Advapi32.StartupInfoOptions.CREATE_SUSPENDED;
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
                            Interop.Kernel32.STARTUPINFO startupInfo = startupInfoEx.StartupInfo;

                            retVal = Interop.Advapi32.CreateProcessWithLogonW(
                                startInfo.UserName,
                                startInfo.Domain,
                                (passwordPtr != IntPtr.Zero) ? passwordPtr : (IntPtr)passwordInClearTextPtr,
                                logonFlags,
                                null,                // we don't need this since all the info is in commandLine
                                commandLinePtr,
                                creationFlags,
                                environmentBlockPtr,
                                workingDirectory,
                                &startupInfo,        // pointer to STARTUPINFO
                                &processInfo         // pointer to PROCESS_INFORMATION
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
                            bInheritHandles,     // handle inheritance flag
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

                if (!IsInvalidHandle(processInfo.hProcess))
                {
                    Marshal.InitHandle(procSH, processInfo.hProcess);

                    // When the process was started suspended for KillOnParentExit with CreateProcessWithLogonW,
                    // assign it to the job object and then resume the thread.
                    if (killOnParentExit && logon)
                    {
                        AssignJobAndResumeThread(processInfo.hThread, procSH);
                    }
                }

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
                if (!IsInvalidHandle(processInfo.hThread))
                    Interop.Kernel32.CloseHandle(processInfo.hThread);

                // If the provided handle was inheritable, just release the reference we added.
                // Otherwise if we created a valid duplicate, close it.

                if (stdinRefAdded)
                    stdinHandle.DangerousRelease();
                else if (!IsInvalidHandle(startupInfoEx.StartupInfo.hStdInput))
                    Interop.Kernel32.CloseHandle(startupInfoEx.StartupInfo.hStdInput);

                if (stdoutRefAdded)
                    stdoutHandle.DangerousRelease();
                else if (!IsInvalidHandle(startupInfoEx.StartupInfo.hStdOutput))
                    Interop.Kernel32.CloseHandle(startupInfoEx.StartupInfo.hStdOutput);

                if (stderrRefAdded)
                    stderrHandle.DangerousRelease();
                else if (!IsInvalidHandle(startupInfoEx.StartupInfo.hStdError))
                    Interop.Kernel32.CloseHandle(startupInfoEx.StartupInfo.hStdError);

                NativeMemory.Free(handlesToInherit);
                NativeMemory.Free(jobHandles);

                if (attributeListBuffer is not null)
                {
                    Interop.Kernel32.DeleteProcThreadAttributeList(attributeListBuffer);
                    NativeMemory.Free(attributeListBuffer);
                }

                if (handlesToRelease is not null)
                {
                    DisableInheritanceAndRelease(handlesToRelease);
                }

                if (restrictInheritedHandles)
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

        private static bool IsInvalidHandle(nint handle) => handle == -1 || handle == 0;

        private static void AddToInheritListIfValid(nint handle, Span<nint> handlesToInherit, ref int handleCount)
        {
            // The user can't specify invalid handle via ProcessStartInfo.Standard*Handle APIs.
            // However, Console.OpenStandard*Handle() can return INVALID_HANDLE_VALUE for a process
            // that was started with INVALID_HANDLE_VALUE as given standard handle.
            if (IsInvalidHandle(handle))
            {
                return;
            }

            handlesToInherit[handleCount++] = handle;
        }

        private static unsafe bool ConfigureExtendedStartupInfo(SafeHandle[]? inheritedHandles, bool killOnParentExit,
            in Interop.Kernel32.STARTUPINFOEX startupInfoEx, ref void* attributeListBuffer,
            ref nint* handlesToInherit, ref SafeHandle?[]? handlesToRelease, ref bool bInheritHandles,
            ref nint* jobHandles)
        {
            // Determine the number of attributes we need to set in the proc thread attribute list.
            int attributeCount = 0;

            int handleCount = 0;
            if (inheritedHandles is not null)
            {
                int maxHandleCount = 3 + inheritedHandles.Length;
                handlesToInherit = (IntPtr*)NativeMemory.Alloc((nuint)maxHandleCount, (nuint)sizeof(IntPtr));
                Span<nint> handlesToInheritSpan = new Span<nint>(handlesToInherit, maxHandleCount);

                // Add valid effective stdio handles (already made inheritable via DuplicateAsInheritableIfNeeded)
                AddToInheritListIfValid(startupInfoEx.StartupInfo.hStdInput, handlesToInheritSpan, ref handleCount);
                AddToInheritListIfValid(startupInfoEx.StartupInfo.hStdOutput, handlesToInheritSpan, ref handleCount);
                AddToInheritListIfValid(startupInfoEx.StartupInfo.hStdError, handlesToInheritSpan, ref handleCount);

                EnableInheritanceAndAddRef(inheritedHandles, handlesToInheritSpan, ref handleCount, ref handlesToRelease);

                if (handleCount == 0)
                {
                    // When InheritedHandles is set but handleCount is 0 (e.g. all standard handles are invalid),
                    // pass false to prevent all inheritable handles from leaking to the child.
                    bInheritHandles = false;
                }
                else
                {
                    attributeCount++; // PROC_THREAD_ATTRIBUTE_HANDLE_LIST
                }
            }

            if (killOnParentExit)
            {
                jobHandles = (IntPtr*)NativeMemory.Alloc(1, (nuint)sizeof(IntPtr));
                jobHandles[0] = s_killOnParentExitJob.Value.DangerousGetHandle();

                attributeCount++; // PROC_THREAD_ATTRIBUTE_JOB_LIST
            }

            if (attributeCount == 0)
            {
                return false;
            }

            nuint size = 0;
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
                null))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            if (killOnParentExit && !Interop.Kernel32.UpdateProcThreadAttribute(
                attributeListBuffer,
                0,
                (IntPtr)Interop.Kernel32.PROC_THREAD_ATTRIBUTE_JOB_LIST,
                jobHandles,
                (nuint)sizeof(IntPtr),
                null,
                null))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return true;
        }

        private static void EnableInheritanceAndAddRef(
            SafeHandle[] inheritedHandles,
            Span<nint> handlesToInherit,
            ref int handleCount,
            ref SafeHandle?[]? handlesToRelease)
        {
            handlesToRelease = new SafeHandle[inheritedHandles.Length];
            bool ignore = false;

            for (int i = 0; i < inheritedHandles.Length; i++)
            {
                SafeHandle safeHandle = inheritedHandles[i];
                Debug.Assert(safeHandle is not null && !safeHandle.IsInvalid);

                // Transfer ref ownership to handlesToRelease; DisableInheritanceAndRelease will release it.
                safeHandle.DangerousAddRef(ref ignore);
                handlesToRelease[i] = safeHandle;

                // Enable inheritance on this handle so the child process can use it.
                // It's defacto our validation that the handles passed in the allow list are actually inheritable handles.
                if (!Interop.Kernel32.SetHandleInformation(
                    safeHandle.DangerousGetHandle(),
                    Interop.Kernel32.HandleFlags.HANDLE_FLAG_INHERIT,
                    Interop.Kernel32.HandleFlags.HANDLE_FLAG_INHERIT))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                handlesToInherit[handleCount++] = safeHandle.DangerousGetHandle();
            }
        }

        private static void DisableInheritanceAndRelease(SafeHandle?[] handlesToRelease)
        {
            foreach (SafeHandle? safeHandle in handlesToRelease)
            {
                if (safeHandle is null)
                {
                    break;
                }

                // Remove the inheritance flag so they are not unintentionally inherited by other processes started after this point.
                // Since we used DangerousAddRef before, the handle cannot be closed at this point, so it's safe to call SetHandleInformation.
                bool success = Interop.Kernel32.SetHandleInformation(safeHandle.DangerousGetHandle(), Interop.Kernel32.HandleFlags.HANDLE_FLAG_INHERIT, 0);
                Debug.Assert(success);
                safeHandle.DangerousRelease();
            }
        }

        private static void AssignJobAndResumeThread(IntPtr hThread, SafeProcessHandle procSH)
        {
            Debug.Assert(!IsInvalidHandle(hThread), "Thread handle must be valid for suspended process.");

            try
            {
                if (!Interop.Kernel32.AssignProcessToJobObject(s_killOnParentExitJob.Value, procSH))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (Interop.Kernel32.ResumeThread(hThread) == 0xFFFFFFFF)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            catch
            {
                // If we fail to assign to the job or resume the thread, terminate the process.
                Interop.Kernel32.TerminateProcess(procSH, -1);
                throw;
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
