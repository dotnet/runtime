// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;

namespace System.Text.Json
{
    /// <summary>
    /// Provides a way to transform UTF-8 or UTF-16 encoded text into a form that is suitable for JSON.
    /// </summary>
    /// <remarks>
    /// This can be used to cache and store known strings used for writing JSON ahead of time by pre-encoding them up front.
    /// </remarks>
    public readonly struct JsonEncodedText : IEquatable<JsonEncodedText>
    {
        private readonly byte[] _utf8Value;
        private readonly string _value;

        /// <summary>
        /// Returns the UTF-8 encoded representation of the pre-encoded JSON text.
        /// </summary>
        public ReadOnlySpan<byte> EncodedUtf8Bytes => _utf8Value;

        private JsonEncodedText(byte[] utf8Value)
        {
            Debug.Assert(utf8Value != null);

            _value = JsonReaderHelper.GetTextFromUtf8(utf8Value);
            _utf8Value = utf8Value;
        }

        /// <summary>
        /// Encodes the string text value as a JSON string.
        /// </summary>
        /// <param name="value">The value to be transformed as JSON encoded text.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if value is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large or if it contains invalid UTF-16 characters.
        /// </exception>
        public static JsonEncodedText Encode(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return Encode(value.AsSpan());
        }

        /// <summary>
        /// Encodes the text value as a JSON string.
        /// </summary>
        /// <param name="value">The value to be transformed as JSON encoded text.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large or if it contains invalid UTF-16 characters.
        /// </exception>
        public static JsonEncodedText Encode(ReadOnlySpan<char> value)
        {
            if (value.Length == 0)
            {
                return new JsonEncodedText(Array.Empty<byte>());
            }

            return TranscodeAndEncode(value);
        }

        private static JsonEncodedText TranscodeAndEncode(ReadOnlySpan<char> value)
        {
            JsonWriterHelper.ValidateValue(value);

            int expectedByteCount = JsonReaderHelper.GetUtf8ByteCount(value);
            byte[] utf8Bytes = ArrayPool<byte>.Shared.Rent(expectedByteCount);

            JsonEncodedText encodedText;

            // Since GetUtf8ByteCount above already throws on invalid input, the transcoding
            // to UTF-8 is guaranteed to succeed here. Therefore, there's no need for a try-catch-finally block.
            int actualByteCount = JsonReaderHelper.GetUtf8FromText(value, utf8Bytes);
            Debug.Assert(expectedByteCount == actualByteCount);

            encodedText = EncodeHelper(utf8Bytes.AsSpan(0, actualByteCount));

            // On the basis that this is user data, go ahead and clear it.
            utf8Bytes.AsSpan(0, expectedByteCount).Clear();
            ArrayPool<byte>.Shared.Return(utf8Bytes);

            return encodedText;
        }

        /// <summary>
        /// Encodes the UTF-8 text value as a JSON string.
        /// </summary>
        /// <param name="utf8Value">The UTF-8 encoded value to be transformed as JSON encoded text.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the specified value is too large or if it contains invalid UTF-8 bytes.
        /// </exception>
        public static JsonEncodedText Encode(ReadOnlySpan<byte> utf8Value)
        {
            if (utf8Value.Length == 0)
            {
                return new JsonEncodedText(Array.Empty<byte>());
            }

            JsonWriterHelper.ValidateValue(utf8Value);
            return EncodeHelper(utf8Value);
        }

        private static JsonEncodedText EncodeHelper(ReadOnlySpan<byte> utf8Value)
        {
            int idx = JsonWriterHelper.NeedsEscaping(utf8Value);

            if (idx != -1)
            {
                return new JsonEncodedText(GetEscapedString(utf8Value, idx));
            }
            else
            {
                return new JsonEncodedText(utf8Value.ToArray());
            }
        }

        private static byte[] GetEscapedString(ReadOnlySpan<byte> utf8Value, int firstEscapeIndexVal)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8Value.Length);
            Debug.Assert(firstEscapeIndexVal >= 0 && firstEscapeIndexVal < utf8Value.Length);

            byte[] valueArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndexVal);

            Span<byte> escapedValue = length <= JsonConstants.StackallocThreshold ?
                stackalloc byte[length] :
                (valueArray = ArrayPool<byte>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(utf8Value, escapedValue, firstEscapeIndexVal, out int written);

            byte[] escapedString = escapedValue.Slice(0, written).ToArray();

            if (valueArray != null)
            {
                ArrayPool<byte>.Shared.Return(valueArray);
            }

            return escapedString;
        }

        /// <summary>
        /// Determines whether this instance and another specified <see cref="JsonEncodedText"/> instance have the same value.
        /// </summary>
        /// <remarks>
        /// Default instances of <see cref="JsonEncodedText"/> are treated as equal.
        /// </remarks>
        public bool Equals(JsonEncodedText other)
        {
            if (_value == null)
            {
                return other._value == null;
            }
            else
            {
                return _value.Equals(other._value);
            }
        }

        /// <summary>
        /// Determines whether this instance and a specified object, which must also be a <see cref="JsonEncodedText"/> instance, have the same value.
        /// </summary>
        /// <remarks>
        /// If <paramref name="obj"/> is null, the method returns false.
        /// </remarks>
        public override bool Equals(object obj)
        {
            if (obj is JsonEncodedText encodedText)
            {
                return Equals(encodedText);
            }
            return false;
        }

        /// <summary>
        /// Converts the value of this instance to a <see cref="string"/>.
        /// </summary>
        /// <remarks>
        /// Returns the underlying UTF-16 encoded string.
        /// </remarks>
        /// <remarks>
        /// Returns an empty string on a default instance of <see cref="JsonEncodedText"/>.
        /// </remarks>
        public override string ToString()
            => _value ?? string.Empty;

        /// <summary>
        /// Returns the hash code for this <see cref="JsonEncodedText"/>.
        /// </summary>
        /// <remarks>
        /// Returns 0 on a default instance of <see cref="JsonEncodedText"/>.
        /// </remarks>
        public override int GetHashCode()
            => _value == null ? 0 : _value.GetHashCode();
    }
}
