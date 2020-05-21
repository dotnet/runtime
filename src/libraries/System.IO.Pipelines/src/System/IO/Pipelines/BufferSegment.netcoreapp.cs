// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Pipelines
{
    internal sealed partial class BufferSegment : ReadOnlySequenceSegment<byte>
    {
        public void ResetMemory()
        {
            object memoryOwner = _memoryOwner!;

            // Order of below field clears is significant as it clears in a sequential order
            // https://github.com/dotnet/corefx/pull/35256#issuecomment-462800477
            Next = null;
            RunningIndex = 0;
            Memory = default;
            _memoryOwner = null;
            _next = null;
            _end = 0;
            AvailableMemory = default;

            // Return the memory, use a fast exact type check rather than checking the inheritance hierarchy
            // or following the interface mapping.
            if (memoryOwner.GetType() == typeof(byte[]))
            {
                byte[] poolArray = (byte[])memoryOwner;
                ArrayPool<byte>.Shared.Return(poolArray);
            }
            else
            {
                Debug.Assert(memoryOwner is IMemoryOwner<byte>);
                Unsafe.As<IMemoryOwner<byte>>(memoryOwner).Dispose();
            }
        }
    }
}
