// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;

namespace System.Buffers
{
    [EventSource(
        Guid = "20b30044-b729-457e-8dda-8b41b5dd02e6", 
        Name = "System.Buffers.BufferPoolEventSource",
        LocalizationResources = "FxResources.System.Buffers.SR")]
    internal sealed class ArrayPoolEventSource : EventSource
    {
        internal readonly static ArrayPoolEventSource Log = new ArrayPoolEventSource();

        internal enum BufferAllocationReason : int
        {
            Pooled,
            OverMaximumSize,
            PoolExhausted
        }

        [Event(1, Level = EventLevel.Informational)]
        internal void BufferRented(int bufferId, int bufferSize, int poolId, int bucketId) { WriteEventHelper(1, bufferId, bufferSize, poolId, bucketId); }

        [Event(2, Level = EventLevel.Informational)]
        internal void BufferAllocated(int bufferId, int bufferSize, int poolId, int bucketId, BufferAllocationReason reason)
        {
            unsafe
            {
                 EventData* payload = stackalloc EventData[5];
                 payload[0].Size = sizeof(int);
                 payload[0].DataPointer = ((IntPtr) (&bufferId));
                 payload[1].Size = sizeof(int);
                 payload[1].DataPointer = ((IntPtr) (&bufferSize));
                 payload[2].Size = sizeof(int);
                 payload[2].DataPointer = ((IntPtr) (&poolId));
                 payload[3].Size = sizeof(int);
                 payload[3].DataPointer = ((IntPtr) (&bucketId));
                 payload[4].Size = sizeof(BufferAllocationReason);
                 payload[4].DataPointer = ((IntPtr) (&reason));
                 WriteEventCore(2, 5, payload);
             }
        }

        [Event(3, Level = EventLevel.Informational)]
        internal void BufferReturned(int bufferId, int poolId) { WriteEvent(3, bufferId, poolId); }

        [Event(4, Level = EventLevel.Warning)]
        internal void BucketExhausted(int bucketId, int bucketSize, int buffersInBucket, int poolId) { WriteEventHelper(4, bucketId, bucketSize, buffersInBucket, poolId); }
        
        [NonEvent]
        private unsafe void WriteEventHelper(int eventId, int arg0, int arg1, int arg2, int arg3)
        {
            if (IsEnabled())
            {
                unsafe
                {
                    EventData* payload = stackalloc EventData[4];
                    payload[0].Size = sizeof(int);
                    payload[0].DataPointer = ((IntPtr) (&arg0));
                    payload[1].Size = sizeof(int);
                    payload[1].DataPointer = ((IntPtr) (&arg1));
                    payload[2].Size = sizeof(int);
                    payload[2].DataPointer = ((IntPtr) (&arg2));
                    payload[3].Size = sizeof(int);
                    payload[3].DataPointer = ((IntPtr) (&arg3));
                    WriteEventCore(eventId, 4, payload);
                }
            }
        }
    }
}
