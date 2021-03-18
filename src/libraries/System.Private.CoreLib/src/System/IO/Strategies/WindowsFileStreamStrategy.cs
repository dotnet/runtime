// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;

namespace System.IO.Strategies
{
    // this type serves some basic functionality that is common for Async and Sync Windows File Stream Strategies
    internal abstract class WindowsFileStreamStrategy : FileStreamStrategy
    {
        // Error codes (not HRESULTS), from winerror.h
        internal const int ERROR_BROKEN_PIPE = 109;
        internal const int ERROR_NO_DATA = 232;
        protected const int ERROR_HANDLE_EOF = 38;
        protected const int ERROR_INVALID_PARAMETER = 87;
        protected const int ERROR_IO_PENDING = 997;

        protected readonly SafeFileHandle _fileHandle; // only ever null if ctor throws
        protected readonly string? _path; // The path to the opened file.
        private readonly FileAccess _access; // What file was opened for.
        private readonly bool _canSeek; // Whether can seek (file) or not (pipe).
        private readonly bool _isPipe; // Whether to disable async buffering code.

        protected long _filePosition;
        protected bool _exposedHandle; // Whether the file stream's handle has been exposed.
        private long _appendStart; // When appending, prevent overwriting file.

        internal WindowsFileStreamStrategy(SafeFileHandle handle, FileAccess access)
        {
            _exposedHandle = true;

            InitFromHandle(handle, access, out _canSeek, out _isPipe);

            // Note: Cleaner to set the following fields in ValidateAndInitFromHandle,
            // but we can't as they're readonly.
            _access = access;

            // As the handle was passed in, we must set the handle field at the very end to
            // avoid the finalizer closing the handle when we throw errors.
            _fileHandle = handle;
        }

        internal WindowsFileStreamStrategy(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options)
        {
            string fullPath = Path.GetFullPath(path);

            _path = fullPath;
            _access = access;

            _fileHandle = FileStreamHelpers.OpenHandle(fullPath, mode, access, share, options);

            try
            {
                _canSeek = true;

                Init(mode, path);
            }
            catch
            {
                // If anything goes wrong while setting up the stream, make sure we deterministically dispose
                // of the opened handle.
                _fileHandle.Dispose();
                _fileHandle = null!;
                throw;
            }
        }

        public sealed override bool CanSeek => _canSeek;

        public sealed override bool CanRead => !_fileHandle.IsClosed && (_access & FileAccess.Read) != 0;

        public sealed override bool CanWrite => !_fileHandle.IsClosed && (_access & FileAccess.Write) != 0;

        public unsafe sealed override long Length => FileStreamHelpers.GetFileLength(_fileHandle, _path);

        /// <summary>Gets or sets the position within the current stream</summary>
        public override long Position
        {
            get
            {
                VerifyOSHandlePosition();

                return _filePosition;
            }
            set
            {
                Seek(value, SeekOrigin.Begin);
            }
        }

        internal sealed override string Name => _path ?? SR.IO_UnknownFileName;

        internal sealed override bool IsClosed => _fileHandle.IsClosed;

        internal sealed override bool IsPipe => _isPipe;

        internal sealed override SafeFileHandle SafeFileHandle
        {
            get
            {
                // Flushing is the responsibility of BufferedFileStreamStrategy
                _exposedHandle = true;
                return _fileHandle;
            }
        }

        // ReadByte and WriteByte methods are used only when the user has disabled buffering on purpose
        // their performance is going to be horrible
        // TODO: should we consider adding a new event provider and log an event so it can be detected?
        public override int ReadByte()
        {
            Span<byte> buffer = stackalloc byte[1];
            int bytesRead = Read(buffer);
            return bytesRead == 1 ? buffer[0] : -1;
        }

        public override void WriteByte(byte value)
        {
            Span<byte> buffer = stackalloc byte[1];
            buffer[0] = value;
            Write(buffer);
        }

        // this method just disposes everything (no buffer, no need to flush)
        public override ValueTask DisposeAsync()
        {
            if (_fileHandle != null && !_fileHandle.IsClosed)
            {
                _fileHandle.ThreadPoolBinding?.Dispose();
                _fileHandle.Dispose();
            }

            return ValueTask.CompletedTask;
        }

        internal sealed override void DisposeInternal(bool disposing) => Dispose(disposing);

        // this method just disposes everything (no buffer, no need to flush)
        protected override void Dispose(bool disposing)
        {
            if (_fileHandle != null && !_fileHandle.IsClosed)
            {
                _fileHandle.ThreadPoolBinding?.Dispose();
                _fileHandle.Dispose();
            }
        }

        public sealed override void Flush() => Flush(flushToDisk: false); // we have nothing to flush as there is no buffer here

        internal sealed override void Flush(bool flushToDisk)
        {
            if (flushToDisk && CanWrite)
            {
                FileStreamHelpers.FlushToDisk(_fileHandle, _path);
            }
        }

