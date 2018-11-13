// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    public partial class DeflateStream : Stream
    {
        private const int DefaultBufferSize = 8192;

        private Stream _stream;
        private CompressionMode _mode;
        private bool _leaveOpen;
        private Inflater _inflater;
        private Deflater _deflater;
        private byte[] _buffer;
        private int _activeAsyncOperation; // 1 == true, 0 == false
        private bool _wroteBytes;

        public DeflateStream(Stream stream, CompressionMode mode) : this(stream, mode, leaveOpen: false)
        {
        }

        public DeflateStream(Stream stream, CompressionMode mode, bool leaveOpen) : this(stream, mode, leaveOpen, ZLibNative.Deflate_DefaultWindowBits)
        {
        }

        // Implies mode = Compress
        public DeflateStream(Stream stream, CompressionLevel compressionLevel) : this(stream, compressionLevel, leaveOpen: false)
        {
        }

        // Implies mode = Compress
        public DeflateStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen) : this(stream, compressionLevel, leaveOpen, ZLibNative.Deflate_DefaultWindowBits)
        {
        }

        /// <summary>
        /// Internal constructor to check stream validity and call the correct initialization function depending on
        /// the value of the CompressionMode given.
        /// </summary>
        internal DeflateStream(Stream stream, CompressionMode mode, bool leaveOpen, int windowBits)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            switch (mode)
            {
                case CompressionMode.Decompress:
                    InitializeInflater(stream, leaveOpen, windowBits);
                    break;

                case CompressionMode.Compress:
                    InitializeDeflater(stream, leaveOpen, windowBits, CompressionLevel.Optimal);
                    break;

                default:
                    throw new ArgumentException(SR.ArgumentOutOfRange_Enum, nameof(mode));
            }
        }

        /// <summary>
        /// Internal constructor to specify the compressionlevel as well as the windowbits
        /// </summary>
        internal DeflateStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen, int windowBits)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            InitializeDeflater(stream, leaveOpen, windowBits, compressionLevel);
        }

        /// <summary>
        /// Sets up this DeflateStream to be used for Zlib Inflation/Decompression
        /// </summary>
        internal void InitializeInflater(Stream stream, bool leaveOpen, int windowBits)
        {
            Debug.Assert(stream != null);
            if (!stream.CanRead)
                throw new ArgumentException(SR.NotSupported_UnreadableStream, nameof(stream));

            _inflater = new Inflater(windowBits);

            _stream = stream;
            _mode = CompressionMode.Decompress;
            _leaveOpen = leaveOpen;
        }

        /// <summary>
        /// Sets up this DeflateStream to be used for Zlib Deflation/Compression
        /// </summary>
        internal void InitializeDeflater(Stream stream, bool leaveOpen, int windowBits, CompressionLevel compressionLevel)
        {
            Debug.Assert(stream != null);
            if (!stream.CanWrite)
                throw new ArgumentException(SR.NotSupported_UnwritableStream, nameof(stream));

            _deflater = new Deflater(compressionLevel, windowBits);

            _stream = stream;
            _mode = CompressionMode.Compress;
            _leaveOpen = leaveOpen;
            InitializeBuffer();
        }

        private void InitializeBuffer()
        {
            Debug.Assert(_buffer == null);
            _buffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
        }

        private void EnsureBufferInitialized()
        {
            if (_buffer == null)
            {
                InitializeBuffer();
            }
        }

        public Stream BaseStream => _stream;

        public override bool CanRead
        {
            get
            {
                if (_stream == null)
                {
                    return false;
                }

                return (_mode == CompressionMode.Decompress && _stream.CanRead);
            }
        }

        public override bool CanWrite
        {
            get
            {
                if (_stream == null)
                {
                    return false;
                }

                return (_mode == CompressionMode.Compress && _stream.CanWrite);
            }
        }

        public override bool CanSeek => false;

        public override long Length
        {
            get { throw new NotSupportedException(SR.NotSupported); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(SR.NotSupported); }
            set { throw new NotSupportedException(SR.NotSupported); }
        }

        public override void Flush()
        {
            EnsureNotDisposed();
            if (_mode == CompressionMode.Compress)
                FlushBuffers();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            EnsureNoActiveAsyncOperation();
            EnsureNotDisposed();

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            return _mode != CompressionMode.Compress || !_wroteBytes ? Task.CompletedTask : FlushAsyncCore(cancellationToken);
        }

        private async Task FlushAsyncCore(CancellationToken cancellationToken)
        {
            AsyncOperationStarting();
            try
            {
                // Compress any bytes left:
                await WriteDeflaterOutputAsync(cancellationToken).ConfigureAwait(false);

                // Pull out any bytes left inside deflater:
                bool flushSuccessful;
                do
                {
                    int compressedBytes;
                    flushSuccessful = _deflater.Flush(_buffer, out compressedBytes);
                    if (flushSuccessful)
                    {
                        await _stream.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, compressedBytes), cancellationToken).ConfigureAwait(false);
                    }
                    Debug.Assert(flushSuccessful == (compressedBytes > 0));
                } while (flushSuccessful);
            }
            finally
            {
                AsyncOperationCompleting();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(SR.NotSupported);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException(SR.NotSupported);
        }

        public override int ReadByte()
        {
            EnsureDecompressionMode();
            EnsureNotDisposed();

            // Try to read a single byte from zlib without allocating an array, pinning an array, etc.
            // If zlib doesn't have any data, fall back to the base stream implementation, which will do that.
            byte b;
            return _inflater.Inflate(out b) ? b : base.ReadByte();
        }

        public override int Read(byte[] array, int offset, int count)
        {
            ValidateParameters(array, offset, count);
            return ReadCore(new Span<byte>(array, offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            if (GetType() != typeof(DeflateStream))
            {
                // DeflateStream is not sealed, and a derived type may have overridden Read(byte[], int, int) prior
                // to this Read(Span<byte>) overload being introduced.  In that case, this Read(Span<byte>) overload
                // should use the behavior of Read(byte[],int,int) overload.
                return base.Read(buffer);
            }
            else
            {
                return ReadCore(buffer);
            }
        }

        internal int ReadCore(Span<byte> buffer)
        {
            EnsureDecompressionMode();
            EnsureNotDisposed();
            EnsureBufferInitialized();

            int totalRead = 0;

            while (true)
            {
                int bytesRead = _inflater.Inflate(buffer.Slice(totalRead));
                totalRead += bytesRead;
                if (totalRead == buffer.Length)
                {
                    break;
                }

                // If the stream is finished then we have a few potential cases here:
                // 1. DeflateStream => return
                // 2. GZipStream that is finished but may have an additional GZipStream appended => feed more input
                // 3. GZipStream that is finished and appended with garbage => return
                if (_inflater.Finished() && (!_inflater.IsGzipStream() || !_inflater.NeedsInput()))
                {
                    break;
                }

                if (_inflater.NeedsInput())
                {
                    int bytes = _stream.Read(_buffer, 0, _buffer.Length);
                    if (bytes <= 0)
                    {
                        break;
                    }
                    else if (bytes > _buffer.Length)
                    {
                        // The stream is either malicious or poorly implemented and returned a number of
                        // bytes larger than the buffer supplied to it.
                        throw new InvalidDataException(SR.GenericInvalidData);
                    }

                     _inflater.SetInput(_buffer, 0, bytes);
                }
            }

            return totalRead;
        }

        private void ValidateParameters(byte[] array, int offset, int count)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (array.Length - offset < count)
                throw new ArgumentException(SR.InvalidArgumentOffsetCount);
        }

        private void EnsureNotDisposed()
        {
            if (_stream == null)
                ThrowStreamClosedException();
        }

        private static void ThrowStreamClosedException()
        {
            throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
        }

        private void EnsureDecompressionMode()
        {
            if (_mode != CompressionMode.Decompress)
                ThrowCannotReadFromDeflateStreamException();
        }

        private static void ThrowCannotReadFromDeflateStreamException()
        {
            throw new InvalidOperationException(SR.CannotReadFromDeflateStream);
        }

        private void EnsureCompressionMode()
        {
            if (_mode != CompressionMode.Compress)
                ThrowCannotWriteToDeflateStreamException();
        }

        private static void ThrowCannotWriteToDeflateStreamException()
        {
            throw new InvalidOperationException(SR.CannotWriteToDeflateStream);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState) =>
            TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);

        public override int EndRead(IAsyncResult asyncResult) =>
            TaskToApm.End<int>(asyncResult);

        public override Task<int> ReadAsync(byte[] array, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateParameters(array, offset, count);
            return ReadAsyncMemory(new Memory<byte>(array, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (GetType() != typeof(DeflateStream))
            {
                // Ensure that existing streams derived from DeflateStream and that override ReadAsync(byte[],...)
                // get their existing behaviors when the newer Memory-based overload is used.
                return base.ReadAsync(buffer, cancellationToken);
            }
            else
            {
                return ReadAsyncMemory(buffer, cancellationToken);
            }
        }

        internal ValueTask<int> ReadAsyncMemory(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            EnsureDecompressionMode();
            EnsureNoActiveAsyncOperation();
            EnsureNotDisposed();

            if (cancellationToken.IsCancellationRequested)
            {
                return new ValueTask<int>(Task.FromCanceled<int>(cancellationToken));
            }

            EnsureBufferInitialized();

            bool cleanup = true;
            AsyncOperationStarting();
            try
            {
                while (true)
                {
                    // Finish inflating any bytes in the input buffer
                    int bytesRead = 0, bytesReadIteration = -1;
                    while (bytesRead < buffer.Length && bytesReadIteration != 0)
                    {
                        bytesReadIteration = _inflater.Inflate(buffer.Span.Slice(bytesRead));
                        bytesRead += bytesReadIteration;
                    }

                    if (bytesRead != 0)
                    {
                        // If decompression output buffer is not empty, return immediately.
                        return new ValueTask<int>(bytesRead);
                    }

                    // If the stream is finished then we have a few potential cases here:
                    // 1. DeflateStream that is finished => return
                    // 2. GZipStream that is finished but may have an additional GZipStream appended => feed more input
                    // 3. GZipStream that is finished and appended with garbage => return
                    if (_inflater.Finished() && (!_inflater.IsGzipStream() || !_inflater.NeedsInput()))
                    {
                        return new ValueTask<int>(0);
                    }

                    if (_inflater.NeedsInput())
                    {
                        // If there is no data on the output buffer and we are not at
                        // the end of the stream, we need to get more data from the base stream
                        ValueTask<int> readTask = _stream.ReadAsync(_buffer, cancellationToken);
                        cleanup = false;
                        return FinishReadAsyncMemory(readTask, buffer, cancellationToken);
                    }
                }
            }
            finally
            {
                // if we haven't started any async work, decrement the counter to end the transaction
                if (cleanup)
                {
                    AsyncOperationCompleting();
                }
            }
        }

        private async ValueTask<int> FinishReadAsyncMemory(
            ValueTask<int> readTask, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    if (_inflater.NeedsInput())
                    {
                        int bytesRead = await readTask.ConfigureAwait(false);
                        EnsureNotDisposed();

                        if (bytesRead <= 0)
                        {
                            // This indicates the base stream has received EOF
                            return 0;
                        }
                        else if (bytesRead > _buffer.Length)
                        {
                            // The stream is either malicious or poorly implemented and returned a number of
                            // bytes larger than the buffer supplied to it.
                            throw new InvalidDataException(SR.GenericInvalidData);
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        // Feed the data from base stream into decompression engine
                        _inflater.SetInput(_buffer, 0, bytesRead);
                    }

                    // Finish inflating any bytes in the input buffer
                    int inflatedBytes = 0, bytesReadIteration = -1;
                    while (inflatedBytes < buffer.Length && bytesReadIteration != 0)
                    {
                        bytesReadIteration = _inflater.Inflate(buffer.Span.Slice(inflatedBytes));
                        inflatedBytes += bytesReadIteration;
                    }

                    // There are a few different potential states here
                    // 1. DeflateStream or GZipStream that succesfully read bytes => return those bytes
                    // 2. DeflateStream or GZipStream that didn't read bytes and isn't finished => feed more input
                    // 3. DeflateStream that didn't read bytes, but is finished => return 0
                    // 4. GZipStream that is finished but is appended with another gzip stream => feed more input
                    // 5. GZipStream that is finished and appended with garbage => return 0
                    if (inflatedBytes != 0)
                    {
                        // If decompression output buffer is not empty, return immediately.
                        return inflatedBytes;
                    }
                    else if (_inflater.Finished() && (!_inflater.IsGzipStream() || !_inflater.NeedsInput()))
                    {
                        return 0;
                    }
                    else if (_inflater.NeedsInput())
                    {
                        // We could have read in head information and didn't get any data.
                        // Read from the base stream again.
                        readTask = _stream.ReadAsync(_buffer, cancellationToken);
                    }
                }
            }
            finally
            {
                AsyncOperationCompleting();
            }
        }

        public override void Write(byte[] array, int offset, int count)
        {
            ValidateParameters(array, offset, count);
            WriteCore(new ReadOnlySpan<byte>(array, offset, count));
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (GetType() != typeof(DeflateStream))
            {
                // DeflateStream is not sealed, and a derived type may have overridden Write(byte[], int, int) prior
                // to this Write(ReadOnlySpan<byte>) overload being introduced.  In that case, this Write(ReadOnlySpan<byte>) overload
                // should use the behavior of Write(byte[],int,int) overload.
                base.Write(buffer);
            }
            else
            {
                WriteCore(buffer);
            }
        }

        internal void WriteCore(ReadOnlySpan<byte> buffer)
        {
            EnsureCompressionMode();
            EnsureNotDisposed();

            // Write compressed the bytes we already passed to the deflater:
            WriteDeflaterOutput();

            unsafe
            {
                // Pass new bytes through deflater and write them too:
                fixed (byte* bufferPtr = &MemoryMarshal.GetReference(buffer))
                {
                    _deflater.SetInput(bufferPtr, buffer.Length);
                    WriteDeflaterOutput();
                    _wroteBytes = true;
                }
            }
        }

        private void WriteDeflaterOutput()
        {
            while (!_deflater.NeedsInput())
            {
                int compressedBytes = _deflater.GetDeflateOutput(_buffer);
                if (compressedBytes > 0)
                {
                    _stream.Write(_buffer, 0, compressedBytes);
                }
            }
        }

        // This is called by Flush:
        private void FlushBuffers()
        {
            // Make sure to only "flush" when we actually had some input:
            if (_wroteBytes)
            {
                // Compress any bytes left:
                WriteDeflaterOutput();

                // Pull out any bytes left inside deflater:
                bool flushSuccessful;
                do
                {
                    int compressedBytes;
                    flushSuccessful = _deflater.Flush(_buffer, out compressedBytes);
                    if (flushSuccessful)
                    {
                        _stream.Write(_buffer, 0, compressedBytes);
                    }
                    Debug.Assert(flushSuccessful == (compressedBytes > 0));
                } while (flushSuccessful);
            }
        }

        // This is called by Dispose:
        private void PurgeBuffers(bool disposing)
        {
            if (!disposing)
                return;

            if (_stream == null)
                return;

            if (_mode != CompressionMode.Compress)
                return;

            // Some deflaters (e.g. ZLib) write more than zero bytes for zero byte inputs.
            // This round-trips and we should be ok with this, but our legacy managed deflater
            // always wrote zero output for zero input and upstack code (e.g. ZipArchiveEntry)
            // took dependencies on it. Thus, make sure to only "flush" when we actually had
            // some input:
            if (_wroteBytes)
            {
                // Compress any bytes left
                WriteDeflaterOutput();

                // Pull out any bytes left inside deflater:
                bool finished;
                do
                {
                    int compressedBytes;
                    finished = _deflater.Finish(_buffer, out compressedBytes);

                    if (compressedBytes > 0)
                        _stream.Write(_buffer, 0, compressedBytes);
                } while (!finished);
            }
            else
            {
                // In case of zero length buffer, we still need to clean up the native created stream before
                // the object get disposed because eventually ZLibNative.ReleaseHandle will get called during
                // the dispose operation and although it frees the stream but it return error code because the
                // stream state was still marked as in use. The symptoms of this problem will not be seen except
                // if running any diagnostic tools which check for disposing safe handle objects
                bool finished;
                do
                {
                    int compressedBytes;
                    finished = _deflater.Finish(_buffer, out compressedBytes);
                } while (!finished);
            }
        }

        private async Task PurgeBuffersAsync()
        {
            // Same logic as PurgeBuffers, except with async counterparts.

            if (_stream == null)
                return;

            if (_mode != CompressionMode.Compress)
                return;

            // Some deflaters (e.g. ZLib) write more than zero bytes for zero byte inputs.
            // This round-trips and we should be ok with this, but our legacy managed deflater
            // always wrote zero output for zero input and upstack code (e.g. ZipArchiveEntry)
            // took dependencies on it. Thus, make sure to only "flush" when we actually had
            // some input.
            if (_wroteBytes)
            {
                // Compress any bytes left
                await WriteDeflaterOutputAsync(default).ConfigureAwait(false);

                // Pull out any bytes left inside deflater:
                bool finished;
                do
                {
                    int compressedBytes;
                    finished = _deflater.Finish(_buffer, out compressedBytes);

                    if (compressedBytes > 0)
                        await _stream.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, compressedBytes)).ConfigureAwait(false);
                } while (!finished);
            }
            else
            {
                // In case of zero length buffer, we still need to clean up the native created stream before
                // the object get disposed because eventually ZLibNative.ReleaseHandle will get called during
                // the dispose operation and although it frees the stream, it returns an error code because the
                // stream state was still marked as in use. The symptoms of this problem will not be seen except
                // if running any diagnostic tools which check for disposing safe handle objects.
                bool finished;
                do
                {
                    int compressedBytes;
                    finished = _deflater.Finish(_buffer, out compressedBytes);
                } while (!finished);
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                PurgeBuffers(disposing);
            }
            finally
            {
                // Close the underlying stream even if PurgeBuffers threw.
                // Stream.Close() may throw here (may or may not be due to the same error).
                // In this case, we still need to clean up internal resources, hence the inner finally blocks.
                try
                {
                    if (disposing && !_leaveOpen)
                        _stream?.Dispose();
                }
                finally
                {
                    _stream = null;

                    try
                    {
                        _deflater?.Dispose();
                        _inflater?.Dispose();
                    }
                    finally
                    {
                        _deflater = null;
                        _inflater = null;

                        byte[] buffer = _buffer;
                        if (buffer != null)
                        {
                            _buffer = null;
                            if (!AsyncOperationIsActive)
                            {
                                ArrayPool<byte>.Shared.Return(buffer);
                            }
                        }

                        base.Dispose(disposing);
                    }
                }
            }
        }

        public override ValueTask DisposeAsync()
        {
            return GetType() == typeof(DeflateStream) ?
                DisposeAsyncCore() :
                base.DisposeAsync();
        }

        private async ValueTask DisposeAsyncCore()
        {
            // Same logic as Dispose(true), except with async counterparts.
            try
            {
                await PurgeBuffersAsync().ConfigureAwait(false);
            }
            finally
            {
                // Close the underlying stream even if PurgeBuffers threw.
                // Stream.Close() may throw here (may or may not be due to the same error).
                // In this case, we still need to clean up internal resources, hence the inner finally blocks.
                Stream stream = _stream;
                _stream = null;
                try
                {
                    if (!_leaveOpen && stream != null)
                        await stream.DisposeAsync().ConfigureAwait(false);
                }
                finally
                {
                    try
                    {
                        _deflater?.Dispose();
                        _inflater?.Dispose();
                    }
                    finally
                    {
                        _deflater = null;
                        _inflater = null;

                        byte[] buffer = _buffer;
                        if (buffer != null)
                        {
                            _buffer = null;
                            if (!AsyncOperationIsActive)
                            {
                                ArrayPool<byte>.Shared.Return(buffer);
                            }
                        }
                    }
                }
            }
        }

        public override IAsyncResult BeginWrite(byte[] array, int offset, int count, AsyncCallback asyncCallback, object asyncState) =>
            TaskToApm.Begin(WriteAsync(array, offset, count, CancellationToken.None), asyncCallback, asyncState);

        public override void EndWrite(IAsyncResult asyncResult) =>
            TaskToApm.End(asyncResult);

        public override Task WriteAsync(byte[] array, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateParameters(array, offset, count);
            return WriteAsyncMemory(new ReadOnlyMemory<byte>(array, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            if (GetType() != typeof(DeflateStream))
            {
                // Ensure that existing streams derived from DeflateStream and that override WriteAsync(byte[],...)
                // get their existing behaviors when the newer Memory-based overload is used.
                return base.WriteAsync(buffer, cancellationToken);
            }
            else
            {
                return WriteAsyncMemory(buffer, cancellationToken);
            }
        }

        internal ValueTask WriteAsyncMemory(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            EnsureCompressionMode();
            EnsureNoActiveAsyncOperation();
            EnsureNotDisposed();

            return new ValueTask(cancellationToken.IsCancellationRequested ?
                Task.FromCanceled<int>(cancellationToken) :
                WriteAsyncMemoryCore(buffer, cancellationToken));
        }

        private async Task WriteAsyncMemoryCore(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            AsyncOperationStarting();
            try
            {
                await WriteDeflaterOutputAsync(cancellationToken).ConfigureAwait(false);

                // Pass new bytes through deflater
                _deflater.SetInput(buffer);

                await WriteDeflaterOutputAsync(cancellationToken).ConfigureAwait(false);

                _wroteBytes = true;
            }
            finally
            {
                AsyncOperationCompleting();
            }
        }

        /// <summary>
        /// Writes the bytes that have already been deflated
        /// </summary>
        private async Task WriteDeflaterOutputAsync(CancellationToken cancellationToken)
        {
            while (!_deflater.NeedsInput())
            {
                int compressedBytes = _deflater.GetDeflateOutput(_buffer);
                if (compressedBytes > 0)
                {
                    await _stream.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, compressedBytes), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public override void CopyTo(Stream destination, int bufferSize)
        {
            StreamHelpers.ValidateCopyToArgs(this, destination, bufferSize);

            EnsureDecompressionMode();
            EnsureNotDisposed();

            new CopyToStream(this, destination, bufferSize).CopyFromSourceToDestination();
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            // Validation as base CopyToAsync would do
            StreamHelpers.ValidateCopyToArgs(this, destination, bufferSize);

            // Validation as ReadAsync would do
            EnsureDecompressionMode();
            EnsureNoActiveAsyncOperation();
            EnsureNotDisposed();

            // Early check for cancellation
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            // Do the copy
            return new CopyToStream(this, destination, bufferSize, cancellationToken).CopyFromSourceToDestinationAsync();
        }

        private sealed class CopyToStream : Stream
        {
            private readonly DeflateStream _deflateStream;
            private readonly Stream _destination;
            private readonly CancellationToken _cancellationToken;
            private byte[] _arrayPoolBuffer;

            public CopyToStream(DeflateStream deflateStream, Stream destination, int bufferSize) :
                this(deflateStream, destination, bufferSize, CancellationToken.None)
            {
            }

            public CopyToStream(DeflateStream deflateStream, Stream destination, int bufferSize, CancellationToken cancellationToken)
            {
                Debug.Assert(deflateStream != null);
                Debug.Assert(destination != null);
                Debug.Assert(bufferSize > 0);

                _deflateStream = deflateStream;
                _destination = destination;
                _cancellationToken = cancellationToken;
                _arrayPoolBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            }

            public async Task CopyFromSourceToDestinationAsync()
            {
                _deflateStream.AsyncOperationStarting();
                try
                {
                    // Flush any existing data in the inflater to the destination stream.
                    while (true)
                    {
                        int bytesRead = _deflateStream._inflater.Inflate(_arrayPoolBuffer, 0, _arrayPoolBuffer.Length);
                        if (bytesRead > 0)
                        {
                            await _destination.WriteAsync(new ReadOnlyMemory<byte>(_arrayPoolBuffer, 0, bytesRead), _cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Now, use the source stream's CopyToAsync to push directly to our inflater via this helper stream
                    await _deflateStream._stream.CopyToAsync(this, _arrayPoolBuffer.Length, _cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    _deflateStream.AsyncOperationCompleting();

                    ArrayPool<byte>.Shared.Return(_arrayPoolBuffer);
                    _arrayPoolBuffer = null;
                }
            }

            public void CopyFromSourceToDestination()
            {
                try
                {
                    // Flush any existing data in the inflater to the destination stream.
                    while (true)
                    {
                        int bytesRead = _deflateStream._inflater.Inflate(_arrayPoolBuffer, 0, _arrayPoolBuffer.Length);
                        if (bytesRead > 0)
                        {
                            _destination.Write(_arrayPoolBuffer, 0, bytesRead);
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Now, use the source stream's CopyToAsync to push directly to our inflater via this helper stream
                    _deflateStream._stream.CopyTo(this, _arrayPoolBuffer.Length);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(_arrayPoolBuffer);
                    _arrayPoolBuffer = null;
                }
            }

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                // Validate inputs
                Debug.Assert(buffer != _arrayPoolBuffer);
                _deflateStream.EnsureNotDisposed();
                if (count <= 0)
                {
                    return;
                }
                else if (count > buffer.Length - offset)
                {
                    // The buffer stream is either malicious or poorly implemented and returned a number of
                    // bytes larger than the buffer supplied to it.
                    throw new InvalidDataException(SR.GenericInvalidData);
                }

                // Feed the data from base stream into the decompression engine.
                _deflateStream._inflater.SetInput(buffer, offset, count);

                // While there's more decompressed data available, forward it to the buffer stream.
                while (true)
                {
                    int bytesRead = _deflateStream._inflater.Inflate(new Span<byte>(_arrayPoolBuffer));
                    if (bytesRead > 0)
                    {
                        await _destination.WriteAsync(new ReadOnlyMemory<byte>(_arrayPoolBuffer, 0, bytesRead), cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                // Validate inputs
                Debug.Assert(buffer != _arrayPoolBuffer);
                _deflateStream.EnsureNotDisposed();

                if (count <= 0)
                {
                    return;
                }
                else if (count > buffer.Length - offset)
                {
                    // The buffer stream is either malicious or poorly implemented and returned a number of
                    // bytes larger than the buffer supplied to it.
                    throw new InvalidDataException(SR.GenericInvalidData);
                }

                // Feed the data from base stream into the decompression engine.
                _deflateStream._inflater.SetInput(buffer, offset, count);

                // While there's more decompressed data available, forward it to the buffer stream.
                while (true)
                {
                    int bytesRead = _deflateStream._inflater.Inflate(new Span<byte>(_arrayPoolBuffer));
                    if (bytesRead > 0)
                    {
                        _destination.Write(_arrayPoolBuffer, 0, bytesRead);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            public override bool CanWrite => true;
            public override void Flush() { }
            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override long Length { get { throw new NotSupportedException(); } }
            public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
            public override int Read(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
            public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
            public override void SetLength(long value) { throw new NotSupportedException(); }
        }

        private bool AsyncOperationIsActive => _activeAsyncOperation != 0;

        private void EnsureNoActiveAsyncOperation()
        {
            if (AsyncOperationIsActive)
                ThrowInvalidBeginCall();
        }

        private void AsyncOperationStarting()
        {
            if (Interlocked.CompareExchange(ref _activeAsyncOperation, 1, 0) != 0)
            {
                ThrowInvalidBeginCall();
            }
        }

        private void AsyncOperationCompleting()
        {
            int oldValue = Interlocked.CompareExchange(ref _activeAsyncOperation, 0, 1);
            Debug.Assert(oldValue == 1, $"Expected {nameof(_activeAsyncOperation)} to be 1, got {oldValue}");
        }

        private static void ThrowInvalidBeginCall()
        {
            throw new InvalidOperationException(SR.InvalidBeginCall);
        }
    }
}
