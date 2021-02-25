// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    /// <summary>Provides methods and properties for compressing and decompressing streams by using the Deflate algorithm.</summary>
    /// <remarks>This class represents the Deflate algorithm, which is an industry-standard algorithm for lossless file compression and decompression. Starting with the .NET Framework 4.5, the <see cref="System.IO.Compression.DeflateStream" /> class uses the zlib library. As a result, it provides a better compression algorithm and, in most cases, a smaller compressed file than it provides in earlier versions of the .NET Framework.
    /// This class does not inherently provide functionality for adding files to or extracting files from zip archives. To work with zip archives, use the <see cref="System.IO.Compression.ZipArchive" /> and the <see cref="System.IO.Compression.ZipArchiveEntry" /> classes.
    /// The <see cref="System.IO.Compression.DeflateStream" /> class uses the same compression algorithm as the gzip data format used by the <see cref="System.IO.Compression.GZipStream" /> class.
    /// The compression functionality in <see cref="System.IO.Compression.DeflateStream" /> and <see cref="System.IO.Compression.GZipStream" /> is exposed as a stream. Data is read on a byte-by-byte basis, so it is not possible to perform multiple passes to determine the best method for compressing entire files or large blocks of data. The <see cref="System.IO.Compression.DeflateStream" /> and <see cref="System.IO.Compression.GZipStream" /> classes are best used on uncompressed sources of data. If the source data is already compressed, using these classes may actually increase the size of the stream.</remarks>
    /// <example>The following example shows how to use the <see cref="System.IO.Compression.DeflateStream" /> class to compress and decompress a directory of files.
    /// <format type="text/markdown"><![CDATA[
    /// [!code-csharp[IO.Compression.Deflate1#1](~/samples/snippets/csharp/VS_Snippets_CLR/IO.Compression.Deflate1/CS/deflatetest.cs#1)]
    /// [!code-vb[IO.Compression.Deflate1#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/IO.Compression.Deflate1/VB/deflatetest.vb#1)]
    /// ]]></format></example>
    public partial class DeflateStream : Stream
    {
        private const int DefaultBufferSize = 8192;

        private Stream _stream;
        private CompressionMode _mode;
        private bool _leaveOpen;
        private Inflater? _inflater;
        private Deflater? _deflater;
        private byte[]? _buffer;
        private int _activeAsyncOperation; // 1 == true, 0 == false
        private bool _wroteBytes;

        internal DeflateStream(Stream stream, CompressionMode mode, long uncompressedSize) : this(stream, mode, leaveOpen: false, ZLibNative.Deflate_DefaultWindowBits, uncompressedSize)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="System.IO.Compression.DeflateStream" /> class by using the specified stream and compression mode.</summary>
        /// <param name="stream">The stream to compress or decompress.</param>
        /// <param name="mode">One of the enumeration values that indicates whether to compress or decompress the stream.</param>
        /// <remarks>By default, <see cref="System.IO.Compression.DeflateStream" /> owns the underlying stream, so closing the stream also closes the underlying stream. Note that the state of the underlying stream can affect the usability of the stream. Also, no explicit checks are performed, so no additional exceptions are thrown when the new instance is created.
        /// If an instance of the <see cref="System.IO.Compression.DeflateStream" /> class is created with the <paramref name="mode" /> parameter equal to `Compress`, header information is inserted immediately. If no further action occurs, the stream appears as a valid, empty, compressed file.
        /// Using the <see cref="System.IO.Compression.DeflateStream" /> class to compress a file larger than 4 GB raises an exception.
        /// By default, the compression level is set to <see cref="System.IO.Compression.CompressionLevel.Optimal" /> when the compression mode is <see cref="System.IO.Compression.CompressionMode.Compress" />.</remarks>
        /// <example>The following example shows how to use the <see cref="System.IO.Compression.DeflateStream" /> class to compress and decompress a file.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[IO.Compression.Deflate1#1](~/samples/snippets/csharp/VS_Snippets_CLR/IO.Compression.Deflate1/CS/deflatetest.cs#1)]
        /// [!code-vb[IO.Compression.Deflate1#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/IO.Compression.Deflate1/VB/deflatetest.vb#1)]
        /// ]]></format></example>
        /// <exception cref="System.ArgumentNullException"><paramref name="stream" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="mode" /> is not a valid <see cref="System.IO.Compression.CompressionMode" /> value.
        /// -or-
        /// <see cref="System.IO.Compression.CompressionMode" /> is <see cref="System.IO.Compression.CompressionMode.Compress" /> and <see cref="System.IO.Stream.CanWrite" /> is <see langword="false" />.
        /// -or-
        /// <see cref="System.IO.Compression.CompressionMode" /> is <see cref="System.IO.Compression.CompressionMode.Decompress" /> and <see cref="System.IO.Stream.CanRead" /> is <see langword="false" />.</exception>
        public DeflateStream(Stream stream, CompressionMode mode) : this(stream, mode, leaveOpen: false)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="System.IO.Compression.DeflateStream" /> class by using the specified stream and compression mode, and optionally leaves the stream open.</summary>
        /// <param name="stream">The stream to compress or decompress.</param>
        /// <param name="mode">One of the enumeration values that indicates whether to compress or decompress the stream.</param>
        /// <param name="leaveOpen"><see langword="true" /> to leave the stream open after disposing the <see cref="System.IO.Compression.DeflateStream" /> object; otherwise, <see langword="false" />.</param>
        /// <remarks>By default, <see cref="System.IO.Compression.DeflateStream" /> owns the underlying stream, so closing the stream also closes the underlying stream. Note that the state of the underlying stream can affect the usability of the stream. Also, no explicit checks are performed, so no additional exceptions are thrown when the new instance is created.
        /// If an instance of the <see cref="System.IO.Compression.DeflateStream" /> class is created with the <paramref name="mode" /> parameter equal to `Compress`, header information is inserted immediately. If no further action occurs, the stream appears as a valid, empty, compressed file.
        /// Using the <see cref="System.IO.Compression.DeflateStream" /> class to compress a file larger than 4 GB raises an exception.
        /// By default, the compression level is set to <see cref="System.IO.Compression.CompressionLevel.Optimal" /> when the compression mode is <see cref="System.IO.Compression.CompressionMode.Compress" />.</remarks>
        /// <example>The following code example shows how to use the <see cref="System.IO.Compression.DeflateStream" /> class to compress and decompress a file.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[IO.Compression.Deflate1#1](~/samples/snippets/csharp/VS_Snippets_CLR/IO.Compression.Deflate1/CS/deflatetest.cs#1)]
        /// [!code-vb[IO.Compression.Deflate1#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/IO.Compression.Deflate1/VB/deflatetest.vb#1)]
        /// ]]></format></example>
        /// <exception cref="System.ArgumentNullException"><paramref name="stream" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="mode" /> is not a valid <see cref="System.IO.Compression.CompressionMode" /> value.
        /// -or-
        /// <see cref="System.IO.Compression.CompressionMode" /> is <see cref="System.IO.Compression.CompressionMode.Compress" /> and <see cref="System.IO.Stream.CanWrite" /> is <see langword="false" />.
        /// -or-
        /// <see cref="System.IO.Compression.CompressionMode" /> is <see cref="System.IO.Compression.CompressionMode.Decompress" /> and <see cref="System.IO.Stream.CanRead" /> is <see langword="false" />.</exception>
        public DeflateStream(Stream stream, CompressionMode mode, bool leaveOpen) : this(stream, mode, leaveOpen, ZLibNative.Deflate_DefaultWindowBits)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="System.IO.Compression.DeflateStream" /> class by using the specified stream and compression level.</summary>
        /// <param name="stream">The stream to compress.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates whether to emphasize speed or compression efficiency when compressing the stream.</param>
        /// <remarks>You use this constructor when you want to specify whether compression efficiency or speed is more important for an instance of the <see cref="System.IO.Compression.DeflateStream" /> class.
        /// This constructor overload uses the compression mode <see cref="System.IO.Compression.CompressionMode.Compress" />. To set the compression mode to another value, use the <see cref="System.IO.Compression.DeflateStream.%23ctor%28System.IO.Stream%2CSystem.IO.Compression.CompressionMode%29" /> or <see cref="System.IO.Compression.DeflateStream.%23ctor%28System.IO.Stream%2CSystem.IO.Compression.CompressionMode%2Cbool%29" /> overload.</remarks>
        /// <example>The following example shows how to set the compression level when creating a <see cref="System.IO.Compression.DeflateStream" /> object.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[System.IO.Compression.DeflateStream#1](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.deflatestream/cs/program1.cs#1)]
        /// [!code-vb[System.IO.Compression.DeflateStream#1](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.deflatestream/vb/program1.vb#1)]
        /// ]]></format></example>
        /// <exception cref="System.ArgumentNullException"><paramref name="stream" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">The stream does not support write operations such as compression. (The <see cref="System.IO.Stream.CanWrite" /> property on the stream object is <see langword="false" />.)</exception>
        public DeflateStream(Stream stream, CompressionLevel compressionLevel) : this(stream, compressionLevel, leaveOpen: false)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="System.IO.Compression.DeflateStream" /> class by using the specified stream and compression level, and optionally leaves the stream open.</summary>
        /// <param name="stream">The stream to compress.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates whether to emphasize speed or compression efficiency when compressing the stream.</param>
        /// <param name="leaveOpen"><see langword="true" /> to leave the stream object open after disposing the <see cref="System.IO.Compression.DeflateStream" /> object; otherwise, <see langword="false" />.</param>
        /// <remarks>You use this constructor when you want to specify whether compression efficiency or speed is more important for an instance of the <see cref="System.IO.Compression.DeflateStream" /> class, and whether to leave the stream object open after disposing the <see cref="System.IO.Compression.DeflateStream" /> object.
        /// This constructor overload uses the compression mode <see cref="System.IO.Compression.CompressionMode.Compress" />. To set the compression mode to another value, use the <see cref="System.IO.Compression.DeflateStream.%23ctor%28System.IO.Stream%2CSystem.IO.Compression.CompressionMode%29" /> or <see cref="System.IO.Compression.DeflateStream.%23ctor%28System.IO.Stream%2CSystem.IO.Compression.CompressionMode%2Cbool%29" /> overload.</remarks>
        /// <example>The following example shows how to set the compression level when creating a <see cref="System.IO.Compression.DeflateStream" /> object and how to leave the stream open.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[System.IO.Compression.DeflateStream#2](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.deflatestream/cs/program2.cs#2)]
        /// [!code-vb[System.IO.Compression.DeflateStream#2](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.deflatestream/vb/program2.vb#2)]
        /// ]]></format></example>
        /// <exception cref="System.ArgumentNullException"><paramref name="stream" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">The stream does not support write operations such as compression. (The <see cref="System.IO.Stream.CanWrite" /> property on the stream object is <see langword="false" />.)</exception>
        public DeflateStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen) : this(stream, compressionLevel, leaveOpen, ZLibNative.Deflate_DefaultWindowBits)
        {
        }

        /// <summary>
        /// Internal constructor to check stream validity and call the correct initialization function depending on
        /// the value of the CompressionMode given.
        /// </summary>
        internal DeflateStream(Stream stream, CompressionMode mode, bool leaveOpen, int windowBits, long uncompressedSize = -1)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            switch (mode)
            {
                case CompressionMode.Decompress:
                    if (!stream.CanRead)
                        throw new ArgumentException(SR.NotSupported_UnreadableStream, nameof(stream));

                    _inflater = new Inflater(windowBits, uncompressedSize);
                    _stream = stream;
                    _mode = CompressionMode.Decompress;
                    _leaveOpen = leaveOpen;
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
        /// Sets up this DeflateStream to be used for Zlib Deflation/Compression
        /// </summary>
        [MemberNotNull(nameof(_stream))]
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

        /// <summary>Gets a reference to the underlying stream.</summary>
        /// <value>A stream object that represents the underlying stream.</value>
        /// <exception cref="System.ObjectDisposedException">The underlying stream is closed.</exception>
        public Stream BaseStream => _stream;

        /// <summary>Gets a value indicating whether the stream supports reading while decompressing a file.</summary>
        /// <value><see langword="true" /> if the <see cref="System.IO.Compression.CompressionMode" /> value is <see langword="Decompress" />, and the underlying stream is opened and supports reading; otherwise, <see langword="false" />.</value>
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

        /// <summary>Gets a value indicating whether the stream supports writing.</summary>
        /// <value><see langword="true" /> if the <see cref="System.IO.Compression.CompressionMode" /> value is <see langword="Compress" />, and the underlying stream supports writing and is not closed; otherwise, <see langword="false" />.</value>
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

        /// <summary>Gets a value indicating whether the stream supports seeking.</summary>
        /// <value><see langword="false" /> in all cases.</value>
        public override bool CanSeek => false;

        /// <summary>This property is not supported and always throws a <see cref="System.NotSupportedException" />.</summary>
        /// <value>A long value.</value>
        /// <exception cref="System.NotSupportedException">This property is not supported on this stream.</exception>
        public override long Length
        {
            get { throw new NotSupportedException(SR.NotSupported); }
        }

        /// <summary>This property is not supported and always throws a <see cref="System.NotSupportedException" />.</summary>
        /// <value>A long value.</value>
        /// <exception cref="System.NotSupportedException">This property is not supported on this stream.</exception>
        public override long Position
        {
            get { throw new NotSupportedException(SR.NotSupported); }
            set { throw new NotSupportedException(SR.NotSupported); }
        }

        /// <summary>The current implementation of this method has no functionality.</summary>
        /// <remarks>Flushes the internal buffer if the compression mode is set to <see cref="System.IO.Compression.CompressionMode.Compress" />.</remarks>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public override void Flush()
        {
            EnsureNotDisposed();
            if (_mode == CompressionMode.Compress)
                FlushBuffers();
        }

        /// <summary>Asynchronously clears all buffers for this Deflate stream, causes any buffered data to be written to the underlying device, and monitors cancellation requests.</summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous flush operation.</returns>
        /// <remarks>If the operation is canceled before it completes, the returned task contains the <see cref="System.Threading.Tasks.TaskStatus.Canceled" /> value for the <see cref="System.Threading.Tasks.Task.Status" /> property.</remarks>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            EnsureNoActiveAsyncOperation();
            EnsureNotDisposed();

            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            return _mode != CompressionMode.Compress ?
                Task.CompletedTask :
                Core(cancellationToken);

            async Task Core(CancellationToken cancellationToken)
            {
                AsyncOperationStarting();
                try
                {
                    Debug.Assert(_deflater != null && _buffer != null);

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

                    // Always flush on the underlying stream
                    await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    AsyncOperationCompleting();
                }
            }
        }

        /// <summary>This operation is not supported and always throws a <see cref="System.NotSupportedException" />.</summary>
        /// <param name="offset">The location in the stream.</param>
        /// <param name="origin">One of the <see cref="System.IO.SeekOrigin" /> values.</param>
        /// <returns>A long value.</returns>
        /// <exception cref="System.NotSupportedException">This property is not supported on this stream.</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(SR.NotSupported);
        }

        /// <summary>This operation is not supported and always throws a <see cref="System.NotSupportedException" />.</summary>
        /// <param name="value">The length of the stream.</param>
        /// <exception cref="System.NotSupportedException">This property is not supported on this stream.</exception>
        public override void SetLength(long value)
        {
            throw new NotSupportedException(SR.NotSupported);
        }

        /// <summary>Reads a byte from the Deflate stream and advances the position within the stream by one byte, or returns -1 if at the end of the Deflate stream.</summary>
        /// <returns>The unsigned byte cast to an <see cref="int" />, or -1 if at the end of the stream.</returns>
        /// <remarks>Use the <see cref="System.IO.Compression.DeflateStream.CanRead" /> property to determine whether the current instance supports reading.</remarks>
        public override int ReadByte()
        {
            EnsureDecompressionMode();
            EnsureNotDisposed();

            // Try to read a single byte from zlib without allocating an array, pinning an array, etc.
            // If zlib doesn't have any data, fall back to the base stream implementation, which will do that.
            byte b;
            Debug.Assert(_inflater != null);
            return _inflater.Inflate(out b) ? b : base.ReadByte();
        }

        /// <summary>Reads a number of decompressed bytes into the specified byte array.</summary>
        /// <param name="array">The array to store decompressed bytes.</param>
        /// <param name="offset">The byte offset in <paramref name="array" /> at which the read bytes will be placed.</param>
        /// <param name="count">The maximum number of decompressed bytes to read.</param>
        /// <returns>The number of bytes that were read into the byte array.</returns>
        /// <remarks></remarks>
        /// <example>The following example shows how to compress and decompress bytes by using the <see cref="System.IO.Compression.DeflateStream.Read" /> and <see cref="System.IO.Compression.DeflateStream.Write" /> methods.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[System.IO.Compression.DeflateStream#3](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.deflatestream/cs/program3.cs#3)]
        /// [!code-vb[System.IO.Compression.DeflateStream#3](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.deflatestream/vb/program3.vb#3)]
        /// ]]></format></example>
        /// <exception cref="System.ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
        /// <exception cref="System.InvalidOperationException">The <see cref="System.IO.Compression.CompressionMode" /> value was <see langword="Compress" /> when the object was created.
        /// -or-
        /// The underlying stream does not support reading.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="offset" /> or <paramref name="count" /> is less than zero.
        /// -or-
        /// <paramref name="array" /> length minus the index starting point is less than <paramref name="count" />.</exception>
        /// <exception cref="System.IO.InvalidDataException">The data is in an invalid format.</exception>
        /// <exception cref="System.ObjectDisposedException">The stream is closed.</exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            return ReadCore(new Span<byte>(buffer, offset, count));
        }

        /// <summary>Reads a sequence of bytes from the current Deflate stream into a byte span and advances the position within the Deflate stream by the number of bytes read.</summary>
        /// <param name="buffer">A region of memory. When this method returns, the contents of this region are replaced by the bytes read from the current source.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes allocated in the buffer if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        /// <remarks>Use the <see cref="System.IO.Compression.DeflateStream.CanRead" /> property to determine whether the current instance supports reading. Use the <see cref="System.IO.Compression.DeflateStream.ReadAsync" /> method to read asynchronously from the current stream.
        /// This method read a maximum of `buffer.Length` bytes from the current stream and store them in <paramref name="buffer" />. The current position within the Deflate stream is advanced by the number of bytes read; however, if an exception occurs, the current position within the Deflate stream remains unchanged. This method will block until at least one byte of data can be read, in the event that no data is available. `Read` returns 0 only when there is no more data in the stream and no more is expected (such as a closed socket or end of file). The method is free to return fewer bytes than requested even if the end of the stream has not been reached.
        /// Use <see cref="System.IO.BinaryReader" /> for reading primitive data types.</remarks>
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

            Debug.Assert(_inflater != null);
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
                    Debug.Assert(_buffer != null);
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

        private void EnsureNotDisposed()
        {
            if (_stream == null)
                ThrowStreamClosedException();
        }

        private static void ThrowStreamClosedException()
        {
            throw new ObjectDisposedException(nameof(DeflateStream), SR.ObjectDisposed_StreamClosed);
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

        /// <summary>Begins an asynchronous read operation. (Consider using the <see cref="System.IO.Stream.ReadAsync(byte[],int,int)" /> method instead.)</summary>
        /// <param name="buffer">The byte array to read the data into.</param>
        /// <param name="array">The byte array to read the data into.</param>
        /// <param name="offset">The byte offset in <paramref name="array" /> at which to begin reading data from the stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="asyncCallback">An optional asynchronous callback, to be called when the read operation is complete.</param>
        /// <param name="cback">An optional asynchronous callback, to be called when the read operation is complete.</param>
        /// <param name="asyncState">A user-provided object that distinguishes this particular asynchronous read request from other requests.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous read request from other requests.</param>
        /// <returns>An  object that represents the asynchronous read operation, which could still be pending.</returns>
        /// <remarks>Starting with the .NET Framework 4.5, you can perform asynchronous read operations by using the <see cref="System.IO.Stream.ReadAsync" /> method. The <see cref="System.IO.Compression.DeflateStream.BeginRead" /> method is still available in the .NET Framework 4.5 to support legacy code; however, you can implement asynchronous I/O operations more easily by using the new async methods. For more information, see [Asynchronous File I/O](/dotnet/standard/io/asynchronous-file-i-o).
        /// Pass the <see cref="System.IAsyncResult" /> return value to the <see cref="System.IO.Compression.DeflateStream.EndRead" /> method of the stream to determine how many bytes were read and to release operating system resources used for reading. You can do this either by using the same code that called <see cref="System.IO.Compression.DeflateStream.BeginRead" /> or in a callback passed to <see cref="System.IO.Compression.DeflateStream.BeginRead" />.
        /// The current position in the stream is updated when the asynchronous read or write operation is issued, not when the I/O operation completes.
        /// Multiple simultaneous asynchronous requests render the request completion order uncertain.
        /// Use the <see cref="System.IO.Compression.DeflateStream.CanRead" /> property to determine whether the current <see cref="System.IO.Compression.DeflateStream" /> object supports reading.
        /// If a stream is closed or you pass an invalid argument, exceptions are thrown immediately from <see cref="System.IO.Compression.DeflateStream.BeginRead" />. Errors that occur during an asynchronous read request, such as a disk failure during the I/O request, occur on the thread pool thread and throw exceptions when calling <see cref="System.IO.Compression.DeflateStream.EndRead" />.</remarks>
        /// <exception cref="System.IO.IOException">The method tried to read asynchronously past the end of the stream, or a disk error occurred.</exception>
        /// <exception cref="System.ArgumentException">One or more of the arguments is invalid.</exception>
        /// <exception cref="System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <exception cref="System.NotSupportedException">The current <see cref="System.IO.Compression.DeflateStream" /> implementation does not support the read operation.</exception>
        /// <exception cref="System.InvalidOperationException">This call cannot be completed.</exception>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState) =>
            TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);

        /// <summary>Waits for the pending asynchronous read to complete. (Consider using the <see cref="System.IO.Stream.ReadAsync(byte[],int,int)" /> method instead.)</summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        /// <param name="async_result">The reference to the pending asynchronous request to finish.</param>
        /// <returns>The number of bytes read from the stream, between 0 (zero) and the number of bytes you requested. <see cref="System.IO.Compression.DeflateStream" /> returns 0 only at the end of the stream; otherwise, it blocks until at least one byte is available.</returns>
        /// <remarks>Starting with the .NET Framework 4.5, you can perform asynchronous read operations by using the <see cref="System.IO.Stream.ReadAsync" /> method. The <see cref="System.IO.Compression.DeflateStream.EndRead" /> method is still available in the .NET Framework 4.5 to support legacy code; however, you can implement asynchronous I/O operations more easily by using the new async methods. For more information, see [Asynchronous File I/O](/dotnet/standard/io/asynchronous-file-i-o).
        /// Call this method to determine how many bytes were read from the stream. This method can be called once to return the amount of bytes read between calls to <see cref="System.IO.Compression.DeflateStream.BeginRead" /> and <see cref="System.IO.Compression.DeflateStream.EndRead" />.
        /// This method blocks until the I/O operation has completed.</remarks>
        /// <exception cref="System.ArgumentNullException"><paramref name="asyncResult" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="asyncResult" /> did not originate from a <see cref="System.IO.Compression.DeflateStream.BeginRead(byte[],int,int,System.AsyncCallback,object)" /> method on the current stream.</exception>
        /// <exception cref="System.SystemException">An exception was thrown during a call to <see cref="System.Threading.WaitHandle.WaitOne" />.</exception>
        /// <exception cref="System.InvalidOperationException">The end call is invalid because asynchronous read operations for this stream are not yet complete.
        /// -or-
        /// The stream is <see langword="null" />.</exception>
        public override int EndRead(IAsyncResult asyncResult)
        {
            EnsureDecompressionMode();
            EnsureNotDisposed();
            return TaskToApm.End<int>(asyncResult);
        }

        /// <summary>Asynchronously reads a sequence of bytes from the current Deflate stream, writes them to a byte array, advances the position within the Deflate stream by the number of bytes read, and monitors cancellation requests.</summary>
        /// <param name="array">The buffer to write the data into.</param>
        /// <param name="offset">The byte offset in <paramref name="array" /> at which to begin writing data from the Deflate stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous read operation, which wraps the total number of bytes read into the <paramref name="array" />. The result value can be less than the number of bytes requested if the number of bytes currently available is less than the requested number, or it can be 0 (zero) if the end of the Deflate stream has been reached.</returns>
        /// <remarks>The `ReadAsync` method enables you to perform resource-intensive I/O operations without blocking the main thread. This performance consideration is particularly important in a Windows 8.x Store app or desktop app where a time-consuming stream operation can block the UI thread and make your app appear as if it is not working. The async methods are used in conjunction with the <see langword="async" /> and <see langword="await" /> keywords in Visual Basic and C#.
        /// Use the <see cref="System.IO.Compression.DeflateStream.CanRead" /> property to determine whether the current instance supports reading.
        /// If the operation is canceled before it completes, the returned task contains the <see cref="System.Threading.Tasks.TaskStatus.Canceled" /> value for the <see cref="System.Threading.Tasks.Task.Status" /> property.</remarks>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return ReadAsyncMemory(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        /// <summary>Asynchronously reads a sequence of bytes from the current Deflate stream, writes them to a byte memory range, advances the position within the Deflate stream by the number of bytes read, and monitors cancellation requests.</summary>
        /// <param name="buffer">The region of memory to write the data into.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous read operation, which wraps the total number of bytes read into the buffer. The result value can be less than the number of bytes allocated in the buffer if that many bytes are not currently available, or it can be 0 (zero) if the end of the Deflate stream has been reached.</returns>
        /// <remarks>The `ReadAsync` method enables you to perform resource-intensive I/O operations without blocking the main thread. This performance consideration is particularly important in a Windows 8.x Store app or desktop app where a time-consuming stream operation can block the UI thread and make your app appear as if it is not working. The async methods are used in conjunction with the <see langword="async" /> and <see langword="await" /> keywords in Visual Basic and C#.
        /// Use the <see cref="System.IO.Compression.DeflateStream.CanRead" /> property to determine whether the current instance supports reading.
        /// If the operation is canceled before it completes, the returned task contains the <see cref="System.Threading.Tasks.TaskStatus.Canceled" /> value for the <see cref="System.Threading.Tasks.Task.Status" /> property.</remarks>
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
                return ValueTask.FromCanceled<int>(cancellationToken);
            }

            EnsureBufferInitialized();

            return Core(buffer, cancellationToken);

            async ValueTask<int> Core(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                AsyncOperationStarting();
                try
                {
                    int totalRead = 0;

                    Debug.Assert(_inflater != null);
                    while (true)
                    {
                        int bytesRead = _inflater.Inflate(buffer.Span.Slice(totalRead));
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
                            Debug.Assert(_buffer != null);
                            int bytes = await _stream.ReadAsync(_buffer, cancellationToken).ConfigureAwait(false);
                            EnsureNotDisposed();
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
                finally
                {
                    AsyncOperationCompleting();
                }
            }
        }

        /// <summary>Writes compressed bytes to the underlying stream from the specified byte array.</summary>
        /// <param name="array">The buffer that contains the data to compress.</param>
        /// <param name="offset">The byte offset in <paramref name="array" /> from which the bytes will be read.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <remarks></remarks>
        /// <example>The following example shows how to compress and decompress bytes by using the <see cref="System.IO.Compression.DeflateStream.Read" /> and <see cref="System.IO.Compression.DeflateStream.Write" /> methods.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[System.IO.Compression.DeflateStream#3](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.deflatestream/cs/program3.cs#3)]
        /// [!code-vb[System.IO.Compression.DeflateStream#3](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.deflatestream/vb/program3.vb#3)]
        /// ]]></format></example>
        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            WriteCore(new ReadOnlySpan<byte>(buffer, offset, count));
        }

        /// <summary>Writes a sequence of bytes to the current Deflate stream and advances the current position within this Deflate stream by the number of bytes written.</summary>
        /// <param name="buffer">A region of memory. This method copies the contents of this region to the current Deflate stream.</param>
        /// <remarks>Use the <see cref="System.IO.Compression.DeflateStream.CanWrite" /> property to determine whether the current instance supports writing. Use the <see cref="System.IO.Compression.DeflateStream.WriteAsync" /> method to write asynchronously to the current stream.
        /// If the write operation is successful, the position within the Deflate stream advances by the number of bytes written. If an exception occurs, the position within the Deflate stream remains unchanged.</remarks>
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

            Debug.Assert(_deflater != null);
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
            Debug.Assert(_deflater != null && _buffer != null);
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
            if (_wroteBytes)
            {
                // Compress any bytes left:
                WriteDeflaterOutput();

                Debug.Assert(_deflater != null && _buffer != null);
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

            // Always flush on the underlying stream
            _stream.Flush();
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

            Debug.Assert(_deflater != null && _buffer != null);
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

        private async ValueTask PurgeBuffersAsync()
        {
            // Same logic as PurgeBuffers, except with async counterparts.

            if (_stream == null)
                return;

            if (_mode != CompressionMode.Compress)
                return;

            Debug.Assert(_deflater != null && _buffer != null);
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

        /// <summary>Releases the unmanaged resources used by the <see cref="System.IO.Compression.DeflateStream" /> and optionally releases the managed resources.</summary>
        /// <param name="disposing"><see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.</param>
        /// <remarks>This method is called by the public <see cref="System.ComponentModel.Component.Dispose" /> method and the <see cref="object.Finalize" /> method. <see cref="System.ComponentModel.Component.Dispose" /> invokes the protected <see cref="System.IO.Compression.DeflateStream.Dispose" /> method with the <paramref name="disposing" /> parameter set to <see langword="true" />. <see cref="object.Finalize" /> invokes <see cref="System.IO.Compression.DeflateStream.Dispose" /> with <paramref name="disposing" /> set to <see langword="false" />.
        /// When the <paramref name="disposing" /> parameter is <see langword="true" />, this method releases all resources held by any managed objects that this <see cref="System.IO.Compression.DeflateStream" /> references. This method invokes the <see cref="System.ComponentModel.Component.Dispose" /> method of each referenced object.</remarks>
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
                    _stream = null!;

                    try
                    {
                        _deflater?.Dispose();
                        _inflater?.Dispose();
                    }
                    finally
                    {
                        _deflater = null;
                        _inflater = null;

                        byte[]? buffer = _buffer;
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

        /// <summary>Asynchronously releases the unmanaged resources used by the <see cref="System.IO.Compression.DeflateStream" />.</summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        /// <remarks>The `DisposeAsync` method enables you to perform a resource-intensive dispose operation without blocking the main thread. This performance consideration is particularly important in a Windows 8.x Store app or desktop app where a time-consuming stream operation can block the UI thread and make your app appear as if it is not working. The async methods are used in conjunction with the <see langword="async" /> and <see langword="await" /> keywords in Visual Basic and C#.
        /// This method disposes the Deflate stream by writing any changes to the backing store and closing the stream to release resources.
        /// Calling `DisposeAsync` allows the resources used by the <see cref="System.IO.Compression.DeflateStream" /> to be reallocated for other purposes. For more information, see [Cleaning Up Unmanaged Resources](/dotnet/standard/garbage-collection/unmanaged).</remarks>
        public override ValueTask DisposeAsync()
        {
            return GetType() == typeof(DeflateStream) ?
                Core() :
                base.DisposeAsync();

            async ValueTask Core()
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
                    _stream = null!;
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

                            byte[]? buffer = _buffer;
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
        }

        /// <summary>Begins an asynchronous write operation. (Consider using the <see cref="System.IO.Stream.WriteAsync(byte[],int,int)" /> method instead.)</summary>
        /// <param name="array">The buffer to write data from.</param>
        /// <param name="buffer">The buffer to write data from.</param>
        /// <param name="offset">The byte offset in <paramref name="buffer" /> to begin writing from.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <param name="asyncCallback">An optional asynchronous callback, to be called when the write operation is complete.</param>
        /// <param name="cback">An optional asynchronous callback, to be called when the write operation is complete.</param>
        /// <param name="asyncState">A user-provided object that distinguishes this particular asynchronous write request from other requests.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous write request from other requests.</param>
        /// <returns>An object that represents the asynchronous write operation, which could still be pending.</returns>
        /// <remarks>Starting with the .NET Framework 4.5, you can perform asynchronous write operations by using the <see cref="System.IO.Stream.WriteAsync" /> method. The <see cref="System.IO.Compression.DeflateStream.BeginWrite" /> method is still available in the .NET Framework 4.5 to support legacy code; however, you can implement asynchronous I/O operations more easily by using the new async methods. For more information, see [Asynchronous File I/O](/dotnet/standard/io/asynchronous-file-i-o).
        /// Pass the <see cref="System.IAsyncResult" /> object returned by the current method to <see cref="System.IO.Compression.DeflateStream.EndWrite" /> to ensure that the write completes and frees resources appropriately. You can do this either by using the same code that called <see cref="System.IO.Compression.DeflateStream.BeginWrite" /> or in a callback passed to <see cref="System.IO.Compression.DeflateStream.BeginWrite" />. If an error occurs during an asynchronous write operation, an exception will not be thrown until <see cref="System.IO.Compression.DeflateStream.EndWrite" /> is called with the <see cref="System.IAsyncResult" /> returned by this method.
        /// If a stream is writable, writing at the end of the stream expands the stream.
        /// The current position in the stream is updated when you issue the asynchronous read or write operation, not when the I/O operation completes. Multiple simultaneous asynchronous requests render the request completion order uncertain.
        /// Use the <see cref="System.IO.Compression.DeflateStream.CanWrite" /> property to determine whether the current <see cref="System.IO.Compression.DeflateStream" /> object supports writing.
        /// If a stream is closed or you pass an invalid argument, exceptions are thrown immediately from <see cref="System.IO.Compression.DeflateStream.BeginWrite" />. Errors that occur during an asynchronous write request, such as a disk failure during the I/O request, occur on the thread pool thread and throw exceptions when calling <see cref="System.IO.Compression.DeflateStream.EndWrite" />.</remarks>
        /// <exception cref="System.IO.IOException">The method tried to write asynchronously past the end of the stream, or a disk error occurred.</exception>
        /// <exception cref="System.ArgumentException">One or more of the arguments is invalid.</exception>
        /// <exception cref="System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <exception cref="System.NotSupportedException">The current <see cref="System.IO.Compression.DeflateStream" /> implementation does not support the write operation.</exception>
        /// <exception cref="System.InvalidOperationException">The write operation cannot be performed because the stream is closed.</exception>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState) =>
            TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);

        /// <summary>Ends an asynchronous write operation. (Consider using the <see cref="System.IO.Stream.WriteAsync(byte[],int,int)" /> method instead.)</summary>
        /// <param name="asyncResult">A reference to the outstanding asynchronous I/O request.</param>
        /// <param name="async_result">A reference to the outstanding asynchronous I/O request.</param>
        /// <remarks>Starting with the .NET Framework 4.5, you can perform asynchronous write operations by using the <see cref="System.IO.Stream.WriteAsync" /> method. The <see cref="System.IO.Compression.DeflateStream.EndWrite" /> method is still available in the .NET Framework 4.5 to support legacy code; however, you can implement asynchronous I/O operations more easily by using the new async methods. For more information, see [Asynchronous File I/O](/dotnet/standard/io/asynchronous-file-i-o).
        /// <see cref="System.IO.Compression.DeflateStream.EndWrite" /> must be called only once for every call to the <see cref="System.IO.Compression.DeflateStream.BeginWrite" /> method.
        /// This method blocks until the I/O operation has completed. Errors that occur during an asynchronous write request, such as a disk failure during the I/O request, occur on the thread pool thread and become visible upon a call to <see cref="System.IO.Compression.DeflateStream.EndWrite" />. Exceptions thrown by the thread pool thread will not be visible when calling <see cref="System.IO.Compression.DeflateStream.EndWrite" />.</remarks>
        /// <exception cref="System.ArgumentNullException"><paramref name="asyncResult" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="asyncResult" /> did not originate from a <see cref="System.IO.Compression.DeflateStream.BeginWrite(byte[],int,int,System.AsyncCallback,object)" /> method on the current stream.</exception>
        /// <exception cref="System.Exception">An exception was thrown during a call to <see cref="System.Threading.WaitHandle.WaitOne" />.</exception>
        /// <exception cref="System.InvalidOperationException">The stream is <see langword="null" />.
        /// -or-
        /// The end write call is invalid.</exception>
        public override void EndWrite(IAsyncResult asyncResult)
        {
            EnsureCompressionMode();
            EnsureNotDisposed();
            TaskToApm.End(asyncResult);
        }

        /// <summary>Asynchronously writes compressed bytes to the underlying Deflate stream from the specified byte array.</summary>
        /// <param name="array">The buffer that contains the data to compress.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="array" /> from which to begin copying bytes to the Deflate stream.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <remarks>The `WriteAsync` method enables you to perform resource-intensive I/O operations without blocking the main thread. This performance consideration is particularly important in a Windows 8.x Store app or desktop app where a time-consuming stream operation can block the UI thread and make your app appear as if it is not working. The async methods are used in conjunction with the <see langword="async" /> and <see langword="await" /> keywords in Visual Basic and C#.
        /// Use the <see cref="System.IO.Compression.DeflateStream.CanWrite" /> property to determine whether the current instance supports writing.
        /// If the operation is canceled before it completes, the returned task contains the <see cref="System.Threading.Tasks.TaskStatus.Canceled" /> value for the <see cref="System.Threading.Tasks.Task.Status" /> property.</remarks>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return WriteAsyncMemory(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        /// <summary>Asynchronously writes compressed bytes to the underlying Deflate stream from the specified read-only memory region.</summary>
        /// <param name="buffer">The region of memory to write data from.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <remarks>The `WriteAsync` method enables you to perform resource-intensive I/O operations without blocking the main thread. This performance consideration is particularly important in a Windows 8.x Store app or desktop app where a time-consuming stream operation can block the UI thread and make your app appear as if it is not working. The async methods are used in conjunction with the <see langword="async" /> and <see langword="await" /> keywords in Visual Basic and C#.
        /// Use the <see cref="System.IO.Compression.DeflateStream.CanWrite" /> property to determine whether the current instance supports writing.
        /// If the operation is canceled before it completes, the returned task contains the <see cref="System.Threading.Tasks.TaskStatus.Canceled" /> value for the <see cref="System.Threading.Tasks.Task.Status" /> property.</remarks>
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

            return cancellationToken.IsCancellationRequested ?
                ValueTask.FromCanceled(cancellationToken) :
                Core(buffer, cancellationToken);

            async ValueTask Core(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
            {
                AsyncOperationStarting();
                try
                {
                    await WriteDeflaterOutputAsync(cancellationToken).ConfigureAwait(false);

                    Debug.Assert(_deflater != null);
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
        }

        /// <summary>
        /// Writes the bytes that have already been deflated
        /// </summary>
        private async ValueTask WriteDeflaterOutputAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(_deflater != null && _buffer != null);
            while (!_deflater.NeedsInput())
            {
                int compressedBytes = _deflater.GetDeflateOutput(_buffer);
                if (compressedBytes > 0)
                {
                    await _stream.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, compressedBytes), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>Reads the bytes from the current Deflate stream and writes them to another stream, using a specified buffer size.</summary>
        /// <param name="destination">The stream to which the contents of the current Deflate stream will be copied.</param>
        /// <param name="bufferSize">The size of the buffer. This value must be greater than zero. The default size is 81920.</param>
        /// <remarks>Copying begins at the current position in the current Deflate stream and does not reset the position of the destination stream after the copy operation is complete.</remarks>
        public override void CopyTo(Stream destination, int bufferSize)
        {
            ValidateCopyToArguments(destination, bufferSize);

            EnsureNotDisposed();
            if (!CanRead) throw new NotSupportedException();

            new CopyToStream(this, destination, bufferSize).CopyFromSourceToDestination();
        }

        /// <summary>Asynchronously reads the bytes from the current Deflate stream and writes them to another stream, using a specified buffer size.</summary>
        /// <param name="destination">The stream to which the contents of the current Deflate stream will be copied.</param>
        /// <param name="bufferSize">The size, in bytes, of the buffer. This value must be greater than zero. The default size is 81920.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        /// <remarks>The `CopyToAsync` method enables you to perform resource-intensive I/O operations without blocking the main thread. This performance consideration is particularly important in a Windows 8.x Store app or desktop app where a time-consuming stream operation can block the UI thread and make your app appear as if it is not working. The async methods are used in conjunction with the <see langword="async" /> and <see langword="await" /> keywords in Visual Basic and C#.
        /// If the operation is canceled before it completes, the returned task contains the <see cref="System.Threading.Tasks.TaskStatus.Canceled" /> value for the <see cref="System.Threading.Tasks.Task.Status" /> property.
        /// Copying begins at the current position in the current Deflate stream.</remarks>
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            ValidateCopyToArguments(destination, bufferSize);

            EnsureNotDisposed();
            if (!CanRead) throw new NotSupportedException();
            EnsureNoActiveAsyncOperation();

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
                    Debug.Assert(_deflateStream._inflater != null);
                    // Flush any existing data in the inflater to the destination stream.
                    while (!_deflateStream._inflater.Finished())
                    {
                        int bytesRead = _deflateStream._inflater.Inflate(_arrayPoolBuffer, 0, _arrayPoolBuffer.Length);
                        if (bytesRead > 0)
                        {
                            await _destination.WriteAsync(new ReadOnlyMemory<byte>(_arrayPoolBuffer, 0, bytesRead), _cancellationToken).ConfigureAwait(false);
                        }
                        else if (_deflateStream._inflater.NeedsInput())
                        {
                            // only break if we read 0 and ran out of input, if input is still available it may be another GZip payload
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
                    _arrayPoolBuffer = null!;
                }
            }

            public void CopyFromSourceToDestination()
            {
                try
                {
                    Debug.Assert(_deflateStream._inflater != null);
                    // Flush any existing data in the inflater to the destination stream.
                    while (!_deflateStream._inflater.Finished())
                    {
                        int bytesRead = _deflateStream._inflater.Inflate(_arrayPoolBuffer, 0, _arrayPoolBuffer.Length);
                        if (bytesRead > 0)
                        {
                            _destination.Write(_arrayPoolBuffer, 0, bytesRead);
                        }
                        else if (_deflateStream._inflater.NeedsInput())
                        {
                            // only break if we read 0 and ran out of input, if input is still available it may be another GZip payload
                            break;
                        }
                    }

                    // Now, use the source stream's CopyToAsync to push directly to our inflater via this helper stream
                    _deflateStream._stream.CopyTo(this, _arrayPoolBuffer.Length);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(_arrayPoolBuffer);
                    _arrayPoolBuffer = null!;
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

                Debug.Assert(_deflateStream._inflater != null);
                // Feed the data from base stream into the decompression engine.
                _deflateStream._inflater.SetInput(buffer, offset, count);

                // While there's more decompressed data available, forward it to the buffer stream.
                while (!_deflateStream._inflater.Finished())
                {
                    int bytesRead = _deflateStream._inflater.Inflate(new Span<byte>(_arrayPoolBuffer));
                    if (bytesRead > 0)
                    {
                        await _destination.WriteAsync(new ReadOnlyMemory<byte>(_arrayPoolBuffer, 0, bytesRead), cancellationToken).ConfigureAwait(false);
                    }
                    else if (_deflateStream._inflater.NeedsInput())
                    {
                        // only break if we read 0 and ran out of input, if input is still available it may be another GZip payload
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

                Debug.Assert(_deflateStream._inflater != null);
                // Feed the data from base stream into the decompression engine.
                _deflateStream._inflater.SetInput(buffer, offset, count);

                // While there's more decompressed data available, forward it to the buffer stream.
                while (!_deflateStream._inflater.Finished())
                {
                    int bytesRead = _deflateStream._inflater.Inflate(new Span<byte>(_arrayPoolBuffer));
                    if (bytesRead > 0)
                    {
                        _destination.Write(_arrayPoolBuffer, 0, bytesRead);
                    }
                    else if (_deflateStream._inflater.NeedsInput())
                    {
                        // only break if we read 0 and ran out of input, if input is still available it may be another GZip payload
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
