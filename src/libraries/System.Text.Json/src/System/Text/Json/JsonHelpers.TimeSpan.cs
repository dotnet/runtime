// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    internal static partial class JsonHelpers
    {
        public static bool TryParseAsConstantFormat(ReadOnlySpan<char> source, out TimeSpan value)
        {
            if (!IsValidTimeSpanParseLength(source.Length))
            {
                value = default;
                return false;
            }

            int maxLength = checked(source.Length * JsonConstants.MaxExpansionFactorWhileTranscoding);

            Span<byte> bytes = maxLength <= JsonConstants.StackallocThreshold
                ? stackalloc byte[JsonConstants.StackallocThreshold]
                : new byte[maxLength];

            int length = JsonReaderHelper.GetUtf8FromText(source, bytes);

            bytes = bytes.Slice(0, length);

            if (bytes.IndexOf(JsonConstants.BackSlash) != -1)
            {
                return JsonReaderHelper.TryGetEscapedTimeSpan(bytes, out value);
            }

            Debug.Assert(bytes.IndexOf(JsonConstants.BackSlash) == -1);

            if (TryParseAsConstantFormat(bytes, out TimeSpan tmp))
            {
                value = tmp;
                return true;
            }

            value = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidTimeSpanParseLength(int length)
        {
            return IsInRangeInclusive(length, JsonConstants.MinimumTimeSpanParseLength, JsonConstants.MaximumEscapedTimeSpanParseLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidTimeSpanParseLength(long length)
        {
            return IsInRangeInclusive(length, JsonConstants.MinimumTimeSpanParseLength, JsonConstants.MaximumEscapedTimeSpanParseLength);
        }

        /// <summary>
        /// Parse the given UTF-8 <paramref name="source"/> as TimeSpan constant ("c") format.
        /// </summary>
        /// <param name="source">UTF-8 source to parse.</param>
        /// <param name="value">The parsed <see cref="TimeSpan"/> if successful.</param>
        /// <returns>"true" if successfully parsed.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseAsConstantFormat(ReadOnlySpan<byte> source, out TimeSpan value)
        {
            return Utf8Parser.TryParse(source, out value, out int _, 'c');
        }
    }
}
