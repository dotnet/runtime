// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

#pragma warning disable SA1121 // we don't want to simplify built-ins here as we're using aliasing
using CFStringRef = System.IntPtr;
using CFArrayRef = System.IntPtr;
using FSEventStreamRef = System.IntPtr;
using CFIndex = System.IntPtr;
using size_t = System.IntPtr;
using FSEventStreamEventId = System.UInt64;
using CFTimeInterval = System.Double;
using CFRunLoopRef = System.IntPtr;

internal static partial class Interop
{
    internal static partial class EventStream
    {
        /// <summary>
        /// This constant specifies that we don't want historical file system events, only new ones
        /// </summary>
        internal const ulong kFSEventStreamEventIdSinceNow = 0xFFFFFFFFFFFFFFFF;

        /// <summary>
        /// Flags that describe what happened in the event that was received. These come from the FSEvents.h header file in the CoreServices framework.
        /// </summary>
        [Flags]
        internal enum FSEventStreamEventFlags : uint
        {
            /* flags when creating the stream. */
            kFSEventStreamEventFlagNone                 = 0x00000000,
            kFSEventStreamEventFlagMustScanSubDirs      = 0x00000001,
            kFSEventStreamEventFlagUserDropped          = 0x00000002,
            kFSEventStreamEventFlagKernelDropped        = 0x00000004,
            kFSEventStreamEventFlagEventIdsWrapped      = 0x00000008,
            kFSEventStreamEventFlagHistoryDone          = 0x00000010,
            kFSEventStreamEventFlagRootChanged          = 0x00000020,
            kFSEventStreamEventFlagMount                = 0x00000040,
            kFSEventStreamEventFlagUnmount              = 0x00000080,
            /* These flags are only set if you specified the FileEvents */
            kFSEventStreamEventFlagItemCreated          = 0x00000100,
            kFSEventStreamEventFlagItemRemoved          = 0x00000200,
            kFSEventStreamEventFlagItemInodeMetaMod     = 0x00000400,
            kFSEventStreamEventFlagItemRenamed          = 0x00000800,
            kFSEventStreamEventFlagItemModified         = 0x00001000,
            kFSEventStreamEventFlagItemFinderInfoMod    = 0x00002000,
            kFSEventStreamEventFlagItemChangeOwner      = 0x00004000,
            kFSEventStreamEventFlagItemXattrMod         = 0x00008000,
            kFSEventStreamEventFlagItemIsFile           = 0x00010000,
            kFSEventStreamEventFlagItemIsDir            = 0x00020000,
            kFSEventStreamEventFlagItemIsSymlink        = 0x00040000,
            kFSEventStreamEventFlagOwnEvent             = 0x00080000,
            kFSEventStreamEventFlagItemIsHardlink       = 0x00100000,
            kFSEventStreamEventFlagItemIsLastHardlink   = 0x00200000,
        }

        /// <summary>
        /// Flags that describe what kind of event stream should be created (and therefore what events should be
        /// piped into this stream). These come from the FSEvents.h header file in the CoreServices framework.
        /// </summary>
        [Flags]
        internal enum FSEventStreamCreateFlags : uint
        {
            kFSEventStreamCreateFlagNone        = 0x00000000,
            kFSEventStreamCreateFlagUseCFTypes  = 0x00000001,
            kFSEventStreamCreateFlagNoDefer     = 0x00000002,
            kFSEventStreamCreateFlagWatchRoot   = 0x00000004,
            kFSEventStreamCreateFlagIgnoreSelf  = 0x00000008,
            kFSEventStreamCreateFlagFileEvents  = 0x00000010
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct FSEventStreamContext
        {
            public CFIndex version;
            public IntPtr info;
            public IntPtr retain;
            public IntPtr release;
            public IntPtr copyDescription;
        }

