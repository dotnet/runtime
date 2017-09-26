// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Net.Mail;
using System.Text;

namespace System.Net.Http.Headers
{
    internal static class HeaderUtilities
    {
        private const string qualityName = "q";

        internal const string ConnectionClose = "close";
        internal static readonly TransferCodingHeaderValue TransferEncodingChunked =
            new TransferCodingHeaderValue("chunked");
        internal static readonly NameValueWithParametersHeaderValue ExpectContinue =
            new NameValueWithParametersHeaderValue("100-continue");

        internal const string BytesUnit = "bytes";

        // Validator
        internal static readonly Action<HttpHeaderValueCollection<string>, string> TokenValidator = ValidateToken;

        private static readonly char[] s_hexUpperChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        internal static void SetQuality(ObjectCollection<NameValueHeaderValue> parameters, double? value)
        {
            Debug.Assert(parameters != null);

            NameValueHeaderValue qualityParameter = NameValueHeaderValue.Find(parameters, qualityName);
            if (value.HasValue)
            {
                // Note that even if we check the value here, we can't prevent a user from adding an invalid quality
                // value using Parameters.Add(). Even if we would prevent the user from adding an invalid value
                // using Parameters.Add() he could always add invalid values using HttpHeaders.AddWithoutValidation().
                // So this check is really for convenience to show users that they're trying to add an invalid 
                // value.
                if ((value < 0) || (value > 1))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                string qualityString = ((double)value).ToString("0.0##", NumberFormatInfo.InvariantInfo);
                if (qualityParameter != null)
                {
                    qualityParameter.Value = qualityString;
                }
                else
                {
                    parameters.Add(new NameValueHeaderValue(qualityName, qualityString));
                }
            }
            else
            {
                // Remove quality parameter
                if (qualityParameter != null)
                {
                    parameters.Remove(qualityParameter);
                }
            }
        }

        // Encode a string using RFC 5987 encoding.
        // encoding'lang'PercentEncodedSpecials
        internal static string Encode5987(string input)
        {
            string output;
            IsInputEncoded5987(input, out output);

            return output;
        }

        internal static bool IsInputEncoded5987(string input, out string output)
        {
            // Encode a string using RFC 5987 encoding.
            // encoding'lang'PercentEncodedSpecials
            bool wasEncoded = false;
            StringBuilder builder = StringBuilderCache.Acquire();
            builder.Append("utf-8\'\'");
            foreach (char c in input)
            {
                // attr-char = ALPHA / DIGIT / "!" / "#" / "$" / "&" / "+" / "-" / "." / "^" / "_" / "`" / "|" / "~"
                //      ; token except ( "*" / "'" / "%" )
                if (c > 0x7F) // Encodes as multiple utf-8 bytes
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(c.ToString());
                    foreach (byte b in bytes)
                    {
                        AddHexEscaped((char)b, builder);
                        wasEncoded = true;
                    }
                }
                else if (!HttpRuleParser.IsTokenChar(c) || c == '*' || c == '\'' || c == '%')
                {
                    // ASCII - Only one encoded byte.
                    AddHexEscaped(c, builder);
                    wasEncoded = true;
                }
                else
                {
                    builder.Append(c);
                }

            }

            output = StringBuilderCache.GetStringAndRelease(builder);
            return wasEncoded;
        }

        /// <summary>Transforms an ASCII character into its hexadecimal representation, adding the characters to a StringBuilder.</summary>
        private static void AddHexEscaped(char c, StringBuilder destination)
        {
            Debug.Assert(destination != null);
            Debug.Assert(c <= 0xFF);

            destination.Append('%');
            destination.Append(s_hexUpperChars[(c & 0xf0) >> 4]);
            destination.Append(s_hexUpperChars[c & 0xf]);
        }

