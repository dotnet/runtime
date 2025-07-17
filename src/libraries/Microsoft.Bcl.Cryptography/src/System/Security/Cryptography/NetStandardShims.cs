// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography
{
    internal static class NetStandardShims
    {
        internal static unsafe int GetByteCount(this Encoding encoding, ReadOnlySpan<char> str)
        {
            if (str.IsEmpty)
            {
                return 0;
            }

            fixed (char* pStr = str)
            {
                return encoding.GetByteCount(pStr, str.Length);
            }
        }

        internal static unsafe int GetBytes(this Encoding encoding, string str, Span<byte> destination)
        {
            return GetBytes(encoding, str.AsSpan(), destination);
        }

        internal static unsafe int GetBytes(this Encoding encoding, ReadOnlySpan<char> str, Span<byte> destination)
        {
            if (str.IsEmpty)
            {
                return 0;
            }

            fixed (char* pStr = str)
            fixed (byte* pDestination = destination)
            {
                return encoding.GetBytes(pStr, str.Length, pDestination, destination.Length);
            }
        }

        internal static void ReadExactly(this System.IO.Stream stream, Span<byte> buffer) =>
            ReadAtLeast(stream, buffer, buffer.Length, throwOnEndOfStream: true);

        internal static int ReadAtLeast(
            this System.IO.Stream stream,
            Span<byte> buffer,
            int minimumBytes,
            bool throwOnEndOfStream = true)
        {
            if (minimumBytes > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(minimumBytes));

            byte[] rented = CryptoPool.Rent(Math.Min(minimumBytes, 32768));
            int max = 0;
            int spaceRemaining = buffer.Length;
            int totalRead = 0;

            while (totalRead < minimumBytes)
            {
                int read = stream.Read(rented, 0, Math.Min(spaceRemaining, rented.Length));
                max = Math.Max(read, max);

                if (read == 0)
                {
                    CryptoPool.Return(rented, max);

                    if (throwOnEndOfStream)
                    {
                        throw new System.IO.EndOfStreamException();
                    }

                    return totalRead;
                }

                spaceRemaining -= read;
                totalRead += read;
                rented.AsSpan(0, read).CopyTo(buffer);
                buffer = buffer.Slice(read);
            }

            CryptoPool.Return(rented, max);
            return totalRead;
        }

        internal static void AppendData(this IncrementalHash hash, ReadOnlySpan<byte> data)
        {
            byte[] rented = CryptoPool.Rent(data.Length);

            try
            {
                data.CopyTo(rented);
                hash.AppendData(rented, 0, data.Length);
            }
            finally
            {
                CryptoPool.Return(rented, data.Length);
            }
        }

        internal static bool TryGetHashAndReset(
            this IncrementalHash hash,
            Span<byte> destination,
            out int bytesWritten)
        {
            byte[] actual = hash.GetHashAndReset();

            if (destination.Length < actual.Length)
            {
                bytesWritten = 0;
                return false;
            }

            actual.AsSpan().CopyTo(destination);
            bytesWritten = actual.Length;
            return true;
        }

#if NETFRAMEWORK || NETSTANDARD2_0
        internal static async Task<int> ReadAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            Debug.Assert(stream != null);

            if (buffer.IsEmpty)
            {
                return await stream.ReadAsync(Array.Empty<byte>(), 0, 0, cancellationToken).ConfigureAwait(false)   ;
            }

            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment))
            {
                return await stream.ReadAsync(segment.Array!, segment.Offset, segment.Count, cancellationToken).ConfigureAwait(false);
            }

            byte[] rented = ArrayPool<byte>.Shared.Rent(buffer.Length);
            int bytesRead = await stream.ReadAsync(rented, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
            rented.AsSpan(0, bytesRead).CopyTo(buffer.Span);
            ArrayPool<byte>.Shared.Return(rented, clearArray: true);
            return bytesRead;
        }
#endif
    }

#if !NETSTANDARD2_1_OR_GREATER
    internal static class CryptographicOperations
    {
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void ZeroMemory(Span<byte> buffer)
        {
            buffer.Clear();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
        {
            // NoOptimization because we want this method to be exactly as non-short-circuiting
            // as written.
            //
            // NoInlining because the NoOptimization would get lost if the method got inlined.

            if (left.Length != right.Length)
            {
                return false;
            }

            int length = left.Length;
            int accum = 0;

            for (int i = 0; i < length; i++)
            {
                accum |= left[i] - right[i];
            }

            return accum == 0;
        }
    }
#endif
}
