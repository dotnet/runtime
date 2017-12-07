// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.IO
{
    internal unsafe partial class FindEnumerable<TResult, TState> : CriticalFinalizerObject, IEnumerable<TResult>, IEnumerator<TResult>
    {
        private readonly string _originalFullPath;
        private readonly string _originalUserPath;
        private readonly bool _recursive;
        private readonly FindTransform<TResult> _transform;
        private readonly FindPredicate<TState> _predicate;
        private readonly TState _state;

        private object _lock = new object();
        private int _enumeratorCreated;

        private Interop.NtDll.FILE_FULL_DIR_INFORMATION* _info;
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
        public FindEnumerable(
            string directory,
            FindTransform<TResult> transform,
            FindPredicate<TState> predicate,
            TState state = default,
            bool recursive = false)
        {
            _originalUserPath = directory;
            _originalFullPath = Path.GetFullPath(directory);
            _recursive = recursive;
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            _transform = transform ?? throw new ArgumentNullException(nameof(transform));
            _state = state;
            Initialize();
        }

        private FindEnumerable(
            string originalUserPath,
            string originalFullPath,
            FindTransform<TResult> transform,
            FindPredicate<TState> predicate,
            TState state,
            bool recursive)
        {
            _originalUserPath = originalUserPath;
            _originalFullPath = originalFullPath;
            _predicate = predicate;
            _transform = transform;
            _state = state;
            _recursive = recursive;
            Initialize();
        }

        /// <summary>
        /// Simple wrapper to allow creating a file handle for an existing directory.
        /// </summary>
        public static IntPtr CreateDirectoryHandle(string path)
        {
            IntPtr handle = Interop.Kernel32.CreateFile_IntPtr(
                path,
                Interop.Kernel32.FileOperations.FILE_LIST_DIRECTORY,
                FileShare.ReadWrite | FileShare.Delete,
                FileMode.Open,
                Interop.Kernel32.FileOperations.FILE_FLAG_BACKUP_SEMANTICS);

            if (handle == IntPtr.Zero || handle == (IntPtr)(-1))
            {
                // Historically we throw directory not found rather than file not found
                int error = Marshal.GetLastWin32Error();
                if (error == Interop.Errors.ERROR_FILE_NOT_FOUND)
                    error = Interop.Errors.ERROR_PATH_NOT_FOUND;

                throw Win32Marshal.GetExceptionForWin32Error(error, path);
            }

            return handle;
        }

        public IEnumerator<TResult> GetEnumerator()
        {
            if (Interlocked.Exchange(ref _enumeratorCreated, 1) == 0)
            {
                return this;
            }
            else
            {
                return new FindEnumerable<TResult, TState>(_originalUserPath, _originalFullPath, _transform, _predicate, _state, _recursive);
            }
        }

        private void Initialize()
        {
            _currentPath = _originalFullPath;
            _buffer = ArrayPool<byte>.Shared.Rent(4096);
            _pinnedBuffer = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
            if (_recursive)
                _pending = new Queue<(IntPtr, string)>();
            _directoryHandle = CreateDirectoryHandle(_originalFullPath);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public TResult Current => _current;

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_lastEntryFound)
                return false;

            lock (_lock)
            {
                if (_lastEntryFound)
                    return false;

                RawFindData findData = default;
                do
                {
                    FindNextFile();
                    if (!_lastEntryFound && _info != null)
                    {
                        // If needed, stash any subdirectories to process later
                        if (_recursive && (_info->FileAttributes & FileAttributes.Directory) != 0
                            && !PathHelpers.IsDotOrDotDot(_info->FileName))
                        {
                            string subDirectory = PathHelpers.CombineNoChecks(_currentPath, _info->FileName);
                            IntPtr subDirectoryHandle = CreateDirectoryHandle(subDirectory);
                            try
                            {
                                // It is possible this might allocate and run out of memory
                                _pending.Enqueue((subDirectoryHandle, subDirectory));
                            }
                            catch
                            {
                                Interop.Kernel32.CloseHandle(subDirectoryHandle);
                                throw;
                            }
                        }

                        findData = new RawFindData(_info, _currentPath, _originalFullPath, _originalUserPath);
                    }
                } while (!_lastEntryFound && !_predicate(ref findData, _state));

                if (!_lastEntryFound)
                    _current = _transform(ref findData);

                return !_lastEntryFound;
            }
        }

        private unsafe void FindNextFile()
        {
            Interop.NtDll.FILE_FULL_DIR_INFORMATION* info = _info;
            if (info != null && info->NextEntryOffset != 0)
            {
                // We're already in a buffer and have another entry
                _info = (Interop.NtDll.FILE_FULL_DIR_INFORMATION*)((byte*)info + info->NextEntryOffset);
                return;
            }

            // We need more data
            if (GetData())
                _info = (Interop.NtDll.FILE_FULL_DIR_INFORMATION*)_pinnedBuffer.AddrOfPinnedObject();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DirectoryFinished()
        {
            _info = null;
            if (_pending == null || _pending.Count == 0)
            {
                _lastEntryFound = true;
            }
            else
            {
                // Grab the next directory to parse
                Interop.Kernel32.CloseHandle(_directoryHandle);
                (_directoryHandle, _currentPath) = _pending.Dequeue();
                FindNextFile();
            }
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            // It is possible to fail to allocate the lock, but the finalizer will still run
            if (_lock != null)
            {
                lock (_lock)
                {
                    _lastEntryFound = true;

                    // Don't ever close a valid handle twice as they can be reused- set to zero to ensure this
                    Interop.Kernel32.CloseHandle(_directoryHandle);
                    _directoryHandle = IntPtr.Zero;

                    if (_recursive && _pending != null)
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
        }

        ~FindEnumerable()
        {
            Dispose(disposing: false);
        }
    }
}
