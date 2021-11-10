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
        private readonly long? _from;
        private readonly long? _to;

        public long? From
        {
            get { return _from; }
        }

        public long? To
        {
            get { return _to; }
        }

        public RangeItemHeaderValue(long? from, long? to)
        {
            if (!from.HasValue && !to.HasValue)
            {
                throw new ArgumentException(SR.net_http_headers_invalid_range);
            }
            if (from.HasValue && (from.Value < 0))
            {
                throw new ArgumentOutOfRangeException(nameof(from));
            }
            if (to.HasValue && (to.Value < 0))
            {
                throw new ArgumentOutOfRangeException(nameof(to));
            }
            if (from.HasValue && to.HasValue && (from.Value > to.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(from));
            }

            _from = from;
            _to = to;
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

            if (!_from.HasValue)
            {
                Debug.Assert(_to != null);
                return string.Create(CultureInfo.InvariantCulture, stackBuffer, $"-{_to.Value}");
            }

            if (!_to.HasValue)
            {
                return string.Create(CultureInfo.InvariantCulture, stackBuffer, $"{_from.Value}-"); ;
            }

            return string.Create(CultureInfo.InvariantCulture, stackBuffer, $"{_from.Value}-{_to.Value}");
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            RangeItemHeaderValue? other = obj as RangeItemHeaderValue;

            if (other == null)
            {
                return false;
            }
            return ((_from == other._from) && (_to == other._to));
        }

        public override int GetHashCode()
        {
            if (!_from.HasValue)
            {
                return _to.GetHashCode();
            }
            else if (!_to.HasValue)
            {
                return _from.GetHashCode();
            }
            return _from.GetHashCode() ^ _to.GetHashCode();
        }

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
            bool separatorFound = false;
            int current = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(input, startIndex, true, out separatorFound);
            // It's OK if we didn't find leading separator characters. Ignore 'separatorFound'.

            if (current == input.Length)
            {
                return 0;
            }

            RangeItemHeaderValue? range;
            while (true)
            {
                int rangeLength = GetRangeItemLength(input, current, out range);

                if (rangeLength == 0)
                {
                    return 0;
                }

                rangeCollection.Add(range!);

                current = current + rangeLength;
                current = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(input, current, true, out separatorFound);

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

            current = current + fromLength;
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            // After the first value, the '-' character must follow.
            if ((current == input.Length) || (input[current] != '-'))
            {
                // We need a '-' character otherwise this can't be a valid range.
                return 0;
            }

            current++; // skip the '-' character
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

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

                current = current + toLength;
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);
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

            parsedValue = new RangeItemHeaderValue((fromLength == 0 ? (long?)null : (long?)from),
                (toLength == 0 ? (long?)null : (long?)to));
            return current - startIndex;
        }

        object ICloneable.Clone()
        {
            return new RangeItemHeaderValue(this);
        }
    }
}
