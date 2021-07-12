// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    // this type serves some basic functionality that is common for native OS File Stream Strategies
    internal abstract class OSFileStreamStrategy : FileStreamStrategy
    {
        protected readonly SafeFileHandle _fileHandle; // only ever null if ctor throws
        private readonly FileAccess _access; // What file was opened for.
        private readonly FileShare _share;

        protected long _filePosition;
        private long _appendStart; // When appending, prevent overwriting file.
        private long _length = -1; // When the file is locked for writes on Windows (_share <= FileShare.Read) cache file length in-memory, negative means that hasn't been fetched.
        private bool _exposedHandle; // created from handle, or SafeFileHandle was used and the handle got exposed

        internal OSFileStreamStrategy(SafeFileHandle handle, FileAccess access, FileShare share)
        {
            _access = access;
            _share = share;
            _exposedHandle = true;

            handle.EnsureThreadPoolBindingInitialized();

            if (handle.CanSeek)
            {
                // given strategy was created out of existing handle, so we have to perform
                // a syscall to get the current handle offset
                _filePosition = FileStreamHelpers.Seek(handle, 0, SeekOrigin.Current);
            }
            else
            {
                _filePosition = 0;
            }

            _fileHandle = handle;
        }

        internal OSFileStreamStrategy(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize)
        {
            string fullPath = Path.GetFullPath(path);

            _access = access;
            _share = share;

            _fileHandle = SafeFileHandle.Open(fullPath, mode, access, share, options, preallocationSize);

            try
            {
                if (mode == FileMode.Append && CanSeek)
                {
                    _appendStart = _filePosition = Length;
                }
                else
                {
                    _appendStart = -1;
                }
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

        public sealed override bool CanSeek => _fileHandle.CanSeek;

        public sealed override bool CanRead => !_fileHandle.IsClosed && (_access & FileAccess.Read) != 0;

        public sealed override bool CanWrite => !_fileHandle.IsClosed && (_access & FileAccess.Write) != 0;

        public unsafe sealed override long Length
        {
            get
            {
                if (!LengthCachingSupported)
                {
                    return RandomAccess.GetFileLength(_fileHandle);
                }

                // On Windows, when the file is locked for writes we can cache file length
                // in memory and avoid subsequent native calls which are expensive.

                if (_length < 0)
                {
                    _length = RandomAccess.GetFileLength(_fileHandle);
                }

                return _length;
            }
        }

        protected void UpdateLengthOnChangePosition()
        {
            // Do not update the cached length if the file is not locked
            // or if the length hasn't been fetched.
            if (!LengthCachingSupported || _length < 0)
            {
                Debug.Assert(_length < 0);
                return;
            }

            if (_filePosition > _length)
            {
                _length = _filePosition;
            }
        }

        private bool LengthCachingSupported => OperatingSystem.IsWindows() && _share <= FileShare.Read && !_exposedHandle;

        /// <summary>Gets or sets the position within the current stream</summary>
        public sealed override long Position
        {
            get => _filePosition;
            set => _filePosition = value;
        }

        internal sealed override string Name => _fileHandle.Path ?? SR.IO_UnknownFileName;

        internal sealed override bool IsClosed => _fileHandle.IsClosed;

        // Flushing is the responsibility of BufferedFileStreamStrategy
        internal sealed override SafeFileHandle SafeFileHandle
        {
            get
            {
                if (CanSeek)
                {
                    // Update the file offset before exposing it since it's possible that
                    // in memory position is out-of-sync with the actual file position.
                    FileStreamHelpers.Seek(_fileHandle, _filePosition, SeekOrigin.Begin);
                }

                _exposedHandle = true;
                _length = -1; // invalidate cached length

                return _fileHandle;
            }
        }

        // this method just disposes everything (no buffer, no need to flush)
        public sealed override ValueTask DisposeAsync()
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
        protected sealed override void Dispose(bool disposing)
        {
            if (disposing && _fileHandle != null && !_fileHandle.IsClosed)
            {
                _fileHandle.ThreadPoolBinding?.Dispose();
                _fileHandle.Dispose();
            }
        }

        public sealed override void Flush() { }  // no buffering = nothing to flush

        public sealed override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask; // no buffering = nothing to flush

        internal sealed override void Flush(bool flushToDisk)
        {
            if (flushToDisk && CanWrite)
            {
                FileStreamHelpers.FlushToDisk(_fileHandle);
            }
        }

        public sealed override long Seek(long offset, SeekOrigin origin)
        {
            if (origin < SeekOrigin.Begin || origin > SeekOrigin.End)
                throw new ArgumentException(SR.Argument_InvalidSeekOrigin, nameof(origin));
            if (_fileHandle.IsClosed) ThrowHelper.ThrowObjectDisposedException_FileClosed();
            if (!CanSeek) ThrowHelper.ThrowNotSupportedException_UnseekableStream();

            long oldPos = _filePosition;
            long pos = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.End => Length + offset,
                _ => _filePosition + offset // SeekOrigin.Current
            };

            if (pos >= 0)
            {
                _filePosition = pos;
            }
            else
            {
                // keep throwing the same exception we did when seek was causing actual offset change
                FileStreamHelpers.ThrowInvalidArgument(_fileHandle);
            }

            // Prevent users from overwriting data in a file that was opened in append mode.
            if (_appendStart != -1 && pos < _appendStart)
            {
                _filePosition = oldPos;
                throw new IOException(SR.IO_SeekAppendOverwrite);
            }

            return pos;
        }

        internal sealed override void Lock(long position, long length) => FileStreamHelpers.Lock(_fileHandle, CanWrite, position, length);

        internal sealed override void Unlock(long position, long length) => FileStreamHelpers.Unlock(_fileHandle, position, length);

        public sealed override void SetLength(long value)
        {
            if (_appendStart != -1 && value < _appendStart)
                throw new IOException(SR.IO_SetLengthAppendTruncate);

            SetLengthCore(value);
        }

        protected unsafe void SetLengthCore(long value)
        {
            Debug.Assert(value >= 0, "value >= 0");

            FileStreamHelpers.SetFileLength(_fileHandle, value);
            if (LengthCachingSupported)
            {
                _length = value;
            }

            if (_filePosition > value)
            {
                _filePosition = value;
            }
        }

        public sealed override unsafe int ReadByte()
        {
            byte b;
            return Read(new Span<byte>(&b, 1)) != 0 ? b : -1;
        }

        public sealed override int Read(byte[] buffer, int offset, int count) =>
            Read(new Span<byte>(buffer, offset, count));

        public sealed override int Read(Span<byte> buffer)
        {
            if (_fileHandle.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }
            else if ((_access & FileAccess.Read) == 0)
            {
                ThrowHelper.ThrowNotSupportedException_UnreadableStream();
            }

            int r = RandomAccess.ReadAtOffset(_fileHandle, buffer, _filePosition);
            Debug.Assert(r >= 0, $"RandomAccess.ReadAtOffset returned {r}.");
            _filePosition += r;

            return r;
        }

        public sealed override unsafe void WriteByte(byte value) =>
            Write(new ReadOnlySpan<byte>(&value, 1));

        public override void Write(byte[] buffer, int offset, int count) =>
            Write(new ReadOnlySpan<byte>(buffer, offset, count));

        public sealed override void Write(ReadOnlySpan<byte> buffer)
        {
            if (_fileHandle.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }
            else if ((_access & FileAccess.Write) == 0)
            {
                ThrowHelper.ThrowNotSupportedException_UnwritableStream();
            }

            int r = RandomAccess.WriteAtOffset(_fileHandle, buffer, _filePosition);
            Debug.Assert(r >= 0, $"RandomAccess.WriteAtOffset returned {r}.");
            _filePosition += r;

            UpdateLengthOnChangePosition();
        }
    }
}
