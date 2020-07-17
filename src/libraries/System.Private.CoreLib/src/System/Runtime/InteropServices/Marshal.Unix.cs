// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

namespace System.Runtime.InteropServices
{
    public static partial class Marshal
    {
        public static string? PtrToStringAuto(IntPtr ptr, int len)
        {
            return PtrToStringUTF8(ptr, len);
        }

        public static string? PtrToStringAuto(IntPtr ptr)
        {
            return PtrToStringUTF8(ptr);
        }

        public static IntPtr StringToHGlobalAuto(string? s)
        {
            return StringToHGlobalUTF8(s);
        }

        public static IntPtr StringToCoTaskMemAuto(string? s)
        {
            return StringToCoTaskMemUTF8(s);
        }

        private static int GetSystemMaxDBCSCharSize() => 3;

        private static bool IsNullOrWin32Atom(IntPtr ptr) => ptr == IntPtr.Zero;

        internal static unsafe int StringToAnsiString(string s, byte* buffer, int bufferLength, bool bestFit = false, bool throwOnUnmappableChar = false)
        {
            Debug.Assert(bufferLength >= (s.Length + 1) * SystemMaxDBCSCharSize, "Insufficient buffer length passed to StringToAnsiString");

            int convertedBytes;

            fixed (char* pChar = s)
            {
                convertedBytes = Encoding.UTF8.GetBytes(pChar, s.Length, buffer, bufferLength);
            }

            buffer[convertedBytes] = 0;

            return convertedBytes;
        }

        // Returns number of bytes required to convert given string to Ansi string. The return value includes null terminator.
        internal static unsafe int GetAnsiStringByteCount(ReadOnlySpan<char> chars)
        {
            int byteLength = Encoding.UTF8.GetByteCount(chars);
            return checked(byteLength + 1);
        }

        // Converts given string to Ansi string. The destination buffer must be large enough to hold the converted value, including null terminator.
        internal static unsafe void GetAnsiStringBytes(ReadOnlySpan<char> chars, Span<byte> bytes)
        {
            int actualByteLength = Encoding.UTF8.GetBytes(chars, bytes);
            bytes[actualByteLength] = 0;
        }
    }
}
