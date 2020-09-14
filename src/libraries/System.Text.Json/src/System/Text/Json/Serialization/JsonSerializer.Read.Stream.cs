// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json.Serialization;
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
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="utf8Json"/>is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the JSON is invalid,
        /// <typeparamref name="TValue"/> is not compatible with the JSON,
        /// or when there is remaining data in the Stream.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        public static ValueTask<TValue?> DeserializeAsync<[DynamicallyAccessedMembers(MembersAccessedOnRead)] TValue>(
            Stream utf8Json,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json == null)
            {
                throw new ArgumentNullException(nameof(utf8Json));
            }

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
        /// <paramref name="utf8Json"/> or <paramref name="returnType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the JSON is invalid,
        /// the <paramref name="returnType"/> is not compatible with the JSON,
        /// or when there is remaining data in the Stream.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="returnType"/> or its serializable members.
        /// </exception>
        public static ValueTask<object?> DeserializeAsync(
            Stream utf8Json,
            [DynamicallyAccessedMembers(MembersAccessedOnRead)] Type returnType,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json == null)
                throw new ArgumentNullException(nameof(utf8Json));

            if (returnType == null)
                throw new ArgumentNullException(nameof(returnType));

            return ReadAsync<object?>(utf8Json, returnType, options, cancellationToken);
        }

        private static async ValueTask<TValue?> ReadAsync<TValue>(
            Stream utf8Json,
            Type returnType,
            JsonSerializerOptions? options,
            CancellationToken cancellationToken)
        {
            if (options == null)
            {
                options = JsonSerializerOptions.s_defaultOptions;
            }

            ReadStack state = default;
            state.Initialize(returnType, options, supportContinuation: true);

            JsonConverter converter = state.Current.JsonPropertyInfo!.ConverterBase;

            var readerState = new JsonReaderState(options.GetReaderOptions());

            // todo: https://github.com/dotnet/runtime/issues/32355
            int utf8BomLength = JsonConstants.Utf8Bom.Length;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(Math.Max(options.DefaultBufferSize, utf8BomLength));
            int bytesInBuffer = 0;
            long totalBytesRead = 0;
            int clearMax = 0;
            bool isFirstIteration = true;

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

                    int start = 0;
                    if (isFirstIteration)
                    {
                        isFirstIteration = false;

                        // Handle the UTF-8 BOM if present
                        Debug.Assert(buffer.Length >= JsonConstants.Utf8Bom.Length);
                        if (buffer.AsSpan().StartsWith(JsonConstants.Utf8Bom))
                        {
                            start += utf8BomLength;
                            bytesInBuffer -= utf8BomLength;
                        }
                    }

                    // Process the data available
                    TValue value = ReadCore<TValue>(
                        ref readerState,
                        isFinalBlock,
                        new ReadOnlySpan<byte>(buffer, start, bytesInBuffer),
                        options,
                        ref state,
                        converter);

                    Debug.Assert(state.BytesConsumed <= bytesInBuffer);
                    int bytesConsumed = checked((int)state.BytesConsumed);

                    bytesInBuffer -= bytesConsumed;

                    if (isFinalBlock)
                    {
                        // The reader should have thrown if we have remaining bytes.
                        Debug.Assert(bytesInBuffer == 0);

                        return value;
                    }

                    // Check if we need to shift or expand the buffer because there wasn't enough data to complete deserialization.
                    if ((uint)bytesInBuffer > ((uint)buffer.Length / 2))
                    {
                        // We have less than half the buffer available, double the buffer size.
                        byte[] dest = ArrayPool<byte>.Shared.Rent((buffer.Length < (int.MaxValue / 2)) ? buffer.Length * 2 : int.MaxValue);

                        // Copy the unprocessed data to the new buffer while shifting the processed bytes.
                        Buffer.BlockCopy(buffer, bytesConsumed + start, dest, 0, bytesInBuffer);

                        new Span<byte>(buffer, 0, clearMax).Clear();
                        ArrayPool<byte>.Shared.Return(buffer);

                        clearMax = bytesInBuffer;
                        buffer = dest;
                    }
                    else if (bytesInBuffer != 0)
                    {
                        // Shift the processed bytes to the beginning of buffer to make more room.
                        Buffer.BlockCopy(buffer, bytesConsumed + start, buffer, 0, bytesInBuffer);
                    }
                }
            }
            finally
            {
                // Clear only what we used and return the buffer to the pool
                new Span<byte>(buffer, 0, clearMax).Clear();
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static TValue ReadCore<TValue>(
            ref JsonReaderState readerState,
            bool isFinalBlock,
            ReadOnlySpan<byte> buffer,
            JsonSerializerOptions options,
            ref ReadStack state,
            JsonConverter converterBase)
        {
            var reader = new Utf8JsonReader(buffer, isFinalBlock, readerState);

            // If we haven't read in the entire stream's payload we'll need to signify that we want
            // to enable read ahead behaviors to ensure we have complete json objects and arrays
            // ({}, []) when needed. (Notably to successfully parse JsonElement via JsonDocument
            // to assign to object and JsonElement properties in the constructed .NET object.)
            state.ReadAhead = !isFinalBlock;
            state.BytesConsumed = 0;

            TValue value = ReadCore<TValue>(converterBase, ref reader, options, ref state);

            readerState = reader.CurrentState;
            return value!;
        }
    }
}
