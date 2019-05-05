// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Microsoft.Framework.WebEncoders
{
    // These implement ASP.NET interfaces. They will be removed once we transition ASP.NET
    internal sealed class HtmlEncoder : IHtmlEncoder
    {
        System.Text.Encodings.Web.HtmlEncoder _encoder;
        static HtmlEncoder s_default;

        /// <summary>
        /// A default instance of <see cref="HtmlEncoder"/>.
        /// </summary>
        /// <remarks>
        /// This normally corresponds to <see cref="UnicodeRanges.BasicLatin"/>. However, this property is
        /// settable so that a developer can change the default implementation application-wide.
        /// </remarks>
        public static HtmlEncoder Default
        {
            get
            {
                if (s_default == null)
                {
                    s_default = new HtmlEncoder();
                }
                return s_default;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                s_default = value;
            }
        }

        public HtmlEncoder()
        {
            _encoder = System.Text.Encodings.Web.HtmlEncoder.Default;
        }
        public HtmlEncoder(TextEncoderSettings filter)
        {
            _encoder = System.Text.Encodings.Web.HtmlEncoder.Create(filter);
        }

        public HtmlEncoder(UnicodeRange allowedRange) : this(new TextEncoderSettings(allowedRange))
        { }

        public HtmlEncoder(params UnicodeRange[] allowedRanges) : this(new TextEncoderSettings(allowedRanges))
        { }

        public void HtmlEncode(char[] value, int startIndex, int characterCount, TextWriter output)
        {
            _encoder.Encode(output, value, startIndex, characterCount);
        }

        public string HtmlEncode(string value)
        {
            return _encoder.Encode(value);
        }

        public void HtmlEncode(string value, int startIndex, int characterCount, TextWriter output)
        {
            _encoder.Encode(output, value, startIndex, characterCount);
        }
    }

    internal sealed class JavaScriptStringEncoder : IJavaScriptStringEncoder
    {
        System.Text.Encodings.Web.JavaScriptEncoder _encoder;
        static JavaScriptStringEncoder s_default;

        /// <summary>
        /// A default instance of <see cref="JavaScriptEncoder"/>.
        /// </summary>
        /// <remarks>
        /// This normally corresponds to <see cref="UnicodeRanges.BasicLatin"/>. However, this property is
        /// settable so that a developer can change the default implementation application-wide.
        /// </remarks>
        public static JavaScriptStringEncoder Default
        {
            get
            {
                if (s_default == null)
                {
                    s_default = new JavaScriptStringEncoder();
                }
                return s_default;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                s_default = value;
            }
        }

        public JavaScriptStringEncoder()
        {
            _encoder = System.Text.Encodings.Web.JavaScriptEncoder.Default;
        }
        public JavaScriptStringEncoder(TextEncoderSettings filter)
        {
            _encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(filter);
        }

        public JavaScriptStringEncoder(UnicodeRange allowedRange) : this(new TextEncoderSettings(allowedRange))
        { }

        public JavaScriptStringEncoder(params UnicodeRange[] allowedRanges) : this(new TextEncoderSettings(allowedRanges))
        { }

        public void JavaScriptStringEncode(char[] value, int startIndex, int characterCount, TextWriter output)
        {
            _encoder.Encode(output, value, startIndex, characterCount);
        }

        public string JavaScriptStringEncode(string value)
        {
            return _encoder.Encode(value);
        }

        public void JavaScriptStringEncode(string value, int startIndex, int characterCount, TextWriter output)
        {
            _encoder.Encode(output, value, startIndex, characterCount);
        }
    }

    internal sealed class UrlEncoder : IUrlEncoder
    {
        System.Text.Encodings.Web.UrlEncoder _encoder;
        static UrlEncoder s_default;

        /// <summary>
        /// A default instance of <see cref="UrlEncoder"/>.
        /// </summary>
        /// <remarks>
        /// This normally corresponds to <see cref="UnicodeRanges.BasicLatin"/>. However, this property is
        /// settable so that a developer can change the default implementation application-wide.
        /// </remarks>
        public static UrlEncoder Default
        {
            get
            {
                if (s_default == null)
                {
                    s_default = new UrlEncoder();
                }
                return s_default;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                s_default = value;
            }
        }

        public UrlEncoder()
        {
            _encoder = System.Text.Encodings.Web.UrlEncoder.Default;
        }
        public UrlEncoder(TextEncoderSettings filter)
        {
            _encoder = System.Text.Encodings.Web.UrlEncoder.Create(filter);
        }

        public UrlEncoder(UnicodeRange allowedRange) : this(new TextEncoderSettings(allowedRange))
        { }

        public UrlEncoder(params UnicodeRange[] allowedRanges) : this(new TextEncoderSettings(allowedRanges))
        { }

        public void UrlEncode(char[] value, int startIndex, int characterCount, TextWriter output)
        {
            _encoder.Encode(output, value, startIndex, characterCount);
        }

        public string UrlEncode(string value)
        {
            return _encoder.Encode(value); ;
        }

        public void UrlEncode(string value, int startIndex, int characterCount, TextWriter output)
        {
            _encoder.Encode(output, value, startIndex, characterCount);
        }
    }
}
