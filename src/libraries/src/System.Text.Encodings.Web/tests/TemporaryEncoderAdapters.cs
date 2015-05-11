// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Microsoft.Framework.WebEncoders
{
    // These implement ASP.NET interfaces. They will be removed once we transition ASP.NET
    internal sealed class HtmlEncoder : IHtmlEncoder
    {
        System.Text.Encodings.Web.DefaultHtmlEncoder _encoder;
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
                    throw new ArgumentNullException("value");
                }
                s_default = value;
            }
        }

        public HtmlEncoder()
        {
            _encoder = System.Text.Encodings.Web.DefaultHtmlEncoder.Singleton;
        }
        public HtmlEncoder(CodePointFilter filter)
        {
            _encoder = new System.Text.Encodings.Web.DefaultHtmlEncoder(filter);
        }

        public HtmlEncoder(UnicodeRange allowedRange) : this(new CodePointFilter(allowedRange))
        { }

        public HtmlEncoder(params UnicodeRange[] allowedRanges) : this(new CodePointFilter(allowedRanges))
        { }

        public void HtmlEncode(char[] value, int startIndex, int characterCount, TextWriter output)
        {
            _encoder.Encode(value, startIndex, characterCount, output);
        }

        public string HtmlEncode(string value)
        {
            return _encoder.Encode(value);
        }

        public void HtmlEncode(string value, int startIndex, int characterCount, TextWriter output)
        {
            _encoder.Encode(value, startIndex, characterCount, output);
        }
    }

    internal sealed class JavaScriptStringEncoder : IJavaScriptStringEncoder
    {
        System.Text.Encodings.Web.DefaultJavaScriptEncoder _encoder;
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
                    throw new ArgumentNullException("value");
                }
                s_default = value;
            }
        }

        public JavaScriptStringEncoder()
        {
            _encoder = System.Text.Encodings.Web.DefaultJavaScriptEncoder.Singleton;
        }
        public JavaScriptStringEncoder(CodePointFilter filter)
        {
            _encoder = new System.Text.Encodings.Web.DefaultJavaScriptEncoder(filter);
        }

        public JavaScriptStringEncoder(UnicodeRange allowedRange) : this(new CodePointFilter(allowedRange))
        { }

        public JavaScriptStringEncoder(params UnicodeRange[] allowedRanges) : this(new CodePointFilter(allowedRanges))
        { }

        public void JavaScriptStringEncode(char[] value, int startIndex, int characterCount, TextWriter output)
        {
            _encoder.Encode(value, startIndex, characterCount, output);
        }

        public string JavaScriptStringEncode(string value)
        {
            return _encoder.Encode(value);
        }

        public void JavaScriptStringEncode(string value, int startIndex, int characterCount, TextWriter output)
        {
            _encoder.Encode(value, startIndex, characterCount, output);
        }
    }

    internal sealed class UrlEncoder : IUrlEncoder
    {
        System.Text.Encodings.Web.DefaultUrlEncoder _encoder;
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
                    throw new ArgumentNullException("value");
                }
                s_default = value;
            }
        }

        public UrlEncoder()
        {
            _encoder = System.Text.Encodings.Web.DefaultUrlEncoder.Singleton;
        }
        public UrlEncoder(CodePointFilter filter)
        {
            _encoder = new System.Text.Encodings.Web.DefaultUrlEncoder(filter);
        }

        public UrlEncoder(UnicodeRange allowedRange) : this(new CodePointFilter(allowedRange))
        { }

        public UrlEncoder(params UnicodeRange[] allowedRanges) : this(new CodePointFilter(allowedRanges))
        { }

        public void UrlEncode(char[] value, int startIndex, int characterCount, TextWriter output)
        {
            _encoder.Encode(value, startIndex, characterCount, output);
        }

        public string UrlEncode(string value)
        {
            return _encoder.Encode(value); ;
        }

        public void UrlEncode(string value, int startIndex, int characterCount, TextWriter output)
        {
            _encoder.Encode(value, startIndex, characterCount, output);
        }
    }
}
