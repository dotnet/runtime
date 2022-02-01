// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    // DeflateManagedStream supports decompression of Deflate64 format only.
    internal sealed partial class DeflateManagedStream : Stream
    {
        internal const int DefaultBufferSize = 8192;

        private Stream? _stream;
        private InflaterManaged _inflater;
        private readonly byte[] _buffer;

        private int _asyncOperations;

        // A specific constructor to allow decompression of Deflate64
        internal DeflateManagedStream(Stream stream, ZipArchiveEntry.CompressionMethodValues method, long uncompressedSize = -1)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException(SR.NotSupported_UnreadableStream, nameof(stream));

            Debug.Assert(method == ZipArchiveEntry.CompressionMethodValues.Deflate64);

            _inflater = new InflaterManaged(method == ZipArchiveEntry.CompressionMethodValues.Deflate64, uncompressedSize);

            _stream = stream;
            _buffer = new byte[DefaultBufferSize];
        }

        public override bool CanRead
        {
            get
            {
                if (_stream == null)
                {
                    return false;
                }

                return _stream.CanRead;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
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
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            EnsureNotDisposed();
            return cancellationToken.IsCancellationRequested ?
                Task.FromCanceled(cancellationToken) :
                Task.CompletedTask;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(SR.NotSupported);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException(SR.NotSupported);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            return Read(new Span<byte>(buffer, offset, count));
        }

        public override int Read(Span<byte> buffer)
        {
            EnsureNotDisposed();

            int initialLength = buffer.Length;

            int bytesRead;
            while (true)
            {
                bytesRead = _inflater.Inflate(buffer);
                buffer = buffer.Slice(bytesRead);

                if (buffer.Length == 0)
                {
                    break;
                }

                if (_inflater.Finished())
                {
                    // if we finished decompressing, we can't have anything left in the outputwindow.
                    Debug.Assert(_inflater.AvailableOutput == 0, "We should have copied all stuff out!");
                    break;
                }

                int bytes = _stream!.Read(_buffer, 0, _buffer.Length);
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

            return initialLength - buffer.Length;
        }

        public override int ReadByte()
        {
            byte b = default;
            return Read(MemoryMarshal.CreateSpan(ref b, 1)) == 1 ? b : -1;
        }

        private void EnsureNotDisposed()
        {
            if (_stream == null)
                ThrowStreamClosedException();
        }

        private static void ThrowStreamClosedException()
        {
            throw new ObjectDisposedException(nameof(DeflateStream), SR.ObjectDisposed_StreamClosed);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState) =>
            TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);

        public override int EndRead(IAsyncResult asyncResult) =>
            TaskToApm.End<int>(asyncResult);

        private ValueTask<int> ReadAsyncInternal(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            Interlocked.Increment(ref _asyncOperations);
            bool startedAsyncWork = false;

            try
            {
                // Try to read decompressed data in output buffer
                int bytesRead = _inflater.Inflate(buffer.Span);
                if (bytesRead != 0)
                {
                    // If decompression output buffer is not empty, return immediately.
                    return ValueTask.FromResult(bytesRead);
                }

                if (_inflater.Finished())
                {
                    // end of compression stream
                    return ValueTask.FromResult(0);
                }

                // If there is no data on the output buffer and we are not at
                // the end of the stream, we need to get more data from the base stream
                ValueTask<int> readTask = _stream!.ReadAsync(_buffer.AsMemory(), cancellationToken);
                startedAsyncWork = true;

                return ReadAsyncCore(readTask, buffer, cancellationToken);
            }
            finally
            {
                // if we haven't started any async work, decrement the counter to end the transaction
                if (!startedAsyncWork)
                {
                    Interlocked.Decrement(ref _asyncOperations);
                }
            }
        }

        private async ValueTask<int> ReadAsyncCore(ValueTask<int> readTask, Memory<byte> buffer, CancellationToken cancellationToken)
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
                    bytesRead = _inflater.Inflate(buffer.Span);

                    if (bytesRead == 0 && !_inflater.Finished())
                    {
                        // We could have read in head information and didn't get any data.
                        // Read from the base stream again.
                        readTask = _stream!.ReadAsync(_buffer.AsMemory(), cancellationToken);
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

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            // We use this checking order for compat to earlier versions:
            if (_asyncOperations != 0)
                throw new InvalidOperationException(SR.InvalidBeginCall);

            ValidateBufferArguments(buffer, offset, count);
            EnsureNotDisposed();

            return ReadAsyncInternal(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            // We use this checking order for compat to earlier versions:
            if (_asyncOperations != 0)
                throw new InvalidOperationException(SR.InvalidBeginCall);

            EnsureNotDisposed();

            return ReadAsyncInternal(buffer, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException(SR.CannotWriteToDeflateStream);
        }

        // This is called by Dispose:
        private void PurgeBuffers(bool disposing)
        {
            if (!disposing)
                return;

            if (_stream == null)
                return;

            Flush();
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
                    if (disposing && _stream != null)
                        _stream.Dispose();
                }
                finally
                {
                    _stream = null!;

                    try
                    {
                        _inflater?.Dispose();
                    }
                    finally
                    {
                        _inflater = null!;
                        base.Dispose(disposing);
                    }
                }
            }
        }
    }
}
