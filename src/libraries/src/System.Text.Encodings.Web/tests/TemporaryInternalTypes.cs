// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading;

namespace Microsoft.Framework.WebEncoders
{
    /// <summary>
    /// A class which can perform HTML encoding given an allow list of characters which
    /// can be represented unencoded.
    /// </summary>
    /// <remarks>
    /// Instances of this type will always encode a certain set of characters (such as &lt;
    /// and &gt;), even if the filter provided in the constructor allows such characters.
    /// Once constructed, instances of this class are thread-safe for multiple callers.
    /// </remarks>
    internal unsafe sealed class HtmlEncoderOld : IHtmlEncoder
    {
        // The default HtmlEncoder (Basic Latin), instantiated on demand
        private static HtmlEncoderOld _defaultEncoder;

        // The inner encoder, responsible for the actual encoding routines
        private readonly HtmlUnicodeEncoder _innerUnicodeEncoder;

        /// <summary>
        /// Instantiates an encoder using <see cref="UnicodeRanges.BasicLatin"/> as its allow list.
        /// Any character not in the <see cref="UnicodeRanges.BasicLatin"/> range will be escaped.
        /// </summary>
        public HtmlEncoderOld()
            : this(HtmlUnicodeEncoder.BasicLatin)
        {
        }

        /// <summary>
        /// Instantiates an encoder specifying which Unicode character ranges are allowed to
        /// pass through the encoder unescaped. Any character not in the set of ranges specified
        /// by <paramref name="allowedRanges"/> will be escaped.
        /// </summary>
        public HtmlEncoderOld(params UnicodeRange[] allowedRanges)
            : this(new HtmlUnicodeEncoder(new TextEncoderSettings(allowedRanges)))
        {
        }

        /// <summary>
        /// Instantiates an encoder using a custom code point filter. Any character not in the
        /// set returned by <paramref name="settings"/>'s <see cref="TextEncoderSettings.GetAllowedCodePoints"/>
        /// method will be escaped.
        /// </summary>
        public HtmlEncoderOld(TextEncoderSettings settings)
            : this(new HtmlUnicodeEncoder(settings))
        {
        }

        private HtmlEncoderOld(HtmlUnicodeEncoder innerEncoder)
        {
            Debug.Assert(innerEncoder != null);
            _innerUnicodeEncoder = innerEncoder;
        }

