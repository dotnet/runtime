// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace System.Net.Http.Headers
{
    public class ViaHeaderValue : ICloneable
    {
        private string _protocolName;
        private string _protocolVersion;
        private string _receivedBy;
        private string _comment;

        public string ProtocolName
        {
            get { return _protocolName; }
        }

        public string ProtocolVersion
        {
            get { return _protocolVersion; }
        }

        public string ReceivedBy
        {
            get { return _receivedBy; }
        }

        public string Comment
        {
            get { return _comment; }
        }

        public ViaHeaderValue(string protocolVersion, string receivedBy)
            : this(protocolVersion, receivedBy, null, null)
        {
        }

        public ViaHeaderValue(string protocolVersion, string receivedBy, string protocolName)
            : this(protocolVersion, receivedBy, protocolName, null)
        {
        }

        public ViaHeaderValue(string protocolVersion, string receivedBy, string protocolName, string comment)
        {
            HeaderUtilities.CheckValidToken(protocolVersion, nameof(protocolVersion));
            CheckReceivedBy(receivedBy);

            if (!string.IsNullOrEmpty(protocolName))
            {
                HeaderUtilities.CheckValidToken(protocolName, nameof(protocolName));
                _protocolName = protocolName;
            }

            if (!string.IsNullOrEmpty(comment))
            {
                HeaderUtilities.CheckValidComment(comment, nameof(comment));
                _comment = comment;
            }

            _protocolVersion = protocolVersion;
            _receivedBy = receivedBy;
        }

        private ViaHeaderValue()
        {
        }

        private ViaHeaderValue(ViaHeaderValue source)
        {
            Debug.Assert(source != null);

            _protocolName = source._protocolName;
            _protocolVersion = source._protocolVersion;
            _receivedBy = source._receivedBy;
            _comment = source._comment;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(_protocolName))
            {
                sb.Append(_protocolName);
                sb.Append('/');
            }

            sb.Append(_protocolVersion);
            sb.Append(' ');
            sb.Append(_receivedBy);

            if (!string.IsNullOrEmpty(_comment))
            {
                sb.Append(' ');
                sb.Append(_comment);
            }

            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            ViaHeaderValue other = obj as ViaHeaderValue;

            if (other == null)
            {
                return false;
            }

            // Note that for token and host case-insensitive comparison is used. Comments are compared using case-
            // sensitive comparison.
            return string.Equals(_protocolVersion, other._protocolVersion, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(_receivedBy, other._receivedBy, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(_protocolName, other._protocolName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(_comment, other._comment, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            int result = StringComparer.OrdinalIgnoreCase.GetHashCode(_protocolVersion) ^
                StringComparer.OrdinalIgnoreCase.GetHashCode(_receivedBy);

            if (!string.IsNullOrEmpty(_protocolName))
            {
                result = result ^ StringComparer.OrdinalIgnoreCase.GetHashCode(_protocolName);
            }

            if (!string.IsNullOrEmpty(_comment))
            {
                result = result ^ _comment.GetHashCode();
            }

            return result;
        }

        public static ViaHeaderValue Parse(string input)
        {
            int index = 0;
            return (ViaHeaderValue)GenericHeaderParser.SingleValueViaParser.ParseValue(input, null, ref index);
        }

        public static bool TryParse(string input, out ViaHeaderValue parsedValue)
        {
            int index = 0;
            object output;
            parsedValue = null;

            if (GenericHeaderParser.SingleValueViaParser.TryParseValue(input, null, ref index, out output))
            {
                parsedValue = (ViaHeaderValue)output;
                return true;
            }
            return false;
        }

        internal static int GetViaLength(string input, int startIndex, out object parsedValue)
        {
            Debug.Assert(startIndex >= 0);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Read <protocolName> and <protocolVersion> in '[<protocolName>/]<protocolVersion> <receivedBy> [<comment>]'
            string protocolName = null;
            string protocolVersion = null;
            int current = GetProtocolEndIndex(input, startIndex, out protocolName, out protocolVersion);

            // If we reached the end of the string after reading protocolName/Version we return (we expect at least
            // <receivedBy> to follow). If reading protocolName/Version read 0 bytes, we return. 
            if ((current == startIndex) || (current == input.Length))
            {
                return 0;
            }
            Debug.Assert(protocolVersion != null);

            // Read <receivedBy> in '[<protocolName>/]<protocolVersion> <receivedBy> [<comment>]'
            string receivedBy = null;
            int receivedByLength = HttpRuleParser.GetHostLength(input, current, true, out receivedBy);

            if (receivedByLength == 0)
            {
                return 0;
            }

            current = current + receivedByLength;
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            string comment = null;
            if ((current < input.Length) && (input[current] == '('))
            {
                // We have a <comment> in '[<protocolName>/]<protocolVersion> <receivedBy> [<comment>]'
                int commentLength = 0;
                if (HttpRuleParser.GetCommentLength(input, current, out commentLength) != HttpParseResult.Parsed)
                {
                    return 0; // We found a '(' character but it wasn't a valid comment. Abort.
                }

                comment = input.Substring(current, commentLength);

                current = current + commentLength;
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);
            }

            ViaHeaderValue result = new ViaHeaderValue();
            result._protocolVersion = protocolVersion;
            result._protocolName = protocolName;
            result._receivedBy = receivedBy;
            result._comment = comment;

            parsedValue = result;
            return current - startIndex;
        }

        private static int GetProtocolEndIndex(string input, int startIndex, out string protocolName,
            out string protocolVersion)
        {
            // We have a string of the form '[<protocolName>/]<protocolVersion> <receivedBy> [<comment>]'. The first
            // token may either be the protocol name or protocol version. We'll only find out after reading the token
            // and by looking at the following character: If it is a '/' we just parsed the protocol name, otherwise
            // the protocol version.
            protocolName = null;
            protocolVersion = null;

            int current = startIndex;
            int protocolVersionOrNameLength = HttpRuleParser.GetTokenLength(input, current);

            if (protocolVersionOrNameLength == 0)
            {
                return 0;
            }

            current = startIndex + protocolVersionOrNameLength;
            int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
            current = current + whitespaceLength;

            if (current == input.Length)
            {
                return 0;
            }

            if (input[current] == '/')
            {
                // We parsed the protocol name
                protocolName = input.Substring(startIndex, protocolVersionOrNameLength);

                current++; // skip the '/' delimiter
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);

                protocolVersionOrNameLength = HttpRuleParser.GetTokenLength(input, current);

                if (protocolVersionOrNameLength == 0)
                {
                    return 0; // We have a string "<token>/" followed by non-token chars. This is invalid.
                }

                protocolVersion = input.Substring(current, protocolVersionOrNameLength);

                current = current + protocolVersionOrNameLength;
                whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
                current = current + whitespaceLength;
            }
            else
            {
                protocolVersion = input.Substring(startIndex, protocolVersionOrNameLength);
            }

            if (whitespaceLength == 0)
            {
                return 0; // We were able to parse [<protocolName>/]<protocolVersion> but it wasn't followed by a WS
            }

            return current;
        }

        object ICloneable.Clone()
        {
            return new ViaHeaderValue(this);
        }

        private static void CheckReceivedBy(string receivedBy)
        {
            if (string.IsNullOrEmpty(receivedBy))
            {
                throw new ArgumentException(SR.net_http_argument_empty_string, nameof(receivedBy));
            }

            // 'receivedBy' can either be a host or a token. Since a token is a valid host, we only verify if the value
            // is a valid host.
            string host = null;
            if (HttpRuleParser.GetHostLength(receivedBy, 0, true, out host) != receivedBy.Length)
            {
                throw new FormatException(string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, receivedBy));
            }
        }
    }
}
