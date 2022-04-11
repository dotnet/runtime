// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        // We flush the Stream when the buffer is >=90% of capacity.
        // This threshold is a compromise between buffer utilization and minimizing cases where the buffer
        // needs to be expanded\doubled because it is not large enough to write the current property or element.
        // We check for flush after each JSON property and element is written to the buffer.
        // Once the buffer is expanded to contain the largest single element\property, a 90% thresold
        // means the buffer may be expanded a maximum of 4 times: 1-(1/(2^4))==.9375.
        private const float FlushThreshold = .90f;

        /// <summary>
        /// Converts the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">Options to control the conversion behavior.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> that can be used to cancel the write operation.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        public static Task SerializeAsync<TValue>(
            Stream utf8Json!!,
            TValue value,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Type runtimeType = GetRuntimeType(value);
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(options, runtimeType);
            return WriteStreamAsync(utf8Json, value!, jsonTypeInfo, cancellationToken);
        }

        /// <summary>
        /// Converts the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">Options to control the conversion behavior.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        public static void Serialize<TValue>(
            Stream utf8Json!!,
            TValue value,
            JsonSerializerOptions? options = null)
        {
            Type runtimeType = GetRuntimeType(value);
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(options, runtimeType);
            WriteStream(utf8Json, value!, jsonTypeInfo);
        }

        /// <summary>
        /// Converts the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type of the <paramref name="value"/> to convert.</param>
        /// <param name="options">Options to control the conversion behavior.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> that can be used to cancel the write operation.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="inputType"/> is not compatible with <paramref name="value"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/> or <paramref name="inputType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="inputType"/>  or its serializable members.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        public static Task SerializeAsync(
            Stream utf8Json!!,
            object? value,
            Type inputType,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Type runtimeType = GetRuntimeTypeAndValidateInputType(value, inputType);
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(options, runtimeType);
            return WriteStreamAsync(utf8Json, value!, jsonTypeInfo, cancellationToken);
        }

        /// <summary>
        /// Converts the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type of the <paramref name="value"/> to convert.</param>
        /// <param name="options">Options to control the conversion behavior.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="inputType"/> is not compatible with <paramref name="value"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/> or <paramref name="inputType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="inputType"/>  or its serializable members.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        public static void Serialize(
            Stream utf8Json!!,
            object? value,
            Type inputType,
            JsonSerializerOptions? options = null)
        {
            Type runtimeType = GetRuntimeTypeAndValidateInputType(value, inputType);
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(options, runtimeType);
            WriteStream(utf8Json, value!, jsonTypeInfo);
        }

        /// <summary>
        /// Converts the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> that can be used to cancel the write operation.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        public static Task SerializeAsync<TValue>(
            Stream utf8Json!!,
            TValue value,
            JsonTypeInfo<TValue> jsonTypeInfo!!,
            CancellationToken cancellationToken = default)
        {
            return WriteStreamAsync(utf8Json, value, jsonTypeInfo, cancellationToken);
        }

        /// <summary>
        /// Converts the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        public static void Serialize<TValue>(
            Stream utf8Json!!,
            TValue value,
            JsonTypeInfo<TValue> jsonTypeInfo!!)
        {
            WriteStream(utf8Json, value, jsonTypeInfo);
        }

        /// <summary>
        /// Converts the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type of the <paramref name="value"/> to convert.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> that can be used to cancel the write operation.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="inputType"/> is not compatible with <paramref name="value"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/>, <paramref name="inputType"/>, or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="inputType"/>  or its serializable members.
        /// </exception>
        public static Task SerializeAsync(
            Stream utf8Json!!,
            object? value,
            Type inputType,
            JsonSerializerContext context!!,
            CancellationToken cancellationToken = default)
        {
            Type runtimeType = GetRuntimeTypeAndValidateInputType(value, inputType);
            return WriteStreamAsync(
                utf8Json,
                value!,
                GetTypeInfo(context, runtimeType),
                cancellationToken);
        }

        /// <summary>
        /// Converts the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type of the <paramref name="value"/> to convert.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="inputType"/> is not compatible with <paramref name="value"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/>, <paramref name="inputType"/>, or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="inputType"/>  or its serializable members.
        /// </exception>
        public static void Serialize(
            Stream utf8Json!!,
            object? value,
            Type inputType,
            JsonSerializerContext context!!)
        {
            Type runtimeType = GetRuntimeTypeAndValidateInputType(value, inputType);
            WriteStream(utf8Json, value!, GetTypeInfo(context, runtimeType));
        }

        private static async Task WriteStreamAsync<TValue>(
            Stream utf8Json,
            TValue value,
            JsonTypeInfo jsonTypeInfo,
            CancellationToken cancellationToken)
        {
            JsonSerializerOptions options = jsonTypeInfo.Options;
            JsonWriterOptions writerOptions = options.GetWriterOptions();

            using (var bufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize))
            using (var writer = new Utf8JsonWriter(bufferWriter, writerOptions))
            {
                WriteStack state = new WriteStack { CancellationToken = cancellationToken };
                jsonTypeInfo.EnsureConfigured();
                JsonConverter converter = state.Initialize(jsonTypeInfo, supportContinuation: true, supportAsync: true);

                bool isFinalBlock;

                try
                {
                    do
                    {
                        state.FlushThreshold = (int)(bufferWriter.Capacity * FlushThreshold);

                        try
                        {
                            isFinalBlock = WriteCore(converter, writer, value, options, ref state);

                            if (state.SuppressFlush)
                            {
                                Debug.Assert(!isFinalBlock);
                                Debug.Assert(state.PendingTask is not null);
                                state.SuppressFlush = false;
                            }
                            else
                            {
                                await bufferWriter.WriteToStreamAsync(utf8Json, cancellationToken).ConfigureAwait(false);
                                bufferWriter.Clear();
                            }
                        }
                        finally
                        {
                            // Await any pending resumable converter tasks (currently these can only be IAsyncEnumerator.MoveNextAsync() tasks).
                            // Note that pending tasks are always awaited, even if an exception has been thrown or the cancellation token has fired.
                            if (state.PendingTask is not null)
                            {
                                try
                                {
                                    await state.PendingTask.ConfigureAwait(false);
                                }
                                catch
                                {
                                    // Exceptions should only be propagated by the resuming converter
                                    // TODO https://github.com/dotnet/runtime/issues/22144
                                }
                            }

                            // Dispose any pending async disposables (currently these can only be completed IAsyncEnumerators).
                            if (state.CompletedAsyncDisposables?.Count > 0)
                            {
                                await state.DisposeCompletedAsyncDisposables().ConfigureAwait(false);
                            }
                        }

                    } while (!isFinalBlock);
                }
                catch
                {
                    // On exception, walk the WriteStack for any orphaned disposables and try to dispose them.
                    await state.DisposePendingDisposablesOnExceptionAsync().ConfigureAwait(false);
                    throw;
                }
            }
        }

        private static void WriteStream<TValue>(
            Stream utf8Json,
            in TValue value,
            JsonTypeInfo jsonTypeInfo)
        {
            JsonSerializerOptions options = jsonTypeInfo.Options;
            JsonWriterOptions writerOptions = options.GetWriterOptions();

            using (var bufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize))
            using (var writer = new Utf8JsonWriter(bufferWriter, writerOptions))
            {
                WriteStack state = default;
                jsonTypeInfo.EnsureConfigured();
                JsonConverter converter = state.Initialize(jsonTypeInfo, supportContinuation: true, supportAsync: false);

                bool isFinalBlock;

                do
                {
                    state.FlushThreshold = (int)(bufferWriter.Capacity * FlushThreshold);

                    isFinalBlock = WriteCore(converter, writer, value, options, ref state);

                    bufferWriter.WriteToStream(utf8Json);
                    bufferWriter.Clear();

                    Debug.Assert(state.PendingTask == null);
                } while (!isFinalBlock);
            }
        }
    }
}
