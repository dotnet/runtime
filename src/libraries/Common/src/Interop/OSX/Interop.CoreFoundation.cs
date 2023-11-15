// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using CFArrayRef = System.IntPtr;
using CFIndex = System.IntPtr;
using CFStringRef = System.IntPtr;

internal static partial class Interop
{
    internal static partial class CoreFoundation
    {
        /// <summary>
        /// Tells the OS what encoding the passed in String is in. These come from the CFString.h header file in the CoreFoundation framework.
        /// </summary>
        private enum CFStringBuiltInEncodings : uint
        {
            kCFStringEncodingMacRoman       = 0,
            kCFStringEncodingWindowsLatin1  = 0x0500,
            kCFStringEncodingISOLatin1      = 0x0201,
            kCFStringEncodingNextStepLatin  = 0x0B01,
            kCFStringEncodingASCII          = 0x0600,
            kCFStringEncodingUnicode        = 0x0100,
            kCFStringEncodingUTF8           = 0x08000100,
            kCFStringEncodingNonLossyASCII  = 0x0BFF,

            kCFStringEncodingUTF16          = 0x0100,
            kCFStringEncodingUTF16BE        = 0x10000100,
            kCFStringEncodingUTF16LE        = 0x14000100,
            kCFStringEncodingUTF32          = 0x0c000100,
            kCFStringEncodingUTF32BE        = 0x18000100,
            kCFStringEncodingUTF32LE        = 0x1c000100
        }

        /// <summary>
        /// Creates a CFStringRef from a specified range of memory with a specified encoding.
        /// Follows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="alloc">Should be IntPtr.Zero</param>
        /// <param name="bytes">The pointer to the beginning of the encoded string.</param>
        /// <param name="numBytes">The number of bytes in the encoding to read.</param>
        /// <param name="encoding">The encoding type.</param>
        /// <param name="isExternalRepresentation">Whether or not a BOM is present.</param>
        /// <returns>A CFStringRef on success, otherwise a SafeCreateHandle(IntPtr.Zero).</returns>
        [LibraryImport(Interop.Libraries.CoreFoundationLibrary)]
        private static partial SafeCreateHandle CFStringCreateWithBytes(
            IntPtr alloc,
            IntPtr bytes,
            CFIndex numBytes,
            CFStringBuiltInEncodings encoding,
            [MarshalAs(UnmanagedType.Bool)] bool isExternalRepresentation);

        /// <summary>
        /// Creates a CFStringRef from a 8-bit String object. Follows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="allocator">Should be IntPtr.Zero</param>
        /// <param name="str">The string to get a CFStringRef for</param>
        /// <param name="encoding">The encoding of the str variable. This should be UTF 8 for OS X</param>
        /// <returns>Returns a pointer to a CFString on success; otherwise, returns IntPtr.Zero</returns>
        /// <remarks>For *nix systems, the CLR maps ANSI to UTF-8, so be explicit about that</remarks>
        [LibraryImport(Interop.Libraries.CoreFoundationLibrary, StringMarshalling = StringMarshalling.Utf8)]
        private static partial SafeCreateHandle CFStringCreateWithCString(
            IntPtr allocator,
            string str,
            CFStringBuiltInEncodings encoding);

        /// <summary>
        /// Creates a CFStringRef from a 8-bit String object. Follows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="allocator">Should be IntPtr.Zero</param>
        /// <param name="str">The string to get a CFStringRef for</param>
        /// <param name="encoding">The encoding of the str variable. This should be UTF 8 for OS X</param>
        /// <returns>Returns a pointer to a CFString on success; otherwise, returns IntPtr.Zero</returns>
        /// <remarks>For *nix systems, the CLR maps ANSI to UTF-8, so be explicit about that</remarks>
        [LibraryImport(Interop.Libraries.CoreFoundationLibrary, StringMarshalling = StringMarshalling.Utf8)]
        private static partial SafeCreateHandle CFStringCreateWithCString(
            IntPtr allocator,
            IntPtr str,
            CFStringBuiltInEncodings encoding);

        /// <summary>
        /// Creates a CFStringRef from a 8-bit String object. Follows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="str">The string to get a CFStringRef for</param>
        /// <returns>Returns a valid SafeCreateHandle to a CFString on success; otherwise, returns an invalid SafeCreateHandle</returns>
        internal static SafeCreateHandle CFStringCreateWithCString(string str)
        {
            return CFStringCreateWithCString(IntPtr.Zero, str, CFStringBuiltInEncodings.kCFStringEncodingUTF8);
        }

