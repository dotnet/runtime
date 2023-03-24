// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Net.Http.Headers
{
    public class StringWithQualityHeaderValue : ICloneable
    {
        private const double NotSetSentinel = double.PositiveInfinity;

        private readonly string _value;
        private readonly double _quality;

        public string Value => _value;

        public double? Quality => _quality == NotSetSentinel ? null : _quality;

        public StringWithQualityHeaderValue(string value)
        {
            HeaderUtilities.CheckValidToken(value, nameof(value));

            _value = value;
            _quality = NotSetSentinel;
        }

        public StringWithQualityHeaderValue(string value, double quality)
        {
            HeaderUtilities.CheckValidToken(value, nameof(value));

            ArgumentOutOfRangeException.ThrowIfNegative(quality);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(quality, 1.0);

            _value = value;
            _quality = quality;
        }

        private StringWithQualityHeaderValue(StringWithQualityHeaderValue source)
        {
            Debug.Assert(source != null);

            _value = source._value;
            _quality = source._quality;
        }

        public override string ToString() =>
            _quality == NotSetSentinel
                ? _value
                : string.Create(CultureInfo.InvariantCulture, stackalloc char[128], $"{_value}; q={_quality:0.0##}");

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is StringWithQualityHeaderValue other &&
            string.Equals(_value, other._value, StringComparison.OrdinalIgnoreCase) &&
            // Note that we don't consider double.Epsilon here. We really consider two values equal if they're
            // actually equal. This makes sure that we also get the same hashcode for two values considered equal
            // by Equals().
            _quality == other._quality;

        public override int GetHashCode() =>
            HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(_value), _quality);

        public static StringWithQualityHeaderValue Parse(string input)
        {
            int index = 0;
            return (StringWithQualityHeaderValue)GenericHeaderParser.SingleValueStringWithQualityParser.ParseValue(
                input, null, ref index);
        }

        public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out StringWithQualityHeaderValue? parsedValue)
        {
            int index = 0;
            parsedValue = null;

            if (GenericHeaderParser.SingleValueStringWithQualityParser.TryParseValue(
                input, null, ref index, out object? output))
            {
                parsedValue = (StringWithQualityHeaderValue)output!;
                return true;
            }
            return false;
        }

        internal static int GetStringWithQualityLength(string? input, int startIndex, out object? parsedValue)
        {
            Debug.Assert(startIndex >= 0);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Parse the value string: <value> in '<value>; q=<quality>'
            int valueLength = HttpRuleParser.GetTokenLength(input, startIndex);

            if (valueLength == 0)
            {
                return 0;
            }

            string value = input.Substring(startIndex, valueLength);
            int current = startIndex + valueLength;
            current += HttpRuleParser.GetWhitespaceLength(input, current);

            if ((current == input.Length) || (input[current] != ';'))
            {
                parsedValue = new StringWithQualityHeaderValue(value);
                return current - startIndex; // we have a valid token, but no quality.
            }

            current++; // skip ';' separator
            current += HttpRuleParser.GetWhitespaceLength(input, current);

            // If we found a ';' separator, it must be followed by a quality information
            if (!TryReadQuality(input, out double quality, ref current))
            {
                return 0;
            }

            parsedValue = new StringWithQualityHeaderValue(value, quality);
            return current - startIndex;
        }

        private static bool TryReadQuality(string input, out double quality, ref int index)
        {
            int current = index;
            quality = default;

            // See if we have a quality value by looking for "q"
            if ((current == input.Length) || ((input[current] != 'q') && (input[current] != 'Q')))
            {
                return false;
            }

            current++; // skip 'q' identifier
            current += HttpRuleParser.GetWhitespaceLength(input, current);

            // If we found "q" it must be followed by "="
            if ((current == input.Length) || (input[current] != '='))
            {
                return false;
            }

            current++; // skip '=' separator
            current += HttpRuleParser.GetWhitespaceLength(input, current);

            if (current == input.Length)
            {
                return false;
            }

            int qualityLength = HttpRuleParser.GetNumberLength(input, current, true);

            if (qualityLength == 0)
            {
                return false;
            }

            if (!double.TryParse(input.AsSpan(current, qualityLength), NumberStyles.AllowDecimalPoint,
                NumberFormatInfo.InvariantInfo, out quality))
            {
                return false;
            }

            if ((quality < 0) || (quality > 1))
            {
                return false;
            }

            current += qualityLength;
            current += HttpRuleParser.GetWhitespaceLength(input, current);

            index = current;
            return true;
        }

        object ICloneable.Clone()
        {
            return new StringWithQualityHeaderValue(this);
        }
    }
}
