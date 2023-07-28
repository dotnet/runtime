// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Unicode;

namespace System.Text.Encodings.Web
{
    /// <summary>
    /// Represents a type used to do JavaScript encoding/escaping.
    /// </summary>
    public abstract class JavaScriptEncoder : TextEncoder
    {
        /// <summary>
        /// Returns a default built-in instance of <see cref="JavaScriptEncoder"/>.
        /// </summary>
        public static JavaScriptEncoder Default => DefaultJavaScriptEncoder.BasicLatinSingleton;

        /// <summary>
        /// Returns a built-in instance of <see cref="JavaScriptEncoder"/> that is less strict about what gets encoded.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Unlike the <see cref="Default"/>, this encoder instance does not escape HTML-sensitive characters like &lt;, &gt;, &amp;, etc. and hence must be used cautiously
        /// (for example, if the output data is within a response whose content-type is known with a charset set to UTF-8).
        /// </para>
        /// <para>
        /// Unlike the <see cref="Default"/>, the quotation mark is encoded as \" rather than \u0022.
        /// </para>
        /// <para>
        /// Unlike the <see cref="Default"/> (which only allows <see cref="UnicodeRanges.BasicLatin"/>), using this encoder instance allows <see cref="UnicodeRanges.All"/> to go through unescaped.
        /// </para>
        /// <para>
        /// Unlike the <see cref="Default"/>, this encoder instance allows some other characters to go through unescaped (for example, '+'), and hence must be used cautiously.
        /// </para>
        /// </remarks>
        public static JavaScriptEncoder UnsafeRelaxedJsonEscaping => DefaultJavaScriptEncoder.UnsafeRelaxedEscapingSingleton;

        /// <summary>
        /// Creates a new instance of JavaScriptEncoder with provided settings.
        /// </summary>
        /// <param name="settings">Settings used to control how the created <see cref="JavaScriptEncoder"/> encodes, primarily which characters to encode.</param>
        /// <returns>A new instance of the <see cref="JavaScriptEncoder"/>.</returns>
        public static JavaScriptEncoder Create(TextEncoderSettings settings)
        {
            return new DefaultJavaScriptEncoder(settings);
        }

        /// <summary>
        /// Creates a new instance of JavaScriptEncoder specifying character to be encoded.
        /// </summary>
        /// <param name="allowedRanges">Set of characters that the encoder is allowed to not encode.</param>
        /// <returns>A new instance of the <see cref="JavaScriptEncoder"/>.</returns>
        /// <remarks>Some characters in <paramref name="allowedRanges"/> might still get encoded, i.e. this parameter is just telling the encoder what ranges it is allowed to not encode, not what characters it must not encode.</remarks>
        public static JavaScriptEncoder Create(params UnicodeRange[] allowedRanges)
        {
            return new DefaultJavaScriptEncoder(new TextEncoderSettings(allowedRanges));
        }
    }
}
