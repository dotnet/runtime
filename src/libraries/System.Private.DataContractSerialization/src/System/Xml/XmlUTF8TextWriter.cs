// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace System.Xml
{
    public interface IXmlTextWriterInitializer
    {
        void SetOutput(Stream stream, Encoding encoding, bool ownsStream);
    }

    internal sealed class XmlUTF8TextWriter : XmlBaseWriter, IXmlTextWriterInitializer
    {
        private XmlUTF8NodeWriter _writer = null!;  // initialized in SetOutput

        public void SetOutput(Stream stream, Encoding encoding, bool ownsStream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(encoding);

            if (encoding.WebName != Encoding.UTF8.WebName)
            {
                stream = new EncodingStreamWrapper(stream, encoding, true);
            }

            _writer ??= new XmlUTF8NodeWriter();
            _writer.SetOutput(stream, ownsStream, encoding);
            SetOutput(_writer);
        }

        public override bool CanFragment
        {
            get
            {
                // Fragmenting only works for utf8
                return _writer.Encoding == null;
            }
        }

        protected override XmlSigningNodeWriter CreateSigningNodeWriter()
        {
            return new XmlSigningNodeWriter(true);
        }
    }

    internal class XmlUTF8NodeWriter : XmlStreamNodeWriter
    {
        private byte[]? _entityChars;
        private readonly bool[] _isEscapedAttributeChar;
        private readonly bool[] _isEscapedElementChar;
        private bool _inAttribute;
        private const int bufferLength = 512;
        private const int maxEntityLength = 32;
        private Encoding? _encoding;
        private char[]? _chars;

        private static ReadOnlySpan<byte> Digits => "0123456789ABCDEF"u8;

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

        public new void SetOutput(Stream stream, bool ownsStream, Encoding? encoding)
        {
            Encoding? utf8Encoding = null;
            if (encoding != null && encoding.CodePage == Encoding.UTF8.CodePage)
            {
                utf8Encoding = encoding;
                encoding = null;
            }
            base.SetOutput(stream, ownsStream, utf8Encoding);
            _encoding = encoding;
            _inAttribute = false;
        }

        public Encoding? Encoding
        {
            get
            {
                return _encoding;
            }
        }

        private byte[] GetCharEntityBuffer() => _entityChars ??= new byte[maxEntityLength];

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
                WriteUTF8Bytes("<?xml version=\"1.0\" encoding=\"utf-8\"?>"u8);
            }
            else
            {
                WriteUTF8Bytes("<?xml version=\"1.0\" encoding=\""u8);
                if (_encoding.WebName == Encoding.BigEndianUnicode.WebName)
                    WriteUTF8Bytes("utf-16BE"u8);
                else
                    WriteUTF8Chars(_encoding.WebName);
                WriteUTF8Bytes("\"?>"u8);
            }
        }

        public override void WriteCData(string text)
        {
            WriteUTF8Bytes("<![CDATA["u8);
            WriteUTF8Chars(text);
            WriteUTF8Bytes("]]>"u8);
        }

        private void WriteStartComment()
        {
            WriteUTF8Bytes("<!--"u8);
        }

        private void WriteEndComment()
        {
            WriteUTF8Bytes("-->"u8);
        }

        public override void WriteComment(string text)
        {
            WriteStartComment();
            WriteUTF8Chars(text);
            WriteEndComment();
        }

        public override void WriteStartElement(string? prefix, string localName)
        {
            WriteByte('<');
            if (!string.IsNullOrEmpty(prefix))
            {
                WritePrefix(prefix);
                WriteByte(':');
            }
            WriteLocalName(localName);
        }

        public override async Task WriteStartElementAsync(string? prefix, string localName)
        {
            await WriteByteAsync('<').ConfigureAwait(false);
            if (!string.IsNullOrEmpty(prefix))
            {
                // This method calls into unsafe method which cannot run asyncly.
                WritePrefix(prefix);
                await WriteByteAsync(':').ConfigureAwait(false);
            }

            // This method calls into unsafe method which cannot run asyncly.
            WriteLocalName(localName);
        }

        public override void WriteStartElement(string? prefix, XmlDictionaryString localName)
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

        public override void WriteEndElement(string? prefix, string localName)
        {
            WriteBytes('<', '/');
            if (!string.IsNullOrEmpty(prefix))
            {
                WritePrefix(prefix);
                WriteByte(':');
            }
            WriteLocalName(localName);
            WriteByte('>');
        }

        public override async Task WriteEndElementAsync(string? prefix, string localName)
        {
            await WriteBytesAsync('<', '/').ConfigureAwait(false);
            if (!string.IsNullOrEmpty(prefix))
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
            WriteUTF8Bytes(" xmlns"u8);
            _inAttribute = true;
        }

        public override void WriteXmlnsAttribute(string? prefix, string ns)
        {
            WriteStartXmlnsAttribute();
            if (!string.IsNullOrEmpty(prefix))
            {
                WriteByte(':');
                WritePrefix(prefix);
            }
            WriteBytes('=', '"');
            WriteEscapedText(ns);
            WriteEndAttribute();
        }

        public override void WriteXmlnsAttribute(string? prefix, XmlDictionaryString ns)
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
                WriteUTF8Bytes(prefixBuffer.AsSpan(prefixOffset, prefixLength));
            }
        }

        private void WriteLocalName(string localName)
        {
            WriteUTF8Chars(localName);
        }

        private void WriteLocalName(byte[] localNameBuffer, int localNameOffset, int localNameLength)
        {
            WriteUTF8Bytes(localNameBuffer.AsSpan(localNameOffset, localNameLength));
        }

        public override void WriteEscapedText(XmlDictionaryString s)
        {
            WriteEscapedText(s.Value);
        }

        public override unsafe void WriteEscapedText(string s)
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

        public override unsafe void WriteEscapedText(char[] s, int offset, int count)
        {
            if (count > 0)
            {
                fixed (char* chars = &s[offset])
                {
                    UnsafeWriteEscapedText(chars, count);
                }
            }
        }

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
                    WriteUTF8Bytes(chars.AsSpan(offset + i, j - i));
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
                        WriteUTF8Bytes(chars.AsSpan(offset + i, j - i));
                        WriteCharEntity(ch3 == 190 ? (char)0xFFFE : (char)0xFFFF);
                        i = j + 3;
                    }
                }
            }
            WriteUTF8Bytes(chars.AsSpan(offset + i, count - i));
        }

        public void WriteText(int ch)
        {
            WriteUTF8Char(ch);
        }

        public override void WriteText(byte[] chars, int offset, int count)
        {
            WriteUTF8Bytes(chars.AsSpan(offset, count));
        }

        public override unsafe void WriteText(char[] chars, int offset, int count)
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
            WriteUTF8Bytes("&lt;"u8);
        }

        public void WriteGreaterThanCharEntity()
        {
            WriteUTF8Bytes("&gt;"u8);
        }

        public void WriteAmpersandCharEntity()
        {
            WriteUTF8Bytes("&amp;"u8);
        }

        public void WriteApostropheCharEntity()
        {
            WriteUTF8Bytes("&apos;"u8);
        }

        public void WriteQuoteCharEntity()
        {
            WriteUTF8Bytes("&quot;"u8);
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
            WriteUTF8Bytes(chars.AsSpan(offset, maxEntityLength - offset));
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

        private static int ToBase16(byte[] chars, int offset, uint value)
        {
            int count = 0;
            do
            {
                count++;
                chars[--offset] = Digits[(int)(value & 0x0F)];
                value /= 16;
            }
            while (value != 0);
            return count;
        }

        public override void WriteBoolText(bool value)
        {
            byte[] buffer = GetBuffer(XmlConverter.MaxBoolChars, out int offset);
            Advance(XmlConverter.ToChars(value, buffer, offset));
        }

        public override void WriteDecimalText(decimal value)
        {
            byte[] buffer = GetBuffer(XmlConverter.MaxDecimalChars, out int offset);
            Advance(XmlConverter.ToChars(value, buffer, offset));
        }

        public override void WriteDoubleText(double value)
        {
            byte[] buffer = GetBuffer(XmlConverter.MaxDoubleChars, out int offset);
            Advance(XmlConverter.ToChars(value, buffer, offset));
        }

        public override void WriteFloatText(float value)
        {
            byte[] buffer = GetBuffer(XmlConverter.MaxFloatChars, out int offset);
            Advance(XmlConverter.ToChars(value, buffer, offset));
        }

        public override void WriteDateTimeText(DateTime value)
        {
            byte[] buffer = GetBuffer(XmlConverter.MaxDateTimeChars, out int offset);
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
            byte[] buffer = GetBuffer(XmlConverter.MaxInt32Chars, out int offset);
            Advance(XmlConverter.ToChars(value, buffer, offset));
        }

        public override void WriteInt64Text(long value)
        {
            byte[] buffer = GetBuffer(XmlConverter.MaxInt64Chars, out int offset);
            Advance(XmlConverter.ToChars(value, buffer, offset));
        }

        public override void WriteUInt64Text(ulong value)
        {
            byte[] buffer = GetBuffer(XmlConverter.MaxUInt64Chars, out int offset);
            Advance(XmlConverter.ToChars(value, buffer, offset));
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
            Base64Encoding encoding = DataContractSerializer.Base64Encoding;
            while (count >= 3)
            {
                int byteCount = Math.Min(bufferLength / 4 * 3, count - count % 3);
                int charCount = byteCount / 3 * 4;
                byte[] chars = GetBuffer(charCount, out int charOffset);
                Advance(encoding.GetChars(buffer, offset, byteCount, chars, charOffset));
                offset += byteCount;
                count -= byteCount;
            }
            if (count > 0)
            {
                byte[] chars = GetBuffer(4, out int charOffset);
                Advance(encoding.GetChars(buffer, offset, count, chars, charOffset));
            }
        }

        private async Task InternalWriteBase64TextAsync(byte[] buffer, int offset, int count)
        {
            Base64Encoding encoding = DataContractSerializer.Base64Encoding;
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
