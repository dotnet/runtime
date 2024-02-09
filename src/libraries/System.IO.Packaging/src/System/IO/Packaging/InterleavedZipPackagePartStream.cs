// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.IO.Packaging
{
    /// <summary>
    /// The class InterleavedZipPackagePartStream is used to wrap one or more Zip package part streams
    /// for an interleaved part. It hides the interleaving from its callers by offering
    /// the abstraction of a continuous stream across pieces.
    /// </summary>
    /// <remarks>
    /// This class is defined for the benefit of ZipPackage, ZipPackagePart and
    /// InternalRelationshipCollection.
    /// Although it is quite specialized, it would hardly make sense to nest its definition in any
    /// of these clases.
    /// </remarks>
    internal sealed partial class InterleavedZipPackagePartStream : Stream
    {
        /// <summary>
        /// Build a System.IO.Stream on a part that possibly consists of multiple files
        /// An InterleavedZipPackagePartStream gets created by ZipPackagePart.GetStreamCore when the part
        /// is interleaved. It wraps one or more Zip streams (one per piece).
        /// (pieces).
        /// </summary>
        /// <param name="access">Access (read, write, etc.) with which piece streams should be opened</param>
        /// <param name="owningPart">
        /// The part to build a stream on. It contains all ZipFileInfo descriptors for the part's pieces
        /// (see ZipPackage.GetPartsCore).
        /// </param>
        /// <param name="zipStreamManager"></param>
        internal InterleavedZipPackagePartStream(ZipPackagePart owningPart, ZipStreamManager zipStreamManager, FileAccess access)
            : this(zipStreamManager, owningPart.PieceDescriptors!, access)
        {
        }

        /// <summary>
        /// This constructor is provided to be able to interleave other files than just parts,
        /// notably the contents type file.
        /// </summary>
        internal InterleavedZipPackagePartStream(ZipStreamManager zipStreamManager, List<ZipPackagePartPiece> sortedPieceInfoList, FileAccess access)
        {
            // The PieceDirectory mediates access to pieces.
            // It maps offsets to piece numbers and piece numbers to streams and start offsets.
            // Mode and access are entirely managed by the underlying streams, assumed to be seekable.
            _dir = new PieceDirectory(sortedPieceInfoList, zipStreamManager, access);

            // GetCurrentPieceNumber is operational from the beginning.
            Debug.Assert(_dir.GetStartOffset(GetCurrentPieceNumber()) == 0);
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
            => ReadCore(new Span<byte>(buffer, offset, count));

#if !NETFRAMEWORK && !NETSTANDARD2_0
        /// <inheritdoc/>
        public override int Read(Span<byte> buffer)
            => ReadCore(buffer);
#endif

        private int ReadCore(Span<byte> buffer)
        {
            CheckClosed();

            // Check arguments.
            if (!CanRead)
                throw new NotSupportedException(SR.ReadNotSupported);

            // Leave capability and FileAccess checks up to the underlying stream(s).

            // Reading 0 bytes is a no-op.
            if (buffer.Length == 0)
                return 0;

            int pieceNumber = GetCurrentPieceNumber();
            int totalBytesRead = 0;

            Stream pieceStream = _dir.GetStream(pieceNumber);
            long pieceStreamRelativeOffset = _currentOffset - _dir.GetStartOffset(pieceNumber);

            // .NET Standard 2.0 doesn't support the Read(Span<byte>) method. Instead, we rent a temporary
            // buffer of the same length, read into that and perform a copy into the span.
#if NETFRAMEWORK || NETSTANDARD2_0
            byte[] tempInputBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
#endif

            checked
            {
                // Find the current position of the first stream. Most of the time, this will be zero.
                // If the current position is ahead of the required relative position and the stream doesn't
                // support seeking, close and reopen the stream to force the position back to zero. Then,
                // read the remaining bytes to force the stream forwards
                if (pieceStream.CanSeek)
                {
                    if (pieceStream.Position != pieceStreamRelativeOffset)
                    {
                        pieceStream.Seek(pieceStreamRelativeOffset, SeekOrigin.Begin);
                    }
                }
                else
                {
                    pieceStream = _dir.ResetStream(pieceNumber);
                    SeekUnderlyingPieceStream(pieceStream, pieceStreamRelativeOffset);
                }

                while (totalBytesRead < buffer.Length)
                {
#if !NETFRAMEWORK && !NETSTANDARD2_0
                    int numBytesRead = pieceStream.Read(buffer.Slice(totalBytesRead));
#else
                    int numBytesRead = pieceStream.Read(
                        tempInputBuffer,
                        totalBytesRead,
                        buffer.Length - totalBytesRead);

                    tempInputBuffer.AsSpan(totalBytesRead, numBytesRead).CopyTo(buffer.Slice(totalBytesRead, numBytesRead));
#endif


                    // End of the current stream: try to move to the next stream.
                    if (numBytesRead == 0)
                    {
                        if (_dir.IsLastPiece(pieceNumber))
                            break;

                        ++pieceNumber;
                        Debug.Assert(_dir.GetStartOffset(pieceNumber) == _currentOffset + totalBytesRead);

                        pieceStream = _dir.GetStream(pieceNumber);

                        //Seek inorder to set the correct pointer for the next piece stream
                        if (pieceStream.CanSeek)
                        {
                            if (pieceStream.Position != 0)
                            {
                                pieceStream.Seek(0, SeekOrigin.Begin);
                            }
                            else
                            {
                                pieceStream = _dir.ResetStream(pieceNumber);
                            }
                        }
                    }

                    totalBytesRead += numBytesRead;
                }

#if NETFRAMEWORK || NETSTANDARD2_0
                ArrayPool<byte>.Shared.Return(tempInputBuffer);
#endif

                // Advance current position now we know the operation completed successfully.
                _currentOffset += totalBytesRead;
            }

            return totalBytesRead;
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckClosed();

            // Check stream capabilities. (Normally, CanSeek will be false only
            // when the stream is closed.)
            if (!CanSeek)
                throw new NotSupportedException(SR.SeekNotSupported);

            // Convert offset to a start-based offset.
            switch (origin)
            {
                case SeekOrigin.Begin:
                    break;

                case SeekOrigin.Current:
                    checked { offset += _currentOffset; }
                    break;

                case SeekOrigin.End:
                    checked { offset += Length; }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(origin));
            }

            // Check offset validity.
            if (offset < 0)
                throw new ArgumentException(SR.SeekNegative);

            // OK if _currentOffset points beyond end of stream.

            // Update position field and return.
            _currentOffset = offset;

            return _currentOffset;
        }

        /// <inheritdoc/>
        public override void SetLength(long newLength)
        {
            CheckClosed();

            // Check argument and stream capabilities.
            if (newLength < 0)
                throw new ArgumentOutOfRangeException(nameof(newLength));
            if (!CanWrite)
                throw new NotSupportedException(SR.StreamDoesNotSupportWrite);
            if (!CanSeek)
                throw new NotSupportedException(SR.SeekNotSupported);

            // If some pieces are to be deleted, this is reflected only in memory at present.
            int lastPieceNumber;
            if (newLength == 0)
            {
                // This is special-cased because there is no last offset to speak of, and
                // so the piece directory cannot return any piece by offset.
                lastPieceNumber = 0;
            }
            else
            {
                lastPieceNumber = _dir.GetPieceNumberFromOffset(newLength - 1); // No need to use checked{] since newLength != 0
            }
            _dir.SetLogicalLastPiece(lastPieceNumber);

            // Adjust last active stream to new size.
            Stream lastPieceStream = _dir.GetStream(lastPieceNumber);

            Debug.Assert(newLength - _dir.GetStartOffset(lastPieceNumber) >= 0);
            long lastPieceStreamSize = newLength - _dir.GetStartOffset(lastPieceNumber);
            lastPieceStream.SetLength(lastPieceStreamSize);

            if (_currentOffset > newLength)
            {
                _currentOffset = newLength;
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Zip streams can be assumed seekable so the length will be available for chaining
        /// pieces.
        /// </remarks>
        public override void Write(byte[] buffer, int offset, int count)
            => WriteCore(new ReadOnlySpan<byte>(buffer, offset, count));

#if !NETFRAMEWORK && !NETSTANDARD2_0
        /// <inheritdoc/>
        /// <remarks>
        /// Zip streams can be assumed seekable so the length will be available for chaining
        /// pieces.
        /// </remarks>
        public override void Write(ReadOnlySpan<byte> buffer)
            => WriteCore(buffer);
#endif

        private void WriteCore(ReadOnlySpan<byte> buffer)
        {
            CheckClosed();

            if (!CanWrite)
                throw new NotSupportedException(SR.WriteNotSupported);

            // No check for FileAccess and stream capability (CanWrite). This is the responsibility
            // of the underlying stream(s).

            // A no-op if zero bytes to write.
            if (buffer.Length == 0)
                return;

            // Write into piece streams, preserving all lengths in non-terminal pieces.
            int totalBytesWritten = 0;
            int pieceNumber = GetCurrentPieceNumber();
            Stream pieceStream = _dir.GetStream(pieceNumber);

            // .NET Standard 2.0 doesn't support the Write(ReadOnlySpan<byte>) method. Instead, rent a temporary
            // buffer of a specific length, write into that and write that to the underlying stream.
            // To slightly reduce memory usage, this buffer is reallocated with every new piece.
#if NETFRAMEWORK || NETSTANDARD2_0
            byte[] tempInputBuffer = null;
#endif

            checked
            {
                //Seek to the correct location in the underlying stream for the current piece
                pieceStream.Seek(_currentOffset - _dir.GetStartOffset(pieceNumber), SeekOrigin.Begin);

                while (totalBytesWritten < buffer.Length)
                {
                    // Compute the number of bytes to write into pieceStream.
                    int numBytesToWriteInCurrentPiece = buffer.Length - totalBytesWritten;
                    if (!_dir.IsLastPiece(pieceNumber))
                    {
                        // The write should not change the length of an intermediate piece.
                        long currentPosition = _currentOffset + totalBytesWritten;
                        long maxPosition = _dir.GetStartOffset(pieceNumber + 1) - 1;
                        if (numBytesToWriteInCurrentPiece > (maxPosition - currentPosition + 1))
                        {
                            // Cast from long to cast is safe in so far as *count*, which is the
                            // absolute max for all byte counts, is a positive int.
                            numBytesToWriteInCurrentPiece = checked((int)(maxPosition - currentPosition + 1));
                        }
                    }

#if NETFRAMEWORK || NETSTANDARD2_0
                    // Allocate memory to tempInputBuffer, copy the correct segment from the ReadOnlySpan, and
                    // do the write to pieceStream.
                    tempInputBuffer ??= ArrayPool<byte>.Shared.Rent(numBytesToWriteInCurrentPiece);

                    buffer.Slice(totalBytesWritten, numBytesToWriteInCurrentPiece).CopyTo(tempInputBuffer);
                    pieceStream.Write(tempInputBuffer, 0, numBytesToWriteInCurrentPiece);
#else
                    // Do the write.
                    pieceStream.Write(buffer.Slice(totalBytesWritten, numBytesToWriteInCurrentPiece));
#endif

                    // Update the tally.
                    totalBytesWritten += numBytesToWriteInCurrentPiece;

                    // If there is more data to write, get the next piece stream
                    if (!_dir.IsLastPiece(pieceNumber) && totalBytesWritten < buffer.Length)
                    {
                        // The next write, should involve the next piece.
                        ++pieceNumber;

                        pieceStream = _dir.GetStream(pieceNumber);

                        //Seek inorder to set the correct pointer for the next piece stream
                        pieceStream.Seek(0, SeekOrigin.Begin);

#if NETFRAMEWORK || NETSTANDARD2_0
                        // Return and unset tempInputBuffer, forcing it to be reallocated with the size of
                        // the next piece.
                        ArrayPool<byte>.Shared.Return(tempInputBuffer);
                        tempInputBuffer = null;
#endif
                    }
                }

                // Now we know the operation has completed, the current position can be updated.
                Debug.Assert(totalBytesWritten == buffer.Length);
                _currentOffset += totalBytesWritten;
            }
        }

        /// <summary>
        /// Flush all dirty streams and commit pending piece deletions.
        /// </summary>
        /// <remarks>
        /// Flush gets called on all underlying streams ever accessed. If it turned out
        /// this is too inefficient, the PieceDirectory could be made to expose a SetDirty
        /// method that takes a piece number.
        /// </remarks>
        public override void Flush()
        {
            CheckClosed();

            // The underlying streams know whether they are dirty or not;
            // so _dir will indiscriminately flush all the streams that have been accessed.
            // It will also carry out necessary renamings and deletions to reflect calls to
            // SetLogicalLastPiece.
            _dir.Flush();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// <para>
        /// Here, the assumption, as in all capability tests, is that the status of
        /// the first piece reflects the status of all pieces for the part.
        /// This is justified by the fact that (i) all piece streams are opened with the same
        /// parameters against the same archive and (ii) the current piece stream cannot get
        /// closed unless the whole part stream is closed.
        /// </para>
        /// <para>
        /// A further assumption is that, as soon as interleaved zip part stream is initialized, there
        /// is a descriptor for the 1st piece.
        /// </para>
        /// </remarks>
        public override bool CanRead
        {
            get
            {
                return _closed ? false : _dir.GetStream(0).CanRead;
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// <para>
        /// Here, the assumption, as in all capability tests, is that the status of
        /// the first piece reflects the status of all pieces for the part.
        /// This is justified by the fact that (i) all piece streams are opened with the same
        /// parameters against the same archive and (ii) the current piece stream cannot get
        /// closed unless the whole part stream is closed.
        /// </para>
        /// <para>
        /// A further assumption is that, as soon as interleaved zip part stream is initialized, there
        /// is a descriptor for the 1st piece.
        /// </para>
        /// </remarks>
        public override bool CanSeek
        {
            get
            {
                return _closed ? false : _dir.GetStream(0).CanSeek;
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// <para>
        /// Here, the assumption, as in all capability tests, is that the status of
        /// the first piece reflects the status of all pieces for the part.
        /// This is justified by the fact that (i) all piece streams are opened with the same
        /// parameters against the same archive and (ii) the current piece stream cannot get
        /// closed unless the whole part stream is closed.
        /// </para>
        /// <para>
        /// A further assumption is that, as soon as interleaved zip part stream is initialized, there
        /// is a descriptor for the 1st piece.
        /// </para>
        /// </remarks>
        //
        public override bool CanWrite
        {
            get
            {
                return _closed ? false : _dir.GetStream(0).CanWrite;
            }
        }

        /// <inheritdoc/>
        public override long Position
        {
            get
            {
                CheckClosed();

                // Current offset is systematically updated to reflect the current position.
                return _currentOffset;
            }
            set
            {
                CheckClosed();
                Seek(value, SeekOrigin.Begin);
            }
        }

        /// <inheritdoc/>
        public override long Length
        {
            get
            {
                CheckClosed();
                Debug.Assert(CanSeek);

                long length = 0;
                for (int pieceNumber = 0; pieceNumber < _dir.GetNumberOfPieces(); ++pieceNumber)
                {
                    checked { length += _dir.GetStream(pieceNumber).Length; }
                }
                return length;
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {


            try
            {
                if (disposing)
                {
                    if (!_closed)
                    {
                        _dir.Close();
                    }
                }
            }
            finally
            {
                _closed = true;
                base.Dispose(disposing);
            }
        }

        private void CheckClosed()
        {
            if (_closed)
                throw new ObjectDisposedException(null, SR.StreamObjectDisposed);
        }

        /// <summary>
        /// Infer the current piece number from _currentOffset.
        /// </summary>
        /// <remarks>
        /// Storing the current piece number in a field and computing the current offset from it
        /// would also have been possible, but less efficient.
        /// </remarks>
        private int GetCurrentPieceNumber()
        {
            // Since this property is likely to be read more often than _currentOffset
            // gets updated, its value is cached in _currentPieceNumber.
            // The validity of the cached value is monitored using _offsetForCurrentPieceNumber.
            if (_offsetForCurrentPieceNumber != _currentOffset)
            {
                // Cached value is stale. Refresh.
                _currentPieceNumber = _dir.GetPieceNumberFromOffset(_currentOffset);
                _offsetForCurrentPieceNumber = _currentOffset;
            }
            return _currentPieceNumber;
        }

        /// <summary>
        /// Moves a piece stream at position zero to its new absolute position by reading from it.
        /// </summary>
        private static void SeekUnderlyingPieceStream(Stream pieceStream, long byteCount)
        {
            const int BufferSize = 4096;
            long remainingBytes = byteCount;
            byte[] readBuffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            int bytesRead = byteCount < BufferSize ? (int)byteCount : BufferSize;

            do
            {
                bytesRead = pieceStream.Read(readBuffer, 0, bytesRead);
                remainingBytes -= bytesRead;
            } while (remainingBytes > 0 && bytesRead > 0);

            ArrayPool<byte>.Shared.Return(readBuffer);

            if (remainingBytes != 0)
            {
                throw new NullReferenceException();
            }
        }

        // High-level object to access the collection of pieces by offset and pieceNumber.
        private readonly PieceDirectory _dir;

        // Cached value for the current piece number.
        // (Lazily sync'ed to _currentOffset when GetCurrentPieceNumber() is invoked.)
        private int _currentPieceNumber;

        // Control value to decide whether to use _currentPieceNumber without updating it.
        private long? _offsetForCurrentPieceNumber;

        // This variable continuously tracks the current stream position.
        private long _currentOffset;

        // Closed status.
        private bool _closed;

    }

}
