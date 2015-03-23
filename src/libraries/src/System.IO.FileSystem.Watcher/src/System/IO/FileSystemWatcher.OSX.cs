// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

using CFStringRef = System.IntPtr;
using CFArrayRef = System.IntPtr;
using FSEventStreamRef = System.IntPtr;
using size_t = System.IntPtr;
using FSEventStreamEventId = System.UInt64;
using CFTimeInterval = System.Double;
using CFRunLoopRef = System.IntPtr;

namespace System.IO
{
    // OSX implementation of FileSystemWatcher's PAL.  The main partial definition of FileSystemWatcher expects:
    // - StartRaisingEvents
    // - StopRaisingEvents
    // - FinalizeDispose

    public partial class FileSystemWatcher
    {
        // OS X has a case-sensitive File System
        internal const bool CaseSensitive = true;

        // Flags used to create the event stream
        private const Interop.CoreServices.FSEventStreamCreateFlags EventStreamFlags = (Interop.CoreServices.FSEventStreamCreateFlags.kFSEventStreamCreateFlagFileEvents |
                                                                                        Interop.CoreServices.FSEventStreamCreateFlags.kFSEventStreamCreateFlagWatchRoot);

        // The last directory we were told to watch. This allows us to be smart about how we create streams
        // and to only teardown and recreate streams when we need to.
        private String _lastWatchedDirectory = String.Empty;

        // The bitmask of events that we want to send to the user
        private Interop.CoreServices.FSEventStreamEventFlags _filterFlags;

        // The EventStream to listen for events on
        private FSEventStreamRef _eventStream = IntPtr.Zero;

        // The background thread to use to monitor events
        private Thread _watcherThread = null;

        // A reference to the RunLoop that we can use to start or stop a Watcher
        private CFRunLoopRef _watcherRunLoop = IntPtr.Zero;

