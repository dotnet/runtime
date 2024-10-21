// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

namespace System.Web.Util
{
    internal static class HttpEncoder
    {
        private const int MaxStackAllocUrlLength = 256;
        private const int StackallocThreshold = 512;

        // Set of safe chars, from RFC 1738.4 minus '+'
        private static readonly SearchValues<byte> s_urlSafeBytes = SearchValues.Create(
            "!()*-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz"u8);

        private static readonly SearchValues<char> s_invalidJavaScriptChars = SearchValues.Create(
            // Any Control, < 32 (' ')
            "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u0009\u000A\u000B\u000C\u000D\u000E\u000F\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001A\u001B\u001C\u001D\u001E\u001F" +
            // Chars which must be encoded per JSON spec / HTML-sensitive chars encoded for safety
            "\"&'<>\\" +
            // newline chars (see Unicode 6.2, Table 5-1 [http://www.unicode.org/versions/Unicode6.2.0/ch05.pdf]) have to be encoded
            "\u0085\u2028\u2029");

        [return: NotNullIfNotNull(nameof(value))]
        internal static string? HtmlAttributeEncode(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            // Don't create string writer if we don't have nothing to encode
            int pos = IndexOfHtmlAttributeEncodingChars(value);
            if (pos < 0)
            {
                return value;
            }

            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
            HtmlAttributeEncodeInternal(value, pos, writer);
            return writer.ToString();
        }

        internal static void HtmlAttributeEncode(string? value, TextWriter output)
        {
            if (value == null)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(output);

            int pos = IndexOfHtmlAttributeEncodingChars(value);
            if (pos < 0)
            {
                output.Write(value);
                return;
            }

            HtmlAttributeEncodeInternal(value, pos, output);
        }

        private static void HtmlAttributeEncodeInternal(string s, int index, TextWriter output)
        {
            output.Write(s.AsSpan(0, index));

            ReadOnlySpan<char> remaining = s.AsSpan(index);
            for (int i = 0; i < remaining.Length; i++)
            {
                char ch = remaining[i];
                if (ch <= '<')
                {
                    switch (ch)
                    {
                        case '<':
                            output.Write("&lt;");
                            break;
                        case '"':
                            output.Write("&quot;");
                            break;
                        case '\'':
                            output.Write("&#39;");
                            break;
                        case '&':
                            output.Write("&amp;");
                            break;
                        default:
                            output.Write(ch);
                            break;
                    }
                }
                else
                {
                    output.Write(ch);
                }
            }
        }

        [return: NotNullIfNotNull(nameof(value))]
        internal static string? HtmlDecode(string? value) => string.IsNullOrEmpty(value) ? value : WebUtility.HtmlDecode(value);

        internal static void HtmlDecode(string? value, TextWriter output)
        {
            ArgumentNullException.ThrowIfNull(output);

            output.Write(WebUtility.HtmlDecode(value));
        }

        [return: NotNullIfNotNull(nameof(value))]
        internal static string? HtmlEncode(string? value) => string.IsNullOrEmpty(value) ? value : WebUtility.HtmlEncode(value);

        internal static void HtmlEncode(string? value, TextWriter output)
        {
            ArgumentNullException.ThrowIfNull(output);

            output.Write(WebUtility.HtmlEncode(value));
        }

        private static int IndexOfHtmlAttributeEncodingChars(string s) =>
            s.AsSpan().IndexOfAny("<\"'&");

