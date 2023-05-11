// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Xml;

namespace System.Runtime.Serialization.Xml.Canonicalization.Tests
{
    internal static class C14nUtil
    {
        public const string NamespaceUrlForXmlPrefix = "http://www.w3.org/XML/1998/namespace";

        public static bool IsEmptyDefaultNamespaceDeclaration(string prefix, string value)
        {
            return prefix.Length == 0 && value.Length == 0;
        }

        public static bool IsXmlPrefixDeclaration(string prefix, string value)
        {
            return prefix == "xml" && value == NamespaceUrlForXmlPrefix;
        }

        public static string[] TokenizeInclusivePrefixList(string prefixList)
        {
            if (prefixList == null)
            {
                return null;
            }

            string[] prefixes = prefixList.Split(null);
            int count = 0;

            for (int i = 0; i < prefixes.Length; i++)
            {
                string prefix = prefixes[i];
                if (prefix == "#default")
                {
                    prefixes[count++] = string.Empty;
                }
                else if (prefix.Length > 0)
                {
                    prefixes[count++] = prefix;
                }
            }

            if (count == 0)
            {
                return null;
            }

            else if (count == prefixes.Length)
            {
                return prefixes;
            }

            else
            {
                string[] result = new string[count];
                Array.Copy(prefixes, result, count);
                return result;
            }
        }
    }

    internal sealed class CanonicalEncoder
    {
        private const char Char10 = (char)10;
        private const char Char13 = (char)13;
        private const char Char9 = (char)9;
        private const char Char32 = (char)32;
        private const string Char13String = "\r";
        private const string Char13EntityReference = "&#xD;";

        private const int charBufSize = 252;
        private const int byteBufSize = charBufSize * 4 + 4;

        internal static readonly UTF8Encoding Utf8WithoutPreamble = new UTF8Encoding(false);

        private readonly Encoder _encoder;
        private readonly byte[] _byteBuf;
        private readonly char[] _charBuf;
        private int _count;

        private Stream _stream;
        private bool _needsReset;

        public CanonicalEncoder(Stream stream)
        {
            _encoder = Utf8WithoutPreamble.GetEncoder();
            _charBuf = new char[charBufSize];
            _byteBuf = new byte[byteBufSize];
            SetOutput(stream);
        }

        public void Encode(char c)
        {
            if (_count >= charBufSize)
            {
                WriteBuffer(false);
            }

            _charBuf[_count++] = c;
        }

        public void Encode(string str)
        {
            unsafe
            {
                fixed (char* start = str)
                {
                    char* end = start + str.Length;
                    for (char* p = start; p < end; p++)
                    {
                        Encode(*p);
                    }
                }
            }
        }

        public void Encode(char[] buffer, int offset, int count)
        {
            ValidateBufferBounds(buffer, offset, count);

            unsafe
            {
                fixed (char* bufferStart = buffer)
                {
                    char* start = bufferStart + offset;
                    char* end = bufferStart + (offset + count);
                    for (char* p = start; p < end; p++)
                    {
                        Encode(*p);
                    }
                }
            }
        }

        public void EncodeAttribute(string prefix, string localName, string value)
        {
            Encode(' ');
            if (string.IsNullOrEmpty(prefix))
            {
                Encode(localName);
            }
            else
            {
                Encode(prefix);
                Encode(':');
                Encode(localName);
            }
            Encode("=\"");
            EncodeWithTranslation(value, CanonicalEncoder.XmlStringType.AttributeValue);
            Encode('\"');
        }

        public void EncodeComment(string value, XmlDocumentPosition docPosition)
        {
            if (docPosition == XmlDocumentPosition.AfterRootElement)
            {
                Encode(Char10);
            }

            Encode("<!--");
            EncodeWithLineBreakNormalization(value);
            Encode("-->");

            if (docPosition == XmlDocumentPosition.BeforeRootElement)
            {
                Encode(Char10);
            }
        }

        public void EncodeEndElement(string prefix, string localName)
        {
            Encode("</");
            if (prefix.Length != 0)
            {
                Encode(prefix);
                Encode(':');
            }

            Encode(localName);
            Encode('>');
        }

        public void EncodeStartElementOpen(string prefix, string localName)
        {
            Encode("<");
            if (prefix.Length != 0)
            {
                Encode(prefix);
                Encode(':');
            }

            Encode(localName);
        }

        public void EncodeStartElementClose()
        {
            Encode('>');
        }

