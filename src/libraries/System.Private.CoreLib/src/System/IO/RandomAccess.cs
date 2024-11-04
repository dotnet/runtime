// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Strategies;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public static partial class RandomAccess
    {
        /// <summary>
        /// Gets the length of the file in bytes.
        /// </summary>
        /// <param name="handle">The file handle.</param>
        /// <returns>A long value representing the length of the file in bytes.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="handle" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="handle" /> is invalid.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The file is closed.</exception>
        /// <exception cref="T:System.NotSupportedException">The file does not support seeking (pipe or socket).</exception>
        public static long GetLength(SafeFileHandle handle)
        {
            ValidateInput(handle, fileOffset: 0, allowUnseekableHandles: false);

            return handle.GetFileLength();
        }

        /// <summary>
        /// Sets the length of the file to the given value.
        /// </summary>
        /// <param name="handle">The file handle.</param>
        /// <param name="length">A long value representing the length of the file in bytes.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="handle" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="handle" /> is invalid.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The file is closed.</exception>
        /// <exception cref="T:System.NotSupportedException">The file does not support seeking (pipe or socket).</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="length" /> is negative.</exception>
        public static void SetLength(SafeFileHandle handle, long length)
        {
            ValidateInput(handle, fileOffset: 0, allowUnseekableHandles: false);

            if (length < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_NeedNonNegNum(nameof(length));
            }

            SetFileLength(handle, length);
        }

        /// <summary>
        /// Reads a sequence of bytes from given file at given offset.
        /// </summary>
        /// <param name="handle">The file handle.</param>
        /// <param name="buffer">A region of memory. When this method returns, the contents of this region are replaced by the bytes read from the file.</param>
        /// <param name="fileOffset">The file position to read from. For a file that does not support seeking (pipe or socket), it's ignored.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes allocated in the buffer if that many bytes are not currently available, or zero (0) if the end of the file has been reached.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="handle" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="handle" /> is invalid.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The file is closed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="fileOffset" /> is negative.</exception>
        /// <exception cref="T:System.UnauthorizedAccessException"><paramref name="handle" /> was not opened for reading.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <remarks>Position of the file is not advanced.</remarks>
        public static int Read(SafeFileHandle handle, Span<byte> buffer, long fileOffset)
        {
            ValidateInput(handle, fileOffset);

            return ReadAtOffset(handle, buffer, fileOffset);
        }

        /// <summary>
        /// Reads a sequence of bytes from given file at given offset.
        /// </summary>
        /// <param name="handle">The file handle.</param>
        /// <param name="buffers">A list of memory buffers. When this method returns, the contents of the buffers are replaced by the bytes read from the file.</param>
        /// <param name="fileOffset">The file position to read from. For a file that does not support seeking (pipe or socket), it's ignored.</param>
        /// <returns>The total number of bytes read into the buffers. This can be less than the number of bytes allocated in the buffers if that many bytes are not currently available, or zero (0) if the end of the file has been reached.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="handle" /> or <paramref name="buffers" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="handle" /> is invalid.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The file is closed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="fileOffset" /> is negative.</exception>
        /// <exception cref="T:System.UnauthorizedAccessException"><paramref name="handle" /> was not opened for reading.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <remarks>Position of the file is not advanced.</remarks>
        public static long Read(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset)
        {
            ValidateInput(handle, fileOffset);
            ValidateBuffers(buffers);

            return ReadScatterAtOffset(handle, buffers, fileOffset);
        }

        /// <summary>
        /// Reads a sequence of bytes from given file at given offset.
        /// </summary>
        /// <param name="handle">The file handle.</param>
        /// <param name="buffer">A region of memory. When this method returns, the contents of this region are replaced by the bytes read from the file.</param>
        /// <param name="fileOffset">The file position to read from. For a file that does not support seeking (pipe or socket), it's ignored.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes allocated in the buffer if that many bytes are not currently available, or zero (0) if the end of the file has been reached.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="handle" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="handle" /> is invalid.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The file is closed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="fileOffset" /> is negative.</exception>
        /// <exception cref="T:System.UnauthorizedAccessException"><paramref name="handle" /> was not opened for reading.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <remarks>Position of the file is not advanced.</remarks>
        public static ValueTask<int> ReadAsync(SafeFileHandle handle, Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken = default)
        {
            ValidateInput(handle, fileOffset);

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            return ReadAtOffsetAsync(handle, buffer, fileOffset, cancellationToken);
        }

        /// <summary>
        /// Reads a sequence of bytes from given file at given offset.
        /// </summary>
        /// <param name="handle">The file handle.</param>
        /// <param name="buffers">A list of memory buffers. When this method returns, the contents of these buffers are replaced by the bytes read from the file.</param>
        /// <param name="fileOffset">The file position to read from. For a file that does not support seeking (pipe or socket), it's ignored.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
        /// <returns>The total number of bytes read into the buffers. This can be less than the number of bytes allocated in the buffers if that many bytes are not currently available, or zero (0) if the end of the file has been reached.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="handle" /> or <paramref name="buffers" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="handle" /> is invalid.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The file is closed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="fileOffset" /> is negative.</exception>
        /// <exception cref="T:System.UnauthorizedAccessException"><paramref name="handle" /> was not opened for reading.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <remarks>Position of the file is not advanced.</remarks>
        public static ValueTask<long> ReadAsync(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset, CancellationToken cancellationToken = default)
        {
            ValidateInput(handle, fileOffset);
            ValidateBuffers(buffers);

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<long>(cancellationToken);
            }

            return ReadScatterAtOffsetAsync(handle, buffers, fileOffset, cancellationToken);
        }

        /// <summary>
        /// Writes a sequence of bytes from given buffer to given file at given offset.
        /// </summary>
        /// <param name="handle">The file handle.</param>
        /// <param name="buffer">A region of memory. This method copies the contents of this region to the file.</param>
        /// <param name="fileOffset">The file position to write to. For a file that does not support seeking (pipe or socket), it's ignored.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="handle" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="handle" /> is invalid.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The file is closed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="fileOffset" /> is negative.</exception>
        /// <exception cref="T:System.UnauthorizedAccessException"><paramref name="handle" /> was not opened for writing.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <remarks>Position of the file is not advanced.</remarks>
        public static void Write(SafeFileHandle handle, ReadOnlySpan<byte> buffer, long fileOffset)
        {
            ValidateInput(handle, fileOffset);

            WriteAtOffset(handle, buffer, fileOffset);
        }

        /// <summary>
        /// Writes a sequence of bytes from given buffers to given file at given offset.
        /// </summary>
        /// <param name="handle">The file handle.</param>
        /// <param name="buffers">A list of memory buffers. This method copies the contents of these buffers to the file.</param>
        /// <param name="fileOffset">The file position to write to. For a file that does not support seeking (pipe or socket), it's ignored.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="handle" /> or <paramref name="buffers" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="handle" /> is invalid.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The file is closed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="fileOffset" /> is negative.</exception>
        /// <exception cref="T:System.UnauthorizedAccessException"><paramref name="handle" /> was not opened for writing.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <remarks>Position of the file is not advanced.</remarks>
        public static void Write(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset)
        {
            ValidateInput(handle, fileOffset);
            ValidateBuffers(buffers);

            WriteGatherAtOffset(handle, buffers, fileOffset);
        }

        /// <summary>
        /// Writes a sequence of bytes from given buffer to given file at given offset.
        /// </summary>
        /// <param name="handle">The file handle.</param>
        /// <param name="buffer">A region of memory. This method copies the contents of this region to the file.</param>
        /// <param name="fileOffset">The file position to write to. For a file that does not support seeking (pipe or socket), it's ignored.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task representing the asynchronous completion of the write operation.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="handle" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="handle" /> is invalid.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The file is closed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="fileOffset" /> is negative.</exception>
        /// <exception cref="T:System.UnauthorizedAccessException"><paramref name="handle" /> was not opened for writing.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <remarks>Position of the file is not advanced.</remarks>
        public static ValueTask WriteAsync(SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken = default)
        {
            ValidateInput(handle, fileOffset);

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(cancellationToken);
            }

            return WriteAtOffsetAsync(handle, buffer, fileOffset, cancellationToken);
        }

        /// <summary>
        /// Writes a sequence of bytes from given buffers to given file at given offset.
        /// </summary>
        /// <param name="handle">The file handle.</param>
        /// <param name="buffers">A list of memory buffers. This method copies the contents of these buffers to the file.</param>
        /// <param name="fileOffset">The file position to write to. For a file that does not support seeking (pipe or socket), it's ignored.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task representing the asynchronous completion of the write operation.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="handle" /> or <paramref name="buffers"/> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="handle" /> is invalid.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The file is closed.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="fileOffset" /> is negative.</exception>
        /// <exception cref="T:System.UnauthorizedAccessException"><paramref name="handle" /> was not opened for writing.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <remarks>Position of the file is not advanced.</remarks>
        public static ValueTask WriteAsync(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset, CancellationToken cancellationToken = default)
        {
            ValidateInput(handle, fileOffset);
            ValidateBuffers(buffers);

            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled(cancellationToken);
            }

            return WriteGatherAtOffsetAsync(handle, buffers, fileOffset, cancellationToken);
        }

        /// <summary>
        /// Flushes the operating system buffers for the given file to disk.
        /// </summary>
        /// <param name="handle">The file handle.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="handle" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="handle" /> is invalid.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The file is closed.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <remarks>
        /// <para>
        /// This method calls platform-dependent APIs such as <c>FlushFileBuffers()</c> on Windows and <c>fsync()</c> on Unix.
        /// </para>
        /// <para>
        /// Flushing the buffers causes data to be written to disk which is a relatively expensive operation. It is recommended
        /// that you perform multiple writes to the file and then call this method either when you are done writing to the file
        /// or periodically if you expect to continue writing to the file over a long period of time.
        /// </para>
        /// </remarks>
        public static void FlushToDisk(SafeFileHandle handle)
        {
            // NOTE: we need to allow unseekable handles when validating the input because the FlushFileBuffers()
            // function on Windows DOES support unseekable handles (e.g. pipe handles). The fsync() function on
            // Unix does NOT support unseekable handles however, the code that ultimately runs on Unix when we
            // call FileStreamHelpers.FlushToDisk() later below, will silently ignore those errors, effectively
            // making FlushToDisk() a no-op on Unix when used with unseekable handles.
            ValidateInput(handle, fileOffset: 0);

            FileStreamHelpers.FlushToDisk(handle);
        }

        private static void ValidateInput(SafeFileHandle handle, long fileOffset, bool allowUnseekableHandles = true)
        {
            if (handle is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.handle);
            }
            else if (handle.IsInvalid)
            {
                ThrowHelper.ThrowArgumentException_InvalidHandle(nameof(handle));
            }
            else if (handle.IsClosed)
            {
                ThrowHelper.ThrowObjectDisposedException_FileClosed();
            }
            else if (!allowUnseekableHandles && !handle.CanSeek)
            {
                ThrowHelper.ThrowNotSupportedException_UnseekableStream();
            }
            else if (fileOffset < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_NeedNonNegNum(nameof(fileOffset));
            }
        }

        private static void ValidateBuffers<T>(IReadOnlyList<T> buffers)
        {
            if (buffers is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.buffers);
            }
        }
    }
}
