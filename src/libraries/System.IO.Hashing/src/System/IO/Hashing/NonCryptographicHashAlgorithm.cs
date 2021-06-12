// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Hashing
{
    /// <summary>
    ///   Represents a non-cryptographic hash algorithm.
    /// </summary>
    public abstract class NonCryptographicHashAlgorithm
    {
        /// <summary>
        ///   Gets the number of bytes produced from this hash algorithm.
        /// </summary>
        /// <value>The number of bytes produced from this hash algorithm.</value>
        public int HashLengthInBytes { get; }

        /// <summary>
        ///   Called from constructors in derived classes to initialize the
        ///   <see cref="NonCryptographicHashAlgorithm"/> class.
        /// </summary>
        /// <param name="hashLengthInBytes">
        ///   The number of bytes produced from this hash algorithm.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="hashLengthInBytes"/> is less than 1.
        /// </exception>
        protected NonCryptographicHashAlgorithm(int hashLengthInBytes)
        {
            if (hashLengthInBytes < 1)
                throw new ArgumentOutOfRangeException(nameof(hashLengthInBytes));

            HashLengthInBytes = hashLengthInBytes;
        }

        /// <summary>
        ///   When overridden in a derived class,
        ///   appends the contents of <paramref name="source"/> to the data already
        ///   processed for the current hash computation.
        /// </summary>
        /// <param name="source">The data to process.</param>
        public abstract void Append(ReadOnlySpan<byte> source);

        /// <summary>
        ///   When overridden in a derived class,
        ///   resets the hash computation to the initial state.
        /// </summary>
        public abstract void Reset();

        /// <summary>
        ///   When overridden in a derived class,
        ///   writes the computed hash value to <paramref name="destination"/>
        ///   without modifying accumulated state.
        /// </summary>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        /// <remarks>
        ///   <para>
        ///     Implementations of this method must write exactly
        ///     <see cref="HashLengthInBytes"/> bytes to <paramref name="destination"/>.
        ///     Do not assume that the buffer was zero-initialized.
        ///   </para>
        ///   <para>
        ///     The <see cref="NonCryptographicHashAlgorithm"/> class validates the
        ///     size of the buffer before calling this method, and slices the span
        ///     down to be exactly <see cref="HashLengthInBytes"/> in length.
        ///   </para>
        /// </remarks>
        protected abstract void GetCurrentHashCore(Span<byte> destination);

        /// <summary>
        ///   Appends the contents of <paramref name="source"/> to the data already
        ///   processed for the current hash computation.
        /// </summary>
        /// <param name="source">The data to process.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source"/> is <see langword="null"/>.
        /// </exception>
        public void Append(byte[] source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            Append(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Appends the contents of <paramref name="stream"/> to the data already
        ///   processed for the current hash computation.
        /// </summary>
        /// <param name="stream">The data to process.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="stream"/> is <see langword="null"/>.
        /// </exception>
        /// <seealso cref="AppendAsync(Stream, CancellationToken)"/>
        public void Append(Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);

            while (true)
            {
                int read = stream.Read(buffer, 0, buffer.Length);

                if (read == 0)
                {
                    break;
                }

                Append(new ReadOnlySpan<byte>(buffer, 0, read));
            }

            ArrayPool<byte>.Shared.Return(buffer);
        }

        /// <summary>
        ///   Asychronously reads the contents of <paramref name="stream"/>
        ///   and appends them to the data already
        ///   processed for the current hash computation.
        /// </summary>
        /// <param name="stream">The data to process.</param>
        /// <param name="cancellationToken">
        ///   The token to monitor for cancellation requests.
        ///   The default value is <see cref="CancellationToken.None"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="stream"/> is <see langword="null"/>.
        /// </exception>
        public Task AppendAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            return AppendAsyncCore(stream, cancellationToken);
        }

        private async Task AppendAsyncCore(Stream stream, CancellationToken cancellationToken)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);

            while (true)
            {
#if NET5_0_OR_GREATER
                int read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
#else
                int read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
#endif

                if (read == 0)
                {
                    break;
                }

                Append(new ReadOnlySpan<byte>(buffer, 0, read));
            }

            ArrayPool<byte>.Shared.Return(buffer);
        }

        /// <summary>
        ///   Gets the current computed hash value without modifying accumulated state.
        /// </summary>
        public byte[] GetCurrentHash()
        {
            byte[] ret = new byte[HashLengthInBytes];
            GetCurrentHashCore(ret);
            return ret;
        }

        /// <summary>
        ///   Attempts to write the computed hash value to <paramref name="destination"/>
        ///   without modifying accumulated state.
        /// </summary>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        /// <param name="bytesWritten">
        ///   On success, receives the number of bytes written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="destination"/> is long enough to receive
        ///   the computed hash value; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetCurrentHash(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length < HashLengthInBytes)
            {
                bytesWritten = 0;
                return false;
            }

            GetCurrentHashCore(destination.Slice(0, HashLengthInBytes));
            bytesWritten = HashLengthInBytes;
            return true;
        }

        /// <summary>
        ///   Writes the computed hash value to <paramref name="destination"/>
        ///   without modifying accumulated state.
        /// </summary>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        /// <returns>
        ///   The number of bytes written to <paramref name="destination"/>,
        ///   which is always <see cref="HashLengthInBytes"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination"/> is shorter than <see cref="HashLengthInBytes"/>.
        /// </exception>
        public int GetCurrentHash(Span<byte> destination)
        {
            if (destination.Length < HashLengthInBytes)
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            GetCurrentHashCore(destination.Slice(0, HashLengthInBytes));
            return HashLengthInBytes;
        }

        /// <summary>
        ///   Gets the current computed hash value and clears the accumulated state.
        /// </summary>
        public byte[] GetHashAndReset()
        {
            byte[] ret = new byte[HashLengthInBytes];
            GetHashAndResetCore(ret);
            return ret;
        }

        /// <summary>
        ///   Attempts to write the computed hash value to <paramref name="destination"/>.
        ///   If successful, clears the accumulated state.
        /// </summary>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        /// <param name="bytesWritten">
        ///   On success, receives the number of bytes written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> and clears the accumulated state
        ///   if <paramref name="destination"/> is long enough to receive
        ///   the computed hash value; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryGetHashAndReset(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length < HashLengthInBytes)
            {
                bytesWritten = 0;
                return false;
            }

            GetHashAndResetCore(destination.Slice(0, HashLengthInBytes));
            bytesWritten = HashLengthInBytes;
            return true;
        }

        /// <summary>
        ///   Writes the computed hash value to <paramref name="destination"/>
        ///   then clears the accumulated state.
        /// </summary>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        /// <returns>
        ///   The number of bytes written to <paramref name="destination"/>,
        ///   which is always <see cref="HashLengthInBytes"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination"/> is shorter than <see cref="HashLengthInBytes"/>.
        /// </exception>
        public int GetHashAndReset(Span<byte> destination)
        {
            if (destination.Length < HashLengthInBytes)
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            GetHashAndResetCore(destination.Slice(0, HashLengthInBytes));
            return HashLengthInBytes;
        }

        /// <summary>
        ///   Writes the computed hash value to <paramref name="destination"/>
        ///   then clears the accumulated state.
        /// </summary>
        /// <param name="destination">The buffer that receives the computed hash value.</param>
        /// <remarks>
        ///   <para>
        ///     Implementations of this method must write exactly
        ///     <see cref="HashLengthInBytes"/> bytes to <paramref name="destination"/>.
        ///     Do not assume that the buffer was zero-initialized.
        ///   </para>
        ///   <para>
        ///     The <see cref="NonCryptographicHashAlgorithm"/> class validates the
        ///     size of the buffer before calling this method, and slices the span
        ///     down to be exactly <see cref="HashLengthInBytes"/> in length.
        ///   </para>
        ///   <para>
        ///     The default implementation of this method calls
        ///     <see cref="GetCurrentHashCore"/> followed by <see cref="Reset"/>.
        ///     Overrides of this method do not need to call either of those methods,
        ///     but must ensure that the caller cannot observe a difference in behavior.
        ///   </para>
        /// </remarks>
        protected virtual void GetHashAndResetCore(Span<byte> destination)
        {
            Debug.Assert(destination.Length == HashLengthInBytes);

            GetCurrentHashCore(destination);
            Reset();
        }

        /// <summary>
        ///   This method is not supported and should not be called.
        ///   Call <see cref="GetCurrentHash()"/> or <see cref="GetHashAndReset()"/>
        ///   instead.
        /// </summary>
        /// <returns>This method will always throw a <see cref="NotSupportedException"/>.</returns>
        /// <exception cref="NotSupportedException">In all cases.</exception>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use GetCurrentHash() to retrieve the computed hash code.", true)]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
        public override int GetHashCode()
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
        {
            throw new NotSupportedException(SR.NotSupported_GetHashCode);
        }
    }
}
