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
        /// Parses the current JSON token value from the source, unescaped, and transcoded as a <see cref="string"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of the JSON token that is not a string
        /// (i.e. other than <see cref="JsonTokenType.String"/> or <see cref="JsonTokenType.PropertyName"/>).
        /// <seealso cref="TokenType" />
        /// It will also throw when the JSON string contains invalid UTF-8 bytes, or invalid UTF-16 surrogates.
        /// </exception>
        public string GetString()
        {
            if (TokenType != JsonTokenType.String && TokenType != JsonTokenType.PropertyName)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedString(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;

            if (_stringHasEscaping)
            {
                int idx = span.IndexOf(JsonConstants.BackSlash);
                Debug.Assert(idx != -1);
                return JsonReaderHelper.GetUnescapedString(span, idx);
            }

            Debug.Assert(span.IndexOf(JsonConstants.BackSlash) == -1);
            return JsonReaderHelper.TranscodeHelper(span);
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a comment, transcoded as a <see cref="string"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of the JSON token that is not a comment.
        /// <seealso cref="TokenType" />
        /// </exception>
        public string GetComment()
        {
            if (TokenType != JsonTokenType.Comment)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedComment(TokenType);
            }
            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return JsonReaderHelper.TranscodeHelper(span);
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="bool"/>.
        /// Returns true if the TokenType is JsonTokenType.True and false if the TokenType is JsonTokenType.False.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a boolean (i.e. <see cref="JsonTokenType.True"/> or <see cref="JsonTokenType.False"/>).
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool GetBoolean()
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
        /// Parses the current JSON token value from the source as an <see cref="int"/>.
        /// Returns the value if the entire UTF-8 encoded token value can be successfully parsed to an <see cref="int"/>
        /// value.
        /// Throws exceptions otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the JSON token value is either of incorrect numeric format (for example if it contains a decimal or 
        /// is written in scientific notation) or, it represents a number less than <see cref="int.MinValue"/> or greater 
        /// than <see cref="int.MaxValue"/>.
        /// </exception>
        public int GetInt32()
        {
            if (!TryGetInt32(out int value))
            {
                throw ThrowHelper.GetFormatException(NumericType.Int32);
            }
            return value;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="long"/>.
        /// Returns the value if the entire UTF-8 encoded token value can be successfully parsed to a <see cref="long"/>
        /// value.
        /// Throws exceptions otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the JSON token value is either of incorrect numeric format (for example if it contains a decimal or 
        /// is written in scientific notation) or, it represents a number less than <see cref="long.MinValue"/> or greater 
        /// than <see cref="long.MaxValue"/>.
        /// </exception>
        public long GetInt64()
        {
            if (!TryGetInt64(out long value))
            {
                throw ThrowHelper.GetFormatException(NumericType.Int64);
            }
            return value;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="uint"/>.
        /// Returns the value if the entire UTF-8 encoded token value can be successfully parsed to a <see cref="uint"/>
        /// value.
        /// Throws exceptions otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the JSON token value is either of incorrect numeric format (for example if it contains a decimal or 
        /// is written in scientific notation) or, it represents a number less than <see cref="uint.MinValue"/> or greater 
        /// than <see cref="uint.MaxValue"/>.
        /// </exception>
        [System.CLSCompliantAttribute(false)]
        public uint GetUInt32()
        {
            if (!TryGetUInt32(out uint value))
            {
                throw ThrowHelper.GetFormatException(NumericType.UInt32);
            }
            return value;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="ulong"/>.
        /// Returns the value if the entire UTF-8 encoded token value can be successfully parsed to a <see cref="ulong"/>
        /// value.
        /// Throws exceptions otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the JSON token value is either of incorrect numeric format (for example if it contains a decimal or 
        /// is written in scientific notation) or, it represents a number less than <see cref="ulong.MinValue"/> or greater 
        /// than <see cref="ulong.MaxValue"/>.
        /// </exception>
        [System.CLSCompliantAttribute(false)]
        public ulong GetUInt64()
        {
            if (!TryGetUInt64(out ulong value))
            {
                throw ThrowHelper.GetFormatException(NumericType.UInt64);
            }
            return value;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="float"/>.
        /// Returns the value if the entire UTF-8 encoded token value can be successfully parsed to a <see cref="float"/>
        /// value.
        /// Throws exceptions otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the JSON token value represents a number less than <see cref="float.MinValue"/> or greater 
        /// than <see cref="float.MaxValue"/>.
        /// </exception>
        public float GetSingle()
        {
            if (!TryGetSingle(out float value))
            {
                throw ThrowHelper.GetFormatException(NumericType.Single);
            }
            return value;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="double"/>.
        /// Returns the value if the entire UTF-8 encoded token value can be successfully parsed to a <see cref="double"/>
        /// value.
        /// Throws exceptions otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the JSON token value represents a number less than <see cref="double.MinValue"/> or greater 
        /// than <see cref="double.MaxValue"/>.
        /// </exception>
        public double GetDouble()
        {
            if (!TryGetDouble(out double value))
            {
                throw ThrowHelper.GetFormatException(NumericType.Double);
            }
            return value;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="decimal"/>.
        /// Returns the value if the entire UTF-8 encoded token value can be successfully parsed to a <see cref="decimal"/>
        /// value.
        /// Throws exceptions otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the JSON token value represents a number less than <see cref="decimal.MinValue"/> or greater 
        /// than <see cref="decimal.MaxValue"/>.
        /// </exception>
        public decimal GetDecimal()
        {
            if (!TryGetDecimal(out decimal value))
            {
                throw ThrowHelper.GetFormatException(NumericType.Decimal);
            }
            return value;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="DateTime"/>.
        /// Returns the value if the entire UTF-8 encoded token value can be successfully parsed to a <see cref="DateTime"/>
        /// value.
        /// Throws exceptions otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.String"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the JSON token value is of an unsupported format. Only a subset of ISO 8601 formats are supported.
        /// </exception>
        public DateTime GetDateTime()
        {
            if (!TryGetDateTime(out DateTime value))
            {
                throw ThrowHelper.GetFormatException(DateType.DateTime);
            }

            return value;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="DateTimeOffset"/>.
        /// Returns the value if the entire UTF-8 encoded token value can be successfully parsed to a <see cref="DateTimeOffset"/>
        /// value.
        /// Throws exceptions otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.String"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the JSON token value is of an unsupported format. Only a subset of ISO 8601 formats are supported.
        /// </exception>
        public DateTimeOffset GetDateTimeOffset()
        {
            if (!TryGetDateTimeOffset(out DateTimeOffset value))
            {
                throw ThrowHelper.GetFormatException(DateType.DateTimeOffset);
            }

            return value;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="Guid"/>.
        /// Returns the value if the entire UTF-8 encoded token value can be successfully parsed to a <see cref="Guid"/>
        /// value.
        /// Throws exceptions otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.String"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the JSON token value is of an unsupported format for a Guid.
        /// </exception>
        public Guid GetGuid()
        {
            if (!TryGetGuid(out Guid value))
            {
                throw new FormatException(SR.FormatGuid);
            }

            return value;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as an <see cref="int"/>.
        /// Returns true if the entire UTF-8 encoded token value can be successfully 
        /// parsed to an <see cref="int"/> value.
        /// Returns false otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetInt32(out int value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return Utf8Parser.TryParse(span, out value, out int bytesConsumed) && span.Length == bytesConsumed;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="long"/>.
        /// Returns true if the entire UTF-8 encoded token value can be successfully 
        /// parsed to a <see cref="long"/> value.
        /// Returns false otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetInt64(out long value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return Utf8Parser.TryParse(span, out value, out int bytesConsumed) && span.Length == bytesConsumed;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="uint"/>.
        /// Returns true if the entire UTF-8 encoded token value can be successfully 
        /// parsed to a <see cref="uint"/> value.
        /// Returns false otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        [System.CLSCompliantAttribute(false)]
        public bool TryGetUInt32(out uint value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return Utf8Parser.TryParse(span, out value, out int bytesConsumed) && span.Length == bytesConsumed;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="ulong"/>.
        /// Returns true if the entire UTF-8 encoded token value can be successfully 
        /// parsed to a <see cref="ulong"/> value.
        /// Returns false otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        [System.CLSCompliantAttribute(false)]
        public bool TryGetUInt64(out ulong value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return Utf8Parser.TryParse(span, out value, out int bytesConsumed) && span.Length == bytesConsumed;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="float"/>.
        /// Returns true if the entire UTF-8 encoded token value can be successfully 
        /// parsed to a <see cref="float"/> value.
        /// Returns false otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetSingle(out float value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return Utf8Parser.TryParse(span, out value, out int bytesConsumed, _numberFormat) && span.Length == bytesConsumed;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="double"/>.
        /// Returns true if the entire UTF-8 encoded token value can be successfully 
        /// parsed to a <see cref="double"/> value.
        /// Returns false otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetDouble(out double value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return Utf8Parser.TryParse(span, out value, out int bytesConsumed, _numberFormat) && span.Length == bytesConsumed;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="decimal"/>.
        /// Returns true if the entire UTF-8 encoded token value can be successfully 
        /// parsed to a <see cref="decimal"/> value.
        /// Returns false otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetDecimal(out decimal value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return Utf8Parser.TryParse(span, out value, out int bytesConsumed, _numberFormat) && span.Length == bytesConsumed;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="DateTime"/>.
        /// Returns true if the entire UTF-8 encoded token value can be successfully
        /// parsed to a <see cref="DateTime"/> value.
        /// Returns false otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.String"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetDateTime(out DateTime value)
        {
            if (TokenType != JsonTokenType.String)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedString(TokenType);
            }

            ReadOnlySpan<byte> span = stackalloc byte[0];

            if (HasValueSequence)
            {
                long sequenceLength = ValueSequence.Length;

                if (!JsonReaderHelper.IsValidDateTimeOffsetParseLength(sequenceLength))
                {
                    value = default;
                    return false;
                }

                Debug.Assert(sequenceLength <= JsonConstants.MaximumEscapedDateTimeOffsetParseLength);
                Span<byte> stackSpan = stackalloc byte[(int)sequenceLength];

                ValueSequence.CopyTo(stackSpan);
                span = stackSpan;
            }
            else
            {
                if (!JsonReaderHelper.IsValidDateTimeOffsetParseLength(ValueSpan.Length))
                {
                    value = default;
                    return false;
                }

                span = ValueSpan;
            }

            if (_stringHasEscaping)
            {
                return JsonReaderHelper.TryGetEscapedDateTime(span, out value);
            }

            Debug.Assert(span.IndexOf(JsonConstants.BackSlash) == -1);

            value = default;
            return (span.Length <= JsonConstants.MaximumDateTimeOffsetParseLength)
                && JsonHelpers.TryParseAsISO(span, out value, out int bytesConsumed)
                && span.Length == bytesConsumed;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="DateTimeOffset"/>.
        /// Returns true if the entire UTF-8 encoded token value can be successfully
        /// parsed to a <see cref="DateTimeOffset"/> value.
        /// Returns false otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.String"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetDateTimeOffset(out DateTimeOffset value)
        {
            if (TokenType != JsonTokenType.String)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedString(TokenType);
            }

            ReadOnlySpan<byte> span = stackalloc byte[0];

            if (HasValueSequence)
            {
                long sequenceLength = ValueSequence.Length;

                if (!JsonReaderHelper.IsValidDateTimeOffsetParseLength(sequenceLength))
                {
                    value = default;
                    return false;
                }

                Debug.Assert(sequenceLength <= JsonConstants.MaximumEscapedDateTimeOffsetParseLength);
                Span<byte> stackSpan = stackalloc byte[(int)sequenceLength];

                ValueSequence.CopyTo(stackSpan);
                span = stackSpan;
            }
            else
            {
                if (!JsonReaderHelper.IsValidDateTimeOffsetParseLength(ValueSpan.Length))
                {
                    value = default;
                    return false;
                }

                span = ValueSpan;
            }

            if (_stringHasEscaping)
            {
                return JsonReaderHelper.TryGetEscapedDateTimeOffset(span, out value);
            }

            Debug.Assert(span.IndexOf(JsonConstants.BackSlash) == -1);

            value = default;
            return (span.Length <= JsonConstants.MaximumDateTimeOffsetParseLength)
                && JsonHelpers.TryParseAsISO(span, out value, out int bytesConsumed)
                && span.Length == bytesConsumed;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="Guid"/>.
        /// Returns <see langword="true"/> if the entire UTF-8 encoded token value can be successfully
        /// parsed to a <see cref="Guid"/> value. Only supports <see cref="Guid"/> values with hyphens
        /// and without any surrounding decorations.
        /// Returns <see langword="false"/> otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.String"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetGuid(out Guid value)
        {
            if (TokenType != JsonTokenType.String)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedString(TokenType);
            }

            ReadOnlySpan<byte> span = stackalloc byte[0];

            if (HasValueSequence)
            {
                long sequenceLength = ValueSequence.Length;
                if (sequenceLength > JsonConstants.MaximumEscapedGuidLength)
                {
                    value = default;
                    return false;
                }

                Debug.Assert(sequenceLength <= JsonConstants.MaximumEscapedGuidLength);
                Span<byte> stackSpan = stackalloc byte[(int)sequenceLength];

                ValueSequence.CopyTo(stackSpan);
                span = stackSpan;
            }
            else
            {
                if (ValueSpan.Length > JsonConstants.MaximumEscapedGuidLength)
                {
                    value = default;
                    return false;
                }

                span = ValueSpan;
            }

            if (_stringHasEscaping)
            {
                return JsonReaderHelper.TryGetEscapedGuid(span, out value);
            }

            Debug.Assert(span.IndexOf(JsonConstants.BackSlash) == -1);

            value = default;
            return (span.Length == JsonConstants.MaximumFormatGuidLength) && Utf8Parser.TryParse(span, out value, out _, 'D');
        }
    }
}
