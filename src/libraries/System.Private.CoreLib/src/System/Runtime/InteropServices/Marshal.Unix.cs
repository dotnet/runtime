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

        public static unsafe IntPtr AllocHGlobal(IntPtr cb)
        {
            return (nint)NativeMemory.Alloc((nuint)(nint)cb);
        }

        public static unsafe void FreeHGlobal(IntPtr hglobal)
        {
            NativeMemory.Free((void*)(nint)hglobal);
        }

        public static unsafe IntPtr ReAllocHGlobal(IntPtr pv, IntPtr cb)
        {
            return (nint)NativeMemory.Realloc((void*)(nint)pv, (nuint)(nint)cb);
        }

        public static IntPtr AllocCoTaskMem(int cb) => AllocHGlobal((nint)(uint)cb);

        public static void FreeCoTaskMem(IntPtr ptr) => FreeHGlobal(ptr);

        public static unsafe IntPtr ReAllocCoTaskMem(IntPtr pv, int cb)
        {
            nuint cbNative = (nuint)(uint)cb;
            void* pvNative = (void*)(nint)pv;

            if ((cbNative == 0) && (pvNative != null))
            {
                Interop.Sys.Free(pvNative);
                return IntPtr.Zero;
            }

            return (nint)NativeMemory.Realloc((void*)(nint)pv, cbNative);
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

            void* p = Interop.Sys.Malloc((nuint)cbNative & ~WIN32_ALLOC_ALIGN);

            if (p == null)
            {
                throw new OutOfMemoryException();
            }

            void* s = (byte*)p + sizeof(nuint);
            *(((uint*)s) - 1) = (uint)(length * sizeof(char));
            ((char*)s)[length] = '\0';

            return (nint)s;
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

            void* p = Interop.Sys.Malloc((nuint)cbNative & ~WIN32_ALLOC_ALIGN);

            if (p == null)
            {
                throw new OutOfMemoryException();
            }

            void* s = (byte*)p + sizeof(nuint);
            *(((uint*)s) - 1) = (uint)length;

            // NULL-terminate with both a narrow and wide zero.
            *(byte*)((byte*)s + length) = (byte)'\0';
            *(short*)((byte*)s + ((length + 1) & ~1)) = 0;

            return (nint)s;
        }

        public static unsafe void FreeBSTR(IntPtr ptr)
        {
            void* ptrNative = (void*)(nint)ptr;

            if (ptrNative != null)
            {
                Interop.Sys.Free((byte*)ptr - sizeof(nuint));
            }
        }

#pragma warning disable IDE0060
        internal static Type? GetTypeFromProgID(string progID, string? server, bool throwOnError)
        {
            ArgumentNullException.ThrowIfNull(progID);

            if (throwOnError)
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);

            return null;
        }
#pragma warning restore IDE0060

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
