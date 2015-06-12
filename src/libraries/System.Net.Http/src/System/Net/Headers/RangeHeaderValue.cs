// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;

namespace System.Net.Http.Headers
{
    public class RangeHeaderValue : ICloneable
    {
        private string _unit;
        private ICollection<RangeItemHeaderValue> _ranges;

        public string Unit
        {
            get { return _unit; }
            set
            {
                HeaderUtilities.CheckValidToken(value, "value");
                _unit = value;
            }
        }

        public ICollection<RangeItemHeaderValue> Ranges
        {
            get
            {
                if (_ranges == null)
                {
                    _ranges = new ObjectCollection<RangeItemHeaderValue>();
                }
                return _ranges;
            }
        }

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
            Contract.Requires(source != null);

            _unit = source._unit;
            if (source._ranges != null)
            {
                foreach (RangeItemHeaderValue range in source._ranges)
                {
                    this.Ranges.Add((RangeItemHeaderValue)((ICloneable)range).Clone());
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(_unit);
            sb.Append('=');

            bool first = true;
            foreach (RangeItemHeaderValue range in Ranges)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(", ");
                }

                sb.Append(range.From);
                sb.Append('-');
                sb.Append(range.To);
            }

            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            RangeHeaderValue other = obj as RangeHeaderValue;

            if (other == null)
            {
                return false;
            }

            return string.Equals(_unit, other._unit, StringComparison.OrdinalIgnoreCase) &&
                HeaderUtilities.AreEqualCollections(Ranges, other.Ranges);
        }

        public override int GetHashCode()
        {
            int result = StringComparer.OrdinalIgnoreCase.GetHashCode(_unit);

            foreach (RangeItemHeaderValue range in Ranges)
            {
                result = result ^ range.GetHashCode();
            }

            return result;
        }

        public static RangeHeaderValue Parse(string input)
        {
            int index = 0;
            return (RangeHeaderValue)GenericHeaderParser.RangeParser.ParseValue(input, null, ref index);
        }

        public static bool TryParse(string input, out RangeHeaderValue parsedValue)
        {
            int index = 0;
            object output;
            parsedValue = null;

            if (GenericHeaderParser.RangeParser.TryParseValue(input, null, ref index, out output))
            {
                parsedValue = (RangeHeaderValue)output;
                return true;
            }
            return false;
        }

        internal static int GetRangeLength(string input, int startIndex, out object parsedValue)
        {
            Contract.Requires(startIndex >= 0);

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
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            if ((current == input.Length) || (input[current] != '='))
            {
                return 0;
            }

            current++; // skip '=' separator
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            int rangesLength = RangeItemHeaderValue.GetRangeItemListLength(input, current, result.Ranges);

            if (rangesLength == 0)
            {
                return 0;
            }

            current = current + rangesLength;
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
