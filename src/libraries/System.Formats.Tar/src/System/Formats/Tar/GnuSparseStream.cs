// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

            (var segments, long dataStart) = ParseSparseMap(isAsync: false, rawStream, CancellationToken.None).GetAwaiter().GetResult();
            return new GnuSparseStream(rawStream, realSize, segments, dataStart);
        }

        // Asynchronously creates a GnuSparseStream by parsing the sparse map from rawStream.
        internal static async ValueTask<GnuSparseStream?> TryCreateAsync(Stream? rawStream, long realSize, CancellationToken cancellationToken)
        {
            if (rawStream is null)
            {
                return null;
            }

            (var segments, long dataStart) = await ParseSparseMap(isAsync: true, rawStream, cancellationToken).ConfigureAwait(false);
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

        // Parses the sparse map from rawStream (positioned at the start of the data section).
        // The map format is: numSegments\n, then pairs of offset\n numbytes\n.
        // After the map text, there is zero-padding to the next 512-byte block boundary,
        // and then the packed data begins.
        //
        // The buffer is 2 * RecordSize (1024 bytes) and each fill reads exactly RecordSize (512)
        // bytes. This guarantees that totalBytesRead is always a multiple of RecordSize and
        // equals dataStart (mapBytesConsumed + padding), so no corrective seeking is needed.
        //
        // Returns the parsed segments and the data-start offset in rawStream.
        private static async Task<((long Offset, long Length)[] Segments, long DataStart)> ParseSparseMap(
            bool isAsync, Stream rawStream, CancellationToken cancellationToken)
        {
            byte[] bytes = new byte[2 * TarHelpers.RecordSize];
            int activeStart = 0;
            int availableStart = 0;
            long totalBytesRead = 0;

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
                totalBytesRead += newBytes;
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

                    if (availableStart == bytes.Length)
                    {
                        // Buffer full but no newline: line is too long (malformed).
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

            // Since each FillBuffer call reads exactly RecordSize (512) bytes, totalBytesRead
            // is always a multiple of RecordSize. It equals mapBytesConsumed + padding = dataStart,
            // so the stream is already positioned at the start of the packed data.
            return (segments, totalBytesRead);
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
