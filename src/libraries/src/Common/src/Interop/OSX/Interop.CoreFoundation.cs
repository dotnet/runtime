// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using CFStringRef = System.IntPtr;
using CFArrayRef = System.IntPtr;


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
        /// Creates a CFStringRef from a 8-bit String object. Follows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="allocator">Should be IntPtr.Zero</param>
        /// <param name="str">The string to get a CFStringRef for</param>
        /// <param name="encoding">The encoding of the str variable. This should be UTF 8 for OS X</param>
        /// <returns>Returns a pointer to a CFString on success; otherwise, returns IntPtr.Zero</returns>
        /// <remarks>For *nix systems, the CLR maps ANSI to UTF-8, so be explicit about that</remarks>
        [DllImport(Interop.Libraries.CoreFoundationLibrary, CharSet = CharSet.Ansi)]
        private static extern SafeCreateHandle CFStringCreateWithCString(
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
        [DllImport(Interop.Libraries.CoreFoundationLibrary, CharSet = CharSet.Ansi)]
        private static extern SafeCreateHandle CFStringCreateWithCString(
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
        /// Creates a pointer to an unmanaged CFArray containing the input values. Follows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="allocator">Should be IntPtr.Zero</param>
        /// <param name="values">The values to put in the array</param>
        /// <param name="numValues">The number of values in the array</param>
        /// <param name="callbacks">Should be IntPtr.Zero</param>
        /// <returns>Returns a pointer to a CFArray on success; otherwise, returns IntPtr.Zero</returns>
        [DllImport(Interop.Libraries.CoreFoundationLibrary)]
        private static extern SafeCreateHandle CFArrayCreate(
            IntPtr allocator,
            [MarshalAs(UnmanagedType.LPArray)]
            IntPtr[] values,
            UIntPtr numValues,
            IntPtr callbacks);

        /// <summary>
        /// Creates a pointer to an unmanaged CFArray containing the input values. Follows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="values">The values to put in the array</param>
        /// <param name="numValues">The number of values in the array</param>
        /// <returns>Returns a valid SafeCreateHandle to a CFArray on success; otherwise, returns an invalid SafeCreateHandle</returns>
        internal static SafeCreateHandle CFArrayCreate(IntPtr[] values, UIntPtr numValues)
        {
            return CFArrayCreate(IntPtr.Zero, values, numValues, IntPtr.Zero);
        }

        /// <summary>
        /// You should retain a Core Foundation object when you receive it from elsewhere
        /// (that is, you did not create or copy it) and you want it to persist. If you 
        /// retain a Core Foundation object you are responsible for releasing it
        /// </summary>
        /// <param name="ptr">The CFType object to retain. This value must not be NULL</param>
        /// <returns>The input value</returns>
        [DllImport(Interop.Libraries.CoreFoundationLibrary)]
        internal extern static IntPtr CFRetain(IntPtr ptr);

        /// <summary>
        /// Decrements the reference count on the specified object and, if the ref count hits 0, cleans up the object.
        /// </summary>
        /// <param name="ptr">The pointer on which to decrement the reference count.</param>
        [DllImport(Interop.Libraries.CoreFoundationLibrary)]
        internal extern static void CFRelease(IntPtr ptr);
    }
}
