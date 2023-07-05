// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace System.Net.Http.Headers
{
    public class RangeHeaderValue : ICloneable
    {
        private string _unit;
        private UnvalidatedObjectCollection<RangeItemHeaderValue>? _ranges;

        public string Unit
        {
            get { return _unit; }
            set
            {
                HeaderUtilities.CheckValidToken(value);
                _unit = value;
            }
        }

        public ICollection<RangeItemHeaderValue> Ranges => _ranges ??= new UnvalidatedObjectCollection<RangeItemHeaderValue>();

        public RangeHeaderValue()
        {
            _unit = HeaderUtilities.BytesUnit;
        }

        public RangeHeaderValue(long? from, long? to)
        {
            // convenience ctor: "Range: bytes=from-to"
            _unit = HeaderUtilities.BytesUnit;
            Ranges.Add(new RangeItemHeaderValue(from, to));
        }

        private RangeHeaderValue(RangeHeaderValue source)
        {
            Debug.Assert(source != null);

            _unit = source._unit;
            if (source._ranges != null)
            {
                foreach (RangeItemHeaderValue range in source._ranges)
                {
                    this.Ranges.Add(new RangeItemHeaderValue(range));
                }
            }
        }

        public override string ToString()
        {
            var sb = new ValueStringBuilder(stackalloc char[256]);
            sb.Append(_unit);
            sb.Append('=');

            if (_ranges != null)
            {
                bool first = true;
                foreach (RangeItemHeaderValue range in _ranges)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(", ");
                    }

                    if (range.From.HasValue) sb.AppendSpanFormattable(range.From.GetValueOrDefault());
                    sb.Append('-');
                    if (range.To.HasValue) sb.AppendSpanFormattable(range.To.GetValueOrDefault());
                }
            }

            return sb.ToString();
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            RangeHeaderValue? other = obj as RangeHeaderValue;

            if (other == null)
            {
                return false;
            }

            return string.Equals(_unit, other._unit, StringComparison.OrdinalIgnoreCase) &&
                HeaderUtilities.AreEqualCollections(_ranges, other._ranges);
        }

        public override int GetHashCode()
        {
            int result = StringComparer.OrdinalIgnoreCase.GetHashCode(_unit);

            if (_ranges != null)
            {
                foreach (RangeItemHeaderValue range in _ranges)
                {
                    result ^= range.GetHashCode();
                }
            }

            return result;
        }

        public static RangeHeaderValue Parse(string input)
        {
            int index = 0;
            return (RangeHeaderValue)GenericHeaderParser.RangeParser.ParseValue(input, null, ref index);
        }

        public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out RangeHeaderValue? parsedValue)
        {
            int index = 0;
             parsedValue = null;

            if (GenericHeaderParser.RangeParser.TryParseValue(input, null, ref index, out object? output))
            {
                parsedValue = (RangeHeaderValue)output!;
                return true;
            }
            return false;
        }

        internal static int GetRangeLength(string? input, int startIndex, out object? parsedValue)
        {
            Debug.Assert(startIndex >= 0);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Parse the unit string: <unit> in '<unit>=<from1>-<to1>, <from2>-<to2>'
            int unitLength = HttpRuleParser.GetTokenLength(input, startIndex);

            if (unitLength == 0)
            {
                return 0;
            }

            RangeHeaderValue result = new RangeHeaderValue();
            result._unit = input.Substring(startIndex, unitLength);
            int current = startIndex + unitLength;
            current += HttpRuleParser.GetWhitespaceLength(input, current);

            if ((current == input.Length) || (input[current] != '='))
            {
                return 0;
            }

            current++; // skip '=' separator
            current += HttpRuleParser.GetWhitespaceLength(input, current);

            int rangesLength = RangeItemHeaderValue.GetRangeItemListLength(input, current, result.Ranges);

            if (rangesLength == 0)
            {
                return 0;
            }

            current += rangesLength;
            Debug.Assert(current == input.Length, "GetRangeItemListLength() should consume the whole string or fail.");

            parsedValue = result;
            return current - startIndex;
        }

        object ICloneable.Clone()
        {
            return new RangeHeaderValue(this);
        }
    }
}
