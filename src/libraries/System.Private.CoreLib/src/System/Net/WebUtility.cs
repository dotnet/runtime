// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                Debug.Assert(s_lookupTable.Count == Count, $"There should be {Count} HTML entities, but {nameof(s_lookupTable)} has {s_lookupTable.Count} of them.");

                // Just a quick check precalculated values in s_lookupTable are correct
                Debug.Assert(ToUInt64Key("quot") == 0x71756F74);
                Debug.Assert(ToUInt64Key("diams") == 0x6469616D73);
            }
#endif

            // The list is from http://www.w3.org/TR/REC-html40/sgml/entities.html, except for &apos;, which
            // is defined in http://www.w3.org/TR/2008/REC-xml-20081126/#sec-predefined-ent.

            private const int Count = 253;

            // maps entity strings => unicode chars
            private static readonly Dictionary<ulong, char> s_lookupTable =
                new Dictionary<ulong, char>(Count)
                {
                    // hashcodes are precalculated
                    [/*ToUInt64Key("quot")*/ 0x71756F74] = '\x0022',
                    [/*ToUInt64Key("amp")*/ 0x616D70] = '\x0026',
                    [/*ToUInt64Key("apos")*/ 0x61706F73] = '\x0027',
                    [/*ToUInt64Key("lt")*/ 0x6C74] = '\x003c',
                    [/*ToUInt64Key("gt")*/ 0x6774] = '\x003e',
                    [/*ToUInt64Key("nbsp")*/ 0x6E627370] = '\x00a0',
                    [/*ToUInt64Key("iexcl")*/ 0x696578636C] = '\x00a1',
                    [/*ToUInt64Key("cent")*/ 0x63656E74] = '\x00a2',
                    [/*ToUInt64Key("pound")*/ 0x706F756E64] = '\x00a3',
                    [/*ToUInt64Key("curren")*/ 0x63757272656E] = '\x00a4',
                    [/*ToUInt64Key("yen")*/ 0x79656E] = '\x00a5',
                    [/*ToUInt64Key("brvbar")*/ 0x627276626172] = '\x00a6',
                    [/*ToUInt64Key("sect")*/ 0x73656374] = '\x00a7',
                    [/*ToUInt64Key("uml")*/ 0x756D6C] = '\x00a8',
                    [/*ToUInt64Key("copy")*/ 0x636F7079] = '\x00a9',
                    [/*ToUInt64Key("ordf")*/ 0x6F726466] = '\x00aa',
                    [/*ToUInt64Key("laquo")*/ 0x6C6171756F] = '\x00ab',
                    [/*ToUInt64Key("not")*/ 0x6E6F74] = '\x00ac',
                    [/*ToUInt64Key("shy")*/ 0x736879] = '\x00ad',
                    [/*ToUInt64Key("reg")*/ 0x726567] = '\x00ae',
                    [/*ToUInt64Key("macr")*/ 0x6D616372] = '\x00af',
                    [/*ToUInt64Key("deg")*/ 0x646567] = '\x00b0',
                    [/*ToUInt64Key("plusmn")*/ 0x706C75736D6E] = '\x00b1',
                    [/*ToUInt64Key("sup2")*/ 0x73757032] = '\x00b2',
                    [/*ToUInt64Key("sup3")*/ 0x73757033] = '\x00b3',
                    [/*ToUInt64Key("acute")*/ 0x6163757465] = '\x00b4',
                    [/*ToUInt64Key("micro")*/ 0x6D6963726F] = '\x00b5',
                    [/*ToUInt64Key("para")*/ 0x70617261] = '\x00b6',
                    [/*ToUInt64Key("middot")*/ 0x6D6964646F74] = '\x00b7',
                    [/*ToUInt64Key("cedil")*/ 0x636564696C] = '\x00b8',
                    [/*ToUInt64Key("sup1")*/ 0x73757031] = '\x00b9',
                    [/*ToUInt64Key("ordm")*/ 0x6F72646D] = '\x00ba',
                    [/*ToUInt64Key("raquo")*/ 0x726171756F] = '\x00bb',
                    [/*ToUInt64Key("frac14")*/ 0x667261633134] = '\x00bc',
                    [/*ToUInt64Key("frac12")*/ 0x667261633132] = '\x00bd',
                    [/*ToUInt64Key("frac34")*/ 0x667261633334] = '\x00be',
                    [/*ToUInt64Key("iquest")*/ 0x697175657374] = '\x00bf',
                    [/*ToUInt64Key("Agrave")*/ 0x416772617665] = '\x00c0',
                    [/*ToUInt64Key("Aacute")*/ 0x416163757465] = '\x00c1',
                    [/*ToUInt64Key("Acirc")*/ 0x4163697263] = '\x00c2',
                    [/*ToUInt64Key("Atilde")*/ 0x4174696C6465] = '\x00c3',
                    [/*ToUInt64Key("Auml")*/ 0x41756D6C] = '\x00c4',
                    [/*ToUInt64Key("Aring")*/ 0x4172696E67] = '\x00c5',
                    [/*ToUInt64Key("AElig")*/ 0x41456C6967] = '\x00c6',
                    [/*ToUInt64Key("Ccedil")*/ 0x43636564696C] = '\x00c7',
                    [/*ToUInt64Key("Egrave")*/ 0x456772617665] = '\x00c8',
                    [/*ToUInt64Key("Eacute")*/ 0x456163757465] = '\x00c9',
                    [/*ToUInt64Key("Ecirc")*/ 0x4563697263] = '\x00ca',
                    [/*ToUInt64Key("Euml")*/ 0x45756D6C] = '\x00cb',
                    [/*ToUInt64Key("Igrave")*/ 0x496772617665] = '\x00cc',
                    [/*ToUInt64Key("Iacute")*/ 0x496163757465] = '\x00cd',
                    [/*ToUInt64Key("Icirc")*/ 0x4963697263] = '\x00ce',
                    [/*ToUInt64Key("Iuml")*/ 0x49756D6C] = '\x00cf',
                    [/*ToUInt64Key("ETH")*/ 0x455448] = '\x00d0',
                    [/*ToUInt64Key("Ntilde")*/ 0x4E74696C6465] = '\x00d1',
                    [/*ToUInt64Key("Ograve")*/ 0x4F6772617665] = '\x00d2',
                    [/*ToUInt64Key("Oacute")*/ 0x4F6163757465] = '\x00d3',
                    [/*ToUInt64Key("Ocirc")*/ 0x4F63697263] = '\x00d4',
                    [/*ToUInt64Key("Otilde")*/ 0x4F74696C6465] = '\x00d5',
                    [/*ToUInt64Key("Ouml")*/ 0x4F756D6C] = '\x00d6',
                    [/*ToUInt64Key("times")*/ 0x74696D6573] = '\x00d7',
                    [/*ToUInt64Key("Oslash")*/ 0x4F736C617368] = '\x00d8',
                    [/*ToUInt64Key("Ugrave")*/ 0x556772617665] = '\x00d9',
                    [/*ToUInt64Key("Uacute")*/ 0x556163757465] = '\x00da',
                    [/*ToUInt64Key("Ucirc")*/ 0x5563697263] = '\x00db',
                    [/*ToUInt64Key("Uuml")*/ 0x55756D6C] = '\x00dc',
                    [/*ToUInt64Key("Yacute")*/ 0x596163757465] = '\x00dd',
                    [/*ToUInt64Key("THORN")*/ 0x54484F524E] = '\x00de',
                    [/*ToUInt64Key("szlig")*/ 0x737A6C6967] = '\x00df',
                    [/*ToUInt64Key("agrave")*/ 0x616772617665] = '\x00e0',
                    [/*ToUInt64Key("aacute")*/ 0x616163757465] = '\x00e1',
                    [/*ToUInt64Key("acirc")*/ 0x6163697263] = '\x00e2',
                    [/*ToUInt64Key("atilde")*/ 0x6174696C6465] = '\x00e3',
                    [/*ToUInt64Key("auml")*/ 0x61756D6C] = '\x00e4',
                    [/*ToUInt64Key("aring")*/ 0x6172696E67] = '\x00e5',
                    [/*ToUInt64Key("aelig")*/ 0x61656C6967] = '\x00e6',
                    [/*ToUInt64Key("ccedil")*/ 0x63636564696C] = '\x00e7',
                    [/*ToUInt64Key("egrave")*/ 0x656772617665] = '\x00e8',
                    [/*ToUInt64Key("eacute")*/ 0x656163757465] = '\x00e9',
                    [/*ToUInt64Key("ecirc")*/ 0x6563697263] = '\x00ea',
                    [/*ToUInt64Key("euml")*/ 0x65756D6C] = '\x00eb',
                    [/*ToUInt64Key("igrave")*/ 0x696772617665] = '\x00ec',
                    [/*ToUInt64Key("iacute")*/ 0x696163757465] = '\x00ed',
                    [/*ToUInt64Key("icirc")*/ 0x6963697263] = '\x00ee',
                    [/*ToUInt64Key("iuml")*/ 0x69756D6C] = '\x00ef',
                    [/*ToUInt64Key("eth")*/ 0x657468] = '\x00f0',
                    [/*ToUInt64Key("ntilde")*/ 0x6E74696C6465] = '\x00f1',
                    [/*ToUInt64Key("ograve")*/ 0x6F6772617665] = '\x00f2',
                    [/*ToUInt64Key("oacute")*/ 0x6F6163757465] = '\x00f3',
                    [/*ToUInt64Key("ocirc")*/ 0x6F63697263] = '\x00f4',
                    [/*ToUInt64Key("otilde")*/ 0x6F74696C6465] = '\x00f5',
                    [/*ToUInt64Key("ouml")*/ 0x6F756D6C] = '\x00f6',
                    [/*ToUInt64Key("divide")*/ 0x646976696465] = '\x00f7',
                    [/*ToUInt64Key("oslash")*/ 0x6F736C617368] = '\x00f8',
                    [/*ToUInt64Key("ugrave")*/ 0x756772617665] = '\x00f9',
                    [/*ToUInt64Key("uacute")*/ 0x756163757465] = '\x00fa',
                    [/*ToUInt64Key("ucirc")*/ 0x7563697263] = '\x00fb',
                    [/*ToUInt64Key("uuml")*/ 0x75756D6C] = '\x00fc',
                    [/*ToUInt64Key("yacute")*/ 0x796163757465] = '\x00fd',
                    [/*ToUInt64Key("thorn")*/ 0x74686F726E] = '\x00fe',
                    [/*ToUInt64Key("yuml")*/ 0x79756D6C] = '\x00ff',
                    [/*ToUInt64Key("OElig")*/ 0x4F456C6967] = '\x0152',
                    [/*ToUInt64Key("oelig")*/ 0x6F656C6967] = '\x0153',
                    [/*ToUInt64Key("Scaron")*/ 0x536361726F6E] = '\x0160',
                    [/*ToUInt64Key("scaron")*/ 0x736361726F6E] = '\x0161',
                    [/*ToUInt64Key("Yuml")*/ 0x59756D6C] = '\x0178',
                    [/*ToUInt64Key("fnof")*/ 0x666E6F66] = '\x0192',
                    [/*ToUInt64Key("circ")*/ 0x63697263] = '\x02c6',
                    [/*ToUInt64Key("tilde")*/ 0x74696C6465] = '\x02dc',
                    [/*ToUInt64Key("Alpha")*/ 0x416C706861] = '\x0391',
                    [/*ToUInt64Key("Beta")*/ 0x42657461] = '\x0392',
                    [/*ToUInt64Key("Gamma")*/ 0x47616D6D61] = '\x0393',
                    [/*ToUInt64Key("Delta")*/ 0x44656C7461] = '\x0394',
                    [/*ToUInt64Key("Epsilon")*/ 0x457073696C6F6E] = '\x0395',
                    [/*ToUInt64Key("Zeta")*/ 0x5A657461] = '\x0396',
                    [/*ToUInt64Key("Eta")*/ 0x457461] = '\x0397',
                    [/*ToUInt64Key("Theta")*/ 0x5468657461] = '\x0398',
                    [/*ToUInt64Key("Iota")*/ 0x496F7461] = '\x0399',
                    [/*ToUInt64Key("Kappa")*/ 0x4B61707061] = '\x039a',
                    [/*ToUInt64Key("Lambda")*/ 0x4C616D626461] = '\x039b',
                    [/*ToUInt64Key("Mu")*/ 0x4D75] = '\x039c',
                    [/*ToUInt64Key("Nu")*/ 0x4E75] = '\x039d',
                    [/*ToUInt64Key("Xi")*/ 0x5869] = '\x039e',
                    [/*ToUInt64Key("Omicron")*/ 0x4F6D6963726F6E] = '\x039f',
                    [/*ToUInt64Key("Pi")*/ 0x5069] = '\x03a0',
                    [/*ToUInt64Key("Rho")*/ 0x52686F] = '\x03a1',
                    [/*ToUInt64Key("Sigma")*/ 0x5369676D61] = '\x03a3',
                    [/*ToUInt64Key("Tau")*/ 0x546175] = '\x03a4',
                    [/*ToUInt64Key("Upsilon")*/ 0x557073696C6F6E] = '\x03a5',
                    [/*ToUInt64Key("Phi")*/ 0x506869] = '\x03a6',
                    [/*ToUInt64Key("Chi")*/ 0x436869] = '\x03a7',
                    [/*ToUInt64Key("Psi")*/ 0x507369] = '\x03a8',
                    [/*ToUInt64Key("Omega")*/ 0x4F6D656761] = '\x03a9',
                    [/*ToUInt64Key("alpha")*/ 0x616C706861] = '\x03b1',
                    [/*ToUInt64Key("beta")*/ 0x62657461] = '\x03b2',
                    [/*ToUInt64Key("gamma")*/ 0x67616D6D61] = '\x03b3',
                    [/*ToUInt64Key("delta")*/ 0x64656C7461] = '\x03b4',
                    [/*ToUInt64Key("epsilon")*/ 0x657073696C6F6E] = '\x03b5',
                    [/*ToUInt64Key("zeta")*/ 0x7A657461] = '\x03b6',
                    [/*ToUInt64Key("eta")*/ 0x657461] = '\x03b7',
                    [/*ToUInt64Key("theta")*/ 0x7468657461] = '\x03b8',
                    [/*ToUInt64Key("iota")*/ 0x696F7461] = '\x03b9',
                    [/*ToUInt64Key("kappa")*/ 0x6B61707061] = '\x03ba',
                    [/*ToUInt64Key("lambda")*/ 0x6C616D626461] = '\x03bb',
                    [/*ToUInt64Key("mu")*/ 0x6D75] = '\x03bc',
                    [/*ToUInt64Key("nu")*/ 0x6E75] = '\x03bd',
                    [/*ToUInt64Key("xi")*/ 0x7869] = '\x03be',
                    [/*ToUInt64Key("omicron")*/ 0x6F6D6963726F6E] = '\x03bf',
                    [/*ToUInt64Key("pi")*/ 0x7069] = '\x03c0',
                    [/*ToUInt64Key("rho")*/ 0x72686F] = '\x03c1',
                    [/*ToUInt64Key("sigmaf")*/ 0x7369676D6166] = '\x03c2',
                    [/*ToUInt64Key("sigma")*/ 0x7369676D61] = '\x03c3',
                    [/*ToUInt64Key("tau")*/ 0x746175] = '\x03c4',
                    [/*ToUInt64Key("upsilon")*/ 0x757073696C6F6E] = '\x03c5',
                    [/*ToUInt64Key("phi")*/ 0x706869] = '\x03c6',
                    [/*ToUInt64Key("chi")*/ 0x636869] = '\x03c7',
                    [/*ToUInt64Key("psi")*/ 0x707369] = '\x03c8',
                    [/*ToUInt64Key("omega")*/ 0x6F6D656761] = '\x03c9',
                    [/*ToUInt64Key("thetasym")*/ 0x746865746173796D] = '\x03d1',
                    [/*ToUInt64Key("upsih")*/ 0x7570736968] = '\x03d2',
                    [/*ToUInt64Key("piv")*/ 0x706976] = '\x03d6',
                    [/*ToUInt64Key("ensp")*/ 0x656E7370] = '\x2002',
                    [/*ToUInt64Key("emsp")*/ 0x656D7370] = '\x2003',
                    [/*ToUInt64Key("thinsp")*/ 0x7468696E7370] = '\x2009',
                    [/*ToUInt64Key("zwnj")*/ 0x7A776E6A] = '\x200c',
                    [/*ToUInt64Key("zwj")*/ 0x7A776A] = '\x200d',
                    [/*ToUInt64Key("lrm")*/ 0x6C726D] = '\x200e',
                    [/*ToUInt64Key("rlm")*/ 0x726C6D] = '\x200f',
                    [/*ToUInt64Key("ndash")*/ 0x6E64617368] = '\x2013',
                    [/*ToUInt64Key("mdash")*/ 0x6D64617368] = '\x2014',
                    [/*ToUInt64Key("lsquo")*/ 0x6C7371756F] = '\x2018',
                    [/*ToUInt64Key("rsquo")*/ 0x727371756F] = '\x2019',
                    [/*ToUInt64Key("sbquo")*/ 0x736271756F] = '\x201a',
                    [/*ToUInt64Key("ldquo")*/ 0x6C6471756F] = '\x201c',
                    [/*ToUInt64Key("rdquo")*/ 0x726471756F] = '\x201d',
                    [/*ToUInt64Key("bdquo")*/ 0x626471756F] = '\x201e',
                    [/*ToUInt64Key("dagger")*/ 0x646167676572] = '\x2020',
                    [/*ToUInt64Key("Dagger")*/ 0x446167676572] = '\x2021',
                    [/*ToUInt64Key("bull")*/ 0x62756C6C] = '\x2022',
                    [/*ToUInt64Key("hellip")*/ 0x68656C6C6970] = '\x2026',
                    [/*ToUInt64Key("permil")*/ 0x7065726D696C] = '\x2030',
                    [/*ToUInt64Key("prime")*/ 0x7072696D65] = '\x2032',
                    [/*ToUInt64Key("Prime")*/ 0x5072696D65] = '\x2033',
                    [/*ToUInt64Key("lsaquo")*/ 0x6C736171756F] = '\x2039',
                    [/*ToUInt64Key("rsaquo")*/ 0x72736171756F] = '\x203a',
                    [/*ToUInt64Key("oline")*/ 0x6F6C696E65] = '\x203e',
                    [/*ToUInt64Key("frasl")*/ 0x667261736C] = '\x2044',
                    [/*ToUInt64Key("euro")*/ 0x6575726F] = '\x20ac',
                    [/*ToUInt64Key("image")*/ 0x696D616765] = '\x2111',
                    [/*ToUInt64Key("weierp")*/ 0x776569657270] = '\x2118',
                    [/*ToUInt64Key("real")*/ 0x7265616C] = '\x211c',
                    [/*ToUInt64Key("trade")*/ 0x7472616465] = '\x2122',
                    [/*ToUInt64Key("alefsym")*/ 0x616C656673796D] = '\x2135',
                    [/*ToUInt64Key("larr")*/ 0x6C617272] = '\x2190',
                    [/*ToUInt64Key("uarr")*/ 0x75617272] = '\x2191',
                    [/*ToUInt64Key("rarr")*/ 0x72617272] = '\x2192',
                    [/*ToUInt64Key("darr")*/ 0x64617272] = '\x2193',
                    [/*ToUInt64Key("harr")*/ 0x68617272] = '\x2194',
                    [/*ToUInt64Key("crarr")*/ 0x6372617272] = '\x21b5',
                    [/*ToUInt64Key("lArr")*/ 0x6C417272] = '\x21d0',
                    [/*ToUInt64Key("uArr")*/ 0x75417272] = '\x21d1',
                    [/*ToUInt64Key("rArr")*/ 0x72417272] = '\x21d2',
                    [/*ToUInt64Key("dArr")*/ 0x64417272] = '\x21d3',
                    [/*ToUInt64Key("hArr")*/ 0x68417272] = '\x21d4',
                    [/*ToUInt64Key("forall")*/ 0x666F72616C6C] = '\x2200',
                    [/*ToUInt64Key("part")*/ 0x70617274] = '\x2202',
                    [/*ToUInt64Key("exist")*/ 0x6578697374] = '\x2203',
                    [/*ToUInt64Key("empty")*/ 0x656D707479] = '\x2205',
                    [/*ToUInt64Key("nabla")*/ 0x6E61626C61] = '\x2207',
                    [/*ToUInt64Key("isin")*/ 0x6973696E] = '\x2208',
                    [/*ToUInt64Key("notin")*/ 0x6E6F74696E] = '\x2209',
                    [/*ToUInt64Key("ni")*/ 0x6E69] = '\x220b',
                    [/*ToUInt64Key("prod")*/ 0x70726F64] = '\x220f',
                    [/*ToUInt64Key("sum")*/ 0x73756D] = '\x2211',
                    [/*ToUInt64Key("minus")*/ 0x6D696E7573] = '\x2212',
                    [/*ToUInt64Key("lowast")*/ 0x6C6F77617374] = '\x2217',
                    [/*ToUInt64Key("radic")*/ 0x7261646963] = '\x221a',
                    [/*ToUInt64Key("prop")*/ 0x70726F70] = '\x221d',
                    [/*ToUInt64Key("infin")*/ 0x696E66696E] = '\x221e',
                    [/*ToUInt64Key("ang")*/ 0x616E67] = '\x2220',
                    [/*ToUInt64Key("and")*/ 0x616E64] = '\x2227',
                    [/*ToUInt64Key("or")*/ 0x6F72] = '\x2228',
                    [/*ToUInt64Key("cap")*/ 0x636170] = '\x2229',
                    [/*ToUInt64Key("cup")*/ 0x637570] = '\x222a',
                    [/*ToUInt64Key("int")*/ 0x696E74] = '\x222b',
                    [/*ToUInt64Key("there4")*/ 0x746865726534] = '\x2234',
                    [/*ToUInt64Key("sim")*/ 0x73696D] = '\x223c',
                    [/*ToUInt64Key("cong")*/ 0x636F6E67] = '\x2245',
                    [/*ToUInt64Key("asymp")*/ 0x6173796D70] = '\x2248',
                    [/*ToUInt64Key("ne")*/ 0x6E65] = '\x2260',
                    [/*ToUInt64Key("equiv")*/ 0x6571756976] = '\x2261',
                    [/*ToUInt64Key("le")*/ 0x6C65] = '\x2264',
                    [/*ToUInt64Key("ge")*/ 0x6765] = '\x2265',
                    [/*ToUInt64Key("sub")*/ 0x737562] = '\x2282',
                    [/*ToUInt64Key("sup")*/ 0x737570] = '\x2283',
                    [/*ToUInt64Key("nsub")*/ 0x6E737562] = '\x2284',
                    [/*ToUInt64Key("sube")*/ 0x73756265] = '\x2286',
                    [/*ToUInt64Key("supe")*/ 0x73757065] = '\x2287',
                    [/*ToUInt64Key("oplus")*/ 0x6F706C7573] = '\x2295',
                    [/*ToUInt64Key("otimes")*/ 0x6F74696D6573] = '\x2297',
                    [/*ToUInt64Key("perp")*/ 0x70657270] = '\x22a5',
                    [/*ToUInt64Key("sdot")*/ 0x73646F74] = '\x22c5',
                    [/*ToUInt64Key("lceil")*/ 0x6C6365696C] = '\x2308',
                    [/*ToUInt64Key("rceil")*/ 0x726365696C] = '\x2309',
                    [/*ToUInt64Key("lfloor")*/ 0x6C666C6F6F72] = '\x230a',
                    [/*ToUInt64Key("rfloor")*/ 0x72666C6F6F72] = '\x230b',
                    [/*ToUInt64Key("lang")*/ 0x6C616E67] = '\x2329',
                    [/*ToUInt64Key("rang")*/ 0x72616E67] = '\x232a',
                    [/*ToUInt64Key("loz")*/ 0x6C6F7A] = '\x25ca',
                    [/*ToUInt64Key("spades")*/ 0x737061646573] = '\x2660',
                    [/*ToUInt64Key("clubs")*/ 0x636C756273] = '\x2663',
                    [/*ToUInt64Key("hearts")*/ 0x686561727473] = '\x2665',
                    [/*ToUInt64Key("diams")*/ 0x6469616D73] = '\x2666',
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
