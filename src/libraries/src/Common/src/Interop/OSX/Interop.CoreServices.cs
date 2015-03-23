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
    internal static partial class CoreServices
    {
        /// <summary>
        /// This constant specifies that we don't want historical file system events, only new ones
        /// </summary>
        internal const ulong kFSEventStreamEventIdSinceNow = ulong.MaxValue;

        /// <summary>
        /// This is the default system path for the CoreServices library that PInvoke will use
        /// </summary>
        private const string CoreServicesLibrary = "/System/Library/Frameworks/CoreServices.framework/CoreServices";

        /// <summary>
        /// Flags that describe what happened in the event that was received. These come from the FSEvents.h header file in the CoreServices framework.
        /// </summary>
        [Flags]
        internal enum FSEventStreamEventFlags : uint
        {
            kFSEventStreamEventFlagNone                 = 0x00000000,
            kFSEventStreamEventFlagMustScanSubDirs      = 0x00000001,
            kFSEventStreamEventFlagUserDropped          = 0x00000002,
            kFSEventStreamEventFlagKernelDropped        = 0x00000004,
            kFSEventStreamEventFlagEventIdsWrapped      = 0x00000008,
            kFSEventStreamEventFlagHistoryDone          = 0x00000010,
            kFSEventStreamEventFlagRootChanged          = 0x00000020,
            kFSEventStreamEventFlagMount                = 0x00000040,
            kFSEventStreamEventFlagUnmount              = 0x00000080, /* These flags are only set if you specified the FileEvents */
            /* flags when creating the stream. */
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
            kFSEventStreamEventFlagItemIsSymlink        = 0x00040000
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
        internal delegate void FSEventStreamCallback(
            FSEventStreamRef streamReference,
            IntPtr clientCallBackInfo,
            size_t numEvents,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
            String[] eventPaths,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
            FSEventStreamEventFlags[] eventFlags,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
            FSEventStreamEventId[] eventIds);

        /// <summary>
        /// Creates a new EventStream to listen to events from the core OS (such as File System events).
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
        /// <returns></returns>
        [DllImport(CoreServicesLibrary)]
        internal static extern FSEventStreamRef FSEventStreamCreate(
            IntPtr allocator,
            FSEventStreamCallback cb,
            IntPtr context,
            CFArrayRef pathsToWatch,
            FSEventStreamEventId sinceWhen,
            CFTimeInterval latency,
            FSEventStreamCreateFlags flags);

        /// <summary>
        /// Attaches an EventStream to a RunLoop so events can be received. This should usually be the current thread's RunLoop.
        /// </summary>
        /// <param name="streamRef">The stream to attach to the RunLoop</param>
        /// <param name="runLoop">The RunLoop to attach the stream to</param>
        /// <param name="runLoopMode">The mode of the RunLoop; this should usually be kCFRunLoopDefaultMode. See the documentation for RunLoops for more info.</param>
        [DllImport(CoreServicesLibrary)]
        internal static extern void FSEventStreamScheduleWithRunLoop(
            FSEventStreamRef streamRef,
            CFRunLoopRef runLoop,
            CFStringRef runLoopMode);

        /// <summary>
        /// Starts receiving events on the specified stream.
        /// </summary>
        /// <param name="streamRef">The stream to receive events on.</param>
        /// <returns>Returns true if the stream was started; otherwise, returns false and no events will be received.</returns>
        [DllImport(CoreServicesLibrary)]
        internal static extern bool FSEventStreamStart(FSEventStreamRef streamRef);

        /// <summary>
        /// Stops receiving events on the specified stream. The stream can be restarted and not miss any events.
        /// </summary>
        /// <param name="streamRef">The stream to stop receiving events on.</param>
        [DllImport(CoreServicesLibrary)]
        internal static extern void FSEventStreamStop(FSEventStreamRef streamRef);

        /// <summary>
        /// Removes the event stream from the RunLoop.
        /// </summary>
        /// <param name="streamRef">The stream to remove from the RunLoop</param>
        /// <param name="runLoop">The RunLoop to remove the stream from.</param>
        /// <param name="runLoopMode">The mode of the RunLoop; this should usually be kCFRunLoopDefaultMode. See the documentation for RunLoops for more info.</param>
        [DllImport(CoreServicesLibrary)]
        internal static extern void FSEventStreamUnscheduleFromRunLoop(
            FSEventStreamRef streamRef, 
            CFRunLoopRef runLoop, 
            CFStringRef runLoopMode);

        /// <summary>
        /// Releases a reference count on the specified EventStream and, if necessary, cleans the stream up.
        /// </summary>
        /// <param name="streamRef">The stream on which to decrement the reference count.</param>
        [DllImport(CoreServicesLibrary)]
        internal static extern void FSEventStreamRelease(FSEventStreamRef streamRef);
    }
}