        public sealed override long Seek(long offset, SeekOrigin origin)
        {
            if (origin < SeekOrigin.Begin || origin > SeekOrigin.End)
                throw new ArgumentException(SR.Argument_InvalidSeekOrigin, nameof(origin));
            if (_fileHandle.IsClosed) ThrowHelper.ThrowObjectDisposedException_FileClosed();
            if (!CanSeek) ThrowHelper.ThrowNotSupportedException_UnseekableStream();

            // Verify that internal position is in sync with the handle
            VerifyOSHandlePosition();

            long oldPos = _filePosition;
            long pos = SeekCore(_fileHandle, offset, origin);

            // Prevent users from overwriting data in a file that was opened in
            // append mode.
            if (_appendStart != -1 && pos < _appendStart)
            {
                SeekCore(_fileHandle, oldPos, SeekOrigin.Begin);
                throw new IOException(SR.IO_SeekAppendOverwrite);
            }

            return pos;
        }

        // This doesn't do argument checking.  Necessary for SetLength, which must
        // set the file pointer beyond the end of the file. This will update the
        // internal position
        protected long SeekCore(SafeFileHandle fileHandle, long offset, SeekOrigin origin, bool closeInvalidHandle = false)
        {
            Debug.Assert(!fileHandle.IsClosed && _canSeek, "!fileHandle.IsClosed && _canSeek");

            return _filePosition = FileStreamHelpers.Seek(fileHandle, _path, offset, origin, closeInvalidHandle);
        }

        internal sealed override void Lock(long position, long length) => FileStreamHelpers.Lock(_fileHandle, _path, position, length);

        internal sealed override void Unlock(long position, long length) => FileStreamHelpers.Unlock(_fileHandle, _path, position, length);

        protected abstract void OnInitFromHandle(SafeFileHandle handle);

        protected virtual void OnInit() { }

        private void Init(FileMode mode, string originalPath)
        {
            FileStreamHelpers.ValidateFileTypeForNonExtendedPaths(_fileHandle, originalPath);

            OnInit();

            // For Append mode...
            if (mode == FileMode.Append)
            {
                _appendStart = SeekCore(_fileHandle, 0, SeekOrigin.End);
            }
            else
            {
                _appendStart = -1;
            }
        }

        private void InitFromHandle(SafeFileHandle handle, FileAccess access, out bool canSeek, out bool isPipe)
        {
#if DEBUG
            bool hadBinding = handle.ThreadPoolBinding != null;

            try
            {
#endif
                InitFromHandleImpl(handle, out canSeek, out isPipe);
#if DEBUG
            }
            catch
            {
                Debug.Assert(hadBinding || handle.ThreadPoolBinding == null, "We should never error out with a ThreadPoolBinding we've added");
                throw;
            }
#endif
        }

        private void InitFromHandleImpl(SafeFileHandle handle, out bool canSeek, out bool isPipe)
        {
            FileStreamHelpers.GetFileTypeSpecificInformation(handle, out canSeek, out isPipe);

            OnInitFromHandle(handle);

            if (_canSeek)
                SeekCore(handle, 0, SeekOrigin.Current);
            else
                _filePosition = 0;
        }

        public sealed override void SetLength(long value)
        {
            if (_appendStart != -1 && value < _appendStart)
                throw new IOException(SR.IO_SetLengthAppendTruncate);

            SetLengthCore(value);
        }

        // We absolutely need this method broken out so that WriteInternalCoreAsync can call
        // a method without having to go through buffering code that might call FlushWrite.
        protected unsafe void SetLengthCore(long value)
        {
            Debug.Assert(value >= 0, "value >= 0");
            VerifyOSHandlePosition();

            FileStreamHelpers.SetFileLength(_fileHandle, _path, value);

            if (_filePosition > value)
            {
                SeekCore(_fileHandle, 0, SeekOrigin.End);
            }
        }

        /// <summary>
        /// Verify that the actual position of the OS's handle equals what we expect it to.
        /// This will fail if someone else moved the UnixFileStream's handle or if
        /// our position updating code is incorrect.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void VerifyOSHandlePosition()
        {
            bool verifyPosition = _exposedHandle; // in release, only verify if we've given out the handle such that someone else could be manipulating it
#if DEBUG
            verifyPosition = true; // in debug, always make sure our position matches what the OS says it should be
#endif
            if (verifyPosition && CanSeek)
            {
                long oldPos = _filePosition; // SeekCore will override the current _position, so save it now
                long curPos = SeekCore(_fileHandle, 0, SeekOrigin.Current);
                if (oldPos != curPos)
                {
                    // For reads, this is non-fatal but we still could have returned corrupted
                    // data in some cases, so discard the internal buffer. For writes,
                    // this is a problem; discard the buffer and error out.

                    throw new IOException(SR.IO_FileStreamHandlePosition);
                }
            }
        }
    }
}
