// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;

#pragma warning disable SA1121 // we don't want to simplify built-ins here as we're using aliasing
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
                var cancellation = new CancellationTokenSource();
                _cancellation = cancellation;
                _enabled = true;

                var instance = new RunningInstance(this, _directory, _includeSubdirectories, TranslateFlags(_notifyFilters));
                instance.Start(cancellation.Token);
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

            CancellationTokenSource? token = _cancellation;
            if (token != null)
            {
                _cancellation = null;
                token.Cancel();
                token.Dispose();
            }
        }

        private CancellationTokenSource? _cancellation;

        private static FSEventStreamEventFlags TranslateFlags(NotifyFilters flagsToTranslate)
        {
            FSEventStreamEventFlags flags = 0;

            // Always re-create the filter flags when start is called since they could have changed
            if ((flagsToTranslate & (NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Size)) != 0)
            {
                flags = FSEventStreamEventFlags.kFSEventStreamEventFlagItemInodeMetaMod |
                        FSEventStreamEventFlags.kFSEventStreamEventFlagItemFinderInfoMod |
                        FSEventStreamEventFlags.kFSEventStreamEventFlagItemModified |
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
                                                                       Interop.EventStream.FSEventStreamCreateFlags.kFSEventStreamCreateFlagNoDefer |
                                                                       Interop.EventStream.FSEventStreamCreateFlags.kFSEventStreamCreateFlagWatchRoot);

            // Weak reference to the associated watcher. A weak reference is used so that the FileSystemWatcher may be collected and finalized,
            // causing an active operation to be torn down.
            private readonly WeakReference<FileSystemWatcher> _weakWatcher;

            // The user can input relative paths, which can muck with our path comparisons. Save off the
            // actual full path so we can use it for comparing
            private readonly string _fullDirectory;

            // Boolean if we allow events from nested folders
            private readonly bool _includeChildren;

            // The bitmask of events that we want to send to the user
            private readonly FSEventStreamEventFlags _filterFlags;

            // Callback delegate for the EventStream events
            private readonly Interop.EventStream.FSEventStreamCallback _callback;

            // The EventStream to listen for events on
            private SafeEventStreamHandle? _eventStream;

            // Registration with the cancellation token.
            private CancellationTokenRegistration _cancellationRegistration;

            private ExecutionContext? _context;

            internal unsafe RunningInstance(
                FileSystemWatcher watcher,
                string directory,
                bool includeChildren,
                FSEventStreamEventFlags filter)
            {
                Debug.Assert(!string.IsNullOrEmpty(directory));

                // Make sure _fullPath doesn't contain a link or alias since the OS will give back the actual,
                // non link'd or alias'd paths.
                _fullDirectory = System.IO.Path.GetFullPath(directory);
                _fullDirectory = Interop.Sys.RealPath(_fullDirectory);
                if (_fullDirectory is null)
                {
                    throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo(), _fullDirectory, isDirectory: true);
                }

                // Also ensure it has a trailing slash.
                if (!_fullDirectory.EndsWith('/'))
                {
                    _fullDirectory += "/";
                }

                _callback = new Interop.EventStream.FSEventStreamCallback(FileSystemEventCallback);
                _weakWatcher = new WeakReference<FileSystemWatcher>(watcher);
                _includeChildren = includeChildren;
                _filterFlags = filter;
            }

            private static class StaticWatcherRunLoopManager
            {
                // A reference to the RunLoop that we can use to start or stop a Watcher
                private static CFRunLoopRef s_watcherRunLoop = IntPtr.Zero;

                private static int s_scheduledStreamsCount;

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
                        new Thread(static args =>
                        {
                            object[] inputArgs = (object[])args!;
                            WatchForFileSystemEventsThreadStart((ManualResetEventSlim)inputArgs[0], (SafeEventStreamHandle)inputArgs[1]);
                        })
                        {
                            IsBackground = true,
                            Name = ".NET File Watcher"
                        }.UnsafeStart(new object[] { runLoopStarted, eventStream });

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

                private static void WatchForFileSystemEventsThreadStart(ManualResetEventSlim runLoopStarted, SafeEventStreamHandle eventStream)
                {
                    // Get this thread's RunLoop
                    IntPtr runLoop = Interop.RunLoop.CFRunLoopGetCurrent();
                    s_watcherRunLoop = runLoop;
                    Debug.Assert(s_watcherRunLoop != IntPtr.Zero);

                    // Retain the RunLoop so that it doesn't get moved or cleaned up before we're done with it.
                    IntPtr retainResult = Interop.CoreFoundation.CFRetain(runLoop);
                    Debug.Assert(retainResult == runLoop, "CFRetain is supposed to return the input value");

                    // Schedule the EventStream to run on the thread's RunLoop
                    Interop.EventStream.FSEventStreamScheduleWithRunLoop(eventStream, runLoop, Interop.RunLoop.kCFRunLoopDefaultMode);

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

            private void CleanupEventStream()
            {
                SafeEventStreamHandle? eventStream = Interlocked.Exchange(ref _eventStream, null);
                if (eventStream != null)
                {
                    _cancellationRegistration.Unregister();
                    try
                    {
                        // When we get here, we've requested to stop so cleanup the EventStream and unschedule from the RunLoop
                        Interop.EventStream.FSEventStreamStop(eventStream);
                    }
                    finally
                    {
                        StaticWatcherRunLoopManager.UnscheduleFromRunLoop(eventStream);
                        eventStream.Close();
                    }
                }
            }

            internal void Start(CancellationToken cancellationToken)
            {
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
                if (started)
                {
                    // Once we've started, register to stop the watcher on cancellation being requested.
                    _cancellationRegistration = cancellationToken.UnsafeRegister(obj => ((RunningInstance)obj!).CleanupEventStream(), this);
                }
                else
                {
                    // Try to get the Watcher to raise the error event; if we can't do that, just silently exit since the watcher is gone anyway
                    int error = Marshal.GetLastWin32Error();
                    if (_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                    {
                        // An error occurred while trying to start the run loop so fail out
                        watcher.OnError(new ErrorEventArgs(new IOException(SR.EventStream_FailedToStart, error)));
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
                if (!_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                {
                    CleanupEventStream();
                    return;
                }

                ExecutionContext? context = _context;
                if (context is null)
                {
                    // Flow suppressed, just run here
                    ProcessEvents(numEvents.ToInt32(), eventPaths, new Span<FSEventStreamEventFlags>(eventFlags, numEvents.ToInt32()), new Span<FSEventStreamEventId>(eventIds, numEvents.ToInt32()), watcher);
                }
                else
                {
                    ExecutionContext.Run(
                        context,
                        (object? o) => ((RunningInstance)o!).ProcessEvents(numEvents.ToInt32(), eventPaths, new Span<FSEventStreamEventFlags>(eventFlags, numEvents.ToInt32()), new Span<FSEventStreamEventId>(eventIds, numEvents.ToInt32()), watcher),
                        this);
                }
            }

            private unsafe void ProcessEvents(int numEvents,
                byte** eventPaths,
                Span<FSEventStreamEventFlags> eventFlags,
                Span<FSEventStreamEventId> eventIds,
                FileSystemWatcher watcher)
            {
                // Since renames come in pairs, when we reach the first we need to test for the next one if it is the case. If the next one belongs into the pair,
                // we'll store the event id so when the for-loop comes across it, we'll skip it since it's already been processed as part of the original of the pair.
                int? handledRenameEvents = null;

                for (int i = 0; i < numEvents; i++)
                {
                    using ParsedEvent parsedEvent = ParseEvent(eventPaths[i]);

                    ReadOnlySpan<char> path = parsedEvent.Path;
                    Debug.Assert(path[^1] != '/', "Trailing slashes on events is not supported");

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
                    else if (handledRenameEvents == i)
                    {
                        // If this event is the second in a rename pair then skip it
                        continue;
                    }
                    else if (CheckIfPathIsNested(path) && ((eventType = FilterEvents(eventFlags[i])) != 0))
                    {
                        // The base FileSystemWatcher does a match check against the relative path before combining with
                        // the root dir; however, null is special cased to signify the root dir, so check if we should use that.
                        ReadOnlySpan<char> relativePath = ReadOnlySpan<char>.Empty;
                        if (path.Length > _fullDirectory.Length && path.StartsWith(_fullDirectory, StringComparison.OrdinalIgnoreCase))
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
                            // Find the rename that is paired to this rename.
                            int? pairedId = FindRenameChangePairedChange(i, eventFlags, eventIds);
                            if (!pairedId.HasValue)
                            {
                                // Getting here means we have a rename without a pair, meaning it should be a create for the
                                // move from unwatched folder to watcher folder scenario or a move from the watcher folder out.
                                // Check if the item exists on disk to check which it is
                                // Don't send a new notification if we already sent one for this event.
                                if (DoesItemExist(path, eventFlags[i].HasFlag(FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsFile)))
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
                                using (ParsedEvent pairedEvent = ParseEvent(eventPaths[pairedId.GetValueOrDefault()]))
                                {
                                    ReadOnlySpan<char> newPathRelativeName = pairedEvent.Path;
                                    if (newPathRelativeName.Length >= _fullDirectory.Length &&
                                        newPathRelativeName.StartsWith(_fullDirectory, StringComparison.OrdinalIgnoreCase))
                                    {
                                        newPathRelativeName = newPathRelativeName.Slice(_fullDirectory.Length);
                                    }

                                    watcher.NotifyRenameEventArgs(WatcherChangeTypes.Renamed, newPathRelativeName, relativePath);
                                }
                                handledRenameEvents = pairedId.GetValueOrDefault();
                            }
                        }
                    }
                }

                this._context = ExecutionContext.Capture();

                static ParsedEvent ParseEvent(byte* nativeEventPath)
                {
                    Debug.Assert(nativeEventPath != null);

                    ReadOnlySpan<byte> eventPath = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(nativeEventPath);
                    Debug.Assert(!eventPath.IsEmpty, "Empty events are not supported");

                    char[] tempBuffer = ArrayPool<char>.Shared.Rent(Encoding.UTF8.GetMaxCharCount(eventPath.Length));

                    // Converting an array of bytes to UTF-8 char array
                    int charCount = Encoding.UTF8.GetChars(eventPath, tempBuffer);
                    return new ParsedEvent(tempBuffer.AsSpan(0, charCount), tempBuffer);
                }

            }

            private readonly ref struct ParsedEvent
            {

                public ParsedEvent(ReadOnlySpan<char> path, char[] tempBuffer)
                {
                    TempBuffer = tempBuffer;
                    Path = path;
                }

                public readonly ReadOnlySpan<char> Path;

                public readonly char[] TempBuffer;

                public void Dispose() => ArrayPool<char>.Shared.Return(TempBuffer);

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
                    if (eventFlags.HasFlag(FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed))
                    {
                        eventType |= WatcherChangeTypes.Renamed;
                    }
                    if (eventFlags.HasFlag(FSEventStreamEventFlags.kFSEventStreamEventFlagItemCreated))
                    {
                        eventType |= WatcherChangeTypes.Created;
                    }
                    if (eventFlags.HasFlag(FSEventStreamEventFlags.kFSEventStreamEventFlagItemRemoved))
                    {
                        eventType |= WatcherChangeTypes.Deleted;
                    }
                }
                return eventType;
            }

            private bool ShouldRescanOccur(FSEventStreamEventFlags flags)
            {
                // Check if any bit is set that signals that the caller should rescan
                return (flags.HasFlag(FSEventStreamEventFlags.kFSEventStreamEventFlagMustScanSubDirs) ||
                        flags.HasFlag(FSEventStreamEventFlags.kFSEventStreamEventFlagUserDropped) ||
                        flags.HasFlag(FSEventStreamEventFlags.kFSEventStreamEventFlagKernelDropped) ||
                        flags.HasFlag(FSEventStreamEventFlags.kFSEventStreamEventFlagRootChanged) ||
                        flags.HasFlag(FSEventStreamEventFlags.kFSEventStreamEventFlagMount) ||
                        flags.HasFlag(FSEventStreamEventFlags.kFSEventStreamEventFlagUnmount));
            }

            private bool CheckIfPathIsNested(ReadOnlySpan<char> eventPath)
            {
                // If we shouldn't include subdirectories, check if this path's parent is the watch directory
                // Check if the parent is the root. If so, then we'll continue processing based on the name.
                // If it isn't, then this will be set to false and we'll skip the name processing since it's irrelevant.
                return _includeChildren || _fullDirectory.AsSpan().StartsWith(System.IO.Path.GetDirectoryName(eventPath), StringComparison.OrdinalIgnoreCase);
            }

            private int? FindRenameChangePairedChange(
                int currentIndex,
                Span<FSEventStreamEventFlags> flags, Span<FSEventStreamEventId> ids)
            {
                // The rename event can be composed of two events. The first contains the original file name the second contains the new file name.
                // Each of the events is delivered only when the corresponding folder is watched. It means both events are delivered when the rename/move
                // occurs inside the watched folder. When the move has origin o final destination outside, only one event is delivered. To distinguish
                // between two nonrelated events and the event which belong together the event ID is tested. Only related rename events differ in ID by one.
                // This behavior isn't documented and there is an open radar http://www.openradar.me/13461247.

                int nextIndex = currentIndex + 1;

                if (nextIndex >= flags.Length)
                    return null;

                if (ids[currentIndex] + 1 == ids[nextIndex] &&
                    flags[nextIndex].HasFlag(FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed))
                {
                    return nextIndex;
                }

                return null;
            }
            private static bool DoesItemExist(ReadOnlySpan<char> path, bool isFile)
            {
                if (path.IsEmpty || path.Length == 0)
                    return false;

                if (!isFile)
                    return FileSystem.DirectoryExists(path);

                return PathInternal.IsDirectorySeparator(path[path.Length - 1])
                    ? false
                    : FileSystem.FileExists(path);
            }
        }
    }
}
