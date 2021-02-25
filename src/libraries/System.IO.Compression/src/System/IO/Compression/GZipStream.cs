// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    /// <summary>Provides methods and properties used to compress and decompress streams by using the GZip data format specification.</summary>
    /// <remarks>This class represents the gzip data format, which uses an industry-standard algorithm for lossless file compression and decompression. The format includes a cyclic redundancy check value for detecting data corruption. The gzip data format uses the same algorithm as the <see cref="System.IO.Compression.DeflateStream" /> class, but can be extended to use other compression formats. The format can be readily implemented in a manner not covered by patents.
    /// Starting with the .NET Framework 4.5, the <see cref="System.IO.Compression.DeflateStream" /> class uses the zlib library for compression. As a result, it provides a better compression algorithm and, in most cases, a smaller compressed file than it provides in earlier versions of the .NET Framework.
    /// Compressed <see cref="System.IO.Compression.GZipStream" /> objects written to a file with an extension of .gz can be decompressed using many common compression tools; however, this class does not inherently provide functionality for adding files to or extracting files from zip archives.
    /// The compression functionality in <see cref="System.IO.Compression.DeflateStream" /> and <see cref="System.IO.Compression.GZipStream" /> is exposed as a stream. Data is read on a byte-by-byte basis, so it is not possible to perform multiple passes to determine the best method for compressing entire files or large blocks of data. The <see cref="System.IO.Compression.DeflateStream" /> and <see cref="System.IO.Compression.GZipStream" /> classes are best used on uncompressed sources of data. If the source data is already compressed, using these classes may actually increase the size of the stream.</remarks>
    /// <example>The following example shows how to use the <see cref="System.IO.Compression.GZipStream" /> class to compress and decompress a directory of files.
    /// <format type="text/markdown"><![CDATA[
    /// [!code-csharp[IO.Compression.GZip1#1](~/samples/snippets/csharp/VS_Snippets_CLR/IO.Compression.GZip1/CS/gziptest.cs#1)]
    /// [!code-vb[IO.Compression.GZip1#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/IO.Compression.GZip1/VB/gziptest.vb#1)]
    /// ]]></format></example>
    public class GZipStream : Stream
    {
        private DeflateStream _deflateStream;

        /// <summary>Initializes a new instance of the <see cref="System.IO.Compression.GZipStream" /> class by using the specified stream and compression mode.</summary>
        /// <param name="stream">The stream the compressed or decompressed data is written to.</param>
        /// <param name="mode">One of the enumeration values that indicates whether to compress or decompress the stream.</param>
        /// <remarks>By default, <see cref="System.IO.Compression.GZipStream" /> owns the underlying stream, so closing the <paramref name="stream" /> parameter also closes the underlying stream. Note that the state of the underlying stream can affect the usability of the stream. Also, no explicit checks are performed, so no additional exceptions are thrown when the new instance is created.
        /// If an instance of the <see cref="System.IO.Compression.GZipStream" /> class is created with the <paramref name="mode" /> parameter equal to `Compress` and no further action occurs, the stream will appear as a valid, empty compressed file.
        /// By default, the compression level is set to <see cref="System.IO.Compression.CompressionLevel.Optimal" /> when the compression mode is <see cref="System.IO.Compression.CompressionMode.Compress" />.</remarks>
        /// <example>The following example initializes a new instance of the <see cref="System.IO.Compression.GZipStream" /> class with <paramref name="mode" /> set to <see cref="System.IO.Compression.CompressionMode.Compress" />. This example is part of a larger example provided for the <see cref="System.IO.Compression.GZipStream" /> class.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[IO.Compression.GZip1#1](~/samples/snippets/csharp/VS_Snippets_CLR/IO.Compression.GZip1/CS/gziptest.cs#1)]
        /// [!code-vb[IO.Compression.GZip1#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/IO.Compression.GZip1/VB/gziptest.vb#1)]
        /// ]]></format></example>
        /// <exception cref="System.ArgumentNullException"><paramref name="stream" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="mode" /> is not a valid <see cref="System.IO.Compression.CompressionMode" /> enumeration value.
        /// -or-
        /// <see cref="System.IO.Compression.CompressionMode" /> is <see cref="System.IO.Compression.CompressionMode.Compress" /> and <see cref="System.IO.Stream.CanWrite" /> is <see langword="false" />.
        /// -or-
        /// <see cref="System.IO.Compression.CompressionMode" /> is <see cref="System.IO.Compression.CompressionMode.Decompress" /> and <see cref="System.IO.Stream.CanRead" /> is <see langword="false" />.</exception>
        public GZipStream(Stream stream, CompressionMode mode) : this(stream, mode, leaveOpen: false)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="System.IO.Compression.GZipStream" /> class by using the specified stream and compression mode, and optionally leaves the stream open.</summary>
        /// <param name="stream">The stream to compress.</param>
        /// <param name="mode">One of the enumeration values that indicates whether to compress or decompress the stream.</param>
        /// <param name="leaveOpen"><see langword="true" /> to leave the stream open after disposing the <see cref="System.IO.Compression.GZipStream" /> object; otherwise, <see langword="false" />.</param>
        /// <remarks>By default, <see cref="System.IO.Compression.GZipStream" /> owns the underlying stream, so closing the <paramref name="stream" /> parameter also closes the underlying stream. Note that the state of the underlying stream can affect the usability of the stream. Also, no explicit checks are performed, so no additional exceptions are thrown when the new instance is created.
        /// If an instance of the <see cref="System.IO.Compression.GZipStream" /> class is created with the <paramref name="mode" /> parameter equal to `Compress` and no further action occurs, the stream will appear as a valid, empty compressed file.
        /// By default, the compression level is set to <see cref="System.IO.Compression.CompressionLevel.Optimal" /> when the compression mode is <see cref="System.IO.Compression.CompressionMode.Compress" />.</remarks>
        /// <exception cref="System.ArgumentNullException"><paramref name="stream" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="mode" /> is not a valid <see cref="System.IO.Compression.CompressionMode" /> value.
        /// -or-
        /// <see cref="System.IO.Compression.CompressionMode" /> is <see cref="System.IO.Compression.CompressionMode.Compress" /> and <see cref="System.IO.Stream.CanWrite" /> is <see langword="false" />.
        /// -or-
        /// <see cref="System.IO.Compression.CompressionMode" /> is <see cref="System.IO.Compression.CompressionMode.Decompress" /> and <see cref="System.IO.Stream.CanRead" /> is <see langword="false" />.</exception>
        public GZipStream(Stream stream, CompressionMode mode, bool leaveOpen)
        {
            _deflateStream = new DeflateStream(stream, mode, leaveOpen, ZLibNative.GZip_DefaultWindowBits);
        }

        /// <summary>Initializes a new instance of the <see cref="System.IO.Compression.GZipStream" /> class by using the specified stream and compression level.</summary>
        /// <param name="stream">The stream to compress.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates whether to emphasize speed or compression efficiency when compressing the stream.</param>
        /// <remarks>You use this constructor when you want to specify whether compression efficiency or speed is more important for an instance of the <see cref="System.IO.Compression.GZipStream" /> class.
        /// This constructor overload uses the compression mode <see cref="System.IO.Compression.CompressionMode.Compress" />. To set the compression mode to another value, use the <see cref="System.IO.Compression.GZipStream.%23ctor%28System.IO.Stream%2CSystem.IO.Compression.CompressionMode%29" /> or <see cref="System.IO.Compression.GZipStream.%23ctor%28System.IO.Stream%2CSystem.IO.Compression.CompressionMode%2Cbool%29" /> overload.</remarks>
        /// <example>The following example shows how to set the compression level when creating a <see cref="System.IO.Compression.GZipStream" /> object.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[System.IO.Compression.GZipStream#1](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.gzipstream/cs/program1.cs#1)]
        /// [!code-vb[System.IO.Compression.GZipStream#1](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.gzipstream/vb/program1.vb#1)]
        /// ]]></format></example>
        /// <exception cref="System.ArgumentNullException"><paramref name="stream" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">The stream does not support write operations such as compression. (The <see cref="System.IO.Stream.CanWrite" /> property on the stream object is <see langword="false" />.)</exception>
        public GZipStream(Stream stream, CompressionLevel compressionLevel) : this(stream, compressionLevel, leaveOpen: false)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="System.IO.Compression.GZipStream" /> class by using the specified stream and compression level, and optionally leaves the stream open.</summary>
        /// <param name="stream">The stream to write the compressed data to.</param>
        /// <param name="compressionLevel">One of the enumeration values that indicates whether to emphasize speed or compression efficiency when compressing the stream.</param>
        /// <param name="leaveOpen"><see langword="true" /> to leave the stream object open after disposing the <see cref="System.IO.Compression.GZipStream" /> object; otherwise, <see langword="false" />.</param>
        /// <remarks>You use this constructor when you want to specify whether compression efficiency or speed is more important for an instance of the <see cref="System.IO.Compression.GZipStream" /> class, and whether to leave the stream object open after disposing the <see cref="System.IO.Compression.GZipStream" /> object.
        /// This constructor overload uses the compression mode <see cref="System.IO.Compression.CompressionMode.Compress" />. To set the compression mode to another value, use the <see cref="System.IO.Compression.GZipStream.%23ctor%28System.IO.Stream%2CSystem.IO.Compression.CompressionMode%29" /> or <see cref="System.IO.Compression.GZipStream.%23ctor%28System.IO.Stream%2CSystem.IO.Compression.CompressionMode%2Cbool%29" /> overload.</remarks>
        /// <example>The following example shows how to set the compression level when creating a <see cref="System.IO.Compression.GZipStream" /> object and how to leave the stream open.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[System.IO.Compression.GZipStream#2](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.gzipstream/cs/program2.cs#2)]
        /// [!code-vb[System.IO.Compression.GZipStream#2](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.gzipstream/vb/program2.vb#2)]
        /// ]]></format></example>
        /// <exception cref="System.ArgumentNullException"><paramref name="stream" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException">The stream does not support write operations such as compression. (The <see cref="System.IO.Stream.CanWrite" /> property on the stream object is <see langword="false" />.)</exception>
        public GZipStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen)
        {
            _deflateStream = new DeflateStream(stream, compressionLevel, leaveOpen, ZLibNative.GZip_DefaultWindowBits);
        }

        /// <summary>Gets a value indicating whether the stream supports reading while decompressing a file.</summary>
        /// <value><see langword="true" /> if the <see cref="System.IO.Compression.CompressionMode" /> value is <see langword="Decompress," /> and the underlying stream supports reading and is not closed; otherwise, <see langword="false" />.</value>
        public override bool CanRead => _deflateStream?.CanRead ?? false;

        /// <summary>Gets a value indicating whether the stream supports writing.</summary>
        /// <value><see langword="true" /> if the <see cref="System.IO.Compression.CompressionMode" /> value is <see langword="Compress" />, and the underlying stream supports writing and is not closed; otherwise, <see langword="false" />.</value>
        public override bool CanWrite => _deflateStream?.CanWrite ?? false;

        /// <summary>Gets a value indicating whether the stream supports seeking.</summary>
        /// <value><see langword="false" /> in all cases.</value>
        public override bool CanSeek => _deflateStream?.CanSeek ?? false;

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

        /// <summary>Flushes the internal buffers.</summary>
        /// <remarks>This method flushes only if the current compression mode is <see cref="System.IO.Compression.CompressionMode.Compress" /> and the underlying stream still has some input left to write.</remarks>
        /// <exception cref="System.ObjectDisposedException">The underlying stream is closed.</exception>
        public override void Flush()
        {
            CheckDeflateStream();
            _deflateStream.Flush();
            return;
        }

        /// <summary>This property is not supported and always throws a <see cref="System.NotSupportedException" />.</summary>
        /// <param name="offset">The location in the stream.</param>
        /// <param name="origin">One of the <see cref="System.IO.SeekOrigin" /> values.</param>
        /// <returns>A long value.</returns>
        /// <exception cref="System.NotSupportedException">This property is not supported on this stream.</exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(SR.NotSupported);
        }

        /// <summary>This property is not supported and always throws a <see cref="System.NotSupportedException" />.</summary>
        /// <param name="value">The length of the stream.</param>
        /// <exception cref="System.NotSupportedException">This property is not supported on this stream.</exception>
        public override void SetLength(long value)
        {
            throw new NotSupportedException(SR.NotSupported);
        }

        /// <summary>Reads a byte from the GZip stream and advances the position within the stream by one byte, or returns -1 if at the end of the GZip stream.</summary>
        /// <returns>The unsigned byte cast to an <see cref="int" />, or -1 if at the end of the stream.</returns>
        /// <remarks>Use the <see cref="System.IO.Compression.GZipStream.CanRead" /> property to determine whether the current instance supports reading.</remarks>
        public override int ReadByte()
        {
            CheckDeflateStream();
            return _deflateStream.ReadByte();
        }

        /// <summary>Begins an asynchronous read operation. (Consider using the <see cref="System.IO.Stream.ReadAsync(byte[],int,int)" /> method instead.)</summary>
        /// <param name="array">The byte array to read the data into.</param>
        /// <param name="buffer">The byte array to read the data into.</param>
        /// <param name="offset">The byte offset in <paramref name="array" /> at which to begin reading data from the stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="asyncCallback">An optional asynchronous callback, to be called when the read operation is complete.</param>
        /// <param name="cback">An optional asynchronous callback, to be called when the read operation is complete.</param>
        /// <param name="asyncState">A user-provided object that distinguishes this particular asynchronous read request from other requests.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous read request from other requests.</param>
        /// <returns>An object that represents the asynchronous read operation, which could still be pending.</returns>
        /// <remarks>Starting with the .NET Framework 4.5, you can perform asynchronous read operations by using the <see cref="System.IO.Stream.ReadAsync" /> method. The <see cref="System.IO.Compression.GZipStream.BeginRead" /> method is still available in .NET Framework 4.5 to support legacy code; however, you can implement asynchronous I/O operations more easily by using the new async methods. For more information, see [Asynchronous File I/O](/dotnet/standard/io/asynchronous-file-i-o).
        /// Pass the <see cref="System.IAsyncResult" /> return value to the <see cref="System.IO.Compression.GZipStream.EndRead" /> method of the stream to determine how many bytes were read and to release operating system resources used for reading. You can do this either by using the same code that called <see cref="System.IO.Compression.GZipStream.BeginRead" /> or in a callback passed to <see cref="System.IO.Compression.GZipStream.BeginRead" />.
        /// The current position in the stream is updated when the asynchronous read or write is issued, not when the I/O operation completes.
        /// Multiple simultaneous asynchronous requests render the request completion order uncertain.
        /// Use the <see cref="System.IO.Compression.GZipStream.CanRead" /> property to determine whether the current <see cref="System.IO.Compression.GZipStream" /> object supports reading.
        /// If a stream is closed or you pass an invalid argument, exceptions are thrown immediately from <see cref="System.IO.Compression.GZipStream.BeginRead" />. Errors that occur during an asynchronous read request, such as a disk failure during the I/O request, occur on the thread pool thread and throw exceptions when calling <see cref="System.IO.Compression.GZipStream.EndRead" />.</remarks>
        /// <example>The following code example shows how to use the <see cref="System.IO.Compression.GZipStream" /> class to compress and decompress a file.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[IO.Compression.GZip1#1](~/samples/snippets/csharp/VS_Snippets_CLR/IO.Compression.GZip1/CS/gziptest.cs#1)]
        /// [!code-vb[IO.Compression.GZip1#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/IO.Compression.GZip1/VB/gziptest.vb#1)]
        /// ]]></format></example>
        /// <exception cref="System.IO.IOException">The method tried to  read asynchronously past the end of the stream, or a disk error occurred.</exception>
        /// <exception cref="System.ArgumentException">One or more of the arguments is invalid.</exception>
        /// <exception cref="System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <exception cref="System.NotSupportedException">The current <see cref="System.IO.Compression.GZipStream" /> implementation does not support the read operation.</exception>
        /// <exception cref="System.InvalidOperationException">A read operation cannot be performed because the stream is closed.</exception>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState) =>
            TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);

        /// <summary>Waits for the pending asynchronous read to complete. (Consider using the <see cref="System.IO.Stream.ReadAsync(byte[],int,int)" /> method instead.)</summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        /// <param name="async_result">The reference to the pending asynchronous request to finish.</param>
        /// <returns>The number of bytes read from the stream, between 0 (zero) and the number of bytes you requested. <see cref="System.IO.Compression.GZipStream" /> returns 0 only at the end of the stream; otherwise, it blocks until at least one byte is available.</returns>
        /// <remarks>Starting with the .NET Framework 4.5, you can perform asynchronous read operations by using the <see cref="System.IO.Stream.ReadAsync" /> method. The <see cref="System.IO.Compression.GZipStream.EndRead" /> method is still available in .NET Framework 4.5 to support legacy code; however, you can implement asynchronous I/O operations more easily by using the new async methods. For more information, see [Asynchronous File I/O](/dotnet/standard/io/asynchronous-file-i-o).
        /// Call this method to determine how many bytes were read from the stream. This method can be called once to return the amount of bytes read between calls to <see cref="System.IO.Compression.GZipStream.BeginRead" /> and <see cref="System.IO.Compression.GZipStream.EndRead" />.
        /// This method blocks until the I/O operation has completed.</remarks>
        /// <example>The following code example shows how to use the <see cref="System.IO.Compression.GZipStream" /> class to compress and decompress a file.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[IO.Compression.GZip1#1](~/samples/snippets/csharp/VS_Snippets_CLR/IO.Compression.GZip1/CS/gziptest.cs#1)]
        /// [!code-vb[IO.Compression.GZip1#1](~/samples/snippets/visualbasic/VS_Snippets_CLR/IO.Compression.GZip1/VB/gziptest.vb#1)]
        /// ]]></format></example>
        /// <exception cref="System.ArgumentNullException"><paramref name="asyncResult" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="asyncResult" /> did not originate from a <see cref="System.IO.Compression.GZipStream.BeginRead(byte[],int,int,System.AsyncCallback,object)" /> method on the current stream.</exception>
        /// <exception cref="System.InvalidOperationException">The end operation cannot be performed because the stream is closed.</exception>
        public override int EndRead(IAsyncResult asyncResult) =>
            _deflateStream.EndRead(asyncResult);

        /// <summary>Reads a number of decompressed bytes into the specified byte array.</summary>
        /// <param name="array">The array used to store decompressed bytes.</param>
        /// <param name="offset">The byte offset in <paramref name="array" /> at which the read bytes will be placed.</param>
        /// <param name="count">The maximum number of decompressed bytes to read.</param>
        /// <returns>The number of bytes that were decompressed into the byte array. If the end of the stream has been reached, zero or the number of bytes read is returned.</returns>
        /// <remarks>If data is found in an invalid format, an <see cref="System.IO.InvalidDataException" /> is thrown as one of the last operations. A cyclic redundancy check (CRC) is performed as one of the last operations of this method.</remarks>
        /// <example>The following example shows how to compress and decompress bytes by using the <see cref="System.IO.Compression.GZipStream.Read" /> and <see cref="System.IO.Compression.GZipStream.Write" /> methods.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[System.IO.Compression.GZipStream#3](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.gzipstream/cs/program3.cs#3)]
        /// [!code-vb[System.IO.Compression.GZipStream#3](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.gzipstream/vb/program3.vb#3)]
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
            CheckDeflateStream();
            return _deflateStream.Read(buffer, offset, count);
        }

        /// <summary>Reads a sequence of bytes from the current GZip stream into a byte span and advances the position within the GZip stream by the number of bytes read.</summary>
        /// <param name="buffer">A region of memory. When this method returns, the contents of this region are replaced by the bytes read from the current source.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes allocated in the buffer if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        /// <remarks>Use the <see cref="System.IO.Compression.GZipStream.CanRead" /> property to determine whether the current instance supports reading. Use the <see cref="System.IO.Compression.GZipStream.ReadAsync" /> method to read asynchronously from the current stream.
        /// This method read a maximum of `buffer.Length` bytes from the current stream and store them in <paramref name="buffer" />. The current position within the GZip stream is advanced by the number of bytes read; however, if an exception occurs, the current position within the GZip stream remains unchanged. This method will block until at least one byte of data can be read, in the event that no data is available. `Read` returns 0 only when there is no more data in the stream and no more is expected (such as a closed socket or end of file). The method is free to return fewer bytes than requested even if the end of the stream has not been reached.
        /// Use <see cref="System.IO.BinaryReader" /> for reading primitive data types.</remarks>
        public override int Read(Span<byte> buffer)
        {
            if (GetType() != typeof(GZipStream))
            {
                // GZipStream is not sealed, and a derived type may have overridden Read(byte[], int, int) prior
                // to this Read(Span<byte>) overload being introduced.  In that case, this Read(Span<byte>) overload
                // should use the behavior of Read(byte[],int,int) overload.
                return base.Read(buffer);
            }
            else
            {
                CheckDeflateStream();
                return _deflateStream.ReadCore(buffer);
            }
        }

        /// <summary>Begins an asynchronous write operation. (Consider using the <see cref="System.IO.Stream.WriteAsync(byte[],int,int)" /> method instead.)</summary>
        /// <param name="array">The buffer containing data to write to the current stream.</param>
        /// <param name="buffer">The buffer containing data to write to the current stream.</param>
        /// <param name="offset">The byte offset in <paramref name="array" /> at which to begin writing.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <param name="asyncCallback">An optional asynchronous callback to be called when the write operation is complete.</param>
        /// <param name="cback">An optional asynchronous callback to be called when the write operation is complete.</param>
        /// <param name="asyncState">A user-provided object that distinguishes this particular asynchronous write request from other requests.</param>
        /// <param name="state">A user-provided object that distinguishes this particular asynchronous write request from other requests.</param>
        /// <returns>An  object that represents the asynchronous write operation, which could still be pending.</returns>
        /// <remarks>Starting with the .NET Framework 4.5, you can perform asynchronous write operations by using the <see cref="System.IO.Stream.WriteAsync" /> method. The <see cref="System.IO.Compression.GZipStream.BeginWrite" /> method is still available in .NET Framework 4.5 to support legacy code; however, you can implement asynchronous I/O operations more easily by using the new async methods. For more information, see [Asynchronous File I/O](/dotnet/standard/io/asynchronous-file-i-o).
        /// The <see cref="System.IO.Compression.GZipStream.BeginWrite" /> method starts an asynchronous write operation to a <see cref="System.IO.Compression.GZipStream" /> stream object.
        /// You must create a callback method that implements the <see cref="System.AsyncCallback" /> delegate and pass its name to the <see cref="System.IO.Compression.GZipStream.BeginWrite" /> method.</remarks>
        /// <exception cref="System.InvalidOperationException">The underlying stream is <see langword="null" />.
        /// -or-
        /// The underlying stream is closed.</exception>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState) =>
            TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);

        /// <summary>Handles the end of an asynchronous write operation. (Consider using the <see cref="System.IO.Stream.WriteAsync(byte[],int,int)" /> method instead.)</summary>
        /// <param name="asyncResult">The object that represents the asynchronous call.</param>
        /// <param name="async_result">The object that represents the asynchronous call.</param>
        /// <remarks>Starting with the .NET Framework 4.5, you can perform asynchronous write operations by using the <see cref="System.IO.Stream.WriteAsync" /> method. The <see cref="System.IO.Compression.GZipStream.EndWrite" /> method is still available in .NET Framework 4.5 to support legacy code; however, you can implement asynchronous I/O operations more easily by using the new async methods. For more information, see [Asynchronous File I/O](/dotnet/standard/io/asynchronous-file-i-o).
        /// The <see cref="System.IO.Compression.GZipStream.EndWrite" /> method completes the asynchronous write operation started in the <see cref="System.IO.Compression.GZipStream.BeginWrite" /> method.</remarks>
        /// <exception cref="System.InvalidOperationException">The underlying stream is <see langword="null" />.
        /// -or-
        /// The underlying stream is closed.</exception>
        public override void EndWrite(IAsyncResult asyncResult) =>
            _deflateStream.EndWrite(asyncResult);

        /// <summary>Writes compressed bytes to the underlying GZip stream from the specified byte array.</summary>
        /// <param name="array">The buffer that contains the data to compress.</param>
        /// <param name="offset">The byte offset in <paramref name="array" /> from which the bytes will be read.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <remarks>The write operation might not occur immediately but is buffered until the buffer size is reached or until the <see cref="System.IO.Compression.GZipStream.Flush" /> or <see cref="System.IO.Stream.Close" /> method is called.</remarks>
        /// <example>The following example shows how to compress and decompress bytes by using the <see cref="System.IO.Compression.GZipStream.Read" /> and <see cref="System.IO.Compression.GZipStream.Write" /> methods.
        /// <format type="text/markdown"><![CDATA[
        /// [!code-csharp[System.IO.Compression.GZipStream#3](~/samples/snippets/csharp/VS_Snippets_CLR_System/system.io.compression.gzipstream/cs/program3.cs#3)]
        /// [!code-vb[System.IO.Compression.GZipStream#3](~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.io.compression.gzipstream/vb/program3.vb#3)]
        /// ]]></format></example>
        /// <exception cref="System.ObjectDisposedException">The write operation cannot be performed because the stream is closed.</exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckDeflateStream();
            _deflateStream.Write(buffer, offset, count);
        }

        /// <summary>Writes a sequence of bytes to the current GZip stream from a read-only byte span and advances the current position within this GZip stream by the number of bytes written.</summary>
        /// <param name="buffer">A region of memory. This method copies the contents of this region to the current GZip stream.</param>
        /// <remarks>Use the <see cref="System.IO.Compression.GZipStream.CanWrite" /> property to determine whether the current instance supports writing. Use the <see cref="System.IO.Compression.GZipStream.WriteAsync" /> method to write asynchronously to the current stream.
        /// If the write operation is successful, the position within the GZip stream advances by the number of bytes written. If an exception occurs, the position within the GZip stream remains unchanged.</remarks>
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (GetType() != typeof(GZipStream))
            {
                // GZipStream is not sealed, and a derived type may have overridden Write(byte[], int, int) prior
                // to this Write(ReadOnlySpan<byte>) overload being introduced.  In that case, this Write(ReadOnlySpan<byte>) overload
                // should use the behavior of Write(byte[],int,int) overload.
                base.Write(buffer);
            }
            else
            {
                CheckDeflateStream();
                _deflateStream.WriteCore(buffer);
            }
        }

        /// <summary>Reads the bytes from the current GZip stream and writes them to another stream, using a specified buffer size.</summary>
        /// <param name="destination">The stream to which the contents of the current GZip stream will be copied.</param>
        /// <param name="bufferSize">The size of the buffer. This value must be greater than zero. The default size is 81920.</param>
        /// <remarks>Copying begins at the current position in the current GZip stream, and does not reset the position of the destination stream after the copy operation is complete.</remarks>
        public override void CopyTo(Stream destination, int bufferSize)
        {
            CheckDeflateStream();
            _deflateStream.CopyTo(destination, bufferSize);
        }

        /// <summary>Releases the unmanaged resources used by the <see cref="System.IO.Compression.GZipStream" /> and optionally releases the managed resources.</summary>
        /// <param name="disposing"><see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.</param>
        /// <remarks>This method is called by the public <see cref="System.ComponentModel.Component.Dispose" /> method and the <see cref="object.Finalize" /> method. <see cref="System.ComponentModel.Component.Dispose" /> invokes the protected <see cref="System.IO.Compression.GZipStream.Dispose" /> method with the <paramref name="disposing" /> parameter set to <see langword="true" />. <see cref="object.Finalize" /> invokes <see cref="System.IO.Compression.GZipStream.Dispose" /> with <paramref name="disposing" /> set to <see langword="false" />.
        /// When the <paramref name="disposing" /> parameter is <see langword="true" />, this method releases all resources held by any managed objects that this <see cref="System.IO.Compression.DeflateStream" /> references. This method invokes the <see cref="System.ComponentModel.Component.Dispose" /> method of each referenced object.</remarks>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && _deflateStream != null)
                {
                    _deflateStream.Dispose();
                }
                _deflateStream = null!;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>Asynchronously releases the unmanaged resources used by the <see cref="System.IO.Compression.GZipStream" />.</summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        /// <remarks>The `DisposeAsync` method enables you to perform a resource-intensive dispose operation without blocking the main thread. This performance consideration is particularly important in a Windows 8.x Store app or desktop app where a time-consuming stream operation can block the UI thread and make your app appear as if it is not working. The async methods are used in conjunction with the <see langword="async" /> and <see langword="await" /> keywords in Visual Basic and C#.
        /// This method disposes the GZip stream by writing any changes to the backing store and closing the stream to release resources.
        /// Calling `DisposeAsync` allows the resources used by the <see cref="System.IO.Compression.GZipStream" /> to be reallocated for other purposes. For more information, see [Cleaning Up Unmanaged Resources](/dotnet/standard/garbage-collection/unmanaged).</remarks>
        public override ValueTask DisposeAsync()
        {
            if (GetType() != typeof(GZipStream))
            {
                return base.DisposeAsync();
            }

            DeflateStream? ds = _deflateStream;
            if (ds != null)
            {
                _deflateStream = null!;
                return ds.DisposeAsync();
            }

            return default;
        }

        /// <summary>Gets a reference to the underlying stream.</summary>
        /// <value>A stream object that represents the underlying stream.</value>
        /// <exception cref="System.ObjectDisposedException">The underlying stream is closed.</exception>
        public Stream BaseStream => _deflateStream?.BaseStream!;

        /// <summary>Asynchronously reads a sequence of bytes from the current GZip stream into a byte array, advances the position within the GZip stream by the number of bytes read, and monitors cancellation requests.</summary>
        /// <param name="array">The buffer to write the data into.</param>
        /// <param name="offset">The byte offset in <paramref name="array" /> at which to begin writing data from the GZip stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous read operation, which wraps the total number of bytes read into the <paramref name="array" />. The result value can be less than the number of bytes requested if the number of bytes currently available is less than the requested number, or it can be 0 (zero) if the end of the GZip stream has been reached.</returns>
        /// <remarks>The `ReadAsync` method enables you to perform resource-intensive I/O operations without blocking the main thread. This performance consideration is particularly important in a Windows 8.x Store app or desktop app where a time-consuming stream operation can block the UI thread and make your app appear as if it is not working. The async methods are used in conjunction with the <see langword="async" /> and <see langword="await" /> keywords in Visual Basic and C#.
        /// Use the <see cref="System.IO.Compression.GZipStream.CanRead" /> property to determine whether the current instance supports reading.
        /// If the operation is canceled before it completes, the returned task contains the <see cref="System.Threading.Tasks.TaskStatus.Canceled" /> value for the <see cref="System.Threading.Tasks.Task.Status" /> property.</remarks>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckDeflateStream();
            return _deflateStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        /// <summary>Asynchronously reads a sequence of bytes from the current GZip stream into a byte memory region, advances the position within the GZip stream by the number of bytes read, and monitors cancellation requests.</summary>
        /// <param name="buffer">The region of memory to write the data into.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous read operation, which wraps the total number of bytes read into the buffer. The result value can be less than the number of bytes allocated in the buffer if that many bytes are not currently available, or it can be 0 (zero) if the end of the GZip stream has been reached.</returns>
        /// <remarks>The `ReadAsync` method enables you to perform resource-intensive I/O operations without blocking the main thread. This performance consideration is particularly important in a Windows 8.x Store app or desktop app where a time-consuming stream operation can block the UI thread and make your app appear as if it is not working. The async methods are used in conjunction with the <see langword="async" /> and <see langword="await" /> keywords in Visual Basic and C#.
        /// Use the <see cref="System.IO.Compression.GZipStream.CanRead" /> property to determine whether the current instance supports reading.
        /// If the operation is canceled before it completes, the returned task contains the <see cref="System.Threading.Tasks.TaskStatus.Canceled" /> value for the <see cref="System.Threading.Tasks.Task.Status" /> property.</remarks>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (GetType() != typeof(GZipStream))
            {
                // GZipStream is not sealed, and a derived type may have overridden ReadAsync(byte[], int, int) prior
                // to this ReadAsync(Memory<byte>) overload being introduced.  In that case, this ReadAsync(Memory<byte>) overload
                // should use the behavior of ReadAsync(byte[],int,int) overload.
                return base.ReadAsync(buffer, cancellationToken);
            }
            else
            {
                CheckDeflateStream();
                return _deflateStream.ReadAsyncMemory(buffer, cancellationToken);
            }
        }

        /// <summary>Asynchronously writes compressed bytes to the underlying GZip stream from the specified byte array.</summary>
        /// <param name="array">The buffer that contains the data to compress.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="array" /> from which to begin copying bytes to the GZip stream.</param>
        /// <param name="count">The maximum number of bytes to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <remarks>The `WriteAsync` method enables you to perform resource-intensive I/O operations without blocking the main thread. This performance consideration is particularly important in a Windows 8.x Store app or desktop app where a time-consuming stream operation can block the UI thread and make your app appear as if it is not working. The async methods are used in conjunction with the <see langword="async" /> and <see langword="await" /> keywords in Visual Basic and C#.
        /// Use the <see cref="System.IO.Compression.GZipStream.CanWrite" /> property to determine whether the current instance supports writing.
        /// If the operation is canceled before it completes, the returned task contains the <see cref="System.Threading.Tasks.TaskStatus.Canceled" /> value for the <see cref="System.Threading.Tasks.Task.Status" /> property.</remarks>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckDeflateStream();
            return _deflateStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        /// <summary>Asynchronously writes compressed bytes to the underlying GZip stream from the specified read-only byte memory region.</summary>
        /// <param name="buffer">The region of memory to write data from.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <remarks>The `WriteAsync` method enables you to perform resource-intensive I/O operations without blocking the main thread. This performance consideration is particularly important in a Windows 8.x Store app or desktop app where a time-consuming stream operation can block the UI thread and make your app appear as if it is not working. The async methods are used in conjunction with the <see langword="async" /> and <see langword="await" /> keywords in Visual Basic and C#.
        /// Use the <see cref="System.IO.Compression.GZipStream.CanWrite" /> property to determine whether the current instance supports writing.
        /// If the operation is canceled before it completes, the returned task contains the <see cref="System.Threading.Tasks.TaskStatus.Canceled" /> value for the <see cref="System.Threading.Tasks.Task.Status" /> property.</remarks>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (GetType() != typeof(GZipStream))
            {
                // GZipStream is not sealed, and a derived type may have overridden WriteAsync(byte[], int, int) prior
                // to this WriteAsync(ReadOnlyMemory<byte>) overload being introduced.  In that case, this
                // WriteAsync(ReadOnlyMemory<byte>) overload should use the behavior of Write(byte[],int,int) overload.
                return base.WriteAsync(buffer, cancellationToken);
            }
            else
            {
                CheckDeflateStream();
                return _deflateStream.WriteAsyncMemory(buffer, cancellationToken);
            }
        }

        /// <summary>Asynchronously clears all buffers for this GZip stream, causes any buffered data to be written to the underlying device, and monitors cancellation requests.</summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous flush operation.</returns>
        /// <remarks>If the operation is canceled before it completes, the returned task contains the <see cref="System.Threading.Tasks.TaskStatus.Canceled" /> value for the <see cref="System.Threading.Tasks.Task.Status" /> property.</remarks>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            CheckDeflateStream();
            return _deflateStream.FlushAsync(cancellationToken);
        }

        /// <summary>Asynchronously reads the bytes from the current GZip stream and writes them to another stream, using a specified buffer size.</summary>
        /// <param name="destination">The stream to which the contents of the current GZip stream will be copied.</param>
        /// <param name="bufferSize">The size, in bytes, of the buffer. This value must be greater than zero. The default size is 81920.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        /// <remarks>The <see cref="System.IO.Compression.GZipStream.CopyToAsync" /> method enables you to perform resource-intensive I/O operations without blocking the main thread. This performance consideration is particularly important in a Windows 8.x Store app or desktop app where a time-consuming stream operation can block the UI thread and make your app appear as if it is not working. The async methods are used in conjunction with the <see langword="async" /> and <see langword="await" /> keywords in Visual Basic and C#.
        /// If the operation is canceled before it completes, the returned task contains the <see cref="System.Threading.Tasks.TaskStatus.Canceled" /> value for the <see cref="System.Threading.Tasks.Task.Status" /> property.
        /// Copying begins at the current position in the current GZip stream.</remarks>
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            CheckDeflateStream();
            return _deflateStream.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        private void CheckDeflateStream()
        {
            if (_deflateStream == null)
            {
                ThrowStreamClosedException();
            }
        }

        private static void ThrowStreamClosedException()
        {
            throw new ObjectDisposedException(nameof(GZipStream), SR.ObjectDisposed_StreamClosed);
        }
    }
}
