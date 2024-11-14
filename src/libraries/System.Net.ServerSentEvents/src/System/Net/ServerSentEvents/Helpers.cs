// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.ServerSentEvents
{
    internal static class Helpers
    {
        public static unsafe void WriteAsUtf8String(this IBufferWriter<byte> bufferWriter, ReadOnlySpan<char> value)
        {
            if (value.IsEmpty)
            {
                return;
            }

            int maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
            Span<byte> buffer = bufferWriter.GetSpan(maxByteCount);
            int bytesWritten;
#if NET
            bytesWritten = Encoding.UTF8.GetBytes(value, buffer);
#else
            fixed (char* chars = value)
            fixed (byte* bytes = buffer)
            {
                bytesWritten = Encoding.UTF8.GetBytes(chars, value.Length, bytes, maxByteCount);
            }
#endif
            bufferWriter.Advance(bytesWritten);
        }

        public static void ValidateParameterDoesNotContainLineBreaks(string? input, string paramName)
        {
            if (input?.Contains('\n') is true)
            {
                Throw(paramName);
                static void Throw(string parameterName) => throw new ArgumentException(SR.ArgumentException_MustNotContainLineBreaks, parameterName);
            }
        }

#if !NET
        public static bool Contains(this string text, char character) => text.IndexOf(character) >= 0;

        public static ValueTask WriteAsync(this Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment))
            {
                return new ValueTask(stream.WriteAsync(segment.Array, segment.Offset, segment.Count, cancellationToken));
            }
            else
            {
                return WriteAsyncUsingPooledBuffer(stream, buffer, cancellationToken);

                static async ValueTask WriteAsyncUsingPooledBuffer(Stream stream, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
                {
                    byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
                    buffer.Span.CopyTo(sharedBuffer);
                    try
                    {
                        await stream.WriteAsync(sharedBuffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(sharedBuffer);
                    }
                }
            }
        }
#endif
    }
}
