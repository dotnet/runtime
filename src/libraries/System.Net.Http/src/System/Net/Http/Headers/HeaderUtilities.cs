// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
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

        // attr-char = ALPHA / DIGIT / "!" / "#" / "$" / "&" / "+" / "-" / "." / "^" / "_" / "`" / "|" / "~"
        //      ; token except ( "*" / "'" / "%" )
        private static readonly SearchValues<byte> s_rfc5987AttrBytes =
            SearchValues.Create("!#$&+-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ^_`abcdefghijklmnopqrstuvwxyz|~"u8);

        internal static void SetQuality(UnvalidatedObjectCollection<NameValueHeaderValue> parameters, double? value)
        {
            Debug.Assert(parameters != null);

            NameValueHeaderValue? qualityParameter = NameValueHeaderValue.Find(parameters, qualityName);
            if (value.HasValue)
            {
                // Note that even if we check the value here, we can't prevent a user from adding an invalid quality
                // value using Parameters.Add(). Even if we would prevent the user from adding an invalid value
                // using Parameters.Add() they could always add invalid values using HttpHeaders.AddWithoutValidation().
                // So this check is really for convenience to show users that they're trying to add an invalid
                // value.
                double d = value.GetValueOrDefault();
                ArgumentOutOfRangeException.ThrowIfNegative(d);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(d, 1);

                string qualityString = d.ToString("0.0##", NumberFormatInfo.InvariantInfo);
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
            var builder = new ValueStringBuilder(stackalloc char[256]);
            byte[] utf8bytes = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(input.Length));
            int utf8length = Encoding.UTF8.GetBytes(input, 0, input.Length, utf8bytes, 0);

            builder.Append("utf-8\'\'");

            ReadOnlySpan<byte> utf8 = utf8bytes.AsSpan(0, utf8length);
            do
            {
                int length = utf8.IndexOfAnyExcept(s_rfc5987AttrBytes);
                if (length < 0)
                {
                    length = utf8.Length;
                }

                Encoding.ASCII.GetChars(utf8.Slice(0, length), builder.AppendSpan(length));

                utf8 = utf8.Slice(length);

                if (utf8.IsEmpty)
                {
                    break;
                }

                length = utf8.IndexOfAny(s_rfc5987AttrBytes);
                if (length < 0)
                {
                    length = utf8.Length;
                }

                foreach (byte b in utf8.Slice(0, length))
                {
                    AddHexEscaped(b, ref builder);
                }

                utf8 = utf8.Slice(length);
            }
            while (!utf8.IsEmpty);

            ArrayPool<byte>.Shared.Return(utf8bytes);

            return builder.ToString();
        }

        /// <summary>Transforms an ASCII character into its hexadecimal representation, adding the characters to a StringBuilder.</summary>
        private static void AddHexEscaped(byte c, ref ValueStringBuilder destination)
        {
            destination.Append('%');
            destination.Append(HexConverter.ToCharUpper(c >> 4));
            destination.Append(HexConverter.ToCharUpper(c));
        }

        internal static double? GetQuality(UnvalidatedObjectCollection<NameValueHeaderValue> parameters)
        {
            Debug.Assert(parameters != null);

            NameValueHeaderValue? qualityParameter = NameValueHeaderValue.Find(parameters, qualityName);
            if (qualityParameter != null)
            {
                // Note that the RFC requires decimal '.' regardless of the culture. I.e. using ',' as decimal
                // separator is considered invalid (even if the current culture would allow it).
                double qualityValue;
                if (double.TryParse(qualityParameter.Value, NumberStyles.AllowDecimalPoint,
                    NumberFormatInfo.InvariantInfo, out qualityValue))
                {
                    return qualityValue;
                }
                // If the stored value is an invalid quality value, just return null and log a warning.
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, SR.Format(SR.net_http_log_headers_invalid_quality, qualityParameter.Value));
            }
            return null;
        }

        internal static void CheckValidToken(string value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

            if (HttpRuleParser.GetTokenLength(value, 0) != value.Length)
            {
                throw new FormatException(SR.Format(CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, value));
            }
        }

        internal static void CheckValidComment(string value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

            int length;
            if ((HttpRuleParser.GetCommentLength(value, 0, out length) != HttpParseResult.Parsed) ||
                (length != value.Length)) // no trailing spaces allowed
            {
                throw new FormatException(SR.Format(CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, value));
            }
        }

        internal static void CheckValidQuotedString(string value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

            int length;
            if ((HttpRuleParser.GetQuotedStringLength(value, 0, out length) != HttpParseResult.Parsed) ||
                (length != value.Length)) // no trailing spaces allowed
            {
                throw new FormatException(SR.Format(CultureInfo.InvariantCulture, SR.net_http_headers_invalid_value, value));
            }
        }

        internal static bool AreEqualCollections<T>(ObjectCollection<T>? x, ObjectCollection<T>? y) where T : class
        {
            return AreEqualCollections(x, y, null);
        }

        internal static bool AreEqualCollections<T>(ObjectCollection<T>? x, ObjectCollection<T>? y, IEqualityComparer<T>? comparer) where T : class
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
            Debug.Assert(Array.TrueForAll(alreadyFound, value => value),
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
            current += HttpRuleParser.GetWhitespaceLength(input, current);

            if (skipEmptyValues)
            {
                while ((current < input.Length) && (input[current] == ','))
                {
                    current++; // skip delimiter.
                    current += HttpRuleParser.GetWhitespaceLength(input, current);
                }
            }

            return current;
        }

        internal static DateTimeOffset? GetDateTimeOffsetValue(HeaderDescriptor descriptor, HttpHeaders store, DateTimeOffset? defaultValue = null)
        {
            Debug.Assert(store != null);

            object? storedValue = store.GetSingleParsedValue(descriptor);
            if (storedValue != null)
            {
                return (DateTimeOffset)storedValue;
            }
            else if (defaultValue != null && store.Contains(descriptor))
            {
                return defaultValue;
            }

            return null;
        }

        internal static TimeSpan? GetTimeSpanValue(HeaderDescriptor descriptor, HttpHeaders store)
        {
            Debug.Assert(store != null);

            object? storedValue = store.GetSingleParsedValue(descriptor);
            if (storedValue != null)
            {
                return (TimeSpan)storedValue;
            }
            return null;
        }

        internal static bool TryParseInt32(string value, out int result) =>
            int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result);

        internal static bool TryParseInt32(string value, int offset, int length, out int result)
        {
            if (offset < 0 || length < 0 || offset > value.Length - length)
            {
                result = 0;
                return false;
            }

            return int.TryParse(value.AsSpan(offset, length), NumberStyles.None, CultureInfo.InvariantCulture, out result);
        }

        internal static bool TryParseInt64(string value, int offset, int length, out long result)
        {
            if (offset < 0 || length < 0 || offset > value.Length - length)
            {
                result = 0;
                return false;
            }

            return long.TryParse(value.AsSpan(offset, length), NumberStyles.None, CultureInfo.InvariantCulture, out result);
        }

        internal static void DumpHeaders(StringBuilder sb, params HttpHeaders?[] headers)
        {
            // Appends all headers as string similar to:
            // {
            //    HeaderName1: Value1
            //    HeaderName1: Value2
            //    HeaderName2: Value1
            //    ...
            // }
            sb.AppendLine("{");

            for (int i = 0; i < headers.Length; i++)
            {
                if (headers[i] is HttpHeaders hh)
                {
                    foreach (KeyValuePair<string, HeaderStringValues> header in hh.NonValidated)
                    {
                        foreach (string headerValue in header.Value)
                        {
                            sb.Append("  ");
                            sb.Append(header.Key);
                            sb.Append(": ");
                            sb.AppendLine(headerValue);
                        }
                    }
                }
            }

            sb.Append('}');
        }

        internal static UnvalidatedObjectCollection<NameValueHeaderValue>? Clone(this UnvalidatedObjectCollection<NameValueHeaderValue>? source)
        {
            if (source == null)
                return null;

            var copy = new UnvalidatedObjectCollection<NameValueHeaderValue>();
            foreach (NameValueHeaderValue item in source)
            {
                copy.Add(new NameValueHeaderValue(item));
            }

            return copy;
        }
    }
}
