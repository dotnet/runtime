// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Net.NetworkInformation
{
    internal static class InterfaceInfoPal
    {
        // Measured in bytes (the native data type for this platform's API.)
        // Selected to occupy as much stack space as Windows' threshold (which is 256 two-byte characters.)
        private const int StackAllocationThreshold = 512;

        public static unsafe uint InterfaceNameToIndex<TChar>(ReadOnlySpan<TChar> interfaceName)
            where TChar : unmanaged, IBinaryNumber<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(byte) || typeof(TChar) == typeof(char));

            // Measured in bytes, including null terminator.
            int bufferSize = 0;
            byte[]? rentedBuffer = null;

            // The underlying API for this method accepts a null-terminated UTF8 string containing the interface name.
            // If TChar is byte, the only work required is a byte copy. If TChar is char, there's a transcoding step from Unicode
            // to UTF8.
            // Notably: the encoding passed to the underlying API is different between Linux and Windows.
            if (typeof(TChar) == typeof(byte))
            {
                bufferSize = interfaceName.Length + 1;
            }
            else if (typeof(TChar) == typeof(char))
            {
                ReadOnlySpan<char> castInterfaceName = MemoryMarshal.Cast<TChar, char>(interfaceName);

                bufferSize = Encoding.UTF8.GetByteCount(castInterfaceName) + 1;
            }
            Debug.Assert(bufferSize <= int.MaxValue - 1);

            try
            {
                Span<byte> buffer = ((uint)bufferSize <= StackAllocationThreshold
                    ? stackalloc byte[StackAllocationThreshold]
                    : (rentedBuffer = ArrayPool<byte>.Shared.Rent(bufferSize)))
                    .Slice(0, bufferSize);

                if (typeof(TChar) == typeof(byte))
                {
                    ReadOnlySpan<byte> castInterfaceName = MemoryMarshal.Cast<TChar, byte>(interfaceName);

                    castInterfaceName.CopyTo(buffer);
                }
                else if (typeof(TChar) == typeof(char))
                {
                    ReadOnlySpan<char> castInterfaceName = MemoryMarshal.Cast<TChar, char>(interfaceName);

                    Encoding.UTF8.GetBytes(castInterfaceName, buffer);
                }
                buffer[buffer.Length - 1] = 0;

                return Interop.Sys.InterfaceNameToIndex(buffer);
            }
            finally
            {
                if (rentedBuffer != null)
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                }
            }
        }
    }
}
