// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class VersionConverter : JsonConverter<Version>
    {
#if BUILDING_INBOX_LIBRARY
        private const int MinimumVersionLength = 3; // 0.0

        private const int MaximumVersionLength = 43; // 2147483647.2147483647.2147483647.2147483647

        private const int MaximumEscapedVersionLength = JsonConstants.MaxExpansionFactorWhileEscaping * MaximumVersionLength;
#endif

        public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw ThrowHelper.GetInvalidOperationException_ExpectedString(reader.TokenType);
            }

#if BUILDING_INBOX_LIBRARY
            bool isEscaped = reader._stringHasEscaping;

            int maxLength = isEscaped ? MaximumEscapedVersionLength : MaximumVersionLength;
            ReadOnlySpan<byte> source = stackalloc byte[0];
            if (reader.HasValueSequence)
            {
                if (!JsonHelpers.IsInRangeInclusive(reader.ValueSequence.Length, MinimumVersionLength, maxLength))
                {
                    throw ThrowHelper.GetFormatException(DataType.Version);
                }

                Span<byte> stackSpan = stackalloc byte[isEscaped ? MaximumEscapedVersionLength : MaximumVersionLength];
                reader.ValueSequence.CopyTo(stackSpan);
                source = stackSpan.Slice(0, (int)reader.ValueSequence.Length);
            }
            else
            {
                source = reader.ValueSpan;

                if (!JsonHelpers.IsInRangeInclusive(source.Length, MinimumVersionLength, maxLength))
                {
                    throw ThrowHelper.GetFormatException(DataType.Version);
                }
            }

            if (isEscaped)
            {
                int backslash = source.IndexOf(JsonConstants.BackSlash);
                Debug.Assert(backslash != -1);

                Span<byte> sourceUnescaped = stackalloc byte[MaximumEscapedVersionLength];

                JsonReaderHelper.Unescape(source, sourceUnescaped, backslash, out int written);
                Debug.Assert(written > 0);

                source = sourceUnescaped.Slice(0, written);
                Debug.Assert(!source.IsEmpty);
            }

            byte firstChar = source[0];
            byte lastChar = source[source.Length - 1];
            if (!JsonHelpers.IsDigit(firstChar) || !JsonHelpers.IsDigit(lastChar))
            {
                // Since leading and trailing whitespaces are forbidden throughout System.Text.Json converters
                // we need to make sure that our input doesn't have them,
                // and if it has - we need to throw, to match behaviour of other converters
                // since Version.TryParse allows them and silently parses input to Version
                throw ThrowHelper.GetFormatException(DataType.Version);
            }

            Span<char> charBuffer = stackalloc char[MaximumVersionLength];
            int writtenChars = JsonReaderHelper.s_utf8Encoding.GetChars(source, charBuffer);
            if (Version.TryParse(charBuffer.Slice(0, writtenChars), out Version? result))
            {
                return result;
            }
#else
            string? versionString = reader.GetString();
            if (!string.IsNullOrEmpty(versionString) && (!char.IsDigit(versionString[0]) || !char.IsDigit(versionString[versionString.Length - 1])))
            {
                // Since leading and trailing whitespaces are forbidden throughout System.Text.Json converters
                // we need to make sure that our input doesn't have them,
                // and if it has - we need to throw, to match behaviour of other converters
                // since Version.TryParse allows them and silently parses input to Version
                throw ThrowHelper.GetFormatException(DataType.Version);
            }
            if (Version.TryParse(versionString, out Version? result))
            {
                return result;
            }
#endif
            ThrowHelper.ThrowJsonException();
            return null;
        }

        public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
        {
#if BUILDING_INBOX_LIBRARY
            Span<char> span = stackalloc char[MaximumVersionLength];
            bool formattedSuccessfully = value.TryFormat(span, out int charsWritten);
            Debug.Assert(formattedSuccessfully && charsWritten >= MinimumVersionLength);
            writer.WriteStringValue(span.Slice(0, charsWritten));
#else
            writer.WriteStringValue(value.ToString());
#endif
        }
    }
}
