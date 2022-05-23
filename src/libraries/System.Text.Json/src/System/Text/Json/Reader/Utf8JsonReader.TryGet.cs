// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    public ref partial struct Utf8JsonReader
    {
        /// <summary>
        /// Parses the current JSON token value from the source, unescaped, and transcoded as a <see cref="string"/>.
        /// </summary>
        /// <remarks>
        /// Returns <see langword="null" /> when <see cref="TokenType"/> is <see cref="JsonTokenType.Null"/>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of the JSON token that is not a string
        /// (i.e. other than <see cref="JsonTokenType.String"/>, <see cref="JsonTokenType.PropertyName"/> or
        /// <see cref="JsonTokenType.Null"/>).
        /// <seealso cref="TokenType" />
        /// It will also throw when the JSON string contains invalid UTF-8 bytes, or invalid UTF-16 surrogates.
        /// </exception>
        public string? GetString()
        {
            if (TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (TokenType != JsonTokenType.String && TokenType != JsonTokenType.PropertyName)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedString(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;

            if (ValueIsEscaped)
            {
                return JsonReaderHelper.GetUnescapedString(span);
            }

            Debug.Assert(span.IndexOf(JsonConstants.BackSlash) == -1);
            return JsonReaderHelper.TranscodeHelper(span);
        }

        /// <summary>
        /// Copies the current JSON token value from the source, unescaped as a UTF-8 string to the destination buffer.
        /// </summary>
        /// <param name="utf8Destination">A buffer to write the unescaped UTF-8 bytes into.</param>
        /// <returns>The number of bytes written to <paramref name="utf8Destination"/>.</returns>
        /// <remarks>
        /// Unlike <see cref="GetString"/>, this method does not support <see cref="JsonTokenType.Null"/>.
        ///
        /// This method will throw <see cref="ArgumentException"/> if the destination buffer is too small to hold the unescaped value.
        /// An appropriately sized buffer can be determined by consulting the length of either <see cref="ValueSpan"/> or <see cref="ValueSequence"/>,
        /// since the unescaped result is always less than or equal to the length of the encoded strings.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of the JSON token that is not a string
        /// (i.e. other than <see cref="JsonTokenType.String"/> or <see cref="JsonTokenType.PropertyName"/>.
        /// <seealso cref="TokenType" />
        /// It will also throw when the JSON string contains invalid UTF-8 bytes, or invalid UTF-16 surrogates.
        /// </exception>
        /// <exception cref="ArgumentException">The destination buffer is too small to hold the unescaped value.</exception>
        public readonly int CopyString(Span<byte> utf8Destination)
        {
            if (_tokenType is not (JsonTokenType.String or JsonTokenType.PropertyName))
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedString(_tokenType);
            }

            int bytesWritten;

            if (ValueIsEscaped)
            {
                if (!TryCopyEscapedString(utf8Destination, out bytesWritten))
                {
                    utf8Destination.Slice(0, bytesWritten).Clear();
                    ThrowHelper.ThrowArgumentException_DestinationTooShort();
                }
            }
            else
            {
                if (HasValueSequence)
                {
                    ReadOnlySequence<byte> valueSequence = ValueSequence;
                    valueSequence.CopyTo(utf8Destination);
                    bytesWritten = (int)valueSequence.Length;
                }
                else
                {
                    ReadOnlySpan<byte> valueSpan = ValueSpan;
                    valueSpan.CopyTo(utf8Destination);
                    bytesWritten = valueSpan.Length;
                }
            }

            JsonReaderHelper.ValidateUtf8(utf8Destination.Slice(0, bytesWritten));
            return bytesWritten;
        }

        /// <summary>
        /// Copies the current JSON token value from the source, unescaped, and transcoded as a UTF-16 char buffer.
        /// </summary>
        /// <param name="destination">A buffer to write the transcoded UTF-16 characters into.</param>
        /// <returns>The number of characters written to <paramref name="destination"/>.</returns>
        /// <remarks>
        /// Unlike <see cref="GetString"/>, this method does not support <see cref="JsonTokenType.Null"/>.
        ///
        /// This method will throw <see cref="ArgumentException"/> if the destination buffer is too small to hold the unescaped value.
        /// An appropriately sized buffer can be determined by consulting the length of either <see cref="ValueSpan"/> or <see cref="ValueSequence"/>,
        /// since the unescaped result is always less than or equal to the length of the encoded strings.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of the JSON token that is not a string
        /// (i.e. other than <see cref="JsonTokenType.String"/> or <see cref="JsonTokenType.PropertyName"/>.
        /// <seealso cref="TokenType" />
        /// It will also throw when the JSON string contains invalid UTF-8 bytes, or invalid UTF-16 surrogates.
        /// </exception>
        /// <exception cref="ArgumentException">The destination buffer is too small to hold the unescaped value.</exception>
        public readonly int CopyString(Span<char> destination)
        {
            if (_tokenType is not (JsonTokenType.String or JsonTokenType.PropertyName))
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedString(_tokenType);
            }

            ReadOnlySpan<byte> unescapedSource = stackalloc byte[0];
            byte[]? rentedBuffer = null;
            int valueLength;

            if (ValueIsEscaped)
            {
                valueLength = ValueLength;

                Span<byte> unescapedBuffer = valueLength <= JsonConstants.StackallocByteThreshold ?
                    stackalloc byte[JsonConstants.StackallocByteThreshold] :
                    (rentedBuffer = ArrayPool<byte>.Shared.Rent(valueLength));

                bool success = TryCopyEscapedString(unescapedBuffer, out int bytesWritten);
                Debug.Assert(success);
                unescapedSource = unescapedBuffer.Slice(0, bytesWritten);
            }
            else
            {
                if (HasValueSequence)
                {
                    ReadOnlySequence<byte> valueSequence = ValueSequence;
                    valueLength = checked((int)valueSequence.Length);

                    Span<byte> intermediate = valueLength <= JsonConstants.StackallocByteThreshold ?
                        stackalloc byte[JsonConstants.StackallocByteThreshold] :
                        (rentedBuffer = ArrayPool<byte>.Shared.Rent(valueLength));

                    valueSequence.CopyTo(intermediate);
                    unescapedSource = intermediate.Slice(0, valueLength);
                }
                else
                {
                    unescapedSource = ValueSpan;
                }
            }

            int charsWritten = JsonReaderHelper.TranscodeHelper(unescapedSource, destination);

            if (rentedBuffer != null)
            {
                new Span<byte>(rentedBuffer, 0, unescapedSource.Length).Clear();
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }

            return charsWritten;
        }

        private readonly bool TryCopyEscapedString(Span<byte> destination, out int bytesWritten)
        {
            Debug.Assert(_tokenType is JsonTokenType.String or JsonTokenType.PropertyName);
            Debug.Assert(ValueIsEscaped);

            byte[]? rentedBuffer = null;
            ReadOnlySpan<byte> source = stackalloc byte[0];

            if (HasValueSequence)
            {
                ReadOnlySequence<byte> valueSequence = ValueSequence;
                int sequenceLength = checked((int)valueSequence.Length);

                Span<byte> intermediate = sequenceLength <= JsonConstants.StackallocByteThreshold ?
                    stackalloc byte[JsonConstants.StackallocByteThreshold] :
                    (rentedBuffer = ArrayPool<byte>.Shared.Rent(sequenceLength));

                valueSequence.CopyTo(intermediate);
                source = intermediate.Slice(0, sequenceLength);
            }
            else
            {
                source = ValueSpan;
            }

            bool success = JsonReaderHelper.TryUnescape(source, destination, out bytesWritten);

            if (rentedBuffer != null)
            {
                new Span<byte>(rentedBuffer, 0, source.Length).Clear();
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }

            Debug.Assert(bytesWritten < source.Length, "source buffer must contain at least one escape sequence");
            return success;
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
                ThrowHelper.ThrowInvalidOperationException_ExpectedComment(TokenType);
            }
            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return JsonReaderHelper.TranscodeHelper(span);
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="bool"/>.
        /// Returns <see langword="true"/> if the TokenType is JsonTokenType.True and <see langword="false"/> if the TokenType is JsonTokenType.False.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a boolean (i.e. <see cref="JsonTokenType.True"/> or <see cref="JsonTokenType.False"/>).
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool GetBoolean()
        {
            JsonTokenType type = TokenType;
            if (type == JsonTokenType.True)
            {
                Debug.Assert((HasValueSequence ? ValueSequence.ToArray() : ValueSpan).Length == 4);
                return true;
            }
            else if (type != JsonTokenType.False)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedBoolean(TokenType);
                Debug.Fail("Throw helper should have thrown an exception.");
            }

            Debug.Assert((HasValueSequence ? ValueSequence.ToArray() : ValueSpan).Length == 5);
            return false;
        }

        /// <summary>
        /// Parses the current JSON token value from the source and decodes the Base64 encoded JSON string as bytes.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.String"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        /// <exception cref="FormatException">
        /// The JSON string contains data outside of the expected Base64 range, or if it contains invalid/more than two padding characters,
        /// or is incomplete (i.e. the JSON string length is not a multiple of 4).
        /// </exception>
        public byte[] GetBytesFromBase64()
        {
            if (!TryGetBytesFromBase64(out byte[]? value))
            {
                ThrowHelper.ThrowFormatException(DataType.Base64String);
            }
            return value;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="byte"/>.
        /// Returns the value if the entire UTF-8 encoded token value can be successfully parsed to a <see cref="byte"/>
        /// value.
        /// Throws exceptions otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the JSON token value is either of incorrect numeric format (for example if it contains a decimal or
        /// is written in scientific notation) or, it represents a number less than <see cref="byte.MinValue"/> or greater
        /// than <see cref="byte.MaxValue"/>.
        /// </exception>
        public byte GetByte()
        {
            if (!TryGetByte(out byte value))
            {
                ThrowHelper.ThrowFormatException(NumericType.Byte);
            }
            return value;
        }

        internal byte GetByteWithQuotes()
        {
            ReadOnlySpan<byte> span = GetUnescapedSpan();
            if (!TryGetByteCore(out byte value, span))
            {
                ThrowHelper.ThrowFormatException(NumericType.Byte);
            }
            return value;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as an <see cref="sbyte"/>.
        /// Returns the value if the entire UTF-8 encoded token value can be successfully parsed to an <see cref="sbyte"/>
        /// value.
        /// Throws exceptions otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the JSON token value is either of incorrect numeric format (for example if it contains a decimal or
        /// is written in scientific notation) or, it represents a number less than <see cref="sbyte.MinValue"/> or greater
        /// than <see cref="sbyte.MaxValue"/>.
        /// </exception>
        [System.CLSCompliantAttribute(false)]
        public sbyte GetSByte()
        {
            if (!TryGetSByte(out sbyte value))
            {
                ThrowHelper.ThrowFormatException(NumericType.SByte);
            }
            return value;
        }

        internal sbyte GetSByteWithQuotes()
        {
            ReadOnlySpan<byte> span = GetUnescapedSpan();
            if (!TryGetSByteCore(out sbyte value, span))
            {
                ThrowHelper.ThrowFormatException(NumericType.SByte);
            }
            return value;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="short"/>.
        /// Returns the value if the entire UTF-8 encoded token value can be successfully parsed to a <see cref="short"/>
        /// value.
        /// Throws exceptions otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the JSON token value is either of incorrect numeric format (for example if it contains a decimal or
        /// is written in scientific notation) or, it represents a number less than <see cref="short.MinValue"/> or greater
        /// than <see cref="short.MaxValue"/>.
        /// </exception>
        public short GetInt16()
        {
            if (!TryGetInt16(out short value))
            {
                ThrowHelper.ThrowFormatException(NumericType.Int16);
            }
            return value;
        }

        internal short GetInt16WithQuotes()
        {
            ReadOnlySpan<byte> span = GetUnescapedSpan();
            if (!TryGetInt16Core(out short value, span))
            {
                ThrowHelper.ThrowFormatException(NumericType.Int16);
            }
            return value;
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
                ThrowHelper.ThrowFormatException(NumericType.Int32);
            }
            return value;
        }

        internal int GetInt32WithQuotes()
        {
            ReadOnlySpan<byte> span = GetUnescapedSpan();
            if (!TryGetInt32Core(out int value, span))
            {
                ThrowHelper.ThrowFormatException(NumericType.Int32);
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
                ThrowHelper.ThrowFormatException(NumericType.Int64);
            }
            return value;
        }

        internal long GetInt64WithQuotes()
        {
            ReadOnlySpan<byte> span = GetUnescapedSpan();
            if (!TryGetInt64Core(out long value, span))
            {
                ThrowHelper.ThrowFormatException(NumericType.Int64);
            }
            return value;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="ushort"/>.
        /// Returns the value if the entire UTF-8 encoded token value can be successfully parsed to a <see cref="ushort"/>
        /// value.
        /// Throws exceptions otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown if the JSON token value is either of incorrect numeric format (for example if it contains a decimal or
        /// is written in scientific notation) or, it represents a number less than <see cref="ushort.MinValue"/> or greater
        /// than <see cref="ushort.MaxValue"/>.
        /// </exception>
        [System.CLSCompliantAttribute(false)]
        public ushort GetUInt16()
        {
            if (!TryGetUInt16(out ushort value))
            {
                ThrowHelper.ThrowFormatException(NumericType.UInt16);
            }
            return value;
        }

        internal ushort GetUInt16WithQuotes()
        {
            ReadOnlySpan<byte> span = GetUnescapedSpan();
            if (!TryGetUInt16Core(out ushort value, span))
            {
                ThrowHelper.ThrowFormatException(NumericType.UInt16);
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
                ThrowHelper.ThrowFormatException(NumericType.UInt32);
            }
            return value;
        }

        internal uint GetUInt32WithQuotes()
        {
            ReadOnlySpan<byte> span = GetUnescapedSpan();
            if (!TryGetUInt32Core(out uint value, span))
            {
                ThrowHelper.ThrowFormatException(NumericType.UInt32);
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
                ThrowHelper.ThrowFormatException(NumericType.UInt64);
            }
            return value;
        }

        internal ulong GetUInt64WithQuotes()
        {
            ReadOnlySpan<byte> span = GetUnescapedSpan();
            if (!TryGetUInt64Core(out ulong value, span))
            {
                ThrowHelper.ThrowFormatException(NumericType.UInt64);
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
        /// On any framework that is not .NET Core 3.0 or higher, thrown if the JSON token value represents a number less than <see cref="float.MinValue"/> or greater
        /// than <see cref="float.MaxValue"/>.
        /// </exception>
        public float GetSingle()
        {
            if (!TryGetSingle(out float value))
            {
                ThrowHelper.ThrowFormatException(NumericType.Single);
            }
            return value;
        }

        internal float GetSingleWithQuotes()
        {
            ReadOnlySpan<byte> span = GetUnescapedSpan();

            if (JsonReaderHelper.TryGetFloatingPointConstant(span, out float value))
            {
                return value;
            }

            // NETCOREAPP implementation of the TryParse method above permits case-insensitive variants of the
            // float constants "NaN", "Infinity", "-Infinity". This differs from the NETFRAMEWORK implementation.
            // The following logic reconciles the two implementations to enforce consistent behavior.
            if (!(Utf8Parser.TryParse(span, out value, out int bytesConsumed)
                  && span.Length == bytesConsumed
                  && JsonHelpers.IsFinite(value)))
            {
                ThrowHelper.ThrowFormatException(NumericType.Single);
            }

            return value;
        }

        internal float GetSingleFloatingPointConstant()
        {
            ReadOnlySpan<byte> span = GetUnescapedSpan();

            if (!JsonReaderHelper.TryGetFloatingPointConstant(span, out float value))
            {
                ThrowHelper.ThrowFormatException(NumericType.Single);
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
        /// On any framework that is not .NET Core 3.0 or higher, thrown if the JSON token value represents a number less than <see cref="double.MinValue"/> or greater
        /// than <see cref="double.MaxValue"/>.
        /// </exception>
        public double GetDouble()
        {
            if (!TryGetDouble(out double value))
            {
                ThrowHelper.ThrowFormatException(NumericType.Double);
            }
            return value;
        }

        internal double GetDoubleWithQuotes()
        {
            ReadOnlySpan<byte> span = GetUnescapedSpan();

            if (JsonReaderHelper.TryGetFloatingPointConstant(span, out double value))
            {
                return value;
            }

            // NETCOREAPP implementation of the TryParse method above permits case-insensitive variants of the
            // float constants "NaN", "Infinity", "-Infinity". This differs from the NETFRAMEWORK implementation.
            // The following logic reconciles the two implementations to enforce consistent behavior.
            if (!(Utf8Parser.TryParse(span, out value, out int bytesConsumed)
                  && span.Length == bytesConsumed
                  && JsonHelpers.IsFinite(value)))
            {
                ThrowHelper.ThrowFormatException(NumericType.Double);
            }

            return value;
        }

        internal double GetDoubleFloatingPointConstant()
        {
            ReadOnlySpan<byte> span = GetUnescapedSpan();

            if (!JsonReaderHelper.TryGetFloatingPointConstant(span, out double value))
            {
                ThrowHelper.ThrowFormatException(NumericType.Double);
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
                ThrowHelper.ThrowFormatException(NumericType.Decimal);
            }
            return value;
        }

        internal decimal GetDecimalWithQuotes()
        {
            ReadOnlySpan<byte> span = GetUnescapedSpan();
            if (!TryGetDecimalCore(out decimal value, span))
            {
                ThrowHelper.ThrowFormatException(NumericType.Decimal);
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
                ThrowHelper.ThrowFormatException(DataType.DateTime);
            }

            return value;
        }

        internal DateTime GetDateTimeNoValidation()
        {
            if (!TryGetDateTimeCore(out DateTime value))
            {
                ThrowHelper.ThrowFormatException(DataType.DateTime);
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
                ThrowHelper.ThrowFormatException(DataType.DateTimeOffset);
            }

            return value;
        }

        internal DateTimeOffset GetDateTimeOffsetNoValidation()
        {
            if (!TryGetDateTimeOffsetCore(out DateTimeOffset value))
            {
                ThrowHelper.ThrowFormatException(DataType.DateTimeOffset);
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
                ThrowHelper.ThrowFormatException(DataType.Guid);
            }

            return value;
        }

        internal Guid GetGuidNoValidation()
        {
            if (!TryGetGuidCore(out Guid value))
            {
                ThrowHelper.ThrowFormatException(DataType.Guid);
            }

            return value;
        }

        /// <summary>
        /// Parses the current JSON token value from the source and decodes the Base64 encoded JSON string as bytes.
        /// Returns <see langword="true"/> if the entire token value is encoded as valid Base64 text and can be successfully
        /// decoded to bytes.
        /// Returns <see langword="false"/> otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.String"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetBytesFromBase64([NotNullWhen(true)] out byte[]? value)
        {
            if (TokenType != JsonTokenType.String)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedString(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;

            if (ValueIsEscaped)
            {
                return JsonReaderHelper.TryGetUnescapedBase64Bytes(span, out value);
            }

            Debug.Assert(span.IndexOf(JsonConstants.BackSlash) == -1);
            return JsonReaderHelper.TryDecodeBase64(span, out value);
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="byte"/>.
        /// Returns <see langword="true"/> if the entire UTF-8 encoded token value can be successfully
        /// parsed to a <see cref="byte"/> value.
        /// Returns <see langword="false"/> otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetByte(out byte value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return TryGetByteCore(out value, span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryGetByteCore(out byte value, ReadOnlySpan<byte> span)
        {
            if (Utf8Parser.TryParse(span, out byte tmp, out int bytesConsumed)
                && span.Length == bytesConsumed)
            {
                value = tmp;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as an <see cref="sbyte"/>.
        /// Returns <see langword="true"/> if the entire UTF-8 encoded token value can be successfully
        /// parsed to an <see cref="sbyte"/> value.
        /// Returns <see langword="false"/> otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        [System.CLSCompliantAttribute(false)]
        public bool TryGetSByte(out sbyte value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return TryGetSByteCore(out value, span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryGetSByteCore(out sbyte value, ReadOnlySpan<byte> span)
        {
            if (Utf8Parser.TryParse(span, out sbyte tmp, out int bytesConsumed)
                && span.Length == bytesConsumed)
            {
                value = tmp;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="short"/>.
        /// Returns <see langword="true"/> if the entire UTF-8 encoded token value can be successfully
        /// parsed to a <see cref="short"/> value.
        /// Returns <see langword="false"/> otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetInt16(out short value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return TryGetInt16Core(out value, span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryGetInt16Core(out short value, ReadOnlySpan<byte> span)
        {
            if (Utf8Parser.TryParse(span, out short tmp, out int bytesConsumed)
                && span.Length == bytesConsumed)
            {
                value = tmp;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as an <see cref="int"/>.
        /// Returns <see langword="true"/> if the entire UTF-8 encoded token value can be successfully
        /// parsed to an <see cref="int"/> value.
        /// Returns <see langword="false"/> otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetInt32(out int value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return TryGetInt32Core(out value, span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryGetInt32Core(out int value, ReadOnlySpan<byte> span)
        {
            if (Utf8Parser.TryParse(span, out int tmp, out int bytesConsumed)
                && span.Length == bytesConsumed)
            {
                value = tmp;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="long"/>.
        /// Returns <see langword="true"/> if the entire UTF-8 encoded token value can be successfully
        /// parsed to a <see cref="long"/> value.
        /// Returns <see langword="false"/> otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetInt64(out long value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return TryGetInt64Core(out value, span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryGetInt64Core(out long value, ReadOnlySpan<byte> span)
        {
            if (Utf8Parser.TryParse(span, out long tmp, out int bytesConsumed)
                && span.Length == bytesConsumed)
            {
                value = tmp;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="ushort"/>.
        /// Returns <see langword="true"/> if the entire UTF-8 encoded token value can be successfully
        /// parsed to a <see cref="ushort"/> value.
        /// Returns <see langword="false"/> otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        [System.CLSCompliantAttribute(false)]
        public bool TryGetUInt16(out ushort value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return TryGetUInt16Core(out value, span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryGetUInt16Core(out ushort value, ReadOnlySpan<byte> span)
        {
            if (Utf8Parser.TryParse(span, out ushort tmp, out int bytesConsumed)
                && span.Length == bytesConsumed)
            {
                value = tmp;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="uint"/>.
        /// Returns <see langword="true"/> if the entire UTF-8 encoded token value can be successfully
        /// parsed to a <see cref="uint"/> value.
        /// Returns <see langword="false"/> otherwise.
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
                ThrowHelper.ThrowInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return TryGetUInt32Core(out value, span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryGetUInt32Core(out uint value, ReadOnlySpan<byte> span)
        {
            if (Utf8Parser.TryParse(span, out uint tmp, out int bytesConsumed)
                && span.Length == bytesConsumed)
            {
                value = tmp;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="ulong"/>.
        /// Returns <see langword="true"/> if the entire UTF-8 encoded token value can be successfully
        /// parsed to a <see cref="ulong"/> value.
        /// Returns <see langword="false"/> otherwise.
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
                ThrowHelper.ThrowInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return TryGetUInt64Core(out value, span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryGetUInt64Core(out ulong value, ReadOnlySpan<byte> span)
        {
            if (Utf8Parser.TryParse(span, out ulong tmp, out int bytesConsumed)
                && span.Length == bytesConsumed)
            {
                value = tmp;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="float"/>.
        /// Returns <see langword="true"/> if the entire UTF-8 encoded token value can be successfully
        /// parsed to a <see cref="float"/> value.
        /// Returns <see langword="false"/> otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetSingle(out float value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;

            if (Utf8Parser.TryParse(span, out float tmp, out int bytesConsumed)
                && span.Length == bytesConsumed)
            {
                value = tmp;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="double"/>.
        /// Returns <see langword="true"/> if the entire UTF-8 encoded token value can be successfully
        /// parsed to a <see cref="double"/> value.
        /// Returns <see langword="false"/> otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetDouble(out double value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;

            if (Utf8Parser.TryParse(span, out double tmp, out int bytesConsumed)
                && span.Length == bytesConsumed)
            {
                value = tmp;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="decimal"/>.
        /// Returns <see langword="true"/> if the entire UTF-8 encoded token value can be successfully
        /// parsed to a <see cref="decimal"/> value.
        /// Returns <see langword="false"/> otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.Number"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetDecimal(out decimal value)
        {
            if (TokenType != JsonTokenType.Number)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedNumber(TokenType);
            }

            ReadOnlySpan<byte> span = HasValueSequence ? ValueSequence.ToArray() : ValueSpan;
            return TryGetDecimalCore(out value, span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryGetDecimalCore(out decimal value, ReadOnlySpan<byte> span)
        {
            if (Utf8Parser.TryParse(span, out decimal tmp, out int bytesConsumed)
                && span.Length == bytesConsumed)
            {
                value = tmp;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="DateTime"/>.
        /// Returns <see langword="true"/> if the entire UTF-8 encoded token value can be successfully
        /// parsed to a <see cref="DateTime"/> value.
        /// Returns <see langword="false"/> otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.String"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetDateTime(out DateTime value)
        {
            if (TokenType != JsonTokenType.String)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedString(TokenType);
            }

            return TryGetDateTimeCore(out value);
        }

        internal bool TryGetDateTimeCore(out DateTime value)
        {
            ReadOnlySpan<byte> span = stackalloc byte[0];

            if (HasValueSequence)
            {
                long sequenceLength = ValueSequence.Length;
                if (!JsonHelpers.IsInRangeInclusive(sequenceLength, JsonConstants.MinimumDateTimeParseLength, JsonConstants.MaximumEscapedDateTimeOffsetParseLength))
                {
                    value = default;
                    return false;
                }

                Span<byte> stackSpan = stackalloc byte[JsonConstants.MaximumEscapedDateTimeOffsetParseLength];
                ValueSequence.CopyTo(stackSpan);
                span = stackSpan.Slice(0, (int)sequenceLength);
            }
            else
            {
                if (!JsonHelpers.IsInRangeInclusive(ValueSpan.Length, JsonConstants.MinimumDateTimeParseLength, JsonConstants.MaximumEscapedDateTimeOffsetParseLength))
                {
                    value = default;
                    return false;
                }

                span = ValueSpan;
            }

            if (ValueIsEscaped)
            {
                return JsonReaderHelper.TryGetEscapedDateTime(span, out value);
            }

            Debug.Assert(span.IndexOf(JsonConstants.BackSlash) == -1);

            if (JsonHelpers.TryParseAsISO(span, out DateTime tmp))
            {
                value = tmp;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Parses the current JSON token value from the source as a <see cref="DateTimeOffset"/>.
        /// Returns <see langword="true"/> if the entire UTF-8 encoded token value can be successfully
        /// parsed to a <see cref="DateTimeOffset"/> value.
        /// Returns <see langword="false"/> otherwise.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if trying to get the value of a JSON token that is not a <see cref="JsonTokenType.String"/>.
        /// <seealso cref="TokenType" />
        /// </exception>
        public bool TryGetDateTimeOffset(out DateTimeOffset value)
        {
            if (TokenType != JsonTokenType.String)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedString(TokenType);
            }

            return TryGetDateTimeOffsetCore(out value);
        }

        internal bool TryGetDateTimeOffsetCore(out DateTimeOffset value)
        {
            ReadOnlySpan<byte> span = stackalloc byte[0];

            if (HasValueSequence)
            {
                long sequenceLength = ValueSequence.Length;
                if (!JsonHelpers.IsInRangeInclusive(sequenceLength, JsonConstants.MinimumDateTimeParseLength, JsonConstants.MaximumEscapedDateTimeOffsetParseLength))
                {
                    value = default;
                    return false;
                }

                Span<byte> stackSpan = stackalloc byte[JsonConstants.MaximumEscapedDateTimeOffsetParseLength];
                ValueSequence.CopyTo(stackSpan);
                span = stackSpan.Slice(0, (int)sequenceLength);
            }
            else
            {
                if (!JsonHelpers.IsInRangeInclusive(ValueSpan.Length, JsonConstants.MinimumDateTimeParseLength, JsonConstants.MaximumEscapedDateTimeOffsetParseLength))
                {
                    value = default;
                    return false;
                }

                span = ValueSpan;
            }

            if (ValueIsEscaped)
            {
                return JsonReaderHelper.TryGetEscapedDateTimeOffset(span, out value);
            }

            Debug.Assert(span.IndexOf(JsonConstants.BackSlash) == -1);

            if (JsonHelpers.TryParseAsISO(span, out DateTimeOffset tmp))
            {
                value = tmp;
                return true;
            }

            value = default;
            return false;
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
                ThrowHelper.ThrowInvalidOperationException_ExpectedString(TokenType);
            }

            return TryGetGuidCore(out value);
        }

        internal bool TryGetGuidCore(out Guid value)
        {
            ReadOnlySpan<byte> span = stackalloc byte[0];

            if (HasValueSequence)
            {
                long sequenceLength = ValueSequence.Length;
                if (sequenceLength > JsonConstants.MaximumEscapedGuidLength)
                {
                    value = default;
                    return false;
                }

                Span<byte> stackSpan = stackalloc byte[JsonConstants.MaximumEscapedGuidLength];
                ValueSequence.CopyTo(stackSpan);
                span = stackSpan.Slice(0, (int)sequenceLength);
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

            if (ValueIsEscaped)
            {
                return JsonReaderHelper.TryGetEscapedGuid(span, out value);
            }

            Debug.Assert(span.IndexOf(JsonConstants.BackSlash) == -1);

            if (span.Length == JsonConstants.MaximumFormatGuidLength
                && Utf8Parser.TryParse(span, out Guid tmp, out _, 'D'))
            {
                value = tmp;
                return true;
            }

            value = default;
            return false;
        }
    }
}
