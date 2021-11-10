// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;

namespace System.Net.Http.Headers
{
    public class WarningHeaderValue : ICloneable
    {
        private readonly int _code;
        private readonly string _agent;
        private readonly string _text;
        private readonly DateTimeOffset? _date;

        public int Code
        {
            get { return _code; }
        }

        public string Agent
        {
            get { return _agent; }
        }

        public string Text
        {
            get { return _text; }
        }

        public DateTimeOffset? Date
        {
            get { return _date; }
        }

        public WarningHeaderValue(int code, string agent, string text)
        {
            CheckCode(code);
            CheckAgent(agent);
            HeaderUtilities.CheckValidQuotedString(text, nameof(text));

            _code = code;
            _agent = agent;
            _text = text;
        }

        public WarningHeaderValue(int code, string agent, string text, DateTimeOffset date)
        {
            CheckCode(code);
            CheckAgent(agent);
            HeaderUtilities.CheckValidQuotedString(text, nameof(text));

            _code = code;
            _agent = agent;
            _text = text;
            _date = date;
        }

        private WarningHeaderValue(WarningHeaderValue source)
        {
            Debug.Assert(source != null);

            _code = source._code;
            _agent = source._agent;
            _text = source._text;
            _date = source._date;
        }

        public override string ToString()
        {
            StringBuilder sb = StringBuilderCache.Acquire();

            // Warning codes are always 3 digits according to RFC2616
            sb.Append(NumberFormatInfo.InvariantInfo, $"{_code:000}");

            sb.Append(' ');
            sb.Append(_agent);
            sb.Append(' ');
            sb.Append(_text);

            if (_date.HasValue)
            {
                sb.Append(" \"");
                sb.Append(HttpDateParser.DateToString(_date.Value));
                sb.Append('\"');
            }

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            WarningHeaderValue? other = obj as WarningHeaderValue;

            if (other == null)
            {
                return false;
            }

            // 'agent' is a host/token, i.e. use case-insensitive comparison. Use case-sensitive comparison for 'text'
            // since it is a quoted string.
            if ((_code != other._code) || (!string.Equals(_agent, other._agent, StringComparison.OrdinalIgnoreCase)) ||
                (!string.Equals(_text, other._text, StringComparison.Ordinal)))
            {
                return false;
            }

            // We have a date set. Verify 'other' has also a date that matches our value.
            if (_date.HasValue)
            {
                return other._date.HasValue && (_date.Value == other._date.Value);
            }

            // We don't have a date. If 'other' has a date, we're not equal.
            return !other._date.HasValue;
        }

        public override int GetHashCode()
        {
            int result = _code.GetHashCode() ^
                StringComparer.OrdinalIgnoreCase.GetHashCode(_agent) ^
                _text.GetHashCode();

            if (_date.HasValue)
            {
                result = result ^ _date.Value.GetHashCode();
            }

            return result;
        }

        public static WarningHeaderValue Parse(string? input)
        {
            int index = 0;
            return (WarningHeaderValue)GenericHeaderParser.SingleValueWarningParser.ParseValue(input, null, ref index);
        }

        public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out WarningHeaderValue? parsedValue)
        {
            int index = 0;
            parsedValue = null;

            if (GenericHeaderParser.SingleValueWarningParser.TryParseValue(input, null, ref index, out object? output))
            {
                parsedValue = (WarningHeaderValue)output!;
                return true;
            }
            return false;
        }

