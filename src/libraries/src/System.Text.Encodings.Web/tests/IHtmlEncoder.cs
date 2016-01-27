// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.Framework.WebEncoders
{
    /// <summary>
    /// Provides services for HTML-encoding input.
    /// </summary>
    internal interface IHtmlEncoder
    {
        /// <summary>
        /// HTML-encodes a character array and writes the result to the supplied
        /// output.
        /// </summary>
        /// <remarks>
        /// The encoded value is also appropriately encoded for inclusion inside an HTML attribute
        /// as long as the attribute value is surrounded by single or double quotes.
        /// </remarks>
        void HtmlEncode(char[] value, int startIndex, int characterCount, TextWriter output);

        /// <summary>
        /// HTML-encodes a given input string.
        /// </summary>
        /// <returns>
        /// The HTML-encoded value, or null if the input string was null.
        /// </returns>
        /// <remarks>
        /// The return value is also appropriately encoded for inclusion inside an HTML attribute
        /// as long as the attribute value is surrounded by single or double quotes.
        /// </remarks>
        string HtmlEncode(string value);

        /// <summary>
        /// HTML-encodes a given input string and writes the result to the
        /// supplied output.
        /// </summary>
        /// <remarks>
        /// The encoded value is also appropriately encoded for inclusion inside an HTML attribute
        /// as long as the attribute value is surrounded by single or double quotes.
        /// </remarks>
        void HtmlEncode(string value, int startIndex, int characterCount, TextWriter output);
    }
}
