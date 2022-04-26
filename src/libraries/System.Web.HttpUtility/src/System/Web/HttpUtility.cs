// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Authors:
//   Patrik Torstensson (Patrik.Torstensson@labs2.com)
//   Wictor Wilén (decode/encode functions) (wictor@ibizkit.se)
//   Tim Coleman (tim@timcoleman.com)
//   Gonzalo Paniagua Javier (gonzalo@ximian.com)
//
// Copyright (C) 2005-2010 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web.Util;

namespace System.Web
{
    public sealed class HttpUtility
    {
        private sealed class HttpQSCollection : NameValueCollection
        {
            internal HttpQSCollection()
                : base(StringComparer.OrdinalIgnoreCase)
            {
            }

            public override string ToString()
            {
                int count = Count;
                if (count == 0)
                {
                    return "";
                }

                StringBuilder sb = new StringBuilder();
                string?[] keys = AllKeys;
                for (int i = 0; i < count; i++)
                {
                    string? key = keys[i];
                    string[]? values = GetValues(key);
                    if (values != null)
                    {
                        foreach (string value in values)
                        {
                            if (!string.IsNullOrEmpty(key))
                            {
                                sb.Append(key).Append('=');
                            }
                            sb.Append(UrlEncode(value)).Append('&');
                        }
                    }
                }

                return sb.ToString(0, sb.Length - 1);
            }
        }

        public static NameValueCollection ParseQueryString(string query) => ParseQueryString(query, Encoding.UTF8);

        public static NameValueCollection ParseQueryString(string query, Encoding encoding)
        {
            ArgumentNullException.ThrowIfNull(query);
            ArgumentNullException.ThrowIfNull(encoding);

            HttpQSCollection result = new HttpQSCollection();
            int queryLength = query.Length;
            int namePos = queryLength > 0 && query[0] == '?' ? 1 : 0;
            if (queryLength == namePos)
            {
                return result;
            }

            while (namePos <= queryLength)
            {
                int valuePos = -1, valueEnd = -1;
                for (int q = namePos; q < queryLength; q++)
                {
                    if (valuePos == -1 && query[q] == '=')
                    {
                        valuePos = q + 1;
                    }
                    else if (query[q] == '&')
                    {
                        valueEnd = q;
                        break;
                    }
                }

                string? name;
                if (valuePos == -1)
                {
                    name = null;
                    valuePos = namePos;
                }
                else
                {
                    name = UrlDecode(query.Substring(namePos, valuePos - namePos - 1), encoding);
                }

                if (valueEnd < 0)
                {
                    valueEnd = query.Length;
                }

                namePos = valueEnd + 1;
                string value = UrlDecode(query.Substring(valuePos, valueEnd - valuePos), encoding);
                result.Add(name, value);
            }

            return result;
        }

        [return: NotNullIfNotNull("s")]
        public static string? HtmlDecode(string? s) => HttpEncoder.HtmlDecode(s);

        public static void HtmlDecode(string? s, TextWriter output) => HttpEncoder.HtmlDecode(s, output);

        [return: NotNullIfNotNull("s")]
        public static string? HtmlEncode(string? s) => HttpEncoder.HtmlEncode(s);

        [return: NotNullIfNotNull("value")]
        public static string? HtmlEncode(object? value) =>
            value == null ? null : HtmlEncode(Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty);

        public static void HtmlEncode(string? s, TextWriter output) => HttpEncoder.HtmlEncode(s, output);

        [return: NotNullIfNotNull("s")]
        public static string? HtmlAttributeEncode(string? s) => HttpEncoder.HtmlAttributeEncode(s);

        public static void HtmlAttributeEncode(string? s, TextWriter output) => HttpEncoder.HtmlAttributeEncode(s, output);

        [return: NotNullIfNotNull("str")]
        public static string? UrlEncode(string? str) => UrlEncode(str, Encoding.UTF8);

        [return: NotNullIfNotNull("str")]
        public static string? UrlPathEncode(string? str) => HttpEncoder.UrlPathEncode(str);

