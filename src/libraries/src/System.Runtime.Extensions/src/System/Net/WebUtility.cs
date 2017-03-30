// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Don't entity encode high chars (160 to 256)
#define ENTITY_ENCODE_HIGH_ASCII_CHARS

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Net
{
    public static class WebUtility
    {
        // some consts copied from Char / CharUnicodeInfo since we don't have friend access to those types
        private const char HIGH_SURROGATE_START = '\uD800';
        private const char LOW_SURROGATE_START = '\uDC00';
        private const char LOW_SURROGATE_END = '\uDFFF';
        private const int UNICODE_PLANE00_END = 0x00FFFF;
        private const int UNICODE_PLANE01_START = 0x10000;
        private const int UNICODE_PLANE16_END = 0x10FFFF;

        private const int UnicodeReplacementChar = '\uFFFD';

        #region HtmlEncode / HtmlDecode methods

        private static readonly char[] s_htmlEntityEndingChars = new char[] { ';', '&' };

        public static string HtmlEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            // Don't create StringBuilder if we don't have anything to encode
            int index = IndexOfHtmlEncodingChars(value, 0);
            if (index == -1)
            {
                return value;
            }

            StringBuilder sb = StringBuilderCache.Acquire(value.Length);
            HtmlEncode(value, index, sb);
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public static void HtmlEncode(string value, TextWriter output)
        {
            output.Write(HtmlEncode(value));
        }

        private static unsafe void HtmlEncode(string value, int index, StringBuilder output)
        {
            Debug.Assert(value != null);
            Debug.Assert(output != null);
            Debug.Assert(0 <= index && index <= value.Length, "0 <= index && index <= value.Length");

            int cch = value.Length - index;
            fixed (char* str = value)
            {
                char* pch = str;
                while (index-- > 0)
                {
                    output.Append(*pch++);
                }

                for (; cch > 0; cch--, pch++)
                {
                    char ch = *pch;
                    if (ch <= '>')
                    {
                        switch (ch)
                        {
                            case '<':
                                output.Append("&lt;");
                                break;
                            case '>':
                                output.Append("&gt;");
                                break;
                            case '"':
                                output.Append("&quot;");
                                break;
                            case '\'':
                                output.Append("&#39;");
                                break;
                            case '&':
                                output.Append("&amp;");
                                break;
                            default:
                                output.Append(ch);
                                break;
                        }
                    }
                    else
                    {
                        int valueToEncode = -1; // set to >= 0 if needs to be encoded

#if ENTITY_ENCODE_HIGH_ASCII_CHARS
                        if (ch >= 160 && ch < 256)
                        {
                            // The seemingly arbitrary 160 comes from RFC
                            valueToEncode = ch;
                        }
                        else
#endif // ENTITY_ENCODE_HIGH_ASCII_CHARS
                        if (Char.IsSurrogate(ch))
                        {
                            int scalarValue = GetNextUnicodeScalarValueFromUtf16Surrogate(ref pch, ref cch);
                            if (scalarValue >= UNICODE_PLANE01_START)
                            {
                                valueToEncode = scalarValue;
                            }
                            else
                            {
                                // Don't encode BMP characters (like U+FFFD) since they wouldn't have
                                // been encoded if explicitly present in the string anyway.
                                ch = (char)scalarValue;
                            }
                        }

                        if (valueToEncode >= 0)
                        {
                            // value needs to be encoded
                            output.Append("&#");
                            output.Append(valueToEncode.ToString(CultureInfo.InvariantCulture));
                            output.Append(';');
                        }
                        else
                        {
                            // write out the character directly
                            output.Append(ch);
                        }
                    }
                }
            }
        }

        public static string HtmlDecode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            // Don't create StringBuilder if we don't have anything to encode
            if (!StringRequiresHtmlDecoding(value))
            {
                return value;
            }

            StringBuilder sb = StringBuilderCache.Acquire(value.Length);
            HtmlDecode(value, sb);
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public static void HtmlDecode(string value, TextWriter output)
        {
            output.Write(HtmlDecode(value));
        }

        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", MessageId = "System.UInt16.TryParse(System.String,System.Globalization.NumberStyles,System.IFormatProvider,System.UInt16@)", Justification = "UInt16.TryParse guarantees that result is zero if the parse fails.")]
        private static void HtmlDecode(string value, StringBuilder output)
        {
            Debug.Assert(output != null);

            int l = value.Length;
            for (int i = 0; i < l; i++)
            {
                char ch = value[i];

                if (ch == '&')
                {
                    // We found a '&'. Now look for the next ';' or '&'. The idea is that
                    // if we find another '&' before finding a ';', then this is not an entity,
                    // and the next '&' might start a real entity (VSWhidbey 275184)
                    int index = value.IndexOfAny(s_htmlEntityEndingChars, i + 1);
                    if (index > 0 && value[index] == ';')
                    {
                        int entityOffset = i + 1;
                        int entityLength = index - entityOffset;

                        if (entityLength > 1 && value[entityOffset] == '#')
                        {
                            // The # syntax can be in decimal or hex, e.g.
                            //      &#229;  --> decimal
                            //      &#xE5;  --> same char in hex
                            // See http://www.w3.org/TR/REC-html40/charset.html#entities

                            bool parsedSuccessfully;
                            uint parsedValue;
                            if (value[entityOffset + 1] == 'x' || value[entityOffset + 1] == 'X')
                            {
                                parsedSuccessfully = uint.TryParse(value.Substring(entityOffset + 2, entityLength - 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out parsedValue);
                            }
                            else
                            {
                                parsedSuccessfully = uint.TryParse(value.Substring(entityOffset + 1, entityLength - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedValue);
                            }

                            if (parsedSuccessfully)
                            {
                                // decoded character must be U+0000 .. U+10FFFF, excluding surrogates
                                parsedSuccessfully = ((parsedValue < HIGH_SURROGATE_START) || (LOW_SURROGATE_END < parsedValue && parsedValue <= UNICODE_PLANE16_END));
                            }

                            if (parsedSuccessfully)
                            {
                                if (parsedValue <= UNICODE_PLANE00_END)
                                {
                                    // single character
                                    output.Append((char)parsedValue);
                                }
                                else
                                {
                                    // multi-character
                                    char leadingSurrogate, trailingSurrogate;
                                    ConvertSmpToUtf16(parsedValue, out leadingSurrogate, out trailingSurrogate);
                                    output.Append(leadingSurrogate);
                                    output.Append(trailingSurrogate);
                                }

                                i = index; // already looked at everything until semicolon
                                continue;
                            }
                        }
                        else
                        {
                            string entity = value.Substring(entityOffset, entityLength);
                            i = index; // already looked at everything until semicolon

                            char entityChar = HtmlEntities.Lookup(entity);
                            if (entityChar != (char)0)
                            {
                                ch = entityChar;
                            }
                            else
                            {
                                output.Append('&');
                                output.Append(entity);
                                output.Append(';');
                                continue;
                            }
                        }
                    }
                }

                output.Append(ch);
            }
        }

        private static unsafe int IndexOfHtmlEncodingChars(string s, int startPos)
        {
            Debug.Assert(0 <= startPos && startPos <= s.Length, "0 <= startPos && startPos <= s.Length");

            int cch = s.Length - startPos;
            fixed (char* str = s)
            {
                for (char* pch = &str[startPos]; cch > 0; pch++, cch--)
                {
                    char ch = *pch;
                    if (ch <= '>')
                    {
                        switch (ch)
                        {
                            case '<':
                            case '>':
                            case '"':
                            case '\'':
                            case '&':
                                return s.Length - cch;
                        }
                    }
#if ENTITY_ENCODE_HIGH_ASCII_CHARS
                    else if (ch >= 160 && ch < 256)
                    {
                        return s.Length - cch;
                    }
#endif // ENTITY_ENCODE_HIGH_ASCII_CHARS
                    else if (Char.IsSurrogate(ch))
                    {
                        return s.Length - cch;
                    }
                }
            }

            return -1;
        }

        #endregion

        #region UrlEncode implementation
        
        private static void GetEncodedBytes(byte[] originalBytes, int offset, int count, byte[] expandedBytes)
        {
            int pos = 0;
            int end = offset + count;
            Debug.Assert(offset < end && end <= originalBytes.Length);
            for (int i = offset; i < end; i++)
            {
#if DEBUG
                // Make sure we never overwrite any bytes if originalBytes and
                // expandedBytes refer to the same array
                if (originalBytes == expandedBytes)
                {
                    Debug.Assert(i >= pos);
                }
#endif

                byte b = originalBytes[i];
                char ch = (char)b;
                if (IsUrlSafeChar(ch))
                {
                    expandedBytes[pos++] = b;
                }
                else if (ch == ' ')
                {
                    expandedBytes[pos++] = (byte)'+';
                }
                else
                {
                    expandedBytes[pos++] = (byte)'%';
                    expandedBytes[pos++] = (byte)IntToHex((b >> 4) & 0xf);
                    expandedBytes[pos++] = (byte)IntToHex(b & 0x0f);
                }
            }
        }

#endregion

#region UrlEncode public methods

        [SuppressMessage("Microsoft.Design", "CA1055:UriReturnValuesShouldNotBeStrings", Justification = "Already shipped public API; code moved here as part of API consolidation")]
        public static string UrlEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            int safeCount = 0;
            int spaceCount = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                if (IsUrlSafeChar(ch))
                {
                    safeCount++;
                }
                else if (ch == ' ')
                {
                    spaceCount++;
                }
            }

            int unexpandedCount = safeCount + spaceCount;
            if (unexpandedCount == value.Length)
            {
                if (spaceCount != 0)
                {
                    // Only spaces to encode
                    return value.Replace(' ', '+');
                }

                // Nothing to expand
                return value;
            }

            int byteCount = Encoding.UTF8.GetByteCount(value);
            int unsafeByteCount = byteCount - unexpandedCount;
            int byteIndex = unsafeByteCount * 2;

            // Instead of allocating one array of length `byteCount` to store
            // the UTF-8 encoded bytes, and then a second array of length 
            // `3 * byteCount - 2 * unexpandedCount`
            // to store the URL-encoded UTF-8 bytes, we allocate a single array of
            // the latter and encode the data in place, saving the first allocation.
            // We store the UTF-8 bytes to the end of this array, and then URL encode to the
            // beginning of the array.
            byte[] newBytes = new byte[byteCount + byteIndex];
            Encoding.UTF8.GetBytes(value, 0, value.Length, newBytes, byteIndex);
            
            GetEncodedBytes(newBytes, byteIndex, byteCount, newBytes);
            return Encoding.UTF8.GetString(newBytes);
        }

        public static byte[] UrlEncodeToBytes(byte[] value, int offset, int count)
        {
            if (!ValidateUrlEncodingParameters(value, offset, count))
            {
                return null;
            }

            bool foundSpaces = false;
            int unsafeCount = 0;

            // count them first
            for (int i = 0; i < count; i++)
            {
                char ch = (char)value[offset + i];

                if (ch == ' ')
                    foundSpaces = true;
                else if (!IsUrlSafeChar(ch))
                    unsafeCount++;
            }

            // nothing to expand?
            if (!foundSpaces && unsafeCount == 0)
            {
                var subarray = new byte[count];
                Buffer.BlockCopy(value, offset, subarray, 0, count);
                return subarray;
            }

            // expand not 'safe' characters into %XX, spaces to +s
            byte[] expandedBytes = new byte[count + unsafeCount * 2];
            GetEncodedBytes(value, offset, count, expandedBytes);
            return expandedBytes;
        }

#endregion

#region UrlDecode implementation

        private static string UrlDecodeInternal(string value, Encoding encoding)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            int count = value.Length;
            UrlDecoder helper = new UrlDecoder(count, encoding);

            // go through the string's chars collapsing %XX and
            // appending each char as char, with exception of %XX constructs
            // that are appended as bytes
            bool needsDecodingUnsafe = false;
            bool needsDecodingSpaces = false;
            for (int pos = 0; pos < count; pos++)
            {
                char ch = value[pos];

                if (ch == '+')
                {
                    needsDecodingSpaces = true;
                    ch = ' ';
                }
                else if (ch == '%' && pos < count - 2)
                {
                    int h1 = HexToInt(value[pos + 1]);
                    int h2 = HexToInt(value[pos + 2]);

                    if (h1 >= 0 && h2 >= 0)
                    {     // valid 2 hex chars
                        byte b = (byte)((h1 << 4) | h2);
                        pos += 2;

                        // don't add as char
                        helper.AddByte(b);
                        needsDecodingUnsafe = true;
                        continue;
                    }
                }

                if ((ch & 0xFF80) == 0)
                    helper.AddByte((byte)ch); // 7 bit have to go as bytes because of Unicode
                else
                    helper.AddChar(ch);
            }
            
            if (!needsDecodingUnsafe)
            {
                if (needsDecodingSpaces)
                {
                    // Only spaces to decode
                    return value.Replace('+', ' ');
                }

                // Nothing to decode
                return value;
            }

            return helper.GetString();
        }

        private static byte[] UrlDecodeInternal(byte[] bytes, int offset, int count)
        {
            if (!ValidateUrlEncodingParameters(bytes, offset, count))
            {
                return null;
            }

            int decodedBytesCount = 0;
            byte[] decodedBytes = new byte[count];

            for (int i = 0; i < count; i++)
            {
                int pos = offset + i;
                byte b = bytes[pos];

                if (b == '+')
                {
                    b = (byte)' ';
                }
                else if (b == '%' && i < count - 2)
                {
                    int h1 = HexToInt((char)bytes[pos + 1]);
                    int h2 = HexToInt((char)bytes[pos + 2]);

                    if (h1 >= 0 && h2 >= 0)
                    {     // valid 2 hex chars
                        b = (byte)((h1 << 4) | h2);
                        i += 2;
                    }
                }

                decodedBytes[decodedBytesCount++] = b;
            }

            if (decodedBytesCount < decodedBytes.Length)
            {
                Array.Resize(ref decodedBytes, decodedBytesCount);
            }

            return decodedBytes;
        }

#endregion

#region UrlDecode public methods


        [SuppressMessage("Microsoft.Design", "CA1055:UriReturnValuesShouldNotBeStrings", Justification = "Already shipped public API; code moved here as part of API consolidation")]
        public static string UrlDecode(string encodedValue)
        {
            return UrlDecodeInternal(encodedValue, Encoding.UTF8);
        }

        public static byte[] UrlDecodeToBytes(byte[] encodedValue, int offset, int count)
        {
            return UrlDecodeInternal(encodedValue, offset, count);
        }

#endregion

#region Helper methods

        // similar to Char.ConvertFromUtf32, but doesn't check arguments or generate strings
        // input is assumed to be an SMP character
        private static void ConvertSmpToUtf16(uint smpChar, out char leadingSurrogate, out char trailingSurrogate)
        {
            Debug.Assert(UNICODE_PLANE01_START <= smpChar && smpChar <= UNICODE_PLANE16_END);

            int utf32 = (int)(smpChar - UNICODE_PLANE01_START);
            leadingSurrogate = (char)((utf32 / 0x400) + HIGH_SURROGATE_START);
            trailingSurrogate = (char)((utf32 % 0x400) + LOW_SURROGATE_START);
        }

        private static unsafe int GetNextUnicodeScalarValueFromUtf16Surrogate(ref char* pch, ref int charsRemaining)
        {
            // invariants
            Debug.Assert(charsRemaining >= 1);
            Debug.Assert(Char.IsSurrogate(*pch));

            if (charsRemaining <= 1)
            {
                // not enough characters remaining to resurrect the original scalar value
                return UnicodeReplacementChar;
            }

            char leadingSurrogate = pch[0];
            char trailingSurrogate = pch[1];

            if (Char.IsSurrogatePair(leadingSurrogate, trailingSurrogate))
            {
                // we're going to consume an extra char
                pch++;
                charsRemaining--;

                // below code is from Char.ConvertToUtf32, but without the checks (since we just performed them)
                return (((leadingSurrogate - HIGH_SURROGATE_START) * 0x400) + (trailingSurrogate - LOW_SURROGATE_START) + UNICODE_PLANE01_START);
            }
            else
            {
                // unmatched surrogate
                return UnicodeReplacementChar;
            }
        }

        private static int HexToInt(char h)
        {
            return (h >= '0' && h <= '9') ? h - '0' :
            (h >= 'a' && h <= 'f') ? h - 'a' + 10 :
            (h >= 'A' && h <= 'F') ? h - 'A' + 10 :
            -1;
        }

        private static char IntToHex(int n)
        {
            Debug.Assert(n < 0x10);

            if (n <= 9)
                return (char)(n + (int)'0');
            else
                return (char)(n - 10 + (int)'A');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUrlSafeChar(char ch)
        {
            // Set of safe chars, from RFC 1738.4 minus '+'
            /*
            if (ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9')
                return true;

            switch (ch)
            {
                case '-':
                case '_':
                case '.':
                case '!':
                case '*':
                case '(':
                case ')':
                    return true;
            }

            return false;
            */
            // Optimized version of the above:

            int code = (int)ch;

            const int safeSpecialCharMask = 0x03FF0000 | // 0..9
                1 << ((int)'!' - 0x20) | // 0x21
                1 << ((int)'(' - 0x20) | // 0x28
                1 << ((int)')' - 0x20) | // 0x29
                1 << ((int)'*' - 0x20) | // 0x2A
                1 << ((int)'-' - 0x20) | // 0x2D
                1 << ((int)'.' - 0x20); // 0x2E

            unchecked
            {
                return ((uint)(code - 'a') <= (uint)('z' - 'a')) ||
                       ((uint)(code - 'A') <= (uint)('Z' - 'A')) ||
                       ((uint)(code - 0x20) <= (uint)('9' - 0x20) && ((1 << (code - 0x20)) & safeSpecialCharMask) != 0) ||
                       (code == (int)'_');
            }
        }

        private static bool ValidateUrlEncodingParameters(byte[] bytes, int offset, int count)
        {
            if (bytes == null && count == 0)
                return false;
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }
            if (offset < 0 || offset > bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (count < 0 || offset + count > bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            return true;
        }

        private static bool StringRequiresHtmlDecoding(string s)
        {
            // this string requires html decoding if it contains '&' or a surrogate character
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '&' || Char.IsSurrogate(c))
                {
                    return true;
                }
            }
            return false;
        }

#endregion

        // Internal struct to facilitate URL decoding -- keeps char buffer and byte buffer, allows appending of either chars or bytes
        private struct UrlDecoder
        {
            private int _bufferSize;

            // Accumulate characters in a special array
            private int _numChars;
            private char[] _charBuffer;

            // Accumulate bytes for decoding into characters in a special array
            private int _numBytes;
            private byte[] _byteBuffer;

            // Encoding to convert chars to bytes
            private Encoding _encoding;

            private void FlushBytes()
            {
                Debug.Assert(_numBytes > 0);
                if (_charBuffer == null)
                    _charBuffer = new char[_bufferSize];

                _numChars += _encoding.GetChars(_byteBuffer, 0, _numBytes, _charBuffer, _numChars);
                _numBytes = 0;
            }

            internal UrlDecoder(int bufferSize, Encoding encoding)
            {
                _bufferSize = bufferSize;
                _encoding = encoding;

                _charBuffer = null; // char buffer created on demand

                _numChars = 0;
                _numBytes = 0;
                _byteBuffer = null; // byte buffer created on demand
            }

            internal void AddChar(char ch)
            {
                if (_numBytes > 0)
                    FlushBytes();

                if (_charBuffer == null)
                    _charBuffer = new char[_bufferSize];

                _charBuffer[_numChars++] = ch;
            }

            internal void AddByte(byte b)
            {
                if (_byteBuffer == null)
                    _byteBuffer = new byte[_bufferSize];

                _byteBuffer[_numBytes++] = b;
            }

            internal String GetString()
            {
                if (_numBytes > 0)
                    FlushBytes();

                Debug.Assert(_numChars > 0);
                return new string(_charBuffer, 0, _numChars);
            }
        }

        // helper class for lookup of HTML encoding entities
        private static class HtmlEntities
        {
#if DEBUG
            static HtmlEntities()
            {
                // Make sure the initial capacity for s_lookupTable is correct
                Debug.Assert(s_lookupTable.Count == Count, $"There should be {Count} HTML entities, but {nameof(s_lookupTable)} has {s_lookupTable.Count} of them.");
            }
#endif

            // The list is from http://www.w3.org/TR/REC-html40/sgml/entities.html, except for &apos;, which
            // is defined in http://www.w3.org/TR/2008/REC-xml-20081126/#sec-predefined-ent.

            private const int Count = 253;

            // maps entity strings => unicode chars
            private static readonly LowLevelDictionary<string, char> s_lookupTable =
                new LowLevelDictionary<string, char>(Count, StringComparer.Ordinal)
                {
                    ["quot"] = '\x0022',
                    ["amp"] = '\x0026',
                    ["apos"] = '\x0027',
                    ["lt"] = '\x003c',
                    ["gt"] = '\x003e',
                    ["nbsp"] = '\x00a0',
                    ["iexcl"] = '\x00a1',
                    ["cent"] = '\x00a2',
                    ["pound"] = '\x00a3',
                    ["curren"] = '\x00a4',
                    ["yen"] = '\x00a5',
                    ["brvbar"] = '\x00a6',
                    ["sect"] = '\x00a7',
                    ["uml"] = '\x00a8',
                    ["copy"] = '\x00a9',
                    ["ordf"] = '\x00aa',
                    ["laquo"] = '\x00ab',
                    ["not"] = '\x00ac',
                    ["shy"] = '\x00ad',
                    ["reg"] = '\x00ae',
                    ["macr"] = '\x00af',
                    ["deg"] = '\x00b0',
                    ["plusmn"] = '\x00b1',
                    ["sup2"] = '\x00b2',
                    ["sup3"] = '\x00b3',
                    ["acute"] = '\x00b4',
                    ["micro"] = '\x00b5',
                    ["para"] = '\x00b6',
                    ["middot"] = '\x00b7',
                    ["cedil"] = '\x00b8',
                    ["sup1"] = '\x00b9',
                    ["ordm"] = '\x00ba',
                    ["raquo"] = '\x00bb',
                    ["frac14"] = '\x00bc',
                    ["frac12"] = '\x00bd',
                    ["frac34"] = '\x00be',
                    ["iquest"] = '\x00bf',
                    ["Agrave"] = '\x00c0',
                    ["Aacute"] = '\x00c1',
                    ["Acirc"] = '\x00c2',
                    ["Atilde"] = '\x00c3',
                    ["Auml"] = '\x00c4',
                    ["Aring"] = '\x00c5',
                    ["AElig"] = '\x00c6',
                    ["Ccedil"] = '\x00c7',
                    ["Egrave"] = '\x00c8',
                    ["Eacute"] = '\x00c9',
                    ["Ecirc"] = '\x00ca',
                    ["Euml"] = '\x00cb',
                    ["Igrave"] = '\x00cc',
                    ["Iacute"] = '\x00cd',
                    ["Icirc"] = '\x00ce',
                    ["Iuml"] = '\x00cf',
                    ["ETH"] = '\x00d0',
                    ["Ntilde"] = '\x00d1',
                    ["Ograve"] = '\x00d2',
                    ["Oacute"] = '\x00d3',
                    ["Ocirc"] = '\x00d4',
                    ["Otilde"] = '\x00d5',
                    ["Ouml"] = '\x00d6',
                    ["times"] = '\x00d7',
                    ["Oslash"] = '\x00d8',
                    ["Ugrave"] = '\x00d9',
                    ["Uacute"] = '\x00da',
                    ["Ucirc"] = '\x00db',
                    ["Uuml"] = '\x00dc',
                    ["Yacute"] = '\x00dd',
                    ["THORN"] = '\x00de',
                    ["szlig"] = '\x00df',
                    ["agrave"] = '\x00e0',
                    ["aacute"] = '\x00e1',
                    ["acirc"] = '\x00e2',
                    ["atilde"] = '\x00e3',
                    ["auml"] = '\x00e4',
                    ["aring"] = '\x00e5',
                    ["aelig"] = '\x00e6',
                    ["ccedil"] = '\x00e7',
                    ["egrave"] = '\x00e8',
                    ["eacute"] = '\x00e9',
                    ["ecirc"] = '\x00ea',
                    ["euml"] = '\x00eb',
                    ["igrave"] = '\x00ec',
                    ["iacute"] = '\x00ed',
                    ["icirc"] = '\x00ee',
                    ["iuml"] = '\x00ef',
                    ["eth"] = '\x00f0',
                    ["ntilde"] = '\x00f1',
                    ["ograve"] = '\x00f2',
                    ["oacute"] = '\x00f3',
                    ["ocirc"] = '\x00f4',
                    ["otilde"] = '\x00f5',
                    ["ouml"] = '\x00f6',
                    ["divide"] = '\x00f7',
                    ["oslash"] = '\x00f8',
                    ["ugrave"] = '\x00f9',
                    ["uacute"] = '\x00fa',
                    ["ucirc"] = '\x00fb',
                    ["uuml"] = '\x00fc',
                    ["yacute"] = '\x00fd',
                    ["thorn"] = '\x00fe',
                    ["yuml"] = '\x00ff',
                    ["OElig"] = '\x0152',
                    ["oelig"] = '\x0153',
                    ["Scaron"] = '\x0160',
                    ["scaron"] = '\x0161',
                    ["Yuml"] = '\x0178',
                    ["fnof"] = '\x0192',
                    ["circ"] = '\x02c6',
                    ["tilde"] = '\x02dc',
                    ["Alpha"] = '\x0391',
                    ["Beta"] = '\x0392',
                    ["Gamma"] = '\x0393',
                    ["Delta"] = '\x0394',
                    ["Epsilon"] = '\x0395',
                    ["Zeta"] = '\x0396',
                    ["Eta"] = '\x0397',
                    ["Theta"] = '\x0398',
                    ["Iota"] = '\x0399',
                    ["Kappa"] = '\x039a',
                    ["Lambda"] = '\x039b',
                    ["Mu"] = '\x039c',
                    ["Nu"] = '\x039d',
                    ["Xi"] = '\x039e',
                    ["Omicron"] = '\x039f',
                    ["Pi"] = '\x03a0',
                    ["Rho"] = '\x03a1',
                    ["Sigma"] = '\x03a3',
                    ["Tau"] = '\x03a4',
                    ["Upsilon"] = '\x03a5',
                    ["Phi"] = '\x03a6',
                    ["Chi"] = '\x03a7',
                    ["Psi"] = '\x03a8',
                    ["Omega"] = '\x03a9',
                    ["alpha"] = '\x03b1',
                    ["beta"] = '\x03b2',
                    ["gamma"] = '\x03b3',
                    ["delta"] = '\x03b4',
                    ["epsilon"] = '\x03b5',
                    ["zeta"] = '\x03b6',
                    ["eta"] = '\x03b7',
                    ["theta"] = '\x03b8',
                    ["iota"] = '\x03b9',
                    ["kappa"] = '\x03ba',
                    ["lambda"] = '\x03bb',
                    ["mu"] = '\x03bc',
                    ["nu"] = '\x03bd',
                    ["xi"] = '\x03be',
                    ["omicron"] = '\x03bf',
                    ["pi"] = '\x03c0',
                    ["rho"] = '\x03c1',
                    ["sigmaf"] = '\x03c2',
                    ["sigma"] = '\x03c3',
                    ["tau"] = '\x03c4',
                    ["upsilon"] = '\x03c5',
                    ["phi"] = '\x03c6',
                    ["chi"] = '\x03c7',
                    ["psi"] = '\x03c8',
                    ["omega"] = '\x03c9',
                    ["thetasym"] = '\x03d1',
                    ["upsih"] = '\x03d2',
                    ["piv"] = '\x03d6',
                    ["ensp"] = '\x2002',
                    ["emsp"] = '\x2003',
                    ["thinsp"] = '\x2009',
                    ["zwnj"] = '\x200c',
                    ["zwj"] = '\x200d',
                    ["lrm"] = '\x200e',
                    ["rlm"] = '\x200f',
                    ["ndash"] = '\x2013',
                    ["mdash"] = '\x2014',
                    ["lsquo"] = '\x2018',
                    ["rsquo"] = '\x2019',
                    ["sbquo"] = '\x201a',
                    ["ldquo"] = '\x201c',
                    ["rdquo"] = '\x201d',
                    ["bdquo"] = '\x201e',
                    ["dagger"] = '\x2020',
                    ["Dagger"] = '\x2021',
                    ["bull"] = '\x2022',
                    ["hellip"] = '\x2026',
                    ["permil"] = '\x2030',
                    ["prime"] = '\x2032',
                    ["Prime"] = '\x2033',
                    ["lsaquo"] = '\x2039',
                    ["rsaquo"] = '\x203a',
                    ["oline"] = '\x203e',
                    ["frasl"] = '\x2044',
                    ["euro"] = '\x20ac',
                    ["image"] = '\x2111',
                    ["weierp"] = '\x2118',
                    ["real"] = '\x211c',
                    ["trade"] = '\x2122',
                    ["alefsym"] = '\x2135',
                    ["larr"] = '\x2190',
                    ["uarr"] = '\x2191',
                    ["rarr"] = '\x2192',
                    ["darr"] = '\x2193',
                    ["harr"] = '\x2194',
                    ["crarr"] = '\x21b5',
                    ["lArr"] = '\x21d0',
                    ["uArr"] = '\x21d1',
                    ["rArr"] = '\x21d2',
                    ["dArr"] = '\x21d3',
                    ["hArr"] = '\x21d4',
                    ["forall"] = '\x2200',
                    ["part"] = '\x2202',
                    ["exist"] = '\x2203',
                    ["empty"] = '\x2205',
                    ["nabla"] = '\x2207',
                    ["isin"] = '\x2208',
                    ["notin"] = '\x2209',
                    ["ni"] = '\x220b',
                    ["prod"] = '\x220f',
                    ["sum"] = '\x2211',
                    ["minus"] = '\x2212',
                    ["lowast"] = '\x2217',
                    ["radic"] = '\x221a',
                    ["prop"] = '\x221d',
                    ["infin"] = '\x221e',
                    ["ang"] = '\x2220',
                    ["and"] = '\x2227',
                    ["or"] = '\x2228',
                    ["cap"] = '\x2229',
                    ["cup"] = '\x222a',
                    ["int"] = '\x222b',
                    ["there4"] = '\x2234',
                    ["sim"] = '\x223c',
                    ["cong"] = '\x2245',
                    ["asymp"] = '\x2248',
                    ["ne"] = '\x2260',
                    ["equiv"] = '\x2261',
                    ["le"] = '\x2264',
                    ["ge"] = '\x2265',
                    ["sub"] = '\x2282',
                    ["sup"] = '\x2283',
                    ["nsub"] = '\x2284',
                    ["sube"] = '\x2286',
                    ["supe"] = '\x2287',
                    ["oplus"] = '\x2295',
                    ["otimes"] = '\x2297',
                    ["perp"] = '\x22a5',
                    ["sdot"] = '\x22c5',
                    ["lceil"] = '\x2308',
                    ["rceil"] = '\x2309',
                    ["lfloor"] = '\x230a',
                    ["rfloor"] = '\x230b',
                    ["lang"] = '\x2329',
                    ["rang"] = '\x232a',
                    ["loz"] = '\x25ca',
                    ["spades"] = '\x2660',
                    ["clubs"] = '\x2663',
                    ["hearts"] = '\x2665',
                    ["diams"] = '\x2666',
                };

            public static char Lookup(string entity)
            {
                char theChar;
                s_lookupTable.TryGetValue(entity, out theChar);
                return theChar;
            }
        }
    }
}