        internal static string JavaScriptStringEncode(string? value, bool addDoubleQuotes)
        {
            int i = value.AsSpan().IndexOfAny(s_invalidJavaScriptChars);
            if (i < 0)
            {
                return addDoubleQuotes ? $"\"{value}\"" : value ?? string.Empty;
            }

            return EncodeCore(value, i, addDoubleQuotes);

            static string EncodeCore(ReadOnlySpan<char> value, int i, bool addDoubleQuotes)
            {
                var vsb = new ValueStringBuilder(stackalloc char[StackallocThreshold]);
                if (addDoubleQuotes)
                {
                    vsb.Append('"');
                }

                ReadOnlySpan<char> chars = value;
                do
                {
                    vsb.Append(chars.Slice(0, i));
                    char c = chars[i];
                    chars = chars.Slice(i + 1);
                    switch (c)
                    {
                        case '\r':
                            vsb.Append("\\r");
                            break;
                        case '\t':
                            vsb.Append("\\t");
                            break;
                        case '\"':
                            vsb.Append("\\\"");
                            break;
                        case '\\':
                            vsb.Append("\\\\");
                            break;
                        case '\n':
                            vsb.Append("\\n");
                            break;
                        case '\b':
                            vsb.Append("\\b");
                            break;
                        case '\f':
                            vsb.Append("\\f");
                            break;
                        default:
                            vsb.Append("\\u");
                            vsb.AppendSpanFormattable((int)c, "x4");
                            break;
                    }

                    i = chars.IndexOfAny(s_invalidJavaScriptChars);
                } while (i >= 0);

                vsb.Append(chars);

                if (addDoubleQuotes)
                {
                    vsb.Append('"');
                }

                return vsb.ToString();
            }
        }

        [return: NotNullIfNotNull(nameof(bytes))]
        internal static byte[]? UrlDecode(byte[]? bytes, int offset, int count)
        {
            if (!ValidateUrlEncodingParameters(bytes, offset, count))
            {
                return null;
            }

            return UrlDecode(bytes.AsSpan(offset, count));
        }

        internal static byte[] UrlDecode(ReadOnlySpan<byte> bytes)
        {
            int decodedBytesCount = 0;
            int count = bytes.Length;
            Span<byte> decodedBytes = count <= StackallocThreshold ? stackalloc byte[StackallocThreshold] : new byte[count];

            for (int i = 0; i < count; i++)
            {
                byte b = bytes[i];

                if (b == '+')
                {
                    b = (byte)' ';
                }
                else if (b == '%' && i < count - 2)
                {
                    int h1 = HexConverter.FromChar(bytes[i + 1]);
                    int h2 = HexConverter.FromChar(bytes[i + 2]);

                    if ((h1 | h2) != 0xFF)
                    {
                        // valid 2 hex chars
                        b = (byte)((h1 << 4) | h2);
                        i += 2;
                    }
                }

                decodedBytes[decodedBytesCount++] = b;
            }

            return decodedBytes.Slice(0, decodedBytesCount).ToArray();
        }

        [return: NotNullIfNotNull(nameof(bytes))]
        internal static string? UrlDecode(byte[]? bytes, int offset, int count, Encoding encoding)
        {
            if (!ValidateUrlEncodingParameters(bytes, offset, count))
            {
                return null;
            }

            UrlDecoder helper = count <= MaxStackAllocUrlLength
                ? new UrlDecoder(stackalloc char[MaxStackAllocUrlLength], stackalloc byte[MaxStackAllocUrlLength], encoding)
                : new UrlDecoder(new char[count], new byte[count], encoding);

            // go through the bytes collapsing %XX and %uXXXX and appending
            // each byte as byte, with exception of %uXXXX constructs that
            // are appended as chars

            for (int i = 0; i < count; i++)
            {
                int pos = offset + i;
                byte b = bytes[pos];

                // The code assumes that + and % cannot be in multibyte sequence

                if (b == '+')
                {
                    b = (byte)' ';
                }
                else if (b == '%' && i < count - 2)
                {
                    if (bytes[pos + 1] == 'u' && i < count - 5)
                    {
                        int h1 = HexConverter.FromChar(bytes[pos + 2]);
                        int h2 = HexConverter.FromChar(bytes[pos + 3]);
                        int h3 = HexConverter.FromChar(bytes[pos + 4]);
                        int h4 = HexConverter.FromChar(bytes[pos + 5]);

                        if ((h1 | h2 | h3 | h4) != 0xFF)
                        {   // valid 4 hex chars
                            char ch = (char)((h1 << 12) | (h2 << 8) | (h3 << 4) | h4);
                            i += 5;

                            // don't add as byte
                            helper.AddChar(ch);
                            continue;
                        }
                    }
                    else
                    {
                        int h1 = HexConverter.FromChar(bytes[pos + 1]);
                        int h2 = HexConverter.FromChar(bytes[pos + 2]);

                        if ((h1 | h2) != 0xFF)
                        {     // valid 2 hex chars
                            b = (byte)((h1 << 4) | h2);
                            i += 2;
                        }
                    }
                }

                helper.AddByte(b);
            }

            return Utf16StringValidator.ValidateString(helper.GetString());
        }

