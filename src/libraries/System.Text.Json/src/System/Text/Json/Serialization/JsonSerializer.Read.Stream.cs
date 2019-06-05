// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Read the UTF-8 encoded text representing a single JSON value into a <typeparamref name="TValue"/>.
        /// The Stream will be read to completion.
        /// </summary>
        /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <param name="utf8Json">JSON data to parse.</param>
        /// <param name="options">Options to control the behavior during reading.</param>
        /// <param name="cancellationToken">
        /// The <see cref="System.Threading.CancellationToken"/> which may be used to cancel the read operation.
        /// </param>
        /// <exception cref="JsonException">
        /// Thrown when the JSON is invalid,
        /// <typeparamref name="TValue"/> is not compatible with the JSON,
        /// or when there is remaining data in the Stream.
        /// </exception>
        public static ValueTask<TValue> ReadAsync<TValue>(
            Stream utf8Json,
            JsonSerializerOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json == null)
                throw new ArgumentNullException(nameof(utf8Json));

            return ReadAsync<TValue>(utf8Json, typeof(TValue), options, cancellationToken);
        }

        /// <summary>
        /// Read the UTF-8 encoded text representing a single JSON value into a <paramref name="returnType"/>.
        /// The Stream will be read to completion.
        /// </summary>
        /// <returns>A <paramref name="returnType"/> representation of the JSON value.</returns>
        /// <param name="utf8Json">JSON data to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="options">Options to control the behavior during reading.</param>
        /// <param name="cancellationToken">
        /// The <see cref="System.Threading.CancellationToken"/> which may be used to cancel the read operation.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="utf8Json"/> or <paramref name="returnType"/> is null.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the JSON is invalid,
        /// the <paramref name="returnType"/> is not compatible with the JSON,
        /// or when there is remaining data in the Stream.
        /// </exception>
        public static ValueTask<object> ReadAsync(
            Stream utf8Json,
            Type returnType,
            JsonSerializerOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json == null)
                throw new ArgumentNullException(nameof(utf8Json));

            if (returnType == null)
                throw new ArgumentNullException(nameof(returnType));

            return ReadAsync<object>(utf8Json, returnType, options, cancellationToken);
        }

        private static async ValueTask<TValue> ReadAsync<TValue>(
            Stream utf8Json,
            Type returnType,
            JsonSerializerOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (options == null)
            {
                options = JsonSerializerOptions.s_defaultOptions;
            }

            ReadStack readStack = default;
            readStack.Current.Initialize(returnType, options);

            var readerState = new JsonReaderState(options.GetReaderOptions());

            // todo: switch to ArrayBuffer implementation to handle and simplify the allocs?
            byte[] buffer = ArrayPool<byte>.Shared.Rent(options.DefaultBufferSize);
            int bytesInBuffer = 0;
            long totalBytesRead = 0;
            int clearMax = 0;

            try
            {
                while (true)
                {
                    // Read from the stream until either our buffer is filled or we hit EOF.
                    // Calling ReadCore is relatively expensive, so we minimize the number of times
                    // we need to call it.
                    bool isFinalBlock = false;
                    while (true)
                    {
                        int bytesRead = await utf8Json.ReadAsync(
#if BUILDING_INBOX_LIBRARY
                            buffer.AsMemory(bytesInBuffer),
#else
                            buffer, bytesInBuffer, buffer.Length - bytesInBuffer,
#endif
                            cancellationToken).ConfigureAwait(false);

                        if (bytesRead == 0)
                        {
                            isFinalBlock = true;
                            break;
                        }

                        totalBytesRead += bytesRead;
                        bytesInBuffer += bytesRead;

                        if (bytesInBuffer == buffer.Length)
                        {
                            break;
                        }
                    }

                    if (bytesInBuffer > clearMax)
                    {
                        clearMax = bytesInBuffer;
                    }

                    // Process the data available
                    ReadCore(
                        ref readerState,
                        isFinalBlock,
                        new Span<byte>(buffer, 0, bytesInBuffer),
                        options,
                        ref readStack);

                    Debug.Assert(readStack.BytesConsumed <= bytesInBuffer);
                    int bytesConsumed = checked((int)readStack.BytesConsumed);

                    bytesInBuffer -= bytesConsumed;

                    if (isFinalBlock)
                    {
                        break;
                    }

                    // Check if we need to shift or expand the buffer because there wasn't enough data to complete deserialization.
                    if ((uint)bytesInBuffer > ((uint)buffer.Length / 2))
                    {
                        // We have less than half the buffer available, double the buffer size.
                        byte[] dest = ArrayPool<byte>.Shared.Rent((buffer.Length < (int.MaxValue / 2)) ? buffer.Length * 2 : int.MaxValue);
                        
                        // Copy the unprocessed data to the new buffer while shifting the processed bytes.
                        Buffer.BlockCopy(buffer, bytesConsumed, dest, 0, bytesInBuffer);

                        new Span<byte>(buffer, 0, clearMax).Clear();
                        ArrayPool<byte>.Shared.Return(buffer);

                        clearMax = bytesInBuffer;
                        buffer = dest;
                    }
                    else if (bytesInBuffer != 0)
                    {
                        // Shift the processed bytes to the beginning of buffer to make more room.
                        Buffer.BlockCopy(buffer, bytesConsumed, buffer, 0, bytesInBuffer);
                    }
                }
            }
            finally
            {
                // Clear only what we used and return the buffer to the pool
                new Span<byte>(buffer, 0, clearMax).Clear();
                ArrayPool<byte>.Shared.Return(buffer);
            }

            if (bytesInBuffer != 0)
            {
                ThrowHelper.ThrowJsonException_DeserializeDataRemaining(totalBytesRead, bytesInBuffer);
            }

            return (TValue)readStack.Current.ReturnValue;
        }

        private static void ReadCore(
            ref JsonReaderState readerState,
            bool isFinalBlock,
            Span<byte> buffer,
            JsonSerializerOptions options,
            ref ReadStack readStack)
        {
            var reader = new Utf8JsonReader(buffer, isFinalBlock, readerState);

            // If we haven't read in the entire stream's payload we'll need to signify that we want
            // to enable read ahead behaviors to ensure we have complete json objects and arrays
            // ({}, []) when needed. (Notably to successfully parse JsonElement via JsonDocument
            // to assign to object and JsonElement properties in the constructed .NET object.)
            readStack.ReadAhead = !isFinalBlock;
            readStack.BytesConsumed = 0;

            ReadCore(
                options,
                ref reader,
                ref readStack);

            readerState = reader.CurrentState;
        }
    }
}
