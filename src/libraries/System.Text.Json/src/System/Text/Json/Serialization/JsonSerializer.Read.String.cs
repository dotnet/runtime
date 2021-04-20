// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    /// <summary>
    /// Provides functionality to serialize objects or value types to JSON and
    /// deserialize JSON into objects or value types.
    /// </summary>
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Parse the text representing a single JSON value into a <typeparamref name="TValue"/>.
        /// </summary>
        /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <param name="json">JSON text to parse.</param>
        /// <param name="options">Options to control the behavior during parsing.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="json"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// The JSON is invalid.
        ///
        /// -or-
        ///
        /// <typeparamref name="TValue" /> is not compatible with the JSON.
        ///
        /// -or-
        ///
        /// There is remaining data in the string beyond a single JSON value.</exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        /// <remarks>Using a <see cref="string"/> is not as efficient as using the
        /// UTF-8 methods since the implementation natively uses UTF-8.
        /// </remarks>
        public static TValue? Deserialize<[DynamicallyAccessedMembers(JsonHelpers.MembersAccessedOnRead)] TValue>(string json, JsonSerializerOptions? options = null)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            return Deserialize<TValue>(json.AsSpan(), typeof(TValue), options);
        }

        /// <summary>
        /// Parses the text representing a single JSON value into an instance of the type specified by a generic type parameter.
        /// </summary>
        /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <param name="json">The JSON text to parse.</param>
        /// <param name="options">Options to control the behavior during parsing.</param>
        /// <exception cref="JsonException">
        /// The JSON is invalid.
        ///
        /// -or-
        ///
        /// <typeparamref name="TValue" /> is not compatible with the JSON.
        ///
        /// -or-
        ///
        /// There is remaining data in the span beyond a single JSON value.</exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        /// <remarks>Using a UTF-16 span is not as efficient as using the
        /// UTF-8 methods since the implementation natively uses UTF-8.
        /// </remarks>
        public static TValue? Deserialize<[DynamicallyAccessedMembers(JsonHelpers.MembersAccessedOnRead)] TValue>(ReadOnlySpan<char> json, JsonSerializerOptions? options = null)
        {
            // default/null span is treated as empty

            return Deserialize<TValue>(json, typeof(TValue), options);
        }

        /// <summary>
        /// Parse the text representing a single JSON value into a <paramref name="returnType"/>.
        /// </summary>
        /// <returns>A <paramref name="returnType"/> representation of the JSON value.</returns>
        /// <param name="json">JSON text to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="options">Options to control the behavior during parsing.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="json"/> or <paramref name="returnType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// The JSON is invalid.
        ///
        /// -or-
        ///
        /// <paramref name="returnType"/> is not compatible with the JSON.
        ///
        /// -or-
        ///
        /// There is remaining data in the string beyond a single JSON value.</exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="returnType"/> or its serializable members.
        /// </exception>
        /// <remarks>Using a <see cref="string"/> is not as efficient as using the
        /// UTF-8 methods since the implementation natively uses UTF-8.
        /// </remarks>
        public static object? Deserialize(string json, [DynamicallyAccessedMembers(JsonHelpers.MembersAccessedOnRead)] Type returnType, JsonSerializerOptions? options = null)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            if (returnType == null)
            {
                throw new ArgumentNullException(nameof(returnType));
            }

            object? value = Deserialize<object?>(json.AsSpan(), returnType, options)!;

            return value;
        }

        /// <summary>
        /// Parse the text representing a single JSON value into an instance of a specified type.
        /// </summary>
        /// <returns>A <paramref name="returnType"/> representation of the JSON value.</returns>
        /// <param name="json">The JSON text to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="options">Options to control the behavior during parsing.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="returnType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// The JSON is invalid.
        ///
        /// -or-
        ///
        /// <paramref name="returnType"/> is not compatible with the JSON.
        ///
        /// -or-
        ///
        /// There is remaining data in the span beyond a single JSON value.</exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="returnType"/> or its serializable members.
        /// </exception>
        /// <remarks>Using a UTF-16 span is not as efficient as using the
        /// UTF-8 methods since the implementation natively uses UTF-8.
        /// </remarks>
        public static object? Deserialize(ReadOnlySpan<char> json, [DynamicallyAccessedMembers(JsonHelpers.MembersAccessedOnRead)] Type returnType, JsonSerializerOptions? options = null)
        {
            // default/null span is treated as empty

            if (returnType == null)
            {
                throw new ArgumentNullException(nameof(returnType));
            }

            object? value = Deserialize<object?>(json, returnType, options)!;

            return value;
        }

        private static TValue? Deserialize<TValue>(ReadOnlySpan<char> json, Type returnType, JsonSerializerOptions? options)
        {
            if (options == null)
            {
                options = JsonSerializerOptions.s_defaultOptions;
            }

            options.RootBuiltInConvertersAndTypeInfoCreator();

            ReadStack state = default;
            state.Initialize(returnType, options, supportContinuation: false);

            JsonConverter jsonConverter = state.Current.JsonPropertyInfo!.ConverterBase;
            return Deserialize<TValue>(jsonConverter, json, options, ref state);
        }

        private static TValue? Deserialize<TValue>(
            JsonConverter jsonConverter,
            ReadOnlySpan<char> json,
            JsonSerializerOptions options,
            ref ReadStack state)
        {
            const long ArrayPoolMaxSizeBeforeUsingNormalAlloc = 1024 * 1024;

            byte[]? tempArray = null;

            // For performance, avoid obtaining actual byte count unless memory usage is higher than the threshold.
            Span<byte> utf8 = json.Length <= (ArrayPoolMaxSizeBeforeUsingNormalAlloc / JsonConstants.MaxExpansionFactorWhileTranscoding) ?
                // Use a pooled alloc.
                tempArray = ArrayPool<byte>.Shared.Rent(json.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) :
                // Use a normal alloc since the pool would create a normal alloc anyway based on the threshold (per current implementation)
                // and by using a normal alloc we can avoid the Clear().
                new byte[JsonReaderHelper.GetUtf8ByteCount(json)];

            try
            {
                int actualByteCount = JsonReaderHelper.GetUtf8FromText(json, utf8);
                utf8 = utf8.Slice(0, actualByteCount);

                var readerState = new JsonReaderState(options.GetReaderOptions());
                var reader = new Utf8JsonReader(utf8, isFinalBlock: true, readerState);

                TValue? value = ReadCore<TValue>(jsonConverter, ref reader, options, ref state);

                // The reader should have thrown if we have remaining bytes.
                Debug.Assert(reader.BytesConsumed == actualByteCount);

                return value;
            }
            finally
            {
                if (tempArray != null)
                {
                    utf8.Clear();
                    ArrayPool<byte>.Shared.Return(tempArray);
                }
            }
        }

        /// <summary>
        /// Parse the text representing a single JSON value into a <typeparamref name="TValue"/>.
        /// </summary>
        /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <param name="json">JSON text to parse.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="json"/> is <see langword="null"/>.
        ///
        /// -or-
        ///
        /// <paramref name="jsonTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// The JSON is invalid.
        ///
        /// -or-
        ///
        /// <typeparamref name="TValue" /> is not compatible with the JSON.
        ///
        /// -or-
        ///
        /// There is remaining data in the string beyond a single JSON value.</exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        /// <remarks>Using a <see cref="string"/> is not as efficient as using the
        /// UTF-8 methods since the implementation natively uses UTF-8.
        /// </remarks>
        public static TValue? Deserialize<TValue>(string json, JsonTypeInfo<TValue> jsonTypeInfo)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            return DeserializeUsingMetadata<TValue?>(json.AsSpan(), jsonTypeInfo);
        }

        /// <summary>
        /// Parse the text representing a single JSON value into a <typeparamref name="TValue"/>.
        /// </summary>
        /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <param name="json">JSON text to parse.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="json"/> is <see langword="null"/>.
        ///
        /// -or-
        ///
        /// <paramref name="jsonTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// The JSON is invalid.
        ///
        /// -or-
        ///
        /// <typeparamref name="TValue" /> is not compatible with the JSON.
        ///
        /// -or-
        ///
        /// There is remaining data in the string beyond a single JSON value.</exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        /// <remarks>Using a <see cref="string"/> is not as efficient as using the
        /// UTF-8 methods since the implementation natively uses UTF-8.
        /// </remarks>
        public static TValue? Deserialize<TValue>(ReadOnlySpan<char> json, JsonTypeInfo<TValue> jsonTypeInfo)
        {
            return DeserializeUsingMetadata<TValue?>(json, jsonTypeInfo);
        }

        /// <summary>
        /// Parse the text representing a single JSON value into a <paramref name="returnType"/>.
        /// </summary>
        /// <returns>A <paramref name="returnType"/> representation of the JSON value.</returns>
        /// <param name="json">JSON text to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="json"/> is <see langword="null"/>.
        ///
        /// -or-
        ///
        /// <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// The JSON is invalid.
        ///
        /// -or-
        ///
        /// <paramref name="returnType" /> is not compatible with the JSON.
        ///
        /// -or-
        ///
        /// There is remaining data in the string beyond a single JSON value.</exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="returnType"/> or its serializable members.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonSerializerContext.GetTypeInfo(Type)"/> method of the provided
        /// <paramref name="context"/> returns <see langword="null"/> for the type to convert.
        /// </exception>
        /// <remarks>Using a <see cref="string"/> is not as efficient as using the
        /// UTF-8 methods since the implementation natively uses UTF-8.
        /// </remarks>
        public static object? Deserialize(string json, Type returnType, JsonSerializerContext context)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            return Deserialize(json.AsSpan(), returnType, context);
        }

        /// <summary>
        /// Parse the text representing a single JSON value into a <paramref name="returnType"/>.
        /// </summary>
        /// <returns>A <paramref name="returnType"/> representation of the JSON value.</returns>
        /// <param name="json">JSON text to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="json"/> is <see langword="null"/>.
        ///
        /// -or-
        ///
        /// <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// The JSON is invalid.
        ///
        /// -or-
        ///
        /// <paramref name="returnType" /> is not compatible with the JSON.
        ///
        /// -or-
        ///
        /// There is remaining data in the string beyond a single JSON value.</exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="returnType"/> or its serializable members.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonSerializerContext.GetTypeInfo(Type)"/> method of the provided
        /// <paramref name="context"/> returns <see langword="null"/> for the type to convert.
        /// </exception>
        /// <remarks>Using a <see cref="string"/> is not as efficient as using the
        /// UTF-8 methods since the implementation natively uses UTF-8.
        /// </remarks>
        public static object? Deserialize(ReadOnlySpan<char> json, Type returnType, JsonSerializerContext context)
        {
            if (returnType == null)
            {
                throw new ArgumentNullException(nameof(returnType));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return DeserializeUsingMetadata<object?>(
                json,
                JsonHelpers.GetTypeInfo(context, returnType));
        }

        private static TValue? DeserializeUsingMetadata<TValue>(ReadOnlySpan<char> json, JsonTypeInfo? jsonTypeInfo)
        {
            if (jsonTypeInfo == null)
            {
                throw new ArgumentNullException(nameof(jsonTypeInfo));
            }

            ReadStack state = default;
            state.Initialize(jsonTypeInfo);

            return Deserialize<TValue>(
                jsonTypeInfo.PropertyInfoForTypeInfo.ConverterBase,
                json,
                jsonTypeInfo.Options,
                ref state);
        }
    }
}
