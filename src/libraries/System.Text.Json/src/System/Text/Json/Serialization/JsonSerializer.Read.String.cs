// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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
        /// Thrown when the JSON is invalid,
        /// <typeparamref name="TValue"/> is not compatible with the JSON,
        /// or when there is remaining data in the Stream.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        /// <remarks>Using a <see cref="string"/> is not as efficient as using the
        /// UTF-8 methods since the implementation natively uses UTF-8.
        /// </remarks>
        public static TValue? Deserialize<[DynamicallyAccessedMembers(MembersAccessedOnRead)] TValue>(string json, JsonSerializerOptions? options = null)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

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
        /// Thrown when the JSON is invalid,
        /// the <paramref name="returnType"/> is not compatible with the JSON,
        /// or when there is remaining data in the Stream.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="returnType"/> or its serializable members.
        /// </exception>
        /// <remarks>Using a <see cref="string"/> is not as efficient as using the
        /// UTF-8 methods since the implementation natively uses UTF-8.
        /// </remarks>
        public static object? Deserialize(string json, [DynamicallyAccessedMembers(MembersAccessedOnRead)] Type returnType, JsonSerializerOptions? options = null)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            if (returnType == null)
            {
                throw new ArgumentNullException(nameof(returnType));
            }

            object? value = Deserialize<object?>(json, returnType, options)!;

            return value;
        }

        private static TValue? Deserialize<TValue>(string json, Type returnType, JsonSerializerOptions? options)
        {
            const long ArrayPoolMaxSizeBeforeUsingNormalAlloc = 1024 * 1024;

            if (options == null)
            {
                options = JsonSerializerOptions.s_defaultOptions;
            }

            byte[]? tempArray = null;

            // For performance, avoid obtaining actual byte count unless memory usage is higher than the threshold.
            Span<byte> utf8 = json.Length <= (ArrayPoolMaxSizeBeforeUsingNormalAlloc / JsonConstants.MaxExpansionFactorWhileTranscoding) ?
                // Use a pooled alloc.
                tempArray = ArrayPool<byte>.Shared.Rent(json.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) :
                // Use a normal alloc since the pool would create a normal alloc anyway based on the threshold (per current implementation)
                // and by using a normal alloc we can avoid the Clear().
                new byte[JsonReaderHelper.GetUtf8ByteCount(json.AsSpan())];

            try
            {
                int actualByteCount = JsonReaderHelper.GetUtf8FromText(json.AsSpan(), utf8);
                utf8 = utf8.Slice(0, actualByteCount);

                var readerState = new JsonReaderState(options.GetReaderOptions());
                var reader = new Utf8JsonReader(utf8, isFinalBlock: true, readerState);

                TValue value = ReadCore<TValue>(ref reader, returnType, options);

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
    }
}
