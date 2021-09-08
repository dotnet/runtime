// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    // This type is partial so we can avoid code duplication between Windows and Unix Net5Compat implementations
    internal sealed partial class Net5CompatFileStreamStrategy : FileStreamStrategy
    {
        private byte[]? _buffer;
        private readonly int _bufferLength;
        private readonly SafeFileHandle _fileHandle; // only ever null if ctor throws

        /// <summary>Whether the file is opened for reading, writing, or both.</summary>
        private readonly FileAccess _access;

        /// <summary>The next available byte to be read from the _buffer.</summary>
        private int _readPos;

        /// <summary>The number of valid bytes in _buffer.</summary>
        private int _readLength;

        /// <summary>The next location in which a write should occur to the buffer.</summary>
        private int _writePos;

        /// <summary>
        /// Whether asynchronous read/write/flush operations should be performed using async I/O.
        /// On Windows FileOptions.Asynchronous controls how the file handle is configured,
        /// and then as a result how operations are issued against that file handle.  On Unix,
        /// there isn't any distinction around how file descriptors are created for async vs
        /// sync, but we still differentiate how the operations are issued in order to provide
        /// similar behavioral semantics and performance characteristics as on Windows.  On
        /// Windows, if non-async, async read/write requests just delegate to the base stream,
        /// and no attempt is made to synchronize between sync and async operations on the stream;
        /// if async, then async read/write requests are implemented specially, and sync read/write
        /// requests are coordinated with async ones by implementing the sync ones over the async
        /// ones.  On Unix, we do something similar.  If non-async, async read/write requests just
        /// delegate to the base stream, and no attempt is made to synchronize.  If async, we use
        /// a semaphore to coordinate both sync and async operations.
        /// </summary>
        private readonly bool _useAsyncIO;

        /// <summary>cached task for read ops that complete synchronously</summary>
        private Task<int>? _lastSynchronouslyCompletedTask;

        /// <summary>
        /// Currently cached position in the stream.  This should always mirror the underlying file's actual position,
        /// and should only ever be out of sync if another stream with access to this same file manipulates it, at which
        /// point we attempt to error out.
        /// </summary>
        private long _filePosition;

        /// <summary>Whether the file stream's handle has been exposed.</summary>
        private bool _exposedHandle;

        internal Net5CompatFileStreamStrategy(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
        {
            _exposedHandle = true;
            _bufferLength = bufferSize;

            InitFromHandle(handle, access, isAsync);

            // Note: It would be cleaner to set the following fields in ValidateHandle,
            // but we can't as they're readonly.
            _access = access;
            _useAsyncIO = isAsync;

            // As the handle was passed in, we must set the handle field at the very end to
            // avoid the finalizer closing the handle when we throw errors.
            _fileHandle = handle;
        }

        internal Net5CompatFileStreamStrategy(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, long preallocationSize)
        {
            string fullPath = Path.GetFullPath(path);

            _access = access;
            _bufferLength = bufferSize;

            if ((options & FileOptions.Asynchronous) != 0)
                _useAsyncIO = true;

            _fileHandle = SafeFileHandle.Open(fullPath, mode, access, share, options, preallocationSize);

            try
            {
                Init(mode, path, options);
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

        ~Net5CompatFileStreamStrategy() => Dispose(false); // mandatory to Flush the write buffer

        internal override void DisposeInternal(bool disposing) => Dispose(disposing);

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            // TODO: https://github.com/dotnet/runtime/issues/27643 (stop doing this synchronous work!!).
            // The always synchronous data transfer between the OS and the internal buffer is intentional
            // because this is needed to allow concurrent async IO requests. Concurrent data transfer
            // between the OS and the internal buffer will result in race conditions. Since FlushWrite and
            // FlushRead modify internal state of the stream and transfer data between the OS and the
            // internal buffer, they cannot be truly async. We will, however, flush the OS file buffers
            // asynchronously because it doesn't modify any internal state of the stream and is potentially
            // a long running process.
            try
            {
                FlushInternalBuffer();
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }

            return Task.CompletedTask;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _useAsyncIO ?
                ReadAsyncTask(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult() :
                ReadSpan(new Span<byte>(buffer, offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            if (!_useAsyncIO)
            {
                if (_fileHandle.IsClosed)
                {
                    ThrowHelper.ThrowObjectDisposedException_FileClosed();
                }

                return ReadSpan(buffer);
            }

            // If the stream is in async mode, we can't call the synchronous ReadSpan, so we similarly call the base Read,
            // which will turn delegate to Read(byte[],int,int), which will do the right thing if we're in async mode.
            return base.Read(buffer);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!_useAsyncIO)
            {
                // If we weren't opened for asynchronous I/O, we still call to the base implementation so that
                // Read is invoked asynchronously.  But we can do so using the base Stream's internal helper
                // that bypasses delegating to BeginRead, since we already know this is FileStream rather
                // than something derived from it and what our BeginRead implementation is going to do.
                return BeginReadInternal(buffer, offset, count, null, null, serializeAsynchronously: true, apm: false);
            }

            return ReadAsyncTask(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_useAsyncIO)
            {
                // If we weren't opened for asynchronous I/O, we still call to the base implementation so that
                // Read is invoked asynchronously.  But if we have a byte[], we can do so using the base Stream's
                // internal helper that bypasses delegating to BeginRead, since we already know this is FileStream
                // rather than something derived from it and what our BeginRead implementation is going to do.
                return MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment) ?
                    new ValueTask<int>(BeginReadInternal(segment.Array!, segment.Offset, segment.Count, null, null, serializeAsynchronously: true, apm: false)) :
                    base.ReadAsync(buffer, cancellationToken);
            }

            Task<int>? t = ReadAsyncInternal(buffer, cancellationToken, out int synchronousResult);
            return t != null ?
                new ValueTask<int>(t) :
                new ValueTask<int>(synchronousResult);
        }

        private Task<int> ReadAsyncTask(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Task<int>? t = ReadAsyncInternal(new Memory<byte>(buffer, offset, count), cancellationToken, out int synchronousResult);

            if (t == null)
            {
                t = _lastSynchronouslyCompletedTask;
                Debug.Assert(t == null || t.IsCompletedSuccessfully, "Cached task should have completed successfully");

                if (t == null || t.Result != synchronousResult)
                {
                    _lastSynchronouslyCompletedTask = t = Task.FromResult(synchronousResult);
                }
            }

            return t;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_useAsyncIO)
            {
                WriteAsyncInternal(new ReadOnlyMemory<byte>(buffer, offset, count), CancellationToken.None).AsTask().GetAwaiter().GetResult();
            }
            else
            {
                WriteSpan(new ReadOnlySpan<byte>(buffer, offset, count));
            }
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (!_useAsyncIO)
            {
                if (_fileHandle.IsClosed)
                {
                    ThrowHelper.ThrowObjectDisposedException_FileClosed();
                }

                WriteSpan(buffer);
            }
            else
            {
                // If the stream is in async mode, we can't call the synchronous WriteSpan, so we similarly call the base Write,
                // which will turn delegate to Write(byte[],int,int), which will do the right thing if we're in async mode.
                base.Write(buffer);
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!_useAsyncIO)
            {
                // If we weren't opened for asynchronous I/O, we still call to the base implementation so that
                // Write is invoked asynchronously.  But we can do so using the base Stream's internal helper
                // that bypasses delegating to BeginWrite, since we already know this is FileStream rather
                // than something derived from it and what our BeginWrite implementation is going to do.
                return BeginWriteInternal(buffer, offset, count, null, null, serializeAsynchronously: true, apm: false);
            }

            return WriteAsyncInternal(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_useAsyncIO)
            {
                // If we weren't opened for asynchronous I/O, we still call to the base implementation so that
                // Write is invoked asynchronously.  But if we have a byte[], we can do so using the base Stream's
                // internal helper that bypasses delegating to BeginWrite, since we already know this is FileStream
                // rather than something derived from it and what our BeginWrite implementation is going to do.
                return MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment) ?
                    new ValueTask(BeginWriteInternal(segment.Array!, segment.Offset, segment.Count, null, null, serializeAsynchronously: true, apm: false)) :
                    base.WriteAsync(buffer, cancellationToken);
            }

            return WriteAsyncInternal(buffer, cancellationToken);
        }

        public override void Flush() => Flush(flushToDisk: false);

        internal override void Flush(bool flushToDisk)
        {
            FlushInternalBuffer();

            if (flushToDisk && CanWrite)
            {
                FileStreamHelpers.FlushToDisk(_fileHandle);
            }
        }

        public override bool CanRead => !_fileHandle.IsClosed && (_access & FileAccess.Read) != 0;

        public override bool CanWrite => !_fileHandle.IsClosed && (_access & FileAccess.Write) != 0;

        internal override SafeFileHandle SafeFileHandle
        {
            get
            {
                Flush();
                _exposedHandle = true;
                return _fileHandle;
            }
        }

        internal override string Name => _fileHandle.Path ?? SR.IO_UnknownFileName;

        internal override bool IsAsync => _useAsyncIO;

        /// <summary>
        /// Verify that the actual position of the OS's handle equals what we expect it to.
        /// This will fail if someone else moved the UnixFileStream's handle or if
        /// our position updating code is incorrect.
        /// </summary>
        private void VerifyOSHandlePosition()
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
                    _readPos = _readLength = 0;
                    if (_writePos > 0)
                    {
                        _writePos = 0;
                        throw new IOException(SR.IO_FileStreamHandlePosition);
                    }
                }
            }
        }

        /// <summary>Verifies that state relating to the read/write buffer is consistent.</summary>
        [Conditional("DEBUG")]
        private void AssertBufferInvariants()
        {
            // Read buffer values must be in range: 0 <= _bufferReadPos <= _bufferReadLength <= _bufferLength
            Debug.Assert(0 <= _readPos && _readPos <= _readLength && _readLength <= _bufferLength);

            // Write buffer values must be in range: 0 <= _bufferWritePos <= _bufferLength
            Debug.Assert(0 <= _writePos && _writePos <= _bufferLength);

            // Read buffering and write buffering can't both be active
            Debug.Assert((_readPos == 0 && _readLength == 0) || _writePos == 0);
        }

        /// <summary>Validates that we're ready to read from the stream.</summary>
        private void PrepareForReading()
        {
            if (_fileHandle.IsClosed)
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            if (_readLength == 0 && !CanRead)
                ThrowHelper.ThrowNotSupportedException_UnreadableStream();

            AssertBufferInvariants();
        }

        /// <summary>Gets or sets the position within the current stream</summary>
        public override long Position
        {
            get
            {
                AssertBufferInvariants();
                VerifyOSHandlePosition();

                // We may have read data into our buffer from the handle, such that the handle position
                // is artificially further along than the consumer's view of the stream's position.
                // Thus, when reading, our position is really starting from the handle position negatively
                // offset by the number of bytes in the buffer and positively offset by the number of
                // bytes into that buffer we've read.  When writing, both the read length and position
                // must be zero, and our position is just the handle position offset positive by how many
                // bytes we've written into the buffer.
                return (_filePosition - _readLength) + _readPos + _writePos;
            }
            set
            {
                Seek(value, SeekOrigin.Begin);
            }
        }

        // This doesn't do argument checking.  Necessary for SetLength, which must
        // set the file pointer beyond the end of the file. This will update the
        // internal position
        private long SeekCore(SafeFileHandle fileHandle, long offset, SeekOrigin origin, bool closeInvalidHandle = false)
        {
            Debug.Assert(fileHandle.CanSeek, "fileHandle.CanSeek");

            return _filePosition = FileStreamHelpers.Seek(fileHandle, offset, origin, closeInvalidHandle);
        }

        internal override bool IsClosed => _fileHandle.IsClosed;

        /// <summary>
        /// Gets the array used for buffering reading and writing.
        /// If the array hasn't been allocated, this will lazily allocate it.
        /// </summary>
        /// <returns>The buffer.</returns>
        private byte[] GetBuffer()
        {
            Debug.Assert(_buffer == null || _buffer.Length == _bufferLength);
            if (_buffer == null)
            {
                _buffer = new byte[_bufferLength];
                OnBufferAllocated();
            }

            return _buffer;
        }

        /// <summary>
        /// Flushes the internal read/write buffer for this stream.  If write data has been buffered,
        /// that data is written out to the underlying file.  Or if data has been buffered for
        /// reading from the stream, the data is dumped and our position in the underlying file
        /// is rewound as necessary.  This does not flush the OS buffer.
        /// </summary>
        private void FlushInternalBuffer()
        {
            AssertBufferInvariants();
            if (_writePos > 0)
            {
                FlushWriteBuffer();
            }
            else if (_readPos < _readLength && CanSeek)
            {
                FlushReadBuffer();
            }
        }

        /// <summary>Dumps any read data in the buffer and rewinds our position in the stream, accordingly, as necessary.</summary>
        private void FlushReadBuffer()
        {
            // Reading is done by blocks from the file, but someone could read
            // 1 byte from the buffer then write.  At that point, the OS's file
            // pointer is out of sync with the stream's position.  All write
            // functions should call this function to preserve the position in the file.

            AssertBufferInvariants();
            Debug.Assert(_writePos == 0, "FileStream: Write buffer must be empty in FlushReadBuffer!");

            int rewind = _readPos - _readLength;
            if (rewind != 0)
            {
                Debug.Assert(CanSeek, "FileStream will lose buffered read data now.");
                SeekCore(_fileHandle, rewind, SeekOrigin.Current);
            }
            _readPos = _readLength = 0;
        }

        /// <summary>
        /// Reads a byte from the file stream.  Returns the byte cast to an int
        /// or -1 if reading from the end of the stream.
        /// </summary>
        public override int ReadByte()
        {
            PrepareForReading();

            byte[] buffer = GetBuffer();
            if (_readPos == _readLength)
            {
                FlushWriteBuffer();
                _readLength = FillReadBufferForReadByte();
                _readPos = 0;
                if (_readLength == 0)
                {
                    return -1;
                }
            }

            return buffer[_readPos++];
        }

        /// <summary>
        /// Writes a byte to the current position in the stream and advances the position
        /// within the stream by one byte.
        /// </summary>
        /// <param name="value">The byte to write to the stream.</param>
        public override void WriteByte(byte value)
        {
            PrepareForWriting();

            // Flush the write buffer if it's full
            if (_writePos == _bufferLength)
                FlushWriteBufferForWriteByte();

            // We now have space in the buffer. Store the byte.
            GetBuffer()[_writePos++] = value;
        }

        /// <summary>
        /// Validates that we're ready to write to the stream,
        /// including flushing a read buffer if necessary.
        /// </summary>
        private void PrepareForWriting()
        {
            if (_fileHandle.IsClosed)
                ThrowHelper.ThrowObjectDisposedException_FileClosed();

            // Make sure we're good to write.  We only need to do this if there's nothing already
            // in our write buffer, since if there is something in the buffer, we've already done
            // this checking and flushing.
            if (_writePos == 0)
            {
                if (!CanWrite) ThrowHelper.ThrowNotSupportedException_UnwritableStream();
                FlushReadBuffer();
                Debug.Assert(_bufferLength > 0, "_bufferSize > 0");
            }
        }

        partial void OnBufferAllocated();

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            if (!_useAsyncIO)
                return base.BeginRead(buffer, offset, count, callback, state);
            else
                return TaskToApm.Begin(ReadAsyncTask(buffer, offset, count, CancellationToken.None), callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            if (!_useAsyncIO)
                return base.BeginWrite(buffer, offset, count, callback, state);
            else
                return TaskToApm.Begin(WriteAsyncInternal(new ReadOnlyMemory<byte>(buffer, offset, count), CancellationToken.None).AsTask(), callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (!_useAsyncIO)
                return base.EndRead(asyncResult);
            else
                return TaskToApm.End<int>(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (!_useAsyncIO)
                base.EndWrite(asyncResult);
            else
                TaskToApm.End(asyncResult);
        }
    }
}
