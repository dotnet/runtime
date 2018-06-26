// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;

namespace Microsoft.Framework.WebEncoders
{
    /// <summary>
    /// Dummy encoder used for unit testing.
    /// </summary>
    public sealed class CommonTestEncoder : IHtmlEncoder, IJavaScriptStringEncoder, IUrlEncoder
    {
        /// <summary>
        /// Returns "HtmlEncode[[value]]".
        /// </summary>
        public string HtmlEncode(string value)
        {
            return EncodeCore(value);
        }

        /// <summary>
        /// Writes "HtmlEncode[[value]]".
        /// </summary>
        public void HtmlEncode(string value, int startIndex, int characterCount, TextWriter output)
        {
            EncodeCore(value, startIndex, characterCount, output);
        }

        /// <summary>
        /// Writes "HtmlEncode[[value]]".
        /// </summary>
        public void HtmlEncode(char[] value, int startIndex, int characterCount, TextWriter output)
        {
            EncodeCore(value, startIndex, characterCount, output);
        }

        /// <summary>
        /// Returns "JavaScriptStringEncode[[value]]".
        /// </summary>
        public string JavaScriptStringEncode(string value)
        {
            return EncodeCore(value);
        }

        /// <summary>
        /// Writes "JavaScriptStringEncode[[value]]".
        /// </summary>
        public void JavaScriptStringEncode(string value, int startIndex, int characterCount, TextWriter output)
        {
            EncodeCore(value, startIndex, characterCount, output);
        }

        /// <summary>
        /// Writes "JavaScriptStringEncode[[value]]".
        /// </summary>
        public void JavaScriptStringEncode(char[] value, int startIndex, int characterCount, TextWriter output)
        {
            EncodeCore(value, startIndex, characterCount, output);
        }

        /// <summary>
        /// Returns "UrlEncode[[value]]".
        /// </summary>
        public string UrlEncode(string value)
        {
            return EncodeCore(value);
        }

        /// <summary>
        /// Writes "UrlEncode[[value]]".
        /// </summary>
        public void UrlEncode(string value, int startIndex, int characterCount, TextWriter output)
        {
            EncodeCore(value, startIndex, characterCount, output);
        }

        /// <summary>
        /// Writes "UrlEncode[[value]]".
        /// </summary>
        public void UrlEncode(char[] value, int startIndex, int characterCount, TextWriter output)
        {
            EncodeCore(value, startIndex, characterCount, output);
        }

        private static string EncodeCore(string value, [CallerMemberName] string encodeType = null)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}[[{1}]]", encodeType, value);
        }

        private static void EncodeCore(string value, int startIndex, int characterCount, TextWriter output, [CallerMemberName] string encodeType = null)
        {
            output.Write(EncodeCore(value.Substring(startIndex, characterCount), encodeType));
        }

        private static void EncodeCore(char[] value, int startIndex, int characterCount, TextWriter output, [CallerMemberName] string encodeType = null)
        {
            output.Write(EncodeCore(new string(value, startIndex, characterCount), encodeType));
        }
    }
}
