// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using CFStringRef = System.IntPtr;
using FSEventStreamRef = System.IntPtr;
using size_t = System.IntPtr;
using FSEventStreamEventId = System.UInt64;
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
            // Make sure the Start and Stop can be called from different threads and don't 
            // stomp on the other's operation. We use the _syncLock instead of the
            // RunLoop or StreamRef because IntPtrs are value types and can't be locked
            lock (_syncLock)
            {
                // Always re-create the filter flags when start is called since they could have changed
                if ((_notifyFilters & (NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Size)) != 0)
                {
                    _filterFlags = Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemInodeMetaMod  | 
                                   Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemFinderInfoMod |
                                   Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemModified      |
                                   Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemChangeOwner;
                }
                if ((_notifyFilters & NotifyFilters.Security) != 0)
                {
                    _filterFlags |= Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemChangeOwner | Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemXattrMod;
                }
                if ((_notifyFilters & NotifyFilters.DirectoryName) != 0)
                {
                    _filterFlags |= Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsDir |
                                    Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsSymlink |
                                    Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemCreated |
                                    Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRemoved |
                                    Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed;
                }
                if ((_notifyFilters & NotifyFilters.FileName) != 0)
                {
                    _filterFlags |= Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsFile |
                                    Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsSymlink |
                                    Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemCreated |
                                    Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRemoved |
                                    Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed;
                }

                CreateStreamAndStartWatcher();
            }
        }

        private void StopRaisingEvents()
        {
            _enabled = false;

            // Make sure the Start and Stop can be called from different threads and don't 
            // stomp on the other's operation. We use the _syncLock instead of the
            // RunLoop or StreamRef because IntPtrs are value types and can't be locked
            lock (_syncLock)
            {
                // Make sure there's a loop to clear
                if (_watcherRunLoop != IntPtr.Zero)
                {
                    // Stop the RunLoop and wait for the thread to exit gracefully
                    Interop.RunLoop.CFRunLoopStop(_watcherRunLoop);
                    _watcherThread.Join();
                    _watcherRunLoop = IntPtr.Zero;
                }

                // Clean up the EventStream, if it exists
                if (!_eventStream.IsInvalid)
                {
                    _eventStream = new SafeEventStreamHandle(IntPtr.Zero);
                }             

                // Cleanup the callback
                if (_callback != null)
                {
                    _callback = null;
                }
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        // Flags used to create the event stream
        private const Interop.EventStream.FSEventStreamCreateFlags EventStreamFlags = (Interop.EventStream.FSEventStreamCreateFlags.kFSEventStreamCreateFlagFileEvents | 
                                                                                        Interop.EventStream.FSEventStreamCreateFlags.kFSEventStreamCreateFlagNoDefer   | 
                                                                                        Interop.EventStream.FSEventStreamCreateFlags.kFSEventStreamCreateFlagWatchRoot);

        // The user can input relative paths, which can muck with our path comparisons. Save off the 
        // actual full path so we can use it for comparing
        private string _fullDirectory = String.Empty;

        // The bitmask of events that we want to send to the user
        private Interop.EventStream.FSEventStreamEventFlags _filterFlags = 0;

        // The EventStream to listen for events on
        private SafeEventStreamHandle _eventStream = new SafeEventStreamHandle(IntPtr.Zero);

        // The background thread to use to monitor events
        private Thread _watcherThread = null;

        // A reference to the RunLoop that we can use to start or stop a Watcher
        private CFRunLoopRef _watcherRunLoop = IntPtr.Zero;

        // Since all variables are IntPtr, Strings, or change references, using this in our locks
        private readonly object _syncLock = new object();

        // Callback delegate for the EventStream events 
        private Interop.EventStream.FSEventStreamCallback _callback = null;

        // Use an event to try to prevent StartRaisingEvents from returning before the
        // RunLoop actually begins. This will mitigate a race condition where the watcher
        // thread hasn't completed initialization and stop is called before the RunLoop even starts.
        private readonly AutoResetEvent _runLoopStartedEvent = new AutoResetEvent(false);

        private void CreateStreamAndStartWatcher()
        {
            Debug.Assert(_eventStream.IsInvalid);
            Debug.Assert(_watcherRunLoop == IntPtr.Zero);
            Debug.Assert(_callback == null);

            // Make sure we only do this if there is a valid directory
            if (String.IsNullOrEmpty(_directory) == false)
            {
                _fullDirectory = System.IO.Path.GetFullPath(_directory);

                // Get the path to watch and verify we created the CFStringRef
                SafeCreateHandle path = Interop.CoreFoundation.CFStringCreateWithCString(_fullDirectory);
                if (path.IsInvalid)
                {
                    throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo(), _fullDirectory, true);
                }

                // Take the CFStringRef and put it into an array to pass to the EventStream
                SafeCreateHandle arrPaths = Interop.CoreFoundation.CFArrayCreate(new CFStringRef[1] { path.DangerousGetHandle() }, 1);
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

                // Make sure the OS file buffer(s) are fully flushed so we don't get events from cached I/O
                Interop.libc.sync();

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

                _enabled = true;

                // Create and start our watcher thread then wait for the thread to initialize and start 
                // the RunLoop. We wait for that to prevent this function from returning before the RunLoop
                // has a chance to start so that any callers won't race with the background thread's initialization
                // and calling Stop, which would attempt to stop a RunLoop that hasn't started yet.
                _watcherThread = new Thread(new ThreadStart(WatchForFileSystemEventsThreadStart));
                _watcherThread.Start();
                _runLoopStartedEvent.WaitOne();
            }
        }

        private void WatchForFileSystemEventsThreadStart()
        {
            // Get this thread's RunLoop
            _watcherRunLoop = Interop.RunLoop.CFRunLoopGetCurrent();
            Debug.Assert(_watcherRunLoop != IntPtr.Zero);

            // Schedule the EventStream to run on the thread's RunLoop
            Interop.EventStream.FSEventStreamScheduleWithRunLoop(_eventStream, _watcherRunLoop, Interop.RunLoop.kCFRunLoopDefaultMode);
            if (Interop.EventStream.FSEventStreamStart(_eventStream))
            {
                // Notify the StartRaisingEvents call that we are initialized and about to start
                // so that it can return and avoid a race-condition around multiple threads calling Stop and Start
                _runLoopStartedEvent.Set();

                // Start the OS X RunLoop (a blocking call) that will pump file system changes into the callback function
                Interop.RunLoop.CFRunLoopRun();

                // When we get here, we've requested to stop so cleanup the EventStream and unschedule from the RunLoop
                Interop.EventStream.FSEventStreamStop(_eventStream);
                Interop.EventStream.FSEventStreamUnscheduleFromRunLoop(_eventStream, _watcherRunLoop, Interop.RunLoop.kCFRunLoopDefaultMode);
            }
            else
            {
                // We failed and need to release the caller so the OnError event can be processed
                _runLoopStartedEvent.Set();

                // An error occurred while trying to start the run loop so fail out
                Interop.EventStream.FSEventStreamUnscheduleFromRunLoop(_eventStream, _watcherRunLoop, Interop.RunLoop.kCFRunLoopDefaultMode);
                OnError(new ErrorEventArgs(new IOException(SR.EventStream_FailedToStart, Marshal.GetLastWin32Error())));
            }
        }

        private void FileSystemEventCallback( 
            FSEventStreamRef streamRef, 
            IntPtr clientCallBackInfo, 
            size_t numEvents, 
            String[] eventPaths, 
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
            Interop.EventStream.FSEventStreamEventFlags[] eventFlags,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
            FSEventStreamEventId[] eventIds)
        {
            Debug.Assert((numEvents.ToInt32() == eventPaths.Length) && (numEvents.ToInt32() == eventFlags.Length) && (numEvents.ToInt32() == eventIds.Length));

            // Since renames come in pairs, when we find the first we need to search for the next one. Once we find it, we'll add it to this
            // list so when the for-loop comes across it, we'll skip it since it's already been processed as part of the original of the pair.
            List<FSEventStreamEventId> handledRenameEvents = null;

            for (long i = 0; i < numEvents.ToInt32(); i++)
            {
                // We need to special case the root path for pattern matching, so cache the path this way in case it is the root
                // and use it for pattern matching. We'll go back to the eventPaths[i] to get the actual path for notifications
                string path = eventPaths[i];
                if (path.Equals(_fullDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    path += "/.";
                }

                // First, we should check if this event should kick off a re-scan since we can't really rely on anything after this point if that is true
                if (ShouldRescanOccur(eventFlags[i]))
                {
                    OnError(new ErrorEventArgs(new IOException(SR.FSW_BufferOverflow, (int)eventFlags[i])));
                    break;
                }
                else if ((handledRenameEvents != null) && (handledRenameEvents.Contains(eventIds[i])))
                {
                    // If this event is the second in a rename pair then skip it
                    continue;
                }
                else if ((DoesPathPassNameFilter(path)) && ((_filterFlags & eventFlags[i]) != 0))
                {
                    // The base FileSystemWatcher does a match check against the relative path before combining with 
                    // the root dir; however, null is special cased to signify the root dir, so check if we should use that.
                    string relativePath = null;
                    if (eventPaths[i].Equals(_fullDirectory, StringComparison.OrdinalIgnoreCase) == false)
                    {
                        // Check if the event path, with the root directory removed, begins with a / and
                        // if so, remove it; otherwise, just remove the root path (which contains a trailing /)
                        if (eventPaths[i][_fullDirectory.Length] == '/')
                        {
                            relativePath = eventPaths[i].Remove(0, _fullDirectory.Length + 1);
                        }
                        else
                        {
                            relativePath = eventPaths[i].Remove(0, _fullDirectory.Length);
                        }
                    }

                    // Check if this is a rename
                    if (IsFlagSet(eventFlags[i], Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed))
                    {
                        // Find the rename that is paired to this rename, which should be the next rename in the list
                        long pairedId = FindRenameChangePairedChange(i, eventPaths, eventFlags, eventIds);
                        if (pairedId == long.MinValue)
                        {
                            // Getting here means we have a rename without a pair, meaning it should be a create for the 
                            // move from unwatched folder to watcher folder scenario or a move from the watcher folder out.
                            // Check if the item exists on disk to check which it is
                            if (DoesItemExist(eventPaths[i], IsFlagSet(eventFlags[i], Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsFile)))
                            {
                                NotifyFileSystemEventArgs(WatcherChangeTypes.Created, relativePath);
                            }
                            else
                            {
                                NotifyFileSystemEventArgs(WatcherChangeTypes.Deleted, relativePath);
                            }
                        }
                        else
                        {
                            // Remove the base directory prefix (including trailing / that OS X adds) and 
                            // add the paired event to the list of events to skip and notify the user of the rename
                            string newPathRelativeName = eventPaths[pairedId].Remove(0, _fullDirectory.Length + 1);
                            NotifyRenameEventArgs(WatcherChangeTypes.Renamed, newPathRelativeName, relativePath);

                            // Create a new list, if necessary, and add the event
                            if (handledRenameEvents == null)
                            {
                                handledRenameEvents = new List<FSEventStreamEventId>();
                            }
                            handledRenameEvents.Add(eventIds[pairedId]);
                        }
                    }
                    else
                    {
                        // OS X is wonky where it can give back kFSEventStreamEventFlagItemCreated and kFSEventStreamEventFlagItemRemoved
                        // for the same item. The only option we have is to stat and see if the item exists; if so send created, otherwise send deleted.
                        if ((IsFlagSet(eventFlags[i], Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemCreated)) ||
                            (IsFlagSet(eventFlags[i], Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRemoved)))
                        {
                            if (DoesItemExist(eventPaths[i], IsFlagSet(eventFlags[i], Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsFile)))
                            {
                                NotifyFileSystemEventArgs(WatcherChangeTypes.Created, relativePath);
                            }
                            else
                            {
                                NotifyFileSystemEventArgs(WatcherChangeTypes.Deleted, relativePath);
                            }
                        }

                        if (IsFlagSet(eventFlags[i], Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemInodeMetaMod) ||
                            IsFlagSet(eventFlags[i], Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemModified) ||
                            IsFlagSet(eventFlags[i], Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemFinderInfoMod) ||
                            IsFlagSet(eventFlags[i], Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemChangeOwner) ||
                            IsFlagSet(eventFlags[i], Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemXattrMod))
                        {
                            // Everything else is a modification
                            NotifyFileSystemEventArgs(WatcherChangeTypes.Changed, relativePath);
                        }
                    }
                }
            }
        }

        private bool ShouldRescanOccur(Interop.EventStream.FSEventStreamEventFlags flags)
        {
            // Check if any bit is set that signals that the caller should rescan
            return (IsFlagSet(flags, Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagMustScanSubDirs) ||
                    IsFlagSet(flags, Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagUserDropped)     ||
                    IsFlagSet(flags, Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagKernelDropped)   ||
                    IsFlagSet(flags, Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagRootChanged)     ||
                    IsFlagSet(flags, Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagMount)           ||
                    IsFlagSet(flags, Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagUnmount));
        }

        private bool DoesPathPassNameFilter(string eventPath)
        {
            bool doesPathPass = true;
            
            // If we shouldn't include subdirectories, check if this path's parent is the watch directory
            if (_includeSubdirectories == false)
            {
                // Check if the parent is the root. If so, then we'll continue processing based on the name.
                // If it isn't, then this will be set to false and we'll skip the name processing since it's irrelevant.
                string parent = System.IO.Path.GetDirectoryName(eventPath);
                doesPathPass = (parent.Equals(_fullDirectory, StringComparison.OrdinalIgnoreCase));
            }

            if (doesPathPass)
            {
                // Check if the name fits the pattern
                doesPathPass = MatchPattern(eventPath);
            }

            return doesPathPass;
        }

        private long FindRenameChangePairedChange(
            long currentIndex, 
            String[] eventPaths,
            Interop.EventStream.FSEventStreamEventFlags[] eventFlags,
            FSEventStreamEventId[] eventIds)
        {
            // Start at one past the current index and try to find the next Rename item, which should be the old path.
            for (long i = currentIndex + 1; i < eventPaths.Length; i++)
            {
                if (IsFlagSet(eventFlags[i], Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed))
                {
                    // We found match, stop looking
                    return i;
                }
            }

            return long.MinValue;
        }

        private static bool IsFlagSet(Interop.EventStream.FSEventStreamEventFlags flags, Interop.EventStream.FSEventStreamEventFlags value)
        {
            return (value & flags) == value;
        }

        private static bool DoesItemExist(string path, bool isFile)
        {
            if (isFile)
                return File.Exists(path);
            else
                return Directory.Exists(path);
        }
    }
}
