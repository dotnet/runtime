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
            CleanupStreamAndWatcher();
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
                    _filterFlags = Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemInodeMetaMod | Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemFinderInfoMod;
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
                                    Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemModified |
                                    Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRemoved |
                                    Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed;
                }
                if ((_notifyFilters & NotifyFilters.FileName) != 0)
                {
                    _filterFlags |= Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsFile |
                                    Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsSymlink |
                                    Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemCreated |
                                    Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemModified |
                                    Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRemoved |
                                    Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed;
                }

                // The directory has not changed then just restart receiving events. If the directory has changed 
                // then we need to cleanup the old stream and kick off a new one
                if (_lastWatchedDirectory.Equals(_directory, StringComparison.CurrentCultureIgnoreCase))
                {
                    RestartStream();
                }
                else
                {
                    CleanupStreamAndWatcher();
                    CreateStreamAndStartWatcher();
                    _lastWatchedDirectory = _directory;
                }
            }
        }

        private void StopRaisingEvents()
        {
            // Make sure the Start and Stop can be called from different threads and don't 
            // stomp on the other's operation. We use the _syncLock instead of the
            // RunLoop or StreamRef because IntPtrs are value types and can't be locked
            lock (_syncLock)
            {
                // Be optimistic and only stop the stream, don't clean up unless we need to.
                StopStream();
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        // Flags used to create the event stream
        private const Interop.EventStream.FSEventStreamCreateFlags EventStreamFlags = (Interop.EventStream.FSEventStreamCreateFlags.kFSEventStreamCreateFlagFileEvents |
                                                                                        Interop.EventStream.FSEventStreamCreateFlags.kFSEventStreamCreateFlagWatchRoot);

        // The last directory we were told to watch. This allows us to be smart about how we create streams
        // and to only teardown and recreate streams when we need to.
        private string _lastWatchedDirectory = String.Empty;

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
                    throw Interop.GetExceptionForIoErrno(Marshal.GetLastWin32Error(), _fullDirectory, true);
                }

                // Take the CFStringRef and put it into an array to pass to the EventStream
                SafeCreateHandle arrPaths = Interop.CoreFoundation.CFArrayCreate(new CFStringRef[1] { path.DangerousGetHandle() }, 1);
                if (arrPaths.IsInvalid)
                {
                    path.Dispose();
                    throw Interop.GetExceptionForIoErrno(Marshal.GetLastWin32Error(), _fullDirectory, true);
                }

                // Create the callback for the EventStream
                _callback = new Interop.EventStream.FSEventStreamCallback(FileSystemEventCallback);

                // Create the event stream for the path and tell the stream to watch for file system events.
                _eventStream = Interop.EventStream.FSEventStreamCreate(
                    _callback,
                    arrPaths,
                    Interop.EventStream.kFSEventStreamEventIdSinceNow,
                    0.2f,
                    EventStreamFlags);
                if (_eventStream.IsInvalid)
                {
                    arrPaths.Dispose();
                    path.Dispose();
                    throw Interop.GetExceptionForIoErrno(Marshal.GetLastWin32Error(), _fullDirectory, true);
                }

                // Create and start our watcher thread then wait for the thread to initialize and start 
                // the RunLoop. We wait for that to prevent this function from returning before the RunLoop
                // has a chance to start so that any callers won't race with the background thread's initialization
                // and calling Stop, which would attempt to stop a RunLoop that hasn't started yet.
                _watcherThread = new Thread(new ThreadStart(WatchForFileSystemEventsThreadStart));
                _watcherThread.Start();
                _runLoopStartedEvent.WaitOne();
            }
        }

        private void CleanupStreamAndWatcher()
        {
            // If we're currently streaming, stop
            StopStream();

            // Clean up the EventStream, if it exists
            if (!_eventStream.IsInvalid)
            {
                _eventStream = new SafeEventStreamHandle(IntPtr.Zero);
            }

            // Make sure there's a loop to clear
            if (_watcherRunLoop != IntPtr.Zero)
            {
                // Stop the RunLoop and wait for the thread to exit gracefully
                Interop.RunLoop.CFRunLoopStop(_watcherRunLoop);
                _watcherThread.Join();
                _watcherRunLoop = IntPtr.Zero;
            }          

            // Cleanup the callback
            if (_callback != null)
            {
                _callback = null;
            }
        }

        private void RestartStream()
        {
            Debug.Assert(!_eventStream.IsInvalid);

            // We don't need to rebuild the stream since the path is the same so just restart the stream.
            if (Interop.EventStream.FSEventStreamStart(_eventStream) == false)
            {
                // Pass the error back to the user and set the last watched directory to null so that we'll
                // cleanup and recreate our streams next time Start is called.
                OnError(new ErrorEventArgs(new IOException("", Marshal.GetLastWin32Error())));
                _lastWatchedDirectory = String.Empty;
                _fullDirectory = String.Empty;
            }
        }

        private void StopStream()
        {
            // Just stop the EventStream to be optimistic that we'll be restarted with the same path
            // and not have to teardown everything and rebuild.
            if (!_eventStream.IsInvalid)
            {
                Interop.EventStream.FSEventStreamStop(_eventStream);
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
                else if ((DoesPathPassNameFilter(eventPaths[i])) && ((_filterFlags & eventFlags[i]) != 0))
                {
                    // Remove the base directory prefix (including trailing / that OS X adds)
                    string relativePath = eventPaths[i].Remove(0, _fullDirectory.Length + 1);

                    // Check if this is a rename
                    if ((eventFlags[i] & Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed) == Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed)
                    {
                        // Find the rename that is paired to this rename, which should be the next rename in the list
                        long pairedId = FindRenameChangePairedChange(i, eventPaths, eventFlags, eventIds);
                        if (pairedId == long.MinValue)
                        {
                            OnError(new ErrorEventArgs(new ArgumentOutOfRangeException("pairedId")));
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
                        // Note: the order here matters due to coalescing since two events (such as a mod followed by a delete) could be coalesced into one notification
                        //       So look for deletes first since those will mean the listening app can't do anything with the item since it doesn't exist (such as stat 
                        //       the file to determine what exactly changed, in this example).
                        if ((eventFlags[i] & Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRemoved) == Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRemoved)
                        {
                            NotifyFileSystemEventArgs(WatcherChangeTypes.Deleted, relativePath);
                        }
                        else if ((eventFlags[i] & Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemCreated) == Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemCreated)
                        {
                            // Next look for creates since a create + modification coalesced could confuse apps since a file they haven't heard of yet would get a mod event
                            NotifyFileSystemEventArgs(WatcherChangeTypes.Created, relativePath);
                        }
                        else if (((eventFlags[i] & Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemInodeMetaMod) == Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemInodeMetaMod) ||
                                 ((eventFlags[i] & Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemModified) == Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemModified) ||
                                 ((eventFlags[i] & Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemFinderInfoMod) == Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemChangeOwner) ||
                                 ((eventFlags[i] & Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemXattrMod) == Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemXattrMod))
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
            return (((flags & Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagMustScanSubDirs) == Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagMustScanSubDirs) ||
                    ((flags & Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagUserDropped) == Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagUserDropped) ||
                    ((flags & Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagKernelDropped) == Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagKernelDropped) ||
                    ((flags & Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagRootChanged) == Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagRootChanged) ||
                    ((flags & Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagMount) == Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagMount) ||
                    ((flags & Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagUnmount) == Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagUnmount));
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
                doesPathPass = (parent.Equals(_fullDirectory, StringComparison.CurrentCultureIgnoreCase));
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
            Debug.Assert((currentIndex + 1 < eventPaths.Length) && (currentIndex + 1 < eventFlags.Length) & (currentIndex + 1 < eventIds.Length));

            // Start at one past the current index and try to find the next Rename item, which should be the old path.
            for (long i = currentIndex + 1; i < eventPaths.Length; i++)
            {
                if ((eventFlags[i] & Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed) == Interop.EventStream.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed)
                {
                    // We found a match, stop looking
                    return i;
                }
            }

            return long.MinValue;
        }
    }
}
