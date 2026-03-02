// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class VersionConverter : JsonPrimitiveConverter<Version?>
    {
#if NET
        private const int MaximumFormattedVersionLength = 43; // 2147483647.2147483647.2147483647.2147483647
#endif

        public override Version? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType is JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                ThrowHelper.ThrowInvalidOperationException_ExpectedString(reader.TokenType);
            }

            return ReadCore(ref reader);
        }

        private static Version ReadCore(ref Utf8JsonReader reader)
        {
            Debug.Assert(reader.TokenType is JsonTokenType.PropertyName or JsonTokenType.String);

#if NET
            int bufferLength = reader.ValueLength;
            byte[]? rentedBuffer = null;
            try
            {
                Span<byte> utf8Buffer = bufferLength <= JsonConstants.StackallocByteThreshold
                    ? stackalloc byte[JsonConstants.StackallocByteThreshold]
                    : (rentedBuffer = ArrayPool<byte>.Shared.Rent(bufferLength));

                int bytesWritten = reader.CopyString(utf8Buffer);
                ReadOnlySpan<byte> utf8Source = utf8Buffer.Slice(0, bytesWritten);

                // Since leading and trailing whitespaces are forbidden throughout System.Text.Json converters
                // we need to make sure that our input doesn't have them,
                // and if it has - we need to throw, to match behaviour of other converters
                // since Version.TryParse allows them and silently parses input to Version
                if (utf8Source.IsEmpty || IsWhiteSpaceByte(utf8Source[0]) || IsWhiteSpaceByte(utf8Source[^1]))
                {
                    ThrowHelper.ThrowFormatException(DataType.Version);
                }

                if (Version.TryParse(utf8Source, out Version? result))
                {
                    return result!;
                }
            }
            finally
            {
                if (rentedBuffer is not null)
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                }
            }
#else
            string? versionString = reader.GetString();
            if (string.IsNullOrEmpty(versionString) || char.IsWhiteSpace(versionString[0]) || char.IsWhiteSpace(versionString[versionString.Length - 1]))
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

#if NET
        private static bool IsWhiteSpaceByte(byte b)
        {
            // Check for ASCII whitespace characters: space (0x20), tab (0x09), newline (0x0A),
            // carriage return (0x0D), form feed (0x0C), and vertical tab (0x0B)
            // For non-ASCII bytes, convert to char and check with char.IsWhiteSpace
            return b <= 127 ? char.IsWhiteSpace((char)b) : false;
        }
#endif

        public override void Write(Utf8JsonWriter writer, Version? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }

#if NET
            Span<byte> span = stackalloc byte[MaximumFormattedVersionLength];
            bool formattedSuccessfully = value.TryFormat(span, out int charsWritten);
            Debug.Assert(formattedSuccessfully);
            writer.WriteStringValue(span.Slice(0, charsWritten));
#else
            writer.WriteStringValue(value.ToString());
#endif
        }

        internal override Version ReadAsPropertyNameCore(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return ReadCore(ref reader);
        }

        internal override void WriteAsPropertyNameCore(Utf8JsonWriter writer, Version value, JsonSerializerOptions options, bool isWritingExtensionDataProperty)
        {
            ArgumentNullException.ThrowIfNull(value);

#if NET
            Span<byte> span = stackalloc byte[MaximumFormattedVersionLength];
            bool formattedSuccessfully = value.TryFormat(span, out int charsWritten);
            Debug.Assert(formattedSuccessfully);
            writer.WritePropertyName(span.Slice(0, charsWritten));
#else
            writer.WritePropertyName(value.ToString());
#endif
        }

        internal override JsonSchema? GetSchema(JsonNumberHandling _) =>
            new()
            {
                Type = JsonSchemaType.String,
                Comment = "Represents a version string.",
                Pattern = @"^\d+(\.\d+){1,3}$",
            };
    }
}
