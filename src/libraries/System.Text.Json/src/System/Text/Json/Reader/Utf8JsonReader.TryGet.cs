// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;

namespace System.Text.Json
{
    public ref partial struct Utf8JsonReader
    {
        /// <summary>
        /// Reads the next JSON token value from the source, unescaped, and transcoded as a <see cref="string"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of the JSON token that is not a string
        /// (i.e. other than <see cref="JsonTokenType.String"/> or <see cref="JsonTokenType.PropertyName"/>).
        /// <seealso cref="TokenType" />
        /// I will also throw when the JSON string contains invalid UTF-8 bytes, or invalid UTF-16 surrogates.
        /// </exception>
        public string GetStringValue()
        {
            if (TokenType != JsonTokenType.String && TokenType != JsonTokenType.PropertyName)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedString(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;

            int idx = span.IndexOf(JsonConstants.BackSlash);
            if (idx != -1)
            {
                return JsonReaderHelper.GetUnescapedString(span, idx);
            }
            return JsonReaderHelper.TranscodeHelper(span);
        }

        /// <summary>
        /// Reads the next JSON token value from the source as a <see cref="bool"/>.
        /// Returns true if the TokenType is JsonTokenType.True and false if the TokenType is JsonTokenType.False.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of JSON token that is not a boolean (i.e. <see cref="JsonTokenType.True"/> or <see cref="JsonTokenType.False"/>).
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool GetBooleanValue()
        {
            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;

            if (TokenType == JsonTokenType.True)
            {
                Debug.Assert(span.Length == 4);
                return true;
            }
            else if (TokenType == JsonTokenType.False)
            {
                Debug.Assert(span.Length == 5);
                return false;
            }
            else
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedBoolean(TokenType);
            }
        }

        /// <summary>
        /// Reads the next JSON token value from the source and parses it to a <see cref="int"/>.
        /// Returns true if the entire UTF-8 encoded token value can be successfully 
        /// parsed to a <see cref="int"/> value.
        /// Returns false otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetInt32Value(out int value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return Utf8Parser.TryParse(span, out value, out int bytesConsumed) && span.Length == bytesConsumed;
        }

        /// <summary>
        /// Reads the next JSON token value from the source and parses it to a <see cref="long"/>.
        /// Returns true if the entire UTF-8 encoded token value can be successfully 
        /// parsed to a <see cref="long"/> value.
        /// Returns false otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetInt64Value(out long value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return Utf8Parser.TryParse(span, out value, out int bytesConsumed) && span.Length == bytesConsumed;
        }

        /// <summary>
        /// Reads the next JSON token value from the source and parses it to a <see cref="float"/>.
        /// Returns true if the entire UTF-8 encoded token value can be successfully 
        /// parsed to a <see cref="float"/> value.
        /// Returns false otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetSingleValue(out float value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return Utf8Parser.TryParse(span, out value, out int bytesConsumed, _numberFormat) && span.Length == bytesConsumed;
        }

        /// <summary>
        /// Reads the next JSON token value from the source and parses it to a <see cref="double"/>.
        /// Returns true if the entire UTF-8 encoded token value can be successfully 
        /// parsed to a <see cref="double"/> value.
        /// Returns false otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetDoubleValue(out double value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return Utf8Parser.TryParse(span, out value, out int bytesConsumed, _numberFormat) && span.Length == bytesConsumed;
        }

        /// <summary>
        /// Reads the next JSON token value from the source and parses it to a <see cref="decimal"/>.
        /// Returns true if the entire UTF-8 encoded token value can be successfully 
        /// parsed to a <see cref="decimal"/> value.
        /// Returns false otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetDecimalValue(out decimal value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return Utf8Parser.TryParse(span, out value, out int bytesConsumed, _numberFormat) && span.Length == bytesConsumed;
        }
    }
}
