// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using CFStringRef = System.IntPtr;
using CFArrayRef = System.IntPtr;
using CFTimeInterval = System.Double;

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
        private static extern CFStringRef CFStringCreateWithCString(
            IntPtr allocator, 
            string str, 
            CFStringBuiltInEncodings encoding);
        
        /// <summary>
        /// Creates a CFStringRef from a 8-bit String object. Follows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="str">The string to get a CFStringRef for</param>
        /// <returns>Returns a pointer to a CFString on success; otherwise, returns IntPtr.Zero</returns>
        internal static CFStringRef CFStringCreateWithCString(string str)
        {
            return CFStringCreateWithCString(IntPtr.Zero, str, CFStringBuiltInEncodings.kCFStringEncodingUTF8);
        }

        /// <summary>
        /// Creates a pointer to an unmanaged CFArray containing the input values. Folows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="allocator">Should be IntPtr.Zero</param>
        /// <param name="values">The values to put in the array</param>
        /// <param name="numValues">The number of values in the array</param>
        /// <param name="callbacks">Should be IntPtr.Zero</param>
        /// <returns>Returns a pointer to a CFArray on success; otherwise, returns IntPtr.Zero</returns>
        [DllImport(Interop.Libraries.CoreFoundationLibrary)]
        private static extern CFArrayRef CFArrayCreate(
            IntPtr allocator,
            [MarshalAs(UnmanagedType.LPArray)]
            IntPtr[] values,
            ulong numValues,
            IntPtr callbacks);

        /// <summary>
        /// Creates a pointer to an unmanaged CFArray containing the input values. Folows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="values">The values to put in the array</param>
        /// <param name="numValues">The number of values in the array</param>
        /// <returns>Returns a pointer to a CFArray on success; otherwise, returns IntPtr.Zero</returns>
        internal static CFArrayRef CFArrayCreate(IntPtr[] values, ulong numValues)
        {
            return CFArrayCreate(IntPtr.Zero, values, numValues, IntPtr.Zero);
        }

        /// <summary>
        /// Decrements the reference count on the specified object and, if the ref count hits 0, cleans up the object.
        /// </summary>
        /// <param name="ptr">The pointer on which to decrement the reference count.</param>
        [DllImport(Interop.Libraries.CoreFoundationLibrary)]
        internal extern static void CFRelease(IntPtr ptr);
    }
}
