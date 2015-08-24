// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;

namespace System.IO.Compression
{
    public partial class DeflateStream : Stream
    {
        internal const int DefaultBufferSize = 8192;

        private Stream _stream;
        private CompressionMode _mode;
        private bool _leaveOpen;
        private IInflater _inflater;
        private IDeflater _deflater;
        private byte[] _buffer;

        private int _asyncOperations;

        private IFileFormatWriter _formatWriter;
        private bool _wroteHeader;
        private bool _wroteBytes;

        private enum WorkerType : byte { Unknown = 0, Managed = 1, ZLib = 2 };
        private static readonly WorkerType s_deflaterType = GetDeflaterType();
#if DEBUG
        // This field is used for testing purposes and is accessed via reflection.
        // NOTE: If the name of this field changes, the test must also be updated.
        private static WorkerType s_forcedTestingDeflaterType = WorkerType.Unknown;
#endif

        public DeflateStream(Stream stream, CompressionMode mode)
            : this(stream, mode, false)
        {
        }

        // Since a reader is being taken, CompressionMode.Decompress is implied
        internal DeflateStream(Stream stream, bool leaveOpen, IFileFormatReader reader)
        {
            Debug.Assert(reader != null, "The IFileFormatReader passed to the internal DeflateStream constructor must be non-null");
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (!stream.CanRead)
                throw new ArgumentException(SR.NotReadableStream, "stream");

            _inflater = CreateInflater(reader);
            _stream = stream;
            _mode = CompressionMode.Decompress;
            _leaveOpen = leaveOpen;
            _buffer = new byte[DefaultBufferSize];
        }


        public DeflateStream(Stream stream, CompressionMode mode, bool leaveOpen)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            
            switch (mode)
            {
                case CompressionMode.Decompress:
                    if (!stream.CanRead)
                    {
                        throw new ArgumentException(SR.NotReadableStream, "stream");
                    }
                    _inflater = CreateInflater();
                    break;

                case CompressionMode.Compress:
                    if (!stream.CanWrite)
                    {
                        throw new ArgumentException(SR.NotWriteableStream, "stream");
                    }
                    _deflater = CreateDeflater(null);
                    break;

                default:
                    throw new ArgumentException(SR.ArgumentOutOfRange_Enum, "mode");
            }

            _stream = stream;
            _mode = mode;
            _leaveOpen = leaveOpen;
            _buffer = new byte[DefaultBufferSize];
        }

        // Implies mode = Compress
        public DeflateStream(Stream stream, CompressionLevel compressionLevel)

            : this(stream, compressionLevel, false)
        {
        }

        // Implies mode = Compress
        public DeflateStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            if (!stream.CanWrite)
                throw new ArgumentException(SR.NotWriteableStream, "stream");

            // Checking of compressionLevel is passed down to the IDeflater implementation as it
            // is a pugable component that completely encapsulates the meaning of compressionLevel.

            Contract.EndContractBlock();

            _stream = stream;
            _mode = CompressionMode.Compress;
            _leaveOpen = leaveOpen;

            _deflater = CreateDeflater(compressionLevel);