        [return: NotNullIfNotNull("str")]
        public static string? UrlEncode(string? str, Encoding e) =>
            str == null ? null : Encoding.ASCII.GetString(UrlEncodeToBytes(str, e));

        [return: NotNullIfNotNull("bytes")]
        public static string? UrlEncode(byte[]? bytes) => bytes == null ? null : Encoding.ASCII.GetString(UrlEncodeToBytes(bytes));

        [return: NotNullIfNotNull("bytes")]
        public static string? UrlEncode(byte[]? bytes, int offset, int count) => bytes == null ? null : Encoding.ASCII.GetString(UrlEncodeToBytes(bytes, offset, count));

        [return: NotNullIfNotNull("str")]
        public static byte[]? UrlEncodeToBytes(string? str) => UrlEncodeToBytes(str, Encoding.UTF8);

        [return: NotNullIfNotNull("bytes")]
        public static byte[]? UrlEncodeToBytes(byte[]? bytes) => bytes == null ? null : UrlEncodeToBytes(bytes, 0, bytes.Length);

        [Obsolete("This method produces non-standards-compliant output and has interoperability issues. The preferred alternative is UrlEncodeToBytes(String).")]
        [return: NotNullIfNotNull("str")]
        public static byte[]? UrlEncodeUnicodeToBytes(string? str) => str == null ? null : Encoding.ASCII.GetBytes(UrlEncodeUnicode(str));

        [return: NotNullIfNotNull("str")]
        public static string? UrlDecode(string? str) => UrlDecode(str, Encoding.UTF8);

        [return: NotNullIfNotNull("bytes")]
        public static string? UrlDecode(byte[]? bytes, Encoding e) => bytes == null ? null : UrlDecode(bytes, 0, bytes.Length, e);

        [return: NotNullIfNotNull("str")]
        public static byte[]? UrlDecodeToBytes(string? str) => UrlDecodeToBytes(str, Encoding.UTF8);

        [return: NotNullIfNotNull("str")]
        public static byte[]? UrlDecodeToBytes(string? str, Encoding e) => str == null ? null : UrlDecodeToBytes(e.GetBytes(str));

        [return: NotNullIfNotNull("bytes")]
        public static byte[]? UrlDecodeToBytes(byte[]? bytes) => bytes == null ? null : UrlDecodeToBytes(bytes, 0, bytes.Length);

        [return: NotNullIfNotNull("str")]
        public static byte[]? UrlEncodeToBytes(string? str, Encoding e)
        {
            if (str == null)
            {
                return null;
            }

            byte[] bytes = e.GetBytes(str);
            return HttpEncoder.UrlEncode(bytes, 0, bytes.Length, alwaysCreateNewReturnValue: false);
        }

        [return: NotNullIfNotNull("bytes")]
        public static byte[]? UrlEncodeToBytes(byte[]? bytes, int offset, int count) => HttpEncoder.UrlEncode(bytes, offset, count, alwaysCreateNewReturnValue: true);

        [Obsolete("This method produces non-standards-compliant output and has interoperability issues. The preferred alternative is UrlEncode(String).")]
        [return: NotNullIfNotNull("str")]
        public static string? UrlEncodeUnicode(string? str) => HttpEncoder.UrlEncodeUnicode(str);

        [return: NotNullIfNotNull("str")]
        public static string? UrlDecode(string? str, Encoding e) => HttpEncoder.UrlDecode(str, e);

        [return: NotNullIfNotNull("bytes")]
        public static string? UrlDecode(byte[]? bytes, int offset, int count, Encoding e) =>
            HttpEncoder.UrlDecode(bytes, offset, count, e);

        [return: NotNullIfNotNull("bytes")]
        public static byte[]? UrlDecodeToBytes(byte[]? bytes, int offset, int count) => HttpEncoder.UrlDecode(bytes, offset, count);

        public static string JavaScriptStringEncode(string? value) => HttpEncoder.JavaScriptStringEncode(value);

        public static string JavaScriptStringEncode(string? value, bool addDoubleQuotes)
        {
            string encoded = HttpEncoder.JavaScriptStringEncode(value);
            return addDoubleQuotes ? "\"" + encoded + "\"" : encoded;
        }
    }
}
