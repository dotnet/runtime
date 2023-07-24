// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Web
{
    public sealed partial class HttpUtility
    {
        public HttpUtility() { }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("s")]
        public static string? HtmlAttributeEncode(string? s) { throw null; }
        public static void HtmlAttributeEncode(string? s, System.IO.TextWriter output) { }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("s")]
        public static string? HtmlDecode(string? s) { throw null; }
        public static void HtmlDecode(string? s, System.IO.TextWriter output) { }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("value")]
        public static string? HtmlEncode(object? value) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("s")]
        public static string? HtmlEncode(string? s) { throw null; }
        public static void HtmlEncode(string? s, System.IO.TextWriter output) { }
        public static string JavaScriptStringEncode(string? value) { throw null; }
        public static string JavaScriptStringEncode(string? value, bool addDoubleQuotes) { throw null; }
        public static System.Collections.Specialized.NameValueCollection ParseQueryString(string query) { throw null; }
        public static System.Collections.Specialized.NameValueCollection ParseQueryString(string query, System.Text.Encoding encoding) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("bytes")]
        public static string? UrlDecode(byte[]? bytes, int offset, int count, System.Text.Encoding e) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("bytes")]
        public static string? UrlDecode(byte[]? bytes, System.Text.Encoding e) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("str")]
        public static string? UrlDecode(string? str) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("str")]
        public static string? UrlDecode(string? str, System.Text.Encoding e) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("bytes")]
        public static byte[]? UrlDecodeToBytes(byte[]? bytes) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("bytes")]
        public static byte[]? UrlDecodeToBytes(byte[]? bytes, int offset, int count) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("str")]
        public static byte[]? UrlDecodeToBytes(string? str) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("str")]
        public static byte[]? UrlDecodeToBytes(string? str, System.Text.Encoding e) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("bytes")]
        public static string? UrlEncode(byte[]? bytes) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("bytes")]
        public static string? UrlEncode(byte[]? bytes, int offset, int count) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("str")]
        public static string? UrlEncode(string? str) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("str")]
        public static string? UrlEncode(string? str, System.Text.Encoding e) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("bytes")]
        public static byte[]? UrlEncodeToBytes(byte[]? bytes) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("bytes")]
        public static byte[]? UrlEncodeToBytes(byte[]? bytes, int offset, int count) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("str")]
        public static byte[]? UrlEncodeToBytes(string? str) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("str")]
        public static byte[]? UrlEncodeToBytes(string? str, System.Text.Encoding e) { throw null; }
        [System.ObsoleteAttribute("This method produces non-standards-compliant output and has interoperability issues. The preferred alternative is UrlEncode(String).")]
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("str")]
        public static string? UrlEncodeUnicode(string? str) { throw null; }
        [System.ObsoleteAttribute("This method produces non-standards-compliant output and has interoperability issues. The preferred alternative is UrlEncodeToBytes(String).")]
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("str")]
        public static byte[]? UrlEncodeUnicodeToBytes(string? str) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("str")]
        public static string? UrlPathEncode(string? str) { throw null; }
    }
    public partial interface IHtmlString
    {
        string? ToHtmlString();
    }
}