        /// <summary>
        /// A default instance of <see cref="HtmlEncoderOld"/>.
        /// </summary>
        /// <remarks>
        /// This normally corresponds to <see cref="UnicodeRanges.BasicLatin"/>. However, this property is
        /// settable so that a developer can change the default implementation application-wide.
        /// </remarks>
        public static HtmlEncoderOld Default
        {
            get
            {
                return Volatile.Read(ref _defaultEncoder) ?? CreateDefaultEncoderSlow();
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                Volatile.Write(ref _defaultEncoder, value);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // the JITter can attempt to inline the caller itself without worrying about us
        private static HtmlEncoderOld CreateDefaultEncoderSlow()
        {
            var onDemandEncoder = new HtmlEncoderOld();
            return Interlocked.CompareExchange(ref _defaultEncoder, onDemandEncoder, null) ?? onDemandEncoder;
        }

        /// <summary>
        /// Everybody's favorite HtmlEncode routine.
        /// </summary>
        public void HtmlEncode(char[] value, int startIndex, int characterCount, TextWriter output)
        {
            _innerUnicodeEncoder.Encode(value, startIndex, characterCount, output);
        }

        /// <summary>
        /// Everybody's favorite HtmlEncode routine.
        /// </summary>
        public string HtmlEncode(string value)
        {
            return _innerUnicodeEncoder.Encode(value);
        }

        /// <summary>
        /// Everybody's favorite HtmlEncode routine.
        /// </summary>
        public void HtmlEncode(string value, int startIndex, int characterCount, TextWriter output)
        {
            _innerUnicodeEncoder.Encode(value, startIndex, characterCount, output);
        }

        private sealed class HtmlUnicodeEncoder : UnicodeEncoderBase
        {
            // A singleton instance of the basic latin encoder.
            private static HtmlUnicodeEncoder _basicLatinSingleton;

            // The worst case encoding is 8 output chars per input char: [input] U+FFFF -> [output] "&#xFFFF;"
            // We don't need to worry about astral code points since they consume *two* input chars to
            // generate at most 10 output chars ("&#x10FFFF;"), which equates to 5 output chars per input char.
            private const int MaxOutputCharsPerInputChar = 8;

            internal HtmlUnicodeEncoder(TextEncoderSettings filter)
                : base(filter, MaxOutputCharsPerInputChar)
            {
            }

            internal static HtmlUnicodeEncoder BasicLatin
            {
                get
                {
                    HtmlUnicodeEncoder encoder = Volatile.Read(ref _basicLatinSingleton);
                    if (encoder == null)
                    {
                        encoder = new HtmlUnicodeEncoder(new TextEncoderSettings(UnicodeRanges.BasicLatin));
                        Volatile.Write(ref _basicLatinSingleton, encoder);
                    }
                    return encoder;
                }
            }

            // Writes a scalar value as an HTML-encoded entity.
            protected override void WriteEncodedScalar(ref Writer writer, uint value)
            {
                if (value == (uint)'\"') { writer.Write("&quot;"); }
                else if (value == (uint)'&') { writer.Write("&amp;"); }
                else if (value == (uint)'<') { writer.Write("&lt;"); }
                else if (value == (uint)'>') { writer.Write("&gt;"); }
                else { WriteEncodedScalarAsNumericEntity(ref writer, value); }
            }

            // Writes a scalar value as an HTML-encoded numeric entity.
            private static void WriteEncodedScalarAsNumericEntity(ref Writer writer, uint value)
            {
                // We're building the characters up in reverse
                char* chars = stackalloc char[8 /* "FFFFFFFF" */];
                int numCharsWritten = 0;
                do
                {
                    Debug.Assert(numCharsWritten < 8, "Couldn't have written 8 characters out by this point.");
                    // Pop off the last nibble
                    chars[numCharsWritten++] = HexUtil.UInt32LsbToHexDigit(value & 0xFU);
                    value >>= 4;
                } while (value != 0);

                // Finally, write out the HTML-encoded scalar value.
                writer.Write('&');
                writer.Write('#');
                writer.Write('x');
                Debug.Assert(numCharsWritten > 0, "At least one character should've been written.");
                do
                {
                    writer.Write(chars[--numCharsWritten]);
                } while (numCharsWritten != 0);
                writer.Write(';');
            }
        }
    }

    /// <summary>
    /// A class which can perform JavaScript string escaping given an allow list of characters which
    /// can be represented unescaped.
    /// </summary>
    /// <remarks>
    /// Instances of this type will always encode a certain set of characters (such as '
    /// and "), even if the filter provided in the constructor allows such characters.
    /// Once constructed, instances of this class are thread-safe for multiple callers.
    /// </remarks>
    internal sealed class JavaScriptStringEncoderOld : IJavaScriptStringEncoder
    {
        // The default JavaScript string encoder (Basic Latin), instantiated on demand
        private static JavaScriptStringEncoderOld _defaultEncoder;

        // The inner encoder, responsible for the actual encoding routines
        private readonly JavaScriptStringUnicodeEncoder _innerUnicodeEncoder;

        /// <summary>
        /// Instantiates an encoder using <see cref="UnicodeRanges.BasicLatin"/> as its allow list.
        /// Any character not in the <see cref="UnicodeRanges.BasicLatin"/> range will be escaped.
        /// </summary>
        public JavaScriptStringEncoderOld()
            : this(JavaScriptStringUnicodeEncoder.BasicLatin)
        {
        }

        /// <summary>
        /// Instantiates an encoder specifying which Unicode character ranges are allowed to
        /// pass through the encoder unescaped. Any character not in the set of ranges specified
        /// by <paramref name="allowedRanges"/> will be escaped.
        /// </summary>
        public JavaScriptStringEncoderOld(params UnicodeRange[] allowedRanges)
            : this(new JavaScriptStringUnicodeEncoder(new TextEncoderSettings(allowedRanges)))
        {
        }

        /// <summary>
        /// Instantiates an encoder using a custom code point filter. Any character not in the
        /// set returned by <paramref name="settings"/>'s <see cref="TextEncoderSettings.GetAllowedCodePoints"/>
        /// method will be escaped.
        /// </summary>
        public JavaScriptStringEncoderOld(TextEncoderSettings settings)
            : this(new JavaScriptStringUnicodeEncoder(settings))
        {
        }

        private JavaScriptStringEncoderOld(JavaScriptStringUnicodeEncoder innerEncoder)
        {
            Debug.Assert(innerEncoder != null);
            _innerUnicodeEncoder = innerEncoder;
        }

        /// <summary>
        /// A default instance of <see cref="JavaScriptStringEncoderOld"/>.
        /// </summary>
        /// <remarks>
        /// This normally corresponds to <see cref="UnicodeRanges.BasicLatin"/>. However, this property is
        /// settable so that a developer can change the default implementation application-wide.
        /// </remarks>
        public static JavaScriptStringEncoderOld Default
        {
            get
            {
                return Volatile.Read(ref _defaultEncoder) ?? CreateDefaultEncoderSlow();
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                Volatile.Write(ref _defaultEncoder, value);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // the JITter can attempt to inline the caller itself without worrying about us
        private static JavaScriptStringEncoderOld CreateDefaultEncoderSlow()
        {
            var onDemandEncoder = new JavaScriptStringEncoderOld();
            return Interlocked.CompareExchange(ref _defaultEncoder, onDemandEncoder, null) ?? onDemandEncoder;
        }

        /// <summary>
        /// Everybody's favorite JavaScriptStringEncode routine.
        /// </summary>
        public void JavaScriptStringEncode(char[] value, int startIndex, int characterCount, TextWriter output)
        {
            _innerUnicodeEncoder.Encode(value, startIndex, characterCount, output);
        }

        /// <summary>
        /// Everybody's favorite JavaScriptStringEncode routine.
        /// </summary>
        public string JavaScriptStringEncode(string value)
        {
            return _innerUnicodeEncoder.Encode(value);
        }

        /// <summary>
        /// Everybody's favorite JavaScriptStringEncode routine.
        /// </summary>
        public void JavaScriptStringEncode(string value, int startIndex, int characterCount, TextWriter output)
        {
            _innerUnicodeEncoder.Encode(value, startIndex, characterCount, output);
        }

        private sealed class JavaScriptStringUnicodeEncoder : UnicodeEncoderBase
        {
            // A singleton instance of the basic latin encoder.
            private static JavaScriptStringUnicodeEncoder _basicLatinSingleton;

            // The worst case encoding is 6 output chars per input char: [input] U+FFFF -> [output] "\uFFFF"
            // We don't need to worry about astral code points since they're represented as encoded
            // surrogate pairs in the output.
            private const int MaxOutputCharsPerInputChar = 6;

            internal JavaScriptStringUnicodeEncoder(TextEncoderSettings filter)
                : base(filter, MaxOutputCharsPerInputChar)
            {
                // The only interesting characters above and beyond what the base encoder
                // already covers are the solidus and reverse solidus.
                ForbidCharacter('\\');
                ForbidCharacter('/');
            }

            internal static JavaScriptStringUnicodeEncoder BasicLatin
            {
                get
                {
                    JavaScriptStringUnicodeEncoder encoder = Volatile.Read(ref _basicLatinSingleton);
                    if (encoder == null)
                    {
                        encoder = new JavaScriptStringUnicodeEncoder(new TextEncoderSettings(UnicodeRanges.BasicLatin));
                        Volatile.Write(ref _basicLatinSingleton, encoder);
                    }
                    return encoder;
                }
            }

            // Writes a scalar value as a JavaScript-escaped character (or sequence of characters).
            // See ECMA-262, Sec. 7.8.4, and ECMA-404, Sec. 9
            // http://www.ecma-international.org/ecma-262/5.1/#sec-7.8.4
            // http://www.ecma-international.org/publications/files/ECMA-ST/ECMA-404.pdf
            protected override void WriteEncodedScalar(ref Writer writer, uint value)
            {
                // ECMA-262 allows encoding U+000B as "\v", but ECMA-404 does not.
                // Both ECMA-262 and ECMA-404 allow encoding U+002F SOLIDUS as "\/".
                // (In ECMA-262 this character is a NonEscape character.)
                // HTML-specific characters (including apostrophe and quotes) will
                // be written out as numeric entities for defense-in-depth.
                // See UnicodeEncoderBase ctor comments for more info.

                if (value == (uint)'\b') { writer.Write(@"\b"); }
                else if (value == (uint)'\t') { writer.Write(@"\t"); }
                else if (value == (uint)'\n') { writer.Write(@"\n"); }
                else if (value == (uint)'\f') { writer.Write(@"\f"); }
                else if (value == (uint)'\r') { writer.Write(@"\r"); }
                else if (value == (uint)'/') { writer.Write(@"\/"); }
                else if (value == (uint)'\\') { writer.Write(@"\\"); }
                else { WriteEncodedScalarAsNumericEntity(ref writer, value); }
            }

            // Writes a scalar value as an JavaScript-escaped character (or sequence of characters).
            private static void WriteEncodedScalarAsNumericEntity(ref Writer writer, uint value)
            {
                if (UnicodeHelpers.IsSupplementaryCodePoint((int)value))
                {
                    // Convert this back to UTF-16 and write out both characters.
                    char leadingSurrogate, trailingSurrogate;
                    UnicodeHelpers.GetUtf16SurrogatePairFromAstralScalarValue((int)value, out leadingSurrogate, out trailingSurrogate);
                    WriteEncodedSingleCharacter(ref writer, leadingSurrogate);
                    WriteEncodedSingleCharacter(ref writer, trailingSurrogate);
                }
                else
                {
                    // This is only a single character.
                    WriteEncodedSingleCharacter(ref writer, value);
                }
            }

            // Writes an encoded scalar value (in the BMP) as a JavaScript-escaped character.
            private static void WriteEncodedSingleCharacter(ref Writer writer, uint value)
            {
                Debug.Assert(!UnicodeHelpers.IsSupplementaryCodePoint((int)value), "The incoming value should've been in the BMP.");

                // Encode this as 6 chars "\uFFFF".
                writer.Write('\\');
                writer.Write('u');
                writer.Write(HexUtil.UInt32LsbToHexDigit(value >> 12));
                writer.Write(HexUtil.UInt32LsbToHexDigit((value >> 8) & 0xFU));
                writer.Write(HexUtil.UInt32LsbToHexDigit((value >> 4) & 0xFU));
                writer.Write(HexUtil.UInt32LsbToHexDigit(value & 0xFU));
            }
        }
    }


    /// <summary>
    /// A class which can perform URL string escaping given an allow list of characters which
    /// can be represented unescaped.
    /// </summary>
    /// <remarks>
    /// Instances of this type will always encode a certain set of characters (such as +
    /// and ?), even if the filter provided in the constructor allows such characters.
    /// Once constructed, instances of this class are thread-safe for multiple callers.
    /// </remarks>
    internal sealed class UrlEncoderOld : IUrlEncoder
    {
        // The default URL string encoder (Basic Latin), instantiated on demand
        private static UrlEncoderOld _defaultEncoder;

        // The inner encoder, responsible for the actual encoding routines
        private readonly UrlUnicodeEncoder _innerUnicodeEncoder;

        /// <summary>
        /// Instantiates an encoder using <see cref="UnicodeRanges.BasicLatin"/> as its allow list.
        /// Any character not in the <see cref="UnicodeRanges.BasicLatin"/> range will be escaped.
        /// </summary>
        public UrlEncoderOld()
            : this(UrlUnicodeEncoder.BasicLatin)
        {
        }

        /// <summary>
        /// Instantiates an encoder specifying which Unicode character ranges are allowed to
        /// pass through the encoder unescaped. Any character not in the set of ranges specified
        /// by <paramref name="allowedRanges"/> will be escaped.
        /// </summary>
        public UrlEncoderOld(params UnicodeRange[] allowedRanges)
            : this(new UrlUnicodeEncoder(new TextEncoderSettings(allowedRanges)))
        {
        }

        /// <summary>
        /// Instantiates an encoder using a custom code point filter. Any character not in the
        /// set returned by <paramref name="settings"/>'s <see cref="TextEncoderSettings.GetAllowedCodePoints"/>
        /// method will be escaped.
        /// </summary>
        public UrlEncoderOld(TextEncoderSettings settings)
            : this(new UrlUnicodeEncoder(settings))
        {
        }

        private UrlEncoderOld(UrlUnicodeEncoder innerEncoder)
        {
            Debug.Assert(innerEncoder != null);
            _innerUnicodeEncoder = innerEncoder;
        }

        /// <summary>
        /// A default instance of <see cref="UrlEncoderOld"/>.
        /// </summary>
        /// <remarks>
        /// This normally corresponds to <see cref="UnicodeRanges.BasicLatin"/>. However, this property is
        /// settable so that a developer can change the default implementation application-wide.
        /// </remarks>
        public static UrlEncoderOld Default
        {
            get
            {
                return Volatile.Read(ref _defaultEncoder) ?? CreateDefaultEncoderSlow();
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                Volatile.Write(ref _defaultEncoder, value);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // the JITter can attempt to inline the caller itself without worrying about us
        private static UrlEncoderOld CreateDefaultEncoderSlow()
        {
            var onDemandEncoder = new UrlEncoderOld();
            return Interlocked.CompareExchange(ref _defaultEncoder, onDemandEncoder, null) ?? onDemandEncoder;
        }

        /// <summary>
        /// Everybody's favorite UrlEncode routine.
        /// </summary>
        public void UrlEncode(char[] value, int startIndex, int characterCount, TextWriter output)
        {
            _innerUnicodeEncoder.Encode(value, startIndex, characterCount, output);
        }

        /// <summary>
        /// Everybody's favorite UrlEncode routine.
        /// </summary>
        public string UrlEncode(string value)
        {
            return _innerUnicodeEncoder.Encode(value);
        }

        /// <summary>
        /// Everybody's favorite UrlEncode routine.
        /// </summary>
        public void UrlEncode(string value, int startIndex, int characterCount, TextWriter output)
        {
            _innerUnicodeEncoder.Encode(value, startIndex, characterCount, output);
        }

        private sealed class UrlUnicodeEncoder : UnicodeEncoderBase
        {
            // A singleton instance of the basic latin encoder.
            private static UrlUnicodeEncoder _basicLatinSingleton;

            // We perform UTF8 conversion of input, which means that the worst case is
            // 9 output chars per input char: [input] U+FFFF -> [output] "%XX%YY%ZZ".
            // We don't need to worry about astral code points since they consume 2 input
            // chars to produce 12 output chars "%XX%YY%ZZ%WW", which is 6 output chars per input char.
            private const int MaxOutputCharsPerInputChar = 9;

            internal UrlUnicodeEncoder(TextEncoderSettings filter)
                : base(filter, MaxOutputCharsPerInputChar)
            {
                // Per RFC 3987, Sec. 2.2, we want encodings that are safe for
                // four particular components: 'isegment', 'ipath-noscheme',
                // 'iquery', and 'ifragment'. The relevant definitions are below.
                //
                //    ipath-noscheme = isegment-nz-nc *( "/" isegment )
                // 
                //    isegment       = *ipchar
                // 
                //    isegment-nz-nc = 1*( iunreserved / pct-encoded / sub-delims
                //                         / "@" )
                //                   ; non-zero-length segment without any colon ":"
                //
                //    ipchar         = iunreserved / pct-encoded / sub-delims / ":"
                //                   / "@"
                // 
                //    iquery         = *( ipchar / iprivate / "/" / "?" )
                // 
                //    ifragment      = *( ipchar / "/" / "?" )
                // 
                //    iunreserved    = ALPHA / DIGIT / "-" / "." / "_" / "~" / ucschar
                // 
                //    ucschar        = %xA0-D7FF / %xF900-FDCF / %xFDF0-FFEF
                //                   / %x10000-1FFFD / %x20000-2FFFD / %x30000-3FFFD
                //                   / %x40000-4FFFD / %x50000-5FFFD / %x60000-6FFFD
                //                   / %x70000-7FFFD / %x80000-8FFFD / %x90000-9FFFD
                //                   / %xA0000-AFFFD / %xB0000-BFFFD / %xC0000-CFFFD
                //                   / %xD0000-DFFFD / %xE1000-EFFFD
                // 
                //    pct-encoded    = "%" HEXDIG HEXDIG
                // 
                //    sub-delims     = "!" / "$" / "&" / "'" / "(" / ")"
                //                   / "*" / "+" / "," / ";" / "="
                //
                // The only common characters between these four components are the
                // intersection of 'isegment-nz-nc' and 'ipchar', which is really
                // just 'isegment-nz-nc' (colons forbidden).
                // 
                // From this list, the base encoder already forbids "&", "'", "+",
                // and we'll additionally forbid "=" since it has special meaning
                // in x-www-form-urlencoded representations.
                //
                // This means that the full list of allowed characters from the
                // Basic Latin set is:
                // ALPHA / DIGIT / "-" / "." / "_" / "~" / "!" / "$" / "(" / ")" / "*" / "," / ";" / "@"

                const string forbiddenChars = @" #%/:=?[\]^`{|}"; // chars from Basic Latin which aren't already disallowed by the base encoder
                foreach (char c in forbiddenChars)
                {
                    ForbidCharacter(c);
                }

                // Specials (U+FFF0 .. U+FFFF) are forbidden by the definition of 'ucschar' above
                for (int i = 0; i < 16; i++)
                {
                    ForbidCharacter((char)(0xFFF0 | i));
                }

                // Supplementary characters are forbidden anyway by the base encoder
            }

            internal static UrlUnicodeEncoder BasicLatin
            {
                get
                {
                    UrlUnicodeEncoder encoder = Volatile.Read(ref _basicLatinSingleton);
                    if (encoder == null)
                    {
                        encoder = new UrlUnicodeEncoder(new TextEncoderSettings(UnicodeRanges.BasicLatin));
                        Volatile.Write(ref _basicLatinSingleton, encoder);
                    }
                    return encoder;
                }
            }

            // Writes a scalar value as a percent-encoded sequence of UTF8 bytes, per RFC 3987.
            protected override void WriteEncodedScalar(ref Writer writer, uint value)
            {
                uint asUtf8 = (uint)UnicodeHelpers.GetUtf8RepresentationForScalarValue(value);
                do
                {
                    char highNibble, lowNibble;
                    HexUtil.ByteToHexDigits((byte)asUtf8, out highNibble, out lowNibble);
                    writer.Write('%');
                    writer.Write(highNibble);
                    writer.Write(lowNibble);
                } while ((asUtf8 >>= 8) != 0);
            }
        }
    }
}
