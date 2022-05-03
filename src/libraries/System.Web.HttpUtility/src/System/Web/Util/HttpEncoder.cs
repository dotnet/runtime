// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private static void AppendCharAsUnicodeJavaScript(StringBuilder builder, char c)
        {
            builder.Append($"\\u{(int)c:x4}");
        }

        private static bool CharRequiresJavaScriptEncoding(char c) =>
            c < 0x20 // control chars always have to be encoded
                || c == '\"' // chars which must be encoded per JSON spec
                || c == '\\'
                || c == '\'' // HTML-sensitive chars encoded for safety
                || c == '<'
                || c == '>'
                || (c == '&')
                || c == '\u0085' // newline chars (see Unicode 6.2, Table 5-1 [http://www.unicode.org/versions/Unicode6.2.0/ch05.pdf]) have to be encoded
                || c == '\u2028'
                || c == '\u2029';

        [return: NotNullIfNotNull("value")]
        internal static string? HtmlAttributeEncode(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            // Don't create string writer if we don't have nothing to encode
            int pos = IndexOfHtmlAttributeEncodingChars(value, 0);
            if (pos == -1)
            {
                return value;
            }

            StringWriter writer = new StringWriter(CultureInfo.InvariantCulture);
            HtmlAttributeEncode(value, writer);
            return writer.ToString();
        }

        internal static void HtmlAttributeEncode(string? value, TextWriter output)
        {
            if (value == null)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(output);

            HtmlAttributeEncodeInternal(value, output);
        }

        private static void HtmlAttributeEncodeInternal(string s, TextWriter output)
        {
            int index = IndexOfHtmlAttributeEncodingChars(s, 0);
            if (index == -1)
            {
                output.Write(s);
            }
            else
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
        }

        [return: NotNullIfNotNull("value")]
        internal static string? HtmlDecode(string? value) => string.IsNullOrEmpty(value) ? value : WebUtility.HtmlDecode(value);

        internal static void HtmlDecode(string? value, TextWriter output)
        {
            ArgumentNullException.ThrowIfNull(output);

            output.Write(WebUtility.HtmlDecode(value));
        }

        [return: NotNullIfNotNull("value")]
        internal static string? HtmlEncode(string? value) => string.IsNullOrEmpty(value) ? value : WebUtility.HtmlEncode(value);

        internal static void HtmlEncode(string? value, TextWriter output)
        {
            ArgumentNullException.ThrowIfNull(output);

            output.Write(WebUtility.HtmlEncode(value));
        }

        private static int IndexOfHtmlAttributeEncodingChars(string s, int startPos)
        {
            Debug.Assert(0 <= startPos && startPos <= s.Length, "0 <= startPos && startPos <= s.Length");

            ReadOnlySpan<char> span = s.AsSpan(startPos);
            for (int i = 0; i < span.Length; i++)
            {
                char ch = span[i];
                if (ch <= '<')
                {
                    switch (ch)
                    {
                        case '<':
                        case '"':
                        case '\'':
                        case '&':
                            return startPos + i;
                    }
                }
            }

            return -1;
        }

        private static bool IsNonAsciiByte(byte b) => b >= 0x7F || b < 0x20;

        internal static string JavaScriptStringEncode(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            StringBuilder? b = null;
            int startIndex = 0;
            int count = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                // Append the unhandled characters (that do not require special treament)
                // to the string builder when special characters are detected.
                if (CharRequiresJavaScriptEncoding(c))
                {
                    if (b == null)
                    {
                        b = new StringBuilder(value.Length + 5);
                    }

                    if (count > 0)
                    {
                        b.Append(value, startIndex, count);
                    }

                    startIndex = i + 1;
                    count = 0;

                    switch (c)
                    {
                        case '\r':
                            b.Append("\\r");
                            break;
                        case '\t':
                            b.Append("\\t");
                            break;
                        case '\"':
                            b.Append("\\\"");
                            break;
                        case '\\':
                            b.Append("\\\\");
                            break;
                        case '\n':
                            b.Append("\\n");
                            break;
                        case '\b':
                            b.Append("\\b");
                            break;
                        case '\f':
                            b.Append("\\f");
                            break;
                        default:
                            AppendCharAsUnicodeJavaScript(b, c);
                            break;
                    }
                }
                else
                {
                    count++;
                }
            }

            if (b == null)
            {
                return value;
            }

            if (count > 0)
            {
                b.Append(value, startIndex, count);
            }

            return b.ToString();
        }

        [return: NotNullIfNotNull("bytes")]
        internal static byte[]? UrlDecode(byte[]? bytes, int offset, int count)
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
                    int h1 = HexConverter.FromChar(bytes[pos + 1]);
                    int h2 = HexConverter.FromChar(bytes[pos + 2]);

                    if ((h1 | h2) != 0xFF)
                    {
                        // valid 2 hex chars
                        b = (byte)((h1 << 4) | h2);
                        i += 2;
                    }
                }

                decodedBytes[decodedBytesCount++] = b;
            }

            if (decodedBytesCount < decodedBytes.Length)
            {
                byte[] newDecodedBytes = new byte[decodedBytesCount];
                Array.Copy(decodedBytes, newDecodedBytes, decodedBytesCount);
                decodedBytes = newDecodedBytes;
            }

            return decodedBytes;
        }

        [return: NotNullIfNotNull("bytes")]
        internal static string? UrlDecode(byte[]? bytes, int offset, int count, Encoding encoding)
        {
            if (!ValidateUrlEncodingParameters(bytes, offset, count))
            {
                return null;
            }

            UrlDecoder helper = new UrlDecoder(count, encoding);

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

        [return: NotNullIfNotNull("value")]
        internal static string? UrlDecode(string? value, Encoding encoding)
        {
            if (value == null)
            {
                return null;
            }

            int count = value.Length;
            UrlDecoder helper = new UrlDecoder(count, encoding);

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

        [return: NotNullIfNotNull("bytes")]
        internal static byte[]? UrlEncode(byte[]? bytes, int offset, int count, bool alwaysCreateNewReturnValue)
        {
            byte[]? encoded = UrlEncode(bytes, offset, count);

            return (alwaysCreateNewReturnValue && (encoded != null) && (encoded == bytes))
                ? (byte[])encoded.Clone()
                : encoded;
        }

        [return: NotNullIfNotNull("bytes")]
        private static byte[]? UrlEncode(byte[]? bytes, int offset, int count)
        {
            if (!ValidateUrlEncodingParameters(bytes, offset, count))
            {
                return null;
            }

            int cSpaces = 0;
            int cUnsafe = 0;

            // count them first
            for (int i = 0; i < count; i++)
            {
                char ch = (char)bytes[offset + i];

                if (ch == ' ')
                {
                    cSpaces++;
                }
                else if (!HttpEncoderUtility.IsUrlSafeChar(ch))
                {
                    cUnsafe++;
                }
            }

            // nothing to expand?
            if (cSpaces == 0 && cUnsafe == 0)
            {
                // DevDiv 912606: respect "offset" and "count"
                if (0 == offset && bytes.Length == count)
                {
                    return bytes;
                }
                else
                {
                    byte[] subarray = new byte[count];
                    Buffer.BlockCopy(bytes, offset, subarray, 0, count);
                    return subarray;
                }
            }

            // expand not 'safe' characters into %XX, spaces to +s
            byte[] expandedBytes = new byte[count + cUnsafe * 2];
            int pos = 0;

            for (int i = 0; i < count; i++)
            {
                byte b = bytes[offset + i];
                char ch = (char)b;

                if (HttpEncoderUtility.IsUrlSafeChar(ch))
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
                    expandedBytes[pos++] = (byte)HexConverter.ToCharLower(b >> 4);
                    expandedBytes[pos++] = (byte)HexConverter.ToCharLower(b);
                }
            }

            return expandedBytes;
        }

        //  Helper to encode the non-ASCII url characters only
        private static string UrlEncodeNonAscii(string str, Encoding e)
        {
            Debug.Assert(!string.IsNullOrEmpty(str));
            Debug.Assert(e != null);
            byte[] bytes = e.GetBytes(str);
            byte[] encodedBytes = UrlEncodeNonAscii(bytes, 0, bytes.Length);
            return Encoding.ASCII.GetString(encodedBytes);
        }

        private static byte[] UrlEncodeNonAscii(byte[] bytes, int offset, int count)
        {
            int cNonAscii = 0;

            // count them first
            for (int i = 0; i < count; i++)
            {
                if (IsNonAsciiByte(bytes[offset + i]))
                {
                    cNonAscii++;
                }
            }

            // nothing to expand?
            if (cNonAscii == 0)
            {
                return bytes;
            }

            // expand not 'safe' characters into %XX, spaces to +s
            byte[] expandedBytes = new byte[count + cNonAscii * 2];
            int pos = 0;

            for (int i = 0; i < count; i++)
            {
                byte b = bytes[offset + i];

                if (IsNonAsciiByte(b))
                {
                    expandedBytes[pos++] = (byte)'%';
                    expandedBytes[pos++] = (byte)HexConverter.ToCharLower(b >> 4);
                    expandedBytes[pos++] = (byte)HexConverter.ToCharLower(b);
                }
                else
                {
                    expandedBytes[pos++] = b;
                }
            }

            return expandedBytes;
        }

        [Obsolete("This method produces non-standards-compliant output and has interoperability issues. The preferred alternative is UrlEncode(*).")]
        [return: NotNullIfNotNull("value")]
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
                    if (HttpEncoderUtility.IsUrlSafeChar(ch))
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

        [return: NotNullIfNotNull("value")]
        internal static string? UrlPathEncode(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            string? schemeAndAuthority;
            string? path;
            string? queryAndFragment;

            if (!UriUtil.TrySplitUriForPathEncode(value, out schemeAndAuthority, out path, out queryAndFragment))
            {
                // If the value is not a valid url, we treat it as a relative url.
                // We don't need to extract query string from the url since UrlPathEncode()
                // does not encode query string.
                schemeAndAuthority = null;
                path = value;
                queryAndFragment = null;
            }

            return schemeAndAuthority + UrlPathEncodeImpl(path) + queryAndFragment;
        }

        // This is the original UrlPathEncode(string)
        private static string UrlPathEncodeImpl(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            // recurse in case there is a query string
            int i = value.IndexOf('?');
            if (i >= 0)
            {
                return string.Concat(UrlPathEncodeImpl(value.Substring(0, i)), value.AsSpan(i));
            }

            // encode DBCS characters and spaces only
            return HttpEncoderUtility.UrlEncodeSpaces(UrlEncodeNonAscii(value, Encoding.UTF8));
        }

        private static bool ValidateUrlEncodingParameters([NotNullWhen(true)] byte[]? bytes, int offset, int count)
        {
            if (bytes == null && count == 0)
            {
                return false;
            }

            ArgumentNullException.ThrowIfNull(bytes);
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

        // Internal class to facilitate URL decoding -- keeps char buffer and byte buffer, allows appending of either chars or bytes
        private sealed class UrlDecoder
        {
            private readonly int _bufferSize;

            // Accumulate characters in a special array
            private int _numChars;
            private readonly char[] _charBuffer;

            // Accumulate bytes for decoding into characters in a special array
            private int _numBytes;
            private byte[]? _byteBuffer;

            // Encoding to convert chars to bytes
            private readonly Encoding _encoding;

            private void FlushBytes()
            {
                if (_numBytes > 0)
                {
                    Debug.Assert(_byteBuffer != null);
                    _numChars += _encoding.GetChars(_byteBuffer, 0, _numBytes, _charBuffer, _numChars);
                    _numBytes = 0;
                }
            }

            internal UrlDecoder(int bufferSize, Encoding encoding)
            {
                _bufferSize = bufferSize;
                _encoding = encoding;

                _charBuffer = new char[bufferSize];
                // byte buffer created on demand
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
                    if (_byteBuffer == null)
                    {
                        _byteBuffer = new byte[_bufferSize];
                    }

                    _byteBuffer[_numBytes++] = b;
                }
            }

            internal string GetString()
            {
                if (_numBytes > 0)
                {
                    FlushBytes();
                }

                return _numChars > 0 ? new string(_charBuffer, 0, _numChars) : "";
            }
        }
    }
}
