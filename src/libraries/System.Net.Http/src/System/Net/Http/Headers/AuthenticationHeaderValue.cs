// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http.Headers
{
    public class AuthenticationHeaderValue : ICloneable
    {
        private readonly string _scheme;
        private readonly string? _parameter;

        public string Scheme
        {
            get { return _scheme; }
        }

        // We simplify parameters by just considering them one string. The caller is responsible for correctly parsing
        // the string.
        // The reason is that we can't determine the format of parameters. According to Errata 1959 in RFC 2617
        // parameters can be "token", "quoted-string", or "#auth-param" where "auth-param" is defined as
        // "token "=" ( token | quoted-string )". E.g. take the following BASIC example:
        // Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==
        // Due to Base64 encoding we have two final "=". The value is neither a token nor a quoted-string, so it must
        // be an auth-param according to the RFC definition. But that's also incorrect: auth-param means that we
        // consider the value before the first "=" as "name" and the final "=" as "value".
        public string? Parameter
        {
            get { return _parameter; }
        }

        public AuthenticationHeaderValue(string scheme)
            : this(scheme, null)
        {
        }

        public AuthenticationHeaderValue(string scheme, string? parameter)
        {
            HeaderUtilities.CheckValidToken(scheme, nameof(scheme));
            HttpHeaders.CheckContainsNewLine(parameter);
            _scheme = scheme;
            _parameter = parameter;
        }

        private AuthenticationHeaderValue(AuthenticationHeaderValue source)
        {
            Debug.Assert(source != null);

            _scheme = source._scheme;
            _parameter = source._parameter;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(_parameter))
            {
                return _scheme;
            }
            return _scheme + " " + _parameter;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            AuthenticationHeaderValue? other = obj as AuthenticationHeaderValue;

            if (other == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(_parameter) && string.IsNullOrEmpty(other._parameter))
            {
                return (string.Equals(_scheme, other._scheme, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                // Since we can't parse the parameter, we use case-sensitive comparison.
                return string.Equals(_scheme, other._scheme, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(_parameter, other._parameter, StringComparison.Ordinal);
            }
        }

        public override int GetHashCode()
        {
            int result = StringComparer.OrdinalIgnoreCase.GetHashCode(_scheme);

            if (!string.IsNullOrEmpty(_parameter))
            {
                result = result ^ _parameter.GetHashCode();
            }

            return result;
        }

        public static AuthenticationHeaderValue Parse(string? input)
        {
            int index = 0;
            return (AuthenticationHeaderValue)GenericHeaderParser.SingleValueAuthenticationParser.ParseValue(
                input, null, ref index);
        }

        public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out AuthenticationHeaderValue? parsedValue)
        {
            int index = 0;
            parsedValue = null;

            if (GenericHeaderParser.SingleValueAuthenticationParser.TryParseValue(input, null, ref index, out object? output))
            {
                parsedValue = (AuthenticationHeaderValue)output!;
                return true;
            }
            return false;
        }

        internal static int GetAuthenticationLength(string? input, int startIndex, out object? parsedValue)
        {
            Debug.Assert(startIndex >= 0);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Parse the scheme string: <scheme> in '<scheme> <parameter>'
            int schemeLength = HttpRuleParser.GetTokenLength(input, startIndex);

            if (schemeLength == 0)
            {
                return 0;
            }

            string? targetScheme = null;
            switch (schemeLength)
            {
                // Avoid allocating a scheme string for the most common cases.
                case 5: targetScheme = "Basic"; break;
                case 6: targetScheme = "Digest"; break;
                case 4: targetScheme = "NTLM"; break;
                case 9: targetScheme = "Negotiate"; break;
            }

            string scheme = targetScheme != null && string.CompareOrdinal(input, startIndex, targetScheme, 0, schemeLength) == 0 ?
                targetScheme :
                input.Substring(startIndex, schemeLength);

            int current = startIndex + schemeLength;
            int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
            current = current + whitespaceLength;

            if ((current == input.Length) || (input[current] == ','))
            {
                // If we only have a scheme followed by whitespace, we're done.
                parsedValue = new AuthenticationHeaderValue(scheme);
                return current - startIndex;
            }

            // We need at least one space between the scheme and parameters. If there is no whitespace, then we must
            // have reached the end of the string (i.e. scheme-only string).
            if (whitespaceLength == 0)
            {
                return 0;
            }

            // If we get here, we have a <scheme> followed by a whitespace. Now we expect the following:
            // '<scheme> <blob>[,<name>=<value>]*[, <otherscheme>...]*': <blob> potentially contains one
            // or more '=' characters, optionally followed by additional name/value pairs, optionally followed by
            // other schemes. <blob> may be a quoted string.
            // We look at the value after ',': if it is <token>=<value> then we have a parameter for <scheme>.
            // If we have either a <token>-only or <token><whitespace><blob> then we have another scheme.
            int parameterStartIndex = current;
            int parameterEndIndex = current;
            if (!TrySkipFirstBlob(input, ref current, ref parameterEndIndex))
            {
                return 0;
            }

            if (current < input.Length)
            {
                if (!TryGetParametersEndIndex(input, ref current, ref parameterEndIndex))
                {
                    return 0;
                }
            }

            string parameter = input.Substring(parameterStartIndex, parameterEndIndex - parameterStartIndex + 1);
            parsedValue = new AuthenticationHeaderValue(scheme, parameter);
            return current - startIndex;
        }

        private static bool TrySkipFirstBlob(string input, ref int current, ref int parameterEndIndex)
        {
            // Find the delimiter: Note that <blob> in "<scheme> <blob>" may be a token, quoted string, name/value
            // pair or a Base64 encoded string. So make sure that we don't consider ',' characters within a quoted
            // string as delimiter.
            while ((current < input.Length) && (input[current] != ','))
            {
                if (input[current] == '"')
                {
                    int quotedStringLength = 0;
                    if (HttpRuleParser.GetQuotedStringLength(input, current, out quotedStringLength) !=
                        HttpParseResult.Parsed)
                    {
                        // We have a quote but an invalid quoted-string.
                        return false;
                    }
                    current = current + quotedStringLength;
                    parameterEndIndex = current - 1; // -1 because 'current' points to the char after the final '"'
                }
                else
                {
                    int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);

                    // We don't want trailing whitespace to be considered part of the parameter blob. Increment
                    // 'parameterEndIndex' only if we don't have a whitespace. E.g. "Basic AbC=  , NTLM" should return
                    // "AbC=" as parameter ignoring the spaces before ','.
                    if (whitespaceLength == 0)
                    {
                        parameterEndIndex = current;
                        current++;
                    }
                    else
                    {
                        current = current + whitespaceLength;
                    }
                }
            }

            return true;
        }

        private static bool TryGetParametersEndIndex(string input, ref int parseEndIndex, ref int parameterEndIndex)
        {
            Debug.Assert(parseEndIndex < input.Length, "Expected string to have at least 1 char");
            Debug.Assert(input[parseEndIndex] == ',');

            int current = parseEndIndex;
            do
            {
                current++; // skip ',' delimiter

                bool separatorFound = false; // ignore value returned by GetNextNonEmptyOrWhitespaceIndex()
                current = HeaderUtilities.GetNextNonEmptyOrWhitespaceIndex(input, current, true, out separatorFound);
                if (current == input.Length)
                {
                    return true;
                }

                // Now we have to determine if after ',' we have a list of <name>=<value> pairs that are part of
                // the auth scheme parameters OR if we have another auth scheme. Either way, after ',' we expect a
                // valid token that is either the <name> in a <name>=<value> pair OR <scheme> of another scheme.
                int tokenLength = HttpRuleParser.GetTokenLength(input, current);
                if (tokenLength == 0)
                {
                    return false;
                }

                current = current + tokenLength;
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);

                // If we reached the end of the string or the token is followed by anything but '=', then the parsed
                // token is another scheme name. The string representing parameters ends before the token (e.g.
                // "Digest a=b, c=d, NTLM": return scheme "Digest" with parameters string "a=b, c=d").
                if ((current == input.Length) || (input[current] != '='))
                {
                    return true;
                }

                current++; // skip '=' delimiter
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);
                int valueLength = NameValueHeaderValue.GetValueLength(input, current);

                // After '<name>=' we expect a valid <value> (either token or quoted string)
                if (valueLength == 0)
                {
                    return false;
                }

                // Update parameter end index, since we just parsed a valid <name>=<value> pair that is part of the
                // parameters string.
                current = current + valueLength;
                parameterEndIndex = current - 1; // -1 because 'current' already points to the char after <value>
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);
                parseEndIndex = current; // this essentially points to parameterEndIndex + whitespace + next char
            } while ((current < input.Length) && (input[current] == ','));

            return true;
        }

        object ICloneable.Clone()
        {
            return new AuthenticationHeaderValue(this);
        }
    }
}
