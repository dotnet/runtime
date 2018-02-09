// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
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
        // Used to get the raw entry data
        private byte[] _entryBuffer;

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
                int size = Interop.Sys.ReadBufferSize;
                _entryBuffer = size > 0 ? ArrayPool<byte>.Shared.Rent(size) : null;
            }
            catch
            {
                // Close the directory handle right away if we fail to allocate
                CloseDirectoryHandle();
                throw;
            }
        }

        private SafeDirectoryHandle CreateDirectoryHandle(string path)
        {
            // TODO: https://github.com/dotnet/corefx/issues/26715
            // - Check access denied option and allow through if specified.
            // - Use IntPtr handle directly
            SafeDirectoryHandle handle = Interop.Sys.OpenDir(path);
            if (handle.IsInvalid)
            {
                Interop.ErrorInfo info = Interop.Sys.GetLastErrorInfo();
                if ((_options.IgnoreInaccessible && IsAccessError(info.RawErrno))
                    || ContinueOnError(info.RawErrno))
                {
                    return null;
                }
                throw Interop.GetExceptionForIoErrno(info, path, isDirectory: true);
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
                                || ((_options.AttributesToSkip & FileAttributes.Hidden) != 0 && _entry.Name[0] == '.')
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
                        if (_entry.Name[0] == '.' && (_entry.Name[1] == 0 || (_entry.Name[1] == '.' && _entry.Name[2] == 0)))
                        {
                            // "." or "..", don't process unless the option is set
                            if (!_options.ReturnSpecialDirectories)
                                continue;
                        }
                        else if (_options.RecurseSubdirectories && ShouldRecurseIntoEntry(ref entry))
                        {
                            // Recursion is on and the directory was accepted, Queue it
                            string subdirectory = PathHelpers.CombineNoChecks(_currentPath, entry.FileName);
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
            Span<byte> buffer = _entryBuffer == null ? Span<byte>.Empty : new Span<byte>(_entryBuffer);
            int result = Interop.Sys.ReadDir(_directoryHandle, buffer, ref _entry);
            switch (result)
            {
                case -1:
                    // End of directory
                    DirectoryFinished();
                    break;
                case 0:
                    // Success
                    break;
                default:
                    // Error
                    if ((_options.IgnoreInaccessible && IsAccessError(result))
                        || ContinueOnError(result))
                    {
                        DirectoryFinished();
                        break;
                    }
                    else
                    {
                        throw Interop.GetExceptionForIoErrno(new Interop.ErrorInfo(result), _currentPath, isDirectory: true);
                    }
            }
        }

        private static bool IsAccessError(int error)
            => error == (int)Interop.Error.EACCES || error == (int)Interop.Error.EBADF
                || error == (int)Interop.Error.EPERM;

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

                    if (_pathBuffer != null)
                        ArrayPool<char>.Shared.Return(_pathBuffer);
                    _pathBuffer = null;
                    if (_entryBuffer != null)
                        ArrayPool<byte>.Shared.Return(_entryBuffer);
                    _entryBuffer = null;
                }
            }

            Dispose(disposing);
        }
    }
}
