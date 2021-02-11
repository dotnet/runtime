// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net
{
    public static class StreamArrayExtensions
    {
        public static ValueTask WriteAsync(this Stream stream, ReadOnlyMemory<byte> memory)
        {
            bool isArray = MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment);
            Assert.True(isArray);

            return new ValueTask(stream.WriteAsync(segment.Array, segment.Offset, segment.Count));
        }

        public static ValueTask WriteAsync(this StreamWriter writer, string text)
        {
            return new ValueTask(writer.WriteAsync(text.ToCharArray(), 0, text.Length));
        }

        public static ValueTask<int> ReadAsync(this Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            bool isArray = MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment);
            Assert.True(isArray);

            return new ValueTask<int>(stream.ReadAsync(segment.Array, segment.Offset, segment.Count, cancellationToken));
        }
    }
}
