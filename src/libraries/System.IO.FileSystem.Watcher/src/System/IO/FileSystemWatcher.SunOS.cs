// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// SunOS FileSystemWatcher Implementation using portfs (event ports)
//
// Design Overview:
// - Single portfs event port for entire watch hierarchy
// - Single background thread running unified event loop
// - Lightweight FileNode/DirectoryNode objects representing watched entries
// - Type-safe global cookie namespace for event routing
//
// Core Data Structures:
// - FileNode: Base class for all watched entries (files and directories)
//   * Contains: Name, Path, Cookie, FileObjBuffer, Metadata
// - DirectoryNode: Inherits FileNode, adds directory-specific state
//   * Adds: RelativePath, CurrentSnapshot, Entries dictionary
//   * Single Entries dictionary holds both files and subdirectories
// - DirectorySnapshot: Immutable snapshot of directory contents for comparison
//   * Contains List<FileEntry> sorted by name
//   * Each FileEntry represents one child of the directory
// - FileEntry: Complete directory entry (name + metadata)
//   * Name: Simple filename relative to parent directory (no path separators)
//   * Status: Complete FileStatus structure (140 bytes) from stat()
//   * Constructor takes name and ref to FileStatus (caller does stat/lstat/fstat)
//   * Invariant: FileEntry.Name is always a simple name, never a path
//
// Resource Efficiency:
//   For watching N subdirectories: 1 port + 1 thread + N lightweight node objects
//   Example: 1000 subdirectories uses 1 file descriptor and 1 thread
//
// Event Flow:
// 1. Initialization (StartRaisingEvents):
//    - Create single port handle
//    - Build DirectoryNode for root directory
//    - Recursively build tree if IncludeSubdirectories=true (breadth-first)
//    - Associate all directories and files with the single port
//    - Start single event processing thread
//
// 2. Event Processing (EventLoop):
//    - Single port_get() loop waiting for events
//    - Route events by cookie to FileNode or DirectoryNode
//    - Type-based dispatch: DirectoryNode → HandleDirectoryEvent
//                          FileNode → HandleFileEvent
//
// 3. Directory Events (HandleDirectoryEvent):
//    - Re-associate directory with port
//    - Take new snapshot of directory contents
//    - Compare with old snapshot (merge algorithm)
//    - Process additions, deletions, renames, and replacements
//
// 4. File Events (HandleFileEvent):
//    - Re-associate file with port
//    - Update metadata (stat) for the file
//    - Raise Changed event for file data or attribute changes
//
// 5. Cleanup (StopRaisingEvents):
//    - Cancel token
//    - Send port_send to wake up port_get
//    - Wait for event thread to exit
//    - Clean up all resources
//
// PortAssociate Strategy:
//
// Each DirectoryNode creates ONE port association:
//   - Path: Directory path
//   - Mask: FILE_MODIFIED | FILE_ATTRIB | FILE_ACCESS (combined mask)
//   - Cookie: Unique identifier for this DirectoryNode
//   - Purpose: Detect structural changes (add/remove/rename) AND attribute changes (chmod, chown)
//   - Handler: HandleDirectoryEvent(DirectoryNode, events)
//
// Each FileNode (when _watchIndividualFiles = true) creates ONE port association:
//   - Path: File path
//   - Mask: _portEventFileMask (computed from NotifyFilters, excludes FileName/DirectoryName)
//   - Cookie: Unique identifier for this FileNode
//   - Purpose: Detect content/attribute changes on individual files
//   - Handler: HandleFileEvent(FileNode, events)
//
// Subdirectory Attribute Detection:
//   - DirectoryNode watches its own attributes via combined mask
//   - No separate FileNode association needed for subdirectory entries
//   - Single association per directory achieves full coverage
//
// Event Mask Computation (TranslateFilters):
//   - NotifyFilters.LastWrite | Size     → FILE_MODIFIED | FILE_TRUNC
//   - NotifyFilters.Attributes | Security | CreationTime → FILE_ATTRIB
//   - NotifyFilters.LastAccess           → FILE_ACCESS
//   - NotifyFilters.FileName | DirectoryName → (NO event mask - handled by directory's FILE_MODIFIED)
//
// Key Design Decisions:
// 1. Inheritance: DirectoryNode inherits from FileNode
// 2. Single dictionary: DirectoryNode.Entries stores both files and subdirectories
// 3. Cached paths: RelativePath is updated recursively on rename
// 4. Full FileStatus: FileEntry stores complete stat structure
// 5. Simple names: FileEntry.Name is always relative to parent
// 6. Breadth-first construction: AssociateDirectoryContents uses queue (no recursion)
// 7. Type-safe routing: Dictionary<nuint, FileNode> (no object casting)
//
// Why not inotify?  There is an inotify implementation on some illumos distributions.
// This implementation does not use inotify because that would restrict us to only
// those distributions that have it, and even for those that do, the developers have
// indicated that their inotify implementation should be treated as experimental.
// By contrast, portfs is reliable and available on all distributions.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public partial class FileSystemWatcher
    {
        // ===== State and Fields =====

        /// <summary>
        /// Cancellation for the currently running watch operation.
        /// This is non-null if an operation has been started and null if stopped.
        /// </summary>
        private CancellationTokenSource? _cancellation;

        // ===== Lifecycle Methods =====

        private void StartRaisingEvents()
        {
            Debug.WriteLine($"[FSW] StartRaisingEvents: Entry");

            // If we're called when "Initializing" is true, set enabled to true
            if (IsSuspended())
            {
                _enabled = true;
                return;
            }

            // If we already have a cancellation object, we're already running.
            if (_cancellation is not null)
            {
                return;
            }

            Debug.WriteLine($"[FSW] StartRaisingEvents: creating event port");

            // Create the event port (portfs)
            SafeFileHandle handle = Interop.PortFs.PortCreate();
            if (handle.IsInvalid)
            {
                Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                handle.Dispose();
                throw Interop.GetExceptionForIoErrno(error);
            }

            Debug.WriteLine($"[FSW] StartRaisingEvents: creating RunningInstance");

            try
            {
                // Create the cancellation object that will be used by this FileSystemWatcher to cancel the new watch operation
                CancellationTokenSource cancellation = new CancellationTokenSource();

                // Create and start the RunningInstance
                // All state associated with the watch operation is stored in a separate object; this is done
                // to avoid race conditions that could result if the users quickly starts/stops repeatedly,
                // causing multiple active operations to all be outstanding at the same time.
                // Note: runner must be a local to avoid GC ref issues.
                var runner = new RunningInstance(
                    this, handle, _directory,
                    IncludeSubdirectories, NotifyFilter, cancellation.Token);

                Debug.WriteLine($"[FSW] StartRaisingEvents: Created RunningInstance");

                // Now that we've created the runner, store the cancellation object and mark the instance
                // as running.  We wait to do this so that if there was a failure, StartRaisingEvents
                // may be called to try again without first having to call StopRaisingEvents.
                _cancellation = cancellation;
                _enabled = true;

                Debug.WriteLine($"[FSW] StartRaisingEvents: Stored cancellation token");

                // Start the runner
                runner.Start();

                Debug.WriteLine($"[FSW] StartRaisingEvents: Started RunningInstance");
            }
            catch
            {
                // If we fail to actually start the watching even though we've opened the
                // portfs handle, close the portfs handle proactively rather than waiting for it
                // to be finalized.
                _enabled = false;
                _cancellation?.Dispose();
                _cancellation = null;
                handle.Dispose();
                throw;
            }
        }

        /// <summary>Cancels the currently running watch operation if there is one.</summary>
        private void StopRaisingEvents()
        {
            Debug.WriteLine($"[FSW] StopRaisingEvents: Entry");

            _enabled = false;

            if (IsSuspended())
                return;

            // If there's an active cancellation token, cancel and release it.
            // The cancellation token and the processing task respond to cancellation
            // to handle all other cleanup.
            var cts = _cancellation;
            if (cts is not null)
            {
                _cancellation = null;
                try
                {
                    Debug.WriteLine($"[FSW] StopRaisingEvents: Calling cts.Cancel()");
                    cts.Cancel();      // calls CancellationCallback
                }
                finally
                {
                    cts.Dispose();
                    Debug.WriteLine($"[FSW] StopRaisingEvents: Finished cts.Cancel()");
                }
            }
        }

        /// <summary>Called when FileSystemWatcher is finalized.</summary>
        private void FinalizeDispose()
        {
            // The RunningInstance remains rooted and holds open the SafeFileHandle until it's explicitly
            // torn down.  FileSystemWatcher.Dispose will call StopRaisingEvents, but not on finalization;
            // thus we need to explicitly call it here.
            Debug.WriteLine($"[FSW] FinalizeDispose on thread {Thread.CurrentThread.ManagedThreadId}");
            StopRaisingEvents();
        }

        // ===== RunningInstance Class =====

        /// <summary>
        /// State and processing associated with an active watch operation.  This state is kept separate from FileSystemWatcher to avoid
        /// race conditions when a user starts/stops/starts/stops/etc. in quick succession, resulting in the potential for multiple
        /// active operations. It also helps with avoiding rooted cycles and enabling proper finalization.
        /// </summary>
        private sealed class RunningInstance
        {
            // ===== Constants =====

            // Event mask for cancelling a PortGet (just make it wake up)
            // The actual event flag chosen here does not matter, though to avoid confusion
            // this uses an event flag that we don't need to use for anything else.
            private const int CancellationEvents = (int)Interop.PortFs.PortEvent.FILE_NOFOLLOW;

            // Cookie value for cancellation wake-up event (sent via port_send)
            private const nuint CancellationCookie = 0;

            // ===== Fields =====

            // Core state

            /// <summary>
            /// Weak reference to the associated watcher.  A weak reference is used so that the FileSystemWatcher may be collected and finalized,
            /// causing an active operation to be torn down.  With a strong reference, a blocking read on the portfs handle will keep alive this
            /// instance which will keep alive the FileSystemWatcher which will not be finalizable and thus which will never signal to the blocking
            /// read to wake up in the event that the user neglects to stop raising events.
            /// </summary>
            private readonly WeakReference<FileSystemWatcher> _weakWatcher;

            /// <summary>
            /// The path for the primary watched directory.
            /// </summary>
            private readonly string _directory;

            private readonly bool _includeSubdirectories;
            private readonly NotifyFilters _notifyFilters;

            // Configuration (computed from NotifyFilters)
            private readonly int _portEventMaskDir;
            private readonly int _portEventMaskFile;
            private readonly bool _watchIndividualFiles;

            // Port and event processing
            private SafeFileHandle? _portfsHandle;
            private CancellationToken _cancellationToken;
            private ExecutionContext? _executionContext;

            // Directory tree (single root with flat hierarchy via Entries dictionaries)
            private DirectoryNode? _rootDirectory;
            private Dictionary<nuint, FileNode>? _cookieMap;
            private nuint _nextCookie = 1; // 0 reserved for cancellation

            // ===== Lifecycle =====

            internal RunningInstance(
                FileSystemWatcher watcher,
                SafeFileHandle portfsHandle,
                string directoryPath,
                bool includeSubdirectories,
                NotifyFilters notifyFilters,
                CancellationToken cancellationToken)
            {
                Debug.Assert(watcher != null);
                Debug.Assert(portfsHandle != null && !portfsHandle.IsInvalid && !portfsHandle.IsClosed);
                Debug.Assert(directoryPath != null);

                Debug.WriteLine($"[RI] RunningInstance Create Entry");

                _weakWatcher = new WeakReference<FileSystemWatcher>(watcher);
                _portfsHandle = portfsHandle;
                _includeSubdirectories = includeSubdirectories;
                _notifyFilters = notifyFilters;
                _cancellationToken = cancellationToken;

                // Normalize and resolve path
                string fullPath = System.IO.Path.GetFullPath(directoryPath);
                _directory = Interop.Sys.RealPath(fullPath) ?? fullPath;

                // Compute event masks from NotifyFilters
                _portEventMaskDir = TranslateFilters(notifyFilters, isDir: true);
                _portEventMaskFile = TranslateFilters(notifyFilters, isDir: false);
                _watchIndividualFiles = (_portEventMaskFile != 0);

                // Initialize cookie map
                _cookieMap = new Dictionary<nuint, FileNode>();

                // Create root directory node
                string rootName = System.IO.Path.GetFileName(_directory) ?? _directory;
                if (Interop.Sys.LStat(_directory, out Interop.Sys.FileStatus rootStatus) != 0)
                {
                    throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo(), _directory);
                }
                DirectorySnapshot rootSnapshot = DirectorySnapshot.Create(_directory);

                nuint rootCookie = GetNextCookie();

                _rootDirectory = new DirectoryNode(
                    name: rootName,
                    path: _directory,
                    relativePath: "",
                    parent: null,
                    cookie: rootCookie,
                    metadata: rootStatus,
                    initialSnapshot: rootSnapshot,
                    trackEntries: _includeSubdirectories || _watchIndividualFiles);

                _cookieMap[rootCookie] = _rootDirectory;

                // Make first-time associations now.  Callers expect to see changes
                // that might happen between now and when ProcessEvents() starts.

                // Associate root directory
                AssociateNode(_rootDirectory!);

                // Process children if tracking subdirectories or individual files
                if (_includeSubdirectories || _watchIndividualFiles)
                {
                    AssociateDirectoryContents(_rootDirectory!);
                }

                // Current thread is done with _nextCookie. Note that ProcessEvents
                // (started via Start()) will be the only thread using it after this.

                Debug.WriteLine($"[RI] RunningInstance Create Done");
            }

            private void Cleanup()
            {
                Debug.WriteLine($"[RI] Cleanup: Entry");

                _rootDirectory = null;
                _cookieMap?.Clear();
                _cookieMap = null;
                _portfsHandle?.Dispose();
                _portfsHandle = null;

                Debug.WriteLine($"[RI] Cleanup: Done");
            }

            // Called from the cancellation token when the FileSystemWatcher stops.
            internal void CancellationCallback()
            {
                Debug.WriteLine($"[RI] CancellationCallback: Entry");

                // Wake up the event loop by sending cancellation event
                if (_portfsHandle is not null && !_portfsHandle.IsInvalid)
                {
                    unsafe
                    {
                        int result = Interop.PortFs.PortSend(_portfsHandle, CancellationEvents, CancellationCookie);
                        if (result != 0)
                        {
                            Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                            Debug.WriteLine($"[RI] CancellationCallback PortSend {error.Error}");
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"[RI] CancellationCallback: No portfs handle");
                }

                Debug.WriteLine($"[RI] CancellationCallback: Done");
            }

            // ===== Interface Methods =====

            internal void Start()
            {
                Debug.WriteLine($"[RI] Start Entry");

                _executionContext = ExecutionContext.Capture();
                new Thread(RunProcessEvents)
                {
                    IsBackground = true,
                    Name = ".NET File Watcher"
                }.Start();

                Debug.WriteLine($"[RI] Start Done");
            }

            /// <summary>
            /// Run ProcessEvents with the captured ExecutionContext.
            /// This ensures AsyncLocal and other execution context flows to event handlers.
            /// </summary>
            private void RunProcessEvents()
            {
                ExecutionContext? context = _executionContext;
                if (context is not null)
                {
                    // Run under captured context
                    ExecutionContext.Run(context, _ => ProcessEvents(), null);
                }
                else
                {
                    // Flow was suppressed, run directly
                    ProcessEvents();
                }
            }

            /// <summary>
            /// Main processing loop.  Reads events and processes them.
            /// One of these for each watcher (running instance).
            /// </summary>
            private void ProcessEvents()
            {
                Debug.WriteLine($"[RI] ProcessEvents: Entry, (TID={Thread.CurrentThread.ManagedThreadId})");

                // Cancellation Token Registration (CTR) of CancellationCallback.
                // Use local cts to avoid GC ref. problems.
                var ctr = _cancellationToken.UnsafeRegister(
                    obj => ((RunningInstance)obj!).CancellationCallback(), this);

                Debug.WriteLine($"[RI] ProcessEvents: Registered cancel callback, enter loop");

                try
                {
                    while (!_cancellationToken.IsCancellationRequested)
                    {
                        unsafe
                        {
                            nuint cookie = 0;
                            int events = 0;
                            int result = -1;

                            Debug.WriteLine($"[RI] ProcessEvents: call PortGet (block)");

                            // Wait for an event to be returned from the PortFs handle.
                            // This will block until either an event is available or an
                            // error is returned.  Note that CancellationCallback() can
                            // send CancellationCookie to force this loop to terminate.

                            result = Interop.PortFs.PortGet(_portfsHandle!, &events, &cookie, null);
                            Debug.WriteLine($"[RI] ProcessEvents: PortGet returned, result={result}");

                            if (result == -1)
                            {
                                Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                                Debug.WriteLine($"[RI] ProcessEvents: PortGet {error.Error}");
                                if (error.Error == Interop.Error.EINTR ||
                                    error.Error == Interop.Error.ETIMEDOUT)
                                {
                                    continue;
                                }

                                if (error.Error == Interop.Error.EBADF)
                                {
                                    // CancellationCallback closed it.  Terminate quietly.
                                    Debug.WriteLine($"[RI] ProcessEvents: got EBADF");
                                    break;
                                }

                                // Report unexpected error before breaking
                                if (_weakWatcher.TryGetTarget(out FileSystemWatcher? watcherForError))
                                {
                                    watcherForError.OnError(new ErrorEventArgs(Interop.GetExceptionForIoErrno(error)));
                                }
                                break;
                            }

                            if (!HandleEvent(cookie, events))
                            {
                                Debug.WriteLine($"[RI] ProcessEvents: !HandleEvent");
                                break;
                            }
                        } // unsafe
                    } // end while
                    Debug.WriteLine($"[RI] ProcessEvents: finished loop");
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
                    Debug.WriteLine($"[RI] ProcessEvents: finally start");

                    ctr.Dispose();
                    Cleanup();

                    Debug.WriteLine($"[RI] ProcessEvents: finally done");
                }
            } // end of ProcessEvents

            // ===== Event Handlers =====

            /// <summary>
            /// Handle one event. Method does not inline to prevent a strong reference to the watcher.
            /// </summary>
            /// <param name="cookie">Opaque value identifying file or directory.</param>
            /// <param name="events">Mask of PortFs events.</param>
            /// <returns><see langword="true"/> if we can continue processing events,
            /// else <see langword="false"/>.</returns>
            [MethodImpl(MethodImplOptions.NoInlining)]
            private bool HandleEvent(nuint cookie, int events)
            {

                // Check if this is a cancellation event
                if (cookie == CancellationCookie)
                {
                    Debug.WriteLine($"[RI] ProcessEvents: got CancellationCookie");
                    return false;
                }

                // Check if watcher is still alive before processing event
                if (!_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                {
                    Debug.WriteLine($"[RI] ProcessEvents: no watcher");
                    return false;
                }

                // Route event to appropriate handler
                if (_cookieMap!.TryGetValue(cookie, out FileNode? node))
                {
                    if (node is DirectoryNode dirNode)
                    {
                        HandleDirectoryEvent(watcher, dirNode, events);
                    }
                    else
                    {
                        HandleFileEvent(watcher, node, events);
                    }
                }
                else
                {
                    Debug.WriteLine($"[FSW] ProcessEvents: unknown cookie={cookie}");
                }
                return true;
            }

            private void HandleDirectoryEvent(FileSystemWatcher watcher, DirectoryNode dir, int events)
            {
                // CRITICAL EVENT PROCESSING PATTERN:
                // (Same pattern as HandleFileEvent)
                // 1. Stat directory at "observation point" (before raising events)
                // 2. Update node metadata with observation-point mtime
                // 3. Raise Changed event for directory itself if attributes changed
                // 4. Read directory contents (if tracking)
                // 5. Process contents changes (may be slow - user handlers)
                // 6. Re-associate with observation-point mtime
                //
                // WHY: If directory changes during steps 3-5, re-association will detect it:
                //      current mtime > observation mtime → immediate re-notification
                // This ensures NO missed events even if changes happen during processing.

                Debug.WriteLine($"[FSW] HandleDirectoryEvent: path='{dir.Path}' cookie={dir.Cookie} events=0x{events:X}");
                bool reAssociate = true;

                try
                {
                    // Stat the directory first
                    if (Interop.Sys.LStat(dir.Path, out Interop.Sys.FileStatus newStatus) != 0)
                    {
                        throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo(), dir.Path);
                    }

                    // Update metadata at observation point
                    dir.Metadata = newStatus;

                    // Raise Changed event for directory itself if masked attributes changed.
                    // Need for both full-tracking directories and lightweight attribute-only watch.
                    // While we know this is a directory, in this case where we want to know about
                    // changes to the directory node itself (not its contents) so the appropriate
                    // event mask to use is the one we use for _files_.

                    if ((events & _portEventMaskFile) != 0)
                    {
                        if (dir.Parent != null)
                        {
                            watcher.NotifyFileSystemEventArgs(WatcherChangeTypes.Changed, dir.RelativePath);
                        }
                    }
                    else
                    {
                        // Unexpected event, just re-associate
                        Debug.WriteLine($"[FSW] FileSystemWatcher: Spurious file event for '{dir.RelativePath}'");
                    }

                    // Note: Per the port_associate documentation, portfs may always deliver
                    // FILE_DELETE, FILE_RENAME_TO, FILE_RENAME_FROM, UNMOUNTED, MOUNTEDOVER
                    // so just handle any combination of events here with the assumption
                    // we should just always "re-check this directory".
                    // Could try to optimize later based on events flags.

                    // Only process directory contents if we're tracking them (snapshot is not null)
                    if (dir.CurrentSnapshot != null)
                    {
                        // Get snapshot at observation point (mtime already captured above)
                        DirectorySnapshot newSnapshot = DirectorySnapshot.Create(dir.Path);
                        DirectorySnapshot oldSnapshot = dir.CurrentSnapshot;
                        dir.CurrentSnapshot = newSnapshot;

                        // Process contents changes (may take time)
                        CompareSnapshotsAndNotify(watcher, dir, oldSnapshot, newSnapshot);
                    }

                    // Re-associate AFTER event processing with mtime from observation point
                    // If directory changed again during event processing, PortGet returns immediately
                    // See finally: AssociateNode(dir);
                }
                catch (Exception ex) when (ex is DirectoryNotFoundException ||
                                           ex is FileNotFoundException)
                {
                    Debug.WriteLine($"[FSW] Can't stat '{dir.Path}' {ex}");
                    // The directory no longer exists. Remove.
                    reAssociate = false;
                    DissociateNode(dir);
                    _cookieMap!.Remove(dir.Cookie);
                    if (dir.Parent is not null)
                    {
                        if (dir.Parent.Entries != null)
                        {
                            dir.Parent.Entries.Remove(dir.Name);
                        }
                    }
                    RemoveDirectoryTree(dir);
                    if (dir.Parent is null)
                    {
                        // Watcher directory itself was removed. Terminate by throwing and let the outer
                        // processing loop surface the error via OnError to avoid double-reporting.
                        throw;
                    }
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException)
                {
                    Debug.WriteLine($"[FSW] Can't stat '{dir.Path}' {ex}");
                    // Access now disallowed.  Continue as best we can.
                    watcher.OnError(new ErrorEventArgs(ex));
                }
                catch (Exception ex)
                {
                    watcher.OnError(new ErrorEventArgs(ex));
                }
                finally
                {
                    if (reAssociate)
                    {
                        AssociateNode(dir);
                    }
                }
            }

            private void HandleFileEvent(FileSystemWatcher watcher, FileNode file, int events)
            {
                // CRITICAL EVENT PROCESSING PATTERN:
                // (Same pattern as HandleDirectoryEvent)
                // 1. Stat file at "observation point" (before raising events)
                // 2. Update node metadata with observation-point mtime
                // 3. Raise event (may be slow - user handler)
                // 4. Re-associate with observation-point mtime
                // This ensures changes during event processing are detected.

                Debug.WriteLine($"[FSW] HandleFileEvent: path='{file.Path}' cookie={file.Cookie} events=0x{events:X}");
                bool reAssociate = true;

                try
                {
                    // Stat the file first
                    if (Interop.Sys.LStat(file.Path, out Interop.Sys.FileStatus newStatus) != 0)
                    {
                        throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo(), file.Path);
                    }

                    // Update metadata at observation point
                    file.Metadata = newStatus;

                    // Raise Changed event for this file.
                    if ((events & _portEventMaskFile) != 0)
                    {
                        if (file.Parent != null)
                        {
                            string relPath = System.IO.Path.Combine(file.Parent.RelativePath, file.Name);
                            watcher.NotifyFileSystemEventArgs(WatcherChangeTypes.Changed, relPath);
                        }
                    }
                    else
                    {
                        // Unexpected event, just re-associate
                        Debug.WriteLine($"[FSW] FileSystemWatcher: Spurious file event for '{file.Path}'");
                    }

                    // Re-associate AFTER event processing
                    // See finally: AssociateNode(file);
                }
                catch (Exception ex) when (ex is DirectoryNotFoundException ||
                                           ex is FileNotFoundException)
                {
                    Debug.WriteLine($"[FSW] Can't stat '{file.Path}' {ex}");
                    // The file no longer exists.
                    // Let CompareSnapshotsAndNotify handle all file lifecycle events
                    // (delete/rename) to avoid duplicate event notifications.
                    // The directory's CompareSnapshotsAndNotify will detect whether
                    // this was a delete or a rename (via inode matching).
                    reAssociate = false;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException)
                {
                    Debug.WriteLine($"[FSW] Can't stat '{file.Path}' {ex}");
                    // Access now disallowed.  Continue as best we can.
                    watcher.OnError(new ErrorEventArgs(ex));
                }
                catch (Exception ex)
                {
                    watcher.OnError(new ErrorEventArgs(ex));
                }
                finally
                {
                    if (reAssociate)
                    {
                        AssociateNode(file);
                    }
                }
            }

            // ===== Helper Methods =====

            private struct ChangeEvent
            {
                public FileEntry Entry;
                public WatcherChangeTypes Type;
                public bool Processed;
            }

            /// <summary>
            /// Compare two directory snapshots and raise events as appropriate.
            /// </summary>
            /// <param name="watcher">The FileSystemWatcher this services.</param>
            /// <param name="dirNode">The DirectoryNode with events.</param>
            /// <param name="oldSnapshot">The previous DirectorySnapshot.</param>
            /// <param name="newSnapshot">The current DirectorySnapshot.</param>
            /// <remarks>
            /// Design Rationale and Limitations:
            ///
            /// Snapshot comparison is inherently limited because we only observe the
            /// filesystem state at discrete points in time. We cannot see intermediate
            /// operations that happened between snapshots. For example:
            ///   - mv A B; mv B C  appears as: A renamed to C
            ///   - create A; rm A  may not be visible at all
            ///
            /// Rename Detection:
            /// We detect renames by matching inode numbers: if a path disappears and a
            /// different path appears with the same inode in a single snapshot interval,
            /// we infer a rename. This works well for the common case of single renames.
            ///
            /// Hard Link Edge Case:
            /// If multiple file names are hard-linked to the same inode, our dictionaries
            /// (which map inode -> change index) will overwrite earlier entries when we
            /// encounter subsequent names with the same inode. This means only one removed
            /// link and one added link for any given inode will be presented as a rename.
            /// Any other creates or deletes of names for that inode (for a given pair of
            /// snapshots) will be reported as independent creates and deletes.
            ///
            /// This limitation is acceptable because:
            ///   1. Hard links are rare in typical FileSystemWatcher scenarios
            ///   2. Users still get correct information about what changed (files appeared
            ///      and disappeared), just with less pairing precision for hard links
            ///   3. The alternative (tracking multiple indices per inode) adds significant
            ///      complexity for an uncommon case
            ///
            /// This practical approach balances correctness, performance, and maintainability.
            /// </remarks>
            private void CompareSnapshotsAndNotify(FileSystemWatcher watcher, DirectoryNode dirNode, DirectorySnapshot oldSnapshot, DirectorySnapshot newSnapshot)
            {
                var oldEntries = oldSnapshot.SortedEntries;
                var newEntries = newSnapshot.SortedEntries;
                int oldIndex = 0;
                int newIndex = 0;

                // Collect all changes in order during single-pass comparison
                var changes = new List<ChangeEvent>();
                // Dictionaries used for rename detection: (inode) -> index in changes list
                var deletes = new Dictionary<long, int>();
                var creates = new Dictionary<long, int>();

                // Single-pass sorted merge comparison - collect entries in discovery order
                while (oldIndex < oldEntries.Count || newIndex < newEntries.Count)
                {
                    int comparison;
                    ChangeEvent changeEvent;

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
                        comparison = StringComparer.Ordinal.Compare(
                            oldEntries[oldIndex].Name,
                            newEntries[newIndex].Name);
                    }

                    if (comparison < 0)
                    {
                        // Entry in old but not in new - deletion
                        changeEvent = new ChangeEvent
                        {
                            Entry = oldEntries[oldIndex],
                            Type = WatcherChangeTypes.Deleted
                        };
                        changes.Add(changeEvent);
                        deletes[changeEvent.Entry.Inode] = changes.Count - 1;
                        oldIndex++;
                    }
                    else if (comparison > 0)
                    {
                        // Entry in new but not in old - addition
                        changeEvent = new ChangeEvent
                        {
                            Entry = newEntries[newIndex],
                            Type = WatcherChangeTypes.Created
                        };
                        changes.Add(changeEvent);
                        creates[changeEvent.Entry.Inode] = changes.Count - 1;
                        newIndex++;
                    }
                    else
                    {
                        // Entry in both - check if inode or type changed,
                        var oldEntry = oldEntries[oldIndex];
                        var newEntry = newEntries[newIndex];

                        if (oldEntry.Inode != newEntry.Inode ||
                            oldEntry.IsDirectory != newEntry.IsDirectory)
                        {
                            // File name was replaced (new inode)
                            // or type changed (file vs. directory).
                            // Generate Deleted+Created to make the replacement observable
                            changeEvent = new ChangeEvent
                            {
                                Entry = oldEntry,
                                Type = WatcherChangeTypes.Deleted
                            };
                            changes.Add(changeEvent);
                            deletes[changeEvent.Entry.Inode] = changes.Count - 1;
                            changeEvent = new ChangeEvent
                            {
                                Entry = newEntry,
                                Type = WatcherChangeTypes.Created
                            };
                            changes.Add(changeEvent);
                            creates[changeEvent.Entry.Inode] = changes.Count - 1;
                        }
                        // Any other changes, if configured for watching, will be reported via
                        // the PortAssociate done when _watchIndividualFiles is true.
                        // Only name changes are handled here.

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
                    long inode = change.Entry.Inode;

                    // Lookup for matching rename pair in dictionary for opposite type
                    Dictionary<long, int> otherDict =
                        change.Type == WatcherChangeTypes.Created
                        ? deletes
                        : creates;

                    if (otherDict.TryGetValue(inode, out int otherIdx))
                    {
                        // Found rename pair
                        var other = changes[otherIdx];

                        if (change.Type == WatcherChangeTypes.Deleted)
                        {
                            // Deletion with matching creation. Other is "new".
                            ProcessRename(watcher, dirNode, change.Entry, other.Entry);
                        }
                        else
                        {
                            // Creation with matching deletion. Other is "old".
                            ProcessRename(watcher, dirNode, other.Entry, change.Entry);
                        }

                        // Mark both as processed
                        change.Processed = true;
                        changes[i] = change;
                        other.Processed = true;
                        changes[otherIdx] = other;
                    }
                    else if (change.Type == WatcherChangeTypes.Deleted)
                    {
                        // Unpaired deletion
                        ProcessDeletion(watcher, dirNode, change.Entry);
                        change.Processed = true;
                        changes[i] = change;
                    }
                    else
                    {
                        // Unpaired creation
                        ProcessAddition(watcher, dirNode, change.Entry);
                        change.Processed = true;
                        changes[i] = change;
                    }
                }
            }

            // Helper for CompareSnapshotsAndNotify: Addition
            // The entry passed has stat and name information, so we know it exists,
            // and we can stat it.  We might not have rights to actually read it.

            private void ProcessAddition(FileSystemWatcher watcher, DirectoryNode parent, FileEntry entry)
            {
                string fullPath = System.IO.Path.Combine(parent.Path, entry.Name);
                string relPath = System.IO.Path.Combine(parent.RelativePath, entry.Name);
                bool isDir = entry.IsDirectory;

                // Raise Created event
                if ((isDir && (_notifyFilters & NotifyFilters.DirectoryName) != 0) ||
                    (!isDir && (_notifyFilters & NotifyFilters.FileName) != 0))
                {
                    watcher.NotifyFileSystemEventArgs(WatcherChangeTypes.Created, relPath);
                }

                // Add to tree if needed
                if (parent.Entries != null)
                {
                    if (entry.IsDirectory)
                    {
                        // Associate subdirectories if watching recursively OR watching attributes
                        if (_includeSubdirectories || _watchIndividualFiles)
                        {
                            string relativePath = string.IsNullOrEmpty(parent.RelativePath)
                                ? entry.Name
                                : parent.RelativePath + System.IO.Path.DirectorySeparatorChar + entry.Name;
                            nuint cookie = GetNextCookie();
                            DirectoryNode? childNode = null;
                            bool associated = false;

                            try
                            {
                                // Only create snapshot and track entries if watching subdirectories recursively
                                DirectorySnapshot? childSnapshot = _includeSubdirectories
                                    ? DirectorySnapshot.Create(fullPath)
                                    : null;

                                childNode = new DirectoryNode(
                                    name: entry.Name,
                                    path: fullPath,
                                    relativePath: relativePath,
                                    parent: parent,
                                    cookie: cookie,
                                    metadata: entry.Status,
                                    initialSnapshot: childSnapshot,
                                    trackEntries: _includeSubdirectories);

                                parent.Entries[entry.Name] = childNode;
                                _cookieMap![cookie] = childNode;
                                AssociateNode(childNode);
                                associated = true;

                                // Process children if watching subdirectories or individual files
                                if (_includeSubdirectories || _watchIndividualFiles)
                                {
                                    AssociateDirectoryContents(childNode);
                                }
                            }
                            catch (Exception ex)
                            {
                                watcher.OnError(new ErrorEventArgs(ex));

                                // Roll back.
                                if (associated && childNode is not null)
                                {
                                    DissociateNode(childNode);
                                }
                                _cookieMap!.Remove(cookie);
                                parent.Entries.Remove(entry.Name);
                            }
                        }
                    }
                    else
                    {
                        // Not a directory
                        // Associate files if watching attributes
                        if (_watchIndividualFiles)
                        {
                            nuint cookie = GetNextCookie();

                            try
                            {
                                FileNode fileNode = new FileNode(
                                    name: entry.Name,
                                    path: fullPath,
                                    parent: parent,
                                    cookie: cookie,
                                    metadata: entry.Status);

                                parent.Entries[entry.Name] = fileNode;
                                _cookieMap![cookie] = fileNode;

                                AssociateNode(fileNode);
                            }
                            catch (Exception ex)
                            {
                                watcher.OnError(new ErrorEventArgs(ex));

                                // Roll back.
                                _cookieMap!.Remove(cookie);
                                parent.Entries.Remove(entry.Name);
                            }
                        }
                    }
                }
            }

            // Helper for CompareSnapshotsAndNotify: Deletion
            // The entry has been unlinked.
            private void ProcessDeletion(FileSystemWatcher watcher, DirectoryNode parent, FileEntry entry)
            {
                string relPath = System.IO.Path.Combine(parent.RelativePath, entry.Name);
                bool isDir = entry.IsDirectory;

                // Raise Deleted event
                if ((isDir && (_notifyFilters & NotifyFilters.DirectoryName) != 0) ||
                    (!isDir && (_notifyFilters & NotifyFilters.FileName) != 0))
                {
                    watcher.NotifyFileSystemEventArgs(WatcherChangeTypes.Deleted, relPath);
                }

                // Remove from tree if needed
                if (parent.Entries != null && parent.Entries.TryGetValue(entry.Name, out FileNode? nodeToRemove))
                {
                    DissociateNode(nodeToRemove);
                    _cookieMap!.Remove(nodeToRemove.Cookie);
                    parent.Entries.Remove(entry.Name);

                    if (nodeToRemove is DirectoryNode dirNode)
                    {
                        RemoveDirectoryTree(dirNode);
                    }
                }
            }

            // Helper for CompareSnapshotsAndNotify: Rename
            // The entry has been renamed.
            private void ProcessRename(FileSystemWatcher watcher, DirectoryNode parent, FileEntry oldEntry, FileEntry newEntry)
            {
                string oldRelPath = System.IO.Path.Combine(parent.RelativePath, oldEntry.Name);
                string newRelPath = System.IO.Path.Combine(parent.RelativePath, newEntry.Name);
                bool isDir = oldEntry.IsDirectory;

                // Raise Renamed event
                if ((isDir && (_notifyFilters & NotifyFilters.DirectoryName) != 0) ||
                    (!isDir && (_notifyFilters & NotifyFilters.FileName) != 0))
                {
                    watcher.NotifyRenameEventArgs(WatcherChangeTypes.Renamed, newRelPath, oldRelPath);
                }

                // Update tree if needed
                if (parent.Entries != null && parent.Entries.TryGetValue(oldEntry.Name, out FileNode? node))
                {
                    // Remove old name, add new name
                    parent.Entries.Remove(oldEntry.Name);
                    node.Name = newEntry.Name;
                    node.Path = System.IO.Path.Combine(parent.Path, newEntry.Name);
                    node.Metadata = newEntry.Status;
                    parent.Entries[newEntry.Name] = node;

                    try
                    {

                        // Re-associate with port using new path
                        if (node is DirectoryNode dirNode)
                        {
                            // Update RelativePath for renamed directory before updating descendants
                            dirNode.RelativePath = newRelPath;

                            // Update recursive paths for subdirectory
                            UpdateDirectoryPaths(dirNode, dirNode.Path);
                            // Re-associate directory with new path
                            AssociateNode(dirNode);
                        }
                        else if (_watchIndividualFiles)
                        {
                            // Re-associate file with new path
                            AssociateNode(node);
                        }
                    }
                    catch (Exception ex)
                    {
                        watcher.OnError(new ErrorEventArgs(ex));

                        // Something failed?  Treat as deletion instead.
                        // Like ProcessDeletion() above, but the parent map was
                        // already updated for the rename, so remove new name.

                        DissociateNode(node);
                        _cookieMap!.Remove(node.Cookie);
                        parent.Entries.Remove(newEntry.Name);
                        if (node is DirectoryNode dirNode)
                        {
                            RemoveDirectoryTree(dirNode);
                        }
                    }
                }
            }

            private static void UpdateDirectoryPaths(DirectoryNode dir, string newPath)
            {
                // Update path for this directory and all descendants
                if (dir.Entries == null)
                    return;

                foreach (var child in dir.Entries.Values)
                {
                    child.Path = System.IO.Path.Combine(newPath, child.Name);

                    if (child is DirectoryNode childDir)
                    {
                        // Update RelativePath for subdirectory
                        string parentRelPath = dir.RelativePath;
                        childDir.RelativePath = string.IsNullOrEmpty(parentRelPath)
                            ? childDir.Name
                            : System.IO.Path.Combine(parentRelPath, childDir.Name);

                        UpdateDirectoryPaths(childDir, child.Path);
                    }
                }
            }

            private void RemoveDirectoryTree(DirectoryNode dir)
            {
                if (dir.Entries == null)
                    return;

                // Safe to iterate directly: recursive calls modify child.Entries, not dir.Entries
                foreach (var entry in dir.Entries.Values)
                {
                    DissociateNode(entry);
                    _cookieMap!.Remove(entry.Cookie);

                    if (entry is DirectoryNode childDir)
                    {
                        RemoveDirectoryTree(childDir);
                    }
                }

                dir.Entries.Clear();
            }

            private void DissociateNode(FileNode node)
            {
                // Dissociate from portfs to avoid events with unknown cookies.
                // Can get ENOENT if the FileObjBuffer (cookie) is not associated.
                // That's not worth making noise about except maybe when debugging.
                unsafe
                {
                    fixed (byte* ptr = node.FileObjBuffer)
                    {
                        IntPtr pFileObj = (IntPtr)ptr;
                        int result = Interop.PortFs.PortDissociate(_portfsHandle!, pFileObj);
                        if (result != 0)
                        {
                            Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                            Debug.WriteLine($"[FSW] DissociateNode {errorInfo} for '{node.Path}'");
                        }
                    }
                }
            }

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

            private static int TranslateFilters(NotifyFilters filters, bool isDir)
            {
                int mask = 0;

                if (isDir)
                {
                    // On a directory, always watch for structural changes.
                    // Implied NotifyFilters: FileName | DirectoryName
                    mask |= (int)Interop.PortFs.PortEvent.FILE_MODIFIED;
                }

                // LastWrite: detect content changes
                if ((filters & NotifyFilters.LastWrite) != 0)
                {
                    mask |= (int)Interop.PortFs.PortEvent.FILE_MODIFIED;
                }

                // Size: detect size changes (skip for dir)
                if (!isDir && (filters & NotifyFilters.Size) != 0)
                {
                    mask |= (int)Interop.PortFs.PortEvent.FILE_MODIFIED;
                    mask |= (int)Interop.PortFs.PortEvent.FILE_TRUNC;
                }

                // Attributes/Security/CreationTime: detect attribute changes
                if ((filters & (NotifyFilters.Attributes | NotifyFilters.Security | NotifyFilters.CreationTime)) != 0)
                {
                    mask |= (int)Interop.PortFs.PortEvent.FILE_ATTRIB;
                }

                // LastAccess: detect access time changes
                if ((filters & NotifyFilters.LastAccess) != 0)
                {
                    mask |= (int)Interop.PortFs.PortEvent.FILE_ACCESS;
                }

                return mask;
            }

            /// <summary>
            /// Associates a node with the port for event notification.
            /// The mtime parameter tells the kernel: "notify me if this node's mtime becomes > saved mtime".
            /// This ensures we detect changes that occur during event processing, preventing missed events.
            /// </summary>
            private void AssociateNode(FileNode node)
            {
                bool isDirectory = node is DirectoryNode;
                int eventMask = isDirectory ? _portEventMaskDir : _portEventMaskFile;

                // Tell PortAssociate not to follow symlinks. We use Sys.LStat as well.
                // Intentionally omitted from masks we use with returned events.
                eventMask |= (int)Interop.PortFs.PortEvent.FILE_NOFOLLOW;

                Debug.WriteLine($"[FSW] AssociateNode: path='{node.Path}' cookie={node.Cookie} events=0x{eventMask:X}");

                unsafe
                {
                    fixed (byte* ptr = node.FileObjBuffer)
                    {
                        IntPtr pFileObj = (IntPtr)ptr;
                        Interop.Sys.TimeSpec mtime = node.SavedMTime;
                        int result = Interop.PortFs.PortAssociate(
                            _portfsHandle!,
                            pFileObj,
                            node.Path,
                            &mtime,
                            eventMask,
                            node.Cookie);

                        if (result != 0)
                        {
                            Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                            throw Interop.GetExceptionForIoErrno(errorInfo, node.Path);
                        }
                    }
                }
            }

            // Similar to ProcessAddition. Could refactor.
            private void AssociateDirectoryContents(DirectoryNode parent)
            {
                if (parent.Entries == null || parent.CurrentSnapshot == null)
                    return;

                Queue<DirectoryNode> queue = new Queue<DirectoryNode>();
                queue.Enqueue(parent);

                while (queue.Count > 0)
                {
                    DirectoryNode current = queue.Dequeue();

                    if (current.CurrentSnapshot == null || current.Entries == null)
                        continue;

                    foreach (var entry in current.CurrentSnapshot.SortedEntries)
                    {
                        if (entry.IsDirectory)
                        {
                            // Create DirectoryNode for subdirectory
                            string childPath = System.IO.Path.Combine(current.Path, entry.Name);
                            string childRelativePath = string.IsNullOrEmpty(current.RelativePath)
                                ? entry.Name
                                : System.IO.Path.Combine(current.RelativePath, entry.Name);
                            nuint childCookie = GetNextCookie();
                            DirectoryNode? childNode = null;
                            bool associated = false;

                            try
                            {
                                // Only create snapshot for child directories if we're tracking subdirectories
                                DirectorySnapshot? childSnapshot = _includeSubdirectories
                                    ? DirectorySnapshot.Create(childPath)
                                    : null;

                                childNode = new DirectoryNode(
                                    name: entry.Name,
                                    path: childPath,
                                    relativePath: childRelativePath,
                                    parent: current,
                                    cookie: childCookie,
                                    metadata: entry.Status,
                                    initialSnapshot: childSnapshot,
                                    trackEntries: _includeSubdirectories);

                                current.Entries[entry.Name] = childNode;
                                _cookieMap![childCookie] = childNode;
                                AssociateNode(childNode);
                                associated = true;

                                if (_includeSubdirectories)
                                {
                                    queue.Enqueue(childNode);
                                }
                            }
                            catch (Exception ex)
                            {
                                if (_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                                {
                                    watcher.OnError(new ErrorEventArgs(ex));
                                }

                                // Roll back.
                                if (associated && childNode is not null)
                                {
                                    DissociateNode(childNode);
                                }
                                _cookieMap!.Remove(childCookie);
                                current.Entries.Remove(entry.Name);
                            }
                        }
                        else if (_watchIndividualFiles)
                        {
                            // Create FileNode for file
                            string filePath = System.IO.Path.Combine(current.Path, entry.Name);
                            nuint fileCookie = GetNextCookie();

                            try
                            {
                                FileNode fileNode = new FileNode(
                                    name: entry.Name,
                                    path: filePath,
                                    parent: current,
                                    cookie: fileCookie,
                                    metadata: entry.Status);

                                current.Entries[entry.Name] = fileNode;
                                _cookieMap![fileCookie] = fileNode;

                                AssociateNode(fileNode);
                            }
                            catch (Exception ex)
                            {
                                if (_weakWatcher.TryGetTarget(out FileSystemWatcher? watcher))
                                {
                                    watcher.OnError(new ErrorEventArgs(ex));
                                }

                                // Roll back.
                                _cookieMap!.Remove(fileCookie);
                                current.Entries.Remove(entry.Name);
                            }
                        }
                    }
                }
            }

        } // End of RunningInstance class


        /// <summary>
        /// Represents a watched file entry. Also serves as the base class for DirectoryNode.
        /// Contains all data needed for port_associate and event handling.
        /// </summary>
        private class FileNode
        {
            // Identity
            public string Name { get; set; }
            public string Path { get; set; }
            public DirectoryNode? Parent { get; }
            public nuint Cookie { get; }

            // Port association state
            public byte[] FileObjBuffer { get; }

            // Metadata (from stat)
            public Interop.Sys.FileStatus Metadata { get; set; }

            /// <summary>
            /// Gets the mtime to pass to port_associate.
            /// CRITICAL: This must be the mtime captured at the "observation point" - when we stat'd
            /// the file/directory BEFORE processing its contents or raising events. This ensures:
            /// - If mtime changes during event processing, port_associate will notice (current > saved)
            /// - We get immediate re-notification, preventing missed events
            /// - The kernel will notify us whenever actual mtime > this saved mtime
            /// Never update Metadata to "latest" mtime - always capture at observation point.
            /// </summary>
            public Interop.Sys.TimeSpec SavedMTime => new Interop.Sys.TimeSpec
            {
                TvSec = Metadata.MTime,
                TvNsec = Metadata.MTimeNsec
            };

            public FileNode(string name, string path, DirectoryNode? parent,
                           nuint cookie, Interop.Sys.FileStatus metadata)
            {
                Name = name;
                Path = path;
                Parent = parent;
                Cookie = cookie;
                Metadata = metadata;
                FileObjBuffer = GC.AllocateArray<byte>(Interop.PortFs.FileObjSize, pinned: true);
            }
        }

        /// <summary>
        /// Represents a watched directory. Inherits all FileNode properties and adds
        /// directory-specific state (snapshot, entry tracking).
        /// </summary>
        private sealed class DirectoryNode : FileNode
        {
            // Directory-specific identity
            public string RelativePath { get; set; }

            // Directory content tracking (null when not tracking subdirectory contents)
            public DirectorySnapshot? CurrentSnapshot { get; set; }

            // All entries in this directory (files + subdirectories)
            // Since DirectoryNode IS-A FileNode, both can be stored here!
            // Only allocated if _includeSubdirectories OR _watchIndividualFiles
            public Dictionary<string, FileNode>? Entries { get; }

            public DirectoryNode(string name, string path, string relativePath,
                                DirectoryNode? parent, nuint cookie,
                                Interop.Sys.FileStatus metadata,
                                DirectorySnapshot? initialSnapshot,
                                bool trackEntries)
                : base(name, path, parent, cookie, metadata)
            {
                RelativePath = relativePath;
                CurrentSnapshot = initialSnapshot;

                if (trackEntries)
                    Entries = new Dictionary<string, FileNode>(StringComparer.Ordinal);
            }
        }

        private sealed class DirectorySnapshot
        {
            internal List<FileEntry> SortedEntries { get; }

            private DirectorySnapshot(List<FileEntry> sortedEntries)
            {
                SortedEntries = sortedEntries;
            }

            /// <summary>
            /// Creates a snapshot of directory contents.
            /// Caller is responsible for stat'ing the directory itself for metadata.
            /// </summary>
            internal static DirectorySnapshot Create(string path)
            {
                // Read directory contents
                var entries = new List<FileEntry>();

                foreach (string fullPath in Directory.EnumerateFileSystemEntries(path))
                {
                    try
                    {
                        // Might like to use statat(2) here later.
                        string name = System.IO.Path.GetFileName(fullPath)!;
                        // Use LStat to not follow symlinks (we don't want to traverse outside the watch tree)
                        if (Interop.Sys.LStat(fullPath, out Interop.Sys.FileStatus entryStatus) == 0)
                        {
                            FileEntry entry = new FileEntry(name, ref entryStatus);
                            entries.Add(entry);
                        }
                    }
                    catch (Exception ex) when (ex is DirectoryNotFoundException ||
                                               ex is FileNotFoundException ||
                                               ex is UnauthorizedAccessException)
                    {
                        Debug.WriteLine($"[FSW] Can't stat '{fullPath}' ENOENT/EACCES Ex={ex}");
                        // Just continue with other directory entries.
                    }
                    catch (IOException ex)
                    {
                        Debug.WriteLine($"[FSW] Can't stat '{fullPath}' Unexpected Ex={ex}");
                        // Just continue with other directory entries.
                    }
                }

                // Sort by name for comparison
                entries.Sort((a, b) => StringComparer.Ordinal.Compare(a.Name, b.Name));

                return new DirectorySnapshot(entries);
            }
        }

        /// <summary>
        /// Represents a directory entry: simple name + file metadata.
        /// Invariant: Name is always a simple filename relative to parent directory (no path separators).
        /// </summary>
        private struct FileEntry
        {
            internal string Name;
            internal Interop.Sys.FileStatus Status;

            internal bool IsDirectory =>
                (Status.Mode & (uint)Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR;

            internal Interop.Sys.TimeSpec MTime =>
                new Interop.Sys.TimeSpec { TvSec = Status.MTime, TvNsec = Status.MTimeNsec };

            internal long Inode => Status.Ino;

            /// <summary>
            /// Creates a FileEntry from a name and already-obtained FileStatus.
            /// Caller is responsible for calling Stat/LStat/FStat to obtain the FileStatus.
            /// </summary>
            /// <param name="name">Simple filename (no path separators)</param>
            /// <param name="status">FileStatus obtained from stat/lstat/fstat</param>
            internal FileEntry(string name, ref Interop.Sys.FileStatus status)
            {
                Name = name;
                Status = status;
            }
        }

        private static void RestartForInternalBufferSize()
        {
            // The implementation is not using InternalBufferSize. There's no need to restart.
        }
    }
}
