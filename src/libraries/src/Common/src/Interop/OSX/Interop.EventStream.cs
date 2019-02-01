// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using CFStringRef = System.IntPtr;
using CFArrayRef = System.IntPtr;
using FSEventStreamRef = System.IntPtr;
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

        /// <summary>
        /// The EventStream callback that will be called for every event batch.
        /// </summary>
        /// <param name="streamReference">The stream that was created for this callback.</param>
        /// <param name="clientCallBackInfo">A pointer to optional context info; otherwise, IntPtr.Zero.</param>
        /// <param name="numEvents">The number of paths, events, and IDs. Path[2] corresponds to Event[2] and ID[2], etc.</param>
        /// <param name="eventPaths">The paths that have changed somehow, according to their corresponding event.</param>
        /// <param name="eventFlags">The events for the corresponding path.</param>
        /// <param name="eventIds">The machine-and-disk-drive-unique Event ID for the specific event.</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal unsafe delegate void FSEventStreamCallback(
            FSEventStreamRef streamReference,
            IntPtr clientCallBackInfo,
            size_t numEvents,
            byte** eventPaths,
            FSEventStreamEventFlags* eventFlags,
            FSEventStreamEventId* eventIds);

        /// <summary>
        /// Internal wrapper to create a new EventStream to listen to events from the core OS (such as File System events).
        /// </summary>
        /// <param name="allocator">Should be IntPtr.Zero</param>
        /// <param name="cb">A callback instance that will be called for every event batch.</param>
        /// <param name="context">Should be IntPtr.Zero</param>
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
        [DllImport(Interop.Libraries.CoreServicesLibrary, CharSet = CharSet.Ansi)]
        private static extern SafeEventStreamHandle FSEventStreamCreate(
            IntPtr                      allocator,
            FSEventStreamCallback       cb,
            IntPtr                      context,
            SafeCreateHandle            pathsToWatch,
            FSEventStreamEventId        sinceWhen,
            CFTimeInterval              latency,
            FSEventStreamCreateFlags    flags);

        /// <summary>
        /// Creates a new EventStream to listen to events from the core OS (such as File System events).
        /// </summary>
        /// <param name="cb">A callback instance that will be called for every event batch.</param>
        /// <param name="pathsToWatch">A CFArray of the path(s) to watch for events.</param>
        /// <param name="sinceWhen">
        /// The start point to receive events from. This can be to retrieve historical events or only new events. 
        /// To get historical events, pass in the corresponding ID of the event you want to start from.
        /// To get only new events, pass in kFSEventStreamEventIdSinceNow.
        /// </param>
        /// <param name="latency">Coalescing period to wait before sending events.</param>
        /// <param name="flags">Flags to say what kind of events should be sent through this stream.</param>
        /// <returns>On success, returns a valid SafeCreateHandle to an FSEventStream object; otherwise, returns an invalid SafeCreateHandle</returns>
        internal static SafeEventStreamHandle FSEventStreamCreate(
            FSEventStreamCallback       cb,
            SafeCreateHandle            pathsToWatch,
            FSEventStreamEventId        sinceWhen,
            CFTimeInterval              latency,
            FSEventStreamCreateFlags    flags)
        {
            return FSEventStreamCreate(IntPtr.Zero, cb, IntPtr.Zero, pathsToWatch, sinceWhen, latency, flags);
        }

        /// <summary>
        /// Attaches an EventStream to a RunLoop so events can be received. This should usually be the current thread's RunLoop.
        /// </summary>
        /// <param name="streamRef">The stream to attach to the RunLoop</param>
        /// <param name="runLoop">The RunLoop to attach the stream to</param>
        /// <param name="runLoopMode">The mode of the RunLoop; this should usually be kCFRunLoopDefaultMode. See the documentation for RunLoops for more info.</param>
        [DllImport(Interop.Libraries.CoreServicesLibrary)]
        internal static extern void FSEventStreamScheduleWithRunLoop(
            SafeEventStreamHandle   streamRef,
            CFRunLoopRef            runLoop,
            SafeCreateHandle        runLoopMode);

        /// <summary>
        /// Starts receiving events on the specified stream.
        /// </summary>
        /// <param name="streamRef">The stream to receive events on.</param>
        /// <returns>Returns true if the stream was started; otherwise, returns false and no events will be received.</returns>
        [DllImport(Interop.Libraries.CoreServicesLibrary)]
        internal static extern bool FSEventStreamStart(SafeEventStreamHandle streamRef);

        /// <summary>
        /// Stops receiving events on the specified stream. The stream can be restarted and not miss any events.
        /// </summary>
        /// <param name="streamRef">The stream to stop receiving events on.</param>
        [DllImport(Interop.Libraries.CoreServicesLibrary)]
        internal static extern void FSEventStreamStop(SafeEventStreamHandle streamRef);

        /// <summary>
        /// Stops receiving events on the specified stream. The stream can be restarted and not miss any events.
        /// </summary>
        /// <param name="streamRef">The stream to stop receiving events on.</param>
        [DllImport(Interop.Libraries.CoreServicesLibrary)]
        internal static extern void FSEventStreamStop(IntPtr streamRef);

        /// <summary>
        /// Invalidates an EventStream and removes it from any RunLoops.
        /// </summary>
        /// <param name="streamRef">The FSEventStream to invalidate</param>
        /// <remarks>This can only be called after FSEventStreamScheduleWithRunLoop has be called</remarks>
        [DllImport(Interop.Libraries.CoreServicesLibrary)]
        internal static extern void FSEventStreamInvalidate(IntPtr streamRef);

        /// <summary>
        /// Removes the event stream from the RunLoop.
        /// </summary>
        /// <param name="streamRef">The stream to remove from the RunLoop</param>
        /// <param name="runLoop">The RunLoop to remove the stream from.</param>
        /// <param name="runLoopMode">The mode of the RunLoop; this should usually be kCFRunLoopDefaultMode. See the documentation for RunLoops for more info.</param>
        [DllImport(Interop.Libraries.CoreServicesLibrary)]
        internal static extern void FSEventStreamUnscheduleFromRunLoop(
            SafeEventStreamHandle   streamRef,
            CFRunLoopRef            runLoop,
            SafeCreateHandle        runLoopMode);

        /// <summary>
        /// Releases a reference count on the specified EventStream and, if necessary, cleans the stream up.
        /// </summary>
        /// <param name="streamRef">The stream on which to decrement the reference count.</param>
        [DllImport(Interop.Libraries.CoreServicesLibrary)]
        internal static extern void FSEventStreamRelease(IntPtr streamRef);
    }
}
