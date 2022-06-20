// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http.Headers
{
    public class RetryConditionHeaderValue : ICloneable
    {
        private readonly DateTimeOffset? _date;
        private readonly TimeSpan? _delta;

        public DateTimeOffset? Date
        {
            get { return _date; }
        }

        public TimeSpan? Delta
        {
            get { return _delta; }
        }

        public RetryConditionHeaderValue(DateTimeOffset date)
        {
            _date = date;
        }

        public RetryConditionHeaderValue(TimeSpan delta)
        {
            // The amount of seconds for 'delta' must be in the range 0..2^31
            if (delta.TotalSeconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(delta));
            }

            _delta = delta;
        }

        private RetryConditionHeaderValue(RetryConditionHeaderValue source)
        {
            Debug.Assert(source != null);

            _delta = source._delta;
            _date = source._date;
        }

        public override string ToString()
        {
            if (_delta.HasValue)
            {
                return ((int)_delta.Value.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
            }

            Debug.Assert(_date != null);
            return _date.GetValueOrDefault().ToString("r");
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            RetryConditionHeaderValue? other = obj as RetryConditionHeaderValue;

            if (other == null)
            {
                return false;
            }

            if (_delta.HasValue)
            {
                return (other._delta != null) && (_delta.Value == other._delta.Value);
            }

            Debug.Assert(_date != null);
            return (other._date != null) && (_date.Value == other._date.Value);
        }

        public override int GetHashCode()
        {
            if (_delta == null)
            {
                Debug.Assert(_date != null);
                return _date.Value.GetHashCode();
            }

            return _delta.Value.GetHashCode();
        }

        public static RetryConditionHeaderValue Parse(string? input)
        {
            int index = 0;
            return (RetryConditionHeaderValue)GenericHeaderParser.RetryConditionParser.ParseValue(
                input, null, ref index);
        }

        public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out RetryConditionHeaderValue? parsedValue)
        {
            int index = 0;
            parsedValue = null;

            if (GenericHeaderParser.RetryConditionParser.TryParseValue(input, null, ref index, out object? output))
            {
                parsedValue = (RetryConditionHeaderValue)output!;
                return true;
            }
            return false;
        }

        internal static int GetRetryConditionLength(string? input, int startIndex, out object? parsedValue)
        {
            Debug.Assert(startIndex >= 0);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            int current = startIndex;

            // Caller must remove leading whitespace.
            DateTimeOffset date = DateTimeOffset.MinValue;
            int deltaSeconds = -1; // use -1 to indicate that the value was not set. 'delta' values are always >=0

            // We either have a timespan or a date/time value. Determine which one we have by looking at the first char.
            // If it is a number, we have a timespan, otherwise we assume we have a date.
            char firstChar = input[current];

            if (char.IsAsciiDigit(firstChar))
            {
                int deltaStartIndex = current;
                int deltaLength = HttpRuleParser.GetNumberLength(input, current, false);

                // The value must be in the range 0..2^31
                if ((deltaLength == 0) || (deltaLength > HttpRuleParser.MaxInt32Digits))
                {
                    return 0;
                }

                current += deltaLength;
                current += HttpRuleParser.GetWhitespaceLength(input, current);

                // RetryConditionHeaderValue only allows 1 value. There must be no delimiter/other chars after 'delta'
                if (current != input.Length)
                {
                    return 0;
                }

                if (!HeaderUtilities.TryParseInt32(input, deltaStartIndex, deltaLength, out deltaSeconds))
                {
                    return 0; // int.TryParse() may return 'false' if the value has 10 digits and is > Int32.MaxValue.
                }
            }
            else
            {
                if (!HttpDateParser.TryParse(input.AsSpan(current), out date))
                {
                    return 0;
                }

                // If we got a valid date, then the parser consumed the whole string (incl. trailing whitespace).
                current = input.Length;
            }

            if (deltaSeconds == -1) // we didn't change delta, so we must have found a date.
            {
                parsedValue = new RetryConditionHeaderValue(date);
            }
            else
            {
                parsedValue = new RetryConditionHeaderValue(new TimeSpan(0, 0, deltaSeconds));
            }

            return current - startIndex;
        }

        object ICloneable.Clone()
        {
            return new RetryConditionHeaderValue(this);
        }
    }
}
