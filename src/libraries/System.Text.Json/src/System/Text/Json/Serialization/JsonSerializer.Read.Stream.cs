// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
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
        public static ValueTask<TValue?> DeserializeAsync<[DynamicallyAccessedMembers(JsonHelpers.MembersAccessedOnRead)] TValue>(
            Stream utf8Json,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json == null)
            {
                throw new ArgumentNullException(nameof(utf8Json));
            }

            return ReadAllUsingOptionsAsync<TValue>(utf8Json, typeof(TValue), options, cancellationToken);
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
            [DynamicallyAccessedMembers(JsonHelpers.MembersAccessedOnRead)] Type returnType,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json == null)
                throw new ArgumentNullException(nameof(utf8Json));

            if (returnType == null)
                throw new ArgumentNullException(nameof(returnType));

            return ReadAllUsingOptionsAsync<object>(utf8Json, returnType, options, cancellationToken);
        }

        private static ValueTask<TValue?> ReadAllUsingOptionsAsync<TValue>(
            Stream utf8Json,
            Type returnType,
            JsonSerializerOptions? options,
            CancellationToken cancellationToken)
        {
            options ??= JsonSerializerOptions.s_defaultOptions;
            options.RootBuiltInConvertersAndTypeInfoCreator();
            JsonTypeInfo jsonTypeInfo = options.GetOrAddClassForRootType(returnType);
            return ReadAllAsync<TValue>(utf8Json, returnType, jsonTypeInfo, cancellationToken);
        }

        /// <summary>
        /// Read the UTF-8 encoded text representing a single JSON value into a <typeparamref name="TValue"/>.
        /// The Stream will be read to completion.
        /// </summary>
        /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <param name="utf8Json">JSON data to parse.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <param name="cancellationToken">
        /// The <see cref="System.Threading.CancellationToken"/> which may be used to cancel the read operation.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="utf8Json"/> or <paramref name="jsonTypeInfo"/> is <see langword="null"/>.
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
        public static ValueTask<TValue?> DeserializeAsync<TValue>(
            Stream utf8Json,
            JsonTypeInfo<TValue> jsonTypeInfo,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json == null)
            {
                throw new ArgumentNullException(nameof(utf8Json));
            }

            if (jsonTypeInfo == null)
            {
                throw new ArgumentNullException(nameof(jsonTypeInfo));
            }

            return ReadAllAsync<TValue>(utf8Json, typeof(TValue), jsonTypeInfo, cancellationToken);
        }

        /// <summary>
        /// Read the UTF-8 encoded text representing a single JSON value into a <paramref name="returnType"/>.
        /// The Stream will be read to completion.
        /// </summary>
        /// <returns>A <paramref name="returnType"/> representation of the JSON value.</returns>
        /// <param name="utf8Json">JSON data to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <param name="cancellationToken">
        /// The <see cref="System.Threading.CancellationToken"/> which may be used to cancel the read operation.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="utf8Json"/>, <paramref name="returnType"/>, or <paramref name="context"/> is <see langword="null"/>.
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
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonSerializerContext.GetTypeInfo(Type)"/> method on the provided <paramref name="context"/>
        /// did not return a compatible <see cref="JsonTypeInfo"/> for <paramref name="returnType"/>.
        /// </exception>
        public static ValueTask<object?> DeserializeAsync(
            Stream utf8Json,
            Type returnType,
            JsonSerializerContext context,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json == null)
                throw new ArgumentNullException(nameof(utf8Json));

            if (returnType == null)
                throw new ArgumentNullException(nameof(returnType));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return ReadAllAsync<object>(utf8Json, returnType, JsonHelpers.GetTypeInfo(context, returnType), cancellationToken);
        }

        /// <summary>
        /// Wraps the UTF-8 encoded text into an <see cref="IAsyncEnumerable{TValue}" />
        /// that can be used to deserialize root-level JSON arrays in a streaming manner.
        /// </summary>
        /// <returns>An <see cref="IAsyncEnumerable{TValue}" /> representation of the provided JSON array.</returns>
        /// <param name="utf8Json">JSON data to parse.</param>
        /// <param name="options">Options to control the behavior during reading.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> which may be used to cancel the read operation.</param>
        /// <returns>An <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="utf8Json"/> is <see langword="null"/>.
        /// </exception>
        public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<[DynamicallyAccessedMembers(JsonHelpers.MembersAccessedOnRead)] TValue>(
            Stream utf8Json,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json == null)
            {
                throw new ArgumentNullException(nameof(utf8Json));
            }

            options ??= JsonSerializerOptions.s_defaultOptions;
            options.RootBuiltInConvertersAndTypeInfoCreator();

            return CreateAsyncEnumerableDeserializer(utf8Json, options, cancellationToken);

            static async IAsyncEnumerable<TValue> CreateAsyncEnumerableDeserializer(
                Stream utf8Json,
                JsonSerializerOptions options,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                var bufferState = new ReadAsyncBufferState(options.DefaultBufferSize);
                ReadStack readStack = default;
                readStack.Initialize(typeof(Queue<TValue>), options, supportContinuation: true);
                JsonConverter converter = readStack.Current.JsonPropertyInfo!.ConverterBase;
                var jsonReaderState = new JsonReaderState(options.GetReaderOptions());

                try
                {
                    do
                    {
                        bufferState = await ReadFromStreamAsync(utf8Json, bufferState, cancellationToken).ConfigureAwait(false);
                        ContinueDeserialize<Queue<TValue>>(ref bufferState, ref jsonReaderState, ref readStack, converter, options);
                        if (readStack.Current.ReturnValue is Queue<TValue> queue)
                        {
                            while (queue.Count > 0)
                            {
                                yield return queue.Dequeue();
                            }
                        }
                    }
                    while (!bufferState.IsFinalBlock);
                }
                finally
                {
                    bufferState.Dispose();
                }
            }
        }

        internal static async ValueTask<TValue?> ReadAllAsync<TValue>(
            Stream utf8Json,
            Type inputType,
            JsonTypeInfo jsonTypeInfo,
            CancellationToken cancellationToken)
        {
            JsonSerializerOptions options = jsonTypeInfo.Options;
            var asyncState = new ReadAsyncBufferState(options.DefaultBufferSize);
            ReadStack readStack = default;
            readStack.Initialize(jsonTypeInfo, supportContinuation: true);
            JsonConverter converter = readStack.Current.JsonPropertyInfo!.ConverterBase;
            var jsonReaderState = new JsonReaderState(options.GetReaderOptions());

            try
            {
                while (true)
                {
                    asyncState = await ReadFromStreamAsync(utf8Json, asyncState, cancellationToken).ConfigureAwait(false);
                    TValue value = ContinueDeserialize<TValue>(ref asyncState, ref jsonReaderState, ref readStack, converter, options);

                    if (asyncState.IsFinalBlock)
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
        internal static async ValueTask<ReadAsyncBufferState> ReadFromStreamAsync(
            Stream utf8Json,
            ReadAsyncBufferState bufferState,
            CancellationToken cancellationToken)
        {
            while (true)
            {
                int bytesRead = await utf8Json.ReadAsync(
#if BUILDING_INBOX_LIBRARY
                    bufferState.Buffer.AsMemory(bufferState.BytesInBuffer),
#else
                    bufferState.Buffer, bufferState.BytesInBuffer, bufferState.Buffer.Length - bufferState.BytesInBuffer,
#endif
                    cancellationToken).ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    bufferState.IsFinalBlock = true;
                    break;
                }

                bufferState.BytesInBuffer += bytesRead;

                if (bufferState.BytesInBuffer == bufferState.Buffer.Length)
                {
                    break;
                }
            }

            return bufferState;
        }

        internal static TValue ContinueDeserialize<TValue>(
            ref ReadAsyncBufferState bufferState,
            ref JsonReaderState jsonReaderState,
            ref ReadStack readStack,
            JsonConverter converter,
            JsonSerializerOptions options)
        {
            if (bufferState.BytesInBuffer > bufferState.ClearMax)
            {
                bufferState.ClearMax = bufferState.BytesInBuffer;
            }

            int start = 0;
            if (bufferState.IsFirstIteration)
            {
                bufferState.IsFirstIteration = false;

                // Handle the UTF-8 BOM if present
                Debug.Assert(bufferState.Buffer.Length >= JsonConstants.Utf8Bom.Length);
                if (bufferState.Buffer.AsSpan().StartsWith(JsonConstants.Utf8Bom))
                {
                    start += JsonConstants.Utf8Bom.Length;
                    bufferState.BytesInBuffer -= JsonConstants.Utf8Bom.Length;
                }
            }

            // Process the data available
            TValue value = ReadCore<TValue>(
                ref jsonReaderState,
                bufferState.IsFinalBlock,
                new ReadOnlySpan<byte>(bufferState.Buffer, start, bufferState.BytesInBuffer),
                options,
                ref readStack,
                converter);

            Debug.Assert(readStack.BytesConsumed <= bufferState.BytesInBuffer);
            int bytesConsumed = checked((int)readStack.BytesConsumed);

            bufferState.BytesInBuffer -= bytesConsumed;

            // The reader should have thrown if we have remaining bytes.
            Debug.Assert(!bufferState.IsFinalBlock || bufferState.BytesInBuffer == 0);

            if (!bufferState.IsFinalBlock)
            {
                // Check if we need to shift or expand the buffer because there wasn't enough data to complete deserialization.
                if ((uint)bufferState.BytesInBuffer > ((uint)bufferState.Buffer.Length / 2))
                {
                    // We have less than half the buffer available, double the buffer size.
                    byte[] oldBuffer = bufferState.Buffer;
                    int oldClearMax = bufferState.ClearMax;
                    byte[] newBuffer = ArrayPool<byte>.Shared.Rent((bufferState.Buffer.Length < (int.MaxValue / 2)) ? bufferState.Buffer.Length * 2 : int.MaxValue);

                    // Copy the unprocessed data to the new buffer while shifting the processed bytes.
                    Buffer.BlockCopy(oldBuffer, bytesConsumed + start, newBuffer, 0, bufferState.BytesInBuffer);
                    bufferState.Buffer = newBuffer;
                    bufferState.ClearMax = bufferState.BytesInBuffer;

                    // Clear and return the old buffer
                    new Span<byte>(oldBuffer, 0, oldClearMax).Clear();
                    ArrayPool<byte>.Shared.Return(oldBuffer);
                }
                else if (bufferState.BytesInBuffer != 0)
                {
                    // Shift the processed bytes to the beginning of buffer to make more room.
                    Buffer.BlockCopy(bufferState.Buffer, bytesConsumed + start, bufferState.Buffer, 0, bufferState.BytesInBuffer);
                }
            }

            return value;
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

            TValue? value = ReadCore<TValue>(converterBase, ref reader, options, ref state);
            readerState = reader.CurrentState;
            return value!;
        }
    }
}
