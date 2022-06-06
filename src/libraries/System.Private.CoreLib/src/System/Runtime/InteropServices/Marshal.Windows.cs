// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public static partial class Marshal
    {
        public static string? PtrToStringAuto(nint ptr, int len)
        {
            return PtrToStringUni(ptr, len);
        }

        public static string? PtrToStringAuto(nint ptr)
        {
            return PtrToStringUni(ptr);
        }

        public static nint StringToHGlobalAuto(string? s)
        {
            return StringToHGlobalUni(s);
        }

        public static nint StringToCoTaskMemAuto(string? s)
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
        private static bool IsNullOrWin32Atom(nint ptr)
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
            Interop.BOOL defaultCharUsed = Interop.BOOL.FALSE;

            fixed (char* pwzChar = s)
            {
                nb = Interop.Kernel32.WideCharToMultiByte(
                    Interop.Kernel32.CP_ACP,
                    flags,
                    pwzChar,
                    s.Length,
                    buffer,
                    bufferLength,
                    null,
                    throwOnUnmappableChar ? &defaultCharUsed : null);
            }

            if (defaultCharUsed != Interop.BOOL.FALSE)
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
                        Interop.Kernel32.CP_ACP, Interop.Kernel32.WC_NO_BEST_FIT_CHARS, pChars, chars.Length, null, 0, null, null);
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
                       Interop.Kernel32.CP_ACP, Interop.Kernel32.WC_NO_BEST_FIT_CHARS, pChars, chars.Length, pBytes, bytes.Length, null, null);
                    if (byteLength <= 0)
                        throw new ArgumentException();
                }
            }

            bytes[byteLength] = 0;
        }

        public static nint AllocHGlobal(nint cb)
        {
            nint pNewMem = Interop.Kernel32.LocalAlloc(Interop.Kernel32.LMEM_FIXED, (nuint)(nint)cb);
            if (pNewMem == 0)
            {
                throw new OutOfMemoryException();
            }
            return pNewMem;
        }

        public static void FreeHGlobal(nint hglobal)
        {
            if (!IsNullOrWin32Atom(hglobal))
            {
                Interop.Kernel32.LocalFree(hglobal);
            }
        }

        public static nint ReAllocHGlobal(nint pv, nint cb)
        {
            if (pv == 0)
            {
                // LocalReAlloc fails for pv == 0. Call AllocHGlobal instead for better fidelity
                // with standard C/C++ realloc behavior.
                return AllocHGlobal(cb);
            }

            nint pNewMem = Interop.Kernel32.LocalReAlloc(pv, (nuint)(nint)cb, Interop.Kernel32.LMEM_MOVEABLE);
            if (pNewMem == 0)
            {
                throw new OutOfMemoryException();
            }
            return pNewMem;
        }

        public static nint AllocCoTaskMem(int cb)
        {
            nint pNewMem = Interop.Ole32.CoTaskMemAlloc((uint)cb);
            if (pNewMem == 0)
            {
                throw new OutOfMemoryException();
            }
            return pNewMem;
        }

        public static void FreeCoTaskMem(nint ptr)
        {
            if (!IsNullOrWin32Atom(ptr))
            {
                Interop.Ole32.CoTaskMemFree(ptr);
            }
        }

        public static nint ReAllocCoTaskMem(nint pv, int cb)
        {
            nint pNewMem = Interop.Ole32.CoTaskMemRealloc(pv, (uint)cb);
            if (pNewMem == 0 && cb != 0)
            {
                throw new OutOfMemoryException();
            }
            return pNewMem;
        }

        internal static nint AllocBSTR(int length)
        {
            nint bstr = Interop.OleAut32.SysAllocStringLen(0, (uint)length);
            if (bstr == 0)
            {
                throw new OutOfMemoryException();
            }
            return bstr;
        }

        internal static unsafe nint AllocBSTRByteLen(uint length)
        {
            nint bstr = Interop.OleAut32.SysAllocStringByteLen(null, length);
            if (bstr == 0)
            {
                throw new OutOfMemoryException();
            }
            return bstr;
        }

        public static void FreeBSTR(nint ptr)
        {
            if (!IsNullOrWin32Atom(ptr))
            {
                Interop.OleAut32.SysFreeString(ptr);
            }
        }

        internal static Type? GetTypeFromProgID(string progID, string? server, bool throwOnError)
        {
            ArgumentNullException.ThrowIfNull(progID);

            int hr = Interop.Ole32.CLSIDFromProgID(progID, out Guid clsid);
            if (hr < 0)
            {
                if (throwOnError)
                    throw Marshal.GetExceptionForHR(hr, -1)!;
                return null;
            }

            return GetTypeFromCLSID(clsid, server, throwOnError);
        }

        /// <summary>
        /// Get the last system error on the current thread
        /// </summary>
        /// <returns>The last system error</returns>
        /// <remarks>
        /// The error is that for the current operating system (e.g. errno on Unix, GetLastError on Windows)
        /// </remarks>
        public static int GetLastSystemError()
        {
            return Interop.Kernel32.GetLastError();
        }

        /// <summary>
        /// Set the last system error on the current thread
        /// </summary>
        /// <param name="error">Error to set</param>
        /// <remarks>
        /// The error is that for the current operating system (e.g. errno on Unix, SetLastError on Windows)
        /// </remarks>
        public static void SetLastSystemError(int error)
        {
            Interop.Kernel32.SetLastError(error);
        }
    }
}
