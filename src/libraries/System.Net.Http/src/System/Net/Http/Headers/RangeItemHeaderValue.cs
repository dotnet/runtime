// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Net.Http.Headers
{
    public class RangeItemHeaderValue : ICloneable
    {
        // Set to -1 if not set.
        private readonly long _from;
        private readonly long _to;

        public long? From => _from >= 0 ? _from : null;

        public long? To => _to >= 0 ? _to : null;

        public RangeItemHeaderValue(long? from, long? to)
        {
            if (!from.HasValue && !to.HasValue)
            {
                throw new ArgumentException(SR.net_http_headers_invalid_range);
            }
            if (from.HasValue)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(from.GetValueOrDefault(), nameof(from));
            }
            if (to.HasValue)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(to.GetValueOrDefault(), nameof(to));
            }
            if (from.HasValue && to.HasValue)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(from.GetValueOrDefault(), to.GetValueOrDefault(), nameof(from));
            }

            _from = from ?? -1;
            _to = to ?? -1;
        }

        internal RangeItemHeaderValue(RangeItemHeaderValue source)
        {
            Debug.Assert(source != null);

            _from = source._from;
            _to = source._to;
        }

        public override string ToString()
        {
            Span<char> stackBuffer = stackalloc char[128];

            if (_from < 0)
            {
                return string.Create(CultureInfo.InvariantCulture, stackBuffer, $"-{_to}");
            }

            if (_to < 0)
            {
                return string.Create(CultureInfo.InvariantCulture, stackBuffer, $"{_from}-"); ;
            }

            return string.Create(CultureInfo.InvariantCulture, stackBuffer, $"{_from}-{_to}");
        }

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is RangeItemHeaderValue other &&
            _from == other._from &&
            _to == other._to;

        public override int GetHashCode() =>
            HashCode.Combine(_from, _to);

        // Returns the length of a range list. E.g. "1-2, 3-4, 5-6" adds 3 ranges to 'rangeCollection'. Note that empty
        // list segments are allowed, e.g. ",1-2, , 3-4,,".
        internal static int GetRangeItemListLength(string? input, int startIndex,
            ICollection<RangeItemHeaderValue> rangeCollection)
        {
            Debug.Assert(rangeCollection != null);
            Debug.Assert(startIndex >= 0);

            if ((string.IsNullOrEmpty(input)) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Empty segments are allowed, so skip all delimiter-only segments (e.g. ", ,").
            int current = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(input, startIndex, true, out _);
            // It's OK if we didn't find leading separator characters. Ignore 'separatorFound'.

            if (current == input.Length)
            {
                return 0;
            }

            while (true)
            {
                int rangeLength = GetRangeItemLength(input, current, out RangeItemHeaderValue? range);

                if (rangeLength == 0)
                {
                    return 0;
                }

                rangeCollection.Add(range!);

                current += rangeLength;
                current = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(input, current, true, out bool separatorFound);

                // If the string is not consumed, we must have a delimiter, otherwise the string is not a valid
                // range list.
                if ((current < input.Length) && !separatorFound)
                {
                    return 0;
                }

                if (current == input.Length)
                {
                    return current - startIndex;
                }
            }
        }

        internal static int GetRangeItemLength(string? input, int startIndex, out RangeItemHeaderValue? parsedValue)
        {
            Debug.Assert(startIndex >= 0);

            // This parser parses number ranges: e.g. '1-2', '1-', '-2'.

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Caller must remove leading whitespace. If not, we'll return 0.
            int current = startIndex;

            // Try parse the first value of a value pair.
            int fromStartIndex = current;
            int fromLength = HttpRuleParser.GetNumberLength(input, current, false);

            if (fromLength > HttpRuleParser.MaxInt64Digits)
            {
                return 0;
            }

            current += fromLength;
            current += HttpRuleParser.GetWhitespaceLength(input, current);

            // After the first value, the '-' character must follow.
            if ((current == input.Length) || (input[current] != '-'))
            {
                // We need a '-' character otherwise this can't be a valid range.
                return 0;
            }

            current++; // skip the '-' character
            current += HttpRuleParser.GetWhitespaceLength(input, current);

            int toStartIndex = current;
            int toLength = 0;

            // If we didn't reach the end of the string, try parse the second value of the range.
            if (current < input.Length)
            {
                toLength = HttpRuleParser.GetNumberLength(input, current, false);

                if (toLength > HttpRuleParser.MaxInt64Digits)
                {
                    return 0;
                }

                current += toLength;
                current += HttpRuleParser.GetWhitespaceLength(input, current);
            }

            if ((fromLength == 0) && (toLength == 0))
            {
                return 0; // At least one value must be provided in order to be a valid range.
            }

            // Try convert first value to int64
            long from = 0;
            if ((fromLength > 0) && !HeaderUtilities.TryParseInt64(input, fromStartIndex, fromLength, out from))
            {
                return 0;
            }

            // Try convert second value to int64
            long to = 0;
            if ((toLength > 0) && !HeaderUtilities.TryParseInt64(input, toStartIndex, toLength, out to))
            {
                return 0;
            }

            // 'from' must not be greater than 'to'
            if ((fromLength > 0) && (toLength > 0) && (from > to))
            {
                return 0;
            }

            parsedValue = new RangeItemHeaderValue((fromLength == 0 ? null : from), (toLength == 0 ? null : to));
            return current - startIndex;
        }

        object ICloneable.Clone()
        {
            return new RangeItemHeaderValue(this);
        }
    }
}
