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
            int bufferSize = GetAnsiStringByteCount(interfaceName);
            byte* nativeMemory = bufferSize <= StackAllocationThreshold ? null : (byte*)NativeMemory.Alloc((nuint)bufferSize);

            try
            {
                Span<byte> buffer = nativeMemory == null ? stackalloc byte[bufferSize] : new Span<byte>(nativeMemory, bufferSize);

                GetAnsiStringBytes(interfaceName, buffer);
                return Interop.IpHlpApi.if_nametoindex(buffer);
            }
            finally
            {
                if (nativeMemory != null)
                {
                    NativeMemory.Free(nativeMemory);
                }
            }
        }

        public static uint InterfaceNameToIndex(ReadOnlySpan<byte> interfaceName)
        {
            // The interface name passed to Windows' if_nametoindex function must be marshalled as an ANSI string.
            // As a result, a UTF8 string requires two transcoding steps: UTF8 to Unicode, and Unicode to ANSI.
            int bufferLength = Encoding.UTF8.GetCharCount(interfaceName);
            Span<char> buffer = bufferLength <= StackAllocationThreshold ? stackalloc char[bufferLength] : new char[bufferLength];

            Encoding.UTF8.GetChars(interfaceName, buffer);
            return InterfaceNameToIndex(buffer);
        }

        // This method is replicated from Marshal.GetAnsiStringByteCount (which is internal to System.Private.CoreLib.)
        private static unsafe int GetAnsiStringByteCount(ReadOnlySpan<char> chars)
        {
            int byteLength;

            if (chars.Length == 0)
            {
                byteLength = 0;
            }
            else
            {
                fixed (char* pChars = chars)
                {
                    byteLength = Interop.Kernel32.WideCharToMultiByte(
                        Interop.Kernel32.CP_ACP, Interop.Kernel32.WC_NO_BEST_FIT_CHARS, pChars, chars.Length, null, 0, null, null);
                    if (byteLength <= 0)
                    {
                        throw new ArgumentException();
                    }
                }
            }

            return checked(byteLength + 1);
        }

        // This method is replicated from Marshal.GetAnsiStringBytes (which is internal to System.Private.CoreLib.)
        private static unsafe void GetAnsiStringBytes(ReadOnlySpan<char> chars, Span<byte> bytes)
        {
            int byteLength;

            if (chars.Length == 0)
            {
                byteLength = 0;
            }
            else
            {
                fixed (char* pChars = chars)
                fixed (byte* pBytes = bytes)
                {
                    byteLength = Interop.Kernel32.WideCharToMultiByte(
                       Interop.Kernel32.CP_ACP, Interop.Kernel32.WC_NO_BEST_FIT_CHARS, pChars, chars.Length, pBytes, bytes.Length, null, null);
                    if (byteLength <= 0)
                    {
                        throw new ArgumentException();
                    }
                }
            }

            bytes[byteLength] = 0;
        }
    }
}
