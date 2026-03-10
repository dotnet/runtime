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
        // Caps the segment count to prevent excessive memory allocation from malformed archives.
        // Each segment entry in the array occupies 16 bytes, so 1M segments = 16 MB.
        private const int MaxSparseSegments = 1_000_000;

        private readonly Stream _rawStream;
        private bool _isDisposed;
        private readonly long _realSize;
        private readonly (long Offset, long Length)[] _segments;
        private readonly long _dataStart; // byte offset in _rawStream where packed data begins

        // Cumulative sum of segment lengths: _packedStartOffsets[i] is the packed-data offset
        // of the first byte of segment i. Allows O(1) ComputePackedOffset lookups.
        private readonly long[] _packedStartOffsets;

        private long _virtualPosition; // current position in the virtual (expanded) file

        // For non-seekable streams: tracks how many bytes of packed data have been consumed
        // so we can skip forward when there are holes between segments.
        private long _nextPackedOffset;

        // Cached segment index for sequential read optimization.
        // For typical forward sequential reads, this avoids repeated binary searches.
        private int _currentSegmentIndex;

        private GnuSparseStream(Stream rawStream, long realSize, (long Offset, long Length)[] segments, long dataStart)
        {
            _rawStream = rawStream;
            _realSize = realSize;
            _segments = segments;
            _dataStart = dataStart;

            // Precompute packed-data start offsets for O(1) lookup during reads.
            _packedStartOffsets = new long[segments.Length];
            long sum = 0;
            for (int i = 0; i < segments.Length; i++)
            {
                _packedStartOffsets[i] = sum;
                sum += segments[i].Length;
            }
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
        public override bool CanSeek => !_isDisposed && _rawStream.CanSeek;
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
                _currentSegmentIndex = 0; // Reset segment hint after seek
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
            _currentSegmentIndex = 0; // Reset segment hint after seek
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
                int segIdx = FindSegmentFromCurrent(vPos);

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

                    long packedOffset = _packedStartOffsets[segIdx] + offsetInSeg;
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
                int segIdx = FindSegmentFromCurrent(vPos);

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

                    long packedOffset = _packedStartOffsets[segIdx] + offsetInSeg;
                    await ReadFromPackedDataAsync(buffer.Slice(totalFilled, countToRead), packedOffset, cancellationToken).ConfigureAwait(false);
                    totalFilled += countToRead;
                }
            }

            _virtualPosition += totalFilled;
            return totalFilled;
        }

        // Returns the underlying SubReadStream for advancing stream position between entries.
        // Returns null if the raw stream is not a SubReadStream (e.g., seekable or copied).
        internal SubReadStream? GetSubReadStream() =>
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

        // Finds the segment containing virtualPosition, using _currentSegmentIndex as a hint
        // to optimize sequential reads by scanning forward before falling back to binary search.
        // Returns the segment index if found, or the bitwise complement of the
        // insertion point (a negative number) if virtualPosition is in a hole.
        private int FindSegmentFromCurrent(long virtualPosition)
        {
            // If the cached index is past the position (e.g. after a seek), reset to binary search.
            if (_currentSegmentIndex > 0 && _currentSegmentIndex < _segments.Length &&
                virtualPosition < _segments[_currentSegmentIndex].Offset)
            {
                _currentSegmentIndex = 0;
            }

            // Scan forward from the current cached index (optimal for sequential reads).
            while (_currentSegmentIndex < _segments.Length)
            {
                var (offset, length) = _segments[_currentSegmentIndex];
                if (virtualPosition < offset)
                {
                    // Position is in a hole before the current segment.
                    return ~_currentSegmentIndex;
                }
                if (virtualPosition < offset + length)
                {
                    // Position is within the current segment.
                    return _currentSegmentIndex;
                }
                // Position is past this segment; advance to the next.
                _currentSegmentIndex++;
            }
            return ~_segments.Length; // Past all segments.
        }

        // Parses the sparse map from rawStream (positioned at start).
        // The map format is: numSegments\n, then pairs of offset\n numbytes\n.
        // After the map text, there is zero-padding to the next 512-byte block boundary,
        // and then the packed data begins.
        // Returns the parsed segments and the data start offset in rawStream.
        private static ((long Offset, long Length)[] Segments, long DataStart) ParseSparseMap(Stream rawStream)
        {
            // Sliding-window buffer: bytes[activeStart..availableStart) are unconsumed data.
            byte[] bytes = new byte[512];
            int activeStart = 0;
            int availableStart = 0;
            long totalBytesReadFromStream = 0;

            // Refills the buffer by compacting and reading from the stream.
            void FillBuffer()
            {
                // Compact: move unread bytes to the front.
                int active = availableStart - activeStart;
                if (active > 0 && activeStart > 0)
                {
                    bytes.AsSpan(activeStart, active).CopyTo(bytes);
                }
                activeStart = 0;
                availableStart = active;

                int read = rawStream.Read(bytes, availableStart, bytes.Length - availableStart);
                availableStart += read;
                totalBytesReadFromStream += read;
            }

            // Reads a newline-terminated decimal line from the buffer, refilling as needed.
            // Returns the parsed value. Throws InvalidDataException if the line is malformed.
            long ReadLine()
            {
                while (true)
                {
                    int nlIdx = bytes.AsSpan(activeStart, availableStart - activeStart).IndexOf((byte)'\n');
                    if (nlIdx >= 0)
                    {
                        long value = ParseDecimalSpan(bytes.AsSpan(activeStart, nlIdx));
                        activeStart += nlIdx + 1;
                        return value;
                    }

                    if (availableStart == bytes.Length)
                    {
                        // Buffer full but no newline: sparse map line is too long (malformed).
                        throw new InvalidDataException(SR.TarInvalidNumber);
                    }

                    FillBuffer();

                    if (availableStart == activeStart)
                    {
                        // EOF reached before newline.
                        throw new InvalidDataException(SR.TarInvalidNumber);
                    }
                }
            }

            FillBuffer();

            long numSegments = ReadLine();
            if ((ulong)numSegments > MaxSparseSegments)
            {
                throw new InvalidDataException(SR.TarInvalidNumber);
            }

            var segments = new (long Offset, long Length)[numSegments];
            for (int i = 0; i < (int)numSegments; i++)
            {
                long offset = ReadLine();
                long length = ReadLine();
                if (offset < 0 || length < 0)
                {
                    throw new InvalidDataException(SR.TarInvalidNumber);
                }
                segments[i] = (offset, length);
            }

            // The number of bytes logically consumed from the sparse map is totalBytesReadFromStream - buffered-but-unread.
            long mapBytesConsumed = totalBytesReadFromStream - (availableStart - activeStart);

            // Skip padding bytes to align to the next 512-byte block boundary.
            // Some padding bytes may already be in the buffer; skip those first.
            int padding = TarHelpers.CalculatePadding(mapBytesConsumed);
            int paddingInBuffer = Math.Min(padding, availableStart - activeStart);
            int paddingFromStream = padding - paddingInBuffer;
            if (paddingFromStream > 0)
            {
                TarHelpers.AdvanceStream(rawStream, paddingFromStream);
            }

            long dataStart = mapBytesConsumed + padding;

            // For seekable streams, seek to the exact dataStart position so ReadFromPackedData
            // can seek correctly even if we buffered ahead into the packed data region.
            if (rawStream.CanSeek)
            {
                rawStream.Seek(dataStart, SeekOrigin.Begin);
            }
            // For non-seekable streams, the buffer is exactly 512 bytes wide and dataStart is
            // always a multiple of 512, so after consuming the padding there are no bytes left
            // in the buffer that belong to the packed data region.

            return (segments, dataStart);
        }

        private static async ValueTask<((long Offset, long Length)[] Segments, long DataStart)> ParseSparseMapAsync(Stream rawStream, CancellationToken cancellationToken)
        {
            byte[] bytes = new byte[512];
            int activeStart = 0;
            int availableStart = 0;
            long totalBytesReadFromStream = 0;

            async ValueTask FillBufferAsync()
            {
                int active = availableStart - activeStart;
                if (active > 0 && activeStart > 0)
                {
                    bytes.AsSpan(activeStart, active).CopyTo(bytes);
                }
                activeStart = 0;
                availableStart = active;

                int read = await rawStream.ReadAsync(bytes.AsMemory(availableStart, bytes.Length - availableStart), cancellationToken).ConfigureAwait(false);
                availableStart += read;
                totalBytesReadFromStream += read;
            }

            async ValueTask<long> ReadLineAsync()
            {
                while (true)
                {
                    int nlIdx = bytes.AsSpan(activeStart, availableStart - activeStart).IndexOf((byte)'\n');
                    if (nlIdx >= 0)
                    {
                        long value = ParseDecimalSpan(bytes.AsSpan(activeStart, nlIdx));
                        activeStart += nlIdx + 1;
                        return value;
                    }

                    if (availableStart == bytes.Length)
                    {
                        throw new InvalidDataException(SR.TarInvalidNumber);
                    }

                    await FillBufferAsync().ConfigureAwait(false);

                    if (availableStart == activeStart)
                    {
                        throw new InvalidDataException(SR.TarInvalidNumber);
                    }
                }
            }

            await FillBufferAsync().ConfigureAwait(false);

            long numSegments = await ReadLineAsync().ConfigureAwait(false);
            if ((ulong)numSegments > MaxSparseSegments)
            {
                throw new InvalidDataException(SR.TarInvalidNumber);
            }

            var segments = new (long Offset, long Length)[numSegments];
            for (int i = 0; i < (int)numSegments; i++)
            {
                long offset = await ReadLineAsync().ConfigureAwait(false);
                long length = await ReadLineAsync().ConfigureAwait(false);
                if (offset < 0 || length < 0)
                {
                    throw new InvalidDataException(SR.TarInvalidNumber);
                }
                segments[i] = (offset, length);
            }

            long mapBytesConsumed = totalBytesReadFromStream - (availableStart - activeStart);
            int padding = TarHelpers.CalculatePadding(mapBytesConsumed);
            int paddingInBuffer = Math.Min(padding, availableStart - activeStart);
            int paddingFromStream = padding - paddingInBuffer;
            if (paddingFromStream > 0)
            {
                await TarHelpers.AdvanceStreamAsync(rawStream, paddingFromStream, cancellationToken).ConfigureAwait(false);
            }

            long dataStart = mapBytesConsumed + padding;

            if (rawStream.CanSeek)
            {
                rawStream.Seek(dataStart, SeekOrigin.Begin);
            }

            return (segments, dataStart);
        }

        // Parses a decimal integer from a span of ASCII digits. Throws InvalidDataException if the span is
        // empty or contains non-digit characters.
        private static long ParseDecimalSpan(ReadOnlySpan<byte> span)
        {
            if (span.IsEmpty)
            {
                throw new InvalidDataException(SR.TarInvalidNumber);
            }

            long value = 0;
            foreach (byte b in span)
            {
                if ((uint)(b - '0') > 9u)
                {
                    throw new InvalidDataException(SR.TarInvalidNumber);
                }
                value = checked(value * 10 + (b - '0'));
            }
            return value;
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
