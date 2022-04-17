// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Reads the UTF-8 encoded text representing a single JSON value into a <typeparamref name="TValue"/>.
        /// The Stream will be read to completion.
        /// </summary>
        /// <typeparam name="TValue">The type to deserialize the JSON value into.</typeparam>
        /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <param name="utf8Json">JSON data to parse.</param>
        /// <param name="options">Options to control the behavior during reading.</param>
        /// <param name="cancellationToken">
        /// The <see cref="System.Threading.CancellationToken"/> that can be used to cancel the read operation.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="utf8Json"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// The JSON is invalid,
        /// <typeparamref name="TValue"/> is not compatible with the JSON,
        /// or when there is remaining data in the Stream.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        public static ValueTask<TValue?> DeserializeAsync<TValue>(
            Stream utf8Json!!,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(options, typeof(TValue));
            return ReadAllAsync<TValue>(utf8Json, jsonTypeInfo, cancellationToken);
        }

        /// <summary>
        /// Reads the UTF-8 encoded text representing a single JSON value into a <typeparamref name="TValue"/>.
        /// The Stream will be read to completion.
        /// </summary>
        /// <typeparam name="TValue">The type to deserialize the JSON value into.</typeparam>
        /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <param name="utf8Json">JSON data to parse.</param>
        /// <param name="options">Options to control the behavior during reading.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="utf8Json"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// The JSON is invalid,
        /// <typeparamref name="TValue"/> is not compatible with the JSON,
        /// or when there is remaining data in the Stream.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        public static TValue? Deserialize<TValue>(
            Stream utf8Json!!,
            JsonSerializerOptions? options = null)
        {
            return ReadAllUsingOptions<TValue>(utf8Json, typeof(TValue), options);
        }

        /// <summary>
        /// Reads the UTF-8 encoded text representing a single JSON value into a <paramref name="returnType"/>.
        /// The Stream will be read to completion.
        /// </summary>
        /// <returns>A <paramref name="returnType"/> representation of the JSON value.</returns>
        /// <param name="utf8Json">JSON data to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="options">Options to control the behavior during reading.</param>
        /// <param name="cancellationToken">
        /// The <see cref="System.Threading.CancellationToken"/> that can be used to cancel the read operation.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="utf8Json"/> or <paramref name="returnType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// The JSON is invalid,
        /// the <paramref name="returnType"/> is not compatible with the JSON,
        /// or when there is remaining data in the Stream.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="returnType"/> or its serializable members.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        public static ValueTask<object?> DeserializeAsync(
            Stream utf8Json!!,
            Type returnType!!,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(options, returnType);
            return ReadAllAsync<object?>(utf8Json, jsonTypeInfo, cancellationToken);
        }

        /// <summary>
        /// Reads the UTF-8 encoded text representing a single JSON value into a <paramref name="returnType"/>.
        /// The Stream will be read to completion.
        /// </summary>
        /// <returns>A <paramref name="returnType"/> representation of the JSON value.</returns>
        /// <param name="utf8Json">JSON data to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="options">Options to control the behavior during reading.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="utf8Json"/> or <paramref name="returnType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// The JSON is invalid,
        /// the <paramref name="returnType"/> is not compatible with the JSON,
        /// or when there is remaining data in the Stream.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="returnType"/> or its serializable members.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        public static object? Deserialize(
            Stream utf8Json!!,
            Type returnType!!,
            JsonSerializerOptions? options = null)
        {
            return ReadAllUsingOptions<object>(utf8Json, returnType, options);
        }

        /// <summary>
        /// Reads the UTF-8 encoded text representing a single JSON value into a <typeparamref name="TValue"/>.
        /// The Stream will be read to completion.
        /// </summary>
        /// <typeparam name="TValue">The type to deserialize the JSON value into.</typeparam>
        /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <param name="utf8Json">JSON data to parse.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <param name="cancellationToken">
        /// The <see cref="System.Threading.CancellationToken"/> that can be used to cancel the read operation.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="utf8Json"/> or <paramref name="jsonTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// The JSON is invalid,
        /// <typeparamref name="TValue"/> is not compatible with the JSON,
        /// or when there is remaining data in the Stream.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        public static ValueTask<TValue?> DeserializeAsync<TValue>(
            Stream utf8Json!!,
            JsonTypeInfo<TValue> jsonTypeInfo!!,
            CancellationToken cancellationToken = default)
        {
            return ReadAllAsync<TValue>(utf8Json, jsonTypeInfo, cancellationToken);
        }

        /// <summary>
        /// Reads the UTF-8 encoded text representing a single JSON value into a <typeparamref name="TValue"/>.
        /// The Stream will be read to completion.
        /// </summary>
        /// <typeparam name="TValue">The type to deserialize the JSON value into.</typeparam>
        /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <param name="utf8Json">JSON data to parse.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="utf8Json"/> or <paramref name="jsonTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// The JSON is invalid,
        /// <typeparamref name="TValue"/> is not compatible with the JSON,
        /// or when there is remaining data in the Stream.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        public static TValue? Deserialize<TValue>(
            Stream utf8Json!!,
            JsonTypeInfo<TValue> jsonTypeInfo!!)
        {
            return ReadAll<TValue>(utf8Json, jsonTypeInfo);
        }

        /// <summary>
        /// Reads the UTF-8 encoded text representing a single JSON value into a <paramref name="returnType"/>.
        /// The Stream will be read to completion.
        /// </summary>
        /// <returns>A <paramref name="returnType"/> representation of the JSON value.</returns>
        /// <param name="utf8Json">JSON data to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <param name="cancellationToken">
        /// The <see cref="System.Threading.CancellationToken"/> that can be used to cancel the read operation.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="utf8Json"/>, <paramref name="returnType"/>, or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// The JSON is invalid,
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
            Stream utf8Json!!,
            Type returnType!!,
            JsonSerializerContext context!!,
            CancellationToken cancellationToken = default)
        {
            return ReadAllAsync<object>(utf8Json, GetTypeInfo(context, returnType), cancellationToken);
        }

        /// <summary>
        /// Reads the UTF-8 encoded text representing a single JSON value into a <paramref name="returnType"/>.
        /// The Stream will be read to completion.
        /// </summary>
        /// <returns>A <paramref name="returnType"/> representation of the JSON value.</returns>
        /// <param name="utf8Json">JSON data to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="utf8Json"/>, <paramref name="returnType"/>, or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// The JSON is invalid,
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
        public static object? Deserialize(
            Stream utf8Json!!,
            Type returnType!!,
            JsonSerializerContext context!!)
        {
            return ReadAll<object>(utf8Json, GetTypeInfo(context, returnType));
        }

        /// <summary>
        /// Wraps the UTF-8 encoded text into an <see cref="IAsyncEnumerable{TValue}" />
        /// that can be used to deserialize root-level JSON arrays in a streaming manner.
        /// </summary>
        /// <typeparam name="TValue">The element type to deserialize asynchronously.</typeparam>
        /// <returns>An <see cref="IAsyncEnumerable{TValue}" /> representation of the provided JSON array.</returns>
        /// <param name="utf8Json">JSON data to parse.</param>
        /// <param name="options">Options to control the behavior during reading.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> that can be used to cancel the read operation.</param>
        /// <returns>An <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="utf8Json"/> is <see langword="null"/>.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        public static IAsyncEnumerable<TValue?> DeserializeAsyncEnumerable<TValue>(
            Stream utf8Json!!,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            options ??= JsonSerializerOptions.Default;
            if (!options.IsInitializedForReflectionSerializer)
            {
                options.InitializeForReflectionSerializer();
            }

            return CreateAsyncEnumerableDeserializer(utf8Json, options, cancellationToken);

            [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
            static async IAsyncEnumerable<TValue> CreateAsyncEnumerableDeserializer(
                Stream utf8Json,
                JsonSerializerOptions options,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                var bufferState = new ReadBufferState(options.DefaultBufferSize);
                // Hardcode the queue converter to avoid accidental use of custom converters
                JsonConverter converter = QueueOfTConverter<Queue<TValue>, TValue>.Instance;
                JsonTypeInfo jsonTypeInfo = CreateQueueJsonTypeInfo<TValue>(converter, options);
                ReadStack readStack = default;
                jsonTypeInfo.EnsureConfigured();
                readStack.Initialize(jsonTypeInfo, supportContinuation: true);
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "Workaround for https://github.com/mono/linker/issues/1416. All usages are marked as unsafe.")]
        private static JsonTypeInfo CreateQueueJsonTypeInfo<TValue>(JsonConverter queueConverter, JsonSerializerOptions queueOptions) =>
                new ReflectionJsonTypeInfo<Queue<TValue>>(queueConverter, queueOptions);

        internal static async ValueTask<TValue?> ReadAllAsync<TValue>(
            Stream utf8Json,
            JsonTypeInfo jsonTypeInfo,
            CancellationToken cancellationToken)
        {
            JsonSerializerOptions options = jsonTypeInfo.Options;
            var bufferState = new ReadBufferState(options.DefaultBufferSize);
            ReadStack readStack = default;
            jsonTypeInfo.EnsureConfigured();
            readStack.Initialize(jsonTypeInfo, supportContinuation: true);
            JsonConverter converter = readStack.Current.JsonPropertyInfo!.ConverterBase;
            var jsonReaderState = new JsonReaderState(options.GetReaderOptions());

            try
            {
                while (true)
                {
                    bufferState = await ReadFromStreamAsync(utf8Json, bufferState, cancellationToken).ConfigureAwait(false);
                    TValue value = ContinueDeserialize<TValue>(ref bufferState, ref jsonReaderState, ref readStack, converter, options);

                    if (bufferState.IsFinalBlock)
                    {
                        return value!;
                    }
                }
            }
            finally
            {
                bufferState.Dispose();
            }
        }

        internal static TValue? ReadAll<TValue>(
            Stream utf8Json,
            JsonTypeInfo jsonTypeInfo)
        {
            JsonSerializerOptions options = jsonTypeInfo.Options;
            var bufferState = new ReadBufferState(options.DefaultBufferSize);
            ReadStack readStack = default;
            jsonTypeInfo.EnsureConfigured();
            readStack.Initialize(jsonTypeInfo, supportContinuation: true);
            JsonConverter converter = readStack.Current.JsonPropertyInfo!.ConverterBase;
            var jsonReaderState = new JsonReaderState(options.GetReaderOptions());

            try
            {
                while (true)
                {
                    bufferState = ReadFromStream(utf8Json, bufferState);
                    TValue value = ContinueDeserialize<TValue>(ref bufferState, ref jsonReaderState, ref readStack, converter, options);

                    if (bufferState.IsFinalBlock)
                    {
                        return value!;
                    }
                }
            }
            finally
            {
                bufferState.Dispose();
            }
        }

        /// <summary>
        /// Read from the stream until either our buffer is filled or we hit EOF.
        /// Calling ReadCore is relatively expensive, so we minimize the number of times
        /// we need to call it.
        /// </summary>
        internal static async ValueTask<ReadBufferState> ReadFromStreamAsync(
            Stream utf8Json,
            ReadBufferState bufferState,
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

        /// <summary>
        /// Read from the stream until either our buffer is filled or we hit EOF.
        /// Calling ReadCore is relatively expensive, so we minimize the number of times
        /// we need to call it.
        /// </summary>
        internal static ReadBufferState ReadFromStream(
            Stream utf8Json,
            ReadBufferState bufferState)
        {
            while (true)
            {
                int bytesRead = utf8Json.Read(
#if BUILDING_INBOX_LIBRARY
                    bufferState.Buffer.AsSpan(bufferState.BytesInBuffer));
#else
                    bufferState.Buffer, bufferState.BytesInBuffer, bufferState.Buffer.Length - bufferState.BytesInBuffer);
#endif

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
            ref ReadBufferState bufferState,
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

        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        private static TValue? ReadAllUsingOptions<TValue>(
            Stream utf8Json,
            Type returnType,
            JsonSerializerOptions? options)
        {
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(options, returnType);
            return ReadAll<TValue>(utf8Json, jsonTypeInfo);
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
