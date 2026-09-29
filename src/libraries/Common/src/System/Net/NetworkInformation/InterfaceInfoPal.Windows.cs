// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Net.NetworkInformation
{
    internal static class InterfaceInfoPal
    {
        // Measured in characters (the native data type for this platform's API.)
        private const int StackAllocationThreshold = 256;

        public static unsafe uint InterfaceNameToIndex<TChar>(ReadOnlySpan<TChar> interfaceName)
            where TChar : unmanaged, IBinaryNumber<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) ||  typeof(TChar) == typeof(byte));

            // Measured in characters, including null terminator.
            int bufferSize = 0;
            char* nativeMemory = null;
            ulong interfaceLuid = 0;
            uint interfaceIndex = 0;

            // The underlying API for this method accepts a null-terminated Unicode string containing the interface name.
            // If TChar is char, the only work required is a byte copy. If TChar is byte, there's a transcoding step from UTF8
            // to Unicode.
            // Notably: the encoding passed to the underlying API is different between Linux and Windows.
            if (typeof(TChar) == typeof(char))
            {
                bufferSize = interfaceName.Length + 1;
            }
            else if (typeof(TChar) == typeof(byte))
            {
                ReadOnlySpan<byte> castInterfaceName = MemoryMarshal.Cast<TChar, byte>(interfaceName);

                bufferSize = Encoding.UTF8.GetCharCount(castInterfaceName) + 1;
            }
            Debug.Assert(bufferSize <= int.MaxValue - 1);

            try
            {
                nativeMemory = (uint)bufferSize <= StackAllocationThreshold ? null : (char*)NativeMemory.Alloc((nuint)(bufferSize * sizeof(char)));

                Span<char> buffer = nativeMemory == null ? stackalloc char[StackAllocationThreshold].Slice(0, bufferSize) : new Span<char>(nativeMemory, bufferSize);

                if (typeof(TChar) == typeof(char))
                {
                    ReadOnlySpan<char> castInterfaceName = MemoryMarshal.Cast<TChar, char>(interfaceName);

                    castInterfaceName.CopyTo(buffer);
                }
                else if (typeof(TChar) == typeof(byte))
                {
                    ReadOnlySpan<byte> castInterfaceName = MemoryMarshal.Cast<TChar, byte>(interfaceName);

                    Encoding.UTF8.GetChars(castInterfaceName, buffer);
                }
                buffer[buffer.Length - 1] = '\0';

                if (Interop.IpHlpApi.ConvertInterfaceNameToLuid(buffer, ref interfaceLuid) != 0)
                {
                    return 0;
                }
            }
            finally
            {
                if (nativeMemory != null)
                {
                    NativeMemory.Free(nativeMemory);
                }
            }

            return Interop.IpHlpApi.ConvertInterfaceLuidToIndex(interfaceLuid, ref interfaceIndex) == 0
                ? interfaceIndex
                : 0;
        }
    }
}
