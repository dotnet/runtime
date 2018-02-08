// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Enumeration
{
    public unsafe abstract partial class FileSystemEnumerator<TResult> : CriticalFinalizerObject, IEnumerator<TResult>
    {
        // The largest supported path on Unix is 4K bytes of UTF-8 (most only support 1K)
        private const int StandardBufferSize = 4096;

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

        // Used for creating full paths
        private char[] _pathBuffer;

        /// <summary>
        /// Encapsulates a find operation.
        /// </summary>
        /// <param name="directory">The directory to search in.</param>
        /// <param name="options">Enumeration options to use.</param>
        public FileSystemEnumerator(string directory, EnumerationOptions options = null)
        {
            _originalRootDirectory = directory ?? throw new ArgumentNullException(nameof(directory));
            _rootDirectory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar);
            _options = options ?? EnumerationOptions.Default;

            // We need to initialize the directory handle up front to ensure
            // we immediately throw IO exceptions for missing directory/etc.
            _directoryHandle = CreateDirectoryHandle(_rootDirectory);
            if (_directoryHandle == null)
                _lastEntryFound = true;

            _currentPath = _rootDirectory;

            try
            {
                _pathBuffer = ArrayPool<char>.Shared.Rent(StandardBufferSize);
            }
            catch
            {
                // Close the directory handle right away if we fail to allocate
                CloseDirectoryHandle();
                throw;
            }
        }

        private static SafeDirectoryHandle CreateDirectoryHandle(string path)
        {
            // TODO: https://github.com/dotnet/corefx/issues/26715
            // - Check access denied option and allow through if specified.
            // - Use IntPtr handle directly
            SafeDirectoryHandle handle = Interop.Sys.OpenDir(path);
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

                    bool isDirectory = FileSystemEntry.Initialize(ref entry, _entry, _currentPath, _rootDirectory, _originalRootDirectory, new Span<char>(_pathBuffer));

                    if (_options.AttributesToSkip != 0)
                    {
                        if ((_options.AttributesToSkip & ~(FileAttributes.Directory | FileAttributes.Hidden | FileAttributes.ReparsePoint)) == 0)
                        {
                            // These three we don't have to hit the disk again to evaluate
                            if (((_options.AttributesToSkip & FileAttributes.Directory) != 0 && isDirectory)
                                || ((_options.AttributesToSkip & FileAttributes.Hidden) != 0 && _entry.InodeName[0] == '.')
                                || ((_options.AttributesToSkip & FileAttributes.ReparsePoint) != 0 && _entry.InodeType == Interop.Sys.NodeType.DT_LNK))
                                continue;
                        }
                        else if ((_options.AttributesToSkip & entry.Attributes) != 0)
                        {
                            // Hitting Attributes on the FileSystemEntry will cause a stat call
                            continue;
                        }
                    }

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
