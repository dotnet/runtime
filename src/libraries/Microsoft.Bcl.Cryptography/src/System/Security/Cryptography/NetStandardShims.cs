// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;

namespace System.Security.Cryptography
{
    internal static class NetStandardShims
    {
        internal static void ReadExactly(this Stream stream, Span<byte> buffer) =>
            ReadAtLeast(stream, buffer, buffer.Length, throwOnEndOfStream: true);

        internal static int ReadAtLeast(
            this Stream stream,
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
