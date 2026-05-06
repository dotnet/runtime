// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
        // Once the buffer is expanded to contain the largest single element\property, a 90% threshold
        // means the buffer may be expanded a maximum of 4 times: 1-(1/(2^4))==.9375.
        internal const float FlushThreshold = .90f;

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
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static Task SerializeAsync<TValue>(
            Stream utf8Json,
            TValue value,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(utf8Json);

            JsonTypeInfo<TValue> jsonTypeInfo = GetTypeInfo<TValue>(options);
            return jsonTypeInfo.SerializeAsync(utf8Json, value, cancellationToken);
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
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static void Serialize<TValue>(
            Stream utf8Json,
            TValue value,
            JsonSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(utf8Json);

            JsonTypeInfo<TValue> jsonTypeInfo = GetTypeInfo<TValue>(options);
            jsonTypeInfo.Serialize(utf8Json, value);
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
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static Task SerializeAsync(
            Stream utf8Json,
            object? value,
            Type inputType,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(utf8Json);

            ValidateInputType(value, inputType);
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(options, inputType);
            return jsonTypeInfo.SerializeAsObjectAsync(utf8Json, value, cancellationToken);
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
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static void Serialize(
            Stream utf8Json,
            object? value,
            Type inputType,
            JsonSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(utf8Json);

            ValidateInputType(value, inputType);
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(options, inputType);
            jsonTypeInfo.SerializeAsObject(utf8Json, value);
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
        public static Task SerializeAsync<TValue>(
            Stream utf8Json,
            TValue value,
            JsonTypeInfo<TValue> jsonTypeInfo,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(utf8Json);
            ArgumentNullException.ThrowIfNull(jsonTypeInfo);

            jsonTypeInfo.EnsureConfigured();
            return jsonTypeInfo.SerializeAsync(utf8Json, value, cancellationToken);
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
        public static void Serialize<TValue>(
            Stream utf8Json,
            TValue value,
            JsonTypeInfo<TValue> jsonTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(utf8Json);
            ArgumentNullException.ThrowIfNull(jsonTypeInfo);

            jsonTypeInfo.EnsureConfigured();
            jsonTypeInfo.Serialize(utf8Json, value);
        }

        /// <summary>
        /// Converts the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> that can be used to cancel the write operation.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidCastException">
        /// <paramref name="value"/> does not match the type of <paramref name="jsonTypeInfo"/>.
        /// </exception>
        public static Task SerializeAsync(
            Stream utf8Json,
            object? value,
            JsonTypeInfo jsonTypeInfo,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(utf8Json);
            ArgumentNullException.ThrowIfNull(jsonTypeInfo);

            jsonTypeInfo.EnsureConfigured();
            return jsonTypeInfo.SerializeAsObjectAsync(utf8Json, value, cancellationToken);
        }

        /// <summary>
        /// Converts the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidCastException">
        /// <paramref name="value"/> does not match the type of <paramref name="jsonTypeInfo"/>.
        /// </exception>
        public static void Serialize(
            Stream utf8Json,
            object? value,
            JsonTypeInfo jsonTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(utf8Json);
            ArgumentNullException.ThrowIfNull(jsonTypeInfo);

            jsonTypeInfo.EnsureConfigured();
            jsonTypeInfo.SerializeAsObject(utf8Json, value);
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
            Stream utf8Json,
            object? value,
            Type inputType,
            JsonSerializerContext context,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(utf8Json);
            ArgumentNullException.ThrowIfNull(context);

            ValidateInputType(value, inputType);
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(context, inputType);
            return jsonTypeInfo.SerializeAsObjectAsync(utf8Json, value, cancellationToken);
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
            Stream utf8Json,
            object? value,
            Type inputType,
            JsonSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(utf8Json);
            ArgumentNullException.ThrowIfNull(context);

            ValidateInputType(value, inputType);
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(context, inputType);
            jsonTypeInfo.SerializeAsObject(utf8Json, value);
        }

        /// <summary>
        /// Serializes each element of an <see cref="IAsyncEnumerable{TValue}"/> to the <see cref="System.IO.Stream"/>
        /// in a streaming manner.
        /// </summary>
        /// <typeparam name="TValue">The type of elements in the sequence to serialize.</typeparam>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The <see cref="IAsyncEnumerable{T}"/> sequence to serialize.</param>
        /// <param name="topLevelValues"><see langword="true"/> to serialize the elements as a sequence of newline-separated top-level JSON values
        /// (a <see href="https://jsonlines.org/">JSON Lines (JSONL)</see> document); <see langword="false"/> to serialize them as a single root-level JSON array.</param>
        /// <param name="options">Options to control the serialization behavior.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> that can be used to cancel the write operation.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/> or <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// When <paramref name="topLevelValues"/> is <see langword="true"/>, the output conforms to the
        /// <see href="https://jsonlines.org/">JSON Lines (JSONL)</see> specification: each element is serialized as a
        /// single-line JSON value followed by a line-feed (<c>\n</c>) terminator. The line terminator is always <c>\n</c>
        /// regardless of <see cref="JsonSerializerOptions.NewLine"/>, and <see cref="JsonSerializerOptions.WriteIndented"/>
        /// is ignored so that each value is emitted on a single line.
        /// </remarks>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static Task SerializeAsyncEnumerable<TValue>(
            Stream utf8Json,
            IAsyncEnumerable<TValue> value,
            bool topLevelValues = false,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(utf8Json);
            ArgumentNullException.ThrowIfNull(value);

            JsonTypeInfo<TValue> jsonTypeInfo = GetTypeInfo<TValue>(options);
            if (topLevelValues)
            {
                return SerializeAsyncEnumerableAsJsonLines(utf8Json, value, jsonTypeInfo, cancellationToken);
            }

            JsonTypeInfo<IAsyncEnumerable<TValue>> collectionTypeInfo = GetOrAddIAsyncEnumerableTypeInfoForSerialize(jsonTypeInfo);
            return collectionTypeInfo.SerializeAsync(utf8Json, value, cancellationToken);
        }

        /// <summary>
        /// Serializes each element of an <see cref="IAsyncEnumerable{TValue}"/> to the <see cref="System.IO.Stream"/>
        /// in a streaming manner.
        /// </summary>
        /// <typeparam name="TValue">The type of elements in the sequence to serialize.</typeparam>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The <see cref="IAsyncEnumerable{T}"/> sequence to serialize.</param>
        /// <param name="jsonTypeInfo">Metadata about the type of elements to serialize.</param>
        /// <param name="topLevelValues"><see langword="true"/> to serialize the elements as a sequence of newline-separated top-level JSON values
        /// (a <see href="https://jsonlines.org/">JSON Lines (JSONL)</see> document); <see langword="false"/> to serialize them as a single root-level JSON array.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> that can be used to cancel the write operation.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/>, <paramref name="value"/>, or <paramref name="jsonTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// When <paramref name="topLevelValues"/> is <see langword="true"/>, the output conforms to the
        /// <see href="https://jsonlines.org/">JSON Lines (JSONL)</see> specification: each element is serialized as a
        /// single-line JSON value followed by a line-feed (<c>\n</c>) terminator. The line terminator is always <c>\n</c>
        /// regardless of <see cref="JsonSerializerOptions.NewLine"/>, and <see cref="JsonSerializerOptions.WriteIndented"/>
        /// is ignored so that each value is emitted on a single line.
        /// </remarks>
        public static Task SerializeAsyncEnumerable<TValue>(
            Stream utf8Json,
            IAsyncEnumerable<TValue> value,
            JsonTypeInfo<TValue> jsonTypeInfo,
            bool topLevelValues = false,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(utf8Json);
            ArgumentNullException.ThrowIfNull(value);
            ArgumentNullException.ThrowIfNull(jsonTypeInfo);

            jsonTypeInfo.EnsureConfigured();
            if (topLevelValues)
            {
                return SerializeAsyncEnumerableAsJsonLines(utf8Json, value, jsonTypeInfo, cancellationToken);
            }

            JsonTypeInfo<IAsyncEnumerable<TValue>> collectionTypeInfo = GetOrAddIAsyncEnumerableTypeInfoForSerialize(jsonTypeInfo);
            return collectionTypeInfo.SerializeAsync(utf8Json, value, cancellationToken);
        }

        private static async Task SerializeAsyncEnumerableAsJsonLines<TValue>(
            Stream utf8Json,
            IAsyncEnumerable<TValue> value,
            JsonTypeInfo<TValue> jsonTypeInfo,
            CancellationToken cancellationToken)
        {
            Debug.Assert(jsonTypeInfo.IsConfigured);

            JsonWriterOptions writerOptions = jsonTypeInfo.Options.GetWriterOptionsForJsonLines();

            var bufferWriter = new PooledByteBufferWriter(jsonTypeInfo.Options.DefaultBufferSize, utf8Json);
            var writer = new Utf8JsonWriter(bufferWriter, writerOptions);

            try
            {
                bool first = true;
                await foreach (TValue item in value.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    if (!first)
                    {
                        writer.Reset();
                    }

                    first = false;
                    jsonTypeInfo.Serialize(writer, item);

                    // The JSON Lines spec mandates a single line-feed character as the line separator,
                    // independently of any platform-specific or user-configured newline preference.
                    Span<byte> dest = bufferWriter.GetSpan(1);
                    dest[0] = (byte)'\n';
                    bufferWriter.Advance(1);
                    await bufferWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch
            {
                // Reset the writer in exception cases so writer.Dispose() doesn't flush a partially-written value.
                writer.Reset();
                throw;
            }
            finally
            {
                writer.Dispose();
                bufferWriter.Dispose();
            }
        }
    }
}
