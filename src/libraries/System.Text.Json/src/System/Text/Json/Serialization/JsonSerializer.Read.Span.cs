// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Parses the UTF-8 encoded text representing a single JSON value into a <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The type to deserialize the JSON value into.</typeparam>
        /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <param name="utf8Json">JSON text to parse.</param>
        /// <param name="options">Options to control the behavior during parsing.</param>
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
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static TValue? Deserialize<TValue>(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions? options = null)
        {
            JsonTypeInfo<TValue> jsonTypeInfo = GetTypeInfo<TValue>(options);
            return ReadFromSpan(utf8Json, jsonTypeInfo);
        }

        /// <summary>
        /// Parses the UTF-8 encoded text representing a single JSON value into a <paramref name="returnType"/>.
        /// </summary>
        /// <returns>A <paramref name="returnType"/> representation of the JSON value.</returns>
        /// <param name="utf8Json">JSON text to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="options">Options to control the behavior during parsing.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="returnType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// The JSON is invalid,
        /// <paramref name="returnType"/> is not compatible with the JSON,
        /// or when there is remaining data in the Stream.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="returnType"/> or its serializable members.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static object? Deserialize(ReadOnlySpan<byte> utf8Json, Type returnType, JsonSerializerOptions? options = null)
        {
            if (returnType is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(returnType));
            }

            JsonTypeInfo jsonTypeInfo = GetTypeInfo(options, returnType);
            return ReadFromSpanAsObject(utf8Json, jsonTypeInfo);
        }

        /// <summary>
        /// Parses the UTF-8 encoded text representing a single JSON value into a <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The type to deserialize the JSON value into.</typeparam>
        /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <param name="utf8Json">JSON text to parse.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <exception cref="JsonException">
        /// The JSON is invalid,
        /// <typeparamref name="TValue"/> is not compatible with the JSON,
        /// or when there is remaining data in the buffer.
        /// </exception>
        public static TValue? Deserialize<TValue>(ReadOnlySpan<byte> utf8Json, JsonTypeInfo<TValue> jsonTypeInfo)
        {
            if (jsonTypeInfo is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(jsonTypeInfo));
            }

            jsonTypeInfo.EnsureConfigured();
            return ReadFromSpan(utf8Json, jsonTypeInfo);
        }

        /// <summary>
        /// Parses the UTF-8 encoded text representing a single JSON value into an instance specified by the <paramref name="jsonTypeInfo"/>.
        /// </summary>
        /// <returns>A <paramref name="jsonTypeInfo"/> representation of the JSON value.</returns>
        /// <param name="utf8Json">JSON text to parse.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <exception cref="JsonException">
        /// The JSON is invalid,
        /// or there is remaining data in the buffer.
        /// </exception>
        public static object? Deserialize(ReadOnlySpan<byte> utf8Json, JsonTypeInfo jsonTypeInfo)
        {
            if (jsonTypeInfo is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(jsonTypeInfo));
            }

            jsonTypeInfo.EnsureConfigured();
            return ReadFromSpanAsObject(utf8Json, jsonTypeInfo);
        }

        /// <summary>
        /// Parses the UTF-8 encoded text representing a single JSON value into a <paramref name="returnType"/>.
        /// </summary>
        /// <returns>A <paramref name="returnType"/> representation of the JSON value.</returns>
        /// <param name="utf8Json">JSON text to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="returnType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// The JSON is invalid,
        /// <paramref name="returnType"/> is not compatible with the JSON,
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
        public static object? Deserialize(ReadOnlySpan<byte> utf8Json, Type returnType, JsonSerializerContext context)
        {
            if (returnType is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(returnType));
            }
            if (context is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(context));
            }

            return ReadFromSpanAsObject(utf8Json, GetTypeInfo(context, returnType));
        }

        private static TValue? ReadFromSpan<TValue>(ReadOnlySpan<byte> utf8Json, JsonTypeInfo<TValue> jsonTypeInfo, int? actualByteCount = null)
        {
            Debug.Assert(jsonTypeInfo.IsConfigured);

            var readerState = new JsonReaderState(jsonTypeInfo.Options.GetReaderOptions());
            var reader = new Utf8JsonReader(utf8Json, isFinalBlock: true, readerState);

            ReadStack state = default;
            state.Initialize(jsonTypeInfo);

            TValue? value = jsonTypeInfo.Deserialize(ref reader, ref state);

            // The reader should have thrown if we have remaining bytes.
            Debug.Assert(reader.BytesConsumed == (actualByteCount ?? utf8Json.Length));
            return value;
        }

        private static object? ReadFromSpanAsObject(ReadOnlySpan<byte> utf8Json, JsonTypeInfo jsonTypeInfo, int? actualByteCount = null)
        {
            Debug.Assert(jsonTypeInfo.IsConfigured);

            var readerState = new JsonReaderState(jsonTypeInfo.Options.GetReaderOptions());
            var reader = new Utf8JsonReader(utf8Json, isFinalBlock: true, readerState);

            ReadStack state = default;
            state.Initialize(jsonTypeInfo);

            object? value = jsonTypeInfo.DeserializeAsObject(ref reader, ref state);

            // The reader should have thrown if we have remaining bytes.
            Debug.Assert(reader.BytesConsumed == (actualByteCount ?? utf8Json.Length));
            return value;
        }
    }
}
