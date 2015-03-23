// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using CFStringRef = System.IntPtr;
using CFArrayRef = System.IntPtr;
using FSEventStreamRef = System.IntPtr;
using size_t = System.IntPtr;
using FSEventStreamEventId = System.UInt64;
using CFTimeInterval = System.Double;
using CFRunLoopRef = System.IntPtr;

internal static partial class Interop
{
    internal static partial class CoreFoundation
    {
        /// <summary>
        /// This constant specifies that we want to use the default Run mode for the thread's Run loop.
        /// </summary>
        /// <remarks>
        /// For more information, see the Apple documentation: https://developer.apple.com/library/mac/documentation/CoreFoundation/Reference/CFRunLoopRef/index.html
        /// </remarks>
        internal static IntPtr kCFRunLoopDefaultMode = CFStringCreateWithCString(IntPtr.Zero, "kCFRunLoopDefaultMode", 0);

        /// <summary>
        /// This is the default system path for the CoreFoundation library that PInvoke will use
        /// </summary>
        private const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

        /// <summary>
        /// Tells the OS what encoding the passed in String is in. These come from the CFString.h header file in the CoreFoundation framework.
        /// </summary>
        internal enum CFStringBuiltInEncodings : uint
        {
            kCFStringEncodingMacRoman       = 0,
            kCFStringEncodingWindowsLatin1  = 0x0500,
            kCFStringEncodingISOLatin1      = 0x0201,
            kCFStringEncodingNextStepLatin  = 0x0B01,
            kCFStringEncodingASCII          = 0x0600,
            kCFStringEncodingUnicode        = 0x0100,
            kCFStringEncodingUTF8           = 0x08000100,
            kCFStringEncodingNonLossyASCII  = 0x0BFF,

            kCFStringEncodingUTF16 = 0x0100,
            kCFStringEncodingUTF16BE = 0x10000100,
            kCFStringEncodingUTF16LE = 0x14000100,
            kCFStringEncodingUTF32 = 0x0c000100,
            kCFStringEncodingUTF32BE = 0x18000100,
            kCFStringEncodingUTF32LE = 0x1c000100
        }

        /// <summary>
        /// Creates a CFStringRef from a 8-bit String object. Follows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="allocator">Should be IntPtr.Zero</param>
        /// <param name="str">The string to get a CFStringRef for</param>
        /// <param name="encoding">The encoding of the str variable. This should be UTF 8 for OS X</param>
        /// <returns>Returns a pointer to a CFString on success; otherwise, returns IntPtr.Zero</returns>
        [DllImport(CoreFoundationLibrary)]
        internal static extern CFStringRef CFStringCreateWithCString(
            IntPtr allocator, 
            string str, 
            CFStringBuiltInEncodings encoding);

        /// <summary>
        /// Creates a pointer to an unmanaged CFArray containing the input values. Folows the "Create Rule" where if you create it, you delete it.
        /// </summary>
        /// <param name="allocator">Should be IntPtr.Zero</param>
        /// <param name="values">The values to put in the array</param>
        /// <param name="numValues">The number of values in the array</param>
        /// <param name="callbacks">Should be IntPtr.Zero</param>
        /// <returns>Returns a pointer to a CFArray on success; otherwise, returns IntPtr.Zero</returns>
        [DllImport(CoreFoundationLibrary)]
        internal static extern CFArrayRef CFArrayCreate(
            IntPtr allocator,
            [MarshalAs(UnmanagedType.LPArray)]
            IntPtr[] values,
            ulong numValues,
            IntPtr callbacks);

        /// <summary>
        /// Retrieves the RunLoop associated with the current thread; all threads automatically have a RunLoop.
        /// Follows the "Get Rule" where you do not own the object unless you CFRetain it; in which case, you must also CFRelease it as well.
        /// </summary>
        /// <returns>Returns a pointer to a CFRunLoop on success; otherwise, returns IntPtr.Zero</returns>
        [DllImport(CoreFoundationLibrary)]
        internal static extern CFRunLoopRef CFRunLoopGetCurrent();

        /// <summary>
        /// Decrements the reference count on the specified object and, if the ref count hits 0, cleans up the object.
        /// </summary>
        /// <param name="ptr">The pointer on which to decrement the reference count.</param>
        [DllImport(CoreFoundationLibrary)]
        internal extern static void CFRelease(IntPtr ptr);

        /// <summary>
        /// Starts the current thread's RunLoop. If the RunLoop is already running, creates a new, nested, RunLoop in the same stack.
        /// </summary>
        [DllImport(CoreFoundationLibrary)]
        internal extern static void CFRunLoopRun();

        /// <summary>
        /// Notifies a RunLoop to stop and return control to the execution context that called CFRunLoopRun
        /// </summary>
        /// <param name="rl">The RunLoop to notify to stop</param>
        [DllImport(CoreFoundationLibrary)]
        internal extern static void CFRunLoopStop(CFRunLoopRef rl);
    }
}