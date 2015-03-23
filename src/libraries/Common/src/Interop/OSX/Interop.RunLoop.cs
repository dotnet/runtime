// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using CFRunLoopRef = System.IntPtr;

internal static partial class Interop
{
    internal static partial class RunLoop
    {
        /// <summary>
        /// This constant specifies that we want to use the default Run mode for the thread's Run loop.
        /// </summary>
        /// <remarks>
        /// For more information, see the Apple documentation: https://developer.apple.com/library/mac/documentation/CoreFoundation/Reference/CFRunLoopRef/index.html
        /// </remarks>
        internal static IntPtr kCFRunLoopDefaultMode = Interop.CoreFoundation.CFStringCreateWithCString("kCFRunLoopDefaultMode");

        /// <summary>
        /// Starts the current thread's RunLoop. If the RunLoop is already running, creates a new, nested, RunLoop in the same stack.
        /// </summary>
        [DllImport(Interop.Libraries.CoreFoundationLibrary)]
        internal extern static void CFRunLoopRun();

        /// <summary>
        /// Notifies a RunLoop to stop and return control to the execution context that called CFRunLoopRun
        /// </summary>
        /// <param name="rl">The RunLoop to notify to stop</param>
        [DllImport(Interop.Libraries.CoreFoundationLibrary)]
        internal extern static void CFRunLoopStop(CFRunLoopRef rl);

        /// <summary>
        /// Retrieves the RunLoop associated with the current thread; all threads automatically have a RunLoop.
        /// Follows the "Get Rule" where you do not own the object unless you CFRetain it; in which case, you must also CFRelease it as well.
        /// </summary>
        /// <returns>Returns a pointer to a CFRunLoop on success; otherwise, returns IntPtr.Zero</returns>
        [DllImport(Interop.Libraries.CoreFoundationLibrary)]
        internal static extern CFRunLoopRef CFRunLoopGetCurrent();
    }
}
