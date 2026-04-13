// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
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

        // Sparse map state — initialized lazily on first Read to avoid consuming the raw
        // stream before TarWriter has a chance to copy the condensed data.
        private (long Offset, long Length)[]? _segments;
        private long[]? _packedStartOffsets;

        private long _virtualPosition; // current position in the virtual (expanded) file

        // For non-seekable streams: tracks how many bytes of packed data have been consumed
        // so we can skip forward when there are holes between segments.
        private long _nextPackedOffset;

        // Cached segment index for sequential read optimization.
        // For typical forward sequential reads, this avoids repeated binary searches.
        private int _currentSegmentIndex;

        internal GnuSparseStream(Stream rawStream, long realSize)
        {
            _rawStream = rawStream;
            _realSize = realSize;
        }

        // Parses the sparse map on first read. Populates _segments, _packedStartOffsets,
        // and _dataStart. Throws InvalidDataException if the sparse map is malformed.
        private void EnsureInitialized()
        {
            if (_segments is not null)
            {
                return;
            }

            var segments = ParseSparseMap(isAsync: false, _rawStream, CancellationToken.None).GetAwaiter().GetResult();
            InitializeFromParsedMap(segments);
        }

        private async ValueTask EnsureInitializedAsync(CancellationToken cancellationToken)
        {
            if (_segments is not null)
            {
                return;
            }

            var segments = await ParseSparseMap(isAsync: true, _rawStream, cancellationToken).ConfigureAwait(false);
            InitializeFromParsedMap(segments);
        }

        private void InitializeFromParsedMap((long Offset, long Length)[] segments)
        {
            _packedStartOffsets = new long[segments.Length];
            long sum = 0;
            long previousEnd = 0;
            for (int i = 0; i < segments.Length; i++)
            {
                var (offset, length) = segments[i];

                // Validate segment ordering and bounds. Avoid overflow by checking length separately.
                if (offset < previousEnd || offset > _realSize || length > _realSize - offset)
                {
                    throw new InvalidDataException(SR.TarInvalidNumber);
                }
                previousEnd = offset + length;

                _packedStartOffsets[i] = sum;
                try
                {
                    sum = checked(sum + length);
                }
                catch (OverflowException ex)
                {
                    throw new InvalidDataException(SR.TarInvalidNumber, ex);
                }
            }
            // Assign _segments last — it serves as the initialization flag.
            _segments = segments;
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
                // _currentSegmentIndex is not reset here; FindSegmentFromCurrent handles
                // backward seeks using binary search.
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
            // _currentSegmentIndex is not reset here; FindSegmentFromCurrent handles
            // backward seeks using binary search.
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
            EnsureInitialized();
            Debug.Assert(_segments is not null && _packedStartOffsets is not null);

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
                    int bytesRead = ReadFromPackedData(destination.Slice(totalFilled, countToRead), packedOffset);
                    totalFilled += bytesRead;
                    break; // Return after an underlying read; caller can call Read again for more.
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
            await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
            Debug.Assert(_segments is not null && _packedStartOffsets is not null);

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
                    int bytesRead = await ReadFromPackedDataAsync(buffer.Slice(totalFilled, countToRead), packedOffset, cancellationToken).ConfigureAwait(false);
                    totalFilled += bytesRead;
                    break;
                }
            }

            _virtualPosition += totalFilled;
            return totalFilled;
        }

        // Exposes the underlying raw stream for callers that need to access the condensed data.
        internal Stream BaseStream => _rawStream;

        // Reads from the packed data at the given packedOffset.
        // After EnsureInitialized, the raw stream is positioned at _dataStart and
        // _nextPackedOffset tracks how far into the packed data we've read.
        // Returns the number of bytes actually read (may be less than destination.Length).
        private int ReadFromPackedData(Span<byte> destination, long packedOffset)
        {
            long skipBytes = packedOffset - _nextPackedOffset;
            if (skipBytes < 0 && !_rawStream.CanSeek)
            {
                throw new InvalidOperationException(SR.IO_NotSupported_UnseekableStream);
            }
            if (skipBytes != 0)
            {
                TarHelpers.AdvanceStream(_rawStream, skipBytes);
            }
            int bytesRead = _rawStream.Read(destination);
            _nextPackedOffset = packedOffset + bytesRead;
            return bytesRead;
        }

        private async ValueTask<int> ReadFromPackedDataAsync(Memory<byte> destination, long packedOffset, CancellationToken cancellationToken)
        {
            long skipBytes = packedOffset - _nextPackedOffset;
            if (skipBytes < 0 && !_rawStream.CanSeek)
            {
                throw new InvalidOperationException(SR.IO_NotSupported_UnseekableStream);
            }
            if (skipBytes != 0)
            {
                await TarHelpers.AdvanceStreamAsync(_rawStream, skipBytes, cancellationToken).ConfigureAwait(false);
            }
            int bytesRead = await _rawStream.ReadAsync(destination, cancellationToken).ConfigureAwait(false);
            _nextPackedOffset = packedOffset + bytesRead;
            return bytesRead;
        }

        // Finds the segment containing virtualPosition.
        // Uses a sequential hint (_currentSegmentIndex) for O(1) amortized forward reads,
        // and falls back to binary search when seeking backward or jumping into already-passed regions.
        // Returns the segment index if found, or the bitwise complement of the
        // insertion point (a negative number) if virtualPosition is in a hole.
        private int FindSegmentFromCurrent(long virtualPosition)
        {
            Debug.Assert(_segments is not null);

            if (_segments.Length == 0)
            {
                return ~0;
            }

            // If the hint is past all segments, check for backward seek.
            if (_currentSegmentIndex >= _segments.Length)
            {
                long lastEnd = _segments[_segments.Length - 1].Offset + _segments[_segments.Length - 1].Length;
                if (virtualPosition >= lastEnd)
                {
                    // Still in the trailing hole — no search needed.
                    return ~_segments.Length;
                }
                // Seeked back into segment range; use binary search over the full array.
                int result = BinarySearchSegment(virtualPosition, 0, _segments.Length - 1);
                _currentSegmentIndex = result >= 0 ? result : ~result;
                return result;
            }

            // If position is before the current hint, use binary search (handles backward seeks).
            if (virtualPosition < _segments[_currentSegmentIndex].Offset)
            {
                int result = BinarySearchSegment(virtualPosition, 0, _currentSegmentIndex - 1);
                _currentSegmentIndex = result >= 0 ? result : ~result;
                return result;
            }

            // Scan forward from the current hint (O(1) amortized for sequential reads).
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

        // Binary search over sorted segments in the range [lo, hi].
        // Returns the segment index if found, or ~insertionPoint if virtualPosition is in a hole.
        private int BinarySearchSegment(long virtualPosition, int lo, int hi)
        {
            Debug.Assert(_segments is not null);
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

        // Parses the sparse map from rawStream (positioned at the start of the data section).
        // The map format is: numSegments\n, then pairs of offset\n numbytes\n.
        // After the map text, there is zero-padding to the next 512-byte block boundary,
        // and then the packed data begins.
        //
        // Returns the parsed segments.
        private static async Task<(long Offset, long Length)[]> ParseSparseMap(
            bool isAsync, Stream rawStream, CancellationToken cancellationToken)
        {
            // The buffer is 2 * RecordSize (1024 bytes) and each fill reads exactly RecordSize (512)
            // bytes. This guarantees that the total bytes read is always a multiple of RecordSize,
            // so the stream is already positioned at the start of the packed data when this method returns.
            int bufferSize = 2 * TarHelpers.RecordSize;
            byte[] bytes = ArrayPool<byte>.Shared.Rent(bufferSize);

            try
            {
                int activeStart = 0;
                int availableStart = 0;

                // Compact the buffer and read exactly one RecordSize (512) block.
                // Returns true if bytes were read, false on EOF.
                async ValueTask<bool> FillBufferAsync()
                {
                    int active = availableStart - activeStart;
                    if (active > 0 && activeStart > 0)
                    {
                        bytes.AsSpan(activeStart, active).CopyTo(bytes);
                    }
                    activeStart = 0;
                    availableStart = active;

                    int newBytes = isAsync
                        ? await rawStream.ReadAtLeastAsync(bytes.AsMemory(availableStart, TarHelpers.RecordSize), TarHelpers.RecordSize, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false)
                        : rawStream.ReadAtLeast(bytes.AsSpan(availableStart, TarHelpers.RecordSize), TarHelpers.RecordSize, throwOnEndOfStream: false);

                    availableStart += newBytes;
                    return newBytes > 0;
                }

                // Reads a newline-terminated decimal line from the buffer, refilling as needed.
                // Returns the parsed value. Throws InvalidDataException if the line is malformed.
                async ValueTask<long> ReadLineAsync()
                {
                    while (true)
                    {
                        int nlIdx = bytes.AsSpan(activeStart, availableStart - activeStart).IndexOf((byte)'\n');
                        if (nlIdx >= 0)
                        {
                            ReadOnlySpan<byte> span = bytes.AsSpan(activeStart, nlIdx);
                            if (!Utf8Parser.TryParse(span, out long value, out int consumed) || consumed != span.Length)
                            {
                                throw new InvalidDataException(SR.TarInvalidNumber);
                            }
                            activeStart += nlIdx + 1;
                            return value;
                        }

                        if (availableStart + TarHelpers.RecordSize > bufferSize)
                        {
                            // Not enough room in the buffer for another block-sized fill
                            // and no newline found: line is too long (malformed).
                            throw new InvalidDataException(SR.TarInvalidNumber);
                        }

                        if (!await FillBufferAsync().ConfigureAwait(false))
                        {
                            // EOF before newline.
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

                var segments = new (long Offset, long Length)[(int)numSegments];
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

                // Since each FillBuffer call reads exactly RecordSize (512) bytes, the total bytes
                // read is always a multiple of RecordSize (mapBytesConsumed + padding), so the stream
                // is already positioned at the start of the packed data.
                return segments;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                _rawStream.Dispose();
            }
            _isDisposed = true;
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (!_isDisposed)
            {
                await _rawStream.DisposeAsync().ConfigureAwait(false);
            }
            _isDisposed = true;
            await base.DisposeAsync().ConfigureAwait(false);
        }

        public override void Flush() { }

        public override Task FlushAsync(CancellationToken cancellationToken) =>
            cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;

        public override void SetLength(long value) => throw new NotSupportedException(SR.IO_NotSupported_UnwritableStream);

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException(SR.IO_NotSupported_UnwritableStream);

        private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
    }
}
