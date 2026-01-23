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
// Event Flow in Hybrid Mode:
// - Directory events (FILE_MODIFIED on directory) → HandleDirectoryEvent
//   * Handles structural changes: file/directory added, deleted, renamed
//   * Does NOT re-associate existing files (not needed - see below)
// - Individual file events (FILE_ATTRIB, FILE_MODIFIED on file) → HandleFileEvent
//   * Handles attribute/content changes on existing files
//   * Automatically re-associates the file after raising Changed event
// - These event paths are independent; portfs fires both when appropriate
//
// Cancellation Design:
// - All subdirectory watchers share the same CancellationToken as their parent
// - Cancellation uses two mechanisms:
//   * Shared CancellationToken: When StopRaisingEvents cancels the token, all instances'
//     CancellationCallback fires and sends PortSend to wake their event loops concurrently
//   * Explicit Cancel(): When subdirectories are deleted/renamed at runtime, Cancel() is
//     called explicitly, which recursively cancels children depth-first before self
// - Each RunningInstance cleans up only its own resources (port handle, associations)
//   after its event loop exits
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
// Why not inotify?  There is an inotify implementation on some illumos distributions.
// This implementation does not use inotify because that would restrict us to only
// those distributions that have it, and even for those that do, the developers have
// indicated that their inotify implementation should be treated as experimental.
// By contrast, portfs is reliable and available on all distributions.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public partial class FileSystemWatcher
    {
        // Configuration for resource limits (loaded once at module initialization)
        private static readonly int s_maxSubdirectoriesPerDirectory = GetConfigurationInt32(
            "System.IO.FileSystem.Watcher.Illumos.MaxSubdirectoriesPerDirectory",
            "DOTNET_SYSTEM_IO_FSW_ILLUMOS_MAXSUBDIRS",
            50);

        private static readonly int s_maxFilesWatchedPerDirectory = GetConfigurationInt32(
            "System.IO.FileSystem.Watcher.Illumos.MaxFilesWatchedPerDirectory",
            "DOTNET_SYSTEM_IO_FSW_ILLUMOS_MAXFILES",
            1000);

        private static int GetConfigurationInt32(string appCtxSettingName, string envVarName, int defaultValue)
        {
            // First check AppContext
            switch (AppContext.GetData(appCtxSettingName))
            {
                case uint value:
                    return (int)value;
                case int value:
                    return value;
                case string str when int.TryParse(str, out int parsed):
                    return parsed;
            }

            // Fall back to environment variable
            string? envVar = Environment.GetEnvironmentVariable(envVarName);
            if (envVar != null && int.TryParse(envVar, out int envValue))
            {
                return envValue;
            }

            return defaultValue;
        }

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
                _cancellation?.Dispose();
                _cancellation = null;
                throw;
            }
        }

        private void StopRaisingEvents()
        {
            _enabled = false;

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
            // Event mask for directory watches (always FILE_MODIFIED to detect entry add/remove)
            private const int DirectoryEventMask = (int)Interop.PortFs.PortEvent.FILE_MODIFIED;

            // Event mask for cancelling a PortGet (just make it wake up)
            // The actual event flag chosen here does not matter, though to avoid confusion
            // this uses an event flag that we don't need to use for anything else.
            private const int CancellationEventMask = (int)Interop.PortFs.PortEvent.FILE_NOFOLLOW;

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
            private readonly Dictionary<nuint, string> _cookieToNameMap = new Dictionary<nuint, string>();
            private readonly Dictionary<string, (nuint cookie, byte[] buffer)> _nameToWatchMap = new Dictionary<string, (nuint cookie, byte[] buffer)>();
            private nuint _nextCookie = 2; // Start at 2 (0 and 1 are reserved -- see next)

            // Special cookie values
            private const nuint CancellationCookie = 0;
            private const nuint DirectoryCookie = 1;

            // Subdirectory management
            private readonly List<RunningInstance> _subdirectoryWatchers = new List<RunningInstance>();

            // Cancellation state
            private volatile bool _isCancelling;

            // Limit tracking - report error only once per directory when runtime limit is exceeded
            private bool _hasReportedFileLimit;
            private bool _hasReportedSubdirLimit;

            private nuint GetNextCookie()
            {
                nuint value = _nextCookie++;

                // SunOS is always 64-bit, so roll over is near impossible. Nonetheless:
                // Sanity check: cookie counter should never approach max value in normal usage
                // If this fires, there's a bug causing excessive cookie allocation
                Debug.Assert(value < nuint.MaxValue / 2,
                    $"Cookie counter unexpectedly high: {value}. Possible leak or overflow issue.");

                return value;
            }

            internal RunningInstance(
                FileSystemWatcher watcher, string directoryPath, string relativePath,
                bool includeSubdirectories, NotifyFilters notifyFilters, CancellationToken cancellationToken)
            {
                _weakWatcher = new WeakReference<FileSystemWatcher>(watcher);

                // Normalize path to handle relative paths and resolve symlinks,
                // aligning with Linux/OSX implementations
                _directoryPath = System.IO.Path.GetFullPath(directoryPath);
                _directoryPath = Interop.Sys.RealPath(_directoryPath) ?? _directoryPath;

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
                    mask |= (int)Interop.PortFs.PortEvent.FILE_MODIFIED;

                // LastWrite/Size: detect content/size changes
                if ((filters & (NotifyFilters.LastWrite | NotifyFilters.Size)) != 0)
                {
                    mask |= (int)Interop.PortFs.PortEvent.FILE_MODIFIED;
                    mask |= (int)Interop.PortFs.PortEvent.FILE_TRUNC;
                }

                // Attributes/Security/CreationTime: detect attribute changes
                if ((filters & (NotifyFilters.Attributes | NotifyFilters.Security | NotifyFilters.CreationTime)) != 0)
                    mask |= (int)Interop.PortFs.PortEvent.FILE_ATTRIB;

                // LastAccess: detect access time changes
                if ((filters & NotifyFilters.LastAccess) != 0)
                    mask |= (int)Interop.PortFs.PortEvent.FILE_ACCESS;

                return mask;
            }

            private void InitializeWatch()
            {
                // Create initial snapshot
                _snapshot = DirectorySnapshot.Create(_directoryPath);

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
                    if (filesAssociated >= s_maxFilesWatchedPerDirectory)
                    {
                        if (!_hasReportedFileLimit && _weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                        {
                            _hasReportedFileLimit = true;
                            watcher.OnError(new ErrorEventArgs(
                                new IOException(SR.Format(SR.FSW_MaxFilesWatchedExceeded, s_maxFilesWatchedPerDirectory, _directoryPath))));
                        }
                        break;
                    }

                    string fullPath = System.IO.Path.Combine(_directoryPath, name);
                    nuint cookie = GetNextCookie();
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
                                {
                                    // File/directory no longer exists, silently skip
                                    continue;
                                }

                                if (error.Error == Interop.Error.ENOSPC)
                                {
                                    // Out of resources, report error and stop associating more files
                                    if (_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                                    {
                                        watcher.OnError(new ErrorEventArgs(Interop.GetExceptionForIoErrno(error, fullPath)));
                                    }
                                    break;
                                }
                                // For other errors, skip this file (buffer will be collected)
                                continue;
                            }

                            // Only add to maps on success
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

                    if (subdirCount >= s_maxSubdirectoriesPerDirectory)
                    {
                        if (!_hasReportedSubdirLimit && _weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                        {
                            _hasReportedSubdirLimit = true;
                            watcher.OnError(new ErrorEventArgs(
                                new IOException(SR.Format(SR.FSW_MaxSubdirectoriesExceeded, s_maxSubdirectoriesPerDirectory, _directoryPath))));
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
                            // Share parent's cancellation token so entire tree can be cancelled together
                            var childWatcher = new RunningInstance(watcher, subdirPath, subdirRelativePath, true, _notifyFilters, _cancellationToken);
                            childWatcher.Start();
                            lock (_subdirectoryWatchers)
                            {
                                _subdirectoryWatchers.Add(childWatcher);
                            }
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
                    Name = ".NET File Watcher"
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
                            nuint cookie = 0;
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
                // This method handles structural directory changes (adds, deletes, renames).
                // In hybrid mode, it does NOT need to re-associate existing files because
                // individual file attribute changes trigger separate FILE_ATTRIB/FILE_MODIFIED
                // events that are handled by HandleFileEvent, which re-associates automatically.

                if (!_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                {
                    return;
                }

                // Create new snapshot
                DirectorySnapshot newSnapshot;
                try
                {
                    newSnapshot = DirectorySnapshot.Create(_directoryPath);
                }
                catch (DirectoryNotFoundException)
                {
                    // Directory was deleted; cancel this instance to prevent indefinite blocking in PortGet.
                    // The parent watcher (if any) will raise the Deleted event.
                    Cancel();
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
                        // Entry in both - check if inode or type changed,
                        // eg. new file or directory with same name.
                        var (oldName, oldEntry) = oldEntries[oldIndex];
                        var (newName, newEntry) = newEntries[newIndex];

                        if (oldEntry.Inode != newEntry.Inode || oldEntry.IsDirectory != newEntry.IsDirectory)
                        {
                            // File was atomically replaced (new inode) or type changed (file↔directory).
                            // Generate Deleted+Created to make the replacement observable.
                            AddChange(changes, WatcherChangeTypes.Deleted, oldName, oldEntry);
                            AddChange(changes, WatcherChangeTypes.Created, newName, newEntry);
                        }
                        // Else: same inode and type, no structural change (mtime changes handled by portfs events)

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
                if (_watchIndividualFiles)
                {
                    if (_nameToWatchMap.Count < s_maxFilesWatchedPerDirectory)
                    {
                        AssociateSingleFile(name, newEntry);
                    }
                    else if (!_hasReportedFileLimit)
                    {
                        _hasReportedFileLimit = true;
                        if (_weakWatcher.TryGetTarget(out FileSystemWatcher? watcherInstance))
                        {
                            watcherInstance.OnError(new ErrorEventArgs(
                                new IOException(SR.Format(SR.FSW_MaxFilesWatchedExceeded, s_maxFilesWatchedPerDirectory, _directoryPath))));
                        }
                    }
                }

                // If subdirectory, create watcher
                if (isDir && _includeSubdirectories)
                {
                    bool createSubdirWatcher = false;
                    bool reportSubdirLimit = false;
                    lock (_subdirectoryWatchers)
                    {
                        if (_subdirectoryWatchers.Count < s_maxSubdirectoriesPerDirectory)
                        {
                            createSubdirWatcher = true;
                        }
                        else if (!_hasReportedSubdirLimit)
                        {
                            _hasReportedSubdirLimit = true;
                            reportSubdirLimit = true;
                        }
                    }
                    if (createSubdirWatcher)
                    {
                        CreateSingleSubdirectoryWatcher(name);
                    }
                    else if (reportSubdirLimit &&
                             _weakWatcher.TryGetTarget(out FileSystemWatcher? watcherInstance))
                    {
                        watcherInstance.OnError(new ErrorEventArgs(
                            new IOException(SR.Format(SR.FSW_MaxSubdirectoriesExceeded, s_maxSubdirectoriesPerDirectory, _directoryPath))));
                    }
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
                    RunningInstance? childToRemove;
                    lock (_subdirectoryWatchers)
                    {
                        childToRemove = _subdirectoryWatchers.Find(c => c._directoryPath == subdirPath);
                        if (childToRemove is not null)
                        {
                            _subdirectoryWatchers.Remove(childToRemove);
                        }
                    }
                    if (childToRemove is not null)
                    {
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
                    RunningInstance? childToUpdate;
                    lock (_subdirectoryWatchers)
                    {
                        childToUpdate = _subdirectoryWatchers.Find(c => c._directoryPath == oldSubdirPath);
                        if (childToUpdate is not null)
                        {
                            _subdirectoryWatchers.Remove(childToUpdate);
                        }
                    }
                    if (childToUpdate is not null)
                    {
                        childToUpdate.Cancel();
                        CreateSingleSubdirectoryWatcher(newName);
                    }
                }
            }

            private void AssociateSingleFile(string name, FileEntry entry)
            {
                string fullPath = System.IO.Path.Combine(_directoryPath, name);
                nuint cookie = GetNextCookie();
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

                // Check if it's a symlink - skip symlinks to avoid loops and inconsistent behavior
                if (Interop.Sys.LStat(subdirPath, out Interop.Sys.FileStatus status) == 0 &&
                    ((status.Mode & (uint)Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFLNK))
                {
                    return;
                }

                try
                {
                    if (_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                    {
                        var childWatcher = new RunningInstance(watcher, subdirPath, subdirRelativePath, true, _notifyFilters, _cancellationToken);
                        childWatcher.Start();
                        lock (_subdirectoryWatchers)
                        {
                            _subdirectoryWatchers.Add(childWatcher);
                        }
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

            private void DissociateFile(byte[] buffer, nuint cookie, string name)
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

            private void HandleFileEvent(nuint cookie, int events)
            {
                // This method handles individual file attribute/content changes.
                // It raises a Changed event and re-associates the file to continue receiving events.
                // Called when portfs fires FILE_ATTRIB, FILE_MODIFIED, FILE_TRUNC, or FILE_ACCESS
                // events on individually watched files (hybrid mode only).

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

                // Re-associate the file with its current mtime to continue receiving events.
                // Note: port_associate is one-shot; after an event fires, the association
                // is automatically removed, so we must re-associate to keep watching.
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
                            int result = Interop.PortFs.PortAssociate(_portfsHandle, pFileObj, fullPath, &mtime, _portEventMask, cookie);
                            if (result == -1)
                            {
                                // If PortAssociate failed, stop tracking this watch so it can be refreshed
                                // by a subsequent directory snapshot.  The usual reason this would be ENOENT
                                // after a file is deleted, but we can handle all errors the same way.
                                _nameToWatchMap.Remove(name);
                                _cookieToNameMap.Remove(cookie);
                            }
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
                        int result = Interop.PortFs.PortAssociate(_portfsHandle, pFileObj, _directoryPath, &mtime, DirectoryEventMask, DirectoryCookie);

                        if (result == -1)
                        {
                            // If reassociation fails (directory deleted, permissions changed, etc.),
                            // report error and return. The event loop will naturally exit when
                            // no more events arrive since the directory is no longer associated.
                            if (_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                            {
                                Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                                watcher.OnError(new ErrorEventArgs(
                                    Interop.GetExceptionForIoErrno(error, _directoryPath)));
                            }
                        }
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
                // Depth-first cancellation: cancel all children before self.
                // This ensures orderly shutdown of the entire watcher tree. Each child's Cancel()
                // will recursively cancel its own children, then send PortSend to wake its event loop.
                // When the event loop exits, Cleanup() disposes that instance's resources.
                if (_isCancelling)
                    return;

                _isCancelling = true;

                // First, cancel all subdirectory watchers (depth-first)
                // Take a lock when creating the snapshot to avoid races with concurrent modifications
                RunningInstance[] children;
                lock (_subdirectoryWatchers)
                {
                    children = _subdirectoryWatchers.ToArray();
                }
                foreach (RunningInstance child in children)
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
                // Clean up only THIS instance's resources.
                // Child subdirectory watchers clean themselves up via depth-first Cancel() chain.

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

            internal static DirectorySnapshot Create(string path)
            {
                // Get directory mtime before reading
                if (Interop.Sys.Stat(path, out Interop.Sys.FileStatus status) != 0)
                {
                    throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo(), path);
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
                    string name = System.IO.Path.GetFileName(entry)!;
                    try
                    {
                        FileEntry fileEntry = FileEntry.Create(entry);
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

            internal static FileEntry Create(string path)
            {
                if (Interop.Sys.Stat(path, out Interop.Sys.FileStatus status) != 0)
                {
                    throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo(), path);
                }

                FileEntry entry = default;
                entry.IsDirectory = (status.Mode & (uint)Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR;
                entry.MTime = new Interop.Sys.TimeSpec { TvSec = status.MTime, TvNsec = status.MTimeNsec };
                entry.Inode = status.Ino;

                return entry;
            }
        }

        private static void RestartForInternalBufferSize()
        {
            // The implementation is not using InternalBufferSize. There's no need to restart.
        }
    }
}