        // Use an event to try to prevent StartRaisingEvents from returning before the
        // RunLoop actually begins. This will mitigate a race condition where the watcher
        // thread hasn't completed initialization and stop is called before the RunLoop even starts.
        private AutoResetEvent _runLoopStartedEvent = new AutoResetEvent(false);

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
            // stomp on the other's operation. We use the _lastWatchedDirectory instead of the
            // RunLoop or StreamRef because IntPtrs are value types and can't be locked
            lock (_lastWatchedDirectory)
            {
                // Always re-create the filter flags when start is called since they could have changed
                if (_notifyFilters.HasFlag(NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Size))
                {
                    _filterFlags = Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemInodeMetaMod | Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemFinderInfoMod;
                }
                if (_notifyFilters.HasFlag(NotifyFilters.Security))
                {
                    _filterFlags |= Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemChangeOwner | Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemXattrMod;
                }
                if (_notifyFilters.HasFlag(NotifyFilters.DirectoryName))
                {
                    _filterFlags |= Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsDir | 
                                    Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsSymlink | 
                                    Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemCreated | 
                                    Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemModified | 
                                    Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRemoved | 
                                    Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed;
                }
                if (_notifyFilters.HasFlag(NotifyFilters.FileName))
                {
                    _filterFlags |= Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsFile | 
                                    Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemIsSymlink |
                                    Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemCreated |
                                    Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemModified |
                                    Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRemoved |
                                    Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed;
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

        private void CreateStreamAndStartWatcher()
        {
            Debug.Assert(_eventStream == IntPtr.Zero);
            Debug.Assert(_watcherRunLoop == IntPtr.Zero);

            // Make sure we only do this if there is a valid directory
            if (String.IsNullOrEmpty(_directory) == false)
            {
                // Get the path to watch and put it into a CFArray.
                CFStringRef path = Interop.CoreFoundation.CFStringCreateWithCString(IntPtr.Zero, _directory, Interop.CoreFoundation.CFStringBuiltInEncodings.kCFStringEncodingUTF8);
                CFArrayRef arrPaths = Interop.CoreFoundation.CFArrayCreate(IntPtr.Zero, new CFStringRef[1] { path }, 1, IntPtr.Zero);

                // Create the event stream for the path and tell the stream to watch for file system events.
                _eventStream = Interop.CoreServices.FSEventStreamCreate(
                    IntPtr.Zero,
                    FileSystemEventCallback,
                    IntPtr.Zero,
                    arrPaths,
                    Interop.CoreServices.kFSEventStreamEventIdSinceNow,
                    0.2f,
                    EventStreamFlags);
                Debug.Assert(_eventStream != IntPtr.Zero);

                // The paths are copied inside FSEventStreamCreate so delete our references
                Interop.CoreFoundation.CFRelease(path);
                Interop.CoreFoundation.CFRelease(arrPaths);

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
            // Make sure there's a loop to clear
            if (_watcherRunLoop != IntPtr.Zero)
            {
                // Stop the RunLoop and wait for the thread to exit gracefully
                Interop.CoreFoundation.CFRunLoopStop(_watcherRunLoop);
                _watcherThread.Join();
            }

            // Clean up the EventStream, if it exists
            if (_eventStream != IntPtr.Zero)
            {
                Interop.CoreFoundation.CFRelease(_eventStream);
            }

            // Reset back to zero
            _eventStream = IntPtr.Zero;
            _watcherRunLoop = IntPtr.Zero;
        }

        private void RestartStream()
        {
            if (_eventStream != IntPtr.Zero)
            {
                // We don't need to rebuild the stream since the path is the same so just restart the stream.
                if (Interop.CoreServices.FSEventStreamStart(_eventStream) == false)
                {
                    // Pass the error back to the user and set the last watched directory to null so that we'll
                    // cleanup and recreate our streams next time Start is called.
                    OnError(new ErrorEventArgs(new IOException("", Marshal.GetLastWin32Error())));
                    _lastWatchedDirectory = String.Empty;
                }
            }
        }

        private void StopStream()
        {
            // Just stop the EventStream to be optimistic that we'll be restarted with the same path
            // and not have to teardown everything and rebuild.
            if (_eventStream != IntPtr.Zero)
            {
                Interop.CoreServices.FSEventStreamStop(_eventStream);
            }
        }

        private void WatchForFileSystemEventsThreadStart()
        {
            // Get this thread's RunLoop
            _watcherRunLoop = Interop.CoreFoundation.CFRunLoopGetCurrent();
            Debug.Assert(_watcherRunLoop != IntPtr.Zero);

            // Schedule the EventStream to run on the thread's RunLoop
            Interop.CoreServices.FSEventStreamScheduleWithRunLoop(_eventStream, _watcherRunLoop, Interop.CoreFoundation.kCFRunLoopDefaultMode);
            if (Interop.CoreServices.FSEventStreamStart(_eventStream))
            {
                // Notify the StartRaisingEvents call that we are initialized and about to start
                // so that it can return and avoid a race-condition around multiple threads calling Stop and Start
                _runLoopStartedEvent.Set();

                // Start the OS X RunLoop (a blocking call) that will pump file system changes into the callback function
                Interop.CoreFoundation.CFRunLoopRun();

                // When we get here, we've requested to stop so cleanup the EventStream and unschedule from the RunLoop
                Interop.CoreServices.FSEventStreamStop(_eventStream);
                Interop.CoreServices.FSEventStreamUnscheduleFromRunLoop(_eventStream, _watcherRunLoop, Interop.CoreFoundation.kCFRunLoopDefaultMode);
            }
            else
            {
                // An error occurred while trying to start the run loop so fail out
                Interop.CoreServices.FSEventStreamUnscheduleFromRunLoop(_eventStream, _watcherRunLoop, Interop.CoreFoundation.kCFRunLoopDefaultMode);
                OnError(new ErrorEventArgs(new IOException("", Marshal.GetLastWin32Error())));
            }
        }

        private void FileSystemEventCallback( 
            FSEventStreamRef streamRef, 
            IntPtr clientCallBackInfo, 
            size_t numEvents, 
            String[] eventPaths, 
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
            Interop.CoreServices.FSEventStreamEventFlags[] eventFlags,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]
            FSEventStreamEventId[] eventIds)
        {
            Debug.Assert((numEvents.ToInt32() == eventPaths.Length) && (numEvents.ToInt32() == eventFlags.Length) && (numEvents.ToInt32() == eventIds.Length));

            // Since renames come in pairs, when we find the first we need to search for the next one. Once we find it, we'll add it to this
            // list so when the for-loop comes across it, we'll skip it since it's already been processed as part of the original of the pair.
            System.Collections.Generic.List<FSEventStreamEventId> handledRenameEvents = new Collections.Generic.List<FSEventStreamEventId>();

            for (long i = 0; i < numEvents.ToInt32(); i++)
            {
                // First, we should check if this event should kick off a re-scan since we can't really rely on anything after this point if that is true
                if (ShouldRescanOccur(eventFlags[i]))
                {
                    OnError(new ErrorEventArgs(new IOException("", (int)eventFlags[i])));
                    break;
                }
                else if (handledRenameEvents.Contains(eventIds[i]))
                {
                    // If this event is the second in a rename pair then skip it
                    continue;
                }
                else
                {
                    // Check if the path passes the name filter and if the event passes the event filter.
                    if ((DoesPathPassNameFilter(eventPaths[i])) && (_filterFlags.HasFlag(eventFlags[i])))
                    {
                        // Remove the base directory prefix (including trailing / that OS X adds)
                        String relativePath = eventPaths[i].Remove(0, _directory.Length + 1);

                        // Check if this is a rename
                        if ((eventFlags[i] & Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed) == Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed)
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
                                String newPathRelativeName = eventPaths[pairedId].Remove(0, _directory.Length + 1);
                                NotifyRenameEventArgs(WatcherChangeTypes.Renamed, newPathRelativeName, relativePath);
                                handledRenameEvents.Add(eventIds[pairedId]);
                            }
                        }
                        else
                        {
                            // Note: the order here matters due to coalescing since two events (such as a mod followed by a delete) could be coalesced into one notification
                            //       So look for deletes first since those will mean the listening app can't do anything with the item since it doesn't exist (such as stat 
                            //       the file to determine what exactly changed, in this example).
                            if ((eventFlags[i] & Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRemoved) == Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRemoved)
                            {
                                NotifyFileSystemEventArgs(WatcherChangeTypes.Deleted, relativePath);
                            }
                            else if ((eventFlags[i] & Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemCreated) == Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemCreated)
                            {
                                // Next look for creates since a create + modification coalesced could confuse apps since a file they haven't heard of yet would get a mod event
                                NotifyFileSystemEventArgs(WatcherChangeTypes.Created, relativePath);
                            }
                            else if (((eventFlags[i] & Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemInodeMetaMod) == Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemInodeMetaMod) ||
                                     ((eventFlags[i] & Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemModified) == Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemModified) ||
                                     ((eventFlags[i] & Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemFinderInfoMod) == Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemChangeOwner) ||
                                     ((eventFlags[i] & Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemXattrMod) == Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemXattrMod))
                            {
                                // Everything else is a modification
                                NotifyFileSystemEventArgs(WatcherChangeTypes.Changed, relativePath);
                            }
                        }
                    }
                }
            }
        }