            _buffer = new byte[DefaultBufferSize];
        }

        private static IDeflater CreateDeflater(CompressionLevel? compressionLevel)
        {
            // The deflator type (zlib or managed) is normally determined by s_deflatorType,
            // which is initialized by the provider based on what's available on the system.
            // But for testing purposes, we sometimes want to override this, forcing
            // compression/decompression to use a particular type.
            WorkerType deflatorType = s_deflaterType;
#if DEBUG
            if (s_forcedTestingDeflaterType != WorkerType.Unknown)
                deflatorType = s_forcedTestingDeflaterType;
#endif

            if (deflatorType == WorkerType.ZLib)
            {
                return compressionLevel.HasValue ?
                    new DeflaterZLib(compressionLevel.Value) :
                    new DeflaterZLib();
            }
            else
            {
                Debug.Assert(deflatorType == WorkerType.Managed);
                return new DeflaterManaged();
            }
        }

        private static IInflater CreateInflater(IFileFormatReader reader = null)
        {
            // The deflator type (zlib or managed) is normally determined by s_deflatorType,
            // which is initialized by the provider based on what's available on the system.
            // But for testing purposes, we sometimes want to override this, forcing
            // compression/decompression to use a particular type.
            WorkerType deflatorType = s_deflaterType;
#if DEBUG
            if (s_forcedTestingDeflaterType != WorkerType.Unknown)
                deflatorType = s_forcedTestingDeflaterType;
#endif

            if (deflatorType == WorkerType.ZLib)
            {
                // Rather than reading raw data and using a FormatReader to interpret
                // headers/footers manually, we instead set the zlib stream to parse
                // that information for us.
                if (reader == null)
                    return new InflaterZlib(ZLibNative.Deflate_DefaultWindowBits);
                else
                {
                    Debug.Assert(reader.ZLibWindowSize == 47, "A GZip reader must be designated with ZLibWindowSize == 47. Other header formats aren't supported by ZLib.");
                    return new InflaterZlib(reader.ZLibWindowSize);
                }
            }
            else
            {
                return new InflaterManaged(reader);
            }
        }

        internal void SetFileFormatWriter(IFileFormatWriter writer)
        {
            if (writer != null)
            {
                _formatWriter = writer;
            }
        }

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
                throw new ArgumentNullException("array");

            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset");

            if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            if (array.Length - offset < count)
                throw new ArgumentException(SR.InvalidArgumentOffsetCount);
        }

        private void EnsureNotDisposed()
        {
            if (_stream == null)
                throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
        }

        private void EnsureDecompressionMode()
        {
            if (_mode != CompressionMode.Decompress)
                throw new InvalidOperationException(SR.CannotReadFromDeflateStream);
        }

        private void EnsureCompressionMode()
        {
            if (_mode != CompressionMode.Compress)
                throw new InvalidOperationException(SR.CannotWriteToDeflateStream);
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
                    throw new InvalidOperationException(SR.NotReadableStream);
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
                            throw new InvalidOperationException(SR.NotReadableStream);
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
            EnsureCompressionMode();
            ValidateParameters(array, offset, count);
            EnsureNotDisposed();
            InternalWrite(array, offset, count);
        }

        internal void InternalWrite(byte[] array, int offset, int count)
        {
            DoMaintenance(array, offset, count);

            // Write compressed the bytes we already passed to the deflater:

            WriteDeflaterOutput();

            // Pass new bytes through deflater and write them too:

            _deflater.SetInput(array, offset, count);
            WriteDeflaterOutput();
        }


        private void WriteDeflaterOutput()
        {
            while (!_deflater.NeedsInput())
            {
                int compressedBytes = _deflater.GetDeflateOutput(_buffer);
                if (compressedBytes > 0)
                    DoWrite(_buffer, 0, compressedBytes);
            }
        }

        private void DoWrite(byte[] array, int offset, int count)
        {
            Debug.Assert(array != null);
            Debug.Assert(count != 0);

            _stream.Write(array, offset, count);
        }

        // Perform deflate-mode maintenance required due to custom header and footer writers
        // (e.g. set by GZipStream):
        private void DoMaintenance(byte[] array, int offset, int count)
        {
            // If no bytes written, do nothing:
            if (count <= 0)
                return;

            // Note that stream contains more than zero data bytes:
            _wroteBytes = true;

            // If no header/footer formatter present, nothing else to do:
            if (_formatWriter == null)
                return;

            // If formatter has not yet written a header, do it now:
            if (!_wroteHeader)
            {
                byte[] b = _formatWriter.GetHeader();
                _stream.Write(b, 0, b.Length);
                _wroteHeader = true;
            }

            // Inform formatter of the data bytes written:
            _formatWriter.UpdateWithBytesRead(array, offset, count);
        }

        // This is called by Dispose:
        private void PurgeBuffers(bool disposing)
        {
            if (!disposing)
                return;

            if (_stream == null)
                return;

            Flush();

            if (_mode != CompressionMode.Compress)
                return;

            // Some deflaters (e.g. ZLib) write more than zero bytes for zero byte inputs.
            // This round-trips and we should be ok with this, but our legacy managed deflater
            // always wrote zero output for zero input and upstack code (e.g. GZipStream)
            // took dependencies on it. Thus, make sure to only "flush" when we actually had
            // some input:
            if (_wroteBytes)
            {
                // Compress any bytes left:                        
                WriteDeflaterOutput();

                // Pull out any bytes left inside deflater:
                bool finished;
                do
                {
                    int compressedBytes;
                    finished = _deflater.Finish(_buffer, out compressedBytes);

                    if (compressedBytes > 0)
                        DoWrite(_buffer, 0, compressedBytes);
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

            // Write format footer:
            if (_formatWriter != null && _wroteHeader)
            {
                byte[] b = _formatWriter.GetFooter();
                _stream.Write(b, 0, b.Length);
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
                        _deflater = null;
                        _inflater = null;
                        base.Dispose(disposing);
                    }
                }  // finally
            }  // finally
        }  // Dispose

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
                await base.WriteAsync(array, offset, count, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref _asyncOperations);
            }
        }

    }  // public class DeflateStream
}  // namespace System.IO.Compression