        internal static int GetWarningLength(string? input, int startIndex, out object? parsedValue)
        {
            Debug.Assert(startIndex >= 0);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Read <code> in '<code> <agent> <text> ["<date>"]'
            int code;
            int current = startIndex;

            if (!TryReadCode(input, ref current, out code))
            {
                return 0;
            }

            // Read <agent> in '<code> <agent> <text> ["<date>"]'
            if (!TryReadAgent(input, current, ref current, out string? agent))
            {
                return 0;
            }

            // Read <text> in '<code> <agent> <text> ["<date>"]'
            int textLength = 0;
            int textStartIndex = current;
            if (HttpRuleParser.GetQuotedStringLength(input, current, out textLength) != HttpParseResult.Parsed)
            {
                return 0;
            }

            string text = input.Substring(textStartIndex, textLength);

            current = current + textLength;

            // Read <date> in '<code> <agent> <text> ["<date>"]'
            DateTimeOffset? date = null;
            if (!TryReadDate(input, ref current, out date))
            {
                return 0;
            }

            parsedValue = date is null ?
                new WarningHeaderValue(code, agent, text) :
                new WarningHeaderValue(code, agent, text, date.Value);

            return current - startIndex;
        }

        private static bool TryReadAgent(string input, int startIndex, ref int current, [NotNullWhen(true)] out string? agent)
        {
            int agentLength = HttpRuleParser.GetHostLength(input, startIndex, true, out agent!);

            if (agentLength == 0)
            {
                return false;
            }

            current = current + agentLength;
            int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
            current = current + whitespaceLength;

            // At least one whitespace required after <agent>. Also make sure we have characters left for <text>
            if ((whitespaceLength == 0) || (current == input.Length))
            {
                return false;
            }

            return true;
        }

        private static bool TryReadCode(string input, ref int current, out int code)
        {
            code = 0;
            int codeLength = HttpRuleParser.GetNumberLength(input, current, false);

            // code must be a 3 digit value. We accept less digits, but we don't accept more.
            if ((codeLength == 0) || (codeLength > 3))
            {
                return false;
            }

            if (!HeaderUtilities.TryParseInt32(input, current, codeLength, out code))
            {
                Debug.Fail("Unable to parse value even though it was parsed as <=3 digits string. Input: '" +
                    input + "', Current: " + current + ", CodeLength: " + codeLength);
                return false;
            }

            current = current + codeLength;

            int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
            current = current + whitespaceLength;

            // Make sure the number is followed by at least one whitespace and that we have characters left to parse.
            if ((whitespaceLength == 0) || (current == input.Length))
            {
                return false;
            }

            return true;
        }

        private static bool TryReadDate(string input, ref int current, out DateTimeOffset? date)
        {
            date = null;

            // Make sure we have at least one whitespace between <text> and <date> (if we have <date>)
            int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
            current = current + whitespaceLength;

            // Read <date> in '<code> <agent> <text> ["<date>"]'
            if ((current < input.Length) && (input[current] == '"'))
            {
                if (whitespaceLength == 0)
                {
                    return false; // we have characters after <text> but they were not separated by a whitespace
                }

                current++; // skip opening '"'

                // Find the closing '"'
                int dateStartIndex = current;
                while (current < input.Length)
                {
                    if (input[current] == '"')
                    {
                        break;
                    }
                    current++;
                }

                if ((current == input.Length) || (current == dateStartIndex))
                {
                    return false; // we couldn't find the closing '"' or we have an empty quoted string.
                }

                DateTimeOffset temp;
                if (!HttpDateParser.TryParse(input.AsSpan(dateStartIndex, current - dateStartIndex), out temp))
                {
                    return false;
                }

                date = temp;

                current++; // skip closing '"'
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);
            }

            return true;
        }

        object ICloneable.Clone()
        {
            return new WarningHeaderValue(this);
        }

        private static void CheckCode(int code)
        {
            if ((code < 0) || (code > 999))
            {
                throw new ArgumentOutOfRangeException(nameof(code));
            }
        }

        private static void CheckAgent(string agent)
        {
            if (string.IsNullOrEmpty(agent))
            {
                throw new ArgumentException(SR.net_http_argument_empty_string, nameof(agent));
            }

            // 'receivedBy' can either be a host or a token. Since a token is a valid host, we only verify if the value
            // is a valid host.
             if (HttpRuleParser.GetHostLength(agent, 0, true, out string? host) != agent.Length)
            {
                throw new FormatException(SR.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, agent));
            }
        }
    }
}
