// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.IO;

/// <summary>Provides downlevel polyfills for Span-based instance methods on <see cref="Stream"/>.</summary>
internal static class StreamSpanPolyfills
{
    extension(Stream stream)
    {
        public int ReadAtLeast(Span<byte> destination, int minimumBytes, bool throwOnEndOfStream = true)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(Math.Min(destination.Length, 81920));
            int totalWritten = 0;
            while (totalWritten < minimumBytes && !destination.IsEmpty)
            {
                int written = stream.Read(buffer, 0, Math.Min(buffer.Length, destination.Length));
                if (written == 0)
                {
                    if (throwOnEndOfStream)
                    {
                        ThrowEndOfStreamException();
                    }
                    break;
                }
                buffer.AsSpan(0, written).CopyTo(destination);
                totalWritten += written;
                destination = destination.Slice(written);
            }
            ArrayPool<byte>.Shared.Return(buffer);
            return totalWritten;

            static void ThrowEndOfStreamException() => throw new EndOfStreamException();
        }
    }
}
