// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Net.Http.Headers
{
    /// <summary>
    /// Parses Alt-Svc header values, per RFC 7838 Section 3.
    /// </summary>
    internal sealed class AltSvcHeaderParser : BaseHeaderParser
    {
        internal const long DefaultMaxAgeTicks = 24 * TimeSpan.TicksPerHour;

        public static AltSvcHeaderParser Parser { get; } = new AltSvcHeaderParser();

        private AltSvcHeaderParser()
            : base(supportsMultipleValues: true)
        {
        }

        protected override int GetParsedValueLength(string value, int startIndex, object? storeValue,
            out object? parsedValue)
        {
            Debug.Assert(startIndex >= 0);
            Debug.Assert(startIndex < value.Length);

            if (string.IsNullOrEmpty(value))
            {
                parsedValue = null;
                return 0;
            }

            int idx = startIndex;

            if (!TryReadPercentEncodedAlpnProtocolName(value, idx, out string? alpnProtocolName, out int alpnProtocolNameLength))
            {
                parsedValue = null;
                return 0;
            }

            idx += alpnProtocolNameLength;

            if (alpnProtocolName == "clear")
            {
                if (idx != value.Length)
                {
                    // Clear has no parameters and should be the only Alt-Svc value present, so there should be nothing after it.
                    parsedValue = null;
                    return 0;
                }

                parsedValue = AltSvcHeaderValue.Clear;
                return idx - startIndex;
            }

            if (idx == value.Length || value[idx++] != '=')
            {
                parsedValue = null;
                return 0;
            }

            if (!TryReadQuotedAltAuthority(value, idx, out string? altAuthorityHost, out int altAuthorityPort, out int altAuthorityLength))
            {
                parsedValue = null;
                return 0;
            }
            idx += altAuthorityLength;

            // Parse parameters: *( OWS ";" OWS parameter )
            int? maxAge = null;
            bool persist = false;

            while (idx != value.Length)
            {
                // Skip OWS before semicolon.
                while (idx != value.Length && IsOptionalWhiteSpace(value[idx])) ++idx;

                if (idx == value.Length)
                {
                    parsedValue = null;
                    return 0;
                }

                char ch = value[idx];

                if (ch == ',')
                {
                    // Multi-value header: return this value; will get called again to parse the next.
                    break;
                }

                if (ch != ';')
                {
                    // Expecting parameters starting with semicolon; fail out.
                    parsedValue = null;
                    return 0;
                }

                ++idx;

                // Skip OWS after semicolon / before value.
                while (idx != value.Length && IsOptionalWhiteSpace(value[idx])) ++idx;

                // Get the parameter key length.
                int tokenLength = HttpRuleParser.GetTokenLength(value, idx);
                if (tokenLength == 0)
                {
                    parsedValue = null;
                    return 0;
                }

                if ((idx + tokenLength) >= value.Length || value[idx + tokenLength] != '=')
                {
                    parsedValue = null;
                    return 0;
                }

                if (tokenLength == 2 && value[idx] == 'm' && value[idx + 1] == 'a')
                {
                    // Parse "ma" (Max Age).

                    idx += 3; // Skip "ma="
                    if (!TryReadTokenOrQuotedInt32(value, idx, out int maxAgeTmp, out int parameterLength))
                    {
                        parsedValue = null;
                        return 0;
                    }

                    if (maxAge == null)
                    {
                        maxAge = maxAgeTmp;
                    }
                    else
                    {
                        // RFC makes it unclear what to do if a duplicate parameter is found. For now, take the minimum.
                        maxAge = Math.Min(maxAge.GetValueOrDefault(), maxAgeTmp);
                    }

                    idx += parameterLength;
                }
                else if (value.AsSpan(idx).StartsWith("persist="))
                {
                    idx += 8; // Skip "persist="
                    if (TryReadTokenOrQuotedInt32(value, idx, out int persistInt, out int parameterLength))
                    {
                        persist = persistInt == 1;
                    }
                    else if (!TrySkipTokenOrQuoted(value, idx, out parameterLength))
                    {
                        // Cold path: unsupported value, just skip the parameter.
                        parsedValue = null;
                        return 0;
                    }

                    idx += parameterLength;
                }
                else
                {
                    // Some unknown parameter. Skip it.

                    idx += tokenLength + 1;
                    if (!TrySkipTokenOrQuoted(value, idx, out int parameterLength))
                    {
                        parsedValue = null;
                        return 0;
                    }
                    idx += parameterLength;
                }
            }

            // If no "ma" parameter present, use the default.
            TimeSpan maxAgeTimeSpan = TimeSpan.FromTicks(maxAge * TimeSpan.TicksPerSecond ?? DefaultMaxAgeTicks);

            parsedValue = new AltSvcHeaderValue(alpnProtocolName, altAuthorityHost, altAuthorityPort, maxAgeTimeSpan, persist);
            return idx - startIndex;
        }

        private static bool IsOptionalWhiteSpace(char ch)
        {
            return ch == ' ' || ch == '\t';
        }

        private static bool TryReadPercentEncodedAlpnProtocolName(string value, int startIndex, [NotNullWhen(true)] out string? result, out int readLength)
        {
            int tokenLength = HttpRuleParser.GetTokenLength(value, startIndex);

            if (tokenLength == 0)
            {
                result = null;
                readLength = 0;
                return false;
            }

            ReadOnlySpan<char> span = value.AsSpan(startIndex, tokenLength);

            readLength = tokenLength;

            // Special-case expected values to avoid allocating one-off strings.
            switch (span.Length)
            {
                case 2:
                    if (span[0] == 'h')
                    {
                        char ch = span[1];
                        if (ch == '3')
                        {
                            result = "h3";
                            return true;
                        }
                        if (ch == '2')
                        {
                            result = "h2";
                            return true;
                        }
                    }
                    break;
                case 3:
                    if (span[0] == 'h' && span[1] == '2' && span[2] == 'c')
                    {
                        result = "h2c";
                        readLength = 3;
                        return true;
                    }
                    break;
                case 5:
                    if (span.SequenceEqual("clear"))
                    {
                        result = "clear";
                        return true;
                    }
                    break;
                case 10:
                    if (span.StartsWith("http%2F1."))
                    {
                        char ch = span[9];
                        if (ch == '1')
                        {
                            result = "http/1.1";
                            return true;
                        }
                        if (ch == '0')
                        {
                            result = "http/1.0";
                            return true;
                        }
                    }
                    break;
            }

            // Unrecognized ALPN protocol name. Percent-decode.
            return TryReadUnknownPercentEncodedAlpnProtocolName(span, out result);
        }

        private static bool TryReadUnknownPercentEncodedAlpnProtocolName(ReadOnlySpan<char> value, [NotNullWhen(true)] out string? result)
        {
            int idx = value.IndexOf('%');

            if (idx == -1)
            {
                result = new string(value);
                return true;
            }

            var builder = new ValueStringBuilder(value.Length <= 128 ? stackalloc char[128] : new char[value.Length]);

            do
            {
                if (idx != 0)
                {
                    builder.Append(value.Slice(0, idx));
                }

                if ((value.Length - idx) < 3 || !TryReadAlpnHexDigit(value[1], out int hi) || !TryReadAlpnHexDigit(value[2], out int lo))
                {
                    result = null;
                    return false;
                }

                builder.Append((char)((hi << 8) | lo));

                value = value.Slice(idx + 3);
                idx = value.IndexOf('%');
            }
            while (idx != -1);

            if (value.Length != 0)
            {
                builder.Append(value);
            }

            result = builder.ToString();
            return true;
        }

        /// <summary>
        /// Reads a hex nibble. Specialized for ALPN protocol names as they explicitly can not contain lower-case hex.
        /// </summary>
        private static bool TryReadAlpnHexDigit(char ch, out int nibble)
        {
            int result = HexConverter.FromUpperChar(ch);
            if (result == 0xFF)
            {
                nibble = 0;
                return false;
            }

            nibble = result;
            return true;
        }

        private static bool TryReadQuotedAltAuthority(string value, int startIndex, out string? host, out int port, out int readLength)
        {
            if (HttpRuleParser.GetQuotedStringLength(value, startIndex, out int quotedLength) != HttpParseResult.Parsed)
            {
                goto parseError;
            }

            Debug.Assert(value[startIndex] == '"' && value[startIndex + quotedLength - 1] == '"', $"{nameof(HttpRuleParser.GetQuotedStringLength)} should return {nameof(HttpParseResult.NotParsed)} if the opening/closing quotes are missing.");
            ReadOnlySpan<char> quoted = value.AsSpan(startIndex + 1, quotedLength - 2);

            int idx = quoted.IndexOf(':');
            if (idx == -1)
            {
                goto parseError;
            }

            // Parse the port. Port comes at the end of the string, but do this first so we don't allocate a host string if port fails to parse.
            if (!TryReadQuotedInt32Value(quoted.Slice(idx + 1), out port))
            {
                goto parseError;
            }

            // Parse the optional host.
            if (idx == 0)
            {
                host = null;
            }
            else if (!TryReadQuotedValue(quoted.Slice(0, idx), out host))
            {
                goto parseError;
            }

            readLength = quotedLength;
            return true;

        parseError:
            host = null;
            port = 0;
            readLength = 0;
            return false;
        }

        private static bool TryReadQuotedValue(ReadOnlySpan<char> value, out string? result)
        {
            int idx = value.IndexOf('\\');

            if (idx == -1)
            {
                // Hostnames shouldn't require quoted pairs, so this should be the hot path.
                result = value.Length != 0 ? new string(value) : null;
                return true;
            }

            var builder = new ValueStringBuilder(stackalloc char[128]);

            do
            {
                if (idx + 1 == value.Length)
                {
                    // quoted-pair requires two characters: the quote, and the quoted character.
                    builder.Dispose();
                    result = null;
                    return false;
                }

                if (idx != 0)
                {
                    builder.Append(value.Slice(0, idx));
                }

                builder.Append(value[idx + 1]);

                value = value.Slice(idx + 2);
                idx = value.IndexOf('\\');
            }
            while (idx != -1);

            if (value.Length != 0)
            {
                builder.Append(value);
            }

            result = builder.ToString();
            return true;
        }

        private static bool TryReadTokenOrQuotedInt32(string value, int startIndex, out int result, out int readLength)
        {
            if (startIndex >= value.Length)
            {
                result = 0;
                readLength = 0;
                return false;
            }

            if (HttpRuleParser.IsTokenChar(value[startIndex]))
            {
                // No reason for integers to be quoted, so this should be the hot path.

                int tokenLength = HttpRuleParser.GetTokenLength(value, startIndex);

                readLength = tokenLength;
                return HeaderUtilities.TryParseInt32(value, startIndex, tokenLength, out result);
            }

            if (HttpRuleParser.GetQuotedStringLength(value, startIndex, out int quotedLength) == HttpParseResult.Parsed)
            {
                readLength = quotedLength;
                return TryReadQuotedInt32Value(value.AsSpan(1, quotedLength - 2), out result);
            }

            result = 0;
            readLength = 0;
            return false;
        }

        private static bool TryReadQuotedInt32Value(ReadOnlySpan<char> value, out int result)
        {
            if (value.Length == 0)
            {
                result = 0;
                return false;
            }

            int port = 0;

            foreach (char ch in value)
            {
                // The port shouldn't ever need a quoted-pair, but they're still valid... skip if found.
                if (ch == '\\') continue;

                if ((uint)(ch - '0') > '9' - '0') // ch < '0' || ch > '9'
                {
                    result = 0;
                    return false;
                }

                long portTmp = port * 10L + (ch - '0');

                if (portTmp > int.MaxValue)
                {
                    result = 0;
                    return false;
                }

                port = (int)portTmp;
            }

            result = port;
            return true;
        }

        private static bool TrySkipTokenOrQuoted(string value, int startIndex, out int readLength)
        {
            if (startIndex >= value.Length)
            {
                readLength = 0;
                return false;
            }

            if (HttpRuleParser.IsTokenChar(value[startIndex]))
            {
                readLength = HttpRuleParser.GetTokenLength(value, startIndex);
                return true;
            }

            if (HttpRuleParser.GetQuotedStringLength(value, startIndex, out int quotedLength) == HttpParseResult.Parsed)
            {
                readLength = quotedLength;
                return true;
            }

            readLength = 0;
            return false;
        }
    }
}
