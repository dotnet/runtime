// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // On Windows, SafeProcessHandle represents the actual OS handle for the process.
        // On Unix, there's no such concept.  Instead, the implementation manufactures
        // a WaitHandle that it manually sets when the process completes; SafeProcessHandle
        // then just wraps that same WaitHandle instance.  This allows consumers that use
        // Process.{Safe}Handle to initialize and use a WaitHandle to successfully use it on
        // Unix as well to wait for the process to complete.

        // Use int.MinValue instead of -1 because SafeProcessHandle derives from SafeHandleZeroOrMinusOneIsInvalid.
        internal const int NoPidFd = int.MinValue;

        private readonly SafeWaitHandle? _handle;
        private readonly bool _releaseRef;

        internal SafeProcessHandle(int processId, SafeWaitHandle handle) :
            this(handle.DangerousGetHandle(), ownsHandle: true)
        {
            ProcessId = processId;
            _handle = handle;
            handle.DangerousAddRef(ref _releaseRef);
        }

        private SafeProcessHandle(int pidfd, int pid)
            : this(existingHandle: (IntPtr)pidfd, ownsHandle: true)
        {
            ProcessId = pid;
        }

        protected override bool ReleaseHandle()
        {
            if (_releaseRef)
            {
                Debug.Assert(_handle is not null);
                _handle.DangerousRelease();
                return true;
            }

            return (int)handle switch
            {
                NoPidFd => true,
                _ => Interop.Sys.Close((IntPtr)(int)handle) == 0,
            };
        }

        private static SafeProcessHandle OpenCore(int processId)
        {
            int result = Interop.Sys.OpenProcess(processId, out int pidfd);

            if (result == -1)
            {
                throw new Win32Exception();
            }

            return new SafeProcessHandle(pidfd, processId);
        }

        private static SafeProcessHandle StartCore(ProcessStartOptions options, SafeFileHandle inputHandle, SafeFileHandle outputHandle, SafeFileHandle errorHandle, bool createSuspended)
        {
            // Prepare arguments array (argv)
            string[] argv = [options.FileName, .. options.Arguments];

            // Prepare environment array (envp) only if the user has accessed it
            // If not accessed, pass null to use the current environment (environ)
            string[]? envp = options.HasEnvironmentBeenAccessed ? ProcessUtils.CreateEnvp(options.Environment) : null;

            // Get file descriptors for stdin/stdout/stderr
            int stdInFd = (int)inputHandle.DangerousGetHandle();
            int stdOutFd = (int)outputHandle.DangerousGetHandle();
            int stdErrFd = (int)errorHandle.DangerousGetHandle();

            return StartProcessInternal(options.FileName, argv, envp, options, stdInFd, stdOutFd, stdErrFd, createSuspended);
        }

        private static unsafe SafeProcessHandle StartProcessInternal(string resolvedPath, string[] argv, string[]? envp,
            ProcessStartOptions options, int stdinFd, int stdoutFd, int stderrFd, bool createSuspended)
        {
            byte** argvPtr = null;
            byte** envpPtr = null;
            int* inheritedHandlesPtr = null;
            int inheritedHandlesCount = 0;

            try
            {
                ProcessUtils.AllocNullTerminatedArray(argv, ref argvPtr);

                // Only allocate envp if the user has accessed the environment
                if (envp is not null)
                {
                    ProcessUtils.AllocNullTerminatedArray(envp, ref envpPtr);
                }

                // Allocate and copy inherited handles if provided
                if (options.HasInheritedHandlesBeenAccessed && options.InheritedHandles.Count > 0)
                {
                    inheritedHandlesCount = options.InheritedHandles.Count;
                    inheritedHandlesPtr = (int*)NativeMemory.Alloc((nuint)inheritedHandlesCount, (nuint)sizeof(int));

                    for (int i = 0; i < inheritedHandlesCount; i++)
                    {
                        inheritedHandlesPtr[i] = (int)options.InheritedHandles[i].DangerousGetHandle();
                    }
                }

                // Call native library to spawn process
                int result = Interop.Sys.SpawnProcess(
                    resolvedPath,
                    argvPtr,
                    envpPtr,
                    options.WorkingDirectory,
                    inheritedHandlesPtr,
                    inheritedHandlesCount,
                    stdinFd,
                    stdoutFd,
                    stderrFd,
                    options.KillOnParentExit ? 1 : 0,
                    createSuspended ? 1 : 0,
                    options.CreateNewProcessGroup ? 1 : 0,
                    out int pid,
                    out int pidfd);

                if (result == -1)
                {
                    throw new Win32Exception();
                }

                return new SafeProcessHandle(pidfd == -1 ? NoPidFd : pidfd, pid);
            }
            finally
            {
                ProcessUtils.FreeArray(envpPtr, envp?.Length ?? 0);
                ProcessUtils.FreeArray(argvPtr, argv.Length);
                NativeMemory.Free(inheritedHandlesPtr);
            }
        }

        private ProcessExitStatus WaitForExitCore()
        {
            switch (Interop.Sys.WaitForExitAndReap(this, ProcessId, out int exitCode, out int rawSignal))
            {
                case -1:
                    throw new Win32Exception();
                default:
                    return new(exitCode, false, rawSignal != 0 ? (PosixSignal)rawSignal : null);
            }
        }

        private bool TryWaitForExitCore(int milliseconds, [NotNullWhen(true)] out ProcessExitStatus? exitStatus)
        {
            switch (Interop.Sys.TryWaitForExit(this, ProcessId, milliseconds, out int exitCode, out int rawSignal))
            {
                case -1:
                    throw new Win32Exception();
                case 1: // timeout
                    exitStatus = null;
                    return false;
                default:
                    exitStatus = new(exitCode, false, rawSignal != 0 ? (PosixSignal)rawSignal : null);
                    return true;
            }
        }

        private ProcessExitStatus WaitForExitOrKillOnTimeoutCore(int milliseconds)
        {
            switch (Interop.Sys.WaitForExitOrKillOnTimeout(this, ProcessId, milliseconds, out int exitCode, out int rawSignal, out int hasTimedout))
            {
                case -1:
                    throw new Win32Exception();
                default:
                    return new(exitCode, hasTimedout == 1, rawSignal != 0 ? (PosixSignal)rawSignal : null);
            }
        }

        private async Task<ProcessExitStatus> WaitForExitAsyncCore(CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                return await Task.Run(WaitForExitCore, cancellationToken).ConfigureAwait(false);
            }

            CreatePipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle);

            using (readHandle)
            using (writeHandle)
            {
                using CancellationTokenRegistration registration = cancellationToken.Register(static state =>
                {
                    ((SafeFileHandle)state!).Close(); // Close the write end of the pipe to signal cancellation
                }, writeHandle);

                return await Task.Run(() =>
                {
                    switch (Interop.Sys.TryWaitForExitCancellable(this, ProcessId, (int)readHandle.DangerousGetHandle(), out int exitCode, out int rawSignal))
                    {
                        case -1:
                            throw new Win32Exception();
                        case 1: // canceled
                            throw new OperationCanceledException(cancellationToken);
                        default:
                            return new ProcessExitStatus(exitCode, false, rawSignal != 0 ? (PosixSignal)rawSignal : null);
                    }
                }, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<ProcessExitStatus> WaitForExitOrKillOnCancellationAsyncCore(CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                return await Task.Run(WaitForExitCore, cancellationToken).ConfigureAwait(false);
            }

            CreatePipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle);

            using (readHandle)
            using (writeHandle)
            {
                using CancellationTokenRegistration registration = cancellationToken.Register(static state =>
                {
                    ((SafeFileHandle)state!).Close(); // Close the write end of the pipe to signal cancellation
                }, writeHandle);

                return await Task.Run(() =>
                {
                    switch (Interop.Sys.TryWaitForExitCancellable(this, ProcessId, (int)readHandle.DangerousGetHandle(), out int exitCode, out int rawSignal))
                    {
                        case -1:
                            throw new Win32Exception();
                        case 1: // canceled
                            bool wasKilled = KillCore(throwOnError: false);
                            ProcessExitStatus status = WaitForExitCore();
                            return new ProcessExitStatus(status.ExitCode, wasKilled, status.Signal);
                        default:
                            return new ProcessExitStatus(exitCode, false, rawSignal != 0 ? (PosixSignal)rawSignal : null);
                    }
                }, cancellationToken).ConfigureAwait(false);
            }
        }

        private static unsafe void CreatePipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle)
        {
            int* fds = stackalloc int[2];
            if (Interop.Sys.Pipe(fds, Interop.Sys.PipeFlags.O_CLOEXEC) != 0)
            {
                throw new Win32Exception();
            }

            readHandle = new SafeFileHandle((IntPtr)fds[Interop.Sys.ReadEndOfPipe], ownsHandle: true);
            writeHandle = new SafeFileHandle((IntPtr)fds[Interop.Sys.WriteEndOfPipe], ownsHandle: true);
        }

        internal bool KillCore(bool throwOnError, bool entireProcessGroup = false)
        {
            // If entireProcessGroup is true, send to -pid (negative pid), don't use pidfd.
            int pidfd = entireProcessGroup ? NoPidFd : (int)this.handle;
            int pid = entireProcessGroup ? -ProcessId : ProcessId;
            int result = Interop.Sys.SendSignal(pidfd, pid, PosixSignal.SIGKILL);
            if (result == 0)
            {
                return true;
            }

            const int ESRCH = 3;
            int errno = Marshal.GetLastPInvokeError();
            if (errno == ESRCH)
            {
                return false; // Process already exited
            }

            if (!throwOnError)
            {
                return false;
            }

            throw new Win32Exception(errno);
        }

        private void ResumeCore()
        {
            // Resume a suspended process by sending SIGCONT
            int result = Interop.Sys.SendSignal((int)this.handle, ProcessId, PosixSignal.SIGCONT);
            if (result == 0)
            {
                return;
            }

            throw new Win32Exception();
        }

        private void SendSignalCore(PosixSignal signal, bool entireProcessGroup)
        {
            // If entireProcessGroup is true, send to -pid (negative pid), don't use pidfd.
            int pidfd = entireProcessGroup ? NoPidFd : (int)this.handle;
            int pid = entireProcessGroup ? -ProcessId : ProcessId;
            int result = Interop.Sys.SendSignal(pidfd, pid, signal);

            if (result == 0)
            {
                return;
            }

            throw new Win32Exception();
        }
    }
}
