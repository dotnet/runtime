// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO.Strategies;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public class FileStream : Stream
    {
        internal const int DefaultBufferSize = 4096;
        internal const FileShare DefaultShare = FileShare.Read;
        private const bool DefaultIsAsync = false;

        /// <summary>Caches whether Serialization Guard has been disabled for file writes</summary>
        private static int s_cachedSerializationSwitch;

        private readonly FileStreamStrategy _strategy;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This constructor has been deprecated.  Please use new FileStream(SafeFileHandle handle, FileAccess access) instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public FileStream(IntPtr handle, FileAccess access)
            : this(handle, access, true, DefaultBufferSize, false)
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This constructor has been deprecated.  Please use new FileStream(SafeFileHandle handle, FileAccess access) instead, and optionally make a new SafeFileHandle with ownsHandle=false if needed.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public FileStream(IntPtr handle, FileAccess access, bool ownsHandle)
            : this(handle, access, ownsHandle, DefaultBufferSize, false)
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This constructor has been deprecated.  Please use new FileStream(SafeFileHandle handle, FileAccess access, int bufferSize) instead, and optionally make a new SafeFileHandle with ownsHandle=false if needed.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public FileStream(IntPtr handle, FileAccess access, bool ownsHandle, int bufferSize)
            : this(handle, access, ownsHandle, bufferSize, false)
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This constructor has been deprecated.  Please use new FileStream(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync) instead, and optionally make a new SafeFileHandle with ownsHandle=false if needed.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public FileStream(IntPtr handle, FileAccess access, bool ownsHandle, int bufferSize, bool isAsync)
        {
            SafeFileHandle safeHandle = new SafeFileHandle(handle, ownsHandle: ownsHandle);
            try
            {
                ValidateHandle(safeHandle, access, bufferSize, isAsync);

                _strategy = FileStreamHelpers.ChooseStrategy(this, safeHandle, access, DefaultShare, bufferSize, isAsync);
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
            {
                throw new ArgumentException(SR.Arg_InvalidHandle, nameof(handle));
            }
            else if (access < FileAccess.Read || access > FileAccess.ReadWrite)
            {
                throw new ArgumentOutOfRangeException(nameof(access), SR.ArgumentOutOfRange_Enum);
            }
            else if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedPosNum);
            }
            else if (handle.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }
            else if (handle.IsAsync.HasValue && isAsync != handle.IsAsync.GetValueOrDefault())
            {
                throw new ArgumentException(SR.Arg_HandleNotAsync, nameof(handle));
            }
        }

        public FileStream(SafeFileHandle handle, FileAccess access)
            : this(handle, access, DefaultBufferSize)
        {
        }

        public FileStream(SafeFileHandle handle, FileAccess access, int bufferSize)
            : this(handle, access, bufferSize, FileStreamHelpers.GetDefaultIsAsync(handle, DefaultIsAsync))
        {
        }

        public FileStream(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
        {
            ValidateHandle(handle, access, bufferSize, isAsync);

            _strategy = FileStreamHelpers.ChooseStrategy(this, handle, access, DefaultShare, bufferSize, isAsync);
        }

        public FileStream(string path, FileMode mode)
            : this(path, mode, mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite, DefaultShare, DefaultBufferSize, DefaultIsAsync)
        {
        }

        public FileStream(string path, FileMode mode, FileAccess access)
            : this(path, mode, access, DefaultShare, DefaultBufferSize, DefaultIsAsync)
        {
        }

        public FileStream(string path, FileMode mode, FileAccess access, FileShare share)
            : this(path, mode, access, share, DefaultBufferSize, DefaultIsAsync)
        {
        }

        public FileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize)
            : this(path, mode, access, share, bufferSize, DefaultIsAsync)
        {
        }

        public FileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync)
            : this(path, mode, access, share, bufferSize, useAsync ? FileOptions.Asynchronous : FileOptions.None)
        {
        }

        public FileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
            : this(path, mode, access, share, bufferSize, options, 0)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="System.IO.FileStream" /> class with the specified path, creation mode, read/write and sharing permission, the access other FileStreams can have to the same file, the buffer size,  additional file options and the allocation size.
        /// </summary>
        /// <param name="path">A relative or absolute path for the file that the current <see cref="System.IO.FileStream" /> instance will encapsulate.</param>
        /// <param name="options">An object that describes optional <see cref="System.IO.FileStream" /> parameters to use.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="path" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="path" /> is an empty string (""), contains only white space, or contains one or more invalid characters.
        /// -or-
        /// <paramref name="path" /> refers to a non-file device, such as <c>CON:</c>, <c>COM1:</c>, <c>LPT1:</c>, etc. in an NTFS environment.</exception>
        /// <exception cref="T:System.NotSupportedException"><paramref name="path" /> refers to a non-file device, such as <c>CON:</c>, <c>COM1:</c>, <c>LPT1:</c>, etc. in a non-NTFS environment.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><see cref="System.IO.FileStreamOptions.BufferSize" /> is negative.
        /// -or-
        /// <see cref="System.IO.FileStreamOptions.PreallocationSize" /> is negative.
        /// -or-
        /// <see cref="System.IO.FileStreamOptions.Mode" />, <see cref="System.IO.FileStreamOptions.Access" />, or <see cref="System.IO.FileStreamOptions.Share" /> contain an invalid value.</exception>
        /// <exception cref="T:System.IO.FileNotFoundException">The file cannot be found, such as when <see cref="System.IO.FileStreamOptions.Mode" /> is <see langword="FileMode.Truncate" /> or <see langword="FileMode.Open" />, and the file specified by <paramref name="path" /> does not exist. The file must already exist in these modes.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error, such as specifying <see langword="FileMode.CreateNew" /> when the file specified by <paramref name="path" /> already exists, occurred.
        ///  -or-
        ///  The stream has been closed.
        ///  -or-
        ///  The disk was full (when <see cref="System.IO.FileStreamOptions.PreallocationSize" /> was provided and <paramref name="path" /> was pointing to a regular file).
        ///  -or-
        ///  The file was too large (when <see cref="System.IO.FileStreamOptions.PreallocationSize" /> was provided and <paramref name="path" /> was pointing to a regular file).</exception>
        /// <exception cref="T:System.Security.SecurityException">The caller does not have the required permission.</exception>
        /// <exception cref="T:System.IO.DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
        /// <exception cref="T:System.UnauthorizedAccessException">The <see cref="System.IO.FileStreamOptions.Access" /> requested is not permitted by the operating system for the specified <paramref name="path" />, such as when <see cref="System.IO.FileStreamOptions.Access" />  is <see cref="System.IO.FileAccess.Write" /> or <see cref="System.IO.FileAccess.ReadWrite" /> and the file or directory is set for read-only access.
        ///  -or-
        /// <see cref="F:System.IO.FileOptions.Encrypted" /> is specified for <see cref="System.IO.FileStreamOptions.Options" /> , but file encryption is not supported on the current platform.</exception>
        /// <exception cref="T:System.IO.PathTooLongException">The specified path, file name, or both exceed the system-defined maximum length. </exception>
        public FileStream(string path, FileStreamOptions options)
            : this(path, options.Mode, options.Access, options.Share, options.BufferSize, options.Options, options.PreallocationSize)
        {
        }

        private FileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, long preallocationSize)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path), SR.ArgumentNull_Path);
            }
            else if (path.Length == 0)
            {
                throw new ArgumentException(SR.Argument_EmptyPath, nameof(path));
            }

            // don't include inheritable in our bounds check for share
            FileShare tempshare = share & ~FileShare.Inheritable;
            string? badArg = null;

            if (mode < FileMode.CreateNew || mode > FileMode.Append)
            {
                badArg = nameof(mode);
            }
            else if (access < FileAccess.Read || access > FileAccess.ReadWrite)
            {
                badArg = nameof(access);
            }
            else if (tempshare < FileShare.None || tempshare > (FileShare.ReadWrite | FileShare.Delete))
            {
                badArg = nameof(share);
            }

            if (badArg != null)
            {
                throw new ArgumentOutOfRangeException(badArg, SR.ArgumentOutOfRange_Enum);
            }

            // NOTE: any change to FileOptions enum needs to be matched here in the error validation
            if (options != FileOptions.None && (options & ~(FileOptions.WriteThrough | FileOptions.Asynchronous | FileOptions.RandomAccess | FileOptions.DeleteOnClose | FileOptions.SequentialScan | FileOptions.Encrypted | (FileOptions)0x20000000 /* NoBuffering */)) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options), SR.ArgumentOutOfRange_Enum);
            }
            else if (bufferSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), SR.ArgumentOutOfRange_NeedPosNum);
            }
            else if (preallocationSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(preallocationSize), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

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
            {
                throw new ArgumentException(SR.Argument_InvalidAppendMode, nameof(access));
            }
            else if ((access & FileAccess.Write) == FileAccess.Write)
            {
                SerializationInfo.ThrowIfDeserializationInProgress("AllowFileWrites", ref s_cachedSerializationSwitch);
            }

            _strategy = FileStreamHelpers.ChooseStrategy(this, path, mode, access, share, bufferSize, options, preallocationSize);
        }

        [Obsolete("This property has been deprecated.  Please use FileStream's SafeFileHandle property instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public virtual IntPtr Handle => _strategy.Handle;

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("macos")]
        [UnsupportedOSPlatform("tvos")]
        public virtual void Lock(long position, long length)
        {
            if (position < 0 || length < 0)
            {
                throw new ArgumentOutOfRangeException(position < 0 ? nameof(position) : nameof(length), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            else if (_strategy.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }

            _strategy.Lock(position, length);
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("macos")]
        [UnsupportedOSPlatform("tvos")]
        public virtual void Unlock(long position, long length)
        {
            if (position < 0 || length < 0)
            {
                throw new ArgumentOutOfRangeException(position < 0 ? nameof(position) : nameof(length), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            else if (_strategy.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }

            _strategy.Unlock(position, length);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }
            else if (_strategy.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }

            return _strategy.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateReadWriteArgs(buffer, offset, count);

            return _strategy.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer) => _strategy.Read(buffer);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }
            else if (_strategy.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }

            return _strategy.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }
            else if (_strategy.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }

            return _strategy.ReadAsync(buffer, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateReadWriteArgs(buffer, offset, count);

            _strategy.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer) => _strategy.Write(buffer);

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }
            else if (_strategy.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }

            return _strategy.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(cancellationToken);
            }
            else if (_strategy.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }

            return _strategy.WriteAsync(buffer, cancellationToken);
        }

        /// <summary>
        /// Clears buffers for this stream and causes any buffered data to be written to the file.
        /// </summary>
        public override void Flush()
        {
            // Make sure that we call through the public virtual API
            Flush(flushToDisk: false);
        }

        /// <summary>
        /// Clears buffers for this stream, and if <param name="flushToDisk"/> is true,
        /// causes any buffered data to be written to the file.
        /// </summary>
        public virtual void Flush(bool flushToDisk)
        {
            if (_strategy.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }

            _strategy.Flush(flushToDisk);
        }

        /// <summary>Gets a value indicating whether the current stream supports reading.</summary>
        public override bool CanRead => _strategy.CanRead;

        /// <summary>Gets a value indicating whether the current stream supports writing.</summary>
        public override bool CanWrite => _strategy.CanWrite;

        /// <summary>Validates arguments to Read and Write and throws resulting exceptions.</summary>
        /// <param name="buffer">The buffer to read from or write to.</param>
        /// <param name="offset">The zero-based offset into the buffer.</param>
        /// <param name="count">The maximum number of bytes to read or write.</param>
        private void ValidateReadWriteArgs(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            if (_strategy.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }
        }

        /// <summary>Sets the length of this stream to the given value.</summary>
        /// <param name="value">The new length of the stream.</param>
        public override void SetLength(long value)
        {
            if (value < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            }
            else if (_strategy.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }
            else if (!CanSeek)
            {
                ThrowHelper.ThrowNotSupportedException_UnseekableStream();
            }
            else if (!CanWrite)
            {
                ThrowHelper.ThrowNotSupportedException_UnwritableStream();
            }

            _strategy.SetLength(value);
        }

        public virtual SafeFileHandle SafeFileHandle => _strategy.SafeFileHandle;

        /// <summary>Gets the path that was passed to the constructor.</summary>
        public virtual string Name => _strategy.Name;

        /// <summary>Gets a value indicating whether the stream was opened for I/O to be performed synchronously or asynchronously.</summary>
        public virtual bool IsAsync => _strategy.IsAsync;

        /// <summary>Gets the length of the stream in bytes.</summary>
        public override long Length
        {
            get
            {
                if (_strategy.IsClosed)
                {
                    ThrowHelper.ThrowObjectDisposedException_FileClosed();
                }
                else if (!CanSeek)
                {
                    ThrowHelper.ThrowNotSupportedException_UnseekableStream();
                }

                return _strategy.Length;
            }
        }

        /// <summary>Gets or sets the position within the current stream</summary>
        public override long Position
        {
            get
            {
                if (_strategy.IsClosed)
                {
                    ThrowHelper.ThrowObjectDisposedException_FileClosed();
                }
                else if (!CanSeek)
                {
                    ThrowHelper.ThrowNotSupportedException_UnseekableStream();
                }

                return _strategy.Position;
            }
            set
            {
                if (value < 0)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
                }

                _strategy.Seek(value, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Reads a byte from the file stream.  Returns the byte cast to an int
        /// or -1 if reading from the end of the stream.
        /// </summary>
        public override int ReadByte() => _strategy.ReadByte();

        /// <summary>
        /// Writes a byte to the current position in the stream and advances the position
        /// within the stream by one byte.
        /// </summary>
        /// <param name="value">The byte to write to the stream.</param>
        public override void WriteByte(byte value) => _strategy.WriteByte(value);

        protected override void Dispose(bool disposing) => _strategy.DisposeInternal(disposing);

        internal void DisposeInternal(bool disposing) => Dispose(disposing);

        public override ValueTask DisposeAsync() => _strategy.DisposeAsync();

        public override void CopyTo(Stream destination, int bufferSize) => _strategy.CopyTo(destination, bufferSize);

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            => _strategy.CopyToAsync(destination, bufferSize, cancellationToken);

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            ValidateBufferArguments(buffer, offset, count);

            if (_strategy.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }
            else if (!CanRead)
            {
                ThrowHelper.ThrowNotSupportedException_UnreadableStream();
            }

            return _strategy.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            return _strategy.EndRead(asyncResult);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            ValidateBufferArguments(buffer, offset, count);

            if (_strategy.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }
            else if (!CanWrite)
            {
                ThrowHelper.ThrowNotSupportedException_UnwritableStream();
            }

            return _strategy.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            _strategy.EndWrite(asyncResult);
        }

        public override bool CanSeek => _strategy.CanSeek;

        public override long Seek(long offset, SeekOrigin origin) => _strategy.Seek(offset, origin);

        internal Task BaseFlushAsync(CancellationToken cancellationToken)
            => base.FlushAsync(cancellationToken);

        internal int BaseRead(Span<byte> buffer) => base.Read(buffer);

        internal Task<int> BaseReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => base.ReadAsync(buffer, offset, count, cancellationToken);

        internal ValueTask<int> BaseReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => base.ReadAsync(buffer, cancellationToken);

        internal void BaseWrite(ReadOnlySpan<byte> buffer) => base.Write(buffer);

        internal Task BaseWriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => base.WriteAsync(buffer, offset, count, cancellationToken);

        internal ValueTask BaseWriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            => base.WriteAsync(buffer, cancellationToken);

        internal ValueTask BaseDisposeAsync() => base.DisposeAsync();

        internal Task BaseCopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            => base.CopyToAsync(destination, bufferSize, cancellationToken);

        internal IAsyncResult BaseBeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
            => base.BeginRead(buffer, offset, count, callback, state);

        internal int BaseEndRead(IAsyncResult asyncResult) => base.EndRead(asyncResult);

        internal IAsyncResult BaseBeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
            => base.BeginWrite(buffer, offset, count, callback, state);

        internal void BaseEndWrite(IAsyncResult asyncResult) => base.EndWrite(asyncResult);
    }
}
