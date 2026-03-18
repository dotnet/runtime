// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    // Implementation notes:
    //
    // Missed events for recursive watching:
    //   The inotify APIs are not recursive. We need to call inotify_add_watch when we detect a child directory to track it.
    //   Events that occurred on the directory before we've added it will be lost.
    //
    // Path vs directory:
    //   Note that inotify does not watch a path, but it watches directories.
    //   When a path is passed to inotify_add_watch, the directory is looked up by the kernel and a watch descriptor (wd) is returned for watching that directory.
    //   If the directory is moved to a different path, inotify will continue to reports its events.
    //   If we have previously added a watch for a path, and we call inotify_add_watch again for that path then:
    //   - if the looked up directory is still the same, the same wd will be returned, or
    //   - if the path now refers to a different directory, another wd will be returned.
    //
    //   For each FileSystemWatcher we use a Watcher object that represents all inotify operations performed for that FileSystemWatcher.
    //   To represent the difference explained above (path vs directory) we use a WatchedDirectory object to represent a path that is watched
    //   and a separate Watch object that represent the wd returned by the inotify_add_watch.
    //   Each WatchedDirectory has a single Watch, while a Watch may be used by several WatchDirectories.
    //   When there are no more WatchDirectories using the Watch, we can remove it.
    //
    // Locking:
    //   To prevent deadlocks, the locks (as needed) should be taken in this order: s_watchersLock, _addLock, lock on Watcher instance, lock on Watch instance.
    //
    // Shared inotify instance:
    //   By default, the number of inotify instances per user is limited to 128.
    //   Because of this low limit, we make all the FileSystemWatchers share a single inotify instance to reduce contention with other processes.
    //   A dedicated thread dequeues the inotify events. From the inotify events, FileSystemWatcher events are emitted from the ThreadPool.
    //   This stops FileSystemWatcher event handlers to block one another, or them blocking the inotify thread which could cause the inotify event queue to overflow.
    //   This requires us to use IN_MASK_ADD which may cause us to continue receive events that no FileSystemWatcher is still interested in.
    public partial class FileSystemWatcher
    {
        private const int PATH_MAX = 4096;

        /// <summary>Starts a new watch operation if one is not currently running.</summary>
        private void StartRaisingEvents()
        {
            // If we're called when "Initializing" is true, set enabled to true
            if (IsSuspended())
            {
                _enabled = true;
                return;
            }

            // If we already have a watcher object, we're already running.
            if (_watcher != null)
            {
                return;
            }

            _watcher = new INotify.Watcher(this);
            _enabled = true;

            _watcher.Start();
        }

        /// <summary>Cancels the currently running watch operation if there is one.</summary>
        private void StopRaisingEvents()
        {
            _enabled = false;

            if (IsSuspended())
                return;

            _watcher?.Stop();
            _watcher = null;
        }

        /// <summary>Called when FileSystemWatcher is finalized.</summary>
        private void FinalizeDispose()
        {
            // The Watcher remains rooted and holds open the SafeFileHandle until it's explicitly
            // torn down.  FileSystemWatcher.Dispose will call StopRaisingEvents, but not on finalization;
            // thus we need to explicitly call it here.
            StopRaisingEvents();
        }

        /// <summary>Path to the procfs file that contains the maximum number of inotify instances an individual user may create.</summary>
        private const string MaxUserInstancesPath = "/proc/sys/fs/inotify/max_user_instances";

        /// <summary>Path to the procfs file that contains the maximum number of inotify watches an individual user may create.</summary>
        private const string MaxUserWatchesPath = "/proc/sys/fs/inotify/max_user_watches";

        private INotify.Watcher? _watcher;

        private static void RestartForInternalBufferSize()
        {
            // The implementation is not using InternalBufferSize. There's no need to restart.
        }

        /// <summary>Reads the value of a max user limit path from procfs.</summary>
        /// <param name="path">The path to read.</param>
        /// <returns>The value read, or "0" if a failure occurred.</returns>
        private static string? ReadMaxUserLimit(string path)
        {
            try { return File.ReadAllText(path).Trim(); }
            catch { return null; }
        }

        private sealed class INotify
        {
            /// <summary>
            /// The size of the native struct inotify_event.  4 32-bit integer values, the last of which is a length
            /// that indicates how many bytes follow to form the string name.
            /// </summary>
            private const int INotifyEventSize = 16;

            // The name buffer in struct inotify_event is 0-256 bytes making the total inotify_event size 16-272 bytes.
            // The below buffer fits at 60+ events of the largest events.
            // For a typical file name size of <32 bytes, we can receive 300+ events in a single read.
            // This buffer size is assumed to be plenty because the read loop dispatches the work for user event handling to the ThreadPool and then performs a new read.
            private const int BufferSize = 16384;

            // Guards the watchers of the inotify instance and starting the inotify thread.
            private static readonly object s_watchersLock = new();
            private static INotify? s_currentINotify;
            private readonly List<Watcher> _watchers = new();
            private readonly byte[] _buffer = new byte[BufferSize];
            private readonly SafeFileHandle _inotifyHandle;
            private readonly ConcurrentDictionary<int, Watch> _wdToWatch = new ConcurrentDictionary<int, Watch>();
            private readonly ReaderWriterLockSlim _addLock = new(LockRecursionPolicy.NoRecursion);
            private bool _isThreadStopping;
            private bool _allWatchersStopped;
            private int _bufferAvailable;
            private int _bufferPos;
            private WatchedDirectory[] _dirBuffer = new WatchedDirectory[4];

            public INotify()
            {
                _inotifyHandle = CreateINotifyHandle();

                static SafeFileHandle CreateINotifyHandle()
                {
                    SafeFileHandle handle = Interop.Sys.INotifyInit();

                    if (handle.IsInvalid)
                    {
                        Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                        handle.Dispose();
                        switch (error.Error)
                        {
                            case Interop.Error.EMFILE:
                                string? maxValue = ReadMaxUserLimit(MaxUserInstancesPath);
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

                    return handle;
                }
            }

            private bool IsStopped => _isThreadStopping || _allWatchersStopped;

            private void AddWatcher(Watcher watcher)
            {
                Debug.Assert(Monitor.IsEntered(s_watchersLock));
                _watchers.Add(watcher);
            }

            private void StartThread()
            {
                Debug.Assert(Monitor.IsEntered(s_watchersLock));

                try
                {
                    // Spawn a thread to read from the inotify queue and process the events.
                    Thread thread = new Thread(obj => ((INotify)obj!).ProcessEvents())
                    {
                        IsBackground = true,
                        Name = ".NET File Watcher"
                    };
                    thread.Start(this);
                }
                catch
                {
                    StopINotify();

                    throw;
                }
            }

            private void StopINotify()
            {
                // This method gets called only on the ProcessEvents thread, or when that thread fails to start.
                // It closes the inotify handle.
                Debug.Assert(!_isThreadStopping);

                // Sync with AddOrUpdateWatchedDirectory and RemoveUnusedINotifyWatches to stop using the inotify handle.
                _addLock.EnterWriteLock();
                _isThreadStopping = true;
                _addLock.ExitWriteLock();

                // Close the handle.
                _inotifyHandle.Dispose();
            }

            private WatchedDirectory? AddOrUpdateWatchedDirectory(Watcher watcher, WatchedDirectory? parent, string directoryPath, Interop.Sys.NotifyEvents watchFilters, bool ignoreMissing = true)
            {
                Debug.Assert(!Monitor.IsEntered(watcher)); // We mustn't hold the watcher lock prior to taking the _addLock.

                WatchedDirectory? inotifyWatchesToRemove = null;
                WatchedDirectory dir;

                // This locks prevents removing watches while watches are being added.
                // It is also used to synchronize with Stop.
                _addLock.EnterReadLock();
                try
                {
                    // Serialize adding watches to the same watcher.
                    // Concurrently adding watches may happen during the initial recursive iteration of the directory.
                    // This ensures the WatchedDirectory matches with the most recent INotifyAddWatch directory.
                    lock (watcher)
                    {
                        if (_isThreadStopping // inotify thread stopping
                            || watcher.IsWatcherStopped // user stopped raising events
                            || (parent is not null && watcher.RootDirectory is null)) // process events removed the root
                        {
                            return null;
                        }

                        Interop.Sys.NotifyEvents mask = watchFilters |
                            Interop.Sys.NotifyEvents.IN_ONLYDIR |     // we only allow watches on directories
                            Interop.Sys.NotifyEvents.IN_EXCL_UNLINK | // we want to stop monitoring unlinked files
                            (parent == null ? 0 : Interop.Sys.NotifyEvents.IN_DONT_FOLLOW); // Follow links only for the root path, not the subdirs.

                        // To support multiple FileSystemWatchers on the same inotify instance, we need to use IN_MASK_ADD
                        // so we don't remove events another watcher is interested in.
                        // The downside is that we won't unsubscribe from events that are unique to a watcher when it stops.
                        mask |= Interop.Sys.NotifyEvents.IN_MASK_ADD;

                        // Track when directories are added/removed.
                        if (watcher.IncludeSubdirectories)
                        {
                            mask |= Interop.Sys.NotifyEvents.IN_MOVED_TO | Interop.Sys.NotifyEvents.IN_MOVED_FROM;
                        }

                        int wd = Interop.Sys.INotifyAddWatch(_inotifyHandle, directoryPath, (uint)mask);
                        if (wd == -1)
                        {
                            // If we get an error when trying to add the watch, don't let that tear down processing.
                            // Instead, raise the Error event with the exception and let the user decide how to handle it.
                            Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();

                            // Don't report an error when we can't add a watch because the child directory was removed or replaced by a file.
                            if (ignoreMissing && (error.Error == Interop.Error.ENOENT || error.Error == Interop.Error.ENOTDIR))
                            {
                                return null;
                            }

                            Exception exc;
                            if (error.Error == Interop.Error.ENOSPC)
                            {
                                string? maxValue = ReadMaxUserLimit(MaxUserWatchesPath);
                                string message = !string.IsNullOrEmpty(maxValue) ?
                                    SR.Format(SR.IOException_INotifyWatchesUserLimitExceeded_Value, maxValue) :
                                    SR.IOException_INotifyWatchesUserLimitExceeded;
                                exc = new IOException(message, error.RawErrno);
                            }
                            else
                            {
                                exc = Interop.GetExceptionForIoErrno(error, directoryPath);
                            }

                            watcher.QueueError(exc);

                            return null;
                        }

                        Watch watch = _wdToWatch.AddOrUpdate(wd, (int watchDescriptor) => new Watch(watchDescriptor), (int watchDescriptor, Watch current) => current);

                        if (parent is null)
                        {
                            Debug.Assert(watcher.RootDirectory is null);
                            dir = new WatchedDirectory(watch, watcher, "", parent);
                            watcher.RootDirectory = dir;
                        }
                        else
                        {
                            // Check if the parent already has a watch for this child name.
                            string name = System.IO.Path.GetFileName(directoryPath);
                            int idx = parent.FindChild(name);
                            if (idx != -1)
                            {
                                dir = parent.Children![idx];
                                if (dir.Watch == watch)
                                {
                                    // The inotify watch is the same.
                                    return dir;
                                }

                                // The current watch is watching a different directory which was moved/deleted, use the new watch instead.
                                bool removeINotifyWatches = false;

                                RemoveWatchedDirectoryFromParentAndWatches(dir, ref removeINotifyWatches);

                                if (removeINotifyWatches)
                                {
                                    inotifyWatchesToRemove = dir;
                                }
                            }
                            dir = new WatchedDirectory(watch, watcher, name, parent);
                            parent.InitializedChildren.Add(dir);
                        }

                        lock (watch)
                        {
                            watch.Watchers.Add(dir);
                        }
                    }
                }
                finally
                {
                    _addLock.ExitReadLock();
                }

                if (inotifyWatchesToRemove is not null)
                {
                    RemoveUnusedINotifyWatches(inotifyWatchesToRemove);
                }

                return dir;
            }

            private void RemoveWatchedDirectory(WatchedDirectory dir, int ignoredFd = -1)
            {
                bool removeINotifyWatches = false;

                RemoveWatchedDirectoryFromParentAndWatches(dir, ref removeINotifyWatches);

                if (removeINotifyWatches)
                {
                    RemoveUnusedINotifyWatches(dir, ignoredFd);
                }
            }

            private void RemoveUnusedINotifyWatches(WatchedDirectory removedDir, int ignoredFd = -1)
            {
                Debug.Assert(!Monitor.IsEntered(removedDir.Watcher)); // We mustn't hold the watcher lock prior to taking the _addLock.

                // _addLock stops handles from being added while we're removing watches.
                // This is needed to prevent removing watch descriptors between INotifyAddWatch and adding them to the Watch.Watchers.
                // _addLock is also used to synchronize with Stop.
                _addLock.EnterWriteLock();
                try
                {
                    if (_isThreadStopping)
                    {
                        return;
                    }

                    RemoveINotifyWatchWhenNoMoreWatchers(removedDir.Watch, ignoredFd);

                    // We don't need to remove the children when all watchers have stopped and the inotify will be closed.
                    if (_allWatchersStopped)
                    {
                        return;
                    }

                    lock (removedDir.Watcher)
                    {
                        if (removedDir.Children is { } children)
                        {
                            foreach (WatchedDirectory child in children)
                            {
                                RemoveINotifyWatchWhenNoMoreWatchers(child.Watch, ignoredFd);
                            }
                        }
                    }
                }
                finally
                {
                    _addLock.ExitWriteLock();
                }

                void RemoveINotifyWatchWhenNoMoreWatchers(Watch watch, int ignoredFd)
                {
                    lock (watch)
                    {
                        if (watch.Watchers.Count == 0)
                        {
                            if (_wdToWatch.TryRemove(watch.WatchDescriptor, out _))
                            {
                                if (watch.WatchDescriptor != ignoredFd)
                                {
                                    Interop.Sys.INotifyRemoveWatch(_inotifyHandle, watch.WatchDescriptor);
                                }
                            }
                        }
                    }
                }
            }

            private void RemoveWatchedDirectoryFromParentAndWatches(WatchedDirectory dir, ref bool removeINotifyWatches)
            {
                if (dir.IsRootDir)
                {
                    lock (s_watchersLock)
                    {
                        _watchers.Remove(dir.Watcher);

                        // Set _allWatchersStopped before we update the Watch and _wdToWatch.
                        _allWatchersStopped = _watchers.Count == 0;
                    }
                }

                Watcher watcher = dir.Watcher;
                lock (watcher)
                {
                    if (dir.IsRootDir)
                    {
                        if (watcher.RootDirectory == null)
                        {
                            return; // Already removed.
                        }
                        watcher.RootDirectory = null;
                    }
                    else
                    {
                        Debug.Assert(dir.Parent is not null); // !IsRootDirectory
                        int idx = dir.Parent.FindChild(dir.Name);
                        Debug.Assert(idx != -1);
                        if (idx == -1)
                        {
                            return; // Already removed.
                        }
                        dir.Parent.Children!.RemoveAt(idx);
                    }

                    RemoveFromWatch(dir, ref removeINotifyWatches);

                    if (dir.Children is { } children)
                    {
                        foreach (var child in children)
                        {
                            RemoveFromWatch(child, ref removeINotifyWatches);
                        }
                    }
                }

                static void RemoveFromWatch(WatchedDirectory dir, ref bool removeINotifyWatches)
                {
                    Watch watch = dir.Watch;
                    lock (watch)
                    {
                        watch.Watchers.Remove(dir);
                        removeINotifyWatches |= watch.Watchers.Count == 0;
                    }
                }
            }

            private void ProcessEvents()
            {
                try
                {
                    lock (s_watchersLock)
                    {
                        // We've started an INotify, but no root watch was created for the FileSystemWatcher that started it.
                        if (_watchers.Count == 0)
                        {
                            StopINotify();
                            return;
                        }
                    }

                    // Carry over information from MOVED_FROM to MOVED_TO events.
                    int movedFromWatchCount = 0;
                    string movedFromName = "";
                    uint movedFromCookie = 0;
                    bool movedFromIsDir = false;

                    while (TryReadEvent(out NotifyEvent nextEvent))
                    {
                        if (!ProcessEvent(nextEvent, ref movedFromWatchCount, ref movedFromName, ref movedFromCookie, ref movedFromIsDir))
                            break;
                    }
                }
                catch (Exception ex)
                {
                    lock (s_watchersLock)
                    {
                        StopINotify();

                        foreach (var watcher in _watchers)
                        {
                            watcher.QueueError(ex);
                        }
                    }
                }
                finally
                {
                    Debug.Assert(_inotifyHandle.IsClosed);
                }
            }

            private bool ProcessEvent(NotifyEvent nextEvent, ref int movedFromWatchCount, ref string movedFromName, ref uint movedFromCookie, ref bool movedFromIsDir)
            {
                // Subset of EventMask that are emitted conditionally based on NotifyFilters.DirectoryName/FileName.
                const Interop.Sys.NotifyEvents FileDirEvents =
                    Interop.Sys.NotifyEvents.IN_CREATE |
                    Interop.Sys.NotifyEvents.IN_DELETE |
                    Interop.Sys.NotifyEvents.IN_MOVED_FROM |
                    Interop.Sys.NotifyEvents.IN_MOVED_TO;
                // NotifyEvents that generate FileSystemWatcher events.
                const Interop.Sys.NotifyEvents EventMask =
                    FileDirEvents |
                    Interop.Sys.NotifyEvents.IN_ACCESS |
                    Interop.Sys.NotifyEvents.IN_MODIFY |
                    Interop.Sys.NotifyEvents.IN_ATTRIB;

                Span<char> pathBuffer = stackalloc char[PATH_MAX];
                Interop.Sys.NotifyEvents mask = (Interop.Sys.NotifyEvents)nextEvent.mask;

                // An overflow event means we missed events.
                if ((mask & Interop.Sys.NotifyEvents.IN_Q_OVERFLOW) != 0)
                {
                    lock (s_watchersLock)
                    {
                        StopINotify();

                        foreach (var watcher in _watchers)
                        {
                            watcher.QueueError(CreateBufferOverflowException(watcher.BasePath));
                        }
                    }
                    return false;
                }

                // Renames come in the form of two events: IN_MOVED_FROM and IN_MOVED_TO.
                // These should come as a sequence, one immediately after the other.
                // This holds the directories from the previous event in case it was IN_MOVED_FROM.
                ReadOnlySpan<WatchedDirectory> movedFromDirs = _dirBuffer.AsSpan(0, movedFromWatchCount);

                // Look up the Watch in _wdToWatch.
                // Synchronize with AddOrUpdateWatchedDirectory to make sure newly added watch descriptors can be found in _wdToWatch.
                _addLock.EnterWriteLock();
                _addLock.ExitWriteLock();
                _wdToWatch.TryGetValue(nextEvent.wd, out Watch? watch);

                // Watches for this event.
                ReadOnlySpan<WatchedDirectory> dirs = watch is not null ? GetWatchedDirectories(watch, ref _dirBuffer, offset: movedFromDirs.Length) : default;

                // If the event after IN_MOVED_FROM is not a matching IN_MOVED_TO, we treat the IN_MOVED_FROM as a 'Deleted' in the next block.
                // A matching IN_MOVED_TO will be handled as a 'Renamed' later on.
                if (!movedFromDirs.IsEmpty)
                {
                    bool isMatchingMovedTo = (mask & Interop.Sys.NotifyEvents.IN_MOVED_TO) != 0 && movedFromCookie == nextEvent.cookie;

                    foreach (var movedFrom in movedFromDirs)
                    {
                        bool isRename = isMatchingMovedTo && FindMatchingWatchedDirectory(dirs, movedFrom.Watcher) is not null;
                        if (isRename)
                        {
                            continue; // Handled as a Rename.
                        }

                        if (movedFromIsDir)
                        {
                            RemoveWatchedDirectoryChild(movedFrom, movedFromName);
                        }

                        var watcher = movedFrom.Watcher;
                        if (!IsIgnoredEvent(watcher, Interop.Sys.NotifyEvents.IN_DELETE, movedFromIsDir))
                        {
                            watcher.QueueEvent(WatcherEvent.Deleted(movedFrom, movedFromName));
                        }
                    }

                    if (!isMatchingMovedTo)
                    {
                        movedFromDirs = default;
                    }
                }

                // Determine whether the affected object is a directory (rather than a file).
                // If it is, we may need to do special processing, such as adding a watch for new
                // directories if IncludeSubdirectories is enabled.  Since we're only watching
                // directories, any IN_IGNORED event is also for a directory.
                bool isDir = (mask & (Interop.Sys.NotifyEvents.IN_ISDIR | Interop.Sys.NotifyEvents.IN_IGNORED)) != 0;

                // For IN_MOVED_FROM we check if there is an event pending that may be a matching IN_MOVED_TO.
                // If there is, we defer the handling to the next ProcessEvent.
                // If there isn't, we'll handle it as a 'Deleted' later on.
                if ((mask & Interop.Sys.NotifyEvents.IN_MOVED_FROM) != 0)
                {
                    bool eventAvailable = _bufferPos != _bufferAvailable;
                    if (!eventAvailable)
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
                        Interop.PollEvents events;
                        Interop.Sys.Poll(_inotifyHandle, Interop.PollEvents.POLLIN, MillisecondsTimeout, out events);

                        eventAvailable = events != Interop.PollEvents.POLLNONE;
                    }
                    if (eventAvailable)
                    {
                        movedFromName = nextEvent.name;
                        dirs.CopyTo(_dirBuffer); // dirs won't be at the start of _dirBuffer when movedFromWatchCount was not zero.
                        movedFromWatchCount = dirs.Length;
                        movedFromCookie = nextEvent.cookie;
                        movedFromIsDir = isDir;
                        return true;
                    }
                }
                movedFromWatchCount = 0;

                foreach (WatchedDirectory dir in dirs)
                {
                    Watcher watcher = dir.Watcher;
                    WatchedDirectory? matchingFromFound = null; // cache FindMatchingFrom result.

                    if (isDir && watcher.IncludeSubdirectories)
                    {
                        if ((mask & (Interop.Sys.NotifyEvents.IN_CREATE | Interop.Sys.NotifyEvents.IN_MOVED_TO)) != 0)
                        {
                            // If this is a rename, move over the watches from the source.
                            // We'll still call WatchChildDirectories in case the source was still being iterated for adding watches.
                            if (FindMatchingFrom(movedFromDirs) is WatchedDirectory matchingFrom)
                            {
                                RenameWatchedDirectories(dir, nextEvent.name, matchingFrom, movedFromName);
                            }

                            string directoryPath = dir.GetPath(nextEvent.name, pathBuffer, fullPath: true).ToString();
                            watcher.WatchChildDirectories(parent: dir, directoryPath);
                        }
                        else if ((mask & Interop.Sys.NotifyEvents.IN_MOVED_FROM) != 0)
                        {
                            RemoveWatchedDirectoryChild(dir, nextEvent.name);
                        }
                    }
                    // IN_IGNORED: Watch was removed explicitly or automatically because the directory was deleted.
                    if ((mask & Interop.Sys.NotifyEvents.IN_IGNORED) != 0)
                    {
                        RemoveWatchedDirectory(dir, ignoredFd: nextEvent.wd);
                        continue;
                    }

                    // To match Windows, don't emit events for the root directory.
                    if (dir.IsRootDir && nextEvent.name.Length == 0)
                    {
                        continue;
                    }

                    if (IsIgnoredEvent(watcher, mask, isDir))
                    {
                        continue;
                    }

                    switch (mask & EventMask)
                    {
                        case Interop.Sys.NotifyEvents.IN_CREATE:
                            watcher.QueueEvent(WatcherEvent.Created(dir, nextEvent.name));
                            break;
                        case Interop.Sys.NotifyEvents.IN_DELETE:
                            watcher.QueueEvent(WatcherEvent.Deleted(dir, nextEvent.name));
                            break;
                        case Interop.Sys.NotifyEvents.IN_ACCESS:
                        case Interop.Sys.NotifyEvents.IN_MODIFY:
                        case Interop.Sys.NotifyEvents.IN_ATTRIB:
                            watcher.QueueEvent(WatcherEvent.Changed(dir, nextEvent.name));
                            break;
                        case Interop.Sys.NotifyEvents.IN_MOVED_FROM:
                            watcher.QueueEvent(WatcherEvent.Deleted(dir, nextEvent.name));
                            break;
                        case Interop.Sys.NotifyEvents.IN_MOVED_TO:
                            if (FindMatchingFrom(movedFromDirs) is WatchedDirectory matchingFrom)
                            {
                                watcher.QueueEvent(WatcherEvent.Renamed(dir, nextEvent.name, matchingFrom, movedFromName));
                            }
                            else
                            {
                                watcher.QueueEvent(WatcherEvent.Created(dir, nextEvent.name));
                            }
                            break;
                    }

                    WatchedDirectory? FindMatchingFrom(ReadOnlySpan<WatchedDirectory> dirs)
                        => matchingFromFound ??= (mask & Interop.Sys.NotifyEvents.IN_MOVED_TO) != 0 ? FindMatchingWatchedDirectory(dirs, watcher) : null;
                }

                // For each Watcher we'll receive an IN_IGNORED for its root watch.
                // If the root watch was found back as a WatchedDirectory via _wdToWatch above, then _allWatchersStopped will be updated by calling RemoveWatchedDirectory.
                // If we didn't find back the WatchedDirectory, then RemoveWatchedDirectory was called already and it has updated _allWatchersStopped.
                if (_allWatchersStopped)
                {
                    StopINotify();
                    return false;
                }

                return true;

                void RemoveWatchedDirectoryChild(WatchedDirectory dir, string movedFromName)
                {
                    Watcher watcher = dir.Watcher;
                    WatchedDirectory? child = null;
                    lock (watcher)
                    {
                        int idx = dir.FindChild(movedFromName);
                        if (idx != -1)
                        {
                            child = dir.Children![idx];
                        }
                    }
                    if (child is not null)
                    {
                        RemoveWatchedDirectory(child);
                    }
                }

                static ReadOnlySpan<WatchedDirectory> GetWatchedDirectories(Watch watch, ref WatchedDirectory[] buffer, int offset)
                {
                    lock (watch)
                    {
                        int watchersCount = watch.Watchers.Count;
                        int lengthNeeded = watchersCount + offset;
                        if (lengthNeeded > buffer.Length)
                        {
                            Array.Resize(ref buffer, lengthNeeded);
                        }
                        watch.Watchers.CopyTo(buffer.AsSpan(offset));
                        return buffer.AsSpan(offset, watchersCount);
                    }
                }

                static WatchedDirectory? FindMatchingWatchedDirectory(ReadOnlySpan<WatchedDirectory> dir, Watcher watcher)
                {
                    foreach (var d in dir)
                    {
                        if (d.Watcher == watcher)
                        {
                            return d;
                        }
                    }

                    return null;
                }

                static bool IsIgnoredEvent(Watcher watcher, Interop.Sys.NotifyEvents mask, bool isDir)
                {
                    return (watcher.WatchFilters & mask) == 0 ||
                            ((mask & FileDirEvents) != 0) &&
                                ((isDir && ((watcher.NotifyFilters & NotifyFilters.DirectoryName) == 0)) ||
                                 (!isDir && ((watcher.NotifyFilters & NotifyFilters.FileName) == 0)));
                }
            }

            private void RenameWatchedDirectories(WatchedDirectory moveTo, string moveToName, WatchedDirectory moveFrom, string moveFromName)
            {
                WatchedDirectory? sourceToRemove = null;

                Watcher watcher = moveFrom.Watcher;
                Debug.Assert(moveTo.Watcher == watcher);
                lock (watcher)
                {
                    int sourceIdx = moveFrom.FindChild(moveFromName);
                    if (sourceIdx == -1)
                    {
                        // unexpected: source not found.
                        return;
                    }
                    WatchedDirectory source = moveFrom.Children![sourceIdx];

                    int dstIdx = moveTo.FindChild(moveToName);
                    if (dstIdx != -1)
                    {
                        // unexpected: the destination already exists. Leave it and stop watching the source.
                        sourceToRemove = source;
                    }
                    else
                    {
                        // We'll re-use the Watches.
                        moveFrom.Children.RemoveAt(sourceIdx);
                        WatchedDirectory renamed = CreateWatchedDirectoryFrom(moveTo, source, moveToName);
                        moveTo.InitializedChildren.Add(renamed);
                    }
                }

                if (sourceToRemove is not null)
                {
                    RemoveWatchedDirectory(sourceToRemove);
                }

                static WatchedDirectory CreateWatchedDirectoryFrom(WatchedDirectory parent, WatchedDirectory src, string name)
                {
                    Watcher watcher = src.Watcher;
                    Debug.Assert(Monitor.IsEntered(watcher));

                    WatchedDirectory newDir;
                    Watch watch = src.Watch;
                    lock (watch)
                    {
                        newDir = new WatchedDirectory(watch, watcher, name, parent);
                        watch.Watchers.Remove(src);
                        watch.Watchers.Add(newDir);
                    }

                    if (src.Children is { } children)
                    {
                        foreach (var child in children)
                        {
                            newDir.InitializedChildren.Add(CreateWatchedDirectoryFrom(newDir, child, child.Name));
                        }
                    }

                    return newDir;
                }
            }

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
                                _bufferAvailable = Interop.CheckIo(Interop.Sys.Read(_inotifyHandle, buf, this._buffer.Length));
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
                    Debug.Assert(_bufferAvailable >= INotifyEventSize);
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
                Debug.Assert(_bufferPos + INotifyEventSize <= _bufferAvailable);
                NotifyEvent readEvent;
                readEvent.wd = BitConverter.ToInt32(_buffer, _bufferPos);
                readEvent.mask = BitConverter.ToUInt32(_buffer, _bufferPos + 4);       // +4  to get past wd
                readEvent.cookie = BitConverter.ToUInt32(_buffer, _bufferPos + 8);     // +8  to get past wd, mask
                int nameLength = (int)BitConverter.ToUInt32(_buffer, _bufferPos + 12); // +12 to get past wd, mask, cookie
                readEvent.name = ReadName(_bufferPos + INotifyEventSize, nameLength);  // +16 to get past wd, mask, cookie, len
                _bufferPos += INotifyEventSize + nameLength;

                notifyEvent = readEvent;
                return true;
            }

            /// <summary>
            /// Reads a UTF-8 string from _buffer starting at the specified position and up to
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

                int lengthWithoutNullTerm = _buffer.AsSpan(position, nameLength).IndexOf((byte)'\0');
                if (lengthWithoutNullTerm < 0)
                {
                    lengthWithoutNullTerm = nameLength;
                }

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

            internal struct WatcherEvent
            {
                public const WatcherChangeTypes ErrorType = WatcherChangeTypes.All;

                public string? Name { get; }
                public WatchedDirectory? Directory { get; }
                public string? OldName { get; }
                public WatchedDirectory? OldDirectory { get; }
                public Exception? Exception { get; }
                public WatcherChangeTypes Type { get; }

                private WatcherEvent(WatcherChangeTypes type, WatchedDirectory watch, string name, WatchedDirectory? oldWatch = null, string? oldName = null)
                {
                    Type = type;
                    Directory = watch;
                    Name = name;
                    OldDirectory = oldWatch;
                    OldName = oldName;
                }

                private WatcherEvent(Exception exception)
                {
                    Type = ErrorType;
                    Exception = exception;
                }

                public static WatcherEvent Deleted(WatchedDirectory dir, string name)
                    => new WatcherEvent(WatcherChangeTypes.Deleted, dir, name);

                public static WatcherEvent Created(WatchedDirectory dir, string name)
                    => new WatcherEvent(WatcherChangeTypes.Created, dir, name);

                public static WatcherEvent Changed(WatchedDirectory dir, string name)
                    => new WatcherEvent(WatcherChangeTypes.Changed, dir, name);

                public static WatcherEvent Renamed(WatchedDirectory dir, string name, WatchedDirectory oldDir, string oldName)
                    => new WatcherEvent(WatcherChangeTypes.Renamed, dir, name, oldDir, oldName);

                public static WatcherEvent Error(Exception exception)
                    => new WatcherEvent(exception);

                public ReadOnlySpan<char> GetName(Span<char> pathBuffer)
                    => Directory!.GetPath(Name, pathBuffer);

                public ReadOnlySpan<char> GetOldName(Span<char> pathBuffer)
                    => OldDirectory!.GetPath(OldName, pathBuffer);
            }

            public sealed class Watcher
            {
                // Ignore links.
                private static readonly EnumerationOptions ChildEnumerationOptions =
                    new() { RecurseSubdirectories = false, MatchType = MatchType.Win32, AttributesToSkip = FileAttributes.ReparsePoint, IgnoreInaccessible = false };

                /// <summary>
                /// Weak reference to the associated watcher.  A weak reference is used so that the FileSystemWatcher may be collected and finalized,
                /// causing an active operation to be torn down.  With a strong reference, a blocking read on the inotify handle will keep alive this
                /// instance which will keep alive the FileSystemWatcher which will not be finalizable and thus which will never signal to the blocking
                /// read to wake up in the event that the user neglects to stop raising events.
                /// </summary>
                private readonly WeakReference<FileSystemWatcher> _weakFsw;
                private readonly Channel<WatcherEvent> _eventQueue;
                private INotify? _inotify;
                private bool _emitEvents;

                public string BasePath { get; }
                public NotifyFilters NotifyFilters { get; }
                public Interop.Sys.NotifyEvents WatchFilters { get; }
                public bool IncludeSubdirectories { get; }
                public bool IsWatcherStopped { get; set; }

                public WatchedDirectory? RootDirectory
                {
                    get
                    {
                        Debug.Assert(Monitor.IsEntered(this));
                        return field;
                    }
                    set
                    {
                        Debug.Assert(Monitor.IsEntered(this));
                        field = value;
                    }
                }

                public Watcher(FileSystemWatcher fsw)
                {
                    _weakFsw = new WeakReference<FileSystemWatcher>(fsw);
                    BasePath = System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(fsw.Path));
                    IncludeSubdirectories = fsw.IncludeSubdirectories;
                    NotifyFilters = fsw.NotifyFilter;
                    WatchFilters = TranslateFilters(NotifyFilters);

                    // This channel is unbounded which means that if the FileSystemWatcher event handlers can't keep up, the queue size will increase and consume memory.
                    // We could implement a bound that is based on the FileSystemWatcher.InternalBufferSize property.
                    // If InternalBufferSize were used, RestartForInternalBufferSize can be removed/should be updated.
                    _eventQueue = Channel.CreateUnbounded<WatcherEvent>(new UnboundedChannelOptions() { AllowSynchronousContinuations = false, SingleReader = true });
                }

                public void Start()
                {
                    Debug.Assert(_inotify is null);

                    INotify? inotify;
                    WatchedDirectory? rootDirectory;
                    lock (s_watchersLock)
                    {
                        inotify = s_currentINotify;
                        // If there is no running instance, start one.
                        if (inotify is null || inotify.IsStopped)
                        {
                            inotify = new();
                            inotify.StartThread();
                            s_currentINotify = inotify;
                        }

                        _inotify = inotify;

                        rootDirectory = CreateRootWatch();
                        if (rootDirectory is not null)
                        {
                            _inotify.AddWatcher(this);
                        }
                    }

                    _ = DequeueEvents();

                    if (rootDirectory is not null && IncludeSubdirectories)
                    {
                        WatchChildDirectories(rootDirectory, BasePath, includeBasePath: false);
                    }

                    _emitEvents = true;

                    WatchedDirectory? CreateRootWatch()
                        => _inotify.AddOrUpdateWatchedDirectory(this, parent: null, BasePath, WatchFilters, ignoreMissing: false);
                }

                public void Stop()
                {
                    WatchedDirectory? root;
                    lock (this)
                    {
                        if (IsWatcherStopped)
                        {
                            return;
                        }
                        IsWatcherStopped = true;
                        _emitEvents = false;

                        root = RootDirectory;
                    }

                    _eventQueue.Writer.Complete();

                    if (root is not null)
                    {
                        Debug.Assert(_inotify is not null);
                        _inotify.RemoveWatchedDirectory(root);
                    }
                }

                private async Task DequeueEvents()
                {
                    char[] pathBuffer = new char[PATH_MAX];
                    try
                    {
                        await foreach (WatcherEvent @event in _eventQueue.Reader.ReadAllAsync().ConfigureAwait(false))
                        {
                            if (IsWatcherStopped)
                            {
                                break;
                            }
                            EmitEvent(@event, pathBuffer);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!IsWatcherStopped)
                        {
                            Stop();

                            try
                            {
                                Fsw?.OnError(new ErrorEventArgs(ex));
                            }
                            catch
                            { }
                        }
                    }
                }

                private void EmitEvent(WatcherEvent @event, char[] pathBuffer)
                {
                    FileSystemWatcher? fsw = Fsw;
                    if (fsw is null)
                    {
                        return;
                    }

                    switch (@event.Type)
                    {
                        case WatcherEvent.ErrorType:
                            fsw.OnError(new ErrorEventArgs(@event.Exception!));

                            // On InternalBufferOverflowException, the inotify is stopped.
                            // If the Watcher wasn't stopped, Restart it against a new inotify instance.
                            if (@event.Exception is InternalBufferOverflowException)
                            {
                                if (!IsWatcherStopped)
                                {
                                    fsw.Restart();
                                }
                            }

                            break;
                        case WatcherChangeTypes.Created:
                        case WatcherChangeTypes.Deleted:
                        case WatcherChangeTypes.Changed:
                            {
                                ReadOnlySpan<char> name = @event.GetName(pathBuffer);
                                fsw.NotifyFileSystemEventArgs(@event.Type, name);
                            }
                            break;
                        case WatcherChangeTypes.Renamed:
                            {
                                string name = @event.GetName(pathBuffer).ToString();
                                ReadOnlySpan<char> oldName = @event.GetOldName(pathBuffer);
                                fsw.NotifyRenameEventArgs(WatcherChangeTypes.Renamed, name, oldName);
                            }
                            break;
                    }
                }

                internal bool WatchChildDirectories(WatchedDirectory parent, string path, bool includeBasePath = true)
                {
                    Debug.Assert(_inotify is not null);
                    if (IsWatcherStopped)
                    {
                        return false;
                    }

                    if (includeBasePath)
                    {
                        WatchedDirectory? newParent = AddOrUpdateWatch(parent, path);
                        if (newParent is null)
                        {
                            // We couldn't recurse this path, but we should continue to try the others.
                            return true;
                        }
                        parent = newParent;
                    }

                    try
                    {
                        foreach (string childDir in Directory.EnumerateDirectories(path, "*", ChildEnumerationOptions))
                        {
                            if (!WatchChildDirectories(parent, childDir))
                            {
                                return false;
                            }
                        }
                    }
                    catch (DirectoryNotFoundException)
                    { } // path was removed
                    catch (IOException ex) when (ex.HResult == Interop.Error.ENOTDIR.Info().RawErrno)
                    { }  // path was replaced by a file.
                    catch (Exception ex)
                    {
                        QueueError(ex);
                    }

                    return true;

                    WatchedDirectory? AddOrUpdateWatch(WatchedDirectory parent, string path)
                        => _inotify.AddOrUpdateWatchedDirectory(this, parent, path, WatchFilters, ignoreMissing: true);
                }

                internal void QueueEvent(WatcherEvent ev)
                {
                    Debug.Assert(ev.Type != WatcherEvent.ErrorType);
                    if (!_emitEvents)
                    {
                        return;
                    }
                    _eventQueue.Writer.TryWrite(ev);
                }

                internal void QueueError(Exception exception)
                {
                    if (IsWatcherStopped)
                    {
                        return;
                    }
                    _eventQueue.Writer.TryWrite(WatcherEvent.Error(exception));
                }

                private FileSystemWatcher? Fsw
                {
                    get
                    {
                        _weakFsw.TryGetTarget(out FileSystemWatcher? watcher);
                        return watcher;
                    }
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
            }

            internal sealed class WatchedDirectory
            {
                private List<WatchedDirectory>? _children;

                public Watch Watch { get; }
                public Watcher Watcher { get; }
                public string Name { get; }
                public WatchedDirectory? Parent { get; }
                public bool IsRootDir => Parent is null;

                public WatchedDirectory(Watch watch, Watcher watcher, string name, WatchedDirectory? parent)
                {
                    Watch = watch;
                    Watcher = watcher;
                    Name = name;
                    Parent = parent;
                }

                public List<WatchedDirectory>? Children
                {
                    get
                    {
                        Debug.Assert(Monitor.IsEntered(Watcher));
                        return _children;
                    }
                    set
                    {
                        Debug.Assert(Monitor.IsEntered(Watcher));
                        _children = value;
                    }
                }
                public List<WatchedDirectory> InitializedChildren => Children ??= new List<WatchedDirectory>();

                public int FindChild(string name)
                {
                    Debug.Assert(Monitor.IsEntered(Watcher));
                    var children = Children;
                    if (children is null)
                    {
                        return -1;
                    }
                    for (int i = 0; i < children.Count; i++)
                    {
                        if (children[i].Name == name)
                        {
                            return i;
                        }
                    }
                    return -1;
                }

                internal ReadOnlySpan<char> GetPath(ReadOnlySpan<char> childName, Span<char> pathBuffer, bool fullPath = false)
                {
                    int length = 0;

                    if (Parent is not null)
                    {
                        length = Parent.GetPath("", pathBuffer, fullPath).Length;
                        fullPath = false;
                    }

                    if (fullPath)
                    {
                        Append(pathBuffer, Watcher.BasePath);
                    }

                    Append(pathBuffer, Name);
                    Append(pathBuffer, childName);

                    return pathBuffer.Slice(0, length);

                    void Append(Span<char> pathBuffer, ReadOnlySpan<char> path)
                    {
                        if (path.Length == 0)
                        {
                            return;
                        }

                        if (length != 0 && pathBuffer[length - 1] != '/')
                        {
                            pathBuffer[length] = '/';
                            length++;
                        }

                        path.CopyTo(pathBuffer.Slice(length));
                        length += path.Length;
                    }
                }
            }

            internal sealed class Watch
            {
                private List<WatchedDirectory> _watchers = new();

                public int WatchDescriptor { get; }
                public List<WatchedDirectory> Watchers
                {
                    get
                    {
                        Debug.Assert(Monitor.IsEntered(this));
                        return _watchers;
                    }
                }

                public Watch(int watchDescriptor)
                {
                    WatchDescriptor = watchDescriptor;
                }
            }
        }
    }
}
