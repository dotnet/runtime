// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.Framework.WebEncoders
{
    /// <summary>
    /// Provides services for JavaScript-escaping strings.
    /// </summary>
    internal interface IJavaScriptStringEncoder
    {
        /// <summary>
        /// JavaScript-escapes a character array and writes the result to the
        /// supplied output.
        /// </summary>
        /// <remarks>
        /// The encoded value is appropriately encoded for inclusion inside a quoted JSON string.
        /// </remarks>
        void JavaScriptStringEncode(char[] value, int startIndex, int characterCount, TextWriter output);

        /// <summary>
        /// JavaScript-escapes a given input string.
        /// </summary>
        /// <returns>
        /// The JavaScript-escaped value, or null if the input string was null.
        /// The encoded value is appropriately encoded for inclusion inside a quoted JSON string.
        /// </returns>
        string JavaScriptStringEncode(string value);

        /// <summary>
        /// JavaScript-escapes a given input string and writes the
        /// result to the supplied output.
        /// </summary>
        /// <remarks>
        /// The encoded value is appropriately encoded for inclusion inside a quoted JSON string.
        /// </remarks>
        void JavaScriptStringEncode(string value, int startIndex, int characterCount, TextWriter output);
    }
}
