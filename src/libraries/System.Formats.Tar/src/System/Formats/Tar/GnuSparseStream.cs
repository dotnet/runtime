// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Formats.Tar
{
    // Stream that wraps the raw data section of a GNU sparse format 1.0 PAX entry and
    // expands it to the virtual file size by inserting zeros for sparse holes.
    //
    // The raw data section layout:
    //   [sparse map text: numSegs\n, then pairs of offset\n numbytes\n]
    //   [zero padding to the next 512-byte block boundary]
    //   [packed non-zero data segments, stored sequentially]
    //
    // This stream presents a virtual file of size 'realSize' where:
    //   - regions covered by sparse map segments contain the packed data
    //   - all other regions (holes) read as zero bytes
    internal sealed class GnuSparseStream : Stream
    {
        private Stream _rawStream;
        private bool _isDisposed;
        private readonly long _realSize;
        private readonly (long Offset, long Length)[] _segments;
        private readonly long _dataStart; // byte offset in _rawStream where packed data begins

        private long _virtualPosition; // current position in the virtual (expanded) file

        // For non-seekable streams: tracks how many bytes of packed data have been consumed
        // so we can skip forward when there are holes between segments.
        private long _nextPackedOffset;

        private GnuSparseStream(Stream rawStream, long realSize, (long Offset, long Length)[] segments, long dataStart)
        {
            _rawStream = rawStream;
            _realSize = realSize;
            _segments = segments;
            _dataStart = dataStart;
        }

        // Creates a GnuSparseStream by parsing the sparse map from rawStream.
        // Returns null if rawStream is null (no data).
        // Throws InvalidDataException if the sparse map is malformed.
        internal static GnuSparseStream? TryCreate(Stream? rawStream, long realSize)
        {
            if (rawStream is null)
            {
                return null;
            }

            (var segments, long dataStart) = ParseSparseMap(rawStream);
            return new GnuSparseStream(rawStream, realSize, segments, dataStart);
        }

        // Asynchronously creates a GnuSparseStream by parsing the sparse map from rawStream.
        internal static async ValueTask<GnuSparseStream?> TryCreateAsync(Stream? rawStream, long realSize, CancellationToken cancellationToken)
        {
            if (rawStream is null)
            {
                return null;
            }

            (var segments, long dataStart) = await ParseSparseMapAsync(rawStream, cancellationToken).ConfigureAwait(false);
            return new GnuSparseStream(rawStream, realSize, segments, dataStart);
        }

        public override bool CanRead => !_isDisposed;
        public override bool CanSeek => _rawStream.CanSeek && !_isDisposed;
        public override bool CanWrite => false;

        public override long Length
        {
            get
            {
                ThrowIfDisposed();
                return _realSize;
            }
        }

        public override long Position
        {
            get
            {
                ThrowIfDisposed();
                return _virtualPosition;
            }
            set
            {
                ThrowIfDisposed();
                if (!_rawStream.CanSeek)
                {
                    throw new NotSupportedException(SR.IO_NotSupported_UnseekableStream);
                }
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                _virtualPosition = value;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfDisposed();
            if (!_rawStream.CanSeek)
            {
                throw new NotSupportedException(SR.IO_NotSupported_UnseekableStream);
            }

            long newPosition = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _virtualPosition + offset,
                SeekOrigin.End => _realSize + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin)),
            };

            if (newPosition < 0)
            {
                throw new IOException(SR.IO_SeekBeforeBegin);
            }

            _virtualPosition = newPosition;
            return _virtualPosition;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            return Read(buffer.AsSpan(offset, count));
        }

        public override int Read(Span<byte> destination)
        {
            ThrowIfDisposed();

            if (destination.IsEmpty || _virtualPosition >= _realSize)
            {
                return 0;
            }

            int toRead = (int)Math.Min(destination.Length, _realSize - _virtualPosition);
            destination = destination.Slice(0, toRead);

            int totalFilled = 0;
            while (totalFilled < toRead)
            {
                long vPos = _virtualPosition + totalFilled;
                int segIdx = FindSegment(vPos);

                if (segIdx < 0)
                {
                    // vPos is in a sparse hole — fill with zeros until the next segment or end of file.
                    long nextSegStart = ~segIdx < _segments.Length ? _segments[~segIdx].Offset : _realSize;
                    int zeroCount = (int)Math.Min(toRead - totalFilled, nextSegStart - vPos);
                    destination.Slice(totalFilled, zeroCount).Clear();
                    totalFilled += zeroCount;
                }
                else
                {
                    // vPos is within segment segIdx — read from packed data.
                    var (segOffset, segLength) = _segments[segIdx];
                    long offsetInSeg = vPos - segOffset;
                    long remainingInSeg = segLength - offsetInSeg;
                    int countToRead = (int)Math.Min(toRead - totalFilled, remainingInSeg);

                    long packedOffset = ComputePackedOffset(segIdx, offsetInSeg);
                    ReadFromPackedData(destination.Slice(totalFilled, countToRead), packedOffset);
                    totalFilled += countToRead;
                }
            }

            _virtualPosition += totalFilled;
            return totalFilled;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }
            ValidateBufferArguments(buffer, offset, count);
            return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }
            ThrowIfDisposed();
            if (buffer.IsEmpty || _virtualPosition >= _realSize)
            {
                return ValueTask.FromResult(0);
            }
            return ReadAsyncCore(buffer, cancellationToken);
        }

        private async ValueTask<int> ReadAsyncCore(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            int toRead = (int)Math.Min(buffer.Length, _realSize - _virtualPosition);
            buffer = buffer.Slice(0, toRead);

            int totalFilled = 0;
            while (totalFilled < toRead)
            {
                long vPos = _virtualPosition + totalFilled;
                int segIdx = FindSegment(vPos);

                if (segIdx < 0)
                {
                    long nextSegStart = ~segIdx < _segments.Length ? _segments[~segIdx].Offset : _realSize;
                    int zeroCount = (int)Math.Min(toRead - totalFilled, nextSegStart - vPos);
                    buffer.Slice(totalFilled, zeroCount).Span.Clear();
                    totalFilled += zeroCount;
                }
                else
                {
                    var (segOffset, segLength) = _segments[segIdx];
                    long offsetInSeg = vPos - segOffset;
                    long remainingInSeg = segLength - offsetInSeg;
                    int countToRead = (int)Math.Min(toRead - totalFilled, remainingInSeg);

                    long packedOffset = ComputePackedOffset(segIdx, offsetInSeg);
                    await ReadFromPackedDataAsync(buffer.Slice(totalFilled, countToRead), packedOffset, cancellationToken).ConfigureAwait(false);
                    totalFilled += countToRead;
                }
            }

            _virtualPosition += totalFilled;
            return totalFilled;
        }

        // When the caller skips this entry without reading the DataStream,
        // we advance the underlying SubReadStream to its end so the archive
        // stream pointer moves to the start of the next entry.
        // Returns the underlying SubReadStream for callers that need to advance it,
        // or null if the raw stream is not a SubReadStream (e.g., seekable or copied).
        internal SubReadStream? AdvanceToEndAndGetSubReadStream() =>
            _rawStream as SubReadStream;

        // Reads countToRead bytes from the packed data at the given packedOffset.
        // For seekable streams, seeks to the correct position.
        // For non-seekable streams, advances sequentially (skipping if necessary).
        private void ReadFromPackedData(Span<byte> destination, long packedOffset)
        {
            if (_rawStream.CanSeek)
            {
                _rawStream.Seek(_dataStart + packedOffset, SeekOrigin.Begin);
                _rawStream.ReadExactly(destination);
            }
            else
            {
                // Sequential reading: skip over any bytes that belong to holes between segments.
                long skipBytes = packedOffset - _nextPackedOffset;
                Debug.Assert(skipBytes >= 0, "Non-seekable stream read went backwards in packed data.");
                if (skipBytes > 0)
                {
                    TarHelpers.AdvanceStream(_rawStream, skipBytes);
                }
                _rawStream.ReadExactly(destination);
                _nextPackedOffset = packedOffset + destination.Length;
            }
        }

        private async ValueTask ReadFromPackedDataAsync(Memory<byte> destination, long packedOffset, CancellationToken cancellationToken)
        {
            if (_rawStream.CanSeek)
            {
                _rawStream.Seek(_dataStart + packedOffset, SeekOrigin.Begin);
                await _rawStream.ReadExactlyAsync(destination, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                long skipBytes = packedOffset - _nextPackedOffset;
                Debug.Assert(skipBytes >= 0, "Non-seekable stream read went backwards in packed data.");
                if (skipBytes > 0)
                {
                    await TarHelpers.AdvanceStreamAsync(_rawStream, skipBytes, cancellationToken).ConfigureAwait(false);
                }
                await _rawStream.ReadExactlyAsync(destination, cancellationToken).ConfigureAwait(false);
                _nextPackedOffset = packedOffset + destination.Length;
            }
        }

        // Binary searches for the segment containing virtualPosition.
        // Returns the segment index if found, or the bitwise complement of the
        // insertion point (a negative number) if virtualPosition is in a hole.
        private int FindSegment(long virtualPosition)
        {
            int lo = 0, hi = _segments.Length - 1;
            while (lo <= hi)
            {
                int mid = lo + (hi - lo) / 2;
                var (offset, length) = _segments[mid];
                if (virtualPosition < offset)
                {
                    hi = mid - 1;
                }
                else if (virtualPosition >= offset + length)
                {
                    lo = mid + 1;
                }
                else
                {
                    return mid;
                }
            }
            return ~lo;
        }

        // Computes the offset of a byte within the packed data (i.e., relative to _dataStart).
        // segIdx is the segment index; offsetInSeg is the byte offset within that segment.
        private long ComputePackedOffset(int segIdx, long offsetInSeg)
        {
            long offset = 0;
            for (int i = 0; i < segIdx; i++)
            {
                offset += _segments[i].Length;
            }
            return offset + offsetInSeg;
        }

        // Parses the sparse map from rawStream (positioned at start).
        // The map format is: numSegments\n, then pairs of offset\n numbytes\n.
        // After the map text, there is zero-padding to the next 512-byte block boundary,
        // and then the packed data begins.
        // Returns the parsed segments and the data start offset in rawStream.
        private static ((long Offset, long Length)[] Segments, long DataStart) ParseSparseMap(Stream rawStream)
        {
            long bytesConsumed = 0;

            long numSegments = ReadDecimalLine(rawStream, ref bytesConsumed);
            if (numSegments < 0)
            {
                throw new InvalidDataException(SR.TarGnuSparseMapInvalidNumSegments);
            }

            var segments = new (long Offset, long Length)[numSegments];
            for (long i = 0; i < numSegments; i++)
            {
                long offset = ReadDecimalLine(rawStream, ref bytesConsumed);
                long length = ReadDecimalLine(rawStream, ref bytesConsumed);
                if (offset < 0 || length < 0)
                {
                    throw new InvalidDataException(SR.TarGnuSparseMapInvalidSegment);
                }
                segments[i] = (offset, length);
            }

            // Skip padding bytes to align to the next 512-byte block boundary.
            int padding = TarHelpers.CalculatePadding(bytesConsumed);
            if (padding > 0)
            {
                TarHelpers.AdvanceStream(rawStream, padding);
            }

            long dataStart = bytesConsumed + padding;
            return (segments, dataStart);
        }

        private static async ValueTask<((long Offset, long Length)[] Segments, long DataStart)> ParseSparseMapAsync(Stream rawStream, CancellationToken cancellationToken)
        {
            long bytesConsumed = 0;

            (long numSegments, long numSegBytes) = await ReadDecimalLineAsync(rawStream, cancellationToken).ConfigureAwait(false);
            bytesConsumed += numSegBytes;

            if (numSegments < 0)
            {
                throw new InvalidDataException(SR.TarGnuSparseMapInvalidNumSegments);
            }

            var segments = new (long Offset, long Length)[numSegments];
            for (long i = 0; i < numSegments; i++)
            {
                (long offset, long offsetBytes) = await ReadDecimalLineAsync(rawStream, cancellationToken).ConfigureAwait(false);
                bytesConsumed += offsetBytes;
                (long length, long lengthBytes) = await ReadDecimalLineAsync(rawStream, cancellationToken).ConfigureAwait(false);
                bytesConsumed += lengthBytes;
                if (offset < 0 || length < 0)
                {
                    throw new InvalidDataException(SR.TarGnuSparseMapInvalidSegment);
                }
                segments[i] = (offset, length);
            }

            int padding = TarHelpers.CalculatePadding(bytesConsumed);
            if (padding > 0)
            {
                await TarHelpers.AdvanceStreamAsync(rawStream, padding, cancellationToken).ConfigureAwait(false);
            }

            long dataStart = bytesConsumed + padding;
            return (segments, dataStart);
        }

        // Reads one newline-terminated decimal integer from rawStream.
        // Increments bytesConsumed by the number of bytes read (including the '\n').
        // Throws InvalidDataException if the line is malformed.
        private static long ReadDecimalLine(Stream stream, ref long bytesConsumed)
        {
            long value = 0;
            bool hasDigits = false;

            while (true)
            {
                int b = stream.ReadByte();
                if (b == -1)
                {
                    throw new InvalidDataException(SR.TarGnuSparseMapInvalidSegment);
                }
                bytesConsumed++;

                if (b == '\n')
                {
                    break;
                }

                if ((uint)(b - '0') > 9u)
                {
                    throw new InvalidDataException(SR.TarGnuSparseMapInvalidSegment);
                }

                value = checked(value * 10 + (b - '0'));
                hasDigits = true;
            }

            if (!hasDigits)
            {
                throw new InvalidDataException(SR.TarGnuSparseMapInvalidSegment);
            }

            return value;
        }

        // Returns (value, bytesRead) for one newline-terminated decimal integer.
        private static async ValueTask<(long Value, long BytesRead)> ReadDecimalLineAsync(Stream stream, CancellationToken cancellationToken)
        {
            byte[] singleByte = new byte[1];
            long value = 0;
            long bytesRead = 0;
            bool hasDigits = false;

            while (true)
            {
                int read = await stream.ReadAsync(singleByte, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new InvalidDataException(SR.TarGnuSparseMapInvalidSegment);
                }
                bytesRead++;

                int b = singleByte[0];
                if (b == '\n')
                {
                    break;
                }

                if ((uint)(b - '0') > 9u)
                {
                    throw new InvalidDataException(SR.TarGnuSparseMapInvalidSegment);
                }

                value = checked(value * 10 + (b - '0'));
                hasDigits = true;
            }

            if (!hasDigits)
            {
                throw new InvalidDataException(SR.TarGnuSparseMapInvalidSegment);
            }

            return (value, bytesRead);
        }

        protected override void Dispose(bool disposing)
        {
            _isDisposed = true;
            base.Dispose(disposing);
        }

        public override void Flush() { }

        public override Task FlushAsync(CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;

        public override void SetLength(long value) => throw new NotSupportedException(SR.IO_NotSupported_UnwritableStream);

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException(SR.IO_NotSupported_UnwritableStream);

        private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
}
