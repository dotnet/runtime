// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public class FileStream : Stream
    {
        internal const int DefaultBufferSize = 4096;
        private const FileShare DefaultShare = FileShare.Read;
        private const bool DefaultIsAsync = false;

        /// <summary>Caches whether Serialization Guard has been disabled for file writes</summary>
        private static int s_cachedSerializationSwitch;

        private readonly FileStreamStrategy _impl;

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
            SafeFileHandle safeHandle = new SafeFileHandle(handle, ownsHandle: ownsHandle);
            try
            {
                ValidateHandle(safeHandle, access, bufferSize, isAsync);

                // it might seem to have no sense now, but we plan to introduce dedicated strategies for sync and async implementations
                switch (isAsync)
                {
                    case true:
                        _impl = new FileStreamImpl(safeHandle, access, bufferSize, true);
                        return;
                    case false:
                        _impl = new FileStreamImpl(safeHandle, access, bufferSize, false);
                        return;
                }
            }
            catch
            {
                // We don't want to take ownership of closing passed in handles
                // *unless* the constructor completes successfully.
                GC.SuppressFinalize(safeHandle);

                // This would also prevent Close from being called, but is unnecessary
                // as we've removed the object from the finalizer queue.
                //
                // safeHandle.SetHandleAsInvalid();
                throw;
            }
        }

        private static void ValidateHandle(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
        {
            if (handle.IsInvalid)
                throw new ArgumentException(SR.Arg_InvalidHandle, nameof(handle));

            if (access < FileAccess.Read || access > FileAccess.ReadWrite)
                throw new ArgumentOutOfRangeException(nameof(access), SR.ArgumentOutOfRange_Enum);
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedPosNum);

            if (handle.IsClosed)
                throw new ObjectDisposedException(SR.ObjectDisposed_FileClosed);
            if (handle.IsAsync.HasValue && isAsync != handle.IsAsync.GetValueOrDefault())
                throw new ArgumentException(SR.Arg_HandleNotAsync, nameof(handle));
        }

        public FileStream(SafeFileHandle handle, FileAccess access)
            : this(handle, access, DefaultBufferSize)
        {
        }

        public FileStream(SafeFileHandle handle, FileAccess access, int bufferSize)
            : this(handle, access, bufferSize, FileStreamImpl.GetDefaultIsAsync(handle, DefaultIsAsync))
        {
        }

        public FileStream(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
        {
            ValidateHandle(handle, access, bufferSize, isAsync);

            switch (isAsync)
            {
                case true:
                    _impl = new FileStreamImpl(handle, access, bufferSize, true);
                    return;
                case false:
                    _impl = new FileStreamImpl(handle, access, bufferSize, false);
                    return;
            }
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
            if (path == null)
                throw new ArgumentNullException(nameof(path), SR.ArgumentNull_Path);
            if (path.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));

            // don't include inheritable in our bounds check for share
            FileShare tempshare = share & ~FileShare.Inheritable;
            string? badArg = null;

            if (mode < FileMode.CreateNew || mode > FileMode.Append)
                badArg = nameof(mode);
            else if (access < FileAccess.Read || access > FileAccess.ReadWrite)
                badArg = nameof(access);
            else if (tempshare < FileShare.None || tempshare > (FileShare.ReadWrite | FileShare.Delete))
                badArg = nameof(share);

            if (badArg != null)
                throw new ArgumentOutOfRangeException(badArg, SR.ArgumentOutOfRange_Enum);

            // NOTE: any change to FileOptions enum needs to be matched here in the error validation
            if (options != FileOptions.None && (options & ~(FileOptions.WriteThrough | FileOptions.Asynchronous | FileOptions.RandomAccess | FileOptions.DeleteOnClose | FileOptions.SequentialScan | FileOptions.Encrypted | (FileOptions)0x20000000 /* NoBuffering */)) != 0)
                throw new ArgumentOutOfRangeException(nameof(options), SR.ArgumentOutOfRange_Enum);

            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedPosNum);

            // Write access validation
            if ((access & FileAccess.Write) == 0)
            {
                if (mode == FileMode.Truncate || mode == FileMode.CreateNew || mode == FileMode.Create || mode == FileMode.Append)
                {
                    // No write access, mode and access disagree but flag access since mode comes first
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidFileModeAndAccessCombo, mode, access), nameof(access));
                }
            }

            if ((access & FileAccess.Read) != 0 && mode == FileMode.Append)
                throw new ArgumentException(SR.Argument_InvalidAppendMode, nameof(access));

            if ((access & FileAccess.Write) == FileAccess.Write)
            {
                SerializationInfo.ThrowIfDeserializationInProgress("AllowFileWrites", ref s_cachedSerializationSwitch);
            }

            if ((options & FileOptions.Asynchronous) != 0)
            {
                _impl = new FileStreamImpl(path, mode, access, share, bufferSize, options);
            }
            else
            {
                _impl = new FileStreamImpl(path, mode, access, share, bufferSize, options);
            }
        }

        [Obsolete("This property has been deprecated.  Please use FileStream's SafeFileHandle property instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual IntPtr Handle => _impl.Handle;

        public virtual void Lock(long position, long length)
        {
            if (position < 0 || length < 0)
            {
                throw new ArgumentOutOfRangeException(position < 0 ? nameof(position) : nameof(length), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (_impl.IsClosed)
            {
                throw Error.GetFileNotOpen();
            }

            _impl.Lock(position, length);
        }

        public virtual void Unlock(long position, long length)
        {
            if (position < 0 || length < 0)
            {
                throw new ArgumentOutOfRangeException(position < 0 ? nameof(position) : nameof(length), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (_impl.IsClosed)
            {
                throw Error.GetFileNotOpen();
            }

            _impl.Unlock(position, length);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect
            // since it does not call through to Flush() which a subclass might have overridden.  To be safe
            // we will only use this implementation in cases where we know it is safe to do so,
            // and delegate to our base class (which will call into Flush) when we are not sure.
            if (GetType() != typeof(FileStream))
                return base.FlushAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }
            if (_impl.IsClosed)
            {
                throw Error.GetFileNotOpen();
            }

            return _impl.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateReadWriteArgs(buffer, offset, count);

            return _impl.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            if (GetType() == typeof(FileStream) && !_impl.IsAsync)
            {
                if (_impl.IsClosed)
                {
                    throw Error.GetFileNotOpen();
                }

                return _impl.Read(buffer);
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

            if (_impl.IsClosed)
                throw Error.GetFileNotOpen();

            return _impl.ReadAsync(buffer, offset, count, cancellationToken);
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

            if (_impl.IsClosed)
            {
                throw Error.GetFileNotOpen();
            }

            return _impl.ReadAsync(buffer, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateReadWriteArgs(buffer, offset, count);

            _impl.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (GetType() == typeof(FileStream) && !_impl.IsAsync)
            {
                if (_impl.IsClosed)
                {
                    throw Error.GetFileNotOpen();
                }

                _impl.Write(buffer);
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

            if (_impl.IsClosed)
                throw Error.GetFileNotOpen();

            return _impl.WriteAsync(buffer, offset, count, cancellationToken);
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

            if (_impl.IsClosed)
            {
                throw Error.GetFileNotOpen();
            }

            return _impl.WriteAsync(buffer, cancellationToken);
        }

        /// <summary>
        /// Clears buffers for this stream and causes any buffered data to be written to the file.
        /// </summary>
        public override void Flush() => _impl.Flush();

        /// <summary>
        /// Clears buffers for this stream, and if <param name="flushToDisk"/> is true,
        /// causes any buffered data to be written to the file.
        /// </summary>
        public virtual void Flush(bool flushToDisk)
        {
            if (_impl.IsClosed) throw Error.GetFileNotOpen();

            _impl.Flush(flushToDisk);
        }

        /// <summary>Gets a value indicating whether the current stream supports reading.</summary>
        public override bool CanRead => _impl.CanRead;

        /// <summary>Gets a value indicating whether the current stream supports writing.</summary>
        public override bool CanWrite => _impl.CanWrite;

        /// <summary>Validates arguments to Read and Write and throws resulting exceptions.</summary>
        /// <param name="buffer">The buffer to read from or write to.</param>
        /// <param name="offset">The zero-based offset into the buffer.</param>
        /// <param name="count">The maximum number of bytes to read or write.</param>
        private void ValidateReadWriteArgs(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            if (_impl.IsClosed)
                throw Error.GetFileNotOpen();
        }

        /// <summary>Sets the length of this stream to the given value.</summary>
        /// <param name="value">The new length of the stream.</param>
        public override void SetLength(long value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (_impl.IsClosed)
                throw Error.GetFileNotOpen();
            if (!_impl.CanSeek)
                throw Error.GetSeekNotSupported();
            if (!_impl.CanWrite)
                throw Error.GetWriteNotSupported();

            _impl.SetLength(value);
        }

        public virtual SafeFileHandle SafeFileHandle => _impl.SafeFileHandle;

        /// <summary>Gets the path that was passed to the constructor.</summary>
        public virtual string Name => _impl.Name;

        /// <summary>Gets a value indicating whether the stream was opened for I/O to be performed synchronously or asynchronously.</summary>
        public virtual bool IsAsync => _impl.IsAsync;

        /// <summary>Gets the length of the stream in bytes.</summary>
        public override long Length
        {
            get
            {
                if (_impl.IsClosed) throw Error.GetFileNotOpen();
                if (!_impl.CanSeek) throw Error.GetSeekNotSupported();
                return _impl.Length;
            }
        }

        /// <summary>Gets or sets the position within the current stream</summary>
        public override long Position
        {
            get
            {
                if (_impl.IsClosed)
                    throw Error.GetFileNotOpen();

                if (!_impl.CanSeek)
                    throw Error.GetSeekNotSupported();

                return _impl.Position;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_NeedNonNegNum);

                _impl.Seek(value, SeekOrigin.Begin);
            }
        }

        internal virtual bool IsClosed => _impl.IsClosed;

        /// <summary>
        /// Reads a byte from the file stream.  Returns the byte cast to an int
        /// or -1 if reading from the end of the stream.
        /// </summary>
        public override int ReadByte() => _impl.ReadByte();

        /// <summary>
        /// Writes a byte to the current position in the stream and advances the position
        /// within the stream by one byte.
        /// </summary>
        /// <param name="value">The byte to write to the stream.</param>
        public override void WriteByte(byte value) => _impl.WriteByte(value);

        ~FileStream()
        {
            // Preserved for compatibility since FileStream has defined a
            // finalizer in past releases and derived classes may depend
            // on Dispose(false) call.
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (_impl != null) // possible in finalizer
            {
                _impl.DisposeInternal(disposing);
            }
        }

        public override ValueTask DisposeAsync()
        {
            if (OperatingSystem.IsWindows() && GetType() != typeof(FileStream))
            {
                return base.DisposeAsync();
            }

            return _impl.DisposeAsync();
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => _impl.CopyToAsync(destination, bufferSize, cancellationToken);

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            ValidateBufferArguments(buffer, offset, count);
            if (_impl.IsClosed) throw new ObjectDisposedException(SR.ObjectDisposed_FileClosed);
            if (!_impl.CanRead) throw new NotSupportedException(SR.NotSupported_UnreadableStream);

            if (!_impl.IsAsync)
                return base.BeginRead(buffer, offset, count, callback, state);
            else
                return _impl.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            ValidateBufferArguments(buffer, offset, count);
            if (_impl.IsClosed) throw new ObjectDisposedException(SR.ObjectDisposed_FileClosed);
            if (!_impl.CanWrite) throw new NotSupportedException(SR.NotSupported_UnwritableStream);

            if (!_impl.IsAsync)
                return base.BeginWrite(buffer, offset, count, callback, state);
            else
                return _impl.BeginWrite(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult));

            if (!_impl.IsAsync)
                return base.EndRead(asyncResult);
            else
                return TaskToApm.End<int>(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult));

            if (!_impl.IsAsync)
                base.EndWrite(asyncResult);
            else
                TaskToApm.End(asyncResult);
        }

        public override bool CanSeek => _impl.CanSeek;

        public override long Seek(long offset, SeekOrigin origin) => _impl.Seek(offset, origin);
    }
}
