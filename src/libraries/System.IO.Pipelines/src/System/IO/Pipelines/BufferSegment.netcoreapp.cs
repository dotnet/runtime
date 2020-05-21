// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.IO.Pipelines
{
    internal sealed partial class BufferSegment : ReadOnlySequenceSegment<byte>
    {
        public void ResetMemory()
        {
            object? memoryOwner = Interlocked.Exchange(ref _memoryOwner, null);
            if (memoryOwner is null)
            {
                // Already returned block; this can happen from benign race between Advance and Complete
                // as block return is done outside a lock.
                return;
            }

            // Use a fast exact type check rather than checking the inheritance hierarchy
            // or following the interface mapping.
            if (memoryOwner.GetType() == typeof(byte[]))
            {
                byte[] poolArray = Unsafe.As<byte[]>(memoryOwner);
                ArrayPool<byte>.Shared.Return(poolArray);
            }
            else
            {
                Debug.Assert(memoryOwner is IMemoryOwner<byte>);
                Unsafe.As<IMemoryOwner<byte>>(memoryOwner).Dispose();
            }

            RunningIndex = 0;
            Memory = default;
            _end = 0;
            AvailableMemory = default;
        }
    }
}
