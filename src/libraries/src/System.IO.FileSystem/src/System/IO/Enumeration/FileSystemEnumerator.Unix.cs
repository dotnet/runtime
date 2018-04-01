// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Threading;

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
        private IntPtr _directoryHandle;
        private bool _lastEntryFound;
        private Queue<string> _pending;

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
            _rootDirectory = PathInternal.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
            _options = options ?? EnumerationOptions.Default;

            // We need to initialize the directory handle up front to ensure
            // we immediately throw IO exceptions for missing directory/etc.
            _directoryHandle = CreateDirectoryHandle(_rootDirectory);
            if (_directoryHandle == IntPtr.Zero)
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

        private bool InternalContinueOnError(int error)
            => (_options.IgnoreInaccessible && IsAccessError(error)) || ContinueOnError(error);

        private static bool IsAccessError(int error)
            => error == (int)Interop.Error.EACCES || error == (int)Interop.Error.EBADF
                || error == (int)Interop.Error.EPERM;

        private IntPtr CreateDirectoryHandle(string path)
        {
            IntPtr handle = Interop.Sys.OpenDir(path);
            if (handle == IntPtr.Zero)
            {
                Interop.ErrorInfo info = Interop.Sys.GetLastErrorInfo();
                if (InternalContinueOnError(info.RawErrno))
                {
                    return IntPtr.Zero;
                }
                throw Interop.GetExceptionForIoErrno(info, path, isDirectory: true);
            }
            return handle;
        }

        private void CloseDirectoryHandle()
        {
            IntPtr handle = Interlocked.Exchange(ref _directoryHandle, IntPtr.Zero);
            if (handle != IntPtr.Zero)
                Interop.Sys.CloseDir(handle);
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

                // If HAVE_READDIR_R is defined for the platform FindNextEntry depends on _entryBuffer being fixed since
                // _entry will point to a string in the middle of the array. If the array is not fixed GC can move it after
                // the native call and _entry will point to a bogus file name. 
                fixed (byte* _ = _entryBuffer)
                {
                    do
                    {
                        FindNextEntry();
                        if (_lastEntryFound)
                            return false;

                        FileAttributes attributes = FileSystemEntry.Initialize(
                            ref entry, _entry, _currentPath, _rootDirectory, _originalRootDirectory, new Span<char>(_pathBuffer));
                        bool isDirectory = (attributes & FileAttributes.Directory) != 0;

                        bool isSpecialDirectory = false;
                        if (isDirectory)
                        {
                            // Subdirectory found
                            if (_entry.Name[0] == '.' && (_entry.Name[1] == 0 || (_entry.Name[1] == '.' && _entry.Name[2] == 0)))
                            {
                                // "." or "..", don't process unless the option is set
                                if (!_options.ReturnSpecialDirectories)
                                    continue;
                                isSpecialDirectory = true;
                            }
                        }

                        if (!isSpecialDirectory && _options.AttributesToSkip != 0)
                        {
                            if ((_options.AttributesToSkip & FileAttributes.ReadOnly) != 0)
                            {
                                // ReadOnly is the only attribute that requires hitting entry.Attributes (which hits the disk)
                                attributes = entry.Attributes;
                            }

                            if ((_options.AttributesToSkip & attributes) != 0)
                            {
                                continue;
                            }
                        }

                        if (isDirectory && !isSpecialDirectory)
                        {
                            if (_options.RecurseSubdirectories && ShouldRecurseIntoEntry(ref entry))
                            {
                                // Recursion is on and the directory was accepted, Queue it
                                if (_pending == null)
                                    _pending = new Queue<string>();
                                _pending.Enqueue(Path.Join(_currentPath, entry.FileName));
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
                    if (InternalContinueOnError(result))
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

        private void DequeueNextDirectory()
        {
            _currentPath = _pending.Dequeue();
            _directoryHandle = CreateDirectoryHandle(_currentPath);
        }

        private void InternalDispose(bool disposing)
        {
            // It is possible to fail to allocate the lock, but the finalizer will still run
            if (_lock != null)
            {
                lock(_lock)
                {
                    _lastEntryFound = true;
                    _pending = null;

                    CloseDirectoryHandle();

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
