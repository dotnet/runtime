// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class VersionConverter : JsonConverter<Version>
    {
        public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader._stringHasEscaping)
            {
                ThrowHelper.ThrowJsonException();
            }

#if BUILDING_INBOX_LIBRARY

            ReadOnlySpan<byte> source = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;

            int maxCharCount = JsonReaderHelper.s_utf8Encoding.GetMaxCharCount(source.Length);
            char[]? pooledChars = null;
            Span<char> charBuffer = maxCharCount * sizeof(char) <= JsonConstants.StackallocThreshold
                ? stackalloc char[maxCharCount]
                : pooledChars = ArrayPool<char>.Shared.Rent(maxCharCount);
            int writtenChars = JsonReaderHelper.s_utf8Encoding.GetChars(source, charBuffer);
            bool isParsingSuccessful = Version.TryParse(charBuffer.Slice(0, writtenChars), out Version? result);

            if (pooledChars != null)
            {
                charBuffer.Clear();
                ArrayPool<char>.Shared.Return(pooledChars);
            }

            if (isParsingSuccessful)
            {
                Debug.Assert(result != null);
                return result;
            }

#else
            string? versionString = reader.GetString();
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
            const int versionComponentsCount = 4; // Major, Minor, Build, Revision

            const int maxStringLengthOfPositiveInt32 = 10; // int.MaxValue.ToString().Length

            const int maxStringLengthOfVersion = (maxStringLengthOfPositiveInt32 * versionComponentsCount) + 1 + 1 + 1; // 43, 1 is length of '.'
            Debug.Assert(JsonConstants.StackallocThreshold >= maxStringLengthOfVersion * sizeof(char),
                "Stack allocated buffer should not be bigger than stackalloc threshold defined in JsonConstants");
            Span<char> span = stackalloc char[maxStringLengthOfVersion];
            bool formattedSuccessfully = value.TryFormat(span, out int charsWritten);
            Debug.Assert(formattedSuccessfully);
            writer.WriteStringValue(span.Slice(0, charsWritten));
            return;
#else
            writer.WriteStringValue(value.ToString());
            return;
#endif
        }

        private static int GetIndexOfDot(ReadOnlySpan<byte> source) => source.IndexOf((byte)'.');
    }
}
