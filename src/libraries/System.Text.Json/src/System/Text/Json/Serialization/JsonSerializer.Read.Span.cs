// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Parse the UTF-8 encoded text representing a single JSON value into a <typeparamref name="TValue"/>.
        /// </summary>
        /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <param name="utf8Json">JSON text to parse.</param>
        /// <param name="options">Options to control the behavior during parsing.</param>
        /// <exception cref="JsonException">
        /// Thrown when the JSON is invalid,
        /// <typeparamref name="TValue"/> is not compatible with the JSON,
        /// or when there is remaining data in the Stream.
        /// </exception>
        public static TValue Parse<TValue>(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions options = null)
        {
            return (TValue)ParseCore(utf8Json, typeof(TValue), options);
        }

        /// <summary>
        /// Parse the UTF-8 encoded text representing a single JSON value into a <paramref name="returnType"/>.
        /// </summary>
        /// <returns>A <paramref name="returnType"/> representation of the JSON value.</returns>
        /// <param name="utf8Json">JSON text to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="options">Options to control the behavior during parsing.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="returnType"/> is null.
        /// </exception>
        /// <exception cref="JsonException">
        /// Thrown when the JSON is invalid,
        /// <paramref name="returnType"/> is not compatible with the JSON,
        /// or when there is remaining data in the Stream.
        /// </exception>
        public static object Parse(ReadOnlySpan<byte> utf8Json, Type returnType, JsonSerializerOptions options = null)
        {
            if (returnType == null)
                throw new ArgumentNullException(nameof(returnType));

            return ParseCore(utf8Json, returnType, options);
        }

        private static object ParseCore(ReadOnlySpan<byte> utf8Json, Type returnType, JsonSerializerOptions options)
        {
            if (options == null)
            {
                options = JsonSerializerOptions.s_defaultOptions;
            }

            var readerState = new JsonReaderState(options.GetReaderOptions());
            var reader = new Utf8JsonReader(utf8Json, isFinalBlock: true, readerState);
            object result = ReadCore(returnType, options, ref reader);

            if (reader.BytesConsumed != utf8Json.Length)
            {
                readerState = reader.CurrentState;
                ThrowHelper.ThrowJsonException_DeserializeDataRemaining(utf8Json.Length, utf8Json.Length - readerState.BytesConsumed);
            }

            return result;
        }
    }
}