        [return: NotNullIfNotNull(nameof(value))]
        internal static string? UrlDecode(string? value, Encoding encoding)
        {
            if (value == null)
            {
                return null;
            }

            return UrlDecode(value.AsSpan(), encoding);
        }

        internal static string UrlDecode(ReadOnlySpan<char> value, Encoding encoding)
        {
            if (value.IsEmpty)
            {
                return string.Empty;
            }

            int count = value.Length;
            UrlDecoder helper = count <= MaxStackAllocUrlLength
                ? new UrlDecoder(stackalloc char[MaxStackAllocUrlLength], stackalloc byte[MaxStackAllocUrlLength], encoding)
                : new UrlDecoder(new char[count], new byte[count], encoding);

            // go through the string's chars collapsing %XX and %uXXXX and
            // appending each char as char, with exception of %XX constructs
            // that are appended as bytes

            for (int pos = 0; pos < count; pos++)
            {
                char ch = value[pos];

                if (ch == '+')
                {
                    ch = ' ';
                }
                else if (ch == '%' && pos < count - 2)
                {
                    if (value[pos + 1] == 'u' && pos < count - 5)
                    {
                        int h1 = HexConverter.FromChar(value[pos + 2]);
                        int h2 = HexConverter.FromChar(value[pos + 3]);
                        int h3 = HexConverter.FromChar(value[pos + 4]);
                        int h4 = HexConverter.FromChar(value[pos + 5]);

                        if ((h1 | h2 | h3 | h4) != 0xFF)
                        {   // valid 4 hex chars
                            ch = (char)((h1 << 12) | (h2 << 8) | (h3 << 4) | h4);
                            pos += 5;

                            // only add as char
                            helper.AddChar(ch);
                            continue;
                        }
                    }
                    else
                    {
                        int h1 = HexConverter.FromChar(value[pos + 1]);
                        int h2 = HexConverter.FromChar(value[pos + 2]);

                        if ((h1 | h2) != 0xFF)
                        {     // valid 2 hex chars
                            byte b = (byte)((h1 << 4) | h2);
                            pos += 2;

                            // don't add as char
                            helper.AddByte(b);
                            continue;
                        }
                    }
                }

                if ((ch & 0xFF80) == 0)
                {
                    helper.AddByte((byte)ch); // 7 bit have to go as bytes because of Unicode
                }
                else
                {
                    helper.AddChar(ch);
                }
            }

            return Utf16StringValidator.ValidateString(helper.GetString());
        }

        [return: NotNullIfNotNull(nameof(bytes))]
        internal static byte[]? UrlEncode(byte[]? bytes, int offset, int count)
        {
            if (!ValidateUrlEncodingParameters(bytes, offset, count))
            {
                return null;
            }

            return UrlEncode(bytes.AsSpan(offset, count));
        }

        private static byte[] UrlEncode(ReadOnlySpan<byte> bytes)
        {
            // nothing to expand?
            if (!NeedsEncoding(bytes, out int cUnsafe))
            {
                return bytes.ToArray();
            }

            return UrlEncode(bytes, cUnsafe);
        }