        /// <summary>
        /// Internal wrapper to create a new EventStream to listen to events from the core OS (such as File System events).
        /// </summary>
        /// <param name="allocator">Should be IntPtr.Zero</param>
        /// <param name="callback">A callback instance that will be called for every event batch.</param>
        /// <param name="context">FSEventStreamContext structure to associate with this stream.</param>
        /// <param name="pathsToWatch">A CFArray of the path(s) to watch for events.</param>
        /// <param name="sinceWhen">
        /// The start point to receive events from. This can be to retrieve historical events or only new events.
        /// To get historical events, pass in the corresponding ID of the event you want to start from.
        /// To get only new events, pass in kFSEventStreamEventIdSinceNow.
        /// </param>
        /// <param name="latency">Coalescing period to wait before sending events.</param>
        /// <param name="flags">Flags to say what kind of events should be sent through this stream.</param>
        /// <returns>On success, returns a pointer to an FSEventStream object; otherwise, returns IntPtr.Zero</returns>
        /// <remarks>For *nix systems, the CLR maps ANSI to UTF-8, so be explicit about that</remarks>
        [GeneratedDllImport(Interop.Libraries.CoreServicesLibrary, CharSet = CharSet.Ansi)]
        internal static unsafe partial SafeEventStreamHandle FSEventStreamCreate(
            IntPtr                      allocator,
            delegate* unmanaged<FSEventStreamRef, IntPtr, size_t, byte**, FSEventStreamEventFlags*, FSEventStreamEventId*, void> callback,
            FSEventStreamContext*       context,
            SafeCreateHandle            pathsToWatch,
            FSEventStreamEventId        sinceWhen,
            CFTimeInterval              latency,
            FSEventStreamCreateFlags    flags);

        /// <summary>
        /// Attaches an EventStream to a RunLoop so events can be received. This should usually be the current thread's RunLoop.
        /// </summary>
        /// <param name="streamRef">The stream to attach to the RunLoop</param>
        /// <param name="runLoop">The RunLoop to attach the stream to</param>
        /// <param name="runLoopMode">The mode of the RunLoop; this should usually be kCFRunLoopDefaultMode. See the documentation for RunLoops for more info.</param>
        [GeneratedDllImport(Interop.Libraries.CoreServicesLibrary)]
        internal static partial void FSEventStreamScheduleWithRunLoop(
            SafeEventStreamHandle   streamRef,
            CFRunLoopRef            runLoop,
            SafeCreateHandle        runLoopMode);

        /// <summary>
        /// Starts receiving events on the specified stream.
        /// </summary>
        /// <param name="streamRef">The stream to receive events on.</param>
        /// <returns>Returns true if the stream was started; otherwise, returns false and no events will be received.</returns>
        [GeneratedDllImport(Interop.Libraries.CoreServicesLibrary)]
        internal static partial bool FSEventStreamStart(SafeEventStreamHandle streamRef);

        /// <summary>
        /// Stops receiving events on the specified stream. The stream can be restarted and not miss any events.
        /// </summary>
        /// <param name="streamRef">The stream to stop receiving events on.</param>
        [GeneratedDllImport(Interop.Libraries.CoreServicesLibrary)]
        internal static partial void FSEventStreamStop(SafeEventStreamHandle streamRef);

        /// <summary>
        /// Stops receiving events on the specified stream. The stream can be restarted and not miss any events.
        /// </summary>
        /// <param name="streamRef">The stream to stop receiving events on.</param>
        [GeneratedDllImport(Interop.Libraries.CoreServicesLibrary)]
        internal static partial void FSEventStreamStop(IntPtr streamRef);

        /// <summary>
        /// Invalidates an EventStream and removes it from any RunLoops.
        /// </summary>
        /// <param name="streamRef">The FSEventStream to invalidate</param>
        /// <remarks>This can only be called after FSEventStreamScheduleWithRunLoop has be called</remarks>
        [GeneratedDllImport(Interop.Libraries.CoreServicesLibrary)]
        internal static partial void FSEventStreamInvalidate(IntPtr streamRef);

        /// <summary>
        /// Removes the event stream from the RunLoop.
        /// </summary>
        /// <param name="streamRef">The stream to remove from the RunLoop</param>
        /// <param name="runLoop">The RunLoop to remove the stream from.</param>
        /// <param name="runLoopMode">The mode of the RunLoop; this should usually be kCFRunLoopDefaultMode. See the documentation for RunLoops for more info.</param>
        [GeneratedDllImport(Interop.Libraries.CoreServicesLibrary)]
        internal static partial void FSEventStreamUnscheduleFromRunLoop(
            SafeEventStreamHandle   streamRef,
            CFRunLoopRef            runLoop,
            SafeCreateHandle        runLoopMode);

        /// <summary>
        /// Releases a reference count on the specified EventStream and, if necessary, cleans the stream up.
        /// </summary>
        /// <param name="streamRef">The stream on which to decrement the reference count.</param>
        [GeneratedDllImport(Interop.Libraries.CoreServicesLibrary)]
        internal static partial void FSEventStreamRelease(IntPtr streamRef);
    }
}
