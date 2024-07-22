// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text;

namespace System.Net.NetworkInformation
{
    internal static class InterfaceInfoPal
    {
        private const int StackAllocationThreshold = 512;

        public static unsafe uint InterfaceNameToIndex(ReadOnlySpan<char> interfaceName)
        {
            // Includes null terminator.
            int bufferSize = Encoding.UTF8.GetByteCount(interfaceName) + 1;
            byte* nativeMemory = bufferSize <= StackAllocationThreshold ? null : (byte*)NativeMemory.Alloc((nuint)bufferSize);

            try
            {
                Span<byte> buffer = nativeMemory == null ? stackalloc byte[bufferSize] : new Span<byte>(nativeMemory, bufferSize);

                Encoding.UTF8.GetBytes(interfaceName, buffer);
                buffer[bufferSize - 1] = 0;

                return Interop.Sys.InterfaceNameToIndex(buffer);
            }
            finally
            {
                if (nativeMemory != null)
                {
                    NativeMemory.Free(nativeMemory);
                }
            }
        }

        public static unsafe uint InterfaceNameToIndex(ReadOnlySpan<byte> interfaceName)
        {
            int bufferSize = interfaceName.Length + 1;
            byte* nativeMemory = bufferSize <= StackAllocationThreshold ? null : (byte*)NativeMemory.Alloc((nuint)bufferSize);

            try
            {
                Span<byte> buffer = nativeMemory == null ? stackalloc byte[bufferSize] : new Span<byte>(nativeMemory, bufferSize);

                interfaceName.CopyTo(buffer);
                buffer[bufferSize - 1] = 0;

                return Interop.Sys.InterfaceNameToIndex(buffer);
            }
            finally
            {
                if (nativeMemory != null)
                {
                    NativeMemory.Free(nativeMemory);
                }
            }
        }
    }
}
