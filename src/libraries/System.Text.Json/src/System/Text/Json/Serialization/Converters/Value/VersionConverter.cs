// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class VersionConverter : JsonConverter<Version>
    {
        private const int VersionComponentsCount = 4; // Major, Minor, Build, Revision

#if BUILDING_INBOX_LIBRARY
        private const int MaxStringLengthOfPositiveInt32 = 10; // int.MaxValue.ToString().Length

        private const int
            MaxStringLengthOfVersion = (MaxStringLengthOfPositiveInt32 * VersionComponentsCount) + 1 + 1 + 1; // 43, 1 is length of '.'
#endif

        public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            ReadOnlySpan<byte> source = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;

            Span<int?> versionComponents = stackalloc int?[VersionComponentsCount] { null, null, null, null };
            int indexOfDot = GetIndexOfDot(source);

            if (reader._stringHasEscaping || indexOfDot == -1)
            {
                ThrowHelper.ThrowJsonException();
            }

            Debug.Assert(source.IndexOf(JsonConstants.BackSlash) == -1, "Version value should have no escaping");
            Debug.Assert(indexOfDot != -1, "Version should have at least Major and Minor values separated by \".\"");

            for (int i = 0; i < versionComponents.Length; i++)
            {
                bool lastComponent = indexOfDot == -1;
                var readOnlySpan = lastComponent ? source : source.Slice(0, indexOfDot);
                if (Utf8Parser.TryParse(readOnlySpan, out int value, out _))
                {
                    if (value < 0)
                    {
                        ThrowHelper.ThrowJsonException();
                    }

                    versionComponents[i] = value;
                    source = source.Slice(indexOfDot + 1);
                    indexOfDot = GetIndexOfDot(source);
                    if (lastComponent)
                    {
                        break;
                    }
                }
                else
                {
                    ThrowHelper.ThrowJsonException();
                }
            }

            ref int? major = ref versionComponents[0];
            ref int? minor = ref versionComponents[1];
            ref int? build = ref versionComponents[2];
            ref int? revision = ref versionComponents[3];
            if (major.HasValue && minor.HasValue && build.HasValue && revision.HasValue)
            {
                return new Version(major.Value, minor.Value, build.Value, revision.Value);
            }
            else if (major.HasValue && minor.HasValue && build.HasValue)
            {
                return new Version(major.Value, minor.Value, build.Value);
            }
            else if (major.HasValue && minor.HasValue)
            {
                return new Version(major.Value, minor.Value);
            }

            ThrowHelper.ThrowJsonException();
            return null;
        }

        public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
        {
#if BUILDING_INBOX_LIBRARY
            Debug.Assert(
                JsonConstants.StackallocThreshold >= MaxStringLengthOfVersion * sizeof(char),
                "Stack allocated buffer should not be bigger than stackalloc threshold defined in JsonConstants");
            Span<char> span = stackalloc char[MaxStringLengthOfVersion];
            value.TryFormat(span, out int charsWritten);
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
