// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
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

            if (utf8Json == null)
                throw new ArgumentNullException(nameof(utf8Json));

            ReadAsyncState asyncState = new ReadAsyncState(typeof(TValue), cancellationToken, options);

            return ReadAllAsync<TValue>(utf8Json, asyncState);
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

            ReadAsyncState asyncState = new ReadAsyncState(returnType, cancellationToken, options);

            return ReadAllAsync<object?>(utf8Json, asyncState);
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="utf8Json"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static IAsyncEnumerable<TValue> DeserializeAsyncEnumerable<[DynamicallyAccessedMembers(MembersAccessedOnRead)] TValue>(
            Stream utf8Json,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json == null)
            {
                throw new ArgumentNullException(nameof(utf8Json));
            }

            return new SerializerReadAsyncEnumerable<TValue>(utf8Json, options);
        }

        internal static async ValueTask<TValue?> ReadAllAsync<TValue>(Stream utf8Json, ReadAsyncState asyncState)
        {
            try
            {
                while (true)
                {
                    bool isFinalBlock = await ReadFromStream(utf8Json, asyncState).ConfigureAwait(false);

                    TValue value = ContinueDeserialize<TValue>(asyncState, isFinalBlock);
                    if (isFinalBlock)
                    {
                        return value!;
                    }
                }
            }
            finally
            {
                asyncState.Dispose();
            }
        }

        /// <summary>
        /// Read from the stream until either our buffer is filled or we hit EOF.
        /// Calling ReadCore is relatively expensive, so we minimize the number of times
        /// we need to call it.
        /// </summary>
        internal static async ValueTask<bool> ReadFromStream(Stream utf8Json, ReadAsyncState asyncState)
        {
            bool isFinalBlock = false;
            while (!isFinalBlock)
            {
                int bytesRead = await utf8Json.ReadAsync(
#if BUILDING_INBOX_LIBRARY
                    asyncState.Buffer.AsMemory(asyncState.BytesInBuffer),
#else
                    asyncState.Buffer, asyncState.BytesInBuffer, asyncState.Buffer.Length - asyncState.BytesInBuffer,
#endif
                    asyncState.CancellationToken).ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    isFinalBlock = true;
                    break;
                }

                asyncState.TotalBytesRead += bytesRead;
                asyncState.BytesInBuffer += bytesRead;

                if (asyncState.BytesInBuffer == asyncState.Buffer.Length)
                {
                    break;
                }
            }

            return isFinalBlock;
        }

        internal static TValue ContinueDeserialize<TValue>(ReadAsyncState asyncState, bool isFinalBlock)
        {
            if (asyncState.BytesInBuffer > asyncState.ClearMax)
            {
                asyncState.ClearMax = asyncState.BytesInBuffer;
            }

            int start = 0;
            if (asyncState.IsFirstIteration)
            {
                asyncState.IsFirstIteration = false;

                // Handle the UTF-8 BOM if present
                Debug.Assert(asyncState.Buffer.Length >= JsonConstants.Utf8Bom.Length);
                if (asyncState.Buffer.AsSpan().StartsWith(JsonConstants.Utf8Bom))
                {
                    start += JsonConstants.Utf8Bom.Length;
                    asyncState.BytesInBuffer -= JsonConstants.Utf8Bom.Length;
                }
            }

            // Process the data available
            TValue value = ReadCore<TValue>(
                ref asyncState.ReaderState,
                isFinalBlock,
                new ReadOnlySpan<byte>(asyncState.Buffer, start, asyncState.BytesInBuffer),
                asyncState.Options,
                ref asyncState.ReadStack,
                asyncState.Converter);

            Debug.Assert(asyncState.ReadStack.BytesConsumed <= asyncState.BytesInBuffer);
            int bytesConsumed = checked((int)asyncState.ReadStack.BytesConsumed);

            asyncState.BytesInBuffer -= bytesConsumed;

            if (isFinalBlock)
            {
                // The reader should have thrown if we have remaining bytes.
                Debug.Assert(asyncState.BytesInBuffer == 0);
                return value;
            }

            // Check if we need to shift or expand the buffer because there wasn't enough data to complete deserialization.
            if ((uint)asyncState.BytesInBuffer > ((uint)asyncState.Buffer.Length / 2))
            {
                // We have less than half the buffer available, double the buffer size.
                byte[] dest = ArrayPool<byte>.Shared.Rent((asyncState.Buffer.Length < (int.MaxValue / 2)) ? asyncState.Buffer.Length * 2 : int.MaxValue);

                // Copy the unprocessed data to the new buffer while shifting the processed bytes.
                Buffer.BlockCopy(asyncState.Buffer, bytesConsumed + start, dest, 0, asyncState.BytesInBuffer);

                new Span<byte>(asyncState.Buffer, 0, asyncState.ClearMax).Clear();
                ArrayPool<byte>.Shared.Return(asyncState.Buffer);

                asyncState.ClearMax = asyncState.BytesInBuffer;
                asyncState.Buffer = dest;
            }
            else if (asyncState.BytesInBuffer != 0)
            {
                // Shift the processed bytes to the beginning of buffer to make more room.
                Buffer.BlockCopy(asyncState.Buffer, bytesConsumed + start, asyncState.Buffer, 0, asyncState.BytesInBuffer);
            }

            return value!; // Return the partial value.
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

            TValue? value;
            if (isFinalBlock)
            {
                value = ReadCore<TValue>(converterBase, ref reader, options, ref state);
            }
            else
            {
                ReadCore<TValue>(converterBase, ref reader, options, ref state);

                // Obtain the partial value.
                value = (TValue)state.Current.ReturnValue!;
            }

            readerState = reader.CurrentState;
            return value!;
        }
    }
}
