// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.IO.Enumeration
{
    public unsafe abstract partial class FileSystemEnumerator<TResult> : CriticalFinalizerObject, IEnumerator<TResult>
    {
        private const int StandardBufferSize = 4096;

        // We need to have enough room for at least a single entry. The filename alone can be 512 bytes, we'll ensure we have
        // a reasonable buffer for all of the other metadata as well.
        private const int MinimumBufferSize = 1024;

        private readonly string _originalRootDirectory;
        private readonly string _rootDirectory;
        private readonly EnumerationOptions _options;

        private readonly object _lock = new object();

        private Interop.NtDll.FILE_FULL_DIR_INFORMATION* _entry;
        private TResult _current;

        private byte[] _buffer;
        private IntPtr _directoryHandle;
        private string _currentPath;
        private bool _lastEntryFound;
        private Queue<(IntPtr Handle, string Path)> _pending;
        private GCHandle _pinnedBuffer;

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

            // We'll only suppress the media insertion prompt on the topmost directory as that is the
            // most likely scenario and we don't want to take the perf hit for large enumerations.
            // (We weren't consistent with how we handled this historically.)
            using (new DisableMediaInsertionPrompt())
            {
                // We need to initialize the directory handle up front to ensure
                // we immediately throw IO exceptions for missing directory/etc.
                _directoryHandle = CreateDirectoryHandle(_rootDirectory);
                if (_directoryHandle == IntPtr.Zero)
                    _lastEntryFound = true;
            }

            _currentPath = _rootDirectory;

            int requestedBufferSize = _options.BufferSize;
            int bufferSize = requestedBufferSize <= 0 ? StandardBufferSize
                : Math.Max(MinimumBufferSize, requestedBufferSize);

            try
            {
                _buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
                _pinnedBuffer = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
            }
            catch
            {
                // Close the directory handle right away if we fail to allocate
                CloseDirectoryHandle();
                throw;
            }
        }

        private void CloseDirectoryHandle()
        {
            // As handles can be reused we want to be extra careful to close handles only once
            IntPtr handle = Interlocked.Exchange(ref _directoryHandle, IntPtr.Zero);
            if (handle != IntPtr.Zero)
                Interop.Kernel32.CloseHandle(handle);
        }

        /// <summary>
        /// Simple wrapper to allow creating a file handle for an existing directory.
        /// </summary>
        private IntPtr CreateDirectoryHandle(string path)
        {
            IntPtr handle = Interop.Kernel32.CreateFile_IntPtr(
                path,
                Interop.Kernel32.FileOperations.FILE_LIST_DIRECTORY,
                FileShare.ReadWrite | FileShare.Delete,
                FileMode.Open,
                Interop.Kernel32.FileOperations.FILE_FLAG_BACKUP_SEMANTICS);

            if (handle == IntPtr.Zero || handle == (IntPtr)(-1))
            {
                int error = Marshal.GetLastWin32Error();

                if ((error == Interop.Errors.ERROR_ACCESS_DENIED &&
                    _options.IgnoreInaccessible) || ContinueOnError(error))
                {
                    return IntPtr.Zero;
                }

                if (error == Interop.Errors.ERROR_FILE_NOT_FOUND)
                {
                    // Historically we throw directory not found rather than file not found
                    error = Interop.Errors.ERROR_PATH_NOT_FOUND;
                }

                throw Win32Marshal.GetExceptionForWin32Error(error, path);
            }

            return handle;
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

                    // Calling the constructor inside the try block would create a second instance on the stack.
                    FileSystemEntry.Initialize(ref entry, _entry, _currentPath, _rootDirectory, _originalRootDirectory);

                    // Skip specified attributes
                    if ((_entry->FileAttributes & _options.AttributesToSkip) != 0)
                        continue;

                    if ((_entry->FileAttributes & FileAttributes.Directory) != 0)
                    {
                        // Subdirectory found
                        if (!(_entry->FileName.Length > 2 || _entry->FileName[0] != '.' || (_entry->FileName.Length == 2 && _entry->FileName[1] != '.')))
                        {
                            // "." or "..", don't process unless the option is set
                            if (!_options.ReturnSpecialDirectories)
                                continue;
                        }
                        else if (_options.RecurseSubdirectories && ShouldRecurseIntoEntry(ref entry))
                        {
                            // Recursion is on and the directory was accepted, Queue it
                            string subDirectory = Path.Join(_currentPath, _entry->FileName);
                            IntPtr subDirectoryHandle = CreateRelativeDirectoryHandle(_entry->FileName, subDirectory);
                            if (subDirectoryHandle != IntPtr.Zero)
                            {
                                try
                                {
                                    if (_pending == null)
                                        _pending = new Queue<(IntPtr, string)>();
                                    _pending.Enqueue((subDirectoryHandle, subDirectory));
                                }
                                catch
                                {
                                    // Couldn't queue the handle, close it and rethrow
                                    Interop.Kernel32.CloseHandle(subDirectoryHandle);
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
            _entry = Interop.NtDll.FILE_FULL_DIR_INFORMATION.GetNextInfo(_entry);
            if (_entry != null)
                return;

            // We need more data
            if (GetData())
                _entry = (Interop.NtDll.FILE_FULL_DIR_INFORMATION*)_pinnedBuffer.AddrOfPinnedObject();
        }

        private void DequeueNextDirectory()
        {
            (_directoryHandle, _currentPath) = _pending.Dequeue();
        }

        private void InternalDispose(bool disposing)
        {
            // It is possible to fail to allocate the lock, but the finalizer will still run
            if (_lock != null)
            {
                lock (_lock)
                {
                    _lastEntryFound = true;

                    CloseDirectoryHandle();

                    if (_pending != null)
                    {
                        while (_pending.Count > 0)
                            Interop.Kernel32.CloseHandle(_pending.Dequeue().Handle);
                        _pending = null;
                    }

                    if (_pinnedBuffer.IsAllocated)
                        _pinnedBuffer.Free();

                    if (_buffer != null)
                        ArrayPool<byte>.Shared.Return(_buffer);

                    _buffer = null;
                }
            }

            Dispose(disposing);
        }
    }
}
