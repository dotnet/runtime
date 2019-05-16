// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;

using CFStringRef = System.IntPtr;
using FSEventStreamRef = System.IntPtr;
using size_t = System.IntPtr;
using FSEventStreamEventId = System.UInt64;
using FSEventStreamEventFlags = Interop.EventStream.FSEventStreamEventFlags;
using CFRunLoopRef = System.IntPtr;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public partial class FileSystemWatcher
    {
        /// <summary>Called when FileSystemWatcher is finalized.</summary>
        private void FinalizeDispose()
        {
            // Make sure we cleanup
            StopRaisingEvents();
        }

        private void StartRaisingEvents()
        {
            // If we're called when "Initializing" is true, set enabled to true
            if (IsSuspended())
            {
                _enabled = true;
                return;
            }

            // Don't start another instance if one is already runnings
            if (_cancellation != null)
            {
                return;
            }

            try
            {
                CancellationTokenSource cancellation = new CancellationTokenSource();
                RunningInstance instance = new RunningInstance(this, _directory, _includeSubdirectories, TranslateFlags(_notifyFilters), cancellation.Token);
                _enabled = true;
                _cancellation = cancellation;
                instance.Start();
            }
            catch
            {
                _enabled = false;
                _cancellation = null;
                throw;
            }
        }

        private void StopRaisingEvents()
        {
            _enabled = false;

            if (IsSuspended())
                return;

            CancellationTokenSource token = _cancellation;
            if (token != null)
            {
                _cancellation = null;
                token.Cancel();
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private CancellationTokenSource _cancellation;

        private static FSEventStreamEventFlags TranslateFlags(NotifyFilters flagsToTranslate)
        {
            FSEventStreamEventFlags flags = 0;

            // Always re-create the filter flags when start is called since they could have changed
            if ((flagsToTranslate & (NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Size)) != 0)
            {
                flags = FSEventStreamEventFlags.kFSEventStreamEventFlagItemInodeMetaMod  |
                        FSEventStreamEventFlags.kFSEventStreamEventFlagItemFinderInfoMod |
                        FSEventStreamEventFlags.kFSEventStreamEventFlagItemModified      |
                        FSEventStreamEventFlags.kFSEventStreamEventFlagItemChangeOwner;
            }
            if ((flagsToTranslate & NotifyFilters.Security) != 0)
            {
                flags |= FSEventStreamEventFlags.kFSEventStreamEventFlagItemChangeOwner |
                         FSEventStreamEventFlags.kFSEventStreamEventFlagItemXattrMod;
            }
            if ((flagsToTranslate & NotifyFilters.DirectoryName) != 0)
            {
                flags |= FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsDir |
                         FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsSymlink |
                         FSEventStreamEventFlags.kFSEventStreamEventFlagItemCreated |
                         FSEventStreamEventFlags.kFSEventStreamEventFlagItemRemoved |
                         FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed;
            }
            if ((flagsToTranslate & NotifyFilters.FileName) != 0)
            {
                flags |= FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsFile |
                         FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsSymlink |
                         FSEventStreamEventFlags.kFSEventStreamEventFlagItemCreated |
                         FSEventStreamEventFlags.kFSEventStreamEventFlagItemRemoved |
                         FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed;
            }

            return flags;
        }

        private sealed class RunningInstance
        {
            // Flags used to create the event stream
            private const Interop.EventStream.FSEventStreamCreateFlags EventStreamFlags = (Interop.EventStream.FSEventStreamCreateFlags.kFSEventStreamCreateFlagFileEvents | 
                                                                       Interop.EventStream.FSEventStreamCreateFlags.kFSEventStreamCreateFlagNoDefer   | 
                                                                       Interop.EventStream.FSEventStreamCreateFlags.kFSEventStreamCreateFlagWatchRoot);

            // Weak reference to the associated watcher. A weak reference is used so that the FileSystemWatcher may be collected and finalized,
            // causing an active operation to be torn down.
            private readonly WeakReference<FileSystemWatcher> _weakWatcher;

            // The user can input relative paths, which can muck with our path comparisons. Save off the 
            // actual full path so we can use it for comparing
            private string _fullDirectory;

            // Boolean if we allow events from nested folders
            private bool _includeChildren;

            // The bitmask of events that we want to send to the user
            private FSEventStreamEventFlags _filterFlags;

            // The EventStream to listen for events on
            private SafeEventStreamHandle _eventStream;


            // Callback delegate for the EventStream events 
            private Interop.EventStream.FSEventStreamCallback _callback;

            // Token to monitor for cancellation requests, upon which processing is stopped and all
            // state is cleaned up.
            private readonly CancellationToken _cancellationToken;

            // Calling RunLoopStop multiple times SegFaults so protect the call to it
            private bool _stopping;

            private ExecutionContext _context;

            internal RunningInstance(
                FileSystemWatcher watcher,
                string directory,
                bool includeChildren,
                FSEventStreamEventFlags filter,
                CancellationToken cancelToken)
            {
                Debug.Assert(string.IsNullOrEmpty(directory) == false);
                Debug.Assert(!cancelToken.IsCancellationRequested);

                _weakWatcher = new WeakReference<FileSystemWatcher>(watcher);
                _fullDirectory = System.IO.Path.GetFullPath(directory);
                _includeChildren = includeChildren;
                _filterFlags = filter;
                _cancellationToken = cancelToken;
                _cancellationToken.UnsafeRegister(obj => ((RunningInstance)obj).CancellationCallback(), this);
                _stopping = false;
            }

            private static class StaticWatcherRunLoopManager
            {
                // A reference to the RunLoop that we can use to start or stop a Watcher
                private static CFRunLoopRef s_watcherRunLoop = IntPtr.Zero;

                private static int s_scheduledStreamsCount = 0;

                private static readonly object s_lockObject = new object();

                public static void ScheduleEventStream(SafeEventStreamHandle eventStream)
                {
                    lock (s_lockObject)
                    {
                        if (s_watcherRunLoop != IntPtr.Zero)
                        {
                            // Schedule the EventStream to run on the thread's RunLoop
                            s_scheduledStreamsCount++;
                            Interop.EventStream.FSEventStreamScheduleWithRunLoop(eventStream, s_watcherRunLoop, Interop.RunLoop.kCFRunLoopDefaultMode);
                            return;
                        }

                        Debug.Assert(s_scheduledStreamsCount == 0);
                        s_scheduledStreamsCount = 1;
                        var runLoopStarted = new ManualResetEventSlim();
                        new Thread(WatchForFileSystemEventsThreadStart) { IsBackground = true }.Start(new object[] { runLoopStarted, eventStream });
                        runLoopStarted.Wait();
                    }
                }

                public static void UnscheduleFromRunLoop(SafeEventStreamHandle eventStream)
                {
                    Debug.Assert(s_watcherRunLoop != IntPtr.Zero);
                    lock (s_lockObject)
                    {
                        if (s_watcherRunLoop != IntPtr.Zero)
                        { 
                            // Always unschedule the RunLoop before cleaning up
                            Interop.EventStream.FSEventStreamUnscheduleFromRunLoop(eventStream, s_watcherRunLoop, Interop.RunLoop.kCFRunLoopDefaultMode);
                            s_scheduledStreamsCount--;

                            if (s_scheduledStreamsCount == 0)
                            {
                                // Stop the FS event message pump
                                Interop.RunLoop.CFRunLoopStop(s_watcherRunLoop);
                                s_watcherRunLoop = IntPtr.Zero;
                            }
                        }
                    }
                }

                private static void WatchForFileSystemEventsThreadStart(object args)
                {
                    var inputArgs = (object[])args;
                    var runLoopStarted = (ManualResetEventSlim)inputArgs[0];
                    var _eventStream = (SafeEventStreamHandle)inputArgs[1];
                    // Get this thread's RunLoop
                    IntPtr runLoop = Interop.RunLoop.CFRunLoopGetCurrent();
                    s_watcherRunLoop = runLoop;
                    Debug.Assert(s_watcherRunLoop != IntPtr.Zero);

                    // Retain the RunLoop so that it doesn't get moved or cleaned up before we're done with it.
                    IntPtr retainResult = Interop.CoreFoundation.CFRetain(runLoop);
                    Debug.Assert(retainResult == runLoop, "CFRetain is supposed to return the input value");

                    // Schedule the EventStream to run on the thread's RunLoop
                    Interop.EventStream.FSEventStreamScheduleWithRunLoop(_eventStream, runLoop, Interop.RunLoop.kCFRunLoopDefaultMode);

                    runLoopStarted.Set();
                    try
                    {
                        // Start the OS X RunLoop (a blocking call) that will pump file system changes into the callback function
                        Interop.RunLoop.CFRunLoopRun();
                    }
                    finally
                    {
                        lock (s_lockObject)
                        {
                            Interop.CoreFoundation.CFRelease(runLoop);
                        }
                    }
                }
            }

            private void CancellationCallback()
            {
                if (!_stopping && _eventStream != null)
                {
                    _stopping = true;

                    try
                    {
                        // When we get here, we've requested to stop so cleanup the EventStream and unschedule from the RunLoop
                        Interop.EventStream.FSEventStreamStop(_eventStream);
                    }
                    finally 
                    {
                        StaticWatcherRunLoopManager.UnscheduleFromRunLoop(_eventStream);
                    }
                }
            }

            internal unsafe void Start()
            {
                // Make sure _fullPath doesn't contain a link or alias
                // since the OS will give back the actual, non link'd or alias'd paths
                _fullDirectory = Interop.Sys.RealPath(_fullDirectory);
                if (_fullDirectory == null)
                {
                    throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo(), _fullDirectory, true);
                }

                Debug.Assert(string.IsNullOrEmpty(_fullDirectory) == false, "Watch directory is null or empty");

                // Normalize the _fullDirectory path to have a trailing slash
                if (_fullDirectory[_fullDirectory.Length - 1] != '/')
                {
                    _fullDirectory += "/";
                }

                // Get the path to watch and verify we created the CFStringRef
                SafeCreateHandle path = Interop.CoreFoundation.CFStringCreateWithCString(_fullDirectory);
                if (path.IsInvalid)
                {
                    throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo(), _fullDirectory, true);
                }

                // Take the CFStringRef and put it into an array to pass to the EventStream
                SafeCreateHandle arrPaths = Interop.CoreFoundation.CFArrayCreate(new CFStringRef[1] { path.DangerousGetHandle() }, (UIntPtr)1);
                if (arrPaths.IsInvalid)
                {
                    path.Dispose();
                    throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo(), _fullDirectory, true);
                }

                // Create the callback for the EventStream if it wasn't previously created for this instance.
                if (_callback == null)
                {
                    _callback = new Interop.EventStream.FSEventStreamCallback(FileSystemEventCallback);
                }

                _context = ExecutionContext.Capture();

                // Make sure the OS file buffer(s) are fully flushed so we don't get events from cached I/O
                Interop.Sys.Sync();

                // Create the event stream for the path and tell the stream to watch for file system events.
                _eventStream = Interop.EventStream.FSEventStreamCreate(
                    _callback,
                    arrPaths,
                    Interop.EventStream.kFSEventStreamEventIdSinceNow,
                    0.0f,
                    EventStreamFlags);
                if (_eventStream.IsInvalid)
                {
                    arrPaths.Dispose();
                    path.Dispose();
                    throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo(), _fullDirectory, true);
                }

                StaticWatcherRunLoopManager.ScheduleEventStream(_eventStream);

                bool started = Interop.EventStream.FSEventStreamStart(_eventStream);
                if (!started)
                {  
                    // Try to get the Watcher to raise the error event; if we can't do that, just silently exit since the watcher is gone anyway
                    FileSystemWatcher watcher;
                    if (_weakWatcher.TryGetTarget(out watcher))
                    {
                        // An error occurred while trying to start the run loop so fail out
                        watcher.OnError(new ErrorEventArgs(new IOException(SR.EventStream_FailedToStart, Marshal.GetLastWin32Error())));
                    }
                }
            }

            private unsafe void FileSystemEventCallback(
                FSEventStreamRef streamRef,
                IntPtr clientCallBackInfo,
                size_t numEvents,
                byte** eventPaths,
                FSEventStreamEventFlags* eventFlags,
                FSEventStreamEventId* eventIds)
            {
                // Try to get the actual watcher from our weak reference.  We maintain a weak reference most of the time
                // so as to avoid a rooted cycle that would prevent our processing loop from ever ending
                // if the watcher is dropped by the user without being disposed. If we can't get the watcher,
                // there's nothing more to do (we can't raise events), so bail.
                FileSystemWatcher watcher;
                if (!_weakWatcher.TryGetTarget(out watcher))
                {
                    CancellationCallback();
                    return;
                }

                ExecutionContext context = _context;
                if (context is null)
                {
                    // Flow suppressed, just run here
                    ProcessEvents(numEvents.ToInt32(), eventPaths, eventFlags, eventIds, watcher);
                }
                else
                {
                    ExecutionContext.Run(
                        context,
                        (object o) => ((RunningInstance)o).ProcessEvents(numEvents.ToInt32(), eventPaths, eventFlags, eventIds, watcher),
                        this);
                }
            }

            private unsafe void ProcessEvents(int numEvents,
                byte** eventPaths,
                FSEventStreamEventFlags* eventFlags,
                FSEventStreamEventId* eventIds,
                FileSystemWatcher watcher)
            {
                // Since renames come in pairs, when we find the first we need to search for the next one. Once we find it, we'll add it to this
                // list so when the for-loop comes across it, we'll skip it since it's already been processed as part of the original of the pair.
                List<FSEventStreamEventId> handledRenameEvents = null;
                Memory<char>[] events = new Memory<char>[numEvents];
                ParseEvents();

                for (int i = 0; i < numEvents; i++)
                {
                    ReadOnlySpan<char> path = events[i].Span;
                    Debug.Assert(path[path.Length - 1] != '/', "Trailing slashes on events is not supported");

                    // Match Windows and don't notify us about changes to the Root folder
                    if (_fullDirectory.Length >= path.Length && path.Equals(_fullDirectory.AsSpan(0, path.Length), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    WatcherChangeTypes eventType = 0;
                    // First, we should check if this event should kick off a re-scan since we can't really rely on anything after this point if that is true
                    if (ShouldRescanOccur(eventFlags[i]))
                    {
                        watcher.OnError(new ErrorEventArgs(new IOException(SR.FSW_BufferOverflow, (int)eventFlags[i])));
                        break;
                    }
                    else if ((handledRenameEvents != null) && (handledRenameEvents.Contains(eventIds[i])))
                    {
                        // If this event is the second in a rename pair then skip it
                        continue;
                    }
                    else if (CheckIfPathIsNested(path) && ((eventType = FilterEvents(eventFlags[i])) != 0))
                    {
                        // The base FileSystemWatcher does a match check against the relative path before combining with 
                        // the root dir; however, null is special cased to signify the root dir, so check if we should use that.
                        ReadOnlySpan<char> relativePath = ReadOnlySpan<char>.Empty;
                        if (!path.Equals(_fullDirectory, StringComparison.OrdinalIgnoreCase))
                        {
                            // Remove the root directory to get the relative path
                            relativePath = path.Slice(_fullDirectory.Length);
                        }

                        // Raise a notification for the event
                        if (((eventType & WatcherChangeTypes.Changed) > 0))
                        {
                            watcher.NotifyFileSystemEventArgs(WatcherChangeTypes.Changed, relativePath);
                        }
                        if (((eventType & WatcherChangeTypes.Created) > 0))
                        {
                            watcher.NotifyFileSystemEventArgs(WatcherChangeTypes.Created, relativePath);
                        }
                        if (((eventType & WatcherChangeTypes.Deleted) > 0))
                        {
                            watcher.NotifyFileSystemEventArgs(WatcherChangeTypes.Deleted, relativePath);
                        }
                        if (((eventType & WatcherChangeTypes.Renamed) > 0))
                        {
                            // Find the rename that is paired to this rename, which should be the next rename in the list
                            int pairedId = FindRenameChangePairedChange(i, eventFlags, numEvents);
                            if (pairedId == int.MinValue)
                            {
                                // Getting here means we have a rename without a pair, meaning it should be a create for the 
                                // move from unwatched folder to watcher folder scenario or a move from the watcher folder out.
                                // Check if the item exists on disk to check which it is
                                // Don't send a new notification if we already sent one for this event.
                                if (DoesItemExist(path, IsFlagSet(eventFlags[i], FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsFile)))
                                {
                                    if ((eventType & WatcherChangeTypes.Created) == 0)
                                    {
                                        watcher.NotifyFileSystemEventArgs(WatcherChangeTypes.Created, relativePath);
                                    }
                                }
                                else if ((eventType & WatcherChangeTypes.Deleted) == 0)
                                {
                                    watcher.NotifyFileSystemEventArgs(WatcherChangeTypes.Deleted, relativePath);
                                }
                            }
                            else
                            {
                                // Remove the base directory prefix and add the paired event to the list of 
                                // events to skip and notify the user of the rename 
                                ReadOnlySpan<char> newPathRelativeName = events[pairedId].Span.Slice(_fullDirectory.Length);
                                watcher.NotifyRenameEventArgs(WatcherChangeTypes.Renamed, newPathRelativeName, relativePath);

                                // Create a new list, if necessary, and add the event
                                if (handledRenameEvents == null)
                                {
                                    handledRenameEvents = new List<FSEventStreamEventId>();
                                }
                                handledRenameEvents.Add(eventIds[pairedId]);
                            }
                        }
                    }

                    ArraySegment<char> underlyingArray;
                    if (MemoryMarshal.TryGetArray(events[i], out underlyingArray))
                        ArrayPool<char>.Shared.Return(underlyingArray.Array);
                }

                this._context = ExecutionContext.Capture();

                void ParseEvents()
                {
                    for (int i = 0; i < events.Length; i++)
                    {
                        int byteCount = 0;
                        Debug.Assert(eventPaths[i] != null);
                        byte* temp = eventPaths[i];

                        // Finds the position of null character.
                        while (*temp != 0)
                        {
                            temp++;
                            byteCount++;
                        }

                        Debug.Assert(byteCount > 0, "Empty events are not supported");
                        events[i] = new Memory<char>(ArrayPool<char>.Shared.Rent(Encoding.UTF8.GetMaxCharCount(byteCount)));
                        int charCount;

                        // Converting an array of bytes to UTF-8 char array
                        charCount = Encoding.UTF8.GetChars(new ReadOnlySpan<byte>(eventPaths[i], byteCount), events[i].Span);
                        events[i] = events[i].Slice(0, charCount);
                    }
                }
            }

            /// <summary>
            /// Compares the given event flags to the filter flags and returns which event (if any) corresponds
            /// to those flags.
            /// </summary>
            private WatcherChangeTypes FilterEvents(FSEventStreamEventFlags eventFlags)
            {
                const FSEventStreamEventFlags changedFlags = FSEventStreamEventFlags.kFSEventStreamEventFlagItemInodeMetaMod |
                                                                                 FSEventStreamEventFlags.kFSEventStreamEventFlagItemFinderInfoMod |
                                                                                 FSEventStreamEventFlags.kFSEventStreamEventFlagItemModified |
                                                                                 FSEventStreamEventFlags.kFSEventStreamEventFlagItemChangeOwner |
                                                                                 FSEventStreamEventFlags.kFSEventStreamEventFlagItemXattrMod;
                WatcherChangeTypes eventType = 0;
                // If any of the Changed flags are set in both Filter and Event then a Changed event has occurred.
                if (((_filterFlags & changedFlags) & (eventFlags & changedFlags)) > 0)
                {
                    eventType |= WatcherChangeTypes.Changed;
                }

                // Notify created/deleted/renamed events if they pass through the filters
                bool allowDirs = (_filterFlags & FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsDir) > 0;
                bool allowFiles = (_filterFlags & FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsFile) > 0;
                bool isDir = (eventFlags & FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsDir) > 0;
                bool isFile = (eventFlags & FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsFile) > 0;
                bool eventIsCorrectType = (isDir && allowDirs) || (isFile && allowFiles);
                bool eventIsLink = (eventFlags & (FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsHardlink | FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsSymlink | FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsLastHardlink)) > 0;

                if (eventIsCorrectType || ((allowDirs || allowFiles) && (eventIsLink)))
                {
                    // Notify Created/Deleted/Renamed events.
                    if (IsFlagSet(eventFlags, FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed))
                    {
                        eventType |= WatcherChangeTypes.Renamed;
                    }
                    if (IsFlagSet(eventFlags, FSEventStreamEventFlags.kFSEventStreamEventFlagItemCreated))
                    {
                        eventType |= WatcherChangeTypes.Created;
                    }
                    if (IsFlagSet(eventFlags, FSEventStreamEventFlags.kFSEventStreamEventFlagItemRemoved))
                    {
                        eventType |= WatcherChangeTypes.Deleted;
                    }
                }
                return eventType;
            }

            private bool ShouldRescanOccur(FSEventStreamEventFlags flags)
            {
                // Check if any bit is set that signals that the caller should rescan
                return (IsFlagSet(flags, FSEventStreamEventFlags.kFSEventStreamEventFlagMustScanSubDirs) ||
                        IsFlagSet(flags, FSEventStreamEventFlags.kFSEventStreamEventFlagUserDropped)     ||
                        IsFlagSet(flags, FSEventStreamEventFlags.kFSEventStreamEventFlagKernelDropped)   ||
                        IsFlagSet(flags, FSEventStreamEventFlags.kFSEventStreamEventFlagRootChanged)     ||
                        IsFlagSet(flags, FSEventStreamEventFlags.kFSEventStreamEventFlagMount)           ||
                        IsFlagSet(flags, FSEventStreamEventFlags.kFSEventStreamEventFlagUnmount));
            }

            private bool CheckIfPathIsNested(ReadOnlySpan<char> eventPath)
            {
                // If we shouldn't include subdirectories, check if this path's parent is the watch directory
                // Check if the parent is the root. If so, then we'll continue processing based on the name.
                // If it isn't, then this will be set to false and we'll skip the name processing since it's irrelevant.
                return _includeChildren || _fullDirectory.AsSpan().StartsWith(System.IO.Path.GetDirectoryName(eventPath), StringComparison.OrdinalIgnoreCase);
            }

            private unsafe int FindRenameChangePairedChange(
                int currentIndex, 
                FSEventStreamEventFlags* eventFlags,
                int numEvents)
            {
                // Start at one past the current index and try to find the next Rename item, which should be the old path.
                for (int i = currentIndex + 1; i < numEvents; i++)
                {
                    if (IsFlagSet(eventFlags[i], FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed))
                    {
                        // We found match, stop looking
                        return i;
                    }
                }

                return int.MinValue;
            }

            private static bool IsFlagSet(FSEventStreamEventFlags flags, FSEventStreamEventFlags value)
            {
                return (value & flags) == value;
            }

            private static bool DoesItemExist(ReadOnlySpan<char> path, bool isFile)
            {
                if (path.IsEmpty || path.Length == 0)
                    return false;

                if (!isFile)
                    return  FileSystem.DirectoryExists(path);

                return PathInternal.IsDirectorySeparator(path[path.Length - 1])
                    ? false
                    : FileSystem.FileExists(path);
            }
        }
    }
}
