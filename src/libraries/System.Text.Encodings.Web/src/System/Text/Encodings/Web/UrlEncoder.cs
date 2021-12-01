// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Unicode;

namespace System.Text.Encodings.Web
{
    /// <summary>
    /// Represents a type used to do URL encoding.
    /// </summary>
    public abstract class UrlEncoder : TextEncoder
    {
        /// <summary>
        /// Returns a default built-in instance of <see cref="UrlEncoder"/>.
        /// </summary>
        public static UrlEncoder Default => DefaultUrlEncoder.BasicLatinSingleton;

        /// <summary>
        /// Creates a new instance of UrlEncoder with provided settings.
        /// </summary>
        /// <param name="settings">Settings used to control how the created <see cref="UrlEncoder"/> encodes, primarily which characters to encode.</param>
        /// <returns>A new instance of the <see cref="UrlEncoder"/>.</returns>
        public static UrlEncoder Create(TextEncoderSettings settings)
        {
            return new DefaultUrlEncoder(settings);
        }

        /// <summary>
        /// Creates a new instance of UrlEncoder specifying character to be encoded.
        /// </summary>
        /// <param name="allowedRanges">Set of characters that the encoder is allowed to not encode.</param>
        /// <returns>A new instance of the <see cref="UrlEncoder"/>.</returns>
        /// <remarks>Some characters in <paramref name="allowedRanges"/> might still get encoded, i.e. this parameter is just telling the encoder what ranges it is allowed to not encode, not what characters it must not encode.</remarks>
        public static UrlEncoder Create(params UnicodeRange[] allowedRanges)
        {
            return new DefaultUrlEncoder(new TextEncoderSettings(allowedRanges));
        }
    }
}
