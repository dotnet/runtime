// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace System.Text.Json
{
    /// <summary>
    ///   Represents a single property for a JSON object.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public readonly struct JsonProperty
    {
        /// <summary>
        ///   The value of this property.
        /// </summary>
        public JsonElement Value { get; }
        private string? _name { get; }

        internal JsonProperty(JsonElement value, string? name = null)
        {
            Value = value;
            _name = name;
        }

        /// <summary>
        ///   The name of this property.
        /// </summary>
        public string Name => _name ?? Value.GetPropertyName();

        /// <summary>
        ///   Compares <paramref name="text" /> to the name of this property.
        /// </summary>
        /// <param name="text">The text to compare against.</param>
        /// <returns>
        ///   <see langword="true" /> if the name of this property matches <paramref name="text"/>,
        ///   <see langword="false" /> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonTokenType.PropertyName"/>.
        /// </exception>
        /// <remarks>
        ///   This method is functionally equal to doing an ordinal comparison of <paramref name="text" /> and
        ///   <see cref="Name" />, but can avoid creating the string instance.
        /// </remarks>
        public bool NameEquals(string? text)
        {
            return NameEquals(text.AsSpan());
        }

        /// <summary>
        ///   Compares the text represented by <paramref name="utf8Text" /> to the name of this property.
        /// </summary>
        /// <param name="utf8Text">The UTF-8 encoded text to compare against.</param>
        /// <returns>
        ///   <see langword="true" /> if the name of this property has the same UTF-8 encoding as
        ///   <paramref name="utf8Text" />, <see langword="false" /> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonTokenType.PropertyName"/>.
        /// </exception>
        /// <remarks>
        ///   This method is functionally equal to doing an ordinal comparison of <paramref name="utf8Text" /> and
        ///   <see cref="Name" />, but can avoid creating the string instance.
        /// </remarks>
        public bool NameEquals(ReadOnlySpan<byte> utf8Text)
        {
            return Value.TextEqualsHelper(utf8Text, isPropertyName: true, shouldUnescape: true);
        }

        /// <summary>
        ///   Compares <paramref name="text" /> to the name of this property.
        /// </summary>
        /// <param name="text">The text to compare against.</param>
        /// <returns>
        ///   <see langword="true" /> if the name of this property matches <paramref name="text"/>,
        ///   <see langword="false" /> otherwise.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   This value's <see cref="Type"/> is not <see cref="JsonTokenType.PropertyName"/>.
        /// </exception>
        /// <remarks>
        ///   This method is functionally equal to doing an ordinal comparison of <paramref name="text" /> and
        ///   <see cref="Name" />, but can avoid creating the string instance.
        /// </remarks>
        public bool NameEquals(ReadOnlySpan<char> text)
        {
            return Value.TextEqualsHelper(text, isPropertyName: true);
        }

        internal bool EscapedNameEquals(ReadOnlySpan<byte> utf8Text)
        {
            return Value.TextEqualsHelper(utf8Text, isPropertyName: true, shouldUnescape: false);
        }

        /// <summary>
        ///   Write the property into the provided writer as a named JSON object property.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <exception cref="ArgumentNullException">
        ///   The <paramref name="writer"/> parameter is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   This <see cref="Name"/>'s length is too large to be a JSON object property.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   This <see cref="Value"/>'s <see cref="JsonElement.ValueKind"/> would result in an invalid JSON.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The parent <see cref="JsonDocument"/> has been disposed.
        /// </exception>>
        public void WriteTo(Utf8JsonWriter writer)
        {
            if (writer is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(writer));
            }

            writer.WritePropertyName(Name);
            Value.WriteTo(writer);
        }

        /// <summary>
        ///   Provides a <see cref="string"/> representation of the property for
        ///   debugging purposes.
        /// </summary>
        /// <returns>
        ///   A string containing the un-interpreted value of the property, beginning
        ///   at the declaring open-quote and ending at the last character that is part of
        ///   the value.
        /// </returns>
        public override string ToString()
        {
            return Value.GetPropertyRawText();
        }

        private string DebuggerDisplay
            => Value.ValueKind == JsonValueKind.Undefined ? "<Undefined>" : $"\"{ToString()}\"";
    }
}
