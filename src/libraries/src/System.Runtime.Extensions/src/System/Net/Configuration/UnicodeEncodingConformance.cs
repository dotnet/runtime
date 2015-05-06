// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace System.Net.Configuration
{
    /// <summary>
    /// Controls how Unicode characters are output by the WebUtility.HtmlEncode routine.
    /// </summary>
    /// <remarks>
    /// See http://www.w3.org/International/questions/qa-escapes#bytheway for more information
    /// on how Unicode characters in the SMP are supposed to be encoded in HTML.
    /// </remarks>
    internal enum UnicodeEncodingConformance
    {
        /// <summary>
        /// The Unicode encoding behavior is determined by current application's
        /// TargetFrameworkAttribute. Framework40 and earlier gets Compat behavior; Framework45
        /// and later gets Strict behavior.
        /// </summary>
        Auto,

        /// <summary>
        /// Specifies that individual UTF-16 surrogate code points are combined into a single
        /// SMP code point when a call to HtmlEncode takes place. For example, given the input
        /// string "\uD84C\uDFB4" (or "\U000233B4"), the output of HtmlEncode is "&amp;#144308;".
        /// </summary>
        /// <remarks>
        /// If the input is a malformed UTF-16 string, e.g. it contains unpaired surrogates,
        /// the bad code points will be replaced with U+FFFD (Unicode replacement char) before
        /// being HTML-encoded.
        /// </remarks>
        Strict,

        /// <summary>
        /// Specifies that individual UTF-16 surrogate code points are output as-is when a call to
        /// HtmlEncode takes place. For example, given a string "\uD84C\uDFB4" (or "\U000233B4"),
        /// the output of HtmlEncode is "\uD84C\uDFB4" (the input is not encoded).
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Compat", Justification = "Shorthand for 'compatibility mode'.")]
        Compat,
    }
}
