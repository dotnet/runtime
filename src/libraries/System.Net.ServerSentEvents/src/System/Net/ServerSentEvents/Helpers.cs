// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.ServerSentEvents
{
    internal static class Helpers
    {
        public static void WriteUtf8Number(this IBufferWriter<byte> writer, long value)
        {
#if NET
            const int MaxDecimalDigits = 20;
            Span<byte> buffer = writer.GetSpan(MaxDecimalDigits);
            Debug.Assert(MaxDecimalDigits <= buffer.Length);

            bool success = value.TryFormat(buffer, out int bytesWritten, provider: CultureInfo.InvariantCulture);
            Debug.Assert(success);
            writer.Advance(bytesWritten);
#else
            writer.WriteUtf8String(value.ToString(CultureInfo.InvariantCulture));
#endif
        }

        public static void WriteUtf8String(this IBufferWriter<byte> writer, ReadOnlySpan<byte> value)
        {
            if (value.IsEmpty)
            {
                return;
            }

            Span<byte> buffer = writer.GetSpan(value.Length);
            Debug.Assert(value.Length <= buffer.Length);
            value.CopyTo(buffer);
            writer.Advance(value.Length);
        }

        public static unsafe void WriteUtf8String(this IBufferWriter<byte> writer, ReadOnlySpan<char> value)
        {
            if (value.IsEmpty)
            {
                return;
            }

            int maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
            Span<byte> buffer = writer.GetSpan(maxByteCount);
            Debug.Assert(maxByteCount <= buffer.Length);
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
            writer.Advance(bytesWritten);
        }

        public static bool ContainsLineBreaks(this ReadOnlySpan<char> text) =>
            text.IndexOfAny('\r', '\n') >= 0;

#if !NET

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
