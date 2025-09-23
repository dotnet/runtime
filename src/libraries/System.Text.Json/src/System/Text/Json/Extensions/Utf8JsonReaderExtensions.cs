// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Globalization;

namespace System.Text.Json.Extensions
{
    internal static class Utf8JsonReaderExtensions
    {
#if NET
        private const int DateOnlyMaxEscapedFormatLength = JsonConstants.DateOnlyFormatLength * JsonConstants.MaxExpansionFactorWhileEscaping;
        private const int MaximumEscapedTimeOnlyFormatLength = JsonConstants.MaximumTimeOnlyFormatLength * JsonConstants.MaxExpansionFactorWhileEscaping;
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

        internal static Int128 GetInt128(this ref Utf8JsonReader reader)
        {
            int bufferLength = reader.ValueLength;

            byte[]? rentedBuffer = null;
            Span<byte> buffer = bufferLength <= JsonConstants.StackallocByteThreshold
                ? stackalloc byte[JsonConstants.StackallocByteThreshold]
                : (rentedBuffer = ArrayPool<byte>.Shared.Rent(bufferLength));

            int written = reader.CopyValue(buffer);
            if (!Int128.TryParse(buffer.Slice(0, written), CultureInfo.InvariantCulture, out Int128 result))
            {
                ThrowHelper.ThrowFormatException(NumericType.Int128);
            }

            if (rentedBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }

            return result;
        }

        internal static TimeOnly GetTimeOnly(this ref Utf8JsonReader reader)
        {
            Debug.Assert(reader.TokenType is JsonTokenType.String or JsonTokenType.PropertyName);

            if (!JsonHelpers.IsInRangeInclusive(reader.ValueLength, JsonConstants.MinimumTimeOnlyFormatLength, MaximumEscapedTimeOnlyFormatLength))
            {
                ThrowHelper.ThrowFormatException(DataType.TimeOnly);
            }

            scoped ReadOnlySpan<byte> source;
            if (!reader.HasValueSequence && !reader.ValueIsEscaped)
            {
                source = reader.ValueSpan;
            }
            else
            {
                Span<byte> stackSpan = stackalloc byte[MaximumEscapedTimeOnlyFormatLength];
                int bytesWritten = reader.CopyString(stackSpan);
                source = stackSpan.Slice(0, bytesWritten);
            }

            byte firstChar = source[0];
            int firstSeparator = source.IndexOfAny((byte)'.', (byte)':');
            if (!JsonHelpers.IsDigit(firstChar) || firstSeparator < 0 || source[firstSeparator] == (byte)'.')
            {
                // Note: Utf8Parser.TryParse permits leading whitespace, negative values
                // and numbers of days so we need to exclude these cases here.
                ThrowHelper.ThrowFormatException(DataType.TimeOnly);
            }

            bool result = Utf8Parser.TryParse(source, out TimeSpan timespan, out int bytesConsumed, 'c');

            // Note: Utf8Parser.TryParse will return true for invalid input so
            // long as it starts with an integer. Example: "2021-06-18" or
            // "1$$$$$$$$$$". We need to check bytesConsumed to know if the
            // entire source was actually valid.

            if (!result || source.Length != bytesConsumed)
            {
                ThrowHelper.ThrowFormatException(DataType.TimeOnly);
            }

            Debug.Assert(TimeOnly.MinValue.ToTimeSpan() <= timespan && timespan <= TimeOnly.MaxValue.ToTimeSpan());
            return TimeOnly.FromTimeSpan(timespan);
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
