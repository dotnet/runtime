// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        // We should be careful about deallocating this stuff.  Extracted to
        // a function to avoid C# problems with lack of support for IntPtr.
        // We have 2 of these methods for slightly different semantics for NULL.
        private static bool IsWin32Atom(IntPtr ptr)
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
    }
}
