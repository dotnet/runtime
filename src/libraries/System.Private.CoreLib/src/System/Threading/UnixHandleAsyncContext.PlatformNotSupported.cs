// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Threading
{
    public sealed partial class UnixHandleAsyncContext
    {
        public static bool IsSupported => false;

        public UnixHandleAsyncContext(SafeHandle handle) { throw new PlatformNotSupportedException(); }

        public bool InlineCompletions { get; set; }

        public bool IsReadReady(out int observedSequenceNumber) { throw new PlatformNotSupportedException(); }
        public bool IsWriteReady(out int observedSequenceNumber) { throw new PlatformNotSupportedException(); }

        public AsyncResult StartAsyncRead(Operation operation, int observedSequenceNumber, CancellationToken cancellationToken)
        {
            throw new PlatformNotSupportedException();
        }

        public AsyncResult StartAsyncWrite(Operation operation, int observedSequenceNumber, CancellationToken cancellationToken)
        {
            throw new PlatformNotSupportedException();
        }

        public SyncResult Read(Operation operation, int observedSequenceNumber, int timeout)
        {
            throw new PlatformNotSupportedException();
        }

        public SyncResult Write(Operation operation, int observedSequenceNumber, int timeout)
        {
            throw new PlatformNotSupportedException();
        }

        public bool AbortAndDispose()
        {
            throw new PlatformNotSupportedException();
        }
    }
}