        /// <summary>
        /// Creates a CFStringRef from a 8-bit String object. Follows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="utf8str">The string to get a CFStringRef for</param>
        /// <returns>Returns a valid SafeCreateHandle to a CFString on success; otherwise, returns an invalid SafeCreateHandle</returns>
        internal static SafeCreateHandle CFStringCreateWithCString(IntPtr utf8str)
        {
            return CFStringCreateWithCString(IntPtr.Zero, utf8str, CFStringBuiltInEncodings.kCFStringEncodingUTF8);
        }

        /// <summary>
        /// Creates a CFStringRef from a span of chars.
        /// Follows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="source">The chars to make a CFString version of.</param>
        /// <returns>A CFStringRef on success, otherwise a SafeCreateHandle(IntPtr.Zero).</returns>
        internal static unsafe SafeCreateHandle CFStringCreateFromSpan(ReadOnlySpan<char> source)
        {
            fixed (char* sourcePtr = source)
            {
                return CFStringCreateWithBytes(
                    IntPtr.Zero,
                    (IntPtr)sourcePtr,
                    new CFIndex(source.Length * 2),
                    CFStringBuiltInEncodings.kCFStringEncodingUTF16,
                    isExternalRepresentation: false);
            }
        }

        /// <summary>
        /// Creates a pointer to an unmanaged CFArray containing the input values. Follows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="allocator">Should be IntPtr.Zero</param>
        /// <param name="values">The values to put in the array</param>
        /// <param name="numValues">The number of values in the array</param>
        /// <param name="callbacks">Should be IntPtr.Zero</param>
        /// <returns>Returns a pointer to a CFArray on success; otherwise, returns IntPtr.Zero</returns>
        [LibraryImport(Interop.Libraries.CoreFoundationLibrary)]
        private static unsafe partial SafeCreateHandle CFArrayCreate(
            IntPtr allocator,
            IntPtr* values,
            UIntPtr numValues,
            IntPtr callbacks);

        /// <summary>
        /// Creates a pointer to an unmanaged CFArray containing the input values. Follows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="values">The values to put in the array</param>
        /// <param name="numValues">The number of values in the array</param>
        /// <returns>Returns a valid SafeCreateHandle to a CFArray on success; otherwise, returns an invalid SafeCreateHandle</returns>
        internal static unsafe SafeCreateHandle CFArrayCreate(IntPtr[] values, UIntPtr numValues)
        {
            fixed (IntPtr* pValues = values)
            {
                return CFArrayCreate(IntPtr.Zero, pValues, (UIntPtr)values.Length, IntPtr.Zero);
            }
        }

        /// <summary>
        /// Creates a pointer to an unmanaged CFArray containing the input values. Follows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="values">The values to put in the array</param>
        /// <returns>Returns a valid SafeCreateHandle to a CFArray on success; otherwise, returns an invalid SafeCreateHandle</returns>
        internal static unsafe SafeCreateHandle CFArrayCreate(Span<IntPtr> values)
        {
            fixed (IntPtr* pValues = &MemoryMarshal.GetReference(values))
            {
                return CFArrayCreate(IntPtr.Zero, pValues, (UIntPtr)values.Length, IntPtr.Zero);
            }
        }

        /// <summary>
        /// You should retain a Core Foundation object when you receive it from elsewhere
        /// (that is, you did not create or copy it) and you want it to persist. If you
        /// retain a Core Foundation object you are responsible for releasing it
        /// </summary>
        /// <param name="ptr">The CFType object to retain. This value must not be NULL</param>
        /// <returns>The input value</returns>
        [LibraryImport(Interop.Libraries.CoreFoundationLibrary)]
        internal static partial IntPtr CFRetain(IntPtr ptr);

        /// <summary>
        /// Decrements the reference count on the specified object and, if the ref count hits 0, cleans up the object.
        /// </summary>
        /// <param name="ptr">The pointer on which to decrement the reference count.</param>
        [LibraryImport(Interop.Libraries.CoreFoundationLibrary)]
        internal static partial void CFRelease(IntPtr ptr);
    }
}
