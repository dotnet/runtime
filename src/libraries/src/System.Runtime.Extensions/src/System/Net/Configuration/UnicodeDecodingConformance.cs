// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace System.Net.Configuration
{
    /// <summary>
    /// Controls how Unicode characters are interpreted by the WebUtility.HtmlDecode routine.
    /// </summary>
    /// <remarks>
    /// See http://www.w3.org/International/questions/qa-escapes#bytheway for more information
    /// on how Unicode characters in the SMP are supposed to be encoded in HTML.
    /// </remarks>
    internal enum UnicodeDecodingConformance
    {
        /// <summary>
        /// The Unicode encoding behavior is determined by current application's
        /// TargetFrameworkAttribute. Framework40 and earlier gets Compat behavior; Framework45
        /// and later gets Strict behavior.
        /// </summary>
        Auto,

        /// <summary>
        /// Specifies that the incoming encoded data is checked for validity before being
        /// decoded. For example, an input string of "&amp;#144308;" would decode as U+233B4,
        /// but an input string of "&amp;#xD84C;&amp;#xDFB4;" would fail to decode properly.
        /// </summary>
        /// <remarks>
        /// Already-decoded data in the string is not checked for validity. For example, an
        /// input string of "\ud800" will result in an output string of "\ud800", as the
        /// already-decoded surrogate is skipped during decoding, even though it is unpaired.
        /// </remarks>
        Strict,

        /// <summary>
        /// Specifies that incoming data is not checked for validity before being decoded.
        /// For example, an input string of "&amp;#xD84C;" would decode as U+D84C, which is
        /// an unpaired surrogate. Additionally, the decoder does not understand code points
        /// in the SMP unless they're represented as HTML-encoded surrogates, so the input
        /// string "&amp;#144308;" would result in the output string "&amp;#144308;".
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Compat", Justification = "Shorthand for 'compatibility mode'.")]
        Compat,

        /// <summary>
        /// Similar to 'Compat' in that there are no validity checks, but the decoder also
        /// understands SMP code points. The input string "&amp;#144308;" will thus decode
        /// into the character U+233B4 correctly.
        /// </summary>
        /// <remarks>
        /// This switch is meant to provide maximum interoperability when the decoder doesn't
        /// know which format the provider is using to generate the encoded string.
        /// </remarks>
        Loose,
    }
}
