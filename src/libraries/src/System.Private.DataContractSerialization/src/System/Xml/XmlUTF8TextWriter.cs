// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Runtime;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace System.Xml
{
    internal interface IXmlTextWriterInitializer
    {
        void SetOutput(Stream stream, Encoding encoding, bool ownsStream);
    }

    internal class XmlUTF8TextWriter : XmlBaseWriter, IXmlTextWriterInitializer
    {
        private XmlUTF8NodeWriter _writer;

        public void SetOutput(Stream stream, Encoding encoding, bool ownsStream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (encoding.WebName != Encoding.UTF8.WebName)
            {
                stream = new EncodingStreamWrapper(stream, encoding, true);
            }

            if (_writer == null)
            {
                _writer = new XmlUTF8NodeWriter();
            }
            _writer.SetOutput(stream, ownsStream, encoding);
            SetOutput(_writer);
        }
    }

    internal class XmlUTF8NodeWriter : XmlStreamNodeWriter
    {
        private byte[] _entityChars;
        private bool[] _isEscapedAttributeChar;
        private bool[] _isEscapedElementChar;
        private bool _inAttribute;
        private const int bufferLength = 512;
        private const int maxEntityLength = 32;
        private const int maxBytesPerChar = 3;
        private Encoding _encoding;
        private char[] _chars;

        private static readonly byte[] s_startDecl =
        {
            (byte)'<', (byte)'?', (byte)'x', (byte)'m', (byte)'l', (byte)' ',
            (byte)'v', (byte)'e', (byte)'r', (byte)'s', (byte)'i', (byte)'o', (byte)'n', (byte)'=', (byte)'"', (byte)'1', (byte)'.', (byte)'0', (byte)'"', (byte)' ',
            (byte)'e', (byte)'n', (byte)'c', (byte)'o', (byte)'d', (byte)'i', (byte)'n', (byte)'g', (byte)'=', (byte)'"',
        };
        private static readonly byte[] s_endDecl =
        {
            (byte)'"', (byte)'?', (byte)'>'
        };
        private static readonly byte[] s_utf8Decl =
        {
            (byte)'<', (byte)'?', (byte)'x', (byte)'m', (byte)'l', (byte)' ',
            (byte)'v', (byte)'e', (byte)'r', (byte)'s', (byte)'i', (byte)'o', (byte)'n', (byte)'=', (byte)'"', (byte)'1', (byte)'.', (byte)'0', (byte)'"', (byte)' ',
            (byte)'e', (byte)'n', (byte)'c', (byte)'o', (byte)'d', (byte)'i', (byte)'n', (byte)'g', (byte)'=', (byte)'"', (byte)'u', (byte)'t', (byte)'f', (byte)'-', (byte)'8', (byte)'"',
            (byte)'?', (byte)'>'
        };
        private static readonly byte[] s_digits =
        {
            (byte) '0', (byte) '1', (byte) '2', (byte) '3', (byte) '4', (byte) '5', (byte) '6', (byte) '7',
            (byte) '8', (byte) '9', (byte) 'A', (byte) 'B', (byte) 'C', (byte) 'D', (byte) 'E', (byte) 'F'
        };
        private static readonly bool[] s_defaultIsEscapedAttributeChar = new bool[]
        {
            true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true,
            true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true,
            false, false, true, false, false, false, true, false, false, false, false, false, false, false, false, false, // '"', '&'
            false, false, false, false, false, false, false, false, false, false, false, false, true, false, true, false  // '<', '>'
        };
        private static readonly bool[] s_defaultIsEscapedElementChar = new bool[]
        {
            true, true, true, true, true, true, true, true, true, false, false, true, true, true, true, true, // All but 0x09, 0x0A
            true, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true,
            false, false, false, false, false, false, true, false, false, false, false, false, false, false, false, false, // '&'
            false, false, false, false, false, false, false, false, false, false, false, false, true, false, true, false  // '<', '>'
        };

        public XmlUTF8NodeWriter()
            : this(s_defaultIsEscapedAttributeChar, s_defaultIsEscapedElementChar)
        {
        }

        public XmlUTF8NodeWriter(bool[] isEscapedAttributeChar, bool[] isEscapedElementChar)
        {
            _isEscapedAttributeChar = isEscapedAttributeChar;
            _isEscapedElementChar = isEscapedElementChar;
            _inAttribute = false;
        }

        new public void SetOutput(Stream stream, bool ownsStream, Encoding encoding)
        {
            Encoding utf8Encoding = null;
            if (encoding != null && encoding == Encoding.UTF8)
            {
                utf8Encoding = encoding;
                encoding = null;
            }
            base.SetOutput(stream, ownsStream, utf8Encoding);
            _encoding = encoding;
            _inAttribute = false;
        }

        public Encoding Encoding
        {
            get
            {
                return _encoding;
            }
        }

        private byte[] GetCharEntityBuffer()
        {
            if (_entityChars == null)
            {
                _entityChars = new byte[maxEntityLength];
            }
            return _entityChars;
        }

        private char[] GetCharBuffer(int charCount)
        {
            if (charCount >= 256)
                return new char[charCount];
            if (_chars == null || _chars.Length < charCount)
                _chars = new char[charCount];
            return _chars;
        }

        public override void WriteDeclaration()
        {
            if (_encoding == null)
            {
                WriteUTF8Chars(s_utf8Decl, 0, s_utf8Decl.Length);
            }
            else
            {
                WriteUTF8Chars(s_startDecl, 0, s_startDecl.Length);
                if (_encoding.WebName == Encoding.BigEndianUnicode.WebName)
                    WriteUTF8Chars("utf-16BE");
                else
                    WriteUTF8Chars(_encoding.WebName);
                WriteUTF8Chars(s_endDecl, 0, s_endDecl.Length);
            }
        }

        public override void WriteCData(string text)
        {
            byte[] buffer;
            int offset;

            buffer = GetBuffer(9, out offset);
            buffer[offset + 0] = (byte)'<';
            buffer[offset + 1] = (byte)'!';
            buffer[offset + 2] = (byte)'[';
            buffer[offset + 3] = (byte)'C';
            buffer[offset + 4] = (byte)'D';
            buffer[offset + 5] = (byte)'A';
            buffer[offset + 6] = (byte)'T';
            buffer[offset + 7] = (byte)'A';
            buffer[offset + 8] = (byte)'[';
            Advance(9);

            WriteUTF8Chars(text);

            buffer = GetBuffer(3, out offset);
            buffer[offset + 0] = (byte)']';
            buffer[offset + 1] = (byte)']';
            buffer[offset + 2] = (byte)'>';
            Advance(3);
        }

        private void WriteStartComment()
        {
            int offset;
            byte[] buffer = GetBuffer(4, out offset);
            buffer[offset + 0] = (byte)'<';
            buffer[offset + 1] = (byte)'!';
            buffer[offset + 2] = (byte)'-';
            buffer[offset + 3] = (byte)'-';
            Advance(4);
        }

        private void WriteEndComment()
        {
            int offset;
            byte[] buffer = GetBuffer(3, out offset);
            buffer[offset + 0] = (byte)'-';
            buffer[offset + 1] = (byte)'-';
            buffer[offset + 2] = (byte)'>';
            Advance(3);
        }

        public override void WriteComment(string text)
        {
            WriteStartComment();
            WriteUTF8Chars(text);
            WriteEndComment();
        }

        public override void WriteStartElement(string prefix, string localName)
        {
            WriteByte('<');
            if (prefix.Length != 0)
            {
                WritePrefix(prefix);
                WriteByte(':');
            }
            WriteLocalName(localName);
        }

        public override async Task WriteStartElementAsync(string prefix, string localName)
        {
            await WriteByteAsync('<').ConfigureAwait(false);
            if (prefix.Length != 0)
            {
                // This method calls into unsafe method which cannot run asyncly.
                WritePrefix(prefix);
                await WriteByteAsync(':').ConfigureAwait(false);
            }

            // This method calls into unsafe method which cannot run asyncly.
            WriteLocalName(localName);
        }

        public override void WriteStartElement(string prefix, XmlDictionaryString localName)
        {
            WriteStartElement(prefix, localName.Value);
        }

        public override void WriteStartElement(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] localNameBuffer, int localNameOffset, int localNameLength)
        {
            WriteByte('<');
            if (prefixLength != 0)
            {
                WritePrefix(prefixBuffer, prefixOffset, prefixLength);
                WriteByte(':');
            }
            WriteLocalName(localNameBuffer, localNameOffset, localNameLength);
        }

        public override void WriteEndStartElement(bool isEmpty)
        {
            if (!isEmpty)
            {
                WriteByte('>');
            }
            else
            {
                WriteBytes('/', '>');
            }
        }

        public override async Task WriteEndStartElementAsync(bool isEmpty)
        {
            if (!isEmpty)
            {
                await WriteByteAsync('>').ConfigureAwait(false);
            }
            else
            {
                await WriteBytesAsync('/', '>').ConfigureAwait(false);
            }
        }

        public override void WriteEndElement(string prefix, string localName)
        {
            WriteBytes('<', '/');
            if (prefix.Length != 0)
            {
                WritePrefix(prefix);
                WriteByte(':');
            }
            WriteLocalName(localName);
            WriteByte('>');
        }

        public override async Task WriteEndElementAsync(string prefix, string localName)
        {
            await WriteBytesAsync('<', '/').ConfigureAwait(false);
            if (prefix.Length != 0)
            {
                WritePrefix(prefix);
                await WriteByteAsync(':').ConfigureAwait(false);
            }
            WriteLocalName(localName);
            await WriteByteAsync('>').ConfigureAwait(false);
        }

        public override void WriteEndElement(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] localNameBuffer, int localNameOffset, int localNameLength)
        {
            WriteBytes('<', '/');
            if (prefixLength != 0)
            {
                WritePrefix(prefixBuffer, prefixOffset, prefixLength);
                WriteByte(':');
            }
            WriteLocalName(localNameBuffer, localNameOffset, localNameLength);
            WriteByte('>');
        }

        private void WriteStartXmlnsAttribute()
        {
            int offset;
            byte[] buffer = GetBuffer(6, out offset);
            buffer[offset + 0] = (byte)' ';
            buffer[offset + 1] = (byte)'x';
            buffer[offset + 2] = (byte)'m';
            buffer[offset + 3] = (byte)'l';
            buffer[offset + 4] = (byte)'n';
            buffer[offset + 5] = (byte)'s';
            Advance(6);
            _inAttribute = true;
        }

        public override void WriteXmlnsAttribute(string prefix, string ns)
        {
            WriteStartXmlnsAttribute();
            if (prefix.Length != 0)
            {
                WriteByte(':');
                WritePrefix(prefix);
            }
            WriteBytes('=', '"');
            WriteEscapedText(ns);
            WriteEndAttribute();
        }

        public override void WriteXmlnsAttribute(string prefix, XmlDictionaryString ns)
        {
            WriteXmlnsAttribute(prefix, ns.Value);
        }

        public override void WriteXmlnsAttribute(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] nsBuffer, int nsOffset, int nsLength)
        {
            WriteStartXmlnsAttribute();
            if (prefixLength != 0)
            {
                WriteByte(':');
                WritePrefix(prefixBuffer, prefixOffset, prefixLength);
            }
            WriteBytes('=', '"');
            WriteEscapedText(nsBuffer, nsOffset, nsLength);
            WriteEndAttribute();
        }

        public override void WriteStartAttribute(string prefix, string localName)
        {
            WriteByte(' ');
            if (prefix.Length != 0)
            {
                WritePrefix(prefix);
                WriteByte(':');
            }
            WriteLocalName(localName);
            WriteBytes('=', '"');
            _inAttribute = true;
        }

        public override void WriteStartAttribute(string prefix, XmlDictionaryString localName)
        {
            WriteStartAttribute(prefix, localName.Value);
        }

        public override void WriteStartAttribute(byte[] prefixBuffer, int prefixOffset, int prefixLength, byte[] localNameBuffer, int localNameOffset, int localNameLength)
        {
            WriteByte(' ');
            if (prefixLength != 0)
            {
                WritePrefix(prefixBuffer, prefixOffset, prefixLength);
                WriteByte(':');
            }
            WriteLocalName(localNameBuffer, localNameOffset, localNameLength);
            WriteBytes('=', '"');
            _inAttribute = true;
        }

        public override void WriteEndAttribute()
        {
            WriteByte('"');
            _inAttribute = false;
        }

        public override async Task WriteEndAttributeAsync()
        {
            await WriteByteAsync('"').ConfigureAwait(false);
            _inAttribute = false;
        }

        private void WritePrefix(string prefix)
        {
            if (prefix.Length == 1)
            {
                WriteUTF8Char(prefix[0]);
            }
            else
            {
                WriteUTF8Chars(prefix);
            }
        }

        private void WritePrefix(byte[] prefixBuffer, int prefixOffset, int prefixLength)
        {
            if (prefixLength == 1)
            {
                WriteUTF8Char((char)prefixBuffer[prefixOffset]);
            }
            else
            {
                WriteUTF8Chars(prefixBuffer, prefixOffset, prefixLength);
            }
        }

        private void WriteLocalName(string localName)
        {
            WriteUTF8Chars(localName);
        }

        private void WriteLocalName(byte[] localNameBuffer, int localNameOffset, int localNameLength)
        {
            WriteUTF8Chars(localNameBuffer, localNameOffset, localNameLength);
        }

        public override void WriteEscapedText(XmlDictionaryString s)
        {
            WriteEscapedText(s.Value);
        }

        [SecuritySafeCritical]
        unsafe public override void WriteEscapedText(string s)
        {
            int count = s.Length;
            if (count > 0)
            {
                fixed (char* chars = s)
                {
                    UnsafeWriteEscapedText(chars, count);
                }
            }
        }

        [SecuritySafeCritical]
        unsafe public override void WriteEscapedText(char[] s, int offset, int count)
        {
            if (count > 0)
            {
                fixed (char* chars = &s[offset])
                {
                    UnsafeWriteEscapedText(chars, count);
                }
            }
        }

        [SecurityCritical]
        private unsafe void UnsafeWriteEscapedText(char* chars, int count)
        {
            bool[] isEscapedChar = (_inAttribute ? _isEscapedAttributeChar : _isEscapedElementChar);
            int isEscapedCharLength = isEscapedChar.Length;
            int i = 0;
            for (int j = 0; j < count; j++)
            {
                char ch = chars[j];
                if (ch < isEscapedCharLength && isEscapedChar[ch] || ch >= 0xFFFE)
                {
                    UnsafeWriteUTF8Chars(chars + i, j - i);
                    WriteCharEntity(ch);
                    i = j + 1;
                }
            }
            UnsafeWriteUTF8Chars(chars + i, count - i);
        }

        public override void WriteEscapedText(byte[] chars, int offset, int count)
        {
            bool[] isEscapedChar = (_inAttribute ? _isEscapedAttributeChar : _isEscapedElementChar);
            int isEscapedCharLength = isEscapedChar.Length;
            int i = 0;
            for (int j = 0; j < count; j++)
            {
                byte ch = chars[offset + j];
                if (ch < isEscapedCharLength && isEscapedChar[ch])
                {
                    WriteUTF8Chars(chars, offset + i, j - i);
                    WriteCharEntity(ch);
                    i = j + 1;
                }
                else if (ch == 239 && offset + j + 2 < count)
                {
                    // 0xFFFE and 0xFFFF must be written as char entities
                    // UTF8(239, 191, 190) = (char) 0xFFFE
                    // UTF8(239, 191, 191) = (char) 0xFFFF
                    byte ch2 = chars[offset + j + 1];
                    byte ch3 = chars[offset + j + 2];
                    if (ch2 == 191 && (ch3 == 190 || ch3 == 191))
                    {
                        WriteUTF8Chars(chars, offset + i, j - i);
                        WriteCharEntity(ch3 == 190 ? (char)0xFFFE : (char)0xFFFF);
                        i = j + 3;
                    }
                }
            }
            WriteUTF8Chars(chars, offset + i, count - i);
        }

        public void WriteText(int ch)
        {
            WriteUTF8Char(ch);
        }

        public override void WriteText(byte[] chars, int offset, int count)
        {
            WriteUTF8Chars(chars, offset, count);
        }

        [SecuritySafeCritical]
        unsafe public override void WriteText(char[] chars, int offset, int count)
        {
            if (count > 0)
            {
                fixed (char* pch = &chars[offset])
                {
                    UnsafeWriteUTF8Chars(pch, count);
                }
            }
        }

        public override void WriteText(string value)
        {
            WriteUTF8Chars(value);
        }

        public override void WriteText(XmlDictionaryString value)
        {
            WriteUTF8Chars(value.Value);
        }

        public void WriteLessThanCharEntity()
        {
            int offset;
            byte[] buffer = GetBuffer(4, out offset);
            buffer[offset + 0] = (byte)'&';
            buffer[offset + 1] = (byte)'l';
            buffer[offset + 2] = (byte)'t';
            buffer[offset + 3] = (byte)';';
            Advance(4);
        }

        public void WriteGreaterThanCharEntity()
        {
            int offset;
            byte[] buffer = GetBuffer(4, out offset);
            buffer[offset + 0] = (byte)'&';
            buffer[offset + 1] = (byte)'g';
            buffer[offset + 2] = (byte)'t';
            buffer[offset + 3] = (byte)';';
            Advance(4);
        }

        public void WriteAmpersandCharEntity()
        {
            int offset;
            byte[] buffer = GetBuffer(5, out offset);
            buffer[offset + 0] = (byte)'&';
            buffer[offset + 1] = (byte)'a';
            buffer[offset + 2] = (byte)'m';
            buffer[offset + 3] = (byte)'p';
            buffer[offset + 4] = (byte)';';
            Advance(5);
        }

        public void WriteApostropheCharEntity()
        {
            int offset;
            byte[] buffer = GetBuffer(6, out offset);
            buffer[offset + 0] = (byte)'&';
            buffer[offset + 1] = (byte)'a';
            buffer[offset + 2] = (byte)'p';
            buffer[offset + 3] = (byte)'o';
            buffer[offset + 4] = (byte)'s';
            buffer[offset + 5] = (byte)';';
            Advance(6);
        }

        public void WriteQuoteCharEntity()
        {
            int offset;
            byte[] buffer = GetBuffer(6, out offset);
            buffer[offset + 0] = (byte)'&';
            buffer[offset + 1] = (byte)'q';
            buffer[offset + 2] = (byte)'u';
            buffer[offset + 3] = (byte)'o';
            buffer[offset + 4] = (byte)'t';
            buffer[offset + 5] = (byte)';';
            Advance(6);
        }

        private void WriteHexCharEntity(int ch)
        {
            byte[] chars = GetCharEntityBuffer();
            int offset = maxEntityLength;
            chars[--offset] = (byte)';';
            offset -= ToBase16(chars, offset, (uint)ch);
            chars[--offset] = (byte)'x';
            chars[--offset] = (byte)'#';
            chars[--offset] = (byte)'&';
            WriteUTF8Chars(chars, offset, maxEntityLength - offset);
        }

        public override void WriteCharEntity(int ch)
        {
            switch (ch)
            {
                case '<':
                    WriteLessThanCharEntity();
                    break;
                case '>':
                    WriteGreaterThanCharEntity();
                    break;
                case '&':
                    WriteAmpersandCharEntity();
                    break;
                case '\'':
                    WriteApostropheCharEntity();
                    break;
                case '"':
                    WriteQuoteCharEntity();
                    break;
                default:
                    WriteHexCharEntity(ch);
                    break;
            }
        }

        private int ToBase16(byte[] chars, int offset, uint value)
        {
            int count = 0;
            do
            {
                count++;
                chars[--offset] = s_digits[(int)(value & 0x0F)];
                value /= 16;
            }
            while (value != 0);
            return count;
        }

        public override void WriteBoolText(bool value)
        {
            int offset;
            byte[] buffer = GetBuffer(XmlConverter.MaxBoolChars, out offset);
            Advance(XmlConverter.ToChars(value, buffer, offset));
        }

        public override void WriteDecimalText(decimal value)
        {
            int offset;
            byte[] buffer = GetBuffer(XmlConverter.MaxDecimalChars, out offset);
            Advance(XmlConverter.ToChars(value, buffer, offset));
        }

        public override void WriteDoubleText(double value)
        {
            int offset;
            byte[] buffer = GetBuffer(XmlConverter.MaxDoubleChars, out offset);
            Advance(XmlConverter.ToChars(value, buffer, offset));
        }

        public override void WriteFloatText(float value)
        {
            int offset;
            byte[] buffer = GetBuffer(XmlConverter.MaxFloatChars, out offset);
            Advance(XmlConverter.ToChars(value, buffer, offset));
        }

        public override void WriteDateTimeText(DateTime value)
        {
            int offset;
            byte[] buffer = GetBuffer(XmlConverter.MaxDateTimeChars, out offset);
            Advance(XmlConverter.ToChars(value, buffer, offset));
        }

        public override void WriteUniqueIdText(UniqueId value)
        {
            if (value.IsGuid)
            {
                int charCount = value.CharArrayLength;
                char[] chars = GetCharBuffer(charCount);
                value.ToCharArray(chars, 0);
                WriteText(chars, 0, charCount);
            }
            else
            {
                WriteEscapedText(value.ToString());
            }
        }

        public override void WriteInt32Text(int value)
        {
            int offset;
            byte[] buffer = GetBuffer(XmlConverter.MaxInt32Chars, out offset);
            Advance(XmlConverter.ToChars(value, buffer, offset));
        }

        public override void WriteInt64Text(long value)
        {
            int offset;
            byte[] buffer = GetBuffer(XmlConverter.MaxInt64Chars, out offset);
            Advance(XmlConverter.ToChars(value, buffer, offset));
        }

        public override void WriteUInt64Text(ulong value)
        {
            int offset;
            byte[] buffer = GetBuffer(XmlConverter.MaxUInt64Chars, out offset);
            Advance(XmlConverter.ToChars((double)value, buffer, offset));
        }

        public override void WriteGuidText(Guid value)
        {
            WriteText(value.ToString());
        }

        public override void WriteBase64Text(byte[] trailBytes, int trailByteCount, byte[] buffer, int offset, int count)
        {
            if (trailByteCount > 0)
            {
                InternalWriteBase64Text(trailBytes, 0, trailByteCount);
            }
            InternalWriteBase64Text(buffer, offset, count);
        }

        public override async Task WriteBase64TextAsync(byte[] trailBytes, int trailByteCount, byte[] buffer, int offset, int count)
        {
            if (trailByteCount > 0)
            {
                await InternalWriteBase64TextAsync(trailBytes, 0, trailByteCount).ConfigureAwait(false);
            }

            await InternalWriteBase64TextAsync(buffer, offset, count).ConfigureAwait(false);
        }

        private void InternalWriteBase64Text(byte[] buffer, int offset, int count)
        {
            Base64Encoding encoding = XmlConverter.Base64Encoding;
            while (count >= 3)
            {
                int byteCount = Math.Min(bufferLength / 4 * 3, count - count % 3);
                int charCount = byteCount / 3 * 4;
                int charOffset;
                byte[] chars = GetBuffer(charCount, out charOffset);
                Advance(encoding.GetChars(buffer, offset, byteCount, chars, charOffset));
                offset += byteCount;
                count -= byteCount;
            }
            if (count > 0)
            {
                int charOffset;
                byte[] chars = GetBuffer(4, out charOffset);
                Advance(encoding.GetChars(buffer, offset, count, chars, charOffset));
            }
        }

        private async Task InternalWriteBase64TextAsync(byte[] buffer, int offset, int count)
        {
            Base64Encoding encoding = XmlConverter.Base64Encoding;
            while (count >= 3)
            {
                int byteCount = Math.Min(bufferLength / 4 * 3, count - count % 3);
                int charCount = byteCount / 3 * 4;
                int charOffset;
                BytesWithOffset bufferResult = await GetBufferAsync(charCount).ConfigureAwait(false);
                byte[] chars = bufferResult.Bytes;
                charOffset = bufferResult.Offset;
                Advance(encoding.GetChars(buffer, offset, byteCount, chars, charOffset));
                offset += byteCount;
                count -= byteCount;
            }
            if (count > 0)
            {
                int charOffset;
                BytesWithOffset bufferResult = await GetBufferAsync(4).ConfigureAwait(false);
                byte[] chars = bufferResult.Bytes;
                charOffset = bufferResult.Offset;
                Advance(encoding.GetChars(buffer, offset, count, chars, charOffset));
            }
        }

        public override void WriteTimeSpanText(TimeSpan value)
        {
            WriteText(XmlConvert.ToString(value));
        }

        public override void WriteStartListText()
        {
        }

        public override void WriteListSeparator()
        {
            WriteByte(' ');
        }

        public override void WriteEndListText()
        {
        }

        public override void WriteQualifiedName(string prefix, XmlDictionaryString localName)
        {
            if (prefix.Length != 0)
            {
                WritePrefix(prefix);
                WriteByte(':');
            }
            WriteText(localName);
        }
    }
}
