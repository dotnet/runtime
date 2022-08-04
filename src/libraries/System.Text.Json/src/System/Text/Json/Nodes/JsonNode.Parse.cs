// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Nodes
{
    public abstract partial class JsonNode
    {
        /// <summary>
        ///   Parses one JSON value (including objects or arrays) from the provided reader.
        /// </summary>
        /// <param name="reader">The reader to read.</param>
        /// <param name="nodeOptions">Options to control the behavior.</param>
        /// <returns>
        ///   The <see cref="JsonNode"/> from the reader.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///     If the <see cref="Utf8JsonReader.TokenType"/> property of <paramref name="reader"/>
        ///     is <see cref="JsonTokenType.PropertyName"/> or <see cref="JsonTokenType.None"/>, the
        ///     reader will be advanced by one call to <see cref="Utf8JsonReader.Read"/> to determine
        ///     the start of the value.
        ///   </para>
        ///   <para>
        ///     Upon completion of this method, <paramref name="reader"/> will be positioned at the
        ///     final token in the JSON value.  If an exception is thrown, the reader is reset to the state it was in when the method was called.
        ///   </para>
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
        public static JsonNode? Parse(
            ref Utf8JsonReader reader,
            JsonNodeOptions? nodeOptions = null)
        {
            JsonElement element = JsonElement.ParseValue(ref reader);
            return JsonNodeConverter.Create(element, nodeOptions);
        }

        /// <summary>
        ///   Parses text representing a single JSON value.
        /// </summary>
        /// <param name="json">JSON text to parse.</param>
        /// <param name="nodeOptions">Options to control the node behavior after parsing.</param>
        /// <param name="documentOptions">Options to control the document behavior during parsing.</param>
        /// <returns>
        ///   A <see cref="JsonNode"/> representation of the JSON value.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="json"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        ///   <paramref name="json"/> does not represent a valid single JSON value.
        /// </exception>
        public static JsonNode? Parse(
            [StringSyntax(StringSyntaxAttribute.Json)] string json,
            JsonNodeOptions? nodeOptions = null,
            JsonDocumentOptions documentOptions = default(JsonDocumentOptions))
        {
            if (json is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(json));
            }

            JsonElement element = JsonElement.ParseValue(json, documentOptions);
            return JsonNodeConverter.Create(element, nodeOptions);
        }

        /// <summary>
        ///   Parses text representing a single JSON value.
        /// </summary>
        /// <param name="utf8Json">JSON text to parse.</param>
        /// <param name="nodeOptions">Options to control the node behavior after parsing.</param>
        /// <param name="documentOptions">Options to control the document behavior during parsing.</param>
        /// <returns>
        ///   A <see cref="JsonNode"/> representation of the JSON value.
        /// </returns>
        /// <exception cref="JsonException">
        ///   <paramref name="utf8Json"/> does not represent a valid single JSON value.
        /// </exception>
        public static JsonNode? Parse(
            ReadOnlySpan<byte> utf8Json,
            JsonNodeOptions? nodeOptions = null,
            JsonDocumentOptions documentOptions = default(JsonDocumentOptions))
        {
            JsonElement element = JsonElement.ParseValue(utf8Json, documentOptions);
            return JsonNodeConverter.Create(element, nodeOptions);
        }

        /// <summary>
        ///   Parse a <see cref="Stream"/> as UTF-8-encoded data representing a single JSON value into a
        ///   <see cref="JsonNode"/>.  The Stream will be read to completion.
        /// </summary>
        /// <param name="utf8Json">JSON text to parse.</param>
        /// <param name="nodeOptions">Options to control the node behavior after parsing.</param>
        /// <param name="documentOptions">Options to control the document behavior during parsing.</param>
        /// <returns>
        ///   A <see cref="JsonNode"/> representation of the JSON value.
        /// </returns>
        /// <exception cref="JsonException">
        ///   <paramref name="utf8Json"/> does not represent a valid single JSON value.
        /// </exception>
        public static JsonNode? Parse(
            Stream utf8Json,
            JsonNodeOptions? nodeOptions = null,
            JsonDocumentOptions documentOptions = default)
        {
            if (utf8Json is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(utf8Json));
            }

            JsonElement element = JsonElement.ParseValue(utf8Json, documentOptions);
            return JsonNodeConverter.Create(element, nodeOptions);
        }
    }
}
