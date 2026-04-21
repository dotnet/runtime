// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    internal sealed class WrappedStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly bool _closeBaseStream;

        // Delegate that will be invoked on stream disposing
        private readonly Action<ZipArchiveEntry?>? _onClosed;

        // When true, notifies the entry on first write operation
        private bool _notifyEntryOnWrite;

        // Instance that will be passed to _onClose delegate
        private readonly ZipArchiveEntry? _zipArchiveEntry;
        private bool _isDisposed;

        internal WrappedStream(Stream baseStream, bool closeBaseStream)
            : this(baseStream, closeBaseStream, entry: null, onClosed: null, notifyEntryOnWrite: false) { }

        private WrappedStream(Stream baseStream, bool closeBaseStream, ZipArchiveEntry? entry, Action<ZipArchiveEntry?>? onClosed, bool notifyEntryOnWrite)
        {
            _baseStream = baseStream;
            _closeBaseStream = closeBaseStream;
            _onClosed = onClosed;
            _notifyEntryOnWrite = notifyEntryOnWrite;
            _zipArchiveEntry = entry;
            _isDisposed = false;
        }

        internal WrappedStream(Stream baseStream, ZipArchiveEntry entry, Action<ZipArchiveEntry?>? onClosed, bool notifyEntryOnWrite = false)
            : this(baseStream, false, entry, onClosed, notifyEntryOnWrite) { }

        public override long Length
        {
            get
            {
                ThrowIfDisposed();
                return _baseStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                ThrowIfDisposed();
                return _baseStream.Position;
            }
            set
            {
                ThrowIfDisposed();
                ThrowIfCantSeek();

                _baseStream.Position = value;
            }
        }

        public override bool CanRead => !_isDisposed && _baseStream.CanRead;

        public override bool CanSeek => !_isDisposed && _baseStream.CanSeek;

        public override bool CanWrite => !_isDisposed && _baseStream.CanWrite;

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().ToString(), SR.HiddenStreamName);
        }

        private void ThrowIfCantRead()
        {
            if (!CanRead)
                throw new NotSupportedException(SR.ReadingNotSupported);
        }

        private void ThrowIfCantWrite()
        {
            if (!CanWrite)
                throw new NotSupportedException(SR.WritingNotSupported);
        }

        private void ThrowIfCantSeek()
        {
            if (!CanSeek)
                throw new NotSupportedException(SR.SeekingNotSupported);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            ThrowIfCantRead();

            return _baseStream.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            ThrowIfDisposed();
            ThrowIfCantRead();

            return _baseStream.Read(buffer);
        }

        public override int ReadByte()
        {
            ThrowIfDisposed();
            ThrowIfCantRead();

            return _baseStream.ReadByte();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            ThrowIfCantRead();

            return _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfCantRead();

            return _baseStream.ReadAsync(buffer, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfDisposed();
            ThrowIfCantSeek();

            return _baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            ThrowIfDisposed();
            ThrowIfCantSeek();
            ThrowIfCantWrite();

            NotifyWrite();
            _baseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            ThrowIfCantWrite();

            NotifyWrite();
            _baseStream.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> source)
        {
            ThrowIfDisposed();
            ThrowIfCantWrite();

            NotifyWrite();
            _baseStream.Write(source);
        }

        public override void WriteByte(byte value)
        {
            ThrowIfDisposed();
            ThrowIfCantWrite();

            NotifyWrite();
            _baseStream.WriteByte(value);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            ThrowIfCantWrite();

            NotifyWrite();
            return _baseStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ThrowIfCantWrite();

            NotifyWrite();
            return _baseStream.WriteAsync(buffer, cancellationToken);
        }

        private void NotifyWrite()
        {
            if (_notifyEntryOnWrite)
            {
                _zipArchiveEntry?.MarkAsModified();
                _notifyEntryOnWrite = false; // Only notify once
            }
        }

        public override void Flush()
        {
            ThrowIfDisposed();
            ThrowIfCantWrite();

            _baseStream.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            ThrowIfCantWrite();

            return _baseStream.FlushAsync(cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                _onClosed?.Invoke(_zipArchiveEntry);

                if (_closeBaseStream)
                    _baseStream.Dispose();

                _isDisposed = true;
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (!_isDisposed)
            {
                _onClosed?.Invoke(_zipArchiveEntry);

                if (_closeBaseStream)
                    await _baseStream.DisposeAsync().ConfigureAwait(false);

                _isDisposed = true;
            }
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    internal sealed class CheckSumAndSizeWriteStream : Stream
    {
        private readonly Func<Stream> _baseStreamFactory;
        private Stream? _baseStream;
        private readonly Stream _baseBaseStream;
        private long _position;
        private uint _checksum;

        private readonly bool _leaveOpenOnClose;
        private readonly bool _canWrite;
        private bool _isDisposed;

        private bool _everWritten;

        // this is the position in BaseBaseStream
        private long _initialPosition;
        private readonly ZipArchiveEntry _zipArchiveEntry;
        private readonly EventHandler? _onClose;
        // Called when the stream is closed.
        // parameters are initialPosition, currentPosition, checkSum, baseBaseStream, zipArchiveEntry and onClose handler
        private readonly Action<long, long, uint, Stream, ZipArchiveEntry, EventHandler?> _saveCrcAndSizes;

        // parameters to saveCrcAndSizes are
        // initialPosition (initialPosition in baseBaseStream),
        // currentPosition (in this CheckSumAndSizeWriteStream),
        // checkSum (of data passed into this CheckSumAndSizeWriteStream),
        // baseBaseStream it's a backingStream, passed here so as to avoid closure allocation,
        // zipArchiveEntry passed here so as to avoid closure allocation,
        // onClose handler passed here so as to avoid closure allocation
        public CheckSumAndSizeWriteStream(Func<Stream> baseStreamFactory, Stream baseBaseStream, bool leaveOpenOnClose,
            ZipArchiveEntry entry, EventHandler? onClose,
            Action<long, long, uint, Stream, ZipArchiveEntry, EventHandler?> saveCrcAndSizes)
        {
            _baseStreamFactory = baseStreamFactory;
            _baseBaseStream = baseBaseStream;
            _position = 0;
            _checksum = 0;
            _leaveOpenOnClose = leaveOpenOnClose;
            _canWrite = true;
            _isDisposed = false;
            _initialPosition = 0;
            _zipArchiveEntry = entry;
            _onClose = onClose;
            _saveCrcAndSizes = saveCrcAndSizes;
        }

        public override long Length
        {
            get
            {
                ThrowIfDisposed();
                throw new NotSupportedException(SR.SeekingNotSupported);
            }
        }

        public override long Position
        {
            get
            {
                ThrowIfDisposed();
                return _position;
            }
            set
            {
                ThrowIfDisposed();
                throw new NotSupportedException(SR.SeekingNotSupported);
            }
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => _canWrite;

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().ToString(), SR.HiddenStreamName);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            throw new NotSupportedException(SR.ReadingNotSupported);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            throw new NotSupportedException(SR.ReadingNotSupported);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            throw new NotSupportedException(SR.ReadingNotSupported);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfDisposed();
            throw new NotSupportedException(SR.SeekingNotSupported);
        }

        public override void SetLength(long value)
        {
            ThrowIfDisposed();
            throw new NotSupportedException(SR.SetLengthRequiresSeekingAndWriting);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // we can't pass the argument checking down a level
            ValidateBufferArguments(buffer, offset, count);

            // if we're not actually writing anything, we don't want to trigger as if we did write something
            ThrowIfDisposed();
            Debug.Assert(CanWrite);

            if (count == 0)
                return;

            if (!_everWritten)
            {
                Debug.Assert(_baseStream == null);
                _baseStream = _baseStreamFactory();

                _initialPosition = _baseBaseStream.Position;
                _everWritten = true;
            }

            Debug.Assert(_baseStream != null);

            _checksum = Crc32Helper.UpdateCrc32(_checksum, buffer, offset, count);
            _baseStream.Write(buffer, offset, count);
            _position += count;
        }

        public override void Write(ReadOnlySpan<byte> source)
        {
            // if we're not actually writing anything, we don't want to trigger as if we did write something
            ThrowIfDisposed();
            Debug.Assert(CanWrite);

            if (source.Length == 0)
                return;

            if (!_everWritten)
            {
                Debug.Assert(_baseStream == null);
                _baseStream = _baseStreamFactory();

                _initialPosition = _baseBaseStream.Position;
                _everWritten = true;
            }

            Debug.Assert(_baseStream != null);

            _checksum = Crc32Helper.UpdateCrc32(_checksum, source);
            _baseStream.Write(source);
            _position += source.Length;
        }

        public override void WriteByte(byte value) =>
            Write(new ReadOnlySpan<byte>(in value));

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            Debug.Assert(CanWrite);

            return !buffer.IsEmpty ?
                Core(buffer, cancellationToken) :
                default;

            async ValueTask Core(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (!_everWritten)
                {
                    Debug.Assert(_baseStream == null);
                    _baseStream = _baseStreamFactory();

                    _initialPosition = _baseBaseStream.Position;
                    _everWritten = true;
                }

                Debug.Assert(_baseStream != null);

                _checksum = Crc32Helper.UpdateCrc32(_checksum, buffer.Span);

                await _baseStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
                _position += buffer.Length;
            }
        }

        public override void Flush()
        {
            ThrowIfDisposed();

            // assume writable if not disposed
            Debug.Assert(CanWrite);

            _baseStream?.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return _baseStream?.FlushAsync(cancellationToken) ?? Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                // if we never wrote through here, save the position
                if (!_everWritten)
                    _initialPosition = _baseBaseStream.Position;
                if (!_leaveOpenOnClose)
                    _baseStream?.Dispose(); // Close my super-stream (flushes the last data if we ever wrote any)
                _saveCrcAndSizes?.Invoke(_initialPosition, Position, _checksum, _baseBaseStream, _zipArchiveEntry, _onClose);
                _isDisposed = true;
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (!_isDisposed)
            {
                // if we never wrote through here, save the position
                if (!_everWritten)
                    _initialPosition = _baseBaseStream.Position;
                if (!_leaveOpenOnClose && _baseStream != null)
                    await _baseStream.DisposeAsync().ConfigureAwait(false); // Close my super-stream (flushes the last data if we ever wrote any)
                _saveCrcAndSizes?.Invoke(_initialPosition, Position, _checksum, _baseBaseStream, _zipArchiveEntry, _onClose);
                _isDisposed = true;
            }
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    internal sealed class CrcValidatingReadStream : Stream
    {
        private readonly Stream _baseStream;
        private uint _runningCrc;       // CRC32 computed incrementally over bytes read
        private readonly uint _expectedCrc;
        private long _totalBytesRead;
        private readonly long _expectedLength;
        private bool _isDisposed;
        private bool _crcValidated;     // Whether CRC check has been performed
        private bool _crcAbandoned;     // Set when seeking makes CRC validation unreliable

        public CrcValidatingReadStream(Stream baseStream, uint expectedCrc, long expectedLength)
        {
            ArgumentNullException.ThrowIfNull(baseStream);
            ArgumentOutOfRangeException.ThrowIfNegative(expectedLength);

            _baseStream = baseStream;
            _expectedCrc = expectedCrc;
            _expectedLength = expectedLength;
            _runningCrc = 0;
        }

        internal (uint Crc32, long BytesRead)? GetFinalCrcResult()
        {
            if (_crcAbandoned)
                return null;
            return (_runningCrc, _totalBytesRead);
        }

        public override bool CanRead => !_isDisposed && _baseStream.CanRead;
        public override bool CanSeek => !_isDisposed && _baseStream.CanSeek;
        public override bool CanWrite => false;

        public override long Length
        {
            get
            {
                ThrowIfDisposed();
                return _baseStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                ThrowIfDisposed();
                return _baseStream.Position;
            }
            set
            {
                ThrowIfDisposed();
                ThrowIfCantSeek();

                _baseStream.Position = value;

                if (value == 0)
                {
                    ResetCrcState();
                }
                else
                {
                    _crcAbandoned = true;
                }
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, count);

            return Read(new Span<byte>(buffer, offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            ThrowIfDisposed();

            int bytesRead = _baseStream.Read(buffer);
            // Only process when a real read occurred or EOF was signaled
            // (EOF = requested > 0 but got 0 back). Skip zero-length requests.
            if (buffer.Length > 0)
            {
                ProcessBytesRead(buffer.Slice(0, bytesRead));
            }

            return bytesRead;
        }

        public override int ReadByte()
        {
            byte b = default;
            return Read(new Span<byte>(ref b)) == 1 ? b : -1;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            ValidateBufferArguments(buffer, offset, count);

            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            int bytesRead = await _baseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            // Only process when a real read occurred or EOF was signaled
            // (EOF = requested > 0 but got 0 back). Skip zero-length requests.
            if (buffer.Length > 0)
            {
                ProcessBytesRead(buffer.Span.Slice(0, bytesRead));
            }

            return bytesRead;
        }

        private void ProcessBytesRead(ReadOnlySpan<byte> data)
        {
            if (_crcAbandoned)
            {
                return;
            }

            if (data.Length == 0)
            {
                // EOF reached. Only validate CRC if we've read exactly the expected number of bytes.
                // If _totalBytesRead < _expectedLength the declared size was larger than the actual
                // data (e.g. a tampered-but-not-truncated entry);
                // We don't throw here because the caller (decompressor) will surface that as an error!
                // If _totalBytesRead == _expectedLength we can validate the CRC now, which covers
                // zero-length entries and the final EOF read after all expected bytes were consumed.
                if (_totalBytesRead == _expectedLength)
                {
                    ValidateCrc();
                }

                return;
            }

            _runningCrc = Crc32Helper.UpdateCrc32(_runningCrc, data);
            _totalBytesRead += data.Length;

            if (_totalBytesRead == _expectedLength)
            {
                ValidateCrc();
            }
            else if (_totalBytesRead > _expectedLength)
            {
                throw new InvalidDataException(SR.UnexpectedStreamLength);
            }
        }

        private void ValidateCrc()
        {
            if (_crcValidated || _crcAbandoned)
            {
                return;
            }

            if (_runningCrc != _expectedCrc)
            {
                throw new InvalidDataException(SR.CrcMismatch);
            }
            _crcValidated = true;  // only mark validated on success
        }

        /// <summary>
        /// Resets CRC tracking state so validation can be recomputed from the beginning of the stream.
        /// </summary>
        private void ResetCrcState()
        {
            _runningCrc = 0;
            _totalBytesRead = 0;
            _crcAbandoned = false;
            _crcValidated = false;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            throw new NotSupportedException(SR.WritingNotSupported);
        }

        public override void Flush()
        {
            ThrowIfDisposed();
            throw new NotSupportedException(SR.WritingNotSupported);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            throw new NotSupportedException(SR.WritingNotSupported);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfDisposed();
            ThrowIfCantSeek();

            long newPosition = _baseStream.Seek(offset, origin);

            if (newPosition == 0)
            {
                ResetCrcState();
            }
            else
            {
                // we should always start from the beginning of the stream
                // so any seek that doesn't put us back at the start means we can't reliably validate CRC anymore
                _crcAbandoned = true;
            }

            return newPosition;
        }

        public override void SetLength(long value)
        {
            ThrowIfDisposed();
            throw new NotSupportedException(SR.SetLengthRequiresSeekingAndWriting);
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().ToString(), SR.HiddenStreamName);
            }
        }

        private void ThrowIfCantSeek()
        {
            if (!CanSeek)
            {
                throw new NotSupportedException(SR.SeekingNotSupported);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                _baseStream.Dispose();
                _isDisposed = true;
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (!_isDisposed)
            {
                await _baseStream.DisposeAsync().ConfigureAwait(false);
                _isDisposed = true;
            }
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }

    internal sealed class ReadAheadStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly byte[] _history;
        private int _historyCount;
        private byte[]? _pushback;
        private int _pushbackOffset;
        private int _pushbackCount;
        private long _position;
        private bool _isDisposed;

        public ReadAheadStream(Stream baseStream, int historyCapacity = 8192)
        {
            _baseStream = baseStream;
            _history = new byte[historyCapacity];
        }

        public override bool CanRead => !_isDisposed && _baseStream.CanRead;
        // Must report true: DeflateStream checks CanSeek to rewind unconsumed bytes
        // after decompression finishes, which is critical for data descriptor entries.
        public override bool CanSeek => !_isDisposed;
        public override bool CanWrite => false;

        public override long Length
        {
            get
            {
                ThrowIfDisposed();
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                ThrowIfDisposed();
                return _position;
            }
            set
            {
                ThrowIfDisposed();
                throw new NotSupportedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            return Read(buffer.AsSpan(offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            ThrowIfDisposed();

            int totalRead = 0;

            if (_pushbackCount > 0)
            {
                int fromPushback = Math.Min(buffer.Length, _pushbackCount);
                _pushback.AsSpan(_pushbackOffset, fromPushback).CopyTo(buffer);
                RecordHistory(buffer.Slice(0, fromPushback));
                _pushbackOffset += fromPushback;
                _pushbackCount -= fromPushback;
                totalRead += fromPushback;
                buffer = buffer.Slice(fromPushback);

                if (_pushbackCount == 0)
                {
                    _pushback = null;
                }
            }

            if (buffer.Length > 0)
            {
                int fromBase = _baseStream.Read(buffer);
                if (fromBase > 0)
                {
                    RecordHistory(buffer.Slice(0, fromBase));
                    totalRead += fromBase;
                }
            }

            _position += totalRead;
            return totalRead;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            int totalRead = 0;

            if (_pushbackCount > 0)
            {
                int fromPushback = Math.Min(buffer.Length, _pushbackCount);
                _pushback.AsSpan(_pushbackOffset, fromPushback).CopyTo(buffer.Span);
                RecordHistory(buffer.Span.Slice(0, fromPushback));
                _pushbackOffset += fromPushback;
                _pushbackCount -= fromPushback;
                totalRead += fromPushback;
                buffer = buffer.Slice(fromPushback);

                if (_pushbackCount == 0)
                {
                    _pushback = null;
                }
            }

            if (buffer.Length > 0)
            {
                int fromBase = await _baseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (fromBase > 0)
                {
                    RecordHistory(buffer.Span.Slice(0, fromBase));
                    totalRead += fromBase;
                }
            }

            _position += totalRead;
            return totalRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfDisposed();

            if (origin is SeekOrigin.Current && offset < 0)
            {
                int rewindBytes = checked((int)(-offset));

                if (rewindBytes > _historyCount)
                {
                    throw new IOException(SR.IO_SeekBeforeBegin);
                }

                int existingPushback = _pushbackCount;
                byte[] newPushback = new byte[rewindBytes + existingPushback];
                Array.Copy(_history, _historyCount - rewindBytes, newPushback, 0, rewindBytes);

                if (existingPushback > 0)
                {
                    Array.Copy(_pushback!, _pushbackOffset, newPushback, rewindBytes, existingPushback);
                }

                _pushback = newPushback;
                _pushbackOffset = 0;
                _pushbackCount = newPushback.Length;
                _historyCount -= rewindBytes;
                _position -= rewindBytes;

                return _position;
            }

            throw new NotSupportedException();
        }

        private void RecordHistory(ReadOnlySpan<byte> data)
        {
            if (data.Length >= _history.Length)
            {
                data.Slice(data.Length - _history.Length).CopyTo(_history);
                _historyCount = _history.Length;
            }
            else if (_historyCount + data.Length <= _history.Length)
            {
                data.CopyTo(_history.AsSpan(_historyCount));
                _historyCount += data.Length;
            }
            else
            {
                int toKeep = _history.Length - data.Length;
                Array.Copy(_history, _historyCount - toKeep, _history, 0, toKeep);
                data.CopyTo(_history.AsSpan(toKeep));
                _historyCount = _history.Length;
            }
        }

        public override void Flush()
        {
            ThrowIfDisposed();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            return Task.CompletedTask;
        }

        public override void SetLength(long value)
        {
            ThrowIfDisposed();
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            throw new NotSupportedException();
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                _baseStream.Dispose();
                _isDisposed = true;
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (!_isDisposed)
            {
                await _baseStream.DisposeAsync().ConfigureAwait(false);
                _isDisposed = true;
            }
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}
