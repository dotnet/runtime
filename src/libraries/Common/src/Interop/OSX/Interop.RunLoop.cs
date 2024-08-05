// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using CFRunLoopRef = System.IntPtr;
using CFRunLoopSourceRef = System.IntPtr;
using CFStringRef = System.IntPtr;

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
        internal static SafeCreateHandle kCFRunLoopDefaultMode = Interop.CoreFoundation.CFStringCreateWithCString("kCFRunLoopDefaultMode");

        /// <summary>
        /// Starts the current thread's RunLoop. If the RunLoop is already running, creates a new, nested, RunLoop in the same stack.
        /// </summary>
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void CFRunLoopRun();
#else
        [LibraryImport(Interop.Libraries.CoreFoundationLibrary)]
        internal static partial void CFRunLoopRun();
#endif

        /// <summary>
        /// Runs the current thread's CFRunLoop object in a particular mode.
        /// </summary>
        [LibraryImport(Interop.Libraries.CoreFoundationLibrary)]
        internal static partial int CFRunLoopRunInMode(CFStringRef mode, double seconds, int returnAfterSourceHandled);

        /// <summary>
        /// Notifies a RunLoop to stop and return control to the execution context that called CFRunLoopRun
        /// </summary>
        /// <param name="rl">The RunLoop to notify to stop</param>
        [LibraryImport(Interop.Libraries.CoreFoundationLibrary)]
        internal static partial void CFRunLoopStop(CFRunLoopRef rl);

        /// <summary>
        /// Retrieves the RunLoop associated with the current thread; all threads automatically have a RunLoop.
        /// Follows the "Get Rule" where you do not own the object unless you CFRetain it; in which case, you must also CFRelease it as well.
        /// </summary>
        /// <returns>Returns a pointer to a CFRunLoop on success; otherwise, returns IntPtr.Zero</returns>
        [LibraryImport(Interop.Libraries.CoreFoundationLibrary)]
        internal static partial CFRunLoopRef CFRunLoopGetCurrent();

        /// <summary>
        /// Adds a CFRunLoopSource object to a run loop mode.
        /// </summary>
        /// <param name="rl">The run loop to modify.</param>
        /// <param name="source">The run loop source to add. The source is retained by the run loop.</param>
        /// <param name="mode">The run loop mode to which to add source.</param>
        [LibraryImport(Interop.Libraries.CoreFoundationLibrary)]
        internal static partial void CFRunLoopAddSource(CFRunLoopRef rl, CFRunLoopSourceRef source, CFStringRef mode);

        /// <summary>
        /// Removes a CFRunLoopSource object from a run loop mode.
        /// </summary>
        /// <param name="rl">The run loop to modify.</param>
        /// <param name="source">The run loop source to remove.</param>
        /// <param name="mode">The run loop mode of rl from which to remove source.</param>
        [LibraryImport(Interop.Libraries.CoreFoundationLibrary)]
        internal static partial void CFRunLoopRemoveSource(CFRunLoopRef rl, CFRunLoopSourceRef source, CFStringRef mode);

        /// <summary>
        /// Invalidates a CFRunLoopSource object, stopping it from ever firing again.
        /// </summary>
        /// <param name="source">The run loop source to invalidate.</param>
        [LibraryImport(Interop.Libraries.CoreFoundationLibrary)]
        internal static partial void CFRunLoopSourceInvalidate(CFRunLoopSourceRef source);

        /// <summary>
        /// Returns a bool that indicates whether the run loop is waiting for an event.
        /// </summary>
        /// <param name="rl">The run loop to examine.</param>
        /// <returns>true if rl has no events to process and is blocking,
        /// waiting for a source or timer to become ready to fire;
        /// false if rl either is not running or is currently processing
        /// a source, timer, or observer.</returns>
        [LibraryImport(Interop.Libraries.CoreFoundationLibrary)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CFRunLoopIsWaiting(CFRunLoopRef rl);
    }
}
