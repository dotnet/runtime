// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Static job object used for KillOnParentExit functionality
        // All child processes with KillOnParentExit=true are assigned to this job
        // Note: The job handle is intentionally never closed - it should live for the
        // lifetime of the process. When this process exits, the job object is destroyed
        // by the OS, which terminates all child processes in the job.
        private static readonly Lazy<IntPtr> s_killOnParentExitJob = new(CreateKillOnParentExitJob);

        // Thread handle for suspended processes (only used on Windows)
        private IntPtr _threadHandle;

        // Job handle for CreateNewProcessGroup functionality (only used on Windows)
        // This is specific to each process and is used to terminate the entire process group
        private IntPtr _processGroupJobHandle;

        /// <summary>
        /// Gets the process ID.
        /// </summary>
        public int ProcessId { get; private set; }

        private SafeProcessHandle(IntPtr processHandle, IntPtr threadHandle, IntPtr processGroupJobHandle, int processId)
            : base(ownsHandle: true)
        {
            SetHandle(processHandle);
            _threadHandle = threadHandle;
            _processGroupJobHandle = processGroupJobHandle;
            ProcessId = processId;
        }

        private static IntPtr CreateKillOnParentExitJob()
        {
            IntPtr jobHandle = Interop.Kernel32.CreateJobObjectW(IntPtr.Zero, IntPtr.Zero);
            if (jobHandle == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            Interop.Kernel32.JOBOBJECT_EXTENDED_LIMIT_INFORMATION limitInfo = default;
            limitInfo.BasicLimitInformation.LimitFlags = Interop.Kernel32.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

            if (!Interop.Kernel32.SetInformationJobObject(
                jobHandle,
                Interop.Kernel32.JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation,
                ref limitInfo,
                (uint)Marshal.SizeOf<Interop.Kernel32.JOBOBJECT_EXTENDED_LIMIT_INFORMATION>()))
            {
                Interop.Kernel32.CloseHandle(jobHandle);
                throw new Win32Exception();
            }

            return jobHandle;
        }

        protected override bool ReleaseHandle()
        {
            if (_threadHandle != IntPtr.Zero)
            {
                Interop.Kernel32.CloseHandle(_threadHandle);
            }

            if (_processGroupJobHandle != IntPtr.Zero)
            {
                Interop.Kernel32.CloseHandle(_processGroupJobHandle);
            }

            return Interop.Kernel32.CloseHandle(handle);
        }

        internal int GetExitCode()
        {
            if (!Interop.Kernel32.GetExitCodeProcess(this, out int exitCode))
            {
                throw new Win32Exception();
            }
            else if (exitCode == Interop.Kernel32.HandleOptions.STILL_ACTIVE)
            {
                throw new InvalidOperationException();
            }

            return exitCode;
        }

        private bool TryGetExitCodeCore(out int exitCode, out PosixSignal? signal)
        {
            signal = default;

            return Interop.Kernel32.GetExitCodeProcess(this, out exitCode)
                && exitCode != Interop.Kernel32.HandleOptions.STILL_ACTIVE;
        }

        private static unsafe SafeProcessHandle StartCore(ProcessStartOptions options, SafeFileHandle inputHandle, SafeFileHandle outputHandle, SafeFileHandle errorHandle, bool createSuspended)
        {
            Interop.Kernel32.STARTUPINFOEX startupInfoEx = default;
            Interop.Kernel32.PROCESS_INFORMATION processInfo = default;
            Interop.Kernel32.SECURITY_ATTRIBUTES unused_SecAttrs = default;
            SafeProcessHandle? procSH = null;
            IntPtr currentProcHandle = Interop.Kernel32.GetCurrentProcess();
            IntPtr attributeListBuffer = IntPtr.Zero;
            Interop.Kernel32.LPPROC_THREAD_ATTRIBUTE_LIST attributeList = default;

            using SafeFileHandle duplicatedInput = Duplicate(inputHandle, currentProcHandle);
            using SafeFileHandle duplicatedOutput = inputHandle.DangerousGetHandle() == outputHandle.DangerousGetHandle()
                ? duplicatedInput
                : Duplicate(outputHandle, currentProcHandle);
            using SafeFileHandle duplicatedError = outputHandle.DangerousGetHandle() == errorHandle.DangerousGetHandle()
                ? duplicatedOutput
                : (inputHandle.DangerousGetHandle() == errorHandle.DangerousGetHandle()
                    ? duplicatedInput
                    : Duplicate(errorHandle, currentProcHandle));

            int maxHandleCount = 3 + (options.HasInheritedHandlesBeenAccessed ? options.InheritedHandles.Count : 0);

            IntPtr heapHandlesPtr = Marshal.AllocHGlobal(maxHandleCount * sizeof(IntPtr));
            IntPtr* handlesToInherit = (IntPtr*)heapHandlesPtr;
            IntPtr processGroupJobHandle = IntPtr.Zero;

            try
            {
                int handleCount = 0;

                IntPtr inputPtr = duplicatedInput.DangerousGetHandle();
                IntPtr outputPtr = duplicatedOutput.DangerousGetHandle();
                IntPtr errorPtr = duplicatedError.DangerousGetHandle();

                PrepareHandleAllowList(options, handlesToInherit, ref handleCount, inputPtr, outputPtr, errorPtr);

                if (options.CreateNewProcessGroup)
                {
                    processGroupJobHandle = Interop.Kernel32.CreateJobObjectW(IntPtr.Zero, IntPtr.Zero);
                    if (processGroupJobHandle == IntPtr.Zero)
                    {
                        throw new Win32Exception();
                    }
                }

                int attributeCount = 1; // Always need handle list
                if (options.KillOnParentExit || options.CreateNewProcessGroup)
                    attributeCount++;

                IntPtr size = IntPtr.Zero;
                Interop.Kernel32.LPPROC_THREAD_ATTRIBUTE_LIST emptyList = default;
                Interop.Kernel32.InitializeProcThreadAttributeList(emptyList, attributeCount, 0, ref size);

                attributeListBuffer = Marshal.AllocHGlobal(size);
                attributeList.AttributeList = attributeListBuffer;

                if (!Interop.Kernel32.InitializeProcThreadAttributeList(attributeList, attributeCount, 0, ref size))
                {
                    throw new Win32Exception();
                }

                if (!Interop.Kernel32.UpdateProcThreadAttribute(
                    attributeList,
                    0,
                    (IntPtr)Interop.Kernel32.PROC_THREAD_ATTRIBUTE_HANDLE_LIST,
                    handlesToInherit,
                    (IntPtr)(handleCount * sizeof(IntPtr)),
                    null,
                    IntPtr.Zero))
                {
                    throw new Win32Exception();
                }

                if (options.KillOnParentExit || options.CreateNewProcessGroup)
                {
                    IntPtr* pJobHandle = stackalloc IntPtr[2];
                    int jobsCount = 0;

                    if (options.KillOnParentExit)
                        pJobHandle[jobsCount++] = s_killOnParentExitJob.Value;
                    if (options.CreateNewProcessGroup)
                        pJobHandle[jobsCount++] = processGroupJobHandle;

                    if (!Interop.Kernel32.UpdateProcThreadAttribute(
                        attributeList,
                        0,
                        Interop.Kernel32.PROC_THREAD_ATTRIBUTE_JOB_LIST,
                        pJobHandle,
                        jobsCount * sizeof(IntPtr),
                        null,
                        IntPtr.Zero))
                    {
                        throw new Win32Exception();
                    }
                }

                startupInfoEx.lpAttributeList = attributeList;
                startupInfoEx.StartupInfo.cb = sizeof(Interop.Kernel32.STARTUPINFOEX);
                startupInfoEx.StartupInfo.hStdInput = inputPtr;
                startupInfoEx.StartupInfo.hStdOutput = outputPtr;
                startupInfoEx.StartupInfo.hStdError = errorPtr;
                startupInfoEx.StartupInfo.dwFlags = Interop.Advapi32.StartupInfoOptions.STARTF_USESTDHANDLES;

                int creationFlags = Interop.Kernel32.EXTENDED_STARTUPINFO_PRESENT;
                if (createSuspended) creationFlags |= Interop.Advapi32.StartupInfoOptions.CREATE_SUSPENDED;
                if (options.CreateNewProcessGroup) creationFlags |= Interop.Advapi32.StartupInfoOptions.CREATE_NEW_PROCESS_GROUP;

                string? environmentBlock = null;
                if (options.HasEnvironmentBeenAccessed)
                {
                    creationFlags |= Interop.Advapi32.StartupInfoOptions.CREATE_UNICODE_ENVIRONMENT;
                    environmentBlock = Process.GetEnvironmentVariablesBlock(options.Environment);
                }

                string? workingDirectory = options.WorkingDirectory;
                int errorCode = 0;

                ValueStringBuilder applicationName = new(stackalloc char[256]);
                ValueStringBuilder commandLine = new(stackalloc char[256]);
                try
                {
                    ProcessUtils.BuildArgs(options, ref applicationName, ref commandLine);

                    fixed (char* environmentBlockPtr = environmentBlock)
                    fixed (char* applicationNamePtr = &applicationName.GetPinnableReference())
                    fixed (char* commandLinePtr = &commandLine.GetPinnableReference())
                    {
                        bool retVal = Interop.Kernel32.CreateProcess(
                            applicationNamePtr,
                            commandLinePtr,
                            ref unused_SecAttrs,
                            ref unused_SecAttrs,
                            true,
                            creationFlags,
                            environmentBlockPtr,
                            workingDirectory,
                            ref startupInfoEx,
                            ref processInfo
                        );
                        if (!retVal)
                            errorCode = Marshal.GetLastPInvokeError();
                    }
                }
                finally
                {
                    applicationName.Dispose();
                    commandLine.Dispose();
                }

                if (processInfo.hProcess != IntPtr.Zero && processInfo.hProcess != new IntPtr(-1))
                {
                    if (createSuspended && processInfo.hThread != IntPtr.Zero && processInfo.hThread != new IntPtr(-1))
                    {
                        procSH = new(processInfo.hProcess, processInfo.hThread, processGroupJobHandle, processInfo.dwProcessId);
                    }
                    else
                    {
                        procSH = new(processInfo.hProcess, IntPtr.Zero, processGroupJobHandle, processInfo.dwProcessId);
                        if (processInfo.hThread != IntPtr.Zero && processInfo.hThread != new IntPtr(-1))
                            Interop.Kernel32.CloseHandle(processInfo.hThread);
                    }
                }
                else if (processInfo.hThread != IntPtr.Zero && processInfo.hThread != new IntPtr(-1))
                {
                    Interop.Kernel32.CloseHandle(processInfo.hThread);
                }

                if (procSH is null)
                {
                    throw new Win32Exception(errorCode);
                }
            }
            catch
            {
                procSH?.Dispose();

                if (processGroupJobHandle != IntPtr.Zero)
                {
                    Interop.Kernel32.CloseHandle(processGroupJobHandle);
                }

                throw;
            }
            finally
            {
                Marshal.FreeHGlobal(heapHandlesPtr);

                if (attributeListBuffer != IntPtr.Zero)
                {
                    Interop.Kernel32.DeleteProcThreadAttributeList(attributeList);
                    Marshal.FreeHGlobal(attributeListBuffer);
                }
                Interop.Kernel32.CloseHandle(currentProcHandle);
            }

            return procSH;

            static SafeFileHandle Duplicate(SafeFileHandle sourceHandle, nint currentProcHandle)
            {
                if (!Interop.Kernel32.DuplicateHandle(
                    currentProcHandle,
                    sourceHandle,
                    currentProcHandle,
                    out SafeFileHandle duplicated,
                    0,
                    true,
                    Interop.Kernel32.HandleOptions.DUPLICATE_SAME_ACCESS))
                {
                    throw new Win32Exception();
                }

                return duplicated;
            }
        }

        private static unsafe void PrepareHandleAllowList(ProcessStartOptions options, IntPtr* handlesToInherit, ref int handleCount, IntPtr inputPtr, IntPtr outputPtr, IntPtr errorPtr)
        {
            handlesToInherit[handleCount++] = inputPtr;
            if (outputPtr != inputPtr)
                handlesToInherit[handleCount++] = outputPtr;
            if (errorPtr != inputPtr && errorPtr != outputPtr)
                handlesToInherit[handleCount++] = errorPtr;

            if (options.HasInheritedHandlesBeenAccessed)
            {
                foreach (SafeHandle handle in options.InheritedHandles)
                {
                    IntPtr handlePtr = handle.DangerousGetHandle();

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
                        if (!Interop.Kernel32.GetHandleInformation(handlePtr, out int flags))
                        {
                            throw new Win32Exception();
                        }

                        if ((flags & Interop.Kernel32.HandleOptions.HANDLE_FLAG_INHERIT) == 0)
                        {
                            if (!Interop.Kernel32.SetHandleInformation(
                                handlePtr,
                                Interop.Kernel32.HandleOptions.HANDLE_FLAG_INHERIT,
                                Interop.Kernel32.HandleOptions.HANDLE_FLAG_INHERIT))
                            {
                                throw new Win32Exception();
                            }
                        }

                        handlesToInherit[handleCount++] = handlePtr;
                    }
                }
            }
        }

        private ProcessExitStatus WaitForExitCore()
        {
            using Interop.Kernel32.ProcessWaitHandle processWaitHandle = new(this);
            processWaitHandle.WaitOne(Timeout.Infinite);

            return new(GetExitCode(), false);
        }

        private bool TryWaitForExitCore(int milliseconds, [NotNullWhen(true)] out ProcessExitStatus? exitStatus)
        {
            using Interop.Kernel32.ProcessWaitHandle processWaitHandle = new(this);
            if (!processWaitHandle.WaitOne(milliseconds))
            {
                exitStatus = null;
                return false;
            }

            exitStatus = new(GetExitCode(), false);
            return true;
        }

        private ProcessExitStatus WaitForExitOrKillOnTimeoutCore(int milliseconds)
        {
            bool wasKilledOnTimeout = false;
            using Interop.Kernel32.ProcessWaitHandle processWaitHandle = new(this);
            if (!processWaitHandle.WaitOne(milliseconds))
            {
                wasKilledOnTimeout = KillCore(throwOnError: false);
            }

            return new(GetExitCode(), wasKilledOnTimeout);
        }

        private async Task<ProcessExitStatus> WaitForExitAsyncCore(CancellationToken cancellationToken)
        {
            using Interop.Kernel32.ProcessWaitHandle processWaitHandle = new(this);

            TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            RegisteredWaitHandle? registeredWaitHandle = null;
            CancellationTokenRegistration ctr = default;

            try
            {
                registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(
                    processWaitHandle,
                    (state, timedOut) => ((TaskCompletionSource<bool>)state!).TrySetResult(true),
                    tcs,
                    Timeout.Infinite,
                    executeOnlyOnce: true);

                if (cancellationToken.CanBeCanceled)
                {
                    ctr = cancellationToken.Register(
                        static state =>
                        {
                            var taskSource = (TaskCompletionSource<bool>)state!;
                            taskSource.TrySetCanceled();
                        },
                        tcs);
                }

                await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                ctr.Dispose();
                registeredWaitHandle?.Unregister(null);
            }

            return new(GetExitCode(), false);
        }

        private async Task<ProcessExitStatus> WaitForExitOrKillOnCancellationAsyncCore(CancellationToken cancellationToken)
        {
            using Interop.Kernel32.ProcessWaitHandle processWaitHandle = new(this);

            TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            RegisteredWaitHandle? registeredWaitHandle = null;
            CancellationTokenRegistration ctr = default;
            StrongBox<bool> wasKilledBox = new(false);

            try
            {
                registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(
                    processWaitHandle,
                    (state, timedOut) => ((TaskCompletionSource<bool>)state!).TrySetResult(true),
                    tcs,
                    Timeout.Infinite,
                    executeOnlyOnce: true);

                if (cancellationToken.CanBeCanceled)
                {
                    ctr = cancellationToken.Register(
                        static state =>
                        {
                            var (handle, taskSource, wasCancelled) = ((SafeProcessHandle, TaskCompletionSource<bool>, StrongBox<bool>))state!;
                            wasCancelled.Value = handle.KillCore(throwOnError: false);
                            taskSource.TrySetResult(true);
                        },
                        (this, tcs, wasKilledBox));
                }

                await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                ctr.Dispose();
                registeredWaitHandle?.Unregister(null);
            }

            return new(GetExitCode(), wasKilledBox.Value);
        }

        internal bool KillCore(bool throwOnError, bool entireProcessGroup = false)
        {
            if (entireProcessGroup && _processGroupJobHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException(SR.KillProcessGroupWithoutNewProcessGroup);
            }

            if (entireProcessGroup
                ? Interop.Kernel32.TerminateJobObject(_processGroupJobHandle, unchecked((uint)-1))
                : Interop.Kernel32.TerminateProcess(this, exitCode: -1))
            {
                return true;
            }

            int error = Marshal.GetLastPInvokeError();
            return error switch
            {
                Interop.Errors.ERROR_SUCCESS => true,
                Interop.Errors.ERROR_ACCESS_DENIED => false,
                _ when !throwOnError => false,
                _ => throw new Win32Exception(error),
            };
        }

        private void ResumeCore()
        {
            if (_threadHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException(SR.CannotResumeNonSuspendedProcess);
            }

            int result = Interop.Kernel32.ResumeThread(_threadHandle);
            if (result == -1)
            {
                throw new Win32Exception();
            }
        }

        private void SendSignalCore(PosixSignal signal, bool entireProcessGroup)
        {
            if (signal == PosixSignal.SIGKILL)
            {
                KillCore(throwOnError: true, entireProcessGroup);
                return;
            }

            int ctrlEvent = signal switch
            {
                PosixSignal.SIGINT => Interop.Kernel32.CTRL_C_EVENT,
                PosixSignal.SIGQUIT => Interop.Kernel32.CTRL_BREAK_EVENT,
                _ => throw new ArgumentException(SR.Format(SR.SignalNotSupportedOnWindows, signal), nameof(signal))
            };

            if (!Interop.Kernel32.GenerateConsoleCtrlEvent(ctrlEvent, ProcessId))
            {
                throw new Win32Exception();
            }
        }

        private static SafeProcessHandle OpenCore(int processId)
        {
            const int desiredAccess = Interop.Advapi32.ProcessOptions.PROCESS_QUERY_LIMITED_INFORMATION
                | Interop.Advapi32.ProcessOptions.SYNCHRONIZE
                | Interop.Advapi32.ProcessOptions.PROCESS_TERMINATE;

            SafeProcessHandle safeHandle = Interop.Kernel32.OpenProcess(desiredAccess, inherit: false, processId);

            if (safeHandle.IsInvalid)
            {
                int error = Marshal.GetLastPInvokeError();
                safeHandle.Dispose();
                throw new Win32Exception(error);
            }

            // Transfer ownership: take the handle from the returned SafeProcessHandle and
            // create a new one with the ProcessId set properly.
            IntPtr rawHandle = safeHandle.DangerousGetHandle();
            safeHandle.SetHandleAsInvalid(); // Prevent the original from closing it
            return new SafeProcessHandle(rawHandle, IntPtr.Zero, IntPtr.Zero, processId);
        }
    }
}
