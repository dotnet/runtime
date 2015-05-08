// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Framework.WebEncoders
{
    /// <summary>
    /// Helpful extension methods for the encoder classes.
    /// </summary>
    internal static class EncoderExtensions
    {
        /// <summary>
        /// HTML-encodes a string and writes the result to the supplied output.
        /// </summary>
        /// <remarks>
        /// The encoded value is also safe for inclusion inside an HTML attribute
        /// as long as the attribute value is surrounded by single or double quotes.
        /// </remarks>
        public static void HtmlEncode(this IHtmlEncoder htmlEncoder, string value, TextWriter output)
        {
            if (htmlEncoder == null)
            {
                throw new ArgumentNullException("htmlEncoder");
            }

            if (!String.IsNullOrEmpty(value))
            {
                htmlEncoder.HtmlEncode(value, 0, value.Length, output);
            }
        }

        /// <summary>
        /// JavaScript-escapes a string and writes the result to the supplied output.
        /// </summary>
        public static void JavaScriptStringEncode(this IJavaScriptStringEncoder javaScriptStringEncoder, string value, TextWriter output)
        {
            if (javaScriptStringEncoder == null)
            {
                throw new ArgumentNullException("javaScriptStringEncoder");
            }

            if (!String.IsNullOrEmpty(value))
            {
                javaScriptStringEncoder.JavaScriptStringEncode(value, 0, value.Length, output);
            }
        }

        /// <summary>
        /// URL-encodes a string and writes the result to the supplied output.
        /// </summary>
        /// <remarks>
        /// The encoded value is safe for use in the segment, query, or
        /// fragment portion of a URI.
        /// </remarks>
        public static void UrlEncode(this IUrlEncoder urlEncoder, string value, TextWriter output)
        {
            if (urlEncoder == null)
            {
                throw new ArgumentNullException("urlEncoder");
            }

            if (!String.IsNullOrEmpty(value))
            {
                urlEncoder.UrlEncode(value, 0, value.Length, output);
            }
        }
    }
}
