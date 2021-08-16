// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace System.Text.Json
{
    public readonly partial struct JsonElement
    {
        /// <summary>
        ///   Parses one JSON value (including objects or arrays) from the provided reader.
        /// </summary>
        /// <param name="reader">The reader to read.</param>
        /// <returns>
        ///   A JsonElement representing the value (and nested values) read from the reader.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///     If the <see cref="Utf8JsonReader.TokenType"/> property of <paramref name="reader"/>
        ///     is <see cref="JsonTokenType.PropertyName"/> or <see cref="JsonTokenType.None"/>, the
        ///     reader will be advanced by one call to <see cref="Utf8JsonReader.Read"/> to determine
        ///     the start of the value.
        ///   </para>
        ///
        ///   <para>
        ///     Upon completion of this method, <paramref name="reader"/> will be positioned at the
        ///     final token in the JSON value. If an exception is thrown, the reader is reset to
        ///     the state it was in when the method was called.
        ///   </para>
        ///
        ///   <para>
        ///     This method makes a copy of the data the reader acted on, so there is no caller
        ///     requirement to maintain data integrity beyond the return of this method.
        ///   </para>
        /// </remarks>
        /// <exception cref="ArgumentException">
        ///   <paramref name="reader"/> is using unsupported options.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The current <paramref name="reader"/> token does not start or represent a value.
        /// </exception>
        /// <exception cref="JsonException">
        ///   A value could not be read from the reader.
        /// </exception>
        public static JsonElement ParseValue(ref Utf8JsonReader reader)
        {
            bool ret = JsonDocument.TryParseValue(ref reader, out JsonDocument? document, shouldThrow: true, useArrayPools: false);

            Debug.Assert(ret, "TryParseValue returned false with shouldThrow: true.");
            Debug.Assert(document != null, "null document returned with shouldThrow: true.");
            return document.RootElement;
        }

        internal static JsonElement ParseValue(Stream utf8Json, JsonDocumentOptions options)
        {
            JsonDocument document = JsonDocument.ParseValue(utf8Json, options);
            return document.RootElement;
        }

        internal static JsonElement ParseValue(ReadOnlySpan<byte> utf8Json, JsonDocumentOptions options)
        {
            JsonDocument document = JsonDocument.ParseValue(utf8Json, options);
            return document.RootElement;
        }

        internal static JsonElement ParseValue(string json, JsonDocumentOptions options)
        {
            JsonDocument document = JsonDocument.ParseValue(json, options);
            return document.RootElement;
        }

        /// <summary>
        ///   Attempts to parse one JSON value (including objects or arrays) from the provided reader.
        /// </summary>
        /// <param name="reader">The reader to read.</param>
        /// <param name="element">Receives the parsed element.</param>
        /// <returns>
        ///   <see langword="true"/> if a value was read and parsed into a JsonElement;
        ///   <see langword="false"/> if the reader ran out of data while parsing.
        ///   All other situations result in an exception being thrown.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///     If the <see cref="Utf8JsonReader.TokenType"/> property of <paramref name="reader"/>
        ///     is <see cref="JsonTokenType.PropertyName"/> or <see cref="JsonTokenType.None"/>, the
        ///     reader will be advanced by one call to <see cref="Utf8JsonReader.Read"/> to determine
        ///     the start of the value.
        ///   </para>
        ///
        ///   <para>
        ///     Upon completion of this method, <paramref name="reader"/> will be positioned at the
        ///     final token in the JSON value.  If an exception is thrown, or <see langword="false"/>
        ///     is returned, the reader is reset to the state it was in when the method was called.
        ///   </para>
        ///
        ///   <para>
        ///     This method makes a copy of the data the reader acted on, so there is no caller
        ///     requirement to maintain data integrity beyond the return of this method.
        ///   </para>
        /// </remarks>
        /// <exception cref="ArgumentException">
        ///   <paramref name="reader"/> is using unsupported options.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The current <paramref name="reader"/> token does not start or represent a value.
        /// </exception>
        /// <exception cref="JsonException">
        ///   A value could not be read from the reader.
        /// </exception>
        public static bool TryParseValue(ref Utf8JsonReader reader, [NotNullWhen(true)] out JsonElement? element)
        {
            bool ret = JsonDocument.TryParseValue(ref reader, out JsonDocument? document, shouldThrow: false, useArrayPools: false);
            element = document?.RootElement;
            return ret;
        }
    }
}
