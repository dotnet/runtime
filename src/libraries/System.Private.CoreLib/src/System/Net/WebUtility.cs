// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Don't entity encode high chars (160 to 256)
#define ENTITY_ENCODE_HIGH_ASCII_CHARS

using System.Buffers.Binary;
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

        [return: NotNullIfNotNull("value")]
        public static string? HtmlEncode(string? value)
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
            ValueStringBuilder sb = value.Length < 80 ?
                new ValueStringBuilder(stackalloc char[256]) :
                new ValueStringBuilder(value.Length + 200);

            sb.Append(valueSpan.Slice(0, index));
            HtmlEncode(valueSpan.Slice(index), ref sb);

            return sb.ToString();
        }

        public static void HtmlEncode(string? value, TextWriter output)
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
            ValueStringBuilder sb = value.Length < 80 ?
                new ValueStringBuilder(stackalloc char[256]) :
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

        [return: NotNullIfNotNull("value")]
        public static string? HtmlDecode(string? value)
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
            ValueStringBuilder sb = value.Length <= 256 ?
                new ValueStringBuilder(stackalloc char[256]) :
                new ValueStringBuilder(value.Length);

            sb.Append(valueSpan.Slice(0, index));
            HtmlDecode(valueSpan.Slice(index), ref sb);

            return sb.ToString();
        }

        public static void HtmlDecode(string? value, TextWriter output)
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
            ValueStringBuilder sb = value.Length <= 256 ?
                new ValueStringBuilder(stackalloc char[256]) :
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
                    int entityLength = inputSlice.IndexOfAny(';', '&');
                    if (entityLength >= 0 && inputSlice[entityLength] == ';')
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
                    expandedBytes[pos++] = (byte)HexConverter.ToCharUpper(b >> 4);
                    expandedBytes[pos++] = (byte)HexConverter.ToCharUpper(b);
                }
            }
        }

        #endregion

        #region UrlEncode public methods

        [return: NotNullIfNotNull("value")]
        public static string? UrlEncode(string? value)
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

        [return: NotNullIfNotNull("value")]
        public static byte[]? UrlEncodeToBytes(byte[]? value, int offset, int count)
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
                char ch = (char)value![offset + i];

                if (ch == ' ')
                    foundSpaces = true;
                else if (!IsUrlSafeChar(ch))
                    unsafeCount++;
            }

            // nothing to expand?
            if (!foundSpaces && unsafeCount == 0)
            {
                var subarray = new byte[count];
                Buffer.BlockCopy(value!, offset, subarray, 0, count);
                return subarray;
            }

            // expand not 'safe' characters into %XX, spaces to +s
            byte[] expandedBytes = new byte[count + unsafeCount * 2];
            GetEncodedBytes(value!, offset, count, expandedBytes);
            return expandedBytes;
        }

        #endregion

        #region UrlDecode implementation

        [return: NotNullIfNotNull("value")]
        private static string? UrlDecodeInternal(string? value, Encoding encoding)
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
                    int h1 = HexConverter.FromChar(value[pos + 1]);
                    int h2 = HexConverter.FromChar(value[pos + 2]);

                    if ((h1 | h2) != 0xFF)
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

        [return: NotNullIfNotNull("bytes")]
        private static byte[]? UrlDecodeInternal(byte[]? bytes, int offset, int count)
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
                byte b = bytes![pos];

                if (b == '+')
                {
                    b = (byte)' ';
                }
                else if (b == '%' && i < count - 2)
                {
                    int h1 = HexConverter.FromChar(bytes[pos + 1]);
                    int h2 = HexConverter.FromChar(bytes[pos + 2]);

                    if ((h1 | h2) != 0xFF)
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


        [return: NotNullIfNotNull("encodedValue")]
        public static string? UrlDecode(string? encodedValue)
        {
            return UrlDecodeInternal(encodedValue, Encoding.UTF8);
        }

        [return: NotNullIfNotNull("encodedValue")]
        public static byte[]? UrlDecodeToBytes(byte[]? encodedValue, int offset, int count)
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
            Debug.Assert(input.Length - index >= 1);
            Debug.Assert(char.IsSurrogate(input[index]));

            if (input.Length - index <= 1)
            {
                // not enough characters remaining to resurrect the original scalar value
                return UnicodeReplacementChar;
            }

            char leadingSurrogate = input[index];
            char trailingSurrogate = input[index + 1];

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

        private static bool ValidateUrlEncodingParameters(byte[]? bytes, int offset, int count)
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
            private readonly int _bufferSize;

            // Accumulate characters in a special array
            private int _numChars;
            private char[]? _charBuffer;

            // Accumulate bytes for decoding into characters in a special array
            private int _numBytes;
            private byte[]? _byteBuffer;

            // Encoding to convert chars to bytes
            private readonly Encoding _encoding;

            private void FlushBytes()
            {
                Debug.Assert(_numBytes > 0);
                if (_charBuffer == null)
                    _charBuffer = new char[_bufferSize];

                _numChars += _encoding.GetChars(_byteBuffer!, 0, _numBytes, _charBuffer, _numChars);
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
                return new string(_charBuffer!, 0, _numChars);
            }
        }

        // helper class for lookup of HTML encoding entities
        private static class HtmlEntities
        {
#if DEBUG
            static HtmlEntities()
            {
                // Make sure the initial capacity for s_lookupTable is correct
                Debug.Assert(s_lookupTable.Count == 253, $"There should be 253 HTML entities, but {nameof(s_lookupTable)} has {s_lookupTable.Count} of them.");

                // Just a quick check precalculated values in s_lookupTable are correct
                Debug.Assert(s_lookupTable[ToUInt64Key("quot")] == '\x0022');
                Debug.Assert(s_lookupTable[ToUInt64Key("alpha")] == '\x03b1');
                Debug.Assert(s_lookupTable[ToUInt64Key("diams")] == '\x2666');
            }
#endif

            // The list is from http://www.w3.org/TR/REC-html40/sgml/entities.html, except for &apos;, which
            // is defined in http://www.w3.org/TR/2008/REC-xml-20081126/#sec-predefined-ent.
            private static Dictionary<ulong, char> InitializeLookupTable()
            {
                ReadOnlySpan<byte> tableData = new byte[]
                    {
                        0x74, 0x6F, 0x75, 0x71, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("quot")*/    0x22, 0x00, /*'\x0022'*/
                        0x70, 0x6D, 0x61, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("amp")*/     0x26, 0x00, /*'\x0026'*/
                        0x73, 0x6F, 0x70, 0x61, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("apos")*/    0x27, 0x00, /*'\x0027'*/
                        0x74, 0x6C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("lt")*/      0x3C, 0x00, /*'\x003c'*/
                        0x74, 0x67, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("gt")*/      0x3E, 0x00, /*'\x003e'*/
                        0x70, 0x73, 0x62, 0x6E, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("nbsp")*/    0xA0, 0x00, /*'\x00a0'*/
                        0x6C, 0x63, 0x78, 0x65, 0x69, 0x00, 0x00, 0x00, /*ToUInt64Key("iexcl")*/   0xA1, 0x00, /*'\x00a1'*/
                        0x74, 0x6E, 0x65, 0x63, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("cent")*/    0xA2, 0x00, /*'\x00a2'*/
                        0x64, 0x6E, 0x75, 0x6F, 0x70, 0x00, 0x00, 0x00, /*ToUInt64Key("pound")*/   0xA3, 0x00, /*'\x00a3'*/
                        0x6E, 0x65, 0x72, 0x72, 0x75, 0x63, 0x00, 0x00, /*ToUInt64Key("curren")*/  0xA4, 0x00, /*'\x00a4'*/
                        0x6E, 0x65, 0x79, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("yen")*/     0xA5, 0x00, /*'\x00a5'*/
                        0x72, 0x61, 0x62, 0x76, 0x72, 0x62, 0x00, 0x00, /*ToUInt64Key("brvbar")*/  0xA6, 0x00, /*'\x00a6'*/
                        0x74, 0x63, 0x65, 0x73, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("sect")*/    0xA7, 0x00, /*'\x00a7'*/
                        0x6C, 0x6D, 0x75, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("uml")*/     0xA8, 0x00, /*'\x00a8'*/
                        0x79, 0x70, 0x6F, 0x63, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("copy")*/    0xA9, 0x00, /*'\x00a9'*/
                        0x66, 0x64, 0x72, 0x6F, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("ordf")*/    0xAA, 0x00, /*'\x00aa'*/
                        0x6F, 0x75, 0x71, 0x61, 0x6C, 0x00, 0x00, 0x00, /*ToUInt64Key("laquo")*/   0xAB, 0x00, /*'\x00ab'*/
                        0x74, 0x6F, 0x6E, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("not")*/     0xAC, 0x00, /*'\x00ac'*/
                        0x79, 0x68, 0x73, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("shy")*/     0xAD, 0x00, /*'\x00ad'*/
                        0x67, 0x65, 0x72, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("reg")*/     0xAE, 0x00, /*'\x00ae'*/
                        0x72, 0x63, 0x61, 0x6D, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("macr")*/    0xAF, 0x00, /*'\x00af'*/
                        0x67, 0x65, 0x64, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("deg")*/     0xB0, 0x00, /*'\x00b0'*/
                        0x6E, 0x6D, 0x73, 0x75, 0x6C, 0x70, 0x00, 0x00, /*ToUInt64Key("plusmn")*/  0xB1, 0x00, /*'\x00b1'*/
                        0x32, 0x70, 0x75, 0x73, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("sup2")*/    0xB2, 0x00, /*'\x00b2'*/
                        0x33, 0x70, 0x75, 0x73, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("sup3")*/    0xB3, 0x00, /*'\x00b3'*/
                        0x65, 0x74, 0x75, 0x63, 0x61, 0x00, 0x00, 0x00, /*ToUInt64Key("acute")*/   0xB4, 0x00, /*'\x00b4'*/
                        0x6F, 0x72, 0x63, 0x69, 0x6D, 0x00, 0x00, 0x00, /*ToUInt64Key("micro")*/   0xB5, 0x00, /*'\x00b5'*/
                        0x61, 0x72, 0x61, 0x70, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("para")*/    0xB6, 0x00, /*'\x00b6'*/
                        0x74, 0x6F, 0x64, 0x64, 0x69, 0x6D, 0x00, 0x00, /*ToUInt64Key("middot")*/  0xB7, 0x00, /*'\x00b7'*/
                        0x6C, 0x69, 0x64, 0x65, 0x63, 0x00, 0x00, 0x00, /*ToUInt64Key("cedil")*/   0xB8, 0x00, /*'\x00b8'*/
                        0x31, 0x70, 0x75, 0x73, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("sup1")*/    0xB9, 0x00, /*'\x00b9'*/
                        0x6D, 0x64, 0x72, 0x6F, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("ordm")*/    0xBA, 0x00, /*'\x00ba'*/
                        0x6F, 0x75, 0x71, 0x61, 0x72, 0x00, 0x00, 0x00, /*ToUInt64Key("raquo")*/   0xBB, 0x00, /*'\x00bb'*/
                        0x34, 0x31, 0x63, 0x61, 0x72, 0x66, 0x00, 0x00, /*ToUInt64Key("frac14")*/  0xBC, 0x00, /*'\x00bc'*/
                        0x32, 0x31, 0x63, 0x61, 0x72, 0x66, 0x00, 0x00, /*ToUInt64Key("frac12")*/  0xBD, 0x00, /*'\x00bd'*/
                        0x34, 0x33, 0x63, 0x61, 0x72, 0x66, 0x00, 0x00, /*ToUInt64Key("frac34")*/  0xBE, 0x00, /*'\x00be'*/
                        0x74, 0x73, 0x65, 0x75, 0x71, 0x69, 0x00, 0x00, /*ToUInt64Key("iquest")*/  0xBF, 0x00, /*'\x00bf'*/
                        0x65, 0x76, 0x61, 0x72, 0x67, 0x41, 0x00, 0x00, /*ToUInt64Key("Agrave")*/  0xC0, 0x00, /*'\x00c0'*/
                        0x65, 0x74, 0x75, 0x63, 0x61, 0x41, 0x00, 0x00, /*ToUInt64Key("Aacute")*/  0xC1, 0x00, /*'\x00c1'*/
                        0x63, 0x72, 0x69, 0x63, 0x41, 0x00, 0x00, 0x00, /*ToUInt64Key("Acirc")*/   0xC2, 0x00, /*'\x00c2'*/
                        0x65, 0x64, 0x6C, 0x69, 0x74, 0x41, 0x00, 0x00, /*ToUInt64Key("Atilde")*/  0xC3, 0x00, /*'\x00c3'*/
                        0x6C, 0x6D, 0x75, 0x41, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("Auml")*/    0xC4, 0x00, /*'\x00c4'*/
                        0x67, 0x6E, 0x69, 0x72, 0x41, 0x00, 0x00, 0x00, /*ToUInt64Key("Aring")*/   0xC5, 0x00, /*'\x00c5'*/
                        0x67, 0x69, 0x6C, 0x45, 0x41, 0x00, 0x00, 0x00, /*ToUInt64Key("AElig")*/   0xC6, 0x00, /*'\x00c6'*/
                        0x6C, 0x69, 0x64, 0x65, 0x63, 0x43, 0x00, 0x00, /*ToUInt64Key("Ccedil")*/  0xC7, 0x00, /*'\x00c7'*/
                        0x65, 0x76, 0x61, 0x72, 0x67, 0x45, 0x00, 0x00, /*ToUInt64Key("Egrave")*/  0xC8, 0x00, /*'\x00c8'*/
                        0x65, 0x74, 0x75, 0x63, 0x61, 0x45, 0x00, 0x00, /*ToUInt64Key("Eacute")*/  0xC9, 0x00, /*'\x00c9'*/
                        0x63, 0x72, 0x69, 0x63, 0x45, 0x00, 0x00, 0x00, /*ToUInt64Key("Ecirc")*/   0xCA, 0x00, /*'\x00ca'*/
                        0x6C, 0x6D, 0x75, 0x45, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("Euml")*/    0xCB, 0x00, /*'\x00cb'*/
                        0x65, 0x76, 0x61, 0x72, 0x67, 0x49, 0x00, 0x00, /*ToUInt64Key("Igrave")*/  0xCC, 0x00, /*'\x00cc'*/
                        0x65, 0x74, 0x75, 0x63, 0x61, 0x49, 0x00, 0x00, /*ToUInt64Key("Iacute")*/  0xCD, 0x00, /*'\x00cd'*/
                        0x63, 0x72, 0x69, 0x63, 0x49, 0x00, 0x00, 0x00, /*ToUInt64Key("Icirc")*/   0xCE, 0x00, /*'\x00ce'*/
                        0x6C, 0x6D, 0x75, 0x49, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("Iuml")*/    0xCF, 0x00, /*'\x00cf'*/
                        0x48, 0x54, 0x45, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("ETH")*/     0xD0, 0x00, /*'\x00d0'*/
                        0x65, 0x64, 0x6C, 0x69, 0x74, 0x4E, 0x00, 0x00, /*ToUInt64Key("Ntilde")*/  0xD1, 0x00, /*'\x00d1'*/
                        0x65, 0x76, 0x61, 0x72, 0x67, 0x4F, 0x00, 0x00, /*ToUInt64Key("Ograve")*/  0xD2, 0x00, /*'\x00d2'*/
                        0x65, 0x74, 0x75, 0x63, 0x61, 0x4F, 0x00, 0x00, /*ToUInt64Key("Oacute")*/  0xD3, 0x00, /*'\x00d3'*/
                        0x63, 0x72, 0x69, 0x63, 0x4F, 0x00, 0x00, 0x00, /*ToUInt64Key("Ocirc")*/   0xD4, 0x00, /*'\x00d4'*/
                        0x65, 0x64, 0x6C, 0x69, 0x74, 0x4F, 0x00, 0x00, /*ToUInt64Key("Otilde")*/  0xD5, 0x00, /*'\x00d5'*/
                        0x6C, 0x6D, 0x75, 0x4F, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("Ouml")*/    0xD6, 0x00, /*'\x00d6'*/
                        0x73, 0x65, 0x6D, 0x69, 0x74, 0x00, 0x00, 0x00, /*ToUInt64Key("times")*/   0xD7, 0x00, /*'\x00d7'*/
                        0x68, 0x73, 0x61, 0x6C, 0x73, 0x4F, 0x00, 0x00, /*ToUInt64Key("Oslash")*/  0xD8, 0x00, /*'\x00d8'*/
                        0x65, 0x76, 0x61, 0x72, 0x67, 0x55, 0x00, 0x00, /*ToUInt64Key("Ugrave")*/  0xD9, 0x00, /*'\x00d9'*/
                        0x65, 0x74, 0x75, 0x63, 0x61, 0x55, 0x00, 0x00, /*ToUInt64Key("Uacute")*/  0xDA, 0x00, /*'\x00da'*/
                        0x63, 0x72, 0x69, 0x63, 0x55, 0x00, 0x00, 0x00, /*ToUInt64Key("Ucirc")*/   0xDB, 0x00, /*'\x00db'*/
                        0x6C, 0x6D, 0x75, 0x55, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("Uuml")*/    0xDC, 0x00, /*'\x00dc'*/
                        0x65, 0x74, 0x75, 0x63, 0x61, 0x59, 0x00, 0x00, /*ToUInt64Key("Yacute")*/  0xDD, 0x00, /*'\x00dd'*/
                        0x4E, 0x52, 0x4F, 0x48, 0x54, 0x00, 0x00, 0x00, /*ToUInt64Key("THORN")*/   0xDE, 0x00, /*'\x00de'*/
                        0x67, 0x69, 0x6C, 0x7A, 0x73, 0x00, 0x00, 0x00, /*ToUInt64Key("szlig")*/   0xDF, 0x00, /*'\x00df'*/
                        0x65, 0x76, 0x61, 0x72, 0x67, 0x61, 0x00, 0x00, /*ToUInt64Key("agrave")*/  0xE0, 0x00, /*'\x00e0'*/
                        0x65, 0x74, 0x75, 0x63, 0x61, 0x61, 0x00, 0x00, /*ToUInt64Key("aacute")*/  0xE1, 0x00, /*'\x00e1'*/
                        0x63, 0x72, 0x69, 0x63, 0x61, 0x00, 0x00, 0x00, /*ToUInt64Key("acirc")*/   0xE2, 0x00, /*'\x00e2'*/
                        0x65, 0x64, 0x6C, 0x69, 0x74, 0x61, 0x00, 0x00, /*ToUInt64Key("atilde")*/  0xE3, 0x00, /*'\x00e3'*/
                        0x6C, 0x6D, 0x75, 0x61, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("auml")*/    0xE4, 0x00, /*'\x00e4'*/
                        0x67, 0x6E, 0x69, 0x72, 0x61, 0x00, 0x00, 0x00, /*ToUInt64Key("aring")*/   0xE5, 0x00, /*'\x00e5'*/
                        0x67, 0x69, 0x6C, 0x65, 0x61, 0x00, 0x00, 0x00, /*ToUInt64Key("aelig")*/   0xE6, 0x00, /*'\x00e6'*/
                        0x6C, 0x69, 0x64, 0x65, 0x63, 0x63, 0x00, 0x00, /*ToUInt64Key("ccedil")*/  0xE7, 0x00, /*'\x00e7'*/
                        0x65, 0x76, 0x61, 0x72, 0x67, 0x65, 0x00, 0x00, /*ToUInt64Key("egrave")*/  0xE8, 0x00, /*'\x00e8'*/
                        0x65, 0x74, 0x75, 0x63, 0x61, 0x65, 0x00, 0x00, /*ToUInt64Key("eacute")*/  0xE9, 0x00, /*'\x00e9'*/
                        0x63, 0x72, 0x69, 0x63, 0x65, 0x00, 0x00, 0x00, /*ToUInt64Key("ecirc")*/   0xEA, 0x00, /*'\x00ea'*/
                        0x6C, 0x6D, 0x75, 0x65, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("euml")*/    0xEB, 0x00, /*'\x00eb'*/
                        0x65, 0x76, 0x61, 0x72, 0x67, 0x69, 0x00, 0x00, /*ToUInt64Key("igrave")*/  0xEC, 0x00, /*'\x00ec'*/
                        0x65, 0x74, 0x75, 0x63, 0x61, 0x69, 0x00, 0x00, /*ToUInt64Key("iacute")*/  0xED, 0x00, /*'\x00ed'*/
                        0x63, 0x72, 0x69, 0x63, 0x69, 0x00, 0x00, 0x00, /*ToUInt64Key("icirc")*/   0xEE, 0x00, /*'\x00ee'*/
                        0x6C, 0x6D, 0x75, 0x69, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("iuml")*/    0xEF, 0x00, /*'\x00ef'*/
                        0x68, 0x74, 0x65, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("eth")*/     0xF0, 0x00, /*'\x00f0'*/
                        0x65, 0x64, 0x6C, 0x69, 0x74, 0x6E, 0x00, 0x00, /*ToUInt64Key("ntilde")*/  0xF1, 0x00, /*'\x00f1'*/
                        0x65, 0x76, 0x61, 0x72, 0x67, 0x6F, 0x00, 0x00, /*ToUInt64Key("ograve")*/  0xF2, 0x00, /*'\x00f2'*/
                        0x65, 0x74, 0x75, 0x63, 0x61, 0x6F, 0x00, 0x00, /*ToUInt64Key("oacute")*/  0xF3, 0x00, /*'\x00f3'*/
                        0x63, 0x72, 0x69, 0x63, 0x6F, 0x00, 0x00, 0x00, /*ToUInt64Key("ocirc")*/   0xF4, 0x00, /*'\x00f4'*/
                        0x65, 0x64, 0x6C, 0x69, 0x74, 0x6F, 0x00, 0x00, /*ToUInt64Key("otilde")*/  0xF5, 0x00, /*'\x00f5'*/
                        0x6C, 0x6D, 0x75, 0x6F, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("ouml")*/    0xF6, 0x00, /*'\x00f6'*/
                        0x65, 0x64, 0x69, 0x76, 0x69, 0x64, 0x00, 0x00, /*ToUInt64Key("divide")*/  0xF7, 0x00, /*'\x00f7'*/
                        0x68, 0x73, 0x61, 0x6C, 0x73, 0x6F, 0x00, 0x00, /*ToUInt64Key("oslash")*/  0xF8, 0x00, /*'\x00f8'*/
                        0x65, 0x76, 0x61, 0x72, 0x67, 0x75, 0x00, 0x00, /*ToUInt64Key("ugrave")*/  0xF9, 0x00, /*'\x00f9'*/
                        0x65, 0x74, 0x75, 0x63, 0x61, 0x75, 0x00, 0x00, /*ToUInt64Key("uacute")*/  0xFA, 0x00, /*'\x00fa'*/
                        0x63, 0x72, 0x69, 0x63, 0x75, 0x00, 0x00, 0x00, /*ToUInt64Key("ucirc")*/   0xFB, 0x00, /*'\x00fb'*/
                        0x6C, 0x6D, 0x75, 0x75, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("uuml")*/    0xFC, 0x00, /*'\x00fc'*/
                        0x65, 0x74, 0x75, 0x63, 0x61, 0x79, 0x00, 0x00, /*ToUInt64Key("yacute")*/  0xFD, 0x00, /*'\x00fd'*/
                        0x6E, 0x72, 0x6F, 0x68, 0x74, 0x00, 0x00, 0x00, /*ToUInt64Key("thorn")*/   0xFE, 0x00, /*'\x00fe'*/
                        0x6C, 0x6D, 0x75, 0x79, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("yuml")*/    0xFF, 0x00, /*'\x00ff'*/
                        0x67, 0x69, 0x6C, 0x45, 0x4F, 0x00, 0x00, 0x00, /*ToUInt64Key("OElig")*/   0x52, 0x01, /*'\x0152'*/
                        0x67, 0x69, 0x6C, 0x65, 0x6F, 0x00, 0x00, 0x00, /*ToUInt64Key("oelig")*/   0x53, 0x01, /*'\x0153'*/
                        0x6E, 0x6F, 0x72, 0x61, 0x63, 0x53, 0x00, 0x00, /*ToUInt64Key("Scaron")*/  0x60, 0x01, /*'\x0160'*/
                        0x6E, 0x6F, 0x72, 0x61, 0x63, 0x73, 0x00, 0x00, /*ToUInt64Key("scaron")*/  0x61, 0x01, /*'\x0161'*/
                        0x6C, 0x6D, 0x75, 0x59, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("Yuml")*/    0x78, 0x01, /*'\x0178'*/
                        0x66, 0x6F, 0x6E, 0x66, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("fnof")*/    0x92, 0x01, /*'\x0192'*/
                        0x63, 0x72, 0x69, 0x63, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("circ")*/    0xC6, 0x02, /*'\x02c6'*/
                        0x65, 0x64, 0x6C, 0x69, 0x74, 0x00, 0x00, 0x00, /*ToUInt64Key("tilde")*/   0xDC, 0x02, /*'\x02dc'*/
                        0x61, 0x68, 0x70, 0x6C, 0x41, 0x00, 0x00, 0x00, /*ToUInt64Key("Alpha")*/   0x91, 0x03, /*'\x0391'*/
                        0x61, 0x74, 0x65, 0x42, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("Beta")*/    0x92, 0x03, /*'\x0392'*/
                        0x61, 0x6D, 0x6D, 0x61, 0x47, 0x00, 0x00, 0x00, /*ToUInt64Key("Gamma")*/   0x93, 0x03, /*'\x0393'*/
                        0x61, 0x74, 0x6C, 0x65, 0x44, 0x00, 0x00, 0x00, /*ToUInt64Key("Delta")*/   0x94, 0x03, /*'\x0394'*/
                        0x6E, 0x6F, 0x6C, 0x69, 0x73, 0x70, 0x45, 0x00, /*ToUInt64Key("Epsilon")*/ 0x95, 0x03, /*'\x0395'*/
                        0x61, 0x74, 0x65, 0x5A, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("Zeta")*/    0x96, 0x03, /*'\x0396'*/
                        0x61, 0x74, 0x45, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("Eta")*/     0x97, 0x03, /*'\x0397'*/
                        0x61, 0x74, 0x65, 0x68, 0x54, 0x00, 0x00, 0x00, /*ToUInt64Key("Theta")*/   0x98, 0x03, /*'\x0398'*/
                        0x61, 0x74, 0x6F, 0x49, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("Iota")*/    0x99, 0x03, /*'\x0399'*/
                        0x61, 0x70, 0x70, 0x61, 0x4B, 0x00, 0x00, 0x00, /*ToUInt64Key("Kappa")*/   0x9A, 0x03, /*'\x039a'*/
                        0x61, 0x64, 0x62, 0x6D, 0x61, 0x4C, 0x00, 0x00, /*ToUInt64Key("Lambda")*/  0x9B, 0x03, /*'\x039b'*/
                        0x75, 0x4D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("Mu")*/      0x9C, 0x03, /*'\x039c'*/
                        0x75, 0x4E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("Nu")*/      0x9D, 0x03, /*'\x039d'*/
                        0x69, 0x58, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("Xi")*/      0x9E, 0x03, /*'\x039e'*/
                        0x6E, 0x6F, 0x72, 0x63, 0x69, 0x6D, 0x4F, 0x00, /*ToUInt64Key("Omicron")*/ 0x9F, 0x03, /*'\x039f'*/
                        0x69, 0x50, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("Pi")*/      0xA0, 0x03, /*'\x03a0'*/
                        0x6F, 0x68, 0x52, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("Rho")*/     0xA1, 0x03, /*'\x03a1'*/
                        0x61, 0x6D, 0x67, 0x69, 0x53, 0x00, 0x00, 0x00, /*ToUInt64Key("Sigma")*/   0xA3, 0x03, /*'\x03a3'*/
                        0x75, 0x61, 0x54, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("Tau")*/     0xA4, 0x03, /*'\x03a4'*/
                        0x6E, 0x6F, 0x6C, 0x69, 0x73, 0x70, 0x55, 0x00, /*ToUInt64Key("Upsilon")*/ 0xA5, 0x03, /*'\x03a5'*/
                        0x69, 0x68, 0x50, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("Phi")*/     0xA6, 0x03, /*'\x03a6'*/
                        0x69, 0x68, 0x43, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("Chi")*/     0xA7, 0x03, /*'\x03a7'*/
                        0x69, 0x73, 0x50, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("Psi")*/     0xA8, 0x03, /*'\x03a8'*/
                        0x61, 0x67, 0x65, 0x6D, 0x4F, 0x00, 0x00, 0x00, /*ToUInt64Key("Omega")*/   0xA9, 0x03, /*'\x03a9'*/
                        0x61, 0x68, 0x70, 0x6C, 0x61, 0x00, 0x00, 0x00, /*ToUInt64Key("alpha")*/   0xB1, 0x03, /*'\x03b1'*/
                        0x61, 0x74, 0x65, 0x62, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("beta")*/    0xB2, 0x03, /*'\x03b2'*/
                        0x61, 0x6D, 0x6D, 0x61, 0x67, 0x00, 0x00, 0x00, /*ToUInt64Key("gamma")*/   0xB3, 0x03, /*'\x03b3'*/
                        0x61, 0x74, 0x6C, 0x65, 0x64, 0x00, 0x00, 0x00, /*ToUInt64Key("delta")*/   0xB4, 0x03, /*'\x03b4'*/
                        0x6E, 0x6F, 0x6C, 0x69, 0x73, 0x70, 0x65, 0x00, /*ToUInt64Key("epsilon")*/ 0xB5, 0x03, /*'\x03b5'*/
                        0x61, 0x74, 0x65, 0x7A, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("zeta")*/    0xB6, 0x03, /*'\x03b6'*/
                        0x61, 0x74, 0x65, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("eta")*/     0xB7, 0x03, /*'\x03b7'*/
                        0x61, 0x74, 0x65, 0x68, 0x74, 0x00, 0x00, 0x00, /*ToUInt64Key("theta")*/   0xB8, 0x03, /*'\x03b8'*/
                        0x61, 0x74, 0x6F, 0x69, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("iota")*/    0xB9, 0x03, /*'\x03b9'*/
                        0x61, 0x70, 0x70, 0x61, 0x6B, 0x00, 0x00, 0x00, /*ToUInt64Key("kappa")*/   0xBA, 0x03, /*'\x03ba'*/
                        0x61, 0x64, 0x62, 0x6D, 0x61, 0x6C, 0x00, 0x00, /*ToUInt64Key("lambda")*/  0xBB, 0x03, /*'\x03bb'*/
                        0x75, 0x6D, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("mu")*/      0xBC, 0x03, /*'\x03bc'*/
                        0x75, 0x6E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("nu")*/      0xBD, 0x03, /*'\x03bd'*/
                        0x69, 0x78, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("xi")*/      0xBE, 0x03, /*'\x03be'*/
                        0x6E, 0x6F, 0x72, 0x63, 0x69, 0x6D, 0x6F, 0x00, /*ToUInt64Key("omicron")*/ 0xBF, 0x03, /*'\x03bf'*/
                        0x69, 0x70, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("pi")*/      0xC0, 0x03, /*'\x03c0'*/
                        0x6F, 0x68, 0x72, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("rho")*/     0xC1, 0x03, /*'\x03c1'*/
                        0x66, 0x61, 0x6D, 0x67, 0x69, 0x73, 0x00, 0x00, /*ToUInt64Key("sigmaf")*/  0xC2, 0x03, /*'\x03c2'*/
                        0x61, 0x6D, 0x67, 0x69, 0x73, 0x00, 0x00, 0x00, /*ToUInt64Key("sigma")*/   0xC3, 0x03, /*'\x03c3'*/
                        0x75, 0x61, 0x74, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("tau")*/     0xC4, 0x03, /*'\x03c4'*/
                        0x6E, 0x6F, 0x6C, 0x69, 0x73, 0x70, 0x75, 0x00, /*ToUInt64Key("upsilon")*/ 0xC5, 0x03, /*'\x03c5'*/
                        0x69, 0x68, 0x70, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("phi")*/     0xC6, 0x03, /*'\x03c6'*/
                        0x69, 0x68, 0x63, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("chi")*/     0xC7, 0x03, /*'\x03c7'*/
                        0x69, 0x73, 0x70, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("psi")*/     0xC8, 0x03, /*'\x03c8'*/
                        0x61, 0x67, 0x65, 0x6D, 0x6F, 0x00, 0x00, 0x00, /*ToUInt64Key("omega")*/   0xC9, 0x03, /*'\x03c9'*/
                        0x6D, 0x79, 0x73, 0x61, 0x74, 0x65, 0x68, 0x74, /*ToUInt64Key("thetasym")*/0xD1, 0x03, /*'\x03d1'*/
                        0x68, 0x69, 0x73, 0x70, 0x75, 0x00, 0x00, 0x00, /*ToUInt64Key("upsih")*/   0xD2, 0x03, /*'\x03d2'*/
                        0x76, 0x69, 0x70, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("piv")*/     0xD6, 0x03, /*'\x03d6'*/
                        0x70, 0x73, 0x6E, 0x65, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("ensp")*/    0x02, 0x20, /*'\x2002'*/
                        0x70, 0x73, 0x6D, 0x65, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("emsp")*/    0x03, 0x20, /*'\x2003'*/
                        0x70, 0x73, 0x6E, 0x69, 0x68, 0x74, 0x00, 0x00, /*ToUInt64Key("thinsp")*/  0x09, 0x20, /*'\x2009'*/
                        0x6A, 0x6E, 0x77, 0x7A, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("zwnj")*/    0x0C, 0x20, /*'\x200c'*/
                        0x6A, 0x77, 0x7A, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("zwj")*/     0x0D, 0x20, /*'\x200d'*/
                        0x6D, 0x72, 0x6C, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("lrm")*/     0x0E, 0x20, /*'\x200e'*/
                        0x6D, 0x6C, 0x72, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("rlm")*/     0x0F, 0x20, /*'\x200f'*/
                        0x68, 0x73, 0x61, 0x64, 0x6E, 0x00, 0x00, 0x00, /*ToUInt64Key("ndash")*/   0x13, 0x20, /*'\x2013'*/
                        0x68, 0x73, 0x61, 0x64, 0x6D, 0x00, 0x00, 0x00, /*ToUInt64Key("mdash")*/   0x14, 0x20, /*'\x2014'*/
                        0x6F, 0x75, 0x71, 0x73, 0x6C, 0x00, 0x00, 0x00, /*ToUInt64Key("lsquo")*/   0x18, 0x20, /*'\x2018'*/
                        0x6F, 0x75, 0x71, 0x73, 0x72, 0x00, 0x00, 0x00, /*ToUInt64Key("rsquo")*/   0x19, 0x20, /*'\x2019'*/
                        0x6F, 0x75, 0x71, 0x62, 0x73, 0x00, 0x00, 0x00, /*ToUInt64Key("sbquo")*/   0x1A, 0x20, /*'\x201a'*/
                        0x6F, 0x75, 0x71, 0x64, 0x6C, 0x00, 0x00, 0x00, /*ToUInt64Key("ldquo")*/   0x1C, 0x20, /*'\x201c'*/
                        0x6F, 0x75, 0x71, 0x64, 0x72, 0x00, 0x00, 0x00, /*ToUInt64Key("rdquo")*/   0x1D, 0x20, /*'\x201d'*/
                        0x6F, 0x75, 0x71, 0x64, 0x62, 0x00, 0x00, 0x00, /*ToUInt64Key("bdquo")*/   0x1E, 0x20, /*'\x201e'*/
                        0x72, 0x65, 0x67, 0x67, 0x61, 0x64, 0x00, 0x00, /*ToUInt64Key("dagger")*/  0x20, 0x20, /*'\x2020'*/
                        0x72, 0x65, 0x67, 0x67, 0x61, 0x44, 0x00, 0x00, /*ToUInt64Key("Dagger")*/  0x21, 0x20, /*'\x2021'*/
                        0x6C, 0x6C, 0x75, 0x62, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("bull")*/    0x22, 0x20, /*'\x2022'*/
                        0x70, 0x69, 0x6C, 0x6C, 0x65, 0x68, 0x00, 0x00, /*ToUInt64Key("hellip")*/  0x26, 0x20, /*'\x2026'*/
                        0x6C, 0x69, 0x6D, 0x72, 0x65, 0x70, 0x00, 0x00, /*ToUInt64Key("permil")*/  0x30, 0x20, /*'\x2030'*/
                        0x65, 0x6D, 0x69, 0x72, 0x70, 0x00, 0x00, 0x00, /*ToUInt64Key("prime")*/   0x32, 0x20, /*'\x2032'*/
                        0x65, 0x6D, 0x69, 0x72, 0x50, 0x00, 0x00, 0x00, /*ToUInt64Key("Prime")*/   0x33, 0x20, /*'\x2033'*/
                        0x6F, 0x75, 0x71, 0x61, 0x73, 0x6C, 0x00, 0x00, /*ToUInt64Key("lsaquo")*/  0x39, 0x20, /*'\x2039'*/
                        0x6F, 0x75, 0x71, 0x61, 0x73, 0x72, 0x00, 0x00, /*ToUInt64Key("rsaquo")*/  0x3A, 0x20, /*'\x203a'*/
                        0x65, 0x6E, 0x69, 0x6C, 0x6F, 0x00, 0x00, 0x00, /*ToUInt64Key("oline")*/   0x3E, 0x20, /*'\x203e'*/
                        0x6C, 0x73, 0x61, 0x72, 0x66, 0x00, 0x00, 0x00, /*ToUInt64Key("frasl")*/   0x44, 0x20, /*'\x2044'*/
                        0x6F, 0x72, 0x75, 0x65, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("euro")*/    0xAC, 0x20, /*'\x20ac'*/
                        0x65, 0x67, 0x61, 0x6D, 0x69, 0x00, 0x00, 0x00, /*ToUInt64Key("image")*/   0x11, 0x21, /*'\x2111'*/
                        0x70, 0x72, 0x65, 0x69, 0x65, 0x77, 0x00, 0x00, /*ToUInt64Key("weierp")*/  0x18, 0x21, /*'\x2118'*/
                        0x6C, 0x61, 0x65, 0x72, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("real")*/    0x1C, 0x21, /*'\x211c'*/
                        0x65, 0x64, 0x61, 0x72, 0x74, 0x00, 0x00, 0x00, /*ToUInt64Key("trade")*/   0x22, 0x21, /*'\x2122'*/
                        0x6D, 0x79, 0x73, 0x66, 0x65, 0x6C, 0x61, 0x00, /*ToUInt64Key("alefsym")*/ 0x35, 0x21, /*'\x2135'*/
                        0x72, 0x72, 0x61, 0x6C, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("larr")*/    0x90, 0x21, /*'\x2190'*/
                        0x72, 0x72, 0x61, 0x75, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("uarr")*/    0x91, 0x21, /*'\x2191'*/
                        0x72, 0x72, 0x61, 0x72, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("rarr")*/    0x92, 0x21, /*'\x2192'*/
                        0x72, 0x72, 0x61, 0x64, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("darr")*/    0x93, 0x21, /*'\x2193'*/
                        0x72, 0x72, 0x61, 0x68, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("harr")*/    0x94, 0x21, /*'\x2194'*/
                        0x72, 0x72, 0x61, 0x72, 0x63, 0x00, 0x00, 0x00, /*ToUInt64Key("crarr")*/   0xB5, 0x21, /*'\x21b5'*/
                        0x72, 0x72, 0x41, 0x6C, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("lArr")*/    0xD0, 0x21, /*'\x21d0'*/
                        0x72, 0x72, 0x41, 0x75, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("uArr")*/    0xD1, 0x21, /*'\x21d1'*/
                        0x72, 0x72, 0x41, 0x72, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("rArr")*/    0xD2, 0x21, /*'\x21d2'*/
                        0x72, 0x72, 0x41, 0x64, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("dArr")*/    0xD3, 0x21, /*'\x21d3'*/
                        0x72, 0x72, 0x41, 0x68, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("hArr")*/    0xD4, 0x21, /*'\x21d4'*/
                        0x6C, 0x6C, 0x61, 0x72, 0x6F, 0x66, 0x00, 0x00, /*ToUInt64Key("forall")*/  0x00, 0x22, /*'\x2200'*/
                        0x74, 0x72, 0x61, 0x70, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("part")*/    0x02, 0x22, /*'\x2202'*/
                        0x74, 0x73, 0x69, 0x78, 0x65, 0x00, 0x00, 0x00, /*ToUInt64Key("exist")*/   0x03, 0x22, /*'\x2203'*/
                        0x79, 0x74, 0x70, 0x6D, 0x65, 0x00, 0x00, 0x00, /*ToUInt64Key("empty")*/   0x05, 0x22, /*'\x2205'*/
                        0x61, 0x6C, 0x62, 0x61, 0x6E, 0x00, 0x00, 0x00, /*ToUInt64Key("nabla")*/   0x07, 0x22, /*'\x2207'*/
                        0x6E, 0x69, 0x73, 0x69, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("isin")*/    0x08, 0x22, /*'\x2208'*/
                        0x6E, 0x69, 0x74, 0x6F, 0x6E, 0x00, 0x00, 0x00, /*ToUInt64Key("notin")*/   0x09, 0x22, /*'\x2209'*/
                        0x69, 0x6E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("ni")*/      0x0B, 0x22, /*'\x220b'*/
                        0x64, 0x6F, 0x72, 0x70, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("prod")*/    0x0F, 0x22, /*'\x220f'*/
                        0x6D, 0x75, 0x73, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("sum")*/     0x11, 0x22, /*'\x2211'*/
                        0x73, 0x75, 0x6E, 0x69, 0x6D, 0x00, 0x00, 0x00, /*ToUInt64Key("minus")*/   0x12, 0x22, /*'\x2212'*/
                        0x74, 0x73, 0x61, 0x77, 0x6F, 0x6C, 0x00, 0x00, /*ToUInt64Key("lowast")*/  0x17, 0x22, /*'\x2217'*/
                        0x63, 0x69, 0x64, 0x61, 0x72, 0x00, 0x00, 0x00, /*ToUInt64Key("radic")*/   0x1A, 0x22, /*'\x221a'*/
                        0x70, 0x6F, 0x72, 0x70, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("prop")*/    0x1D, 0x22, /*'\x221d'*/
                        0x6E, 0x69, 0x66, 0x6E, 0x69, 0x00, 0x00, 0x00, /*ToUInt64Key("infin")*/   0x1E, 0x22, /*'\x221e'*/
                        0x67, 0x6E, 0x61, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("ang")*/     0x20, 0x22, /*'\x2220'*/
                        0x64, 0x6E, 0x61, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("and")*/     0x27, 0x22, /*'\x2227'*/
                        0x72, 0x6F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("or")*/      0x28, 0x22, /*'\x2228'*/
                        0x70, 0x61, 0x63, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("cap")*/     0x29, 0x22, /*'\x2229'*/
                        0x70, 0x75, 0x63, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("cup")*/     0x2A, 0x22, /*'\x222a'*/
                        0x74, 0x6E, 0x69, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("int")*/     0x2B, 0x22, /*'\x222b'*/
                        0x34, 0x65, 0x72, 0x65, 0x68, 0x74, 0x00, 0x00, /*ToUInt64Key("there4")*/  0x34, 0x22, /*'\x2234'*/
                        0x6D, 0x69, 0x73, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("sim")*/     0x3C, 0x22, /*'\x223c'*/
                        0x67, 0x6E, 0x6F, 0x63, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("cong")*/    0x45, 0x22, /*'\x2245'*/
                        0x70, 0x6D, 0x79, 0x73, 0x61, 0x00, 0x00, 0x00, /*ToUInt64Key("asymp")*/   0x48, 0x22, /*'\x2248'*/
                        0x65, 0x6E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("ne")*/      0x60, 0x22, /*'\x2260'*/
                        0x76, 0x69, 0x75, 0x71, 0x65, 0x00, 0x00, 0x00, /*ToUInt64Key("equiv")*/   0x61, 0x22, /*'\x2261'*/
                        0x65, 0x6C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("le")*/      0x64, 0x22, /*'\x2264'*/
                        0x65, 0x67, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("ge")*/      0x65, 0x22, /*'\x2265'*/
                        0x62, 0x75, 0x73, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("sub")*/     0x82, 0x22, /*'\x2282'*/
                        0x70, 0x75, 0x73, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("sup")*/     0x83, 0x22, /*'\x2283'*/
                        0x62, 0x75, 0x73, 0x6E, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("nsub")*/    0x84, 0x22, /*'\x2284'*/
                        0x65, 0x62, 0x75, 0x73, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("sube")*/    0x86, 0x22, /*'\x2286'*/
                        0x65, 0x70, 0x75, 0x73, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("supe")*/    0x87, 0x22, /*'\x2287'*/
                        0x73, 0x75, 0x6C, 0x70, 0x6F, 0x00, 0x00, 0x00, /*ToUInt64Key("oplus")*/   0x95, 0x22, /*'\x2295'*/
                        0x73, 0x65, 0x6D, 0x69, 0x74, 0x6F, 0x00, 0x00, /*ToUInt64Key("otimes")*/  0x97, 0x22, /*'\x2297'*/
                        0x70, 0x72, 0x65, 0x70, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("perp")*/    0xA5, 0x22, /*'\x22a5'*/
                        0x74, 0x6F, 0x64, 0x73, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("sdot")*/    0xC5, 0x22, /*'\x22c5'*/
                        0x6C, 0x69, 0x65, 0x63, 0x6C, 0x00, 0x00, 0x00, /*ToUInt64Key("lceil")*/   0x08, 0x23, /*'\x2308'*/
                        0x6C, 0x69, 0x65, 0x63, 0x72, 0x00, 0x00, 0x00, /*ToUInt64Key("rceil")*/   0x09, 0x23, /*'\x2309'*/
                        0x72, 0x6F, 0x6F, 0x6C, 0x66, 0x6C, 0x00, 0x00, /*ToUInt64Key("lfloor")*/  0x0A, 0x23, /*'\x230a'*/
                        0x72, 0x6F, 0x6F, 0x6C, 0x66, 0x72, 0x00, 0x00, /*ToUInt64Key("rfloor")*/  0x0B, 0x23, /*'\x230b'*/
                        0x67, 0x6E, 0x61, 0x6C, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("lang")*/    0x29, 0x23, /*'\x2329'*/
                        0x67, 0x6E, 0x61, 0x72, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("rang")*/    0x2A, 0x23, /*'\x232a'*/
                        0x7A, 0x6F, 0x6C, 0x00, 0x00, 0x00, 0x00, 0x00, /*ToUInt64Key("loz")*/     0xCA, 0x25, /*'\x25ca'*/
                        0x73, 0x65, 0x64, 0x61, 0x70, 0x73, 0x00, 0x00, /*ToUInt64Key("spades")*/  0x60, 0x26, /*'\x2660'*/
                        0x73, 0x62, 0x75, 0x6C, 0x63, 0x00, 0x00, 0x00, /*ToUInt64Key("clubs")*/   0x63, 0x26, /*'\x2663'*/
                        0x73, 0x74, 0x72, 0x61, 0x65, 0x68, 0x00, 0x00, /*ToUInt64Key("hearts")*/  0x65, 0x26, /*'\x2665'*/
                        0x73, 0x6D, 0x61, 0x69, 0x64, 0x00, 0x00, 0x00, /*ToUInt64Key("diams")*/   0x66, 0x26, /*'\x2666'*/
                    };

                var dictionary = new Dictionary<ulong, char>(tableData.Length / (sizeof(ulong) + sizeof(char)));
                while (!tableData.IsEmpty)
                {
                    ulong key = BinaryPrimitives.ReadUInt64LittleEndian(tableData);
                    char value = (char) BinaryPrimitives.ReadUInt16LittleEndian(tableData.Slice(sizeof(ulong)));
                    dictionary[key] = value;
                    tableData = tableData.Slice(sizeof(ulong) + sizeof(char));
                }
                return dictionary;
            }

            // maps entity strings => unicode chars
            private static readonly Dictionary<ulong, char> s_lookupTable = InitializeLookupTable();

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
