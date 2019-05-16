// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    // Note: This class has an OS Limitation where the inotify API can miss events if a directory is created and immediately has
    //       changes underneath. This is due to the inotify* APIs not being recursive and needing to call inotify_add_watch on
    //       each subdirectory, causing a race between adding the watch and file system events happening.
    public partial class FileSystemWatcher
    {
        /// <summary>Starts a new watch operation if one is not currently running.</summary>
        private void StartRaisingEvents()
        {
            // If we're called when "Initializing" is true, set enabled to true
            if (IsSuspended())
            {
                _enabled = true;
                return;
            }

            // If we already have a cancellation object, we're already running.
            if (_cancellation != null)
            {
                return;
            }

            // Open an inotify file descriptor. Ideally this would be a constrained execution region, but we don't have access to 
            // PrepareConstrainedRegions. We still use a finally block to house the code that opens the handle and stores it in 
            // hopes of making it as non-interruptible as possible.  Ideally the SafeFileHandle would be allocated before the block, 
            // but SetHandle is protected and SafeFileHandle is sealed, so we're stuck doing the allocation here.
            SafeFileHandle handle;
            try { } finally
            {
                handle = Interop.Sys.INotifyInit();
                if (handle.IsInvalid)
                {
                    Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                    switch (error.Error)
                    {
                        case Interop.Error.EMFILE:
                            string maxValue = ReadMaxUserLimit(MaxUserInstancesPath);
                            string message = !string.IsNullOrEmpty(maxValue) ?
                                SR.Format(SR.IOException_INotifyInstanceUserLimitExceeded_Value, maxValue) :
                                SR.IOException_INotifyInstanceUserLimitExceeded;
                            throw new IOException(message, error.RawErrno);
                        case Interop.Error.ENFILE:
                            throw new IOException(SR.IOException_INotifyInstanceSystemLimitExceeded, error.RawErrno);
                        default:
                            throw Interop.GetExceptionForIoErrno(error);
                    }
                }
            }

            try
            {
                // Create the cancellation object that will be used by this FileSystemWatcher to cancel the new watch operation
                CancellationTokenSource cancellation = new CancellationTokenSource();

                // Start running.  All state associated with the watch operation is stored in a separate object; this is done
                // to avoid race conditions that could result if the users quickly starts/stops/starts/stops/etc. causing multiple
                // active operations to all be outstanding at the same time.
                var runner = new RunningInstance(
                    this, handle, _directory,
                    IncludeSubdirectories, NotifyFilter, cancellation.Token);

                // Now that we've created the runner, store the cancellation object and mark the instance
                // as running.  We wait to do this so that if there was a failure, StartRaisingEvents
                // may be called to try again without first having to call StopRaisingEvents.
                _cancellation = cancellation;
                _enabled = true;

                // Start the runner
                runner.Start();
            }
            catch
            {
                // If we fail to actually start the watching even though we've opened the 
                // inotify handle, close the inotify handle proactively rather than waiting for it 
                // to be finalized.
                handle.Dispose();
                throw;
            }
        }

        /// <summary>Cancels the currently running watch operation if there is one.</summary>
        private void StopRaisingEvents()
        {
            _enabled = false;

            if (IsSuspended())
                return;

            // If there's an active cancellation token, cancel and release it.
            // The cancellation token and the processing task respond to cancellation
            // to handle all other cleanup.
            var cts = _cancellation;
            if (cts != null)
            {
                _cancellation = null;
                cts.Cancel();
            }
        }

        /// <summary>Called when FileSystemWatcher is finalized.</summary>
        private void FinalizeDispose()
        {
            // The RunningInstance remains rooted and holds open the SafeFileHandle until it's explicitly
            // torn down.  FileSystemWatcher.Dispose will call StopRaisingEvents, but not on finalization;
            // thus we need to explicitly call it here.
            StopRaisingEvents();
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        /// <summary>Path to the procfs file that contains the maximum number of inotify instances an individual user may create.</summary>
        private const string MaxUserInstancesPath = "/proc/sys/fs/inotify/max_user_instances";

        /// <summary>Path to the procfs file that contains the maximum number of inotify watches an individual user may create.</summary>
        private const string MaxUserWatchesPath = "/proc/sys/fs/inotify/max_user_watches";

        /// <summary>
        /// Cancellation for the currently running watch operation.  
        /// This is non-null if an operation has been started and null if stopped.
        /// </summary>
        private CancellationTokenSource _cancellation;

        /// <summary>Reads the value of a max user limit path from procfs.</summary>
        /// <param name="path">The path to read.</param>
        /// <returns>The value read, or "0" if a failure occurred.</returns>
        private static string ReadMaxUserLimit(string path)
        {
            try { return File.ReadAllText(path).Trim(); }
            catch { return null; }
        }

        /// <summary>
        /// Maps the FileSystemWatcher's NotifyFilters enumeration to the 
        /// corresponding Interop.Sys.NotifyEvents values.
        /// </summary>
        /// <param name="filters">The filters provided the by user.</param>
        /// <returns>The corresponding NotifyEvents values to use with inotify.</returns>
        private static Interop.Sys.NotifyEvents TranslateFilters(NotifyFilters filters)
        {
            Interop.Sys.NotifyEvents result = 0;

            // We always include a few special inotify watch values that configure
            // the watch's behavior.
            result |=
                Interop.Sys.NotifyEvents.IN_ONLYDIR |     // we only allow watches on directories
                Interop.Sys.NotifyEvents.IN_EXCL_UNLINK;  // we want to stop monitoring unlinked files

            // For the Created and Deleted events, we need to always
            // register for the created/deleted inotify events, regardless
            // of the supplied filters values. We explicitly don't include IN_DELETE_SELF.  
            // The Windows implementation doesn't include notifications for the root directory, 
            // and having this for subdirectories results in duplicate notifications, one from 
            // the parent and one from self.
            result |= 
                Interop.Sys.NotifyEvents.IN_CREATE | 
                Interop.Sys.NotifyEvents.IN_DELETE;

            // For the Changed event, which inotify events we subscribe to
            // are based on the NotifyFilters supplied.
            const NotifyFilters filtersForAccess =
                NotifyFilters.LastAccess;
            const NotifyFilters filtersForModify =
                NotifyFilters.LastAccess |
                NotifyFilters.LastWrite |
                NotifyFilters.Security |
                NotifyFilters.Size;
            const NotifyFilters filtersForAttrib =
                NotifyFilters.Attributes |
                NotifyFilters.CreationTime |
                NotifyFilters.LastAccess |
                NotifyFilters.LastWrite |
                NotifyFilters.Security |
                NotifyFilters.Size;
            if ((filters & filtersForAccess) != 0)
            {
                result |= Interop.Sys.NotifyEvents.IN_ACCESS;
            }
            if ((filters & filtersForModify) != 0)
            {
                result |= Interop.Sys.NotifyEvents.IN_MODIFY;
            }
            if ((filters & filtersForAttrib) != 0)
            {
                result |= Interop.Sys.NotifyEvents.IN_ATTRIB;
            }

            // For the Rename event, we'll register for the corresponding move inotify events if the 
            // caller's NotifyFilters asks for notifications related to names.
            const NotifyFilters filtersForMoved =
                NotifyFilters.FileName |
                NotifyFilters.DirectoryName;
            if ((filters & filtersForMoved) != 0)
            {
                result |=
                    Interop.Sys.NotifyEvents.IN_MOVED_FROM |
                    Interop.Sys.NotifyEvents.IN_MOVED_TO;
            }

            return result;
        }

        /// <summary>
        /// State and processing associated with an active watch operation.  This state is kept separate from FileSystemWatcher to avoid 
        /// race conditions when a user starts/stops/starts/stops/etc. in quick succession, resulting in the potential for multiple 
        /// active operations. It also helps with avoiding rooted cycles and enabling proper finalization.
        /// </summary>
        private sealed class RunningInstance
        {
            /// <summary>
            /// The size of the native struct inotify_event.  4 32-bit integer values, the last of which is a length
            /// that indicates how many bytes follow to form the string name.
            /// </summary>
            const int c_INotifyEventSize = 16;

            /// <summary>
            /// Weak reference to the associated watcher.  A weak reference is used so that the FileSystemWatcher may be collected and finalized,
            /// causing an active operation to be torn down.  With a strong reference, a blocking read on the inotify handle will keep alive this
            /// instance which will keep alive the FileSystemWatcher which will not be finalizable and thus which will never signal to the blocking
            /// read to wake up in the event that the user neglects to stop raising events.
            /// </summary>
            private readonly WeakReference<FileSystemWatcher> _weakWatcher;
            /// <summary>
            /// The path for the primary watched directory.
            /// </summary>
            private readonly string _directoryPath;
            /// <summary>
            /// The inotify handle / file descriptor
            /// </summary>
            private readonly SafeFileHandle _inotifyHandle;
            /// <summary>
            /// Buffer used to store raw bytes read from the inotify handle.
            /// </summary>
            private readonly byte[] _buffer;
            /// <summary>
            /// The number of bytes read into the _buffer.
            /// </summary>
            private int _bufferAvailable;
            /// <summary>
            /// The next position in _buffer from which an event should be read.
            /// </summary>
            private int _bufferPos;
            /// <summary>
            /// Filters to use when adding a watch on directories.
            /// </summary>
            private readonly NotifyFilters _notifyFilters;
            private readonly Interop.Sys.NotifyEvents _watchFilters;
            /// <summary>
            /// Whether to monitor subdirectories.  Unlike Win32, inotify does not implicitly monitor subdirectories;
            /// watches must be explicitly added for those subdirectories.
            /// </summary>
            private readonly bool _includeSubdirectories;
            /// <summary>
            /// Token to monitor for cancellation requests, upon which processing is stopped and all
            /// state is cleaned up.
            /// </summary>
            private readonly CancellationToken _cancellationToken;
            /// <summary>
            /// Mapping from watch descriptor (as returned by inotify_add_watch) to state for
            /// the associated directory being watched.  Events from inotify include only relative
            /// names, so the watch descriptor in an event must be used to look up the associated
            /// directory path in order to convert the relative filename into a full path.
            /// </summary>
            private readonly Dictionary<int, WatchedDirectory> _wdToPathMap = new Dictionary<int, WatchedDirectory>();
            /// <summary>
            /// Maximum length of a name returned from inotify event.
            /// </summary>
            private const int NAME_MAX = 255; // from limits.h

            /// <summary>Initializes the instance with all state necessary to operate a watch.</summary>
            internal RunningInstance(
                FileSystemWatcher watcher, SafeFileHandle inotifyHandle, string directoryPath,
                bool includeSubdirectories, NotifyFilters notifyFilters, CancellationToken cancellationToken)
            {
                Debug.Assert(watcher != null);
                Debug.Assert(inotifyHandle != null && !inotifyHandle.IsInvalid && !inotifyHandle.IsClosed);
                Debug.Assert(directoryPath != null);

                _weakWatcher = new WeakReference<FileSystemWatcher>(watcher);
                _inotifyHandle = inotifyHandle;
                _directoryPath = directoryPath;
                _buffer = watcher.AllocateBuffer();
                Debug.Assert(_buffer != null && _buffer.Length > (c_INotifyEventSize + NAME_MAX + 1));
                _includeSubdirectories = includeSubdirectories;
                _notifyFilters = notifyFilters;
                _watchFilters = TranslateFilters(notifyFilters);
                _cancellationToken = cancellationToken;

                // Add a watch for this starting directory.  We keep track of the watch descriptor => directory information
                // mapping in a dictionary; this is needed in order to be able to determine the containing directory
                // for all notifications so that we can reconstruct the full path.
                AddDirectoryWatchUnlocked(null, directoryPath);
            }

            internal void Start()
            {
                // Schedule a task to read from the inotify queue and process the events.
                Task.Factory.StartNew(obj => ((RunningInstance)obj).ProcessEvents(),
                    this, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                // PERF: As needed, we can look into making this use async I/O rather than burning
                // a thread that blocks in the read syscall.
            }

            /// <summary>Object to use for synchronizing access to state when necessary.</summary>
            private object SyncObj { get { return _wdToPathMap; } }

            /// <summary>Adds a watch on a directory to the existing inotify handle.</summary>
            /// <param name="parent">The parent directory entry.</param>
            /// <param name="directoryName">The new directory path to monitor, relative to the root.</param>
            private void AddDirectoryWatch(WatchedDirectory parent, string directoryName)
            {
                lock (SyncObj)
                {
                    // The read syscall on the file descriptor will block until either close is called or until
                    // all previously added watches are removed.  We don't want to rely on close, as a) that could
                    // lead to race conditions where we inadvertently read from a recycled file descriptor, and b)
                    // the SafeFileHandle that wraps the file descriptor can't be disposed (thus closing
                    // the underlying file descriptor and allowing read to wake up) while there's an active ref count
                    // against the handle, so we'd deadlock if we relied on that approach.  Instead, we want to follow
                    // the approach of removing all watches when we're done, which means we also don't want to 
                    // add any new watches once the count hits zero.
                    if (parent == null || _wdToPathMap.Count > 0)
                    {
                        Debug.Assert(parent != null || _wdToPathMap.Count == 0);
                        AddDirectoryWatchUnlocked(parent, directoryName);
                    }
                }
            }

            /// <summary>Adds a watch on a directory to the existing inotify handle.</summary>
            /// <param name="parent">The parent directory entry.</param>
            /// <param name="directoryName">The new directory path to monitor, relative to the root.</param>
            private void AddDirectoryWatchUnlocked(WatchedDirectory parent, string directoryName)
            {
                string fullPath = parent != null ? parent.GetPath(false, directoryName) : directoryName;

                // inotify_add_watch will fail if this is a symlink, so check that we didn't get a symlink
                Interop.Sys.FileStatus status = default(Interop.Sys.FileStatus);
                if ((Interop.Sys.LStat(fullPath, out status) == 0) &&
                    ((status.Mode & (uint)Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFLNK))
                {
                    return;
                }

                // Add a watch for the full path.  If the path is already being watched, this will return 
                // the existing descriptor.  This works even in the case of a rename. We also add the DONT_FOLLOW
                // and EXCL_UNLINK flags to keep parity with Windows where we don't pickup symlinks or unlinked
                // files (which don't exist in Windows)
                int wd = Interop.Sys.INotifyAddWatch(_inotifyHandle, fullPath, (uint)(this._watchFilters | Interop.Sys.NotifyEvents.IN_DONT_FOLLOW | Interop.Sys.NotifyEvents.IN_EXCL_UNLINK));
                if (wd == -1)
                {
                    // If we get an error when trying to add the watch, don't let that tear down processing.  Instead,
                    // raise the Error event with the exception and let the user decide how to handle it.

                    Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                    Exception exc;
                    if (error.Error == Interop.Error.ENOSPC)
                    {
                        string maxValue = ReadMaxUserLimit(MaxUserWatchesPath);
                        string message = !string.IsNullOrEmpty(maxValue) ?
                            SR.Format(SR.IOException_INotifyWatchesUserLimitExceeded_Value, maxValue) :
                            SR.IOException_INotifyWatchesUserLimitExceeded;
                        exc = new IOException(message, error.RawErrno);
                    }
                    else
                    {
                        exc = Interop.GetExceptionForIoErrno(error, fullPath);
                    }

                    FileSystemWatcher watcher;
                    if (_weakWatcher.TryGetTarget(out watcher))
                    {
                        watcher.OnError(new ErrorEventArgs(exc));
                    }

                    return;
                }

                // Then store the path information into our map.
                WatchedDirectory directoryEntry;
                bool isNewDirectory = false;
                if (_wdToPathMap.TryGetValue(wd, out directoryEntry))
                {
                    // The watch descriptor was already in the map.  Hard links on directories
                    // aren't possible, and symlinks aren't annotated as IN_ISDIR,
                    // so this is a rename. (In extremely remote cases, this could be
                    // a recycled watch descriptor if many, many events were lost
                    // such that our dictionary got very inconsistent with the state
                    // of the world, but there's little that can be done about that.)
                    if (directoryEntry.Parent != parent)
                    {
                        if (directoryEntry.Parent != null)
                        {
                            directoryEntry.Parent.Children.Remove (directoryEntry);
                        }
                        directoryEntry.Parent = parent;
                        if (parent != null)
                        {
                            parent.InitializedChildren.Add (directoryEntry);
                        }
                    }
                    directoryEntry.Name = directoryName;
                }
                else
                {
                    // The watch descriptor wasn't in the map.  This is a creation.
                    directoryEntry = new WatchedDirectory 
                    {
                        Parent = parent,
                        WatchDescriptor = wd,
                        Name = directoryName
                    };
                    if (parent != null)
                    {
                        parent.InitializedChildren.Add (directoryEntry);
                    }
                    _wdToPathMap.Add(wd, directoryEntry);
                    isNewDirectory = true;
                }

                // Since inotify doesn't handle nesting implicitly, explicitly
                // add a watch for each child directory if the developer has
                // asked for subdirectories to be included.
                if (isNewDirectory && _includeSubdirectories)
                {
                    // This method is recursive.  If we expect to see hierarchies
                    // so deep that it would cause us to overflow the stack, we could
                    // consider using an explicit stack object rather than recursion.
                    // This is unlikely, however, given typical directory names
                    // and max path limits.
                    foreach (string subDir in Directory.EnumerateDirectories(fullPath))
                    {
                        AddDirectoryWatchUnlocked(directoryEntry, System.IO.Path.GetFileName(subDir));
                        // AddDirectoryWatchUnlocked will add the new directory to 
                        // this.Children, so we don't have to / shouldn't also do it here.
                    }
                }
            }

            /// <summary>Removes the watched directory from our state, and optionally removes the inotify watch itself.</summary>
            /// <param name="directoryEntry">The directory entry to remove.</param>
            /// <param name="removeInotify">true to remove the inotify watch; otherwise, false.  The default is true.</param>
            private void RemoveWatchedDirectory(WatchedDirectory directoryEntry, bool removeInotify = true)
            {
                Debug.Assert (_includeSubdirectories);
                lock (SyncObj)
                {
                    if (directoryEntry.Parent != null)
                    {
                        directoryEntry.Parent.Children.Remove (directoryEntry);
                    }
                    RemoveWatchedDirectoryUnlocked (directoryEntry, removeInotify);
                }
            }

            /// <summary>Removes the watched directory from our state, and optionally removes the inotify watch itself.</summary>
            /// <param name="directoryEntry">The directory entry to remove.</param>
            /// <param name="removeInotify">true to remove the inotify watch; otherwise, false.  The default is true.</param>
            private void RemoveWatchedDirectoryUnlocked(WatchedDirectory directoryEntry, bool removeInotify)
            {
                // If the directory has children, recursively remove them (see comments on recursion in AddDirectoryWatch).
                if (directoryEntry.Children != null) 
                {
                    foreach (WatchedDirectory child in directoryEntry.Children) 
                    {
                        RemoveWatchedDirectoryUnlocked (child, removeInotify);
                    }
                    directoryEntry.Children = null;
                }

                // Then remove the directory itself.
                _wdToPathMap.Remove(directoryEntry.WatchDescriptor);

                // And if the caller has requested, remove the associated inotify watch.
                if (removeInotify)
                {
                    // Remove the inotify watch.  This could fail if our state has become inconsistent
                    // with the state of the world (e.g. due to lost events).  So we don't want failures
                    // to throw exceptions, but we do assert to detect coding problems during debugging.
                    int result = Interop.Sys.INotifyRemoveWatch(_inotifyHandle, directoryEntry.WatchDescriptor);
                    Debug.Assert(result >= 0);
                }
            }

            /// <summary>
            /// Callback invoked when cancellation is requested.  Removes all watches, 
            /// which will cause the active processing loop to shutdown.
            /// </summary>
            private void CancellationCallback()
            {
                lock (SyncObj)
                {
                    // Remove all watches (inotiy_rm_watch) and clear out the map.
                    // No additional watches will be added after this point.
                    foreach (int wd in this._wdToPathMap.Keys)
                    {
                        int result = Interop.Sys.INotifyRemoveWatch(_inotifyHandle, wd);
                        Debug.Assert(result >= 0); // ignore errors; they're non-fatal, but they also shouldn't happen
                    }

                    _wdToPathMap.Clear();
                }
            }

            /// <summary>
            /// Main processing loop.  This is currently implemented as a synchronous operation that continually
            /// reads events and processes them... in the future, this could be changed to use asynchronous processing
            /// if the impact of using a thread-per-FileSystemWatcher is too high.
            /// </summary>
            private void ProcessEvents()
            {
                // When cancellation is requested, clear out all watches.  This should force any active or future reads 
                // on the inotify handle to return 0 bytes read immediately, allowing us to wake up from the blocking call 
                // and exit the processing loop and clean up.
                var ctr = _cancellationToken.UnsafeRegister(obj => ((RunningInstance)obj).CancellationCallback(), this);
                try
                {
                    // Previous event information
                    ReadOnlySpan<char> previousEventName = ReadOnlySpan<char>.Empty;
                    WatchedDirectory previousEventParent = null;
                    uint previousEventCookie = 0;

                    // Process events as long as we're not canceled and there are more to read...
                    NotifyEvent nextEvent;
                    while (!_cancellationToken.IsCancellationRequested && TryReadEvent(out nextEvent))
                    {
                        // Try to get the actual watcher from our weak reference.  We maintain a weak reference most of the time
                        // so as to avoid a rooted cycle that would prevent our processing loop from ever ending
                        // if the watcher is dropped by the user without being disposed. If we can't get the watcher,
                        // there's nothing more to do (we can't raise events), so bail.
                        FileSystemWatcher watcher;
                        if (!_weakWatcher.TryGetTarget(out watcher))
                        {
                            break;
                        }

                        uint mask = nextEvent.mask;
                        ReadOnlySpan<char> expandedName = ReadOnlySpan<char>.Empty;
                        WatchedDirectory associatedDirectoryEntry = null;

                        // An overflow event means that we can't trust our state without restarting since we missed events and 
                        // some of those events could be a directory create, meaning we wouldn't have added the directory to the 
                        // watch and would not provide correct data to the caller.
                        if ((mask & (uint)Interop.Sys.NotifyEvents.IN_Q_OVERFLOW) != 0)
                        {
                            // Notify the caller of the error and, if the includeSubdirectories flag is set, restart to pick up any
                            // potential directories we missed due to the overflow.
                            watcher.NotifyInternalBufferOverflowEvent();
                            if (_includeSubdirectories)
                            {
                                watcher.Restart();
                            }
                            break;
                        }
                        else
                        {
                            // Look up the directory information for the supplied wd
                            lock (SyncObj)
                            {
                                if (!_wdToPathMap.TryGetValue(nextEvent.wd, out associatedDirectoryEntry))
                                {
                                    // The watch descriptor could be missing from our dictionary if it was removed
                                    // due to cancellation, or if we already removed it and this is a related event
                                    // like IN_IGNORED.  In any case, just ignore it... even if for some reason we 
                                    // should have the value, there's little we can do about it at this point,
                                    // and there's no more processing of this event we can do without it.
                                    continue;
                                }
                            }
                            expandedName = associatedDirectoryEntry.GetPath(true, nextEvent.name);
                        }

                        // To match Windows, ignore all changes that happen on the root folder itself
                        if (expandedName.IsEmpty)
                        {
                            watcher = null;
                            continue;
                        }

                        // Determine whether the affected object is a directory (rather than a file).
                        // If it is, we may need to do special processing, such as adding a watch for new 
                        // directories if IncludeSubdirectories is enabled.  Since we're only watching
                        // directories, any IN_IGNORED event is also for a directory.
                        bool isDir = (mask & (uint)(Interop.Sys.NotifyEvents.IN_ISDIR | Interop.Sys.NotifyEvents.IN_IGNORED)) != 0;

                        // Renames come in the form of two events: IN_MOVED_FROM and IN_MOVED_TO.
                        // In general, these should come as a sequence, one immediately after the other.
                        // So, we delay raising an event for IN_MOVED_FROM until we see what comes next.
                        if (!previousEventName.IsEmpty && ((mask & (uint)Interop.Sys.NotifyEvents.IN_MOVED_TO) == 0 || previousEventCookie != nextEvent.cookie))
                        {
                            // IN_MOVED_FROM without an immediately-following corresponding IN_MOVED_TO.
                            // We have to assume that it was moved outside of our root watch path, which
                            // should be considered a deletion to match Win32 behavior.
                            // But since we explicitly added watches on directories, if it's a directory it'll
                            // still be watched, so we need to explicitly remove the watch.
                            if (previousEventParent != null && previousEventParent.Children != null)
                            {
                                // previousEventParent will be non-null iff the IN_MOVED_FROM
                                // was for a directory, in which case previousEventParent is that directory's
                                // parent and previousEventName is the name of the directory to be removed.
                                foreach (WatchedDirectory child in previousEventParent.Children)
                                {
                                    if (previousEventName.Equals(child.Name, StringComparison.Ordinal))
                                    {
                                        RemoveWatchedDirectory(child);
                                        break;
                                    }
                                }
                            }

                            // Then fire the deletion event, even though the event was IN_MOVED_FROM.
                            watcher.NotifyFileSystemEventArgs(WatcherChangeTypes.Deleted, previousEventName);

                            previousEventName = null;
                            previousEventParent = null;
                            previousEventCookie = 0;
                        }

                        // If the event signaled that there's a new subdirectory and if we're monitoring subdirectories,
                        // add a watch for it.
                        const Interop.Sys.NotifyEvents AddMaskFilters = Interop.Sys.NotifyEvents.IN_CREATE | Interop.Sys.NotifyEvents.IN_MOVED_TO;
                        bool addWatch = ((mask & (uint)AddMaskFilters) != 0);
                        if (addWatch && isDir && _includeSubdirectories)
                        {
                            AddDirectoryWatch(associatedDirectoryEntry, nextEvent.name);
                        }

                        // Check if the event should have been filtered but was unable because of inotify's inability
                        // to filter files vs directories.
                        const Interop.Sys.NotifyEvents fileDirEvents = Interop.Sys.NotifyEvents.IN_CREATE |
                                Interop.Sys.NotifyEvents.IN_DELETE |
                                Interop.Sys.NotifyEvents.IN_MOVED_FROM |
                                Interop.Sys.NotifyEvents.IN_MOVED_TO;
                        if ((((uint)fileDirEvents & mask) > 0) &&
                            (isDir && ((_notifyFilters & NotifyFilters.DirectoryName) == 0) ||
                            (!isDir && ((_notifyFilters & NotifyFilters.FileName) == 0))))
                        {
                            continue;
                        }

                        const Interop.Sys.NotifyEvents switchMask = fileDirEvents | Interop.Sys.NotifyEvents.IN_IGNORED |
                            Interop.Sys.NotifyEvents.IN_ACCESS | Interop.Sys.NotifyEvents.IN_MODIFY | Interop.Sys.NotifyEvents.IN_ATTRIB;
                        switch ((Interop.Sys.NotifyEvents)(mask & (uint)switchMask))
                        {
                            case Interop.Sys.NotifyEvents.IN_CREATE:
                                watcher.NotifyFileSystemEventArgs(WatcherChangeTypes.Created, expandedName);
                                break;
                            case Interop.Sys.NotifyEvents.IN_IGNORED:
                                // We're getting an IN_IGNORED because a directory watch was removed.
                                // and we're getting this far in our code because we still have an entry for it
                                // in our dictionary.  So we want to clean up the relevant state, but not clean
                                // attempt to call back to inotify to remove the watches.
                                RemoveWatchedDirectory(associatedDirectoryEntry, removeInotify:false);
                                break;
                            case Interop.Sys.NotifyEvents.IN_DELETE:
                                watcher.NotifyFileSystemEventArgs(WatcherChangeTypes.Deleted, expandedName);
                                // We don't explicitly RemoveWatchedDirectory here, as that'll be handled
                                // by IN_IGNORED processing if this is a directory.
                                break;
                            case Interop.Sys.NotifyEvents.IN_ACCESS:
                            case Interop.Sys.NotifyEvents.IN_MODIFY:
                            case Interop.Sys.NotifyEvents.IN_ATTRIB:
                                watcher.NotifyFileSystemEventArgs(WatcherChangeTypes.Changed, expandedName);
                                break;
                            case Interop.Sys.NotifyEvents.IN_MOVED_FROM:
                                // We need to check if this MOVED_FROM event is standalone - meaning the item was moved out
                                // of scope. We do this by checking if we are at the end of our buffer (meaning no more events) 
                                // and if there is data to be read by polling the fd. If there aren't any more events, fire the
                                // deleted event; if there are more events, handle it via next pass. This adds an additional
                                // edge case where we get the MOVED_FROM event and the MOVED_TO event hasn't been generated yet
                                // so we will send a DELETE for this event and a CREATE when the MOVED_TO is eventually processed.
                                if (_bufferPos == _bufferAvailable)
                                {
                                    // Do the poll with a small timeout value.  Community research showed that a few milliseconds
                                    // was enough to allow the vast majority of MOVED_TO events that were going to show
                                    // up to actually arrive.  This doesn't need to be perfect; there's always the chance
                                    // that a MOVED_TO could show up after whatever timeout is specified, in which case
                                    // it'll just result in a delete + create instead of a rename.  We need the value to be
                                    // small so that we don't significantly delay the delivery of the deleted event in case
                                    // that's actually what's needed (otherwise it'd be fine to block indefinitely waiting
                                    // for the next event to arrive).
                                    const int MillisecondsTimeout = 2;
                                    Interop.Sys.PollEvents events;
                                    Interop.Sys.Poll(_inotifyHandle, Interop.Sys.PollEvents.POLLIN, MillisecondsTimeout, out events);

                                    // If we error or don't have any signaled handles, send the deleted event
                                    if (events == Interop.Sys.PollEvents.POLLNONE)
                                    {
                                        // There isn't any more data in the queue so this is a deleted event
                                        watcher.NotifyFileSystemEventArgs(WatcherChangeTypes.Deleted, expandedName);
                                        break;
                                    }
                                }

                                // We will set these values if the buffer has more data OR if the poll call tells us that more data is available.
                                previousEventName = expandedName;
                                previousEventParent = isDir ? associatedDirectoryEntry : null;
                                previousEventCookie = nextEvent.cookie;

                                break;
                            case Interop.Sys.NotifyEvents.IN_MOVED_TO:
                                if (previousEventName != null)
                                {
                                    // If the previous name from IN_MOVED_FROM is non-null, then this is a rename.
                                    watcher.NotifyRenameEventArgs(WatcherChangeTypes.Renamed, expandedName, previousEventName);
                                }
                                else
                                {
                                    // If it is null, then we didn't get an IN_MOVED_FROM (or we got it a long time
                                    // ago and treated it as a deletion), in which case this is considered a creation.
                                    watcher.NotifyFileSystemEventArgs(WatcherChangeTypes.Created, expandedName);
                                }
                                previousEventName = ReadOnlySpan<char>.Empty;
                                previousEventParent = null;
                                previousEventCookie = 0;
                                break;
                        }

                        // Drop our strong reference to the watcher now that we're potentially going to block again for another read
                        watcher = null;
                    }
                }
                catch (Exception exc)
                {
                    FileSystemWatcher watcher;
                    if (_weakWatcher.TryGetTarget(out watcher))
                    {
                        watcher.OnError(new ErrorEventArgs(exc));
                    }
                }
                finally
                {
                    ctr.Dispose();
                    _inotifyHandle.Dispose();
                }
            }

            /// <summary>Read event from the inotify handle into the supplied event object.</summary>
            /// <param name="notifyEvent">The event object to be populated.</param>
            /// <returns><see langword="true"/> if event was read successfully, <see langword="false"/> otherwise.</returns>
            private bool TryReadEvent(out NotifyEvent notifyEvent)
            {
                Debug.Assert(_buffer != null);
                Debug.Assert(_buffer.Length > 0);
                Debug.Assert(_bufferAvailable >= 0 && _bufferAvailable <= _buffer.Length);
                Debug.Assert(_bufferPos >= 0 && _bufferPos <= _bufferAvailable);

                // Read more data into our buffer if we need it
                if (_bufferAvailable == 0 || _bufferPos == _bufferAvailable)
                {
                    // Read from the handle.  This will block until either data is available
                    // or all watches have been removed, in which case zero bytes are read.
                    unsafe
                    {
                        try
                        {
                            fixed (byte* buf = &_buffer[0])
                            {
                                _bufferAvailable = Interop.CheckIo(Interop.Sys.Read(_inotifyHandle, buf, this._buffer.Length), 
                                    isDirectory: true);
                                Debug.Assert(_bufferAvailable <= this._buffer.Length);
                            }
                        }
                        catch (ArgumentException)
                        {
                            _bufferAvailable = 0;
                            Debug.Fail("Buffer provided to read was too small");
                        }
                        Debug.Assert(_bufferAvailable >= 0);
                    }
                    if (_bufferAvailable == 0)
                    {
                        notifyEvent = default(NotifyEvent);
                        return false;
                    }
                    Debug.Assert(_bufferAvailable >= c_INotifyEventSize);
                    _bufferPos = 0;
                }

                // Parse each event:
                //     struct inotify_event {
                //         int      wd;
                //         uint32_t mask;
                //         uint32_t cookie;
                //         uint32_t len;
                //         char     name[]; // length determined by len; at least 1 for required null termination
                //     };
                Debug.Assert(_bufferPos + c_INotifyEventSize <= _bufferAvailable);
                NotifyEvent readEvent;
                readEvent.wd = BitConverter.ToInt32(_buffer, _bufferPos);
                readEvent.mask = BitConverter.ToUInt32(_buffer, _bufferPos + 4);       // +4  to get past wd
                readEvent.cookie = BitConverter.ToUInt32(_buffer, _bufferPos + 8);     // +8  to get past wd, mask
                int nameLength = (int)BitConverter.ToUInt32(_buffer, _bufferPos + 12); // +12 to get past wd, mask, cookie
                readEvent.name = ReadName(_bufferPos + c_INotifyEventSize, nameLength);  // +16 to get past wd, mask, cookie, len
                _bufferPos += c_INotifyEventSize + nameLength;
                
                notifyEvent = readEvent;
                return true;
            }

            /// <summary>
            /// Reads a UTF8 string from _buffer starting at the specified position and up to
            /// the specified length.  Null termination is trimmed off (the length may include
            /// many null bytes, not just one, or it may include none).
            /// </summary>
            /// <param name="position"></param>
            /// <param name="nameLength"></param>
            /// <returns></returns>
            private string ReadName(int position, int nameLength)
            {
                Debug.Assert(position > 0);
                Debug.Assert(nameLength >= 0 && (position + nameLength) <= _buffer.Length);

                int lengthWithoutNullTerm = nameLength;
                for (int i = 0; i < nameLength; i++)
                {
                    if (_buffer[position + i] == '\0')
                    {
                        lengthWithoutNullTerm = i;
                        break;
                    }
                }
                Debug.Assert(lengthWithoutNullTerm <= nameLength); // should be null terminated or empty

                return lengthWithoutNullTerm > 0 ?
                    Encoding.UTF8.GetString(_buffer, position, lengthWithoutNullTerm) :
                    string.Empty;
            }

            /// <summary>An event read and translated from the inotify handle.</summary>
            /// <remarks>
            /// Unlike it's native counterpart, this struct stores a string name rather than
            /// an integer length and a char[].  It is not directly marshalable.
            /// </remarks>
            private struct NotifyEvent
            {
                internal int wd;
                internal uint mask;
                internal uint cookie;
                internal string name;
            }

            /// <summary>State associated with a watched directory.</summary>
            private sealed class WatchedDirectory
            {
                /// <summary>A StringBuilder cached on the current thread to avoid allocations when possible.</summary>
                [ThreadStatic]
                private static StringBuilder t_builder;

                /// <summary>The parent directory.</summary>
                internal WatchedDirectory Parent;

                /// <summary>The watch descriptor associated with this directory.</summary>
                internal int WatchDescriptor;

                /// <summary>The filename of this directory.</summary>
                internal string Name;

                /// <summary>Child directories of this directory for which we added explicit watches.</summary>
                internal List<WatchedDirectory> Children;

                /// <summary>Child directories of this directory for which we added explicit watches.  This is the same as Children, but ensured to be initialized as non-null.</summary>
                internal List<WatchedDirectory> InitializedChildren
                {
                    get
                    {
                        if (Children == null)
                        {
                            Children = new List<WatchedDirectory> ();
                        }
                        return Children;
                    }
                }

                // PERF: Work is being done here proportionate to depth of watch directories.
                // If this becomes a bottleneck, we'll need to come up with another mechanism
                // for obtaining and keeping paths up to date, for example storing the full path
                // in each WatchedDirectory node and recursively updating all children on a move,
                // which we can do given that we store all children.  For now we're not doing that
                // because it's not a clear win: either you update all children recursively when
                // a directory moves / is added, or you compute each name when an event occurs.
                // The former is better if there are going to be lots of events causing lots
                // of traversals to compute names, and the latter is better for big directory
                // structures that incur fewer file events.

                /// <summary>Gets the path of this directory.</summary>
                /// <param name="relativeToRoot">Whether to get a path relative to the root directory being watched, or a full path.</param>
                /// <param name="additionalName">An additional name to include in the path, relative to this directory.</param>
                /// <returns>The computed path.</returns>
                internal string GetPath(bool relativeToRoot, string additionalName = null)
                {
                    // Use our cached builder
                    StringBuilder builder = t_builder;
                    if (builder == null)
                    {
                        t_builder = builder = new StringBuilder();
                    }
                    builder.Clear();

                    // Write the directory's path.  Then if an additional filename was supplied, append it
                    Write(builder, relativeToRoot);
                    if (additionalName != null)
                    {
                        AppendSeparatorIfNeeded(builder);
                        builder.Append(additionalName);
                    }
                    return builder.ToString();
                }

                /// <summary>Write's this directory's path to the builder.</summary>
                /// <param name="builder">The builder to which to write.</param>
                /// <param name="relativeToRoot">
                /// true if the path should be relative to the root directory being watched.
                /// false if the path should be a full file system path, including that of
                /// the root directory being watched.
                /// </param>
                private void Write(StringBuilder builder, bool relativeToRoot)
                {
                    // This method is recursive.  If we expect to see hierarchies
                    // so deep that it would cause us to overflow the stack, we could
                    // consider using an explicit stack object rather than recursion.
                    // This is unlikely, however, given typical directory names
                    // and max path limits.

                    // First append the parent's path
                    if (Parent != null)
                    {
                        Parent.Write(builder, relativeToRoot);
                        AppendSeparatorIfNeeded(builder);
                    }

                    // Then append ours.  In the case of the root directory
                    // being watched, we only append its name if the caller
                    // has asked for a full path.
                    if (Parent != null || !relativeToRoot)
                    {
                        builder.Append(Name);
                    }
                }

                /// <summary>Adds a directory path separator to the end of the builder if one isn't there.</summary>
                /// <param name="builder">The builder.</param>
                private static void AppendSeparatorIfNeeded(StringBuilder builder)
                {
                    if (builder.Length > 0)
                    {
                        char c = builder[builder.Length - 1];
                        if (c != System.IO.Path.DirectorySeparatorChar && c != System.IO.Path.AltDirectorySeparatorChar)
                        {
                            builder.Append(System.IO.Path.DirectorySeparatorChar);
                        }
                    }
                }
            }
        }

    }
}
