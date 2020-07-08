// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public static partial class Marshal
    {
        public static string? PtrToStringAuto(IntPtr ptr, int len)
        {
            return PtrToStringUni(ptr, len);
        }

        public static string? PtrToStringAuto(IntPtr ptr)
        {
            return PtrToStringUni(ptr);
        }

        public static IntPtr StringToHGlobalAuto(string? s)
        {
            return StringToHGlobalUni(s);
        }

        public static IntPtr StringToCoTaskMemAuto(string? s)
        {
            return StringToCoTaskMemUni(s);
        }

        private static unsafe int GetSystemMaxDBCSCharSize()
        {
            Interop.Kernel32.CPINFO cpInfo = default;

            if (Interop.Kernel32.GetCPInfo(Interop.Kernel32.CP_ACP, &cpInfo) == Interop.BOOL.FALSE)
                return 2;

            return cpInfo.MaxCharSize;
        }

        // Win32 has the concept of Atoms, where a pointer can either be a pointer
        // or an int.  If it's less than 64K, this is guaranteed to NOT be a
        // pointer since the bottom 64K bytes are reserved in a process' page table.
        // We should be careful about deallocating this stuff.
        private static bool IsNullOrWin32Atom(IntPtr ptr)
        {
            const long HIWORDMASK = unchecked((long)0xffffffffffff0000L);

            long lPtr = (long)ptr;
            return 0 == (lPtr & HIWORDMASK);
        }

        internal static unsafe int StringToAnsiString(string s, byte* buffer, int bufferLength, bool bestFit = false, bool throwOnUnmappableChar = false)
        {
            Debug.Assert(bufferLength >= (s.Length + 1) * SystemMaxDBCSCharSize, "Insufficient buffer length passed to StringToAnsiString");

            int nb;

            uint flags = bestFit ? 0 : Interop.Kernel32.WC_NO_BEST_FIT_CHARS;
            uint defaultCharUsed = 0;

            fixed (char* pwzChar = s)
            {
                nb = Interop.Kernel32.WideCharToMultiByte(
                    Interop.Kernel32.CP_ACP,
                    flags,
                    pwzChar,
                    s.Length,
                    buffer,
                    bufferLength,
                    IntPtr.Zero,
                    throwOnUnmappableChar ? new IntPtr(&defaultCharUsed) : IntPtr.Zero);
            }

            if (defaultCharUsed != 0)
            {
                throw new ArgumentException(SR.Interop_Marshal_Unmappable_Char);
            }

            buffer[nb] = 0;
            return nb;
        }

        // Returns number of bytes required to convert given string to Ansi string. The return value includes null terminator.
        internal static unsafe int GetAnsiStringByteCount(ReadOnlySpan<char> chars)
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
                        Interop.Kernel32.CP_ACP, Interop.Kernel32.WC_NO_BEST_FIT_CHARS, pChars, chars.Length, null, 0, IntPtr.Zero, IntPtr.Zero);
                    if (byteLength <= 0)
                        throw new ArgumentException();
                }
            }

            return checked(byteLength + 1);
        }

        // Converts given string to Ansi string. The destination buffer must be large enough to hold the converted value, including null terminator.
        internal static unsafe void GetAnsiStringBytes(ReadOnlySpan<char> chars, Span<byte> bytes)
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
                       Interop.Kernel32.CP_ACP, Interop.Kernel32.WC_NO_BEST_FIT_CHARS, pChars, chars.Length, pBytes, bytes.Length, IntPtr.Zero, IntPtr.Zero);
                    if (byteLength <= 0)
                        throw new ArgumentException();
                }
            }

            bytes[byteLength] = 0;
        }
    }
}