        private static byte[] UrlEncode(ReadOnlySpan<byte> bytes, int cUnsafe)
        {
            // expand not 'safe' characters into %XX, spaces to +s
            byte[] expandedBytes = new byte[bytes.Length + cUnsafe * 2];
            int pos = 0;

            foreach (byte b in bytes)
            {
                if (s_urlSafeBytes.Contains(b))
                {
                    expandedBytes[pos++] = b;
                }
                else if (b == ' ')
                {
                    expandedBytes[pos++] = (byte)'+';
                }
                else
                {
                    expandedBytes[pos++] = (byte)'%';
                    expandedBytes[pos++] = (byte)HexConverter.ToCharLower(b >> 4);
                    expandedBytes[pos++] = (byte)HexConverter.ToCharLower(b);
                }
            }

            return expandedBytes;
        }

        private static bool NeedsEncoding(ReadOnlySpan<byte> bytes, out int cUnsafe)
        {
            cUnsafe = 0;

            int i = bytes.IndexOfAnyExcept(s_urlSafeBytes);
            if (i < 0)
            {
                return false;
            }

            foreach (byte b in bytes.Slice(i))
            {
                if (!s_urlSafeBytes.Contains(b) && b != ' ')
                {
                    cUnsafe++;
                }
            }

            return true;
        }

        internal static byte[] UrlEncode(string str, Encoding e)
        {
            if (e.GetMaxByteCount(str.Length) <= StackallocThreshold)
            {
                Span<byte> byteSpan = stackalloc byte[StackallocThreshold];
                int encodedBytes = e.GetBytes(str, byteSpan);

                return UrlEncode(byteSpan.Slice(0, encodedBytes));
            }

            byte[] bytes = e.GetBytes(str);
            return NeedsEncoding(bytes, out int cUnsafe)
                ? UrlEncode(bytes, cUnsafe)
                : bytes;
        }

        [Obsolete("This method produces non-standards-compliant output and has interoperability issues. The preferred alternative is UrlEncode(*).")]
        [return: NotNullIfNotNull(nameof(value))]
        internal static string? UrlEncodeUnicode(string? value)
        {
            if (value == null)
            {
                return null;
            }

            int l = value.Length;
            StringBuilder sb = new StringBuilder(l);

            for (int i = 0; i < l; i++)
            {
                char ch = value[i];

                if ((ch & 0xff80) == 0)
                {  // 7 bit?
                    if (s_urlSafeBytes.Contains((byte)ch))
                    {
                        sb.Append(ch);
                    }
                    else if (ch == ' ')
                    {
                        sb.Append('+');
                    }
                    else
                    {
                        sb.Append('%');
                        sb.Append(HexConverter.ToCharLower(ch >> 4));
                        sb.Append(HexConverter.ToCharLower(ch));
                    }
                }
                else
                { // arbitrary Unicode?
                    sb.Append("%u");
                    sb.Append(HexConverter.ToCharLower(ch >> 12));
                    sb.Append(HexConverter.ToCharLower(ch >> 8));
                    sb.Append(HexConverter.ToCharLower(ch >> 4));
                    sb.Append(HexConverter.ToCharLower(ch));
                }
            }

            return sb.ToString();
        }

        [return: NotNullIfNotNull(nameof(value))]
        internal static string? UrlPathEncode(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            ReadOnlySpan<char> schemeAndAuthority;
            string? path;
            ReadOnlySpan<char> queryAndFragment;

            if (!UriUtil.TrySplitUriForPathEncode(value, out schemeAndAuthority, out path, out queryAndFragment))
            {
                // If the value is not a valid url, we treat it as a relative url.
                // We don't need to extract query string from the url since UrlPathEncode()
                // does not encode query string.
                return UrlPathEncodeImpl(value);
            }

            return string.Concat(schemeAndAuthority, UrlPathEncodeImpl(path), queryAndFragment);
        }

