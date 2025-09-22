// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Globalization;

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

        internal static Half GetHalf(this ref Utf8JsonReader reader)
        {
            Half result;

            byte[]? rentedByteBuffer = null;
            int bufferLength = reader.ValueLength;

            Span<byte> byteBuffer = bufferLength <= JsonConstants.StackallocByteThreshold
                ? stackalloc byte[JsonConstants.StackallocByteThreshold]
                : (rentedByteBuffer = ArrayPool<byte>.Shared.Rent(bufferLength));

            int written = reader.CopyValue(byteBuffer);
            byteBuffer = byteBuffer.Slice(0, written);

            bool success = TryParse(byteBuffer, out result);
            if (rentedByteBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(rentedByteBuffer);
            }

            if (!success)
            {
                ThrowHelper.ThrowFormatException(NumericType.Half);
            }

            Debug.Assert(!Half.IsNaN(result) && !Half.IsInfinity(result));
            return result;
        }

        private static bool TryParse(ReadOnlySpan<byte> buffer, out Half result)
        {
            bool success = Half.TryParse(buffer, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result);

            // Half.TryParse is more lax with floating-point literals than other S.T.Json floating-point types
            // e.g: it parses "naN" successfully. Only succeed with the exact match.
            return success &&
                (!Half.IsNaN(result) || buffer.SequenceEqual(JsonConstants.NaNValue)) &&
                (!Half.IsPositiveInfinity(result) || buffer.SequenceEqual(JsonConstants.PositiveInfinityValue)) &&
                (!Half.IsNegativeInfinity(result) || buffer.SequenceEqual(JsonConstants.NegativeInfinityValue));
        }
#endif
    }
}
