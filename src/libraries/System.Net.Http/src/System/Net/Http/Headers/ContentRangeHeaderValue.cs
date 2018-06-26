// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace System.Net.Http.Headers
{
    public class ContentRangeHeaderValue : ICloneable
    {
        private string _unit;
        private long? _from;
        private long? _to;
        private long? _length;

        public string Unit
        {
            get { return _unit; }
            set
            {
                HeaderUtilities.CheckValidToken(value, nameof(value));
                _unit = value;
            }
        }

        public long? From
        {
            get { return _from; }
        }

        public long? To
        {
            get { return _to; }
        }

        public long? Length
        {
            get { return _length; }
        }

        public bool HasLength // e.g. "Content-Range: bytes 12-34/*"
        {
            get { return _length != null; }
        }

        public bool HasRange // e.g. "Content-Range: bytes */1234"
        {
            get { return _from != null; }
        }

        public ContentRangeHeaderValue(long from, long to, long length)
        {
            // Scenario: "Content-Range: bytes 12-34/5678"

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            if ((to < 0) || (to > length))
            {
                throw new ArgumentOutOfRangeException(nameof(to));
            }
            if ((from < 0) || (from > to))
            {
                throw new ArgumentOutOfRangeException(nameof(from));
            }

            _from = from;
            _to = to;
            _length = length;
            _unit = HeaderUtilities.BytesUnit;
        }

        public ContentRangeHeaderValue(long length)
        {
            // Scenario: "Content-Range: bytes */1234"

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            _length = length;
            _unit = HeaderUtilities.BytesUnit;
        }

        public ContentRangeHeaderValue(long from, long to)
        {
            // Scenario: "Content-Range: bytes 12-34/*"

            if (to < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(to));
            }
            if ((from < 0) || (from > to))
            {
                throw new ArgumentOutOfRangeException(nameof(from));
            }

            _from = from;
            _to = to;
            _unit = HeaderUtilities.BytesUnit;
        }

        private ContentRangeHeaderValue()
        {
        }

        private ContentRangeHeaderValue(ContentRangeHeaderValue source)
        {
            Debug.Assert(source != null);

            _from = source._from;
            _to = source._to;
            _length = source._length;
            _unit = source._unit;
        }

        public override bool Equals(object obj)
        {
            ContentRangeHeaderValue other = obj as ContentRangeHeaderValue;

            if (other == null)
            {
                return false;
            }

            return ((_from == other._from) && (_to == other._to) && (_length == other._length) &&
                string.Equals(_unit, other._unit, StringComparison.OrdinalIgnoreCase));
        }

        public override int GetHashCode()
        {
            int result = StringComparer.OrdinalIgnoreCase.GetHashCode(_unit);

            if (HasRange)
            {
                result = result ^ _from.GetHashCode() ^ _to.GetHashCode();
            }

            if (HasLength)
            {
                result = result ^ _length.GetHashCode();
            }

            return result;
        }

        public override string ToString()
        {
            StringBuilder sb = StringBuilderCache.Acquire();
            sb.Append(_unit);
            sb.Append(' ');

            if (HasRange)
            {
                sb.Append(_from.Value);
                sb.Append('-');
                sb.Append(_to.Value);
            }
            else
            {
                sb.Append('*');
            }

            sb.Append('/');
            if (HasLength)
            {
                sb.Append(_length.Value);
            }
            else
            {
                sb.Append('*');
            }

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public static ContentRangeHeaderValue Parse(string input)
        {
            int index = 0;
            return (ContentRangeHeaderValue)GenericHeaderParser.ContentRangeParser.ParseValue(input, null, ref index);
        }

        public static bool TryParse(string input, out ContentRangeHeaderValue parsedValue)
        {
            int index = 0;
            object output;
            parsedValue = null;

            if (GenericHeaderParser.ContentRangeParser.TryParseValue(input, null, ref index, out output))
            {
                parsedValue = (ContentRangeHeaderValue)output;
                return true;
            }
            return false;
        }

        internal static int GetContentRangeLength(string input, int startIndex, out object parsedValue)
        {
            Debug.Assert(startIndex >= 0);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Parse the unit string: <unit> in '<unit> <from>-<to>/<length>'
            int unitLength = HttpRuleParser.GetTokenLength(input, startIndex);

            if (unitLength == 0)
            {
                return 0;
            }

            string unit = input.Substring(startIndex, unitLength);
            int current = startIndex + unitLength;
            int separatorLength = HttpRuleParser.GetWhitespaceLength(input, current);

            if (separatorLength == 0)
            {
                return 0;
            }

            current = current + separatorLength;

            if (current == input.Length)
            {
                return 0;
            }

            // Read range values <from> and <to> in '<unit> <from>-<to>/<length>'
            int fromStartIndex = current;
            int fromLength = 0;
            int toStartIndex = 0;
            int toLength = 0;
            if (!TryGetRangeLength(input, ref current, out fromLength, out toStartIndex, out toLength))
            {
                return 0;
            }

            // After the range is read we expect the length separator '/'
            if ((current == input.Length) || (input[current] != '/'))
            {
                return 0;
            }

            current++; // Skip '/' separator
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            if (current == input.Length)
            {
                return 0;
            }

            // We may not have a length (e.g. 'bytes 1-2/*'). But if we do, parse the length now.
            int lengthStartIndex = current;
            int lengthLength = 0;
            if (!TryGetLengthLength(input, ref current, out lengthLength))
            {
                return 0;
            }

            if (!TryCreateContentRange(input, unit, fromStartIndex, fromLength, toStartIndex, toLength,
                lengthStartIndex, lengthLength, out parsedValue))
            {
                return 0;
            }

            return current - startIndex;
        }

        private static bool TryGetLengthLength(string input, ref int current, out int lengthLength)
        {
            lengthLength = 0;

            if (input[current] == '*')
            {
                current++;
            }
            else
            {
                // Parse length value: <length> in '<unit> <from>-<to>/<length>'
                lengthLength = HttpRuleParser.GetNumberLength(input, current, false);

                if ((lengthLength == 0) || (lengthLength > HttpRuleParser.MaxInt64Digits))
                {
                    return false;
                }

                current = current + lengthLength;
            }

            current = current + HttpRuleParser.GetWhitespaceLength(input, current);
            return true;
        }

        private static bool TryGetRangeLength(string input, ref int current, out int fromLength, out int toStartIndex,
            out int toLength)
        {
            fromLength = 0;
            toStartIndex = 0;
            toLength = 0;

            // Check if we have a value like 'bytes */133'. If yes, skip the range part and continue parsing the 
            // length separator '/'.
            if (input[current] == '*')
            {
                current++;
            }
            else
            {
                // Parse first range value: <from> in '<unit> <from>-<to>/<length>'
                fromLength = HttpRuleParser.GetNumberLength(input, current, false);

                if ((fromLength == 0) || (fromLength > HttpRuleParser.MaxInt64Digits))
                {
                    return false;
                }

                current = current + fromLength;
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);

                // After the first value, the '-' character must follow.
                if ((current == input.Length) || (input[current] != '-'))
                {
                    // We need a '-' character otherwise this can't be a valid range.
                    return false;
                }

                current++; // skip the '-' character
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);

                if (current == input.Length)
                {
                    return false;
                }

                // Parse second range value: <to> in '<unit> <from>-<to>/<length>'
                toStartIndex = current;
                toLength = HttpRuleParser.GetNumberLength(input, current, false);

                if ((toLength == 0) || (toLength > HttpRuleParser.MaxInt64Digits))
                {
                    return false;
                }

                current = current + toLength;
            }

            current = current + HttpRuleParser.GetWhitespaceLength(input, current);
            return true;
        }

        private static bool TryCreateContentRange(string input, string unit, int fromStartIndex, int fromLength,
            int toStartIndex, int toLength, int lengthStartIndex, int lengthLength, out object parsedValue)
        {
            parsedValue = null;

            long from = 0;
            if ((fromLength > 0) && !HeaderUtilities.TryParseInt64(input, fromStartIndex, fromLength, out from))
            {
                return false;
            }

            long to = 0;
            if ((toLength > 0) && !HeaderUtilities.TryParseInt64(input, toStartIndex, toLength, out to))
            {
                return false;
            }

            // 'from' must not be greater than 'to'
            if ((fromLength > 0) && (toLength > 0) && (from > to))
            {
                return false;
            }

            long length = 0;
            if ((lengthLength > 0) && !HeaderUtilities.TryParseInt64(input, lengthStartIndex, lengthLength, out length))
            {
                return false;
            }

            // 'from' and 'to' must be less than 'length'
            if ((toLength > 0) && (lengthLength > 0) && (to >= length))
            {
                return false;
            }

            ContentRangeHeaderValue result = new ContentRangeHeaderValue();
            result._unit = unit;

            if (fromLength > 0)
            {
                result._from = from;
                result._to = to;
            }

            if (lengthLength > 0)
            {
                result._length = length;
            }

            parsedValue = result;
            return true;
        }

        object ICloneable.Clone()
        {
            return new ContentRangeHeaderValue(this);
        }
    }
}
