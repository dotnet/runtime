// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Threading;

namespace System.IO.Enumeration
{
    public abstract unsafe partial class FileSystemEnumerator<TResult> : CriticalFinalizerObject, IEnumerator<TResult>
    {
        // The largest supported path on Unix is 4K bytes of UTF-8 (most only support 1K)
        private const int StandardBufferSize = 4096;

        private readonly string _originalRootDirectory;
        private readonly string _rootDirectory;
        private readonly EnumerationOptions _options;

        private readonly object _lock = new object();

        private string? _currentPath;
        private IntPtr _directoryHandle;
        private bool _lastEntryFound;
        private Queue<(string Path, int RemainingDepth)>? _pending;

        private Interop.Sys.DirectoryEntry _entry;
        private TResult? _current;

        // Used for creating full paths
        private char[]? _pathBuffer;
        // Used to get the raw entry data
        private byte[]? _entryBuffer;

        private void Init()
        {
            // We need to initialize the directory handle up front to ensure
            // we immediately throw IO exceptions for missing directory/etc.
            _directoryHandle = CreateDirectoryHandle(_rootDirectory);
            if (_directoryHandle == IntPtr.Zero)
                _lastEntryFound = true;

            _currentPath = _rootDirectory;

            try
            {
                _pathBuffer = ArrayPool<char>.Shared.Rent(StandardBufferSize);
                int size = Interop.Sys.GetReadDirRBufferSize();
                _entryBuffer = size > 0 ? ArrayPool<byte>.Shared.Rent(size) : null;
            }
            catch
            {
                // Close the directory handle right away if we fail to allocate
                CloseDirectoryHandle();
                throw;
            }
        }

        private bool InternalContinueOnError(Interop.ErrorInfo info, bool ignoreNotFound = false)
            => (ignoreNotFound && IsDirectoryNotFound(info)) || (_options.IgnoreInaccessible && IsAccessError(info)) || ContinueOnError(info.RawErrno);

        private static bool IsDirectoryNotFound(Interop.ErrorInfo info)
            => info.Error == Interop.Error.ENOTDIR || info.Error == Interop.Error.ENOENT;

        private static bool IsAccessError(Interop.ErrorInfo info)
            => info.Error == Interop.Error.EACCES || info.Error == Interop.Error.EBADF
                || info.Error == Interop.Error.EPERM;

        private IntPtr CreateDirectoryHandle(string path, bool ignoreNotFound = false)
        {
            IntPtr handle = Interop.Sys.OpenDir(path);
            if (handle == IntPtr.Zero)
            {
                Interop.ErrorInfo info = Interop.Sys.GetLastErrorInfo();
                if (InternalContinueOnError(info, ignoreNotFound))
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
                fixed (byte* entryBufferPtr = _entryBuffer)
                {
                    do
                    {
                        FindNextEntry(entryBufferPtr, _entryBuffer == null ? 0 : _entryBuffer.Length);
                        if (_lastEntryFound)
                            return false;

                        FileAttributes attributes = FileSystemEntry.Initialize(
                            ref entry, _entry, _currentPath, _rootDirectory, _originalRootDirectory, new Span<char>(_pathBuffer));
                        bool isDirectory = (attributes & FileAttributes.Directory) != 0;
                        bool isSymlink = (attributes & FileAttributes.ReparsePoint) != 0;

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
                            // entry.IsHidden and entry.IsReadOnly will hit the disk if the caches had not been
                            // initialized yet and we could not soft-retrieve the attributes in Initialize
                            if ((ShouldSkip(FileAttributes.Directory) && isDirectory) ||
                                (ShouldSkip(FileAttributes.ReparsePoint) && isSymlink) ||
                                (ShouldSkip(FileAttributes.Hidden) && entry.IsHidden) ||
                                (ShouldSkip(FileAttributes.ReadOnly) && entry.IsReadOnly))
                            {
                                continue;
                            }
                        }

                        if (isDirectory && !isSpecialDirectory)
                        {
                            if (_options.RecurseSubdirectories && _remainingRecursionDepth > 0 && ShouldRecurseIntoEntry(ref entry))
                            {
                                // Recursion is on and the directory was accepted, Queue it
                                _pending ??= new Queue<(string Path, int RemainingDepth)>();
                                _pending.Enqueue((Path.Join(_currentPath, entry.FileName), _remainingRecursionDepth - 1));
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

            bool ShouldSkip(FileAttributes attributeToSkip) => (_options.AttributesToSkip & attributeToSkip) != 0;
        }

        private unsafe void FindNextEntry()
        {
            fixed (byte* entryBufferPtr = _entryBuffer)
            {
                FindNextEntry(entryBufferPtr, _entryBuffer == null ? 0 : _entryBuffer.Length);
            }
        }

        private unsafe void FindNextEntry(byte* entryBufferPtr, int bufferLength)
        {
            int result;
            fixed (Interop.Sys.DirectoryEntry* e = &_entry)
            {
                result = Interop.Sys.ReadDirR(_directoryHandle, entryBufferPtr, bufferLength, e);
            }

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
                    if (InternalContinueOnError(new Interop.ErrorInfo(result)))
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

        private bool DequeueNextDirectory()
        {
            // In Windows we open handles before we queue them, not after. If we fail to create the handle
            // but are ok with it (IntPtr.Zero), we don't queue them. Unix can't handle having a lot of
            // open handles, so we open after the fact.
            //
            // Doing the same on Windows would create a performance hit as we would no longer have the context
            // of the parent handle to open from. Keeping the parent handle open would increase the amount of
            // data we're maintaining, the number of active handles (they're not infinite), and the length
            // of time we have handles open (preventing some actions such as renaming/deleting/etc.).

            _directoryHandle = IntPtr.Zero;

            while (_directoryHandle == IntPtr.Zero)
            {
                if (_pending == null || _pending.Count == 0)
                    return false;

                (_currentPath, _remainingRecursionDepth) = _pending.Dequeue();
                _directoryHandle = CreateDirectoryHandle(_currentPath, ignoreNotFound: true);
            }

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
                    _pending = null;

                    CloseDirectoryHandle();

                    if (_pathBuffer is char[] pathBuffer)
                    {
                        _pathBuffer = null;
                        ArrayPool<char>.Shared.Return(pathBuffer);
                    }

                    if (_entryBuffer is byte[] entryBuffer)
                    {
                        _entryBuffer = null;
                        ArrayPool<byte>.Shared.Return(entryBuffer);
                    }
                }
            }

            Dispose(disposing);
        }
    }
}
