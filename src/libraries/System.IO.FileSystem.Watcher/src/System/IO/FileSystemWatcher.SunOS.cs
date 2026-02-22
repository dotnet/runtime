// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// SunOS FileSystemWatcher Implementation using portfs (event ports)
//
// Design Overview:
// - One portfs event port per directory being watched (not shared across subdirectories)
// - When watching only FileName/DirectoryName: port_associate on the directory itself
//   to detect when entries are added/removed (directory mtime changes)
// - When watching attributes/times: hybrid mode with port_associate on both:
//   * The directory itself (to detect adds/removes)
//   * Each individual file/subdirectory (to detect attribute changes)
// - For IncludeSubdirectories=true: create separate RunningInstance for each subdirectory
//
// Event Detection via PortEvent flags:
// - FileName/DirectoryName: FILE_MODIFIED on directory detects entry add/remove
// - LastWrite/Size: FILE_MODIFIED/FILE_TRUNC on individual files
// - Attributes/Security/CreationTime: FILE_ATTRIB on individual files
// - LastAccess: FILE_ACCESS on individual files
//
// Resource Limits:
// - Max 50 subdirectories per directory
// - Max 1000 files watched per directory (when watching attributes)
//
// Why not iNotify?  There is an iNotify implementation on some illumos distributions.
// This implementation does not use iNotify because that would restrict us to only
// those distributions that have it, and even for those that do, the developers have
// indicated that their iNotify implementation should be treated as experimental.
// By contrast, portfs is reliable and available on all distributions.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public partial class FileSystemWatcher
    {
        private void StartRaisingEvents()
        {
            if (IsSuspended())
            {
                _enabled = true;
                return;
            }

            if (_cancellation is not null)
            {
                return;
            }

            try
            {
                CancellationTokenSource cancellation = new CancellationTokenSource();

                var runner = new RunningInstance(
                    this, _directory, "",
                    IncludeSubdirectories, NotifyFilter, cancellation.Token);

                _cancellation = cancellation;
                _enabled = true;

                runner.Start();
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

            var cts = _cancellation;
            if (cts is not null)
            {
                _cancellation = null;
                try
                {
                    cts.Cancel();
                }
                finally
                {
                    cts.Dispose();
                }
            }
        }

        private void FinalizeDispose()
        {
            StopRaisingEvents();
        }

        private CancellationTokenSource? _cancellation;

        private sealed class RunningInstance
        {
            // Resource limits
            private const int MaxSubdirectoriesPerDirectory = 50;
            private const int MaxFilesWatchedPerDirectory = 1000;

            // Event mask for directory watches (always FILE_MODIFIED to detect entry add/remove)
            private const int DirectoryEventMask = Interop.PortFs.PortEvent.FILE_MODIFIED;

            // Event mask for cancelling a PortGet (just make it wake up)
            // The actual event flag chosen here does not matter, though to avoid confusion
            // this uses an event flag that we don't need to use for anything else.
            private const int CancellationEventMask = Interop.PortFs.PortEvent.FILE_NOFOLLOW;

            // Core state
            private readonly WeakReference<FileSystemWatcher> _weakWatcher;
            private readonly string _directoryPath;
            private readonly string _relativePath;  // Path relative to root watcher
            private readonly SafeFileHandle _portfsHandle;
            private readonly bool _includeSubdirectories;
            private readonly NotifyFilters _notifyFilters;
            private readonly int _portEventMask;  // For file entries, computed from NotifyFilters
            private readonly bool _watchIndividualFiles;
            private readonly CancellationToken _cancellationToken;

            // Snapshot state
            private DirectorySnapshot? _snapshot;
            private byte[] _directoryFileObjBuffer = null!;

            // File watch state (only used for hybrid mode)
            private readonly Dictionary<IntPtr, string> _cookieToNameMap = new Dictionary<IntPtr, string>();
            private readonly Dictionary<string, (IntPtr cookie, byte[] buffer)> _nameToWatchMap = new Dictionary<string, (IntPtr cookie, byte[] buffer)>();
            private int _nextCookie = 1;

            // Special cookie values
            private static readonly IntPtr DirectoryCookie = IntPtr.Zero;
            private static readonly IntPtr CancellationCookie = new IntPtr(-1);

            // Subdirectory management
            private readonly List<RunningInstance> _subdirectoryWatchers = new List<RunningInstance>();

            // Cancellation state
            private bool _isCancelling;

            internal RunningInstance(
                FileSystemWatcher watcher, string directoryPath, string relativePath,
                bool includeSubdirectories, NotifyFilters notifyFilters, CancellationToken cancellationToken)
            {
                Debug.Assert(watcher != null);
                Debug.Assert(directoryPath != null);

                _weakWatcher = new WeakReference<FileSystemWatcher>(watcher);
                _directoryPath = directoryPath;
                _relativePath = relativePath;
                _includeSubdirectories = includeSubdirectories;
                _notifyFilters = notifyFilters;
                _cancellationToken = cancellationToken;

                // Convert NotifyFilters to portfs event mask
                _portEventMask = GetPortEventMask(notifyFilters);

                // Determine if we need to watch individual files (hybrid mode)
                const NotifyFilters NameFilters = NotifyFilters.FileName | NotifyFilters.DirectoryName;
                _watchIndividualFiles = (notifyFilters & ~NameFilters) != 0;

                // Create port
                _portfsHandle = Interop.PortFs.PortCreate();
                if (_portfsHandle.IsInvalid)
                {
                    Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                    _portfsHandle.Dispose();
                    throw Interop.GetExceptionForIoErrno(error);
                }

                try
                {
                    // Initialize watch
                    InitializeWatch();
                }
                catch
                {
                    _portfsHandle.Dispose();
                    throw;
                }
            }

            private static int GetPortEventMask(NotifyFilters filters)
            {
                int mask = 0;

                // FileName/DirectoryName: detect when entries are added/removed
                if ((filters & (NotifyFilters.FileName | NotifyFilters.DirectoryName)) != 0)
                    mask |= Interop.PortFs.PortEvent.FILE_MODIFIED;

                // LastWrite/Size: detect content/size changes
                if ((filters & (NotifyFilters.LastWrite | NotifyFilters.Size)) != 0)
                {
                    mask |= Interop.PortFs.PortEvent.FILE_MODIFIED;
                    mask |= Interop.PortFs.PortEvent.FILE_TRUNC;
                }

                // Attributes/Security/CreationTime: detect attribute changes
                if ((filters & (NotifyFilters.Attributes | NotifyFilters.Security | NotifyFilters.CreationTime)) != 0)
                    mask |= Interop.PortFs.PortEvent.FILE_ATTRIB;

                // LastAccess: detect access time changes
                if ((filters & NotifyFilters.LastAccess) != 0)
                    mask |= Interop.PortFs.PortEvent.FILE_ACCESS;

                return mask;
            }

            private void InitializeWatch()
            {
                // Create initial snapshot
                _snapshot = DirectorySnapshot.Create(_directoryPath, _notifyFilters);

                // Associate directory for name changes
                AssociateDirectory();

                // If watching attributes, associate individual files (hybrid mode)
                if (_watchIndividualFiles)
                {
                    AssociateFiles(_snapshot);
                }

                // Create child watchers for subdirectories
                if (_includeSubdirectories)
                {
                    CreateSubdirectoryWatchers(_snapshot);
                }
            }

            private void AssociateDirectory()
            {
                _directoryFileObjBuffer = GC.AllocateArray<byte>(Interop.PortFs.FileObjSize, pinned: true);

                unsafe
                {
                    fixed (byte* ptr = _directoryFileObjBuffer)
                    {
                        IntPtr pFileObj = (IntPtr)ptr;
                        Interop.Sys.TimeSpec mtime = _snapshot!.DirectoryMTime;
                        int result = Interop.PortFs.PortAssociate(_portfsHandle, pFileObj, _directoryPath, &mtime, DirectoryEventMask, DirectoryCookie);

                        if (result == -1)
                        {
                            Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                            Exception exc = Interop.GetExceptionForIoErrno(error, _directoryPath);

                            if (_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                            {
                                watcher.OnError(new ErrorEventArgs(exc));
                            }
                            throw exc;
                        }
                    }
                }
            }

            private void AssociateFiles(DirectorySnapshot snapshot)
            {
                int filesAssociated = 0;

                foreach ((string name, FileEntry entry) in snapshot.SortedEntries)
                {
                    if (filesAssociated >= MaxFilesWatchedPerDirectory)
                    {
                        if (_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                        {
                            watcher.OnError(new ErrorEventArgs(
                                new IOException($"Max file watch limit ({MaxFilesWatchedPerDirectory}) exceeded for directory: {_directoryPath}")));
                        }
                        break;
                    }

                    string fullPath = System.IO.Path.Combine(_directoryPath, name);
                    IntPtr cookie = (IntPtr)_nextCookie++;
                    byte[] buffer = GC.AllocateArray<byte>(Interop.PortFs.FileObjSize, pinned: true);

                    unsafe
                    {
                        fixed (byte* ptr = buffer)
                        {
                            IntPtr pFileObj = (IntPtr)ptr;
                            Interop.Sys.TimeSpec mtime = entry.MTime;
                            int result = Interop.PortFs.PortAssociate(_portfsHandle, pFileObj, fullPath, &mtime, _portEventMask, cookie);

                            if (result == -1)
                            {
                                // File may have been deleted between snapshot and association
                                Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                                if (error.Error == Interop.Error.ENOENT || error.Error == Interop.Error.ENOTDIR)
                                    continue;

                                if (error.Error == Interop.Error.ENOSPC)
                                {
                                    if (_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                                    {
                                        watcher.OnError(new ErrorEventArgs(Interop.GetExceptionForIoErrno(error, fullPath)));
                                    }
                                    break;
                                }
                                continue;
                            }

                            _cookieToNameMap[cookie] = name;
                            _nameToWatchMap[name] = (cookie, buffer);
                            filesAssociated++;
                        }
                    }
                }
            }

            private void CreateSubdirectoryWatchers(DirectorySnapshot snapshot)
            {
                int subdirCount = 0;

                foreach ((string name, FileEntry entry) in snapshot.SortedEntries)
                {
                    if (!entry.IsDirectory)
                        continue;

                    if (subdirCount >= MaxSubdirectoriesPerDirectory)
                    {
                        if (_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                        {
                            watcher.OnError(new ErrorEventArgs(
                                new IOException($"Max subdirectory limit ({MaxSubdirectoriesPerDirectory}) exceeded for directory: {_directoryPath}")));
                        }
                        break;
                    }

                    string subdirPath = System.IO.Path.Combine(_directoryPath, name);
                    string subdirRelativePath = string.IsNullOrEmpty(_relativePath)
                        ? name
                        : System.IO.Path.Combine(_relativePath, name);

                    // Check if it's a symlink - skip symlinks
                    if (Interop.Sys.LStat(subdirPath, out Interop.Sys.FileStatus status) == 0 &&
                        ((status.Mode & (uint)Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFLNK))
                    {
                        continue;
                    }

                    try
                    {
                        if (_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                        {
                            var childWatcher = new RunningInstance(watcher, subdirPath, subdirRelativePath, true, _notifyFilters, _cancellationToken);
                            childWatcher.Start();
                            _subdirectoryWatchers.Add(childWatcher);
                            subdirCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                        {
                            watcher.OnError(new ErrorEventArgs(ex));
                        }
                    }
                }
            }

            internal void Start()
            {
                new Thread(ProcessEvents)
                {
                    IsBackground = true,
                    Name = ".NET FileSystemWatcher"
                }.Start();
            }

            private void ProcessEvents()
            {
                var ctr = _cancellationToken.UnsafeRegister(obj => ((RunningInstance)obj!).CancellationCallback(), this);
                try
                {
                    while (!_cancellationToken.IsCancellationRequested)
                    {
                        unsafe
                        {
                            IntPtr cookie = IntPtr.Zero;
                            int events = 0;
                            int result = Interop.PortFs.PortGet(_portfsHandle, &events, &cookie, null);

                            if (result == -1)
                            {
                                Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                                if (error.Error == Interop.Error.EINTR || error.Error == Interop.Error.ETIMEDOUT)
                                {
                                    continue;
                                }

                                // Report unexpected error before breaking
                                if (_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                                {
                                    watcher.OnError(new ErrorEventArgs(Interop.GetExceptionForIoErrno(error)));
                                }
                                break;
                            }

                            // Check if this is a cancellation event
                            if (cookie == CancellationCookie)
                            {
                                break;
                            }

                            // Handle event based on cookie
                            if (cookie == DirectoryCookie)
                            {
                                HandleDirectoryEvent();
                            }
                            else
                            {
                                HandleFileEvent(cookie, events);
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    if (_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                    {
                        watcher.OnError(new ErrorEventArgs(exc));
                    }
                }
                finally
                {
                    ctr.Dispose();
                    Cleanup();
                }
            }

            private void HandleDirectoryEvent()
            {
                if (!_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                {
                    return;
                }

                // Create new snapshot
                DirectorySnapshot newSnapshot;
                try
                {
                    newSnapshot = DirectorySnapshot.Create(_directoryPath, _notifyFilters);
                }
                catch (DirectoryNotFoundException)
                {
                    // Directory was deleted
                    return;
                }
                catch (Exception ex)
                {
                    watcher.OnError(new ErrorEventArgs(ex));
                    ReassociateDirectory();
                    return;
                }

                DirectorySnapshot oldSnapshot = _snapshot!;

                // Compare snapshots to find additions and deletions
                CompareSnapshotsAndNotify(watcher, oldSnapshot, newSnapshot);

                // Update snapshot
                _snapshot = newSnapshot;

                // Re-associate directory
                ReassociateDirectory();
            }

            private struct ChangeEvent
            {
                public WatcherChangeTypes Type;
                public string Name;
                public FileEntry Entry;
                public bool Processed;
                public int OtherIndex;  // Index of matching entry for renames, -1 if not a rename
            }

            private static void AddChange(List<ChangeEvent> changes, WatcherChangeTypes type, string name, FileEntry entry)
            {
                // Determine the opposite type for rename matching
                WatcherChangeTypes otherType;
                switch (type)
                {
                    case WatcherChangeTypes.Created:
                        otherType = WatcherChangeTypes.Deleted;
                        break;
                    case WatcherChangeTypes.Deleted:
                        otherType = WatcherChangeTypes.Created;
                        break;
                    default:
                        Debug.Assert(false, $"Unexpected WatcherChangeTypes: {type}");
                        otherType = WatcherChangeTypes.Renamed;  // Impossible value to ensure no matches
                        break;
                }

                int newIndex = changes.Count;
                var newChange = new ChangeEvent { Type = type, Name = name, Entry = entry, OtherIndex = -1 };

                // Search for matching inode of opposite type (potential rename)
                for (int i = 0; i < changes.Count; i++)
                {
                    if (changes[i].OtherIndex >= 0)
                        continue;  // Already paired

                    // Check if opposite type with matching inode
                    if (changes[i].Type == otherType && changes[i].Entry.Inode == entry.Inode)
                    {
                        // Found a match - link both entries
                        newChange.OtherIndex = i;
                        var existing = changes[i];
                        existing.OtherIndex = newIndex;
                        changes[i] = existing;
                        break;
                    }
                }

                changes.Add(newChange);
            }

            private void CompareSnapshotsAndNotify(FileSystemWatcher watcher, DirectorySnapshot oldSnapshot, DirectorySnapshot newSnapshot)
            {
                var oldEntries = oldSnapshot.SortedEntries;
                var newEntries = newSnapshot.SortedEntries;
                int oldIndex = 0;
                int newIndex = 0;

                // Collect all changes in order during single-pass comparison
                var changes = new List<ChangeEvent>();

                // Single-pass sorted merge comparison - collect entries in discovery order
                while (oldIndex < oldEntries.Count || newIndex < newEntries.Count)
                {
                    int comparison;
                    if (oldIndex >= oldEntries.Count)
                    {
                        comparison = 1; // Only new entries remain
                    }
                    else if (newIndex >= newEntries.Count)
                    {
                        comparison = -1; // Only old entries remain
                    }
                    else
                    {
                        comparison = StringComparer.Ordinal.Compare(oldEntries[oldIndex].Name, newEntries[newIndex].Name);
                    }

                    if (comparison < 0)
                    {
                        // Entry in old but not in new - deletion
                        var (name, entry) = oldEntries[oldIndex];
                        AddChange(changes, WatcherChangeTypes.Deleted, name, entry);
                        oldIndex++;
                    }
                    else if (comparison > 0)
                    {
                        // Entry in new but not in old - addition
                        var (name, entry) = newEntries[newIndex];
                        AddChange(changes, WatcherChangeTypes.Created, name, entry);
                        newIndex++;
                    }
                    else
                    {
                        // Entry in both - no change for name-based watching
                        oldIndex++;
                        newIndex++;
                    }
                }

                // Process changes in list order
                for (int i = 0; i < changes.Count; i++)
                {
                    if (changes[i].Processed)
                        continue;

                    var change = changes[i];

                    if (change.OtherIndex >= 0)
                    {
                        // This is part of a rename pair
                        int otherIdx = change.OtherIndex;
                        var other = changes[otherIdx];

                        if (change.Type == WatcherChangeTypes.Deleted)
                        {
                            // Deletion with matching other=creation. Other is "new".
                            ProcessRename(watcher, change.Name, change.Entry, other.Name);
                        }
                        else
                        {
                            // Creation with matching other=deletion. Other is "old".
                            ProcessRename(watcher, other.Name, other.Entry, change.Name);
                        }

                        // Mark both as processed
                        var updatedChange = changes[i];
                        updatedChange.Processed = true;
                        changes[i] = updatedChange;

                        var updatedOther = changes[otherIdx];
                        updatedOther.Processed = true;
                        changes[otherIdx] = updatedOther;
                    }
                    else if (change.Type == WatcherChangeTypes.Deleted)
                    {
                        // Unpaired deletion
                        ProcessDeletion(watcher, change.Name, change.Entry);
                        var updatedChange = changes[i];
                        updatedChange.Processed = true;
                        changes[i] = updatedChange;
                    }
                    else
                    {
                        // Unpaired creation
                        ProcessAddition(watcher, change.Name, change.Entry);
                        var updatedChange = changes[i];
                        updatedChange.Processed = true;
                        changes[i] = updatedChange;
                    }
                }
            }

            private string GetEventPath(string name)
            {
                return string.IsNullOrEmpty(_relativePath)
                    ? name
                    : System.IO.Path.Combine(_relativePath, name);
            }

            private void ProcessAddition(FileSystemWatcher watcher, string name, FileEntry newEntry)
            {
                bool isDir = newEntry.IsDirectory;

                if ((isDir && (_notifyFilters & NotifyFilters.DirectoryName) != 0) ||
                    (!isDir && (_notifyFilters & NotifyFilters.FileName) != 0))
                {
                    watcher.NotifyFileSystemEventArgs(WatcherChangeTypes.Created, GetEventPath(name));
                }

                // If hybrid mode, associate the new file
                if (_watchIndividualFiles && _nameToWatchMap.Count < MaxFilesWatchedPerDirectory)
                {
                    AssociateSingleFile(name, newEntry);
                }

                // If subdirectory, create watcher
                if (isDir && _includeSubdirectories && _subdirectoryWatchers.Count < MaxSubdirectoriesPerDirectory)
                {
                    CreateSingleSubdirectoryWatcher(name);
                }
            }

            private void ProcessDeletion(FileSystemWatcher watcher, string name, FileEntry oldEntry)
            {
                bool isDir = oldEntry.IsDirectory;

                if ((isDir && (_notifyFilters & NotifyFilters.DirectoryName) != 0) ||
                    (!isDir && (_notifyFilters & NotifyFilters.FileName) != 0))
                {
                    watcher.NotifyFileSystemEventArgs(WatcherChangeTypes.Deleted, GetEventPath(name));
                }

                // If hybrid mode, dissociate the file
                if (_watchIndividualFiles && _nameToWatchMap.TryGetValue(name, out var watch))
                {
                    DissociateFile(watch.buffer, watch.cookie, name);
                }

                // If subdirectory watcher exists, cancel it
                if (isDir && _includeSubdirectories)
                {
                    string subdirPath = System.IO.Path.Combine(_directoryPath, name);
                    var childToRemove = _subdirectoryWatchers.Find(c => c._directoryPath == subdirPath);
                    if (childToRemove is not null)
                    {
                        _subdirectoryWatchers.Remove(childToRemove);
                        childToRemove.Cancel();
                    }
                }
            }

            private void ProcessRename(FileSystemWatcher watcher, string oldName, FileEntry oldEntry, string newName)
            {
                bool isDir = oldEntry.IsDirectory;

                if ((isDir && (_notifyFilters & NotifyFilters.DirectoryName) != 0) ||
                    (!isDir && (_notifyFilters & NotifyFilters.FileName) != 0))
                {
                    watcher.NotifyRenameEventArgs(WatcherChangeTypes.Renamed, GetEventPath(newName), GetEventPath(oldName));
                }

                // Update file watch if in hybrid mode
                if (_watchIndividualFiles && _nameToWatchMap.TryGetValue(oldName, out var watch))
                {
                    _nameToWatchMap.Remove(oldName);
                    _nameToWatchMap[newName] = watch;
                    _cookieToNameMap[watch.cookie] = newName;
                }

                // Update subdirectory watcher if needed
                if (isDir && _includeSubdirectories)
                {
                    string oldSubdirPath = System.IO.Path.Combine(_directoryPath, oldName);
                    var childToUpdate = _subdirectoryWatchers.Find(c => c._directoryPath == oldSubdirPath);
                    if (childToUpdate is not null)
                    {
                        _subdirectoryWatchers.Remove(childToUpdate);
                        childToUpdate.Cancel();
                        CreateSingleSubdirectoryWatcher(newName);
                    }
                }
            }

            private void AssociateSingleFile(string name, FileEntry entry)
            {
                string fullPath = System.IO.Path.Combine(_directoryPath, name);
                IntPtr cookie = (IntPtr)_nextCookie++;
                byte[] buffer = GC.AllocateArray<byte>(Interop.PortFs.FileObjSize, pinned: true);

                unsafe
                {
                    fixed (byte* ptr = buffer)
                    {
                        IntPtr pFileObj = (IntPtr)ptr;
                        Interop.Sys.TimeSpec mtime = entry.MTime;
                        int result = Interop.PortFs.PortAssociate(_portfsHandle, pFileObj, fullPath, &mtime, _portEventMask, cookie);

                        if (result == 0)
                        {
                            _cookieToNameMap[cookie] = name;
                            _nameToWatchMap[name] = (cookie, buffer);
                        }
                    }
                }
            }

            private void CreateSingleSubdirectoryWatcher(string name)
            {
                string subdirPath = System.IO.Path.Combine(_directoryPath, name);
                string subdirRelativePath = string.IsNullOrEmpty(_relativePath)
                    ? name
                    : System.IO.Path.Combine(_relativePath, name);

                try
                {
                    if (_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                    {
                        var childWatcher = new RunningInstance(watcher, subdirPath, subdirRelativePath, true, _notifyFilters, _cancellationToken);
                        childWatcher.Start();
                        _subdirectoryWatchers.Add(childWatcher);
                    }
                }
                catch (Exception ex)
                {
                    if (_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                    {
                        watcher.OnError(new ErrorEventArgs(ex));
                    }
                }
            }

            private void DissociateFile(byte[] buffer, IntPtr cookie, string name)
            {
                unsafe
                {
                    fixed (byte* ptr = buffer)
                    {
                        IntPtr pFileObj = (IntPtr)ptr;
                        Interop.PortFs.PortDissociate(_portfsHandle, pFileObj);
                    }
                }
                _cookieToNameMap.Remove(cookie);
                _nameToWatchMap.Remove(name);
            }

            private void HandleFileEvent(IntPtr cookie, int events)
            {
                if (!_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                {
                    return;
                }

                // Look up filename from cookie
                if (!_cookieToNameMap.TryGetValue(cookie, out string? name))
                {
                    // File was deleted between event and handling
                    return;
                }

                // Check if file still exists on disk
                // When a file is moved/deleted, portfs fires FILE_MODIFIED, but we shouldn't
                // raise a Changed event - the Deleted event will come from snapshot comparison
                string fullPath = System.IO.Path.Combine(_directoryPath, name);
                Interop.Sys.FileStatus fileStatus;
                if (Interop.Sys.Stat(fullPath, out fileStatus) != 0)
                {
                    // File doesn't exist anymore - it was deleted or moved out
                    // Don't raise Changed event - Deleted will come from directory event
                    return;
                }

                // File still exists - generate change event
                // The event mask we used in PortAssociate already filtered to only
                // the attributes we care about based on NotifyFilters
                watcher.NotifyFileSystemEventArgs(WatcherChangeTypes.Changed, GetEventPath(name));

                // Re-associate the file with its current mtime
                if (_nameToWatchMap.TryGetValue(name, out var watch))
                {
                    Interop.Sys.TimeSpec mtime = new Interop.Sys.TimeSpec
                    {
                        TvSec = fileStatus.MTime,
                        TvNsec = fileStatus.MTimeNsec
                    };

                    unsafe
                    {
                        fixed (byte* ptr = watch.buffer)
                        {
                            IntPtr pFileObj = (IntPtr)ptr;
                            Interop.PortFs.PortAssociate(_portfsHandle, pFileObj, fullPath, &mtime, _portEventMask, cookie);
                            // If PortAssociate failed, the file was probably deleted,
                            // in which case we don't want more events for it anyway.
                        }
                    }
                }
            }

            private void ReassociateDirectory()
            {
                unsafe
                {
                    fixed (byte* ptr = _directoryFileObjBuffer)
                    {
                        IntPtr pFileObj = (IntPtr)ptr;
                        Interop.Sys.TimeSpec mtime = _snapshot!.DirectoryMTime;
                        Interop.PortFs.PortAssociate(_portfsHandle, pFileObj, _directoryPath, &mtime, DirectoryEventMask, DirectoryCookie);
                    }
                }
            }

            private void CancellationCallback()
            {
                // This is called from the cancellation token when the parent FileSystemWatcher stops.
                // Only send the PortSend event to wake up this instance's PortGet.
                // Don't cancel children here - that would cause re-entrance issues.
                if (_isCancelling)
                    return;

                _isCancelling = true;

                // Send synthetic event to wake up PortGet on this instance.
                // Not concerned with any error return here. All we care about is
                // causing the thread in PortGet to return.  If we got an error
                // dealing with the port, they probably will too.
                Interop.PortFs.PortSend(_portfsHandle, CancellationEventMask, CancellationCookie);
            }

            internal void Cancel()
            {
                // Depth-first cancellation: cancel all children before self
                if (_isCancelling)
                    return;

                _isCancelling = true;

                // First, cancel all subdirectory watchers (depth-first)
                foreach (var child in _subdirectoryWatchers)
                {
                    child.Cancel();
                }

                // Send synthetic event to wake up PortGet on this instance.
                // Not concerned with any error return here. All we care about is
                // causing the thread in PortGet to return.  If we got an error
                // dealing with the port, they probably will too.
                Interop.PortFs.PortSend(_portfsHandle, CancellationEventMask, CancellationCookie);
            }

            private void Cleanup()
            {
                // Dissociate directory
                if (_directoryFileObjBuffer is not null)
                {
                    unsafe
                    {
                        fixed (byte* ptr = _directoryFileObjBuffer)
                        {
                            IntPtr pFileObj = (IntPtr)ptr;
                            Interop.PortFs.PortDissociate(_portfsHandle, pFileObj);
                        }
                    }
                }

                // Dissociate all files
                foreach (var (name, watch) in _nameToWatchMap)
                {
                    unsafe
                    {
                        fixed (byte* ptr = watch.buffer)
                        {
                            IntPtr pFileObj = (IntPtr)ptr;
                            Interop.PortFs.PortDissociate(_portfsHandle, pFileObj);
                        }
                    }
                }

                _portfsHandle.Dispose();
            }
        }

        private sealed class DirectorySnapshot
        {
            internal List<(string Name, FileEntry Entry)> SortedEntries { get; }
            internal Interop.Sys.TimeSpec DirectoryMTime { get; }

            private DirectorySnapshot(List<(string, FileEntry)> sortedEntries, Interop.Sys.TimeSpec mtime)
            {
                SortedEntries = sortedEntries;
                DirectoryMTime = mtime;
            }

            internal static DirectorySnapshot Create(string path, NotifyFilters filters)
            {
                // Get directory mtime before reading
                if (Interop.Sys.Stat(path, out Interop.Sys.FileStatus status) != 0)
                {
                    throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo());
                }

                Interop.Sys.TimeSpec mtime = new Interop.Sys.TimeSpec
                {
                    TvSec = status.MTime,
                    TvNsec = status.MTimeNsec
                };

                // Read directory contents
                var entries = new List<(string Name, FileEntry Entry)>();

                foreach (string entry in Directory.EnumerateFileSystemEntries(path))
                {
                    string name = System.IO.Path.GetFileName(entry);
                    try
                    {
                        FileEntry fileEntry = FileEntry.Create(entry, filters);
                        entries.Add((name, fileEntry));
                    }
                    catch
                    {
                        // Ignore files that can't be stat'd
                    }
                }

                // Sort by name for comparison
                entries.Sort((a, b) => StringComparer.Ordinal.Compare(a.Name, b.Name));

                return new DirectorySnapshot(entries, mtime);
            }
        }

        private struct FileEntry
        {
            internal bool IsDirectory;
            internal Interop.Sys.TimeSpec MTime;
            internal long Inode;
            internal long Length;
            internal DateTime LastWriteTimeUtc;
            internal DateTime LastAccessTimeUtc;
            internal DateTime CreationTimeUtc;
            internal FileAttributes Attributes;

            internal static FileEntry Create(string path, NotifyFilters filters)
            {
                FileEntry entry = default;

                if (Interop.Sys.Stat(path, out Interop.Sys.FileStatus status) == 0)
                {
                    entry.IsDirectory = (status.Mode & (uint)Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR;
                    entry.MTime = new Interop.Sys.TimeSpec { TvSec = status.MTime, TvNsec = status.MTimeNsec };
                    entry.Inode = status.Ino;

                    if ((filters & NotifyFilters.Size) != 0)
                    {
                        entry.Length = status.Size;
                    }

                    if ((filters & NotifyFilters.LastWrite) != 0)
                    {
                        entry.LastWriteTimeUtc = DateTimeOffset.FromUnixTimeSeconds(status.MTime).UtcDateTime;
                    }

                    if ((filters & NotifyFilters.LastAccess) != 0)
                    {
                        entry.LastAccessTimeUtc = DateTimeOffset.FromUnixTimeSeconds(status.ATime).UtcDateTime;
                    }

                    if ((filters & NotifyFilters.CreationTime) != 0)
                    {
                        entry.CreationTimeUtc = DateTimeOffset.FromUnixTimeSeconds(status.CTime).UtcDateTime;
                    }

                    if ((filters & NotifyFilters.Attributes) != 0)
                    {
                        entry.Attributes = (FileAttributes)status.Mode;
                    }
                }

                return entry;
            }
        }

        private static void RestartForInternalBufferSize()
        {
            // The implementation is not using InternalBufferSize. There's no need to restart.
        }
    }
}
