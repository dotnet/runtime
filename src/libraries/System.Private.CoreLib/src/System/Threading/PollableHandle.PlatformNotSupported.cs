// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Threading
{
    public sealed partial class PollableHandle : IDisposable
    {
        public bool InlineCompletions { get; set; }

        public static PollableHandle Create(SafeHandle handle, ref PollableHandle? field)
        {
            throw new PlatformNotSupportedException();
        }

        public bool IsReadReady(out int observedSequenceNumber) { throw new PlatformNotSupportedException(); }
        public bool IsWriteReady(out int observedSequenceNumber) { throw new PlatformNotSupportedException(); }

        public PollOperationAsyncResult ReadAsync(PollTriggeredOperation operation, int observedSequenceNumber, CancellationToken cancellationToken)
        {
            throw new PlatformNotSupportedException();
        }

        public PollOperationAsyncResult WriteAsync(PollTriggeredOperation operation, int observedSequenceNumber, CancellationToken cancellationToken)
        {
            throw new PlatformNotSupportedException();
        }

        public PollOperationSyncResult ReadSync(PollTriggeredOperation operation, int observedSequenceNumber, int timeout)
        {
            throw new PlatformNotSupportedException();
        }

        public PollOperationSyncResult WriteSync(PollTriggeredOperation operation, int observedSequenceNumber, int timeout)
        {
            throw new PlatformNotSupportedException();
        }

        public bool AbortAndDispose()
        {
            throw new PlatformNotSupportedException();
        }

        public void Dispose()
        {
        }
    }
}
