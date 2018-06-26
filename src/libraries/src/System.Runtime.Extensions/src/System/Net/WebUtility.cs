// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Don't entity encode high chars (160 to 256)
#define ENTITY_ENCODE_HIGH_ASCII_CHARS

using System;
using System.Buffers;
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
        private const int MaxInt32Digits = 10;

        #region HtmlEncode / HtmlDecode methods

        public static string HtmlEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            ReadOnlySpan<char> valueSpan = value.AsSpan();

            // Don't create ValueStringBuilder if we don't have anything to encode
            int index = IndexOfHtmlEncodingChars(valueSpan);
            if (index == -1)
            {
                return value;
            }

            // For small inputs we allocate on the stack. In most cases a buffer three 
            // times larger the original string should be sufficient as usually not all 
            // characters need to be encoded.
            // For larger string we rent the input string's length plus a fixed 
            // conservative amount of chars from the ArrayPool.
            Span<char> buffer = value.Length < 80 ?
                stackalloc char[256] :
                null;
            ValueStringBuilder sb = buffer != null ?
                new ValueStringBuilder(buffer) :
                new ValueStringBuilder(value.Length + 200);

            sb.Append(valueSpan.Slice(0, index));
            HtmlEncode(valueSpan.Slice(index), ref sb);

            return sb.ToString();
        }

        public static void HtmlEncode(string value, TextWriter output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }
            if (string.IsNullOrEmpty(value))
            {
                output.Write(value);
                return;
            }

            ReadOnlySpan<char> valueSpan = value.AsSpan();

            // Don't create ValueStringBuilder if we don't have anything to encode
            int index = IndexOfHtmlEncodingChars(valueSpan);
            if (index == -1)
            {
                output.Write(value);
                return;
            }

            // For small inputs we allocate on the stack. In most cases a buffer three 
            // times larger the original string should be sufficient as usually not all 
            // characters need to be encoded.
            // For larger string we rent the input string's length plus a fixed 
            // conservative amount of chars from the ArrayPool.
            Span<char> buffer = value.Length < 80 ?
                stackalloc char[256] :
                null;
            ValueStringBuilder sb = buffer != null ?
                new ValueStringBuilder(buffer) :
                new ValueStringBuilder(value.Length + 200);

            sb.Append(valueSpan.Slice(0, index));
            HtmlEncode(valueSpan.Slice(index), ref sb);

            output.Write(sb.AsSpan());
            sb.Dispose();
        }

        private static void HtmlEncode(ReadOnlySpan<char> input, ref ValueStringBuilder output)
        {
            for (int i = 0; i < input.Length; i++)
            {
                char ch = input[i];
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
                        if (char.IsSurrogate(ch))
                    {
                        int scalarValue = GetNextUnicodeScalarValueFromUtf16Surrogate(input, ref i);
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

                        // Use the buffer directly and reserve a conservative estimate of 10 chars.
                        Span<char> encodingBuffer = output.AppendSpan(MaxInt32Digits);
                        valueToEncode.TryFormat(encodingBuffer, out int charsWritten); // Invariant
                        output.Length -= (MaxInt32Digits - charsWritten);

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

        public static string HtmlDecode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            ReadOnlySpan<char> valueSpan = value.AsSpan();

            int index = IndexOfHtmlDecodingChars(valueSpan);
            if (index == -1)
            {
                return value;
            }

            // In the worst case the decoded string has the same length.
            // For small inputs we use stack allocation.
            Span<char> buffer = value.Length <= 256 ?
                stackalloc char[256] :
                null;
            ValueStringBuilder sb = buffer != null ?
                new ValueStringBuilder(buffer) :
                new ValueStringBuilder(value.Length);

            sb.Append(valueSpan.Slice(0, index));
            HtmlDecode(valueSpan.Slice(index), ref sb);

            return sb.ToString();
        }

        public static void HtmlDecode(string value, TextWriter output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (string.IsNullOrEmpty(value))
            {
                output.Write(value);
                return;
            }

            ReadOnlySpan<char> valueSpan = value.AsSpan();

            int index = IndexOfHtmlDecodingChars(valueSpan);
            if (index == -1)
            {
                output.Write(value);
                return;
            }

            // In the worst case the decoded string has the same length.
            // For small inputs we use stack allocation.
            Span<char> buffer = value.Length <= 256 ?
                stackalloc char[256] :
                null;
            ValueStringBuilder sb = buffer != null ?
                new ValueStringBuilder(buffer) :
                new ValueStringBuilder(value.Length);

            sb.Append(valueSpan.Slice(0, index));
            HtmlDecode(valueSpan.Slice(index), ref sb);

            output.Write(sb.AsSpan());
            sb.Dispose();
        }

        private static void HtmlDecode(ReadOnlySpan<char> input, ref ValueStringBuilder output)
        {
            for (int i = 0; i < input.Length; i++)
            {
                char ch = input[i];

                if (ch == '&')
                {
                    // We found a '&'. Now look for the next ';' or '&'. The idea is that
                    // if we find another '&' before finding a ';', then this is not an entity,
                    // and the next '&' might start a real entity (VSWhidbey 275184)
                    ReadOnlySpan<char> inputSlice = input.Slice(i + 1);
                    int entityLength = inputSlice.IndexOf(';');
                    if (entityLength >= 0)
                    {
                        int entityEndPosition = (i + 1) + entityLength;
                        if (entityLength > 1 && inputSlice[0] == '#')
                        {
                            // The # syntax can be in decimal or hex, e.g.
                            //      &#229;  --> decimal
                            //      &#xE5;  --> same char in hex
                            // See http://www.w3.org/TR/REC-html40/charset.html#entities

                            bool parsedSuccessfully = inputSlice[1] == 'x' || inputSlice[1] == 'X'
                                ? uint.TryParse(inputSlice.Slice(2, entityLength - 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint parsedValue)
                                : uint.TryParse(inputSlice.Slice(1, entityLength - 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedValue);

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
                                    ConvertSmpToUtf16(parsedValue, out char leadingSurrogate, out char trailingSurrogate);
                                    output.Append(leadingSurrogate);
                                    output.Append(trailingSurrogate);
                                }

                                i = entityEndPosition; // already looked at everything until semicolon
                                continue;
                            }
                        }
                        else
                        {
                            ReadOnlySpan<char> entity = inputSlice.Slice(0, entityLength);
                            i = entityEndPosition; // already looked at everything until semicolon
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

        private static int IndexOfHtmlEncodingChars(ReadOnlySpan<char> input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                char ch = input[i];
                if (ch <= '>')
                {
                    switch (ch)
                    {
                        case '<':
                        case '>':
                        case '"':
                        case '\'':
                        case '&':
                            return i;
                    }
                }
#if ENTITY_ENCODE_HIGH_ASCII_CHARS
                else if (ch >= 160 && ch < 256)
                {
                    return i;
                }
#endif // ENTITY_ENCODE_HIGH_ASCII_CHARS
                else if (char.IsSurrogate(ch))
                {
                    return i;
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

        private static int GetNextUnicodeScalarValueFromUtf16Surrogate(ReadOnlySpan<char> input, ref int index)
        {
            // invariants
            Debug.Assert(input.Length >= 1);
            Debug.Assert(char.IsSurrogate(input[0]));

            if (input.Length <= 1)
            {
                // not enough characters remaining to resurrect the original scalar value
                return UnicodeReplacementChar;
            }

            char leadingSurrogate = input[0];
            char trailingSurrogate = input[1];

            if (!char.IsSurrogatePair(leadingSurrogate, trailingSurrogate))
            {
                // unmatched surrogate
                return UnicodeReplacementChar;
            }

            // we're going to consume an extra char
            index++;

            // below code is from Char.ConvertToUtf32, but without the checks (since we just performed them)
            return (((leadingSurrogate - HIGH_SURROGATE_START) * 0x400) + (trailingSurrogate - LOW_SURROGATE_START) + UNICODE_PLANE01_START);
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

        private static int IndexOfHtmlDecodingChars(ReadOnlySpan<char> input)
        {
            // this string requires html decoding if it contains '&' or a surrogate character
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c == '&' || char.IsSurrogate(c))
                {
                    return i;
                }
            }

            return -1;
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

            internal string GetString()
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
            private static readonly Dictionary<ulong, char> s_lookupTable =
                new Dictionary<ulong, char>(Count)
                {
                    [ToUInt64Key("quot")] = '\x0022',
                    [ToUInt64Key("amp")] = '\x0026',
                    [ToUInt64Key("apos")] = '\x0027',
                    [ToUInt64Key("lt")] = '\x003c',
                    [ToUInt64Key("gt")] = '\x003e',
                    [ToUInt64Key("nbsp")] = '\x00a0',
                    [ToUInt64Key("iexcl")] = '\x00a1',
                    [ToUInt64Key("cent")] = '\x00a2',
                    [ToUInt64Key("pound")] = '\x00a3',
                    [ToUInt64Key("curren")] = '\x00a4',
                    [ToUInt64Key("yen")] = '\x00a5',
                    [ToUInt64Key("brvbar")] = '\x00a6',
                    [ToUInt64Key("sect")] = '\x00a7',
                    [ToUInt64Key("uml")] = '\x00a8',
                    [ToUInt64Key("copy")] = '\x00a9',
                    [ToUInt64Key("ordf")] = '\x00aa',
                    [ToUInt64Key("laquo")] = '\x00ab',
                    [ToUInt64Key("not")] = '\x00ac',
                    [ToUInt64Key("shy")] = '\x00ad',
                    [ToUInt64Key("reg")] = '\x00ae',
                    [ToUInt64Key("macr")] = '\x00af',
                    [ToUInt64Key("deg")] = '\x00b0',
                    [ToUInt64Key("plusmn")] = '\x00b1',
                    [ToUInt64Key("sup2")] = '\x00b2',
                    [ToUInt64Key("sup3")] = '\x00b3',
                    [ToUInt64Key("acute")] = '\x00b4',
                    [ToUInt64Key("micro")] = '\x00b5',
                    [ToUInt64Key("para")] = '\x00b6',
                    [ToUInt64Key("middot")] = '\x00b7',
                    [ToUInt64Key("cedil")] = '\x00b8',
                    [ToUInt64Key("sup1")] = '\x00b9',
                    [ToUInt64Key("ordm")] = '\x00ba',
                    [ToUInt64Key("raquo")] = '\x00bb',
                    [ToUInt64Key("frac14")] = '\x00bc',
                    [ToUInt64Key("frac12")] = '\x00bd',
                    [ToUInt64Key("frac34")] = '\x00be',
                    [ToUInt64Key("iquest")] = '\x00bf',
                    [ToUInt64Key("Agrave")] = '\x00c0',
                    [ToUInt64Key("Aacute")] = '\x00c1',
                    [ToUInt64Key("Acirc")] = '\x00c2',
                    [ToUInt64Key("Atilde")] = '\x00c3',
                    [ToUInt64Key("Auml")] = '\x00c4',
                    [ToUInt64Key("Aring")] = '\x00c5',
                    [ToUInt64Key("AElig")] = '\x00c6',
                    [ToUInt64Key("Ccedil")] = '\x00c7',
                    [ToUInt64Key("Egrave")] = '\x00c8',
                    [ToUInt64Key("Eacute")] = '\x00c9',
                    [ToUInt64Key("Ecirc")] = '\x00ca',
                    [ToUInt64Key("Euml")] = '\x00cb',
                    [ToUInt64Key("Igrave")] = '\x00cc',
                    [ToUInt64Key("Iacute")] = '\x00cd',
                    [ToUInt64Key("Icirc")] = '\x00ce',
                    [ToUInt64Key("Iuml")] = '\x00cf',
                    [ToUInt64Key("ETH")] = '\x00d0',
                    [ToUInt64Key("Ntilde")] = '\x00d1',
                    [ToUInt64Key("Ograve")] = '\x00d2',
                    [ToUInt64Key("Oacute")] = '\x00d3',
                    [ToUInt64Key("Ocirc")] = '\x00d4',
                    [ToUInt64Key("Otilde")] = '\x00d5',
                    [ToUInt64Key("Ouml")] = '\x00d6',
                    [ToUInt64Key("times")] = '\x00d7',
                    [ToUInt64Key("Oslash")] = '\x00d8',
                    [ToUInt64Key("Ugrave")] = '\x00d9',
                    [ToUInt64Key("Uacute")] = '\x00da',
                    [ToUInt64Key("Ucirc")] = '\x00db',
                    [ToUInt64Key("Uuml")] = '\x00dc',
                    [ToUInt64Key("Yacute")] = '\x00dd',
                    [ToUInt64Key("THORN")] = '\x00de',
                    [ToUInt64Key("szlig")] = '\x00df',
                    [ToUInt64Key("agrave")] = '\x00e0',
                    [ToUInt64Key("aacute")] = '\x00e1',
                    [ToUInt64Key("acirc")] = '\x00e2',
                    [ToUInt64Key("atilde")] = '\x00e3',
                    [ToUInt64Key("auml")] = '\x00e4',
                    [ToUInt64Key("aring")] = '\x00e5',
                    [ToUInt64Key("aelig")] = '\x00e6',
                    [ToUInt64Key("ccedil")] = '\x00e7',
                    [ToUInt64Key("egrave")] = '\x00e8',
                    [ToUInt64Key("eacute")] = '\x00e9',
                    [ToUInt64Key("ecirc")] = '\x00ea',
                    [ToUInt64Key("euml")] = '\x00eb',
                    [ToUInt64Key("igrave")] = '\x00ec',
                    [ToUInt64Key("iacute")] = '\x00ed',
                    [ToUInt64Key("icirc")] = '\x00ee',
                    [ToUInt64Key("iuml")] = '\x00ef',
                    [ToUInt64Key("eth")] = '\x00f0',
                    [ToUInt64Key("ntilde")] = '\x00f1',
                    [ToUInt64Key("ograve")] = '\x00f2',
                    [ToUInt64Key("oacute")] = '\x00f3',
                    [ToUInt64Key("ocirc")] = '\x00f4',
                    [ToUInt64Key("otilde")] = '\x00f5',
                    [ToUInt64Key("ouml")] = '\x00f6',
                    [ToUInt64Key("divide")] = '\x00f7',
                    [ToUInt64Key("oslash")] = '\x00f8',
                    [ToUInt64Key("ugrave")] = '\x00f9',
                    [ToUInt64Key("uacute")] = '\x00fa',
                    [ToUInt64Key("ucirc")] = '\x00fb',
                    [ToUInt64Key("uuml")] = '\x00fc',
                    [ToUInt64Key("yacute")] = '\x00fd',
                    [ToUInt64Key("thorn")] = '\x00fe',
                    [ToUInt64Key("yuml")] = '\x00ff',
                    [ToUInt64Key("OElig")] = '\x0152',
                    [ToUInt64Key("oelig")] = '\x0153',
                    [ToUInt64Key("Scaron")] = '\x0160',
                    [ToUInt64Key("scaron")] = '\x0161',
                    [ToUInt64Key("Yuml")] = '\x0178',
                    [ToUInt64Key("fnof")] = '\x0192',
                    [ToUInt64Key("circ")] = '\x02c6',
                    [ToUInt64Key("tilde")] = '\x02dc',
                    [ToUInt64Key("Alpha")] = '\x0391',
                    [ToUInt64Key("Beta")] = '\x0392',
                    [ToUInt64Key("Gamma")] = '\x0393',
                    [ToUInt64Key("Delta")] = '\x0394',
                    [ToUInt64Key("Epsilon")] = '\x0395',
                    [ToUInt64Key("Zeta")] = '\x0396',
                    [ToUInt64Key("Eta")] = '\x0397',
                    [ToUInt64Key("Theta")] = '\x0398',
                    [ToUInt64Key("Iota")] = '\x0399',
                    [ToUInt64Key("Kappa")] = '\x039a',
                    [ToUInt64Key("Lambda")] = '\x039b',
                    [ToUInt64Key("Mu")] = '\x039c',
                    [ToUInt64Key("Nu")] = '\x039d',
                    [ToUInt64Key("Xi")] = '\x039e',
                    [ToUInt64Key("Omicron")] = '\x039f',
                    [ToUInt64Key("Pi")] = '\x03a0',
                    [ToUInt64Key("Rho")] = '\x03a1',
                    [ToUInt64Key("Sigma")] = '\x03a3',
                    [ToUInt64Key("Tau")] = '\x03a4',
                    [ToUInt64Key("Upsilon")] = '\x03a5',
                    [ToUInt64Key("Phi")] = '\x03a6',
                    [ToUInt64Key("Chi")] = '\x03a7',
                    [ToUInt64Key("Psi")] = '\x03a8',
                    [ToUInt64Key("Omega")] = '\x03a9',
                    [ToUInt64Key("alpha")] = '\x03b1',
                    [ToUInt64Key("beta")] = '\x03b2',
                    [ToUInt64Key("gamma")] = '\x03b3',
                    [ToUInt64Key("delta")] = '\x03b4',
                    [ToUInt64Key("epsilon")] = '\x03b5',
                    [ToUInt64Key("zeta")] = '\x03b6',
                    [ToUInt64Key("eta")] = '\x03b7',
                    [ToUInt64Key("theta")] = '\x03b8',
                    [ToUInt64Key("iota")] = '\x03b9',
                    [ToUInt64Key("kappa")] = '\x03ba',
                    [ToUInt64Key("lambda")] = '\x03bb',
                    [ToUInt64Key("mu")] = '\x03bc',
                    [ToUInt64Key("nu")] = '\x03bd',
                    [ToUInt64Key("xi")] = '\x03be',
                    [ToUInt64Key("omicron")] = '\x03bf',
                    [ToUInt64Key("pi")] = '\x03c0',
                    [ToUInt64Key("rho")] = '\x03c1',
                    [ToUInt64Key("sigmaf")] = '\x03c2',
                    [ToUInt64Key("sigma")] = '\x03c3',
                    [ToUInt64Key("tau")] = '\x03c4',
                    [ToUInt64Key("upsilon")] = '\x03c5',
                    [ToUInt64Key("phi")] = '\x03c6',
                    [ToUInt64Key("chi")] = '\x03c7',
                    [ToUInt64Key("psi")] = '\x03c8',
                    [ToUInt64Key("omega")] = '\x03c9',
                    [ToUInt64Key("thetasym")] = '\x03d1',
                    [ToUInt64Key("upsih")] = '\x03d2',
                    [ToUInt64Key("piv")] = '\x03d6',
                    [ToUInt64Key("ensp")] = '\x2002',
                    [ToUInt64Key("emsp")] = '\x2003',
                    [ToUInt64Key("thinsp")] = '\x2009',
                    [ToUInt64Key("zwnj")] = '\x200c',
                    [ToUInt64Key("zwj")] = '\x200d',
                    [ToUInt64Key("lrm")] = '\x200e',
                    [ToUInt64Key("rlm")] = '\x200f',
                    [ToUInt64Key("ndash")] = '\x2013',
                    [ToUInt64Key("mdash")] = '\x2014',
                    [ToUInt64Key("lsquo")] = '\x2018',
                    [ToUInt64Key("rsquo")] = '\x2019',
                    [ToUInt64Key("sbquo")] = '\x201a',
                    [ToUInt64Key("ldquo")] = '\x201c',
                    [ToUInt64Key("rdquo")] = '\x201d',
                    [ToUInt64Key("bdquo")] = '\x201e',
                    [ToUInt64Key("dagger")] = '\x2020',
                    [ToUInt64Key("Dagger")] = '\x2021',
                    [ToUInt64Key("bull")] = '\x2022',
                    [ToUInt64Key("hellip")] = '\x2026',
                    [ToUInt64Key("permil")] = '\x2030',
                    [ToUInt64Key("prime")] = '\x2032',
                    [ToUInt64Key("Prime")] = '\x2033',
                    [ToUInt64Key("lsaquo")] = '\x2039',
                    [ToUInt64Key("rsaquo")] = '\x203a',
                    [ToUInt64Key("oline")] = '\x203e',
                    [ToUInt64Key("frasl")] = '\x2044',
                    [ToUInt64Key("euro")] = '\x20ac',
                    [ToUInt64Key("image")] = '\x2111',
                    [ToUInt64Key("weierp")] = '\x2118',
                    [ToUInt64Key("real")] = '\x211c',
                    [ToUInt64Key("trade")] = '\x2122',
                    [ToUInt64Key("alefsym")] = '\x2135',
                    [ToUInt64Key("larr")] = '\x2190',
                    [ToUInt64Key("uarr")] = '\x2191',
                    [ToUInt64Key("rarr")] = '\x2192',
                    [ToUInt64Key("darr")] = '\x2193',
                    [ToUInt64Key("harr")] = '\x2194',
                    [ToUInt64Key("crarr")] = '\x21b5',
                    [ToUInt64Key("lArr")] = '\x21d0',
                    [ToUInt64Key("uArr")] = '\x21d1',
                    [ToUInt64Key("rArr")] = '\x21d2',
                    [ToUInt64Key("dArr")] = '\x21d3',
                    [ToUInt64Key("hArr")] = '\x21d4',
                    [ToUInt64Key("forall")] = '\x2200',
                    [ToUInt64Key("part")] = '\x2202',
                    [ToUInt64Key("exist")] = '\x2203',
                    [ToUInt64Key("empty")] = '\x2205',
                    [ToUInt64Key("nabla")] = '\x2207',
                    [ToUInt64Key("isin")] = '\x2208',
                    [ToUInt64Key("notin")] = '\x2209',
                    [ToUInt64Key("ni")] = '\x220b',
                    [ToUInt64Key("prod")] = '\x220f',
                    [ToUInt64Key("sum")] = '\x2211',
                    [ToUInt64Key("minus")] = '\x2212',
                    [ToUInt64Key("lowast")] = '\x2217',
                    [ToUInt64Key("radic")] = '\x221a',
                    [ToUInt64Key("prop")] = '\x221d',
                    [ToUInt64Key("infin")] = '\x221e',
                    [ToUInt64Key("ang")] = '\x2220',
                    [ToUInt64Key("and")] = '\x2227',
                    [ToUInt64Key("or")] = '\x2228',
                    [ToUInt64Key("cap")] = '\x2229',
                    [ToUInt64Key("cup")] = '\x222a',
                    [ToUInt64Key("int")] = '\x222b',
                    [ToUInt64Key("there4")] = '\x2234',
                    [ToUInt64Key("sim")] = '\x223c',
                    [ToUInt64Key("cong")] = '\x2245',
                    [ToUInt64Key("asymp")] = '\x2248',
                    [ToUInt64Key("ne")] = '\x2260',
                    [ToUInt64Key("equiv")] = '\x2261',
                    [ToUInt64Key("le")] = '\x2264',
                    [ToUInt64Key("ge")] = '\x2265',
                    [ToUInt64Key("sub")] = '\x2282',
                    [ToUInt64Key("sup")] = '\x2283',
                    [ToUInt64Key("nsub")] = '\x2284',
                    [ToUInt64Key("sube")] = '\x2286',
                    [ToUInt64Key("supe")] = '\x2287',
                    [ToUInt64Key("oplus")] = '\x2295',
                    [ToUInt64Key("otimes")] = '\x2297',
                    [ToUInt64Key("perp")] = '\x22a5',
                    [ToUInt64Key("sdot")] = '\x22c5',
                    [ToUInt64Key("lceil")] = '\x2308',
                    [ToUInt64Key("rceil")] = '\x2309',
                    [ToUInt64Key("lfloor")] = '\x230a',
                    [ToUInt64Key("rfloor")] = '\x230b',
                    [ToUInt64Key("lang")] = '\x2329',
                    [ToUInt64Key("rang")] = '\x232a',
                    [ToUInt64Key("loz")] = '\x25ca',
                    [ToUInt64Key("spades")] = '\x2660',
                    [ToUInt64Key("clubs")] = '\x2663',
                    [ToUInt64Key("hearts")] = '\x2665',
                    [ToUInt64Key("diams")] = '\x2666',
                };

            public static char Lookup(ReadOnlySpan<char> entity)
            {
                // To avoid an allocation, keys of type "ulong" are used in the lookup table.
                // Since all entity strings comprise 8 characters or less and are ASCII-only, they "fit" into an ulong (8 bytes).
                if (entity.Length <= 8)
                {
                    s_lookupTable.TryGetValue(ToUInt64Key(entity), out char result);
                    return result;
                }
                else
                {
                    // Currently, there are no entities that are longer than 8 characters.
                    return (char)0;
                }
            }

            private static ulong ToUInt64Key(ReadOnlySpan<char> entity)
            {
                // The ulong key is the reversed single-byte character representation of the actual entity string.
                Debug.Assert(entity.Length <= 8);

                ulong key = 0;
                for (int i = 0; i < entity.Length; i++)
                {
                    if (entity[i] > 0xFF)
                    {
                        return 0;
                    }

                    key = (key << 8) | entity[i];
                }

                return key;
            }
        }
    }
}
