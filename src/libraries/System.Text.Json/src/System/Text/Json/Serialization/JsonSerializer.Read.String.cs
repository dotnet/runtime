// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
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
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        public static TValue? Deserialize<TValue>(string json, JsonSerializerOptions? options = null)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            return ReadUsingOptions<TValue>(json.AsSpan(), typeof(TValue), options);
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
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        public static TValue? Deserialize<TValue>(ReadOnlySpan<char> json, JsonSerializerOptions? options = null)
        {
            // default/null span is treated as empty

            return ReadUsingOptions<TValue>(json, typeof(TValue), options);
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
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        public static object? Deserialize(string json, Type returnType, JsonSerializerOptions? options = null)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            if (returnType == null)
            {
                throw new ArgumentNullException(nameof(returnType));
            }

            return ReadUsingOptions<object?>(json.AsSpan(), returnType, options)!;
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
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        public static object? Deserialize(ReadOnlySpan<char> json, Type returnType, JsonSerializerOptions? options = null)
        {
            // default/null span is treated as empty

            if (returnType == null)
            {
                throw new ArgumentNullException(nameof(returnType));
            }

            return ReadUsingOptions<object?>(json, returnType, options)!;
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
            // default/null span is treated as empty

            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            if (jsonTypeInfo == null)
            {
                throw new ArgumentNullException(nameof(jsonTypeInfo));
            }

            return ReadUsingMetadata<TValue?>(json.AsSpan(), jsonTypeInfo);
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
            // default/null span is treated as empty

            if (jsonTypeInfo == null)
            {
                throw new ArgumentNullException(nameof(jsonTypeInfo));
            }

            return ReadUsingMetadata<TValue?>(json, jsonTypeInfo);
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

            return ReadUsingMetadata<object?>(json, GetTypeInfo(context, returnType));
        }

        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        private static TValue? ReadUsingOptions<TValue>(ReadOnlySpan<char> json, Type returnType, JsonSerializerOptions? options)
            => ReadUsingMetadata<TValue>(json, GetTypeInfo(returnType, options));

        private static TValue? ReadUsingMetadata<TValue>(ReadOnlySpan<char> json, JsonTypeInfo jsonTypeInfo)
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
                return ReadUsingMetadata<TValue>(utf8, jsonTypeInfo, actualByteCount);
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
    }
}