        public void EncodeWithLineBreakNormalization(string str)
        {
            unsafe
            {
                fixed (char* start = str)
                {
                    char* end = start + str.Length;
                    for (char* p = start; p < end; p++)
                    {
                        if (*p == Char13)
                        {
                            Encode(Char13EntityReference);
                        }
                        else
                        {
                            Encode(*p);
                        }
                    }
                }
            }
        }

        public void EncodeWithTranslation(char[] buffer, int offset, int count, XmlStringType valueType)
        {
            ValidateBufferBounds(buffer, offset, count);

            unsafe
            {
                fixed (char* bufferStart = buffer)
                {
                    char* start = bufferStart + offset;
                    char* end = bufferStart + (offset + count);
                    EncodeWithTranslation(start, end, valueType);
                }
            }
        }

        public void EncodeWithTranslation(string str, XmlStringType valueType)
        {
            unsafe
            {
                fixed (char* start = str)
                {
                    char* end = start + str.Length;
                    EncodeWithTranslation(start, end, valueType);
                }
            }
        }

        private unsafe void EncodeWithTranslation(char* start, char* end, XmlStringType valueType)
        {
            for (char* p = start; p < end; p++)
            {
                switch (*p)
                {
                    case '&':
                        Encode("&amp;");
                        break;
                    case '<':
                        Encode("&lt;");
                        break;
                    case '>':
                        if (valueType != XmlStringType.AttributeValue)
                        {
                            Encode("&gt;");
                        }
                        else
                        {
                            Encode(*p);
                        }
                        break;
                    case Char13:
                        Encode(Char13EntityReference);
                        break;
                    case Char10:
                        if (valueType == XmlStringType.AttributeValue)
                        {
                            Encode("&#xA;");
                        }
                        else
                        {
                            Encode(*p);
                        }
                        break;
                    case Char9:
                        if (valueType == XmlStringType.AttributeValue)
                        {
                            Encode("&#x9;");
                        }
                        else
                        {
                            Encode(*p);
                        }
                        break;
                    case '\"':
                        if (valueType == XmlStringType.AttributeValue)
                        {
                            Encode("&quot;");
                        }
                        else
                        {
                            Encode(*p);
                        }
                        break;
                    default:
                        Encode(*p);
                        break;
                }
            }
        }

        public void Flush()
        {
            WriteBuffer(true);
        }

        public void Reset()
        {
            Flush();

            if (_needsReset)
            {
                _encoder.Reset();
                _needsReset = false;
            }
        }

        public void SetOutput(Stream stream)
        {
            Reset();
            _stream = stream;
        }

        private void WriteBuffer(bool flush)
        {
            if (_count > 0)
            {
                int numBytesAdded = _encoder.GetBytes(_charBuf, 0, _count, _byteBuf, 0, flush);
                WriteBufferCore(_byteBuf, 0, numBytesAdded, flush);
                _count = 0;
            }
        }

        private void WriteBufferCore(byte[] buffer, int offset, int count, bool flush)
        {
            _needsReset = true;
            _stream.Write(buffer, offset, count);
            if (flush)
            {
                _stream.Flush();
            }
        }

        public static void WriteEscapedChars(XmlWriter writer, char[] buffer, int index, int count)
        {
            int unescapedSegmentStart = index;
            int bound = index + count;
            for (int i = index; i < bound; i++)
            {
                if (buffer[i] == Char13)
                {
                    if (unescapedSegmentStart < i)
                    {
                        writer.WriteChars(buffer, unescapedSegmentStart, i - unescapedSegmentStart);
                    }

                    writer.WriteCharEntity(Char13);
                    unescapedSegmentStart = i + 1;
                }
            }

            if (unescapedSegmentStart < bound)
            {
                writer.WriteChars(buffer, unescapedSegmentStart, bound - unescapedSegmentStart);
            }
        }

        public static void WriteEscapedString(XmlWriter writer, string s)
        {
            int unescapedSegmentStart = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == Char13)
                {
                    if (unescapedSegmentStart < i)
                    {
                        writer.WriteString(s.Substring(unescapedSegmentStart, i - unescapedSegmentStart));
                    }

                    writer.WriteCharEntity(Char13);
                    unescapedSegmentStart = i + 1;
                }
            }

            if (unescapedSegmentStart < s.Length)
            {
                writer.WriteString(s.Substring(unescapedSegmentStart, s.Length - unescapedSegmentStart));
            }
        }

        public static void ValidateBufferBounds(Array buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (count < 0 || count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            if (offset < 0 || offset > buffer.Length - count)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
        }

        public enum XmlStringType
        {
            AttributeValue,
            CDataContent,
            TextContent,
        }
    }
}