        internal static double? GetQuality(ObjectCollection<NameValueHeaderValue> parameters)
        {
            Debug.Assert(parameters != null);

            NameValueHeaderValue qualityParameter = NameValueHeaderValue.Find(parameters, qualityName);
            if (qualityParameter != null)
            {
                // Note that the RFC requires decimal '.' regardless of the culture. I.e. using ',' as decimal
                // separator is considered invalid (even if the current culture would allow it).
                double qualityValue = 0;
                if (double.TryParse(qualityParameter.Value, NumberStyles.AllowDecimalPoint,
                    NumberFormatInfo.InvariantInfo, out qualityValue))
                {
                    return qualityValue;
                }
                // If the stored value is an invalid quality value, just return null and log a warning. 
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, SR.Format(SR.net_http_log_headers_invalid_quality, qualityParameter.Value));
            }
            return null;
        }

        internal static void CheckValidToken(string value, string parameterName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(SR.net_http_argument_empty_string, parameterName);
            }

            if (HttpRuleParser.GetTokenLength(value, 0) != value.Length)
            {
                throw new FormatException(string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, value));
            }
        }

        internal static void CheckValidComment(string value, string parameterName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(SR.net_http_argument_empty_string, parameterName);
            }

            int length = 0;
            if ((HttpRuleParser.GetCommentLength(value, 0, out length) != HttpParseResult.Parsed) ||
                (length != value.Length)) // no trailing spaces allowed
            {
                throw new FormatException(string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, value));
            }
        }

        internal static void CheckValidQuotedString(string value, string parameterName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(SR.net_http_argument_empty_string, parameterName);
            }

            int length = 0;
            if ((HttpRuleParser.GetQuotedStringLength(value, 0, out length) != HttpParseResult.Parsed) ||
                (length != value.Length)) // no trailing spaces allowed
            {
                throw new FormatException(string.Format(System.Globalization.CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, value));
            }
        }

        internal static bool AreEqualCollections<T>(ObjectCollection<T> x, ObjectCollection<T> y) where T : class
        {
            return AreEqualCollections(x, y, null);
        }

        internal static bool AreEqualCollections<T>(ObjectCollection<T> x, ObjectCollection<T> y, IEqualityComparer<T> comparer) where T : class
        {
            if (x == null)
            {
                return (y == null) || (y.Count == 0);
            }

            if (y == null)
            {
                return (x.Count == 0);
            }

            if (x.Count != y.Count)
            {
                return false;
            }

            if (x.Count == 0)
            {
                return true;
            }

            // We have two unordered lists. So comparison is an O(n*m) operation which is expensive. Usually
            // headers have 1-2 parameters (if any), so this comparison shouldn't be too expensive.
            bool[] alreadyFound = new bool[x.Count];
            int i = 0;
            foreach (var xItem in x)
            {
                Debug.Assert(xItem != null);

                i = 0;
                bool found = false;
                foreach (var yItem in y)
                {
                    if (!alreadyFound[i])
                    {
                        if (((comparer == null) && xItem.Equals(yItem)) ||
                            ((comparer != null) && comparer.Equals(xItem, yItem)))
                        {
                            alreadyFound[i] = true;
                            found = true;
                            break;
                        }
                    }
                    i++;
                }

                if (!found)
                {
                    return false;
                }
            }

            // Since we never re-use a "found" value in 'y', we expect 'alreadyFound' to have all fields set to 'true'.
            // Otherwise the two collections can't be equal and we should not get here.
            Debug.Assert(Contract.ForAll(alreadyFound, value => { return value; }),
                "Expected all values in 'alreadyFound' to be true since collections are considered equal.");

            return true;
        }

        internal static int GetNextNonEmptyOrWhitespaceIndex(string input, int startIndex, bool skipEmptyValues,
            out bool separatorFound)
        {
            Debug.Assert(input != null);
            Debug.Assert(startIndex <= input.Length); // it's OK if index == value.Length.

            separatorFound = false;
            int current = startIndex + HttpRuleParser.GetWhitespaceLength(input, startIndex);

            if ((current == input.Length) || (input[current] != ','))
            {
                return current;
            }

            // If we have a separator, skip the separator and all following whitespace. If we support
            // empty values, continue until the current character is neither a separator nor a whitespace.
            separatorFound = true;
            current++; // skip delimiter.
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            if (skipEmptyValues)
            {
                while ((current < input.Length) && (input[current] == ','))
                {
                    current++; // skip delimiter.
                    current = current + HttpRuleParser.GetWhitespaceLength(input, current);
                }
            }

            return current;
        }

        internal static DateTimeOffset? GetDateTimeOffsetValue(HeaderDescriptor descriptor, HttpHeaders store)
        {
            Debug.Assert(store != null);

            object storedValue = store.GetParsedValues(descriptor);
            if (storedValue != null)
            {
                return (DateTimeOffset)storedValue;
            }
            return null;
        }

        internal static TimeSpan? GetTimeSpanValue(HeaderDescriptor descriptor, HttpHeaders store)
        {
            Debug.Assert(store != null);

            object storedValue = store.GetParsedValues(descriptor);
            if (storedValue != null)
            {
                return (TimeSpan)storedValue;
            }
            return null;
        }

        internal static bool TryParseInt32(string value, out int result) =>
            TryParseInt32(value, 0, value.Length, out result);

        internal static bool TryParseInt32(string value, int offset, int length, out int result) // TODO #21281: Replace with int.TryParse(Span<char>) once it's available
        {
            if (offset < 0 || length < 0 || offset > value.Length - length)
            {
                result = 0;
                return false;
            }

            int tmpResult = 0;
            int pos = offset, endPos = offset + length;
            while (pos < endPos)
            {
                char c = value[pos++];
                int digit = c - '0';
                if ((uint)digit > 9 || // invalid digit
                    tmpResult > int.MaxValue / 10 || // will overflow when shifting digits
                    (tmpResult == int.MaxValue / 10 && digit > 7)) // will overflow when adding in digit
                {
                    result = 0;
                    return false;
                }
                tmpResult = (tmpResult * 10) + digit;
            }

            result = tmpResult;
            return true;
        }

        internal static bool TryParseInt64(string value, int offset, int length, out long result) // TODO #21281: Replace with int.TryParse(Span<char>) once it's available
        {
            if (offset < 0 || length < 0 || offset > value.Length - length)
            {
                result = 0;
                return false;
            }

            long tmpResult = 0;
            int pos = offset, endPos = offset + length;
            while (pos < endPos)
            {
                char c = value[pos++];
                int digit = c - '0';
                if ((uint)digit > 9 || // invalid digit
                    tmpResult > long.MaxValue / 10 || // will overflow when shifting digits
                    (tmpResult == long.MaxValue / 10 && digit > 7)) // will overflow when adding in digit
                {
                    result = 0;
                    return false;
                }
                tmpResult = (tmpResult * 10) + digit;
            }

            result = tmpResult;
            return true;
        }

        internal static string DumpHeaders(params HttpHeaders[] headers)
        {
            // Return all headers as string similar to: 
            // {
            //    HeaderName1: Value1
            //    HeaderName1: Value2
            //    HeaderName2: Value1
            //    ...
            // }
            StringBuilder sb = new StringBuilder();
            sb.Append("{\r\n");

            for (int i = 0; i < headers.Length; i++)
            {
                if (headers[i] != null)
                {
                    foreach (var header in headers[i])
                    {
                        foreach (var headerValue in header.Value)
                        {
                            sb.Append("  ");
                            sb.Append(header.Key);
                            sb.Append(": ");
                            sb.Append(headerValue);
                            sb.Append("\r\n");
                        }
                    }
                }
            }

            sb.Append('}');

            return sb.ToString();
        }

        internal static bool IsValidEmailAddress(string value)
        {
            try
            {
#if uap
                new MailAddress(value);
#else
                MailAddressParser.ParseAddress(value);
#endif
                return true;
            }
            catch (FormatException e)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, SR.Format(SR.net_http_log_headers_wrong_email_format, value, e.Message));
            }
            return false;
        }

        private static void ValidateToken(HttpHeaderValueCollection<string> collection, string value)
        {
            CheckValidToken(value, "item");
        }
    }
}
