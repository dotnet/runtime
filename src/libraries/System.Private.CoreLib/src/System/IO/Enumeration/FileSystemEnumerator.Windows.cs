// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.IO.Enumeration
{
    /// <summary>Enumerates the file system elements of the provided type that are being searched and filtered by a <see cref="Enumeration.FileSystemEnumerable{T}" />.</summary>
    public abstract unsafe partial class FileSystemEnumerator<TResult> : CriticalFinalizerObject, IEnumerator<TResult>
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
        private TResult? _current;

        private IntPtr _buffer;
        private int _bufferLength;
        private IntPtr _directoryHandle;
        private string? _currentPath;
        private bool _lastEntryFound;
        private Queue<(IntPtr Handle, string Path, int RemainingDepth)>? _pending;

        private void Init()
        {
            // We'll only suppress the media insertion prompt on the topmost directory as that is the
            // most likely scenario and we don't want to take the perf hit for large enumerations.
            // (We weren't consistent with how we handled this historically.)
            using (default(DisableMediaInsertionPrompt))
            {
                // We need to initialize the directory handle up front to ensure
                // we immediately throw IO exceptions for missing directory/etc.
                _directoryHandle = CreateDirectoryHandle(_rootDirectory);
                if (_directoryHandle == IntPtr.Zero)
                    _lastEntryFound = true;
            }

            _currentPath = _rootDirectory;

            int requestedBufferSize = _options.BufferSize;
            _bufferLength = requestedBufferSize <= 0 ? StandardBufferSize
                : Math.Max(MinimumBufferSize, requestedBufferSize);

            try
            {
                // NtQueryDirectoryFile needs its buffer to be 64bit aligned to work
                // successfully with FileFullDirectoryInformation on ARM32. AllocHGlobal
                // will return pointers aligned as such, new byte[] does not.
                _buffer = Marshal.AllocHGlobal(_bufferLength);
            }
            catch
            {
                // Close the directory handle right away if we fail to allocate
                CloseDirectoryHandle();
                throw;
            }
        }

        /// <summary>
        /// Fills the buffer with the next set of data.
        /// </summary>
        /// <returns>'true' if new data was found</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe bool GetData()
        {
            Debug.Assert(_directoryHandle != (IntPtr)(-1) && _directoryHandle != IntPtr.Zero && !_lastEntryFound);

            Interop.NtDll.IO_STATUS_BLOCK statusBlock;
            int status = Interop.NtDll.NtQueryDirectoryFile(
                FileHandle: _directoryHandle,
                Event: IntPtr.Zero,
                ApcRoutine: IntPtr.Zero,
                ApcContext: IntPtr.Zero,
                IoStatusBlock: &statusBlock,
                FileInformation: _buffer,
                Length: (uint)_bufferLength,
                FileInformationClass: Interop.NtDll.FILE_INFORMATION_CLASS.FileFullDirectoryInformation,
                ReturnSingleEntry: Interop.BOOLEAN.FALSE,
                FileName: null,
                RestartScan: Interop.BOOLEAN.FALSE);

            switch ((uint)status)
            {
                case Interop.StatusOptions.STATUS_NO_MORE_FILES:
                    DirectoryFinished();
                    return false;
                case Interop.StatusOptions.STATUS_SUCCESS:
                    Debug.Assert(statusBlock.Information.ToInt64() != 0);
                    return true;
                // FILE_NOT_FOUND can occur when there are NO files in a volume root (usually there are hidden system files).
                case Interop.StatusOptions.STATUS_FILE_NOT_FOUND:
                    DirectoryFinished();
                    return false;
                default:
                    int error = (int)Interop.NtDll.RtlNtStatusToDosError(status);

                    // Note that there are many NT status codes that convert to ERROR_ACCESS_DENIED.
                    if ((error == Interop.Errors.ERROR_ACCESS_DENIED && _options.IgnoreInaccessible) || ContinueOnError(error))
                    {
                        DirectoryFinished();
                        return false;
                    }
                    throw Win32Marshal.GetExceptionForWin32Error(error, _currentPath);
            }
        }

        private unsafe IntPtr CreateRelativeDirectoryHandle(ReadOnlySpan<char> relativePath, string fullPath)
        {
            (uint status, IntPtr handle) = Interop.NtDll.CreateFile(
                relativePath,
                _directoryHandle,
                Interop.NtDll.CreateDisposition.FILE_OPEN,
                Interop.NtDll.DesiredAccess.FILE_LIST_DIRECTORY | Interop.NtDll.DesiredAccess.SYNCHRONIZE,
                createOptions: Interop.NtDll.CreateOptions.FILE_SYNCHRONOUS_IO_NONALERT | Interop.NtDll.CreateOptions.FILE_DIRECTORY_FILE
                    | Interop.NtDll.CreateOptions.FILE_OPEN_FOR_BACKUP_INTENT);

            switch (status)
            {
                case Interop.StatusOptions.STATUS_SUCCESS:
                    return handle;
                default:
                    // Note that there are numerous cases where multiple NT status codes convert to a single Win32 System Error codes,
                    // such as ERROR_ACCESS_DENIED. As we want to replicate Win32 handling/reporting and the mapping isn't documented,
                    // we should always do our logic on the converted code, not the NTSTATUS.

                    int error = (int)Interop.NtDll.RtlNtStatusToDosError((int)status);

                    if (ContinueOnDirectoryError(error, ignoreNotFound: true))
                    {
                        return IntPtr.Zero;
                    }

                    throw Win32Marshal.GetExceptionForWin32Error(error, fullPath);
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
        private IntPtr CreateDirectoryHandle(string path, bool ignoreNotFound = false)
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

                if (ContinueOnDirectoryError(error, ignoreNotFound))
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

        private bool ContinueOnDirectoryError(int error, bool ignoreNotFound)
        {
            // Directories can be removed (ERROR_FILE_NOT_FOUND) or replaced with a file of the same name (ERROR_DIRECTORY) while
            // we are enumerating. The only reasonable way to handle this is to simply move on. There is no such thing as a "true"
            // snapshot of filesystem state- our "snapshot" will consider the name non-existent in this rare case.

            return (ignoreNotFound && (error == Interop.Errors.ERROR_FILE_NOT_FOUND || error == Interop.Errors.ERROR_PATH_NOT_FOUND || error == Interop.Errors.ERROR_DIRECTORY))
                || (error == Interop.Errors.ERROR_ACCESS_DENIED && _options.IgnoreInaccessible)
                || ContinueOnError(error);
        }

        /// <summary>Advances the enumerator to the next item of the <see cref="Enumeration.FileSystemEnumerator{T}" />.</summary>
        /// <returns><see langword="true" /> if the enumerator successfully advanced to the next item; <see langword="false" /> if the end of the enumerator has been passed.</returns>
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
                    FileSystemEntry.Initialize(ref entry, _entry, _currentPath.AsSpan(), _rootDirectory.AsSpan(), _originalRootDirectory.AsSpan());

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
                        else if (_options.RecurseSubdirectories && _remainingRecursionDepth > 0 && ShouldRecurseIntoEntry(ref entry))
                        {
                            // Recursion is on and the directory was accepted, Queue it
                            string subDirectory = Path.Join(_currentPath.AsSpan(), _entry->FileName);
                            IntPtr subDirectoryHandle = CreateRelativeDirectoryHandle(_entry->FileName, subDirectory);
                            if (subDirectoryHandle != IntPtr.Zero)
                            {
                                try
                                {
                                    _pending ??= new Queue<(IntPtr, string, int)>();
                                    _pending.Enqueue((subDirectoryHandle, subDirectory, _remainingRecursionDepth - 1));
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
                _entry = (Interop.NtDll.FILE_FULL_DIR_INFORMATION*)_buffer;
        }

        private bool DequeueNextDirectory()
        {
            if (_pending == null || _pending.Count == 0)
                return false;

            (_directoryHandle, _currentPath, _remainingRecursionDepth) = _pending.Dequeue();
            return true;
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

                    if (_buffer != default)
                    {
                        Marshal.FreeHGlobal(_buffer);
                    }

                    _buffer = default;
                }
            }

            Dispose(disposing);
        }
    }
}
