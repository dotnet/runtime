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

        private readonly SafeWaitHandle? _handle;
        private readonly bool _releaseRef;

        internal SafeProcessHandle(int processId, SafeWaitHandle handle) :
            this(handle.DangerousGetHandle(), ownsHandle: true)
        {
            ProcessId = processId;
            _handle = handle;
            handle.DangerousAddRef(ref _releaseRef);
        }

        protected override bool ReleaseHandle()
        {
            if (_releaseRef)
            {
                Debug.Assert(_handle is not null);
                _handle.DangerousRelease();
            }
            return true;
        }

        private static SafeProcessHandle OpenCore(int processId) => throw new NotImplementedException();

        private static SafeProcessHandle StartCore(ProcessStartOptions options, SafeFileHandle inputHandle, SafeFileHandle outputHandle, SafeFileHandle errorHandle, bool createSuspended) => throw new NotImplementedException();

        private ProcessExitStatus WaitForExitCore() => throw new NotImplementedException();

        private bool TryWaitForExitCore(int milliseconds, [NotNullWhen(true)] out ProcessExitStatus? exitStatus) => throw new NotImplementedException();

        private ProcessExitStatus WaitForExitOrKillOnTimeoutCore(int milliseconds) => throw new NotImplementedException();

        private Task<ProcessExitStatus> WaitForExitAsyncCore(CancellationToken cancellationToken) => throw new NotImplementedException();

        private Task<ProcessExitStatus> WaitForExitOrKillOnCancellationAsyncCore(CancellationToken cancellationToken) => throw new NotImplementedException();

        internal bool KillCore(bool throwOnError, bool entireProcessGroup = false) => throw new NotImplementedException();

        private void ResumeCore() => throw new NotImplementedException();

        private void SendSignalCore(PosixSignal signal, bool entireProcessGroup) => throw new NotImplementedException();
    }
}
