// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    public partial class DeflateStream : Stream
    {
        internal const int DefaultBufferSize = 8192;

        private Stream _stream;
        private CompressionMode _mode;
        private bool _leaveOpen;
        private Inflater _inflater;
        private Deflater _deflater;
        private byte[] _buffer;
        private int _asyncOperations;
        private bool _wroteBytes;

        #region Public Constructors

        public DeflateStream(Stream stream, CompressionMode mode): this(stream, mode, false)
        {
        }

        public DeflateStream(Stream stream, CompressionMode mode, bool leaveOpen) : this(stream, mode, leaveOpen, ZLibNative.Deflate_DefaultWindowBits)
        {
        }

        // Implies mode = Compress
        public DeflateStream(Stream stream, CompressionLevel compressionLevel) : this(stream, compressionLevel, false)
        {
        }

        // Implies mode = Compress
        public DeflateStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen) : this(stream, compressionLevel, leaveOpen, ZLibNative.Deflate_DefaultWindowBits)
        {
        }

        #endregion

        #region Private Constructors and Initializers

        /// <summary>
        /// Internal constructor to check stream validity and call the correct initalization function depending on
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
            _buffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
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
            _buffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
        }

        #endregion

        public Stream BaseStream
        {
            get
            {
                return _stream;
            }
        }

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

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                throw new NotSupportedException(SR.NotSupported);
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException(SR.NotSupported);
            }

            set
            {
                throw new NotSupportedException(SR.NotSupported);
            }
        }

        public override void Flush()
        {
            EnsureNotDisposed();
            if (_mode == CompressionMode.Compress)
                FlushBuffers();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (_asyncOperations != 0)
                throw new InvalidOperationException(SR.InvalidBeginCall);

            EnsureNotDisposed();

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            return _mode != CompressionMode.Compress || !_wroteBytes ? Task.CompletedTask : FlushAsyncCore(cancellationToken);
        }

        private async Task FlushAsyncCore(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _asyncOperations);
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
                        await _stream.WriteAsync(_buffer, 0, compressedBytes, cancellationToken).ConfigureAwait(false);
                    }
                    Debug.Assert(flushSuccessful == (compressedBytes > 0));
                } while (flushSuccessful);
            }
            finally
            {
                Interlocked.Decrement(ref _asyncOperations);
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
            EnsureDecompressionMode();
            ValidateParameters(array, offset, count);
            EnsureNotDisposed();

            int bytesRead;
            int currentOffset = offset;
            int remainingCount = count;

            while (true)
            {
                bytesRead = _inflater.Inflate(array, currentOffset, remainingCount);
                currentOffset += bytesRead;
                remainingCount -= bytesRead;

                if (remainingCount == 0)
                {
                    break;
                }

                if (_inflater.Finished())
                {
                    // if we finished decompressing, we can't have anything left in the outputwindow.
                    Debug.Assert(_inflater.AvailableOutput == 0, "We should have copied all stuff out!");
                    break;
                }

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

            return count - remainingCount;
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowStreamClosedException()
        {
            throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
        }

        private void EnsureDecompressionMode()
        {
            if (_mode != CompressionMode.Decompress)
                ThrowCannotReadFromDeflateStreamException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowCannotReadFromDeflateStreamException()
        {
            throw new InvalidOperationException(SR.CannotReadFromDeflateStream);
        }

        private void EnsureCompressionMode()
        {
            if (_mode != CompressionMode.Compress)
                ThrowCannotWriteToDeflateStreamException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowCannotWriteToDeflateStreamException()
        {
            throw new InvalidOperationException(SR.CannotWriteToDeflateStream);
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            return StreamHelpers.ArrayPoolCopyToAsync(this, destination, bufferSize, cancellationToken);
        }

        public override Task<int> ReadAsync(Byte[] array, int offset, int count, CancellationToken cancellationToken)
        {
            EnsureDecompressionMode();

            // We use this checking order for compat to earlier versions:
            if (_asyncOperations != 0)
                throw new InvalidOperationException(SR.InvalidBeginCall);

            ValidateParameters(array, offset, count);
            EnsureNotDisposed();

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            Interlocked.Increment(ref _asyncOperations);
            Task<int> readTask = null;

            try
            {
                // Try to read decompressed data in output buffer
                int bytesRead = _inflater.Inflate(array, offset, count);
                if (bytesRead != 0)
                {
                    // If decompression output buffer is not empty, return immediately.
                    return Task.FromResult(bytesRead);
                }

                if (_inflater.Finished())
                {
                    // end of compression stream
                    return Task.FromResult(0);
                }

                // If there is no data on the output buffer and we are not at 
                // the end of the stream, we need to get more data from the base stream
                readTask = _stream.ReadAsync(_buffer, 0, _buffer.Length, cancellationToken);
                if (readTask == null)
                {
                    throw new InvalidOperationException(SR.NotSupported_UnreadableStream);
                }

                return ReadAsyncCore(readTask, array, offset, count, cancellationToken);
            }
            finally
            {
                // if we haven't started any async work, decrement the counter to end the transaction
                if (readTask == null)
                {
                    Interlocked.Decrement(ref _asyncOperations);
                }
            }
        }

        private async Task<int> ReadAsyncCore(Task<int> readTask, byte[] array, int offset, int count, CancellationToken cancellationToken)
        {
            try
            {
                while (true)
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
                    bytesRead = _inflater.Inflate(array, offset, count);

                    if (bytesRead == 0 && !_inflater.Finished())
                    {
                        // We could have read in head information and didn't get any data.
                        // Read from the base stream again.   
                        readTask = _stream.ReadAsync(_buffer, 0, _buffer.Length, cancellationToken);
                        if (readTask == null)
                        {
                            throw new InvalidOperationException(SR.NotSupported_UnreadableStream);
                        }
                    }
                    else
                    {
                        return bytesRead;
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref _asyncOperations);
            }
        }

        public override void Write(byte[] array, int offset, int count)
        {
            // Validate the state and the parameters
            EnsureCompressionMode();
            ValidateParameters(array, offset, count);
            EnsureNotDisposed();

            // Write compressed the bytes we already passed to the deflater:
            WriteDeflaterOutput();

            // Pass new bytes through deflater and write them too:
            _deflater.SetInput(array, offset, count);
            WriteDeflaterOutput();
            _wroteBytes = true;
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
                    if (disposing && !_leaveOpen && _stream != null)
                        _stream.Dispose();
                }
                finally
                {
                    _stream = null;

                    try
                    {
                        if (_deflater != null)
                            _deflater.Dispose();
                        if (_inflater != null)
                            _inflater.Dispose();
                    }
                    finally
                    {
                        if (_buffer != null)
                            ArrayPool<byte>.Shared.Return(_buffer, clearArray: true);
                        _buffer = null;
                        _deflater = null;
                        _inflater = null;
                        base.Dispose(disposing);
                    }
                }
            }
        }

        public override Task WriteAsync(Byte[] array, int offset, int count, CancellationToken cancellationToken)
        {
            EnsureCompressionMode();

            // We use this checking order for compat to earlier versions:
            if (_asyncOperations != 0)
                throw new InvalidOperationException(SR.InvalidBeginCall);

            ValidateParameters(array, offset, count);
            EnsureNotDisposed();

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<int>(cancellationToken);

            return WriteAsyncCore(array, offset, count, cancellationToken);
        }

        private async Task WriteAsyncCore(Byte[] array, int offset, int count, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _asyncOperations);
            try
            {
                await WriteDeflaterOutputAsync(cancellationToken).ConfigureAwait(false);

                // Pass new bytes through deflater
                _deflater.SetInput(array, offset, count);

                await WriteDeflaterOutputAsync(cancellationToken).ConfigureAwait(false);

                _wroteBytes = true;
            }
            finally
            {
                Interlocked.Decrement(ref _asyncOperations);
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
                    await _stream.WriteAsync(_buffer, 0, compressedBytes, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}

