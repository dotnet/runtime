// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Parse the text representing a single JSON value into a <typeparamref name="TValue"/>.
        /// </summary>
        /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <param name="json">JSON text to parse.</param>
        /// <param name="options">Options to control the behavior during parsing.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="json"/> is null.
        /// </exception>
        /// <exception cref="JsonReaderException">
        /// Thrown when the JSON is invalid,
        /// <typeparamref name="TValue"/> is not compatible with the JSON,
        /// or when there is remaining data in the Stream.
        /// </exception>
        /// <remarks>Using a UTF-16 <see cref="System.String"/> is not as efficient as using the
        /// UTF-8 methods since the implementation natively uses UTF-8.
        /// </remarks>
        public static TValue Parse<TValue>(string json, JsonSerializerOptions options = null)
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));

            return (TValue)ParseCore(json, typeof(TValue), options);
        }

        /// <summary>
        /// Parse the text representing a single JSON value into a <paramref name="returnType"/>.
        /// </summary>
        /// <returns>A <paramref name="returnType"/> representation of the JSON value.</returns>
        /// <param name="json">JSON text to parse.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="options">Options to control the behavior during parsing.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="json"/> or <paramref name="returnType"/> is null.
        /// </exception>
        /// <exception cref="JsonReaderException">
        /// Thrown when the JSON is invalid,
        /// the <paramref name="returnType"/> is not compatible with the JSON,
        /// or when there is remaining data in the Stream.
        /// </exception>
        /// <remarks>Using a UTF-16 <see cref="System.String"/> is not as efficient as using the
        /// UTF-8 methods since the implementation natively uses UTF-8.
        /// </remarks>
        public static object Parse(string json, Type returnType, JsonSerializerOptions options = null)
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));

            if (returnType == null)
                throw new ArgumentNullException(nameof(returnType));

            return ParseCore(json, returnType, options);
        }

        private static object ParseCore(string json, Type returnType, JsonSerializerOptions options = null)
        {
            if (options == null)
            {
                options = JsonSerializerOptions.s_defaultOptions;
            }

            // todo: use an array pool here for smaller requests to avoid the alloc?
            byte[] jsonBytes = JsonReaderHelper.s_utf8Encoding.GetBytes(json);
            var readerState = new JsonReaderState(options.GetReaderOptions());
            var reader = new Utf8JsonReader(jsonBytes, isFinalBlock: true, readerState);
            object result = ReadCore(returnType, options, ref reader);

            if (reader.BytesConsumed != jsonBytes.Length)
            {
                readerState = reader.CurrentState;
                throw new JsonReaderException(SR.Format(SR.DeserializeDataRemaining,
                    jsonBytes.Length, jsonBytes.Length - readerState.BytesConsumed), readerState);
            }

            return result;
        }
    }
}