        private bool ShouldRescanOccur(Interop.CoreServices.FSEventStreamEventFlags flags)
        {
            // Check if the MustScanSubDirs flag exists
            bool shouldRescan = ((flags & Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagMustScanSubDirs) == Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagMustScanSubDirs);
            
            // Check if the UserDropped flag exists; meaning the UserMode daemon dropped a change coming from the kernel
            if (shouldRescan == false)
            {
                shouldRescan = ((flags & Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagUserDropped) == Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagUserDropped);
            }

            // Check if the KernelDropped flag exists; meaning the Kernel dropped a flag before telling the UserMode daemon about it
            if (shouldRescan == false)
            {
                shouldRescan = ((flags & Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagKernelDropped) == Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagKernelDropped);
            }

            // Check if the RootChanged flag exists; meaning the Root folder changed in some way that could affect all child items and requires a rescan to rebuild an accurate snapshot.
            if (shouldRescan == false)
            {
                shouldRescan = ((flags & Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagRootChanged) == Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagRootChanged);
            }

            // Check if the Mount flag exists; meaning a whole directory has been added under the root, which would require a rescan to rebuild an accurate snapshot.
            if (shouldRescan == false)
            {
                shouldRescan = ((flags & Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagMount) == Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagMount);
            }

            // Check if the Unmount flag exists; meaning a whole directory has been removed from under the root, which would require a rescan to rebuild an accurate snapshot
            if (shouldRescan == false)
            {
                shouldRescan = ((flags & Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagUnmount) == Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagUnmount);
            }

            return shouldRescan;
        }

        private bool DoesPathPassNameFilter(string eventPath)
        {
            bool doesPathPass = true;
            
            // If we shouldn't include subdirectories, check if this path's parent is the watch directory
            if (_includeSubdirectories == false)
            {
                // Check if the parent is the root. If so, then we'll continue processing based on the name.
                // If it isn't, then this will be set to false and we'll skip the name processing since it's irrelevant.
                String parent = System.IO.Path.GetDirectoryName(eventPath);
                doesPathPass = (parent.Equals(_directory, StringComparison.CurrentCultureIgnoreCase));
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
            Interop.CoreServices.FSEventStreamEventFlags[] eventFlags,
            FSEventStreamEventId[] eventIds)
        {
            Debug.Assert((currentIndex + 1 < eventPaths.Length) && (currentIndex + 1 < eventFlags.Length) & (currentIndex + 1 < eventIds.Length));

            // The ID of the corresponding rename
            long pairedRenameId = long.MinValue;

            // Start at one past the current index and try to find the next Rename item, which should be the old path.
            for (long i = currentIndex + 1; i < eventPaths.Length; i++)
            {
                if ((eventFlags[i] & Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed) == Interop.CoreServices.FSEventStreamEventFlags.kFSEventStreamEventFlagItemRenamed)
                {
                    // We found a match, stop looking
                    pairedRenameId = i;
                    break;
                }
            }

            return pairedRenameId;
        }

        private void StopRaisingEvents()
        {
            // Make sure the Start and Stop can be called from different threads and don't 
            // stomp on the other's operation. We use the _lastWatchedDirectory instead of the
            // RunLoop or StreamRef because IntPtrs are value types and can't be locked
            lock (_lastWatchedDirectory)
            {
                // Be optimistic and only stop the stream, don't clean up unless we need to.
                StopStream();
            }
        }
    }
}