        // This is the original UrlPathEncode(string)
        private static string UrlPathEncodeImpl(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            int i = value.AsSpan().IndexOfAnyExceptInRange((char)0x21, (char)0x7F);
            if (i < 0)
            {
                return value;
            }

            int indexOfQuery = value.IndexOf('?');
            if ((uint)indexOfQuery < (uint)i)
            {
                // Everything before the Query is valid ASCII
                return value;
            }

            ReadOnlySpan<char> toEncode = indexOfQuery >= 0
                ? value.AsSpan(i, indexOfQuery - i)
                : value.AsSpan(i);

            byte[] bytes = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(toEncode.Length));
            int utf8Length = Encoding.UTF8.GetBytes(toEncode, bytes);
            char[] chars = ArrayPool<char>.Shared.Rent(utf8Length * 3);
            int charCount = 0;
            foreach (byte b in bytes.AsSpan(0, utf8Length))
            {
                if (!char.IsBetween((char)b, (char)0x21, (char)0x7F))
                {
                    chars[charCount++] = '%';
                    chars[charCount++] = HexConverter.ToCharLower(b >> 4);
                    chars[charCount++] = HexConverter.ToCharLower(b);
                }
                else
                {
                    chars[charCount++] = (char)b;
                }
            }

            ArrayPool<byte>.Shared.Return(bytes);
            string result = string.Concat(
                value.AsSpan(0, i),
                chars.AsSpan(0, charCount),
                indexOfQuery >= 0 ? value.AsSpan(indexOfQuery) : ReadOnlySpan<char>.Empty);
            ArrayPool<char>.Shared.Return(chars);
            return result;
        }

        private static bool ValidateUrlEncodingParameters([NotNullWhen(true)] byte[]? bytes, int offset, int count)
        {
            if (bytes == null && count == 0)
            {
                return false;
            }

            ArgumentNullException.ThrowIfNull(bytes);

            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, bytes.Length);

            ArgumentOutOfRangeException.ThrowIfNegative(count);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, bytes.Length - offset);

            return true;
        }

        // Internal class to facilitate URL decoding -- keeps char buffer and byte buffer, allows appending of either chars or bytes
        private ref struct UrlDecoder
        {
            // Accumulate characters in a special array
            private int _numChars;
            private readonly Span<char> _charBuffer;

            // Accumulate bytes for decoding into characters in a special array
            private int _numBytes;
            private readonly Span<byte> _byteBuffer;

            // Encoding to convert chars to bytes
            private readonly Encoding _encoding;

            private void FlushBytes()
            {
                if (_numBytes > 0)
                {
                    Debug.Assert(!_byteBuffer.IsEmpty);
                    _numChars += _encoding.GetChars(_byteBuffer.Slice(0, _numBytes), _charBuffer.Slice(_numChars));
                    _numBytes = 0;
                }
            }

            internal UrlDecoder(Span<char> charBuffer, Span<byte> byteBuffer, Encoding encoding)
            {
                _charBuffer = charBuffer;
                _byteBuffer = byteBuffer;
                _encoding = encoding;
            }

            internal void AddChar(char ch)
            {
                if (_numBytes > 0)
                {
                    FlushBytes();
                }

                _charBuffer[_numChars++] = ch;
            }

            internal void AddByte(byte b)
            {
                // if there are no pending bytes treat 7 bit bytes as characters
                // this optimization is temp disable as it doesn't work for some encodings
                /*
                                if (_numBytes == 0 && ((b & 0x80) == 0)) {
                                    AddChar((char)b);
                                }
                                else
                */
                {
                    _byteBuffer[_numBytes++] = b;
                }
            }

            internal string GetString()
            {
                if (_numBytes > 0)
                {
                    FlushBytes();
                }

                return _charBuffer.Slice(0, _numChars).ToString();
            }
        }
    }
}
