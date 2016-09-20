// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;
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
                    return "";
                StringBuilder sb = new StringBuilder();
                string[] keys = AllKeys;
                for (int i = 0; i < count; i++)
                {
                    sb.AppendFormat("{0}={1}&", keys[i], UrlEncode(this[keys[i]]));
                }
                if (sb.Length > 0)
                    sb.Length--;
                return sb.ToString();
            }
        }

        public static NameValueCollection ParseQueryString(string query)
        {
            return ParseQueryString(query, Encoding.UTF8);
        }

        public static NameValueCollection ParseQueryString(string query, Encoding encoding)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            if ((query.Length > 0) && (query[0] == '?'))
                query = query.Substring(1);

            var result = new HttpQSCollection();
            ParseQueryString(query, encoding, result);
            return result;
        }

        private static void ParseQueryString(string query, Encoding encoding, NameValueCollection result)
        {
            if (query.Length == 0)
                return;

            var decoded = HtmlDecode(query);
            var decodedLength = decoded.Length;
            var namePos = 0;
            var first = true;
            while (namePos <= decodedLength)
            {
                int valuePos = -1, valueEnd = -1;
                for (var q = namePos; q < decodedLength; q++)
                    if ((valuePos == -1) && (decoded[q] == '='))
                    {
                        valuePos = q + 1;
                    }
                    else if (decoded[q] == '&')
                    {
                        valueEnd = q;
                        break;
                    }

                if (first)
                {
                    first = false;
                    if (decoded[namePos] == '?')
                        namePos++;
                }

                string name;
                if (valuePos == -1)
                {
                    name = null;
                    valuePos = namePos;
                }
                else
                {
                    name = UrlDecode(decoded.Substring(namePos, valuePos - namePos - 1), encoding);
                }
                if (valueEnd < 0)
                {
                    namePos = -1;
                    valueEnd = decoded.Length;
                }
                else
                {
                    namePos = valueEnd + 1;
                }
                var value = UrlDecode(decoded.Substring(valuePos, valueEnd - valuePos), encoding);

                result.Add(name, value);
                if (namePos == -1)
                    break;
            }
        }

        public static string HtmlDecode(string s)
        {
            return HttpEncoder.HtmlDecode(s);
        }


        public static void HtmlDecode(string s, TextWriter output)
        {
            HttpEncoder.HtmlDecode(s, output);
        }


        public static string HtmlEncode(string s)
        {
            return HttpEncoder.HtmlEncode(s);
        }


        public static string HtmlEncode(object value)
        {
            if (value == null)
                return null;

            return HtmlEncode(Convert.ToString(value, CultureInfo.CurrentCulture));
        }


        public static void HtmlEncode(string s, TextWriter output)
        {
            HttpEncoder.HtmlEncode(s, output);
        }


        public static string HtmlAttributeEncode(string s)
        {
            return HttpEncoder.HtmlAttributeEncode(s);
        }


        public static void HtmlAttributeEncode(string s, TextWriter output)
        {
            HttpEncoder.HtmlAttributeEncode(s, output);
        }

        public static string UrlEncode(string str)
        {
            if (str == null)
                return null;
            return UrlEncode(str, Encoding.UTF8);
        }


        public static string UrlPathEncode(string str)
        {
            return HttpEncoder.UrlPathEncode(str);
        }

        public static string UrlEncode(string str, Encoding e)
        {
            if (str == null)
                return null;
            return Encoding.ASCII.GetString(UrlEncodeToBytes(str, e));
        }

        public static string UrlEncode(byte[] bytes)
        {
            if (bytes == null)
                return null;
            return Encoding.ASCII.GetString(UrlEncodeToBytes(bytes));
        }

        public static string UrlEncode(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
                return null;
            return Encoding.ASCII.GetString(UrlEncodeToBytes(bytes, offset, count));
        }

        public static byte[] UrlEncodeToBytes(string str)
        {
            if (str == null)
                return null;
            return UrlEncodeToBytes(str, Encoding.UTF8);
        }

        public static byte[] UrlEncodeToBytes(byte[] bytes)
        {
            if (bytes == null)
                return null;
            return UrlEncodeToBytes(bytes, 0, bytes.Length);
        }

        [Obsolete(
             "This method produces non-standards-compliant output and has interoperability issues. The preferred alternative is UrlEncodeToBytes(String)."
         )]
        public static byte[] UrlEncodeUnicodeToBytes(string str)
        {
            if (str == null)
                return null;
            return Encoding.ASCII.GetBytes(UrlEncodeUnicode(str));
        }

        public static string UrlDecode(string str)
        {
            if (str == null)
                return null;
            return UrlDecode(str, Encoding.UTF8);
        }

        public static string UrlDecode(byte[] bytes, Encoding e)
        {
            if (bytes == null)
                return null;
            return UrlDecode(bytes, 0, bytes.Length, e);
        }

        public static byte[] UrlDecodeToBytes(string str)
        {
            if (str == null)
                return null;
            return UrlDecodeToBytes(str, Encoding.UTF8);
        }

        public static byte[] UrlDecodeToBytes(string str, Encoding e)
        {
            if (str == null)
                return null;
            return UrlDecodeToBytes(e.GetBytes(str));
        }

        public static byte[] UrlDecodeToBytes(byte[] bytes)
        {
            if (bytes == null)
                return null;
            return UrlDecodeToBytes(bytes, 0, bytes != null ? bytes.Length : 0);
        }

        public static byte[] UrlEncodeToBytes(string str, Encoding e)
        {
            if (str == null)
                return null;
            var bytes = e.GetBytes(str);
            return HttpEncoder.UrlEncode(bytes, 0, bytes.Length, false /* alwaysCreateNewReturnValue */);
        }

        public static byte[] UrlEncodeToBytes(byte[] bytes, int offset, int count)
        {
            return HttpEncoder.UrlEncode(bytes, offset, count, true /* alwaysCreateNewReturnValue */);
        }

        [Obsolete(
             "This method produces non-standards-compliant output and has interoperability issues. The preferred alternative is UrlEncode(String)."
         )]
        public static string UrlEncodeUnicode(string str)
        {
            return HttpEncoder.UrlEncodeUnicode(str, false /* ignoreAscii */);
        }

        public static string UrlDecode(string str, Encoding e)
        {
            return HttpEncoder.UrlDecode(str, e);
        }

        public static string UrlDecode(byte[] bytes, int offset, int count, Encoding e)
        {
            return HttpEncoder.UrlDecode(bytes, offset, count, e);
        }

        public static byte[] UrlDecodeToBytes(byte[] bytes, int offset, int count)
        {
            return HttpEncoder.UrlDecode(bytes, offset, count);
        }

        public static string JavaScriptStringEncode(string value)
        {
            return JavaScriptStringEncode(value, false);
        }

        public static string JavaScriptStringEncode(string value, bool addDoubleQuotes)
        {
            var encoded = HttpEncoder.JavaScriptStringEncode(value);
            return addDoubleQuotes ? "\"" + encoded + "\"" : encoded;
        }
    }
}