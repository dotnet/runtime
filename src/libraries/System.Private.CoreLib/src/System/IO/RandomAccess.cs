// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public static partial class RandomAccess
    {
        /// <summary>
        /// Gets the length in bytes of the file.
        /// </summary>
        /// <param name="handle">The file handle.</param>
        /// <returns>A long value representing the length of the file in bytes.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="handle" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="handle" /> is invalid.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The file is closed.</exception>
        /// <exception cref="T:System.NotSupportedException">The file does not support seeking (pipe or socket).</exception>
        public static long GetLength(SafeFileHandle handle)
        {
            ValidateInput(handle, fileOffset: 0);

            return GetFileLength(handle, path: null);
        }

        /// <summary>
        /// Reads a sequence of bytes from given file at given offset.
        /// </summary>
        /// <param name="handle">The file handle.</param>
        /// <param name="buffer">A region of memory. When this method returns, the contents of this region are replaced by the bytes read from the file.</param>
        /// <param name="fileOffset">The file position to read from.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes allocated in the buffer if that many bytes are not currently available, or zero (0) if the end of the file has been reached.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="handle" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="handle" /> is invalid.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The file is closed.</exception>
        /// <exception cref="T:System.NotSupportedException">The file does not support seeking (pipe or socket).</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="handle" /> was opened for async IO.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="fileOffset" /> is negative.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <remarks>Position of the file is not advanced.</remarks>
        public static int Read(SafeFileHandle handle, Span<byte> buffer, long fileOffset)
        {
            ValidateInput(handle, fileOffset, mustBeSync: OperatingSystem.IsWindows());

            return ReadAtOffset(handle, buffer, fileOffset);
        }

        /// <summary>
        /// Writes a sequence of bytes from given buffer to given file at given offset.
        /// </summary>
        /// <param name="handle">The file handle.</param>
        /// <param name="buffer">A region of memory. This method copies the contents of this region to the file.</param>
        /// <param name="fileOffset">The file position to write to.</param>
        /// <returns>The total number of bytes written into the file. This can be less than the number of bytes provided in the buffer and it's not an error.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="handle" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="handle" /> is invalid.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The file is closed.</exception>
        /// <exception cref="T:System.NotSupportedException">The file does not support seeking (pipe or socket).</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="handle" /> was opened for async IO.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="fileOffset" /> is negative.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <remarks>Position of the file is not advanced.</remarks>
        public static int Write(SafeFileHandle handle, ReadOnlySpan<byte> buffer, long fileOffset)
        {
            ValidateInput(handle, fileOffset, mustBeSync: OperatingSystem.IsWindows());

            return WriteAtOffset(handle, buffer, fileOffset);
        }

        /// <summary>
        /// Reads a sequence of bytes from given file at given offset.
        /// </summary>
        /// <param name="handle">The file handle.</param>
        /// <param name="buffer">A region of memory. When this method returns, the contents of this region are replaced by the bytes read from the file.</param>
        /// <param name="fileOffset">The file position to read from.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes allocated in the buffer if that many bytes are not currently available, or zero (0) if the end of the file has been reached.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="handle" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="handle" /> is invalid.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The file is closed.</exception>
        /// <exception cref="T:System.NotSupportedException">The file does not support seeking (pipe or socket).</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="handle" /> was not opened for async IO.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="fileOffset" /> is negative.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <remarks>Position of the file is not advanced.</remarks>
        public static ValueTask<int> ReadAsync(SafeFileHandle handle, Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken = default)
        {
            ValidateInput(handle, fileOffset, mustBeAsync: true);

            return ReadAtOffsetAsync(handle, buffer, fileOffset, cancellationToken);
        }

        /// <summary>
        /// Writes a sequence of bytes from given buffer to given file at given offset.
        /// </summary>
        /// <param name="handle">The file handle.</param>
        /// <param name="buffer">A region of memory. This method copies the contents of this region to the file.</param>
        /// <param name="fileOffset">The file position to write to.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="P:System.Threading.CancellationToken.None" />.</param>
        /// <returns>The total number of bytes written into the file. This can be less than the number of bytes provided in the buffer and it's not an error.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="handle" /> is <see langword="null" />.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="handle" /> is invalid.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The file is closed.</exception>
        /// <exception cref="T:System.NotSupportedException">The file does not support seeking (pipe or socket).</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="handle" /> was not opened for async IO.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="fileOffset" /> is negative.</exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
        /// <remarks>Position of the file is not advanced.</remarks>
        public static ValueTask<int> WriteAsync(SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken = default)
        {
            ValidateInput(handle, fileOffset, mustBeAsync: true);

            return WriteAtOffsetAsync(handle, buffer, fileOffset, cancellationToken);
        }

        private static void ValidateInput(SafeFileHandle handle, long fileOffset, bool mustBeSync = false, bool mustBeAsync = false)
        {
            if (handle is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.handle);
            }
            else if (handle.IsInvalid)
            {
                ThrowHelper.ThrowArgumentException_InvalidHandle(nameof(handle));
            }
            else if (!handle.CanSeek)
            {
                // CanSeek calls IsClosed, we don't want to call it twice for valid handles
                if (handle.IsClosed)
                {
                    ThrowHelper.ThrowObjectDisposedException_FileClosed();
                }

                ThrowHelper.ThrowNotSupportedException_UnseekableStream();
            }
            else if (mustBeSync && handle.IsAsync)
            {
                ThrowHelper.ThrowArgumentException_HandleNotSync(nameof(handle));
            }
            else if (mustBeAsync && !handle.IsAsync)
            {
                ThrowHelper.ThrowArgumentException_HandleNotAsync(nameof(handle));
            }
            else if (fileOffset < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_NeedPosNum(nameof(fileOffset));
            }
        }
    }
}
