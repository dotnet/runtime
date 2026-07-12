// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.IO;

/// <summary>Provides downlevel polyfills for Span-based instance methods on <see cref="Stream"/>.</summary>
internal static class StreamSpanPolyfills
{
    extension(Stream stream)
    {
        public int Read(Span<byte> buffer)
        {
            if (buffer.IsEmpty)
            {
                return stream.Read(Array.Empty<byte>(), 0, 0);
            }

            byte[] rented = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                int bytesRead = stream.Read(rented, 0, buffer.Length);
                if ((uint)bytesRead > (uint)buffer.Length)
                {
                    throw new IOException();
                }

                rented.AsSpan(0, bytesRead).CopyTo(buffer);
                return bytesRead;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        public void Write(ReadOnlySpan<byte> buffer)
        {
            if (buffer.IsEmpty)
            {
                stream.Write(Array.Empty<byte>(), 0, 0);
                return;
            }

            byte[] rented = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                buffer.CopyTo(rented);
                stream.Write(rented, 0, buffer.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        public void ReadExactly(Span<byte> buffer) =>
            stream.ReadAtLeast(buffer, buffer.Length);

        public int ReadAtLeast(Span<byte> destination, int minimumBytes, bool throwOnEndOfStream = true)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(minimumBytes);
            if (minimumBytes > destination.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumBytes));
            }

            byte[] buffer = ArrayPool<byte>.Shared.Rent(Math.Min(destination.Length, 81920));
            try
            {
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

                return totalWritten;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            static void ThrowEndOfStreamException() => throw new EndOfStreamException();
        }
    }
}
