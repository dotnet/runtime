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

        public static IntPtr AllocHGlobal(IntPtr cb)
        {
            nuint cbNative = (nuint)(nint)cb;

            // Avoid undefined malloc behavior by always allocating at least one byte
            IntPtr pNewMem = Interop.Sys.MemAlloc((cbNative != 0) ? cbNative : 1);
            if (pNewMem == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            return pNewMem;
        }

        public static void FreeHGlobal(IntPtr hglobal)
        {
            if (hglobal != IntPtr.Zero)
            {
                Interop.Sys.MemFree(hglobal);
            }
        }

        public static IntPtr ReAllocHGlobal(IntPtr pv, IntPtr cb)
        {
            nuint cbNative = (nuint)(nint)cb;

            if (cbNative == 0)
            {
                // ReAllocHGlobal never returns null, even for 0 size (different from standard C/C++ realloc)

                // Avoid undefined realloc behavior by always allocating at least one byte
                cbNative = 1;
            }

            IntPtr pNewMem = Interop.Sys.MemReAlloc(pv, cbNative);
            if (pNewMem == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }
            return pNewMem;
        }

        public static IntPtr AllocCoTaskMem(int cb) => AllocHGlobal((nint)(uint)cb);

        public static void FreeCoTaskMem(IntPtr ptr) => FreeHGlobal(ptr);

        public static IntPtr ReAllocCoTaskMem(IntPtr pv, int cb)
        {
            nuint cbNative = (nuint)(uint)cb;

            if (cbNative == 0)
            {
                if (pv != IntPtr.Zero)
                {
                    Interop.Sys.MemFree(pv);
                    return IntPtr.Zero;
                }
                // Avoid undefined realloc behavior by always allocating at least one byte
                cbNative = 1;
            }

            IntPtr pNewMem = Interop.Sys.MemReAlloc(pv, cbNative);
            if (pNewMem == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }
            return pNewMem;
        }

        internal static unsafe IntPtr AllocBSTR(int length)
        {
            // SysAllocString on Windows aligns the memory block size up
            const nuint WIN32_ALLOC_ALIGN = 15;

            ulong cbNative = 2 * (ulong)(uint)length + (uint)sizeof(IntPtr) + (uint)sizeof(char) + WIN32_ALLOC_ALIGN;
            if (cbNative > uint.MaxValue)
            {
                throw new OutOfMemoryException();
            }

            IntPtr p = Interop.Sys.MemAlloc((nuint)cbNative & ~WIN32_ALLOC_ALIGN);
            if (p == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            IntPtr s = p + sizeof(IntPtr);
            *(((uint*)s) - 1) = (uint)(length * sizeof(char));
            ((char*)s)[length] = '\0';

            return s;
        }

        internal static unsafe IntPtr AllocBSTRByteLen(uint length)
        {
            // SysAllocString on Windows aligns the memory block size up
            const nuint WIN32_ALLOC_ALIGN = 15;

            ulong cbNative = (ulong)(uint)length + (uint)sizeof(IntPtr) + (uint)sizeof(char) + WIN32_ALLOC_ALIGN;
            if (cbNative > uint.MaxValue)
            {
                throw new OutOfMemoryException();
            }

            IntPtr p = Interop.Sys.MemAlloc((nuint)cbNative & ~WIN32_ALLOC_ALIGN);
            if (p == IntPtr.Zero)
            {
                throw new OutOfMemoryException();
            }

            IntPtr s = p + sizeof(IntPtr);
            *(((uint*)s) - 1) = (uint)length;

            // NULL-terminate with both a narrow and wide zero.
            *(byte*)((byte*)s + length) = (byte)'\0';
            *(short*)((byte*)s + ((length + 1) & ~1)) = 0;

            return s;
        }

        public static unsafe void FreeBSTR(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                Interop.Sys.MemFree(ptr - sizeof(IntPtr));
            }
        }

        internal static Type? GetTypeFromProgID(string progID, string? server, bool throwOnError)
        {
            if (progID == null)
                throw new ArgumentNullException(nameof(progID));

            if (throwOnError)
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);

            return null;
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
            return Interop.Sys.GetErrNo();
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
            Interop.Sys.SetErrNo(error);
        }
    }
}
