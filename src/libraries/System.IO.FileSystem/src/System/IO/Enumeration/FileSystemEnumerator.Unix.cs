// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Enumeration
{
    public unsafe abstract partial class FileSystemEnumerator<TResult> : CriticalFinalizerObject, IEnumerator<TResult>
    {
        private readonly string _originalRootDirectory;
        private readonly string _rootDirectory;
        private readonly EnumerationOptions _options;

        private readonly object _lock = new object();

        private string _currentPath;
        private SafeDirectoryHandle _directoryHandle;
        private bool _lastEntryFound;
        private Queue<(SafeDirectoryHandle Handle, string Path)> _pending;

        private Interop.Sys.DirectoryEntry _entry;
        private TResult _current;

        /// <summary>
        /// Encapsulates a find operation.
        /// </summary>
        /// <param name="directory">The directory to search in.</param>
        /// <param name="options">Enumeration options to use.</param>
        public FileSystemEnumerator(string directory, EnumerationOptions options = null)
        {
            _originalRootDirectory = directory ?? throw new ArgumentNullException(nameof(directory));
            _rootDirectory = Path.GetFullPath(directory);
            _options = options ?? EnumerationOptions.Default;

            // We need to initialize the directory handle up front to ensure
            // we immediately throw IO exceptions for missing directory/etc.
            _directoryHandle = CreateDirectoryHandle(_rootDirectory);
            if (_directoryHandle == null)
                _lastEntryFound = true;

            _currentPath = _rootDirectory;
        }

        private static SafeDirectoryHandle CreateDirectoryHandle(string path)
        {
            // TODO: https://github.com/dotnet/corefx/issues/26715
            // - Check access denied option and allow through if specified.
            // - Use IntPtr handle directly
            Microsoft.Win32.SafeHandles.SafeDirectoryHandle handle = Interop.Sys.OpenDir(path);
            if (handle.IsInvalid)
            {
                throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo(), path, isDirectory: true);
            }
            return handle;
        }

        private void CloseDirectoryHandle()
        {
            _directoryHandle?.Dispose();
            _directoryHandle = null;
        }

        public bool MoveNext()
        {
            if (_lastEntryFound)
                return false;

            FileSystemEntry entry = default;

            lock (_lock)
            {
                if (_lastEntryFound)
                    return false;

                do
                {
                    FindNextEntry();
                    if (_lastEntryFound)
                        return false;

                    // Get from the dir entry whether the entry is a file or directory.
                    // We classify everything as a file unless we know it to be a directory.
                    // (This includes regular files, FIFOs, etc.)
                    bool isDirectory = false;
                    if (_entry.InodeType == Interop.Sys.NodeType.DT_DIR)
                    {
                        // We know it's a directory.
                        isDirectory = true;
                    }
                    else if (_entry.InodeType == Interop.Sys.NodeType.DT_LNK || _entry.InodeType == Interop.Sys.NodeType.DT_UNKNOWN)
                    {
                        // It's a symlink or unknown: stat to it to see if we can resolve it to a directory.
                        // If we can't (e.g. symlink to a file, broken symlink, etc.), we'll just treat it as a file.
                        isDirectory = FileSystem.DirectoryExists(Path.Combine(_currentPath, _entry.InodeName));
                    }

                    if (_options.AttributesToSkip != 0)
                    {
                        if (((_options.AttributesToSkip & FileAttributes.Directory) != 0 && isDirectory)
                            || ((_options.AttributesToSkip & FileAttributes.Hidden) != 0 && _entry.InodeName[0] == '.')
                            || ((_options.AttributesToSkip & FileAttributes.ReparsePoint) != 0 && _entry.InodeType == Interop.Sys.NodeType.DT_LNK))
                            continue;

                        // TODO: https://github.com/dotnet/corefx/issues/26715
                        // Handle readonly skipping
                    }

                    FileSystemEntry.Initialize(ref entry, _entry, isDirectory, _currentPath, _rootDirectory, _originalRootDirectory);

                    if (isDirectory)
                    {
                        // Subdirectory found
                        if (PathHelpers.IsDotOrDotDot(_entry.InodeName))
                        {
                            // "." or "..", don't process unless the option is set
                            if (!_options.ReturnSpecialDirectories)
                                continue;
                        }
                        else if (_options.RecurseSubdirectories && ShouldRecurseIntoEntry(ref entry))
                        {
                            // Recursion is on and the directory was accepted, Queue it
                            string subdirectory = PathHelpers.CombineNoChecks(_currentPath, _entry.InodeName);
                            SafeDirectoryHandle subdirectoryHandle = CreateDirectoryHandle(subdirectory);
                            if (subdirectoryHandle != null)
                            {
                                try
                                {
                                    if (_pending == null)
                                        _pending = new Queue<(SafeDirectoryHandle, string)>();
                                    _pending.Enqueue((subdirectoryHandle, subdirectory));
                                }
                                catch
                                {
                                    // Couldn't queue the handle, close it and rethrow
                                    subdirectoryHandle.Dispose();
                                    throw;
                                }
                            }
                        }
                    }

                    if (ShouldIncludeEntry(ref entry))
                    {
                        _current = TransformEntry(ref entry);
                        return true;
                    }
                } while (true);
            }
        }

        private unsafe void FindNextEntry()
        {
            // Read each entry from the enumerator
            if (Interop.Sys.ReadDir(_directoryHandle, out _entry) != 0)
            {
                // TODO: https://github.com/dotnet/corefx/issues/26715
                // - Refactor ReadDir so we can process errors here

                // Directory finished
                DirectoryFinished();
            }
        }

        private void InternalDispose(bool disposing)
        {
            // It is possible to fail to allocate the lock, but the finalizer will still run
            if (_lock != null)
            {
                lock(_lock)
                {
                    _lastEntryFound = true;

                    CloseDirectoryHandle();

                    if (_pending != null)
                    {
                        while (_pending.Count > 0)
                            _pending.Dequeue().Handle.Dispose();
                        _pending = null;
                    }
                }
            }

            Dispose(disposing);
        }
    }
}
