// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Web {
    public sealed class HttpUtility {
        public static string HtmlAttributeEncode(string s) { return default(string); }
        public static void HtmlAttributeEncode(string s, System.IO.TextWriter output) { }
        public static string HtmlDecode(string s) { return default(string); }
        public static void HtmlDecode(string s, System.IO.TextWriter output) { }
        public static string HtmlEncode(string s) { return default(string); }
        public static string HtmlEncode(object value) { return default(string); }
        public static void HtmlEncode(string s, System.IO.TextWriter output) { }
        public static string JavaScriptStringEncode(string value) { return default(string); }
        public static string JavaScriptStringEncode(string value, bool addDoubleQuotes) { return default(string); }
        public static System.Collections.Specialized.NameValueCollection ParseQueryString(string query) { return default(System.Collections.Specialized.NameValueCollection); }
        public static System.Collections.Specialized.NameValueCollection ParseQueryString(string query, System.Text.Encoding encoding) { return default(System.Collections.Specialized.NameValueCollection); }
        public static string UrlDecode(string str, System.Text.Encoding e) { return default(string); }
        public static string UrlDecode(byte[] bytes, int offset, int count, System.Text.Encoding e) { return default(string); }
        public static string UrlDecode(string str) { return default(string); }
        public static string UrlDecode(byte[] bytes, System.Text.Encoding e) { return default(string); }
        public static byte[] UrlDecodeToBytes(string str) { return default(byte[]); }
        public static byte[] UrlDecodeToBytes(string str, System.Text.Encoding e) { return default(byte[]); }
        public static byte[] UrlDecodeToBytes(byte[] bytes) { return default(byte[]); }
        public static byte[] UrlDecodeToBytes(byte[] bytes, int offset, int count) { return default(byte[]); }
        public static string UrlEncode(string str) { return default(string); }
        public static string UrlEncode(string str, System.Text.Encoding e) { return default(string); }
        public static string UrlEncode(byte[] bytes) { return default(string); }
        public static string UrlEncode(byte[] bytes, int offset, int count) { return default(string); }
        public static byte[] UrlEncodeToBytes(string str) { return default(byte[]); }
        public static byte[] UrlEncodeToBytes(byte[] bytes) { return default(byte[]); }
        public static byte[] UrlEncodeToBytes(string str, System.Text.Encoding e) { return default(byte[]); }
        public static byte[] UrlEncodeToBytes(byte[] bytes, int offset, int count) { return default(byte[]); }
        [Obsolete("This method produces non-standards-compliant output and has interoperability issues. The preferred alternative is UrlEncode(String).")]
        public static string UrlEncodeUnicode(string str) { return default(string); }
        [Obsolete("This method produces non-standards-compliant output and has interoperability issues. The preferred alternative is UrlEncodeToBytes(String).")]
        public static byte[] UrlEncodeUnicodeToBytes(string str) { return default(byte[]); }
        public static string UrlPathEncode(string str) { return default(string); }
    }
}
