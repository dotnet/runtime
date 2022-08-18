// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class VersionConverter : JsonConverter<Version>
    {
#if NETCOREAPP
        private const int MinimumVersionLength = 3; // 0.0

        private const int MaximumVersionLength = 43; // 2147483647.2147483647.2147483647.2147483647

        private const int MaximumEscapedVersionLength = JsonConstants.MaxExpansionFactorWhileEscaping * MaximumVersionLength;
#endif

        public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedString(reader.TokenType);
            }

#if NETCOREAPP
            if (!JsonHelpers.IsInRangeInclusive(reader.ValueLength, MinimumVersionLength, MaximumEscapedVersionLength))
            {
                ThrowHelper.ThrowFormatException(DataType.TimeSpan);
            }

            Span<char> charBuffer = stackalloc char[MaximumEscapedVersionLength];
            int bytesWritten = reader.CopyString(charBuffer);
            ReadOnlySpan<char> source = charBuffer.Slice(0, bytesWritten);

            if (!char.IsDigit(source[0]) || !char.IsDigit(source[^1]))
            {
                // Since leading and trailing whitespaces are forbidden throughout System.Text.Json converters
                // we need to make sure that our input doesn't have them,
                // and if it has - we need to throw, to match behaviour of other converters
                // since Version.TryParse allows them and silently parses input to Version
                ThrowHelper.ThrowFormatException(DataType.Version);
            }

            if (Version.TryParse(source, out Version? result))
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
                ThrowHelper.ThrowFormatException(DataType.Version);
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
#if NETCOREAPP
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
