// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public partial class FileStream : Stream
    {
        private readonly LockableStream _actualImplementation;

        private const FileShare DefaultShare = FileShare.Read;
        private const bool DefaultIsAsync = false;
        internal const int DefaultBufferSize = 4096;

        private byte[]? _buffer;
        private int _bufferLength;
        private readonly SafeFileHandle _fileHandle; // only ever null if ctor throws

        /// <summary>Whether the file is opened for reading, writing, or both.</summary>
        private readonly FileAccess _access;

        /// <summary>The path to the opened file.</summary>
        private readonly string? _path;

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

        /// <summary>Caches whether Serialization Guard has been disabled for file writes</summary>
        private static int s_cachedSerializationSwitch;

        [Obsolete("This constructor has been deprecated.  Please use new FileStream(SafeFileHandle handle, FileAccess access) instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public FileStream(IntPtr handle, FileAccess access)
            : this(handle, access, true, DefaultBufferSize, false)
        {
        }

        [Obsolete("This constructor has been deprecated.  Please use new FileStream(SafeFileHandle handle, FileAccess access) instead, and optionally make a new SafeFileHandle with ownsHandle=false if needed.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public FileStream(IntPtr handle, FileAccess access, bool ownsHandle)
            : this(handle, access, ownsHandle, DefaultBufferSize, false)
        {
        }

        [Obsolete("This constructor has been deprecated.  Please use new FileStream(SafeFileHandle handle, FileAccess access, int bufferSize) instead, and optionally make a new SafeFileHandle with ownsHandle=false if needed.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public FileStream(IntPtr handle, FileAccess access, bool ownsHandle, int bufferSize)
            : this(handle, access, ownsHandle, bufferSize, false)
        {
        }

        [Obsolete("This constructor has been deprecated.  Please use new FileStream(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync) instead, and optionally make a new SafeFileHandle with ownsHandle=false if needed.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public FileStream(IntPtr handle, FileAccess access, bool ownsHandle, int bufferSize, bool isAsync)
        {
            _actualImplementation = new FileStreamImpl(handle, access, ownsHandle, bufferSize, isAsync);
        }

        public FileStream(SafeFileHandle handle, FileAccess access)
            : this(handle, access, DefaultBufferSize)
        {
        }

        public FileStream(SafeFileHandle handle, FileAccess access, int bufferSize)
            : this(handle, access, bufferSize, GetDefaultIsAsync(handle))
        {
        }

        public FileStream(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
        {
            _actualImplementation = new FileStreamImpl(handle, access, bufferSize, isAsync);
        }

        public FileStream(string path, FileMode mode) :
            this(path, mode, mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite, DefaultShare, DefaultBufferSize, DefaultIsAsync)
        { }

        public FileStream(string path, FileMode mode, FileAccess access) :
            this(path, mode, access, DefaultShare, DefaultBufferSize, DefaultIsAsync)
        { }

        public FileStream(string path, FileMode mode, FileAccess access, FileShare share) :
            this(path, mode, access, share, DefaultBufferSize, DefaultIsAsync)
        { }

        public FileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize) :
            this(path, mode, access, share, bufferSize, DefaultIsAsync)
        { }

        public FileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync) :
            this(path, mode, access, share, bufferSize, useAsync ? FileOptions.Asynchronous : FileOptions.None)
        { }

        public FileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
        {
            _actualImplementation = new FileStreamImpl(path, mode, access, share, bufferSize, options);
        }

        [Obsolete("This property has been deprecated.  Please use FileStream's SafeFileHandle property instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual IntPtr Handle => _actualImplementation.Handle;

        public virtual void Lock(long position, long length) => _actualImplementation.Lock(position, length);

        public virtual void Unlock(long position, long length) => _actualImplementation.Unlock(position, length);

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Flush() which a subclass might have overridden.  To be safe
            // we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Flush) when we are not sure.
            if (GetType() != typeof(FileStream))
                return base.FlushAsync(cancellationToken);

            return _actualImplementation.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateReadWriteArgs(buffer, offset, count);

            return _actualImplementation.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            if (GetType() == typeof(FileStream) && !_actualImplementation.IsAsync)
            {
                return _actualImplementation.Read(buffer);
            }
            else
            {
                // This type is derived from FileStream and/or the stream is in async mode.  If this is a
                // derived type, it may have overridden Read(byte[], int, int) prior to this Read(Span<byte>)
                // overload being introduced.  In that case, this Read(Span<byte>) overload should use the behavior
                // of Read(byte[],int,int) overload.  Or if the stream is in async mode, we can't call the
                // synchronous ReadSpan, so we similarly call the base Read, which will turn delegate to
                // Read(byte[],int,int), which will do the right thing if we're in async mode.
                return base.Read(buffer);
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);

            if (GetType() != typeof(FileStream))
            {
                // If we have been inherited into a subclass, the following implementation could be incorrect
                // since it does not call through to Read() which a subclass might have overridden.
                // To be safe we will only use this implementation in cases where we know it is safe to do so,
                // and delegate to our base class (which will call into Read/ReadAsync) when we are not sure.
                return base.ReadAsync(buffer, offset, count, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<int>(cancellationToken);

            if (_actualImplementation.IsClosed)
                throw Error.GetFileNotOpen();

            return _actualImplementation.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (GetType() != typeof(FileStream))
            {
                // If this isn't a concrete FileStream, a derived type may have overridden ReadAsync(byte[],...),
                // which was introduced first, so delegate to the base which will delegate to that.
                return base.ReadAsync(buffer, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            if (_actualImplementation.IsClosed)
            {
                throw Error.GetFileNotOpen();
            }

            return _actualImplementation.ReadAsync(buffer, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateReadWriteArgs(buffer, offset, count);

            _actualImplementation.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (GetType() == typeof(FileStream) && !_actualImplementation.IsAsync)
            {
                if (_actualImplementation.IsClosed)
                {
                    throw Error.GetFileNotOpen();
                }

                _actualImplementation.Write(buffer);
            }
            else
            {
                // This type is derived from FileStream and/or the stream is in async mode.  If this is a
                // derived type, it may have overridden Write(byte[], int, int) prior to this Write(ReadOnlySpan<byte>)
                // overload being introduced.  In that case, this Write(ReadOnlySpan<byte>) overload should use the behavior
                // of Write(byte[],int,int) overload.  Or if the stream is in async mode, we can't call the
                // synchronous WriteSpan, so we similarly call the base Write, which will turn delegate to
                // Write(byte[],int,int), which will do the right thing if we're in async mode.
                base.Write(buffer);
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);

            if (GetType() != typeof(FileStream))
            {
                // If we have been inherited into a subclass, the following implementation could be incorrect
                // since it does not call through to Write() or WriteAsync() which a subclass might have overridden.
                // To be safe we will only use this implementation in cases where we know it is safe to do so,
                // and delegate to our base class (which will call into Write/WriteAsync) when we are not sure.
                return base.WriteAsync(buffer, offset, count, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            if (_actualImplementation.IsClosed)
                throw Error.GetFileNotOpen();

            return _actualImplementation.WriteAsync(buffer, offset, count);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (GetType() != typeof(FileStream))
            {
                // If this isn't a concrete FileStream, a derived type may have overridden WriteAsync(byte[],...),
                // which was introduced first, so delegate to the base which will delegate to that.
                return base.WriteAsync(buffer, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(cancellationToken);
            }

            if (_actualImplementation.IsClosed)
            {
                throw Error.GetFileNotOpen();
            }

            return _actualImplementation.WriteAsync(buffer, cancellationToken);
        }

        /// <summary>
        /// Clears buffers for this stream and causes any buffered data to be written to the file.
        /// </summary>
        public override void Flush() => _actualImplementation.Flush();

        /// <summary>
        /// Clears buffers for this stream, and if <param name="flushToDisk"/> is true,
        /// causes any buffered data to be written to the file.
        /// </summary>
        public virtual void Flush(bool flushToDisk)
        {
            if (_actualImplementation.IsClosed) throw Error.GetFileNotOpen();

            _actualImplementation.Flush(flushToDisk);
        }

        /// <summary>Gets a value indicating whether the current stream supports reading.</summary>
        public override bool CanRead => _actualImplementation.CanRead;

        /// <summary>Gets a value indicating whether the current stream supports writing.</summary>
        public override bool CanWrite => _actualImplementation.CanWrite;

        /// <summary>Validates arguments to Read and Write and throws resulting exceptions.</summary>
        /// <param name="buffer">The buffer to read from or write to.</param>
        /// <param name="offset">The zero-based offset into the buffer.</param>
        /// <param name="count">The maximum number of bytes to read or write.</param>
        private void ValidateReadWriteArgs(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            if (_actualImplementation.IsClosed)
                throw Error.GetFileNotOpen();
        }

        /// <summary>Sets the length of this stream to the given value.</summary>
        /// <param name="value">The new length of the stream.</param>
        public override void SetLength(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (_actualImplementation.IsClosed)
                throw Error.GetFileNotOpen();
            if (!_actualImplementation.CanSeek)
                throw Error.GetSeekNotSupported();
            if (!_actualImplementation.CanWrite)
                throw Error.GetWriteNotSupported();

            _actualImplementation.SetLength(value);
        }

        public virtual SafeFileHandle SafeFileHandle => _actualImplementation.SafeFileHandle;

        /// <summary>Gets the path that was passed to the constructor.</summary>
        public virtual string Name => _actualImplementation.Name;

        /// <summary>Gets a value indicating whether the stream was opened for I/O to be performed synchronously or asynchronously.</summary>
        public virtual bool IsAsync => _actualImplementation.IsAsync;

        /// <summary>Gets the length of the stream in bytes.</summary>
        public override long Length
        {
            get
            {
                if (_actualImplementation.IsClosed) throw Error.GetFileNotOpen();
                if (!_actualImplementation.CanSeek) throw Error.GetSeekNotSupported();
                return _actualImplementation.Length;
            }
        }

        /// <summary>Validates that we're ready to read from the stream.</summary>
        private void PrepareForReading()
        {
            if (_fileHandle.IsClosed)
                throw Error.GetFileNotOpen();
            if (_readLength == 0 && !CanRead)
                throw Error.GetReadNotSupported();

            AssertBufferInvariants();
        }

        /// <summary>Gets or sets the position within the current stream</summary>
        public override long Position
        {
            get
            {
                if (_actualImplementation.IsClosed)
                    throw Error.GetFileNotOpen();

                if (!_actualImplementation.CanSeek)
                    throw Error.GetSeekNotSupported();

                return _actualImplementation.Positon;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_NeedNonNegNum);

                _actualImplementation.Seek(value, SeekOrigin.Begin);
            }
        }

        internal virtual bool IsClosed => _fileHandle.IsClosed;

        private static bool IsIoRelatedException(Exception e) =>
            // These all derive from IOException
            //     DirectoryNotFoundException
            //     DriveNotFoundException
            //     EndOfStreamException
            //     FileLoadException
            //     FileNotFoundException
            //     PathTooLongException
            //     PipeException
            e is IOException ||
            // Note that SecurityException is only thrown on runtimes that support CAS
            // e is SecurityException ||
            e is UnauthorizedAccessException ||
            e is NotSupportedException ||
            (e is ArgumentException && !(e is ArgumentNullException));

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

        partial void OnBufferAllocated();

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
                throw Error.GetFileNotOpen();

            // Make sure we're good to write.  We only need to do this if there's nothing already
            // in our write buffer, since if there is something in the buffer, we've already done
            // this checking and flushing.
            if (_writePos == 0)
            {
                if (!CanWrite) throw Error.GetWriteNotSupported();
                FlushReadBuffer();
                Debug.Assert(_bufferLength > 0, "_bufferSize > 0");
            }
        }

        ~FileStream()
        {
            // Preserved for compatibility since FileStream has defined a
            // finalizer in past releases and derived classes may depend
            // on Dispose(false) call.
            Dispose(false);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            ValidateBufferArguments(buffer, offset, count);
            if (IsClosed) throw new ObjectDisposedException(SR.ObjectDisposed_FileClosed);
            if (!CanRead) throw new NotSupportedException(SR.NotSupported_UnreadableStream);

            if (!IsAsync)
                return base.BeginRead(buffer, offset, count, callback, state);
            else
                return TaskToApm.Begin(ReadAsyncTask(buffer, offset, count, CancellationToken.None), callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            ValidateBufferArguments(buffer, offset, count);
            if (IsClosed) throw new ObjectDisposedException(SR.ObjectDisposed_FileClosed);
            if (!CanWrite) throw new NotSupportedException(SR.NotSupported_UnwritableStream);

            if (!IsAsync)
                return base.BeginWrite(buffer, offset, count, callback, state);
            else
                return TaskToApm.Begin(WriteAsyncInternal(new ReadOnlyMemory<byte>(buffer, offset, count), CancellationToken.None).AsTask(), callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult));

            if (!IsAsync)
                return base.EndRead(asyncResult);
            else
                return TaskToApm.End<int>(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult));

            if (!IsAsync)
                base.EndWrite(asyncResult);
            else
                TaskToApm.End(asyncResult);
        }
    }
}
