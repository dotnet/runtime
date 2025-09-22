// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Extensions
{
    internal static class Utf8JsonReaderExtensions
    {
#if NET
        private const int DateOnlyMaxEscapedFormatLength = JsonConstants.DateOnlyFormatLength * JsonConstants.MaxExpansionFactorWhileEscaping;
#endif

#if NET
        internal static DateOnly GetDateOnly(this ref Utf8JsonReader reader)
        {
            if (!JsonHelpers.IsInRangeInclusive(reader.ValueLength, JsonConstants.DateOnlyFormatLength, DateOnlyMaxEscapedFormatLength))
            {
                ThrowHelper.ThrowFormatException(DataType.DateOnly);
            }

            scoped ReadOnlySpan<byte> source;
            if (!reader.HasValueSequence && !reader.ValueIsEscaped)
            {
                source = reader.ValueSpan;
            }
            else
            {
                Span<byte> stackSpan = stackalloc byte[DateOnlyMaxEscapedFormatLength];
                int bytesWritten = reader.CopyString(stackSpan);

                // CopyString can unescape which can change the length, so we need to perform the length check again.
                if (bytesWritten < JsonConstants.DateOnlyFormatLength)
                {
                    ThrowHelper.ThrowFormatException(DataType.DateOnly);
                }

                source = stackSpan.Slice(0, bytesWritten);
            }

            if (!JsonHelpers.TryParseAsIso(source, out DateOnly value))
            {
                ThrowHelper.ThrowFormatException(DataType.DateOnly);
            }

            return value;
        }
#endif
    }
}
