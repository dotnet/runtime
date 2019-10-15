// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Web
{
    public sealed partial class HttpUtility
    {
        public HttpUtility() { }
        public static string HtmlAttributeEncode(string s) { throw null; }
        public static void HtmlAttributeEncode(string s, System.IO.TextWriter output) { }
        public static string HtmlDecode(string s) { throw null; }
        public static void HtmlDecode(string s, System.IO.TextWriter output) { }
        public static string HtmlEncode(object value) { throw null; }
        public static string HtmlEncode(string s) { throw null; }
        public static void HtmlEncode(string s, System.IO.TextWriter output) { }
        public static string JavaScriptStringEncode(string value) { throw null; }
        public static string JavaScriptStringEncode(string value, bool addDoubleQuotes) { throw null; }
        public static System.Collections.Specialized.NameValueCollection ParseQueryString(string query) { throw null; }
        public static System.Collections.Specialized.NameValueCollection ParseQueryString(string query, System.Text.Encoding encoding) { throw null; }
        public static string UrlDecode(byte[] bytes, int offset, int count, System.Text.Encoding e) { throw null; }
        public static string UrlDecode(byte[] bytes, System.Text.Encoding e) { throw null; }
        public static string UrlDecode(string str) { throw null; }
        public static string UrlDecode(string str, System.Text.Encoding e) { throw null; }
        public static byte[] UrlDecodeToBytes(byte[] bytes) { throw null; }
        public static byte[] UrlDecodeToBytes(byte[] bytes, int offset, int count) { throw null; }
        public static byte[] UrlDecodeToBytes(string str) { throw null; }
        public static byte[] UrlDecodeToBytes(string str, System.Text.Encoding e) { throw null; }
        public static string UrlEncode(byte[] bytes) { throw null; }
        public static string UrlEncode(byte[] bytes, int offset, int count) { throw null; }
        public static string UrlEncode(string str) { throw null; }
        public static string UrlEncode(string str, System.Text.Encoding e) { throw null; }
        public static byte[] UrlEncodeToBytes(byte[] bytes) { throw null; }
        public static byte[] UrlEncodeToBytes(byte[] bytes, int offset, int count) { throw null; }
        public static byte[] UrlEncodeToBytes(string str) { throw null; }
        public static byte[] UrlEncodeToBytes(string str, System.Text.Encoding e) { throw null; }
        [System.ObsoleteAttribute("This method produces non-standards-compliant output and has interoperability issues. The preferred alternative is UrlEncode(String).")]
        public static string UrlEncodeUnicode(string str) { throw null; }
        [System.ObsoleteAttribute("This method produces non-standards-compliant output and has interoperability issues. The preferred alternative is UrlEncodeToBytes(String).")]
        public static byte[] UrlEncodeUnicodeToBytes(string str) { throw null; }
        public static string UrlPathEncode(string str) { throw null; }
    }
}
