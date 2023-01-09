// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    /// <summary>
    /// Provides a generic view of a read-only sequence of bytes
    /// </summary>
    public interface IReaadOnlyStream : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
        /// </summary>
        bool CanRead { get; }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
        /// </summary>
        bool CanSeek { get; }

        /// <summary>
        /// Gets a value that determines whether the current stream can time out.
        /// </summary>
        bool CanTimeout { get; }

        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        long Length { get; }

        /// <summary>
        /// Gets or sets the position within the current stream.
        /// </summary>
        long Position { get; set; }

        /// <summary>
        /// Gets or sets a value, in milliseconds, that determines how long the stream will attempt to read before timing out.
        /// </summary>
        int ReadTimeout { get; set; }

        /// <summary>
        /// Reads the bytes from the current stream and writes them to another stream.
        /// </summary>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        void CopyTo(Stream destination);

        /// <summary>
        /// Reads the bytes from the current stream and writes them to another stream, using a specified buffer size.
        /// </summary>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        /// <param name="bufferSize">The size of the buffer. This value must be greater than zero. The default size is 81920.</param>
        void CopyTo(Stream destination, int bufferSize);

        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to another stream.
        /// </summary>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        Task CopyToAsync(Stream destination);

        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to another stream, using a specified buffer size.
        /// </summary>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        /// <param name="bufferSize">The size, in bytes, of the buffer. This value must be greater than zero. The default size is 81920.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        Task CopyToAsync(Stream destination, int bufferSize);

        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to another stream, using a specified cancellation token.
        /// </summary>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is System.Threading.CancellationToken.None.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        Task CopyToAsync(Stream destination, CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to another stream, using a specified buffer size and cancellation token.
        /// </summary>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        /// <param name="bufferSize">The size, in bytes, of the buffer. This value must be greater than zero. The default size is 81920.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is System.Threading.CancellationToken.None.</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken);

        /// <summary>
        /// Closes the current stream and releases any resources (such as sockets and file<br/>
        /// handles) associated with the current stream. Instead of calling this method,<br/>
        /// ensure that the stream is properly disposed.
        /// </summary>
        void Close();

        IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state);

        /// <summary>
        /// Waits for the pending asynchronous read to complete. (Consider using System.IO.Stream.ReadAsync(System.Byte[],System.Int32,System.Int32)<br/>
        /// instead.)
        /// </summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        /// <returns>The number of bytes read from the stream, between zero (0) and the number of<br/>
        /// bytes you requested. Streams return zero (0) only at the end of the stream, otherwise,<br/>
        /// they should block until at least one byte is available.</returns>
        int EndRead(IAsyncResult asyncResult);

        /// <summary>
        /// Asynchronously reads a sequence of bytes from the current stream and advances<br/>
        /// the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">The buffer to write the data into.</param>
        /// <param name="offset">The byte offset in buffer at which to begin writing data from the stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <returns>A task that represents the asynchronous read operation. The value of the TResult<br/>
        /// parameter contains the total number of bytes read into the buffer. The result<br/>
        /// value can be less than the number of bytes requested if the number of bytes currently<br/>
        /// available is less than the requested number, or it can be 0 (zero) if the end<br/>
        /// of the stream has been reached.</returns>
        public Task<int> ReadAsync(byte[] buffer, int offset, int count);

        /// <summary>
        /// Asynchronously reads a sequence of bytes from the current stream, advances the<br/>
        /// position within the stream by the number of bytes read, and monitors cancellation<br/>
        /// requests.
        /// </summary>
        /// <param name="buffer">The buffer to write the data into.</param>
        /// <param name="offset">The byte offset in buffer at which to begin writing data from the stream.</param>
        /// <param name="count">The maximum number of bytes to read.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is System.Threading.CancellationToken.None.</param>
        /// <returns>A task that represents the asynchronous read operation. The value of the TResult<br/>
        /// parameter contains the total number of bytes read into the buffer. The result<br/>
        /// value can be less than the number of bytes requested if the number of bytes currently<br/>
        /// available is less than the requested number, or it can be 0 (zero) if the end<br/>
        /// of the stream has been reached.</returns>
        Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

        /// <summary>
        /// Asynchronously reads a sequence of bytes from the current stream, advances the<br/>
        /// position within the stream by the number of bytes read, and monitors cancellation<br/>
        /// requests.
        /// </summary>
        /// <param name="buffer">The region of memory to write the data into.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is System.Threading.CancellationToken.None.</param>
        /// <returns>A task that represents the asynchronous read operation. The value of its System.Threading.Tasks.ValueTask`1.Result<br/>
        /// property contains the total number of bytes read into the buffer. The result<br/>
        /// value can be less than the number of bytes allocated in the buffer if that many<br/>
        /// bytes are not currently available, or it can be 0 (zero) if the end of the stream<br/>
        /// has been reached.</returns>
        ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);

        /// <summary>
        /// When overridden in a derived class, sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the origin parameter.</param>
        /// <param name="origin">A value of type System.IO.SeekOrigin indicating the reference point used to obtain<br/>
        /// the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        long Seek(long offset, SeekOrigin origin);

        /// <summary>
        /// When overridden in a derived class, reads a sequence of bytes from the current<br/>
        /// stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified<br/>
        /// byte array with the values between offset and (offset + count - 1) replaced by<br/>
        /// the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read
        /// from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number<br/>
        /// of bytes requested if that many bytes are not currently available, or zero (0)<br/>
        /// if the end of the stream has been reached.</returns>
        pint Read(byte[] buffer, int offset, int count);

        /// <summary>
        /// When overridden in a derived class, reads a sequence of bytes from the current<br/>
        /// stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">A region of memory. When this method returns, the contents of this region are<br/>
        /// replaced by the bytes read from the current source.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number<br/>
        /// of bytes allocated in the buffer if that many bytes are not currently available,<br/>
        /// or zero (0) if the end of the stream has been reached.</returns>
        int Read(Span<byte> buffer);

        /// <summary>
        /// Reads a byte from the stream and advances the position within the stream by one<br/>
        /// byte, or returns -1 if at the end of the stream.
        /// </summary>
        /// <returns>The unsigned byte cast to an System.Int32, or -1 if at the end of the stream.</returns>
        int ReadByte();
    }
}