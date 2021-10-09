// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Text;

namespace System.Xml
{
    internal sealed class XmlSigningNodeWriter : XmlNodeWriter
    {
        private XmlNodeWriter _writer = null!; // initialized by SetOutput
        private XmlCanonicalWriter _signingWriter = null!; // initialized by SetOutput
        private byte[] _chars = null!; // initialized by SetOutput
        private byte[]? _base64Chars;
        private readonly bool _text;

        public XmlSigningNodeWriter(bool text)
        {
            _text = text;
        }

        public void SetOutput(XmlNodeWriter writer, Stream stream, bool includeComments, string[]? inclusivePrefixes)
        {
            _writer = writer;
            if (_signingWriter == null)
                _signingWriter = new XmlCanonicalWriter();
            _signingWriter.SetOutput(stream, includeComments, inclusivePrefixes);
            _chars = new byte[XmlConverter.MaxPrimitiveChars];
            _base64Chars = null;
        }

        public XmlNodeWriter NodeWriter
        {
            get
            {
                return _writer;
            }
            set
            {
                _writer = value;
            }
        }

        public XmlCanonicalWriter CanonicalWriter
        {
            get
            {
                return _signingWriter;
            }
        }

        public override void Flush()
        {
            _writer.Flush();
            _signingWriter.Flush();
        }

        public override void Close()
        {
            _writer.Close();
            _signingWriter.Close();
        }

        public override void WriteDeclaration()
        {
            _writer.WriteDeclaration();
            _signingWriter.WriteDeclaration();
        }

        public override void WriteComment(string text)
        {
            _writer.WriteComment(text);
            _signingWriter.WriteComment(text);
        }

        public override void WriteCData(string text)
        {
            _writer.WriteCData(text);
            _signingWriter.WriteEscapedText(text);
        }

        public override void WriteStartElement(string? prefix, string localName)
        {
            Debug.Assert(prefix != null);
            _writer.WriteStartElement(prefix, localName);
            _signingWriter.WriteStartElement(prefix, localName);
        }

        public override void WriteStartElement(byte[] prefixBuffer, int prefixOffset, int prefixLength,
                                               byte[] localNameBuffer, int localNameOffset, int localNameLength)
        {
            _writer.WriteStartElement(prefixBuffer, prefixOffset, prefixLength, localNameBuffer, localNameOffset, localNameLength);
            _signingWriter.WriteStartElement(prefixBuffer, prefixOffset, prefixLength, localNameBuffer, localNameOffset, localNameLength);
        }

        public override void WriteStartElement(string? prefix, XmlDictionaryString localName)
        {
            Debug.Assert(prefix != null);
            _writer.WriteStartElement(prefix, localName);
            _signingWriter.WriteStartElement(prefix, localName.Value);
        }

        public override void WriteEndStartElement(bool isEmpty)
        {
            _writer.WriteEndStartElement(isEmpty);
            _signingWriter.WriteEndStartElement(isEmpty);
        }

        public override void WriteEndElement(string? prefix, string localName)
        {
            Debug.Assert(prefix != null);
            _writer.WriteEndElement(prefix, localName);
            _signingWriter.WriteEndElement(prefix, localName);
        }

        public override void WriteXmlnsAttribute(string? prefix, string ns)
        {
            Debug.Assert(prefix != null);
            _writer.WriteXmlnsAttribute(prefix, ns);
            _signingWriter.WriteXmlnsAttribute(prefix, ns);
        }

        public override void WriteXmlnsAttribute(byte[] prefixBuffer, int prefixOffset, int prefixLength,
                                                 byte[] nsBuffer, int nsOffset, int nsLength)
        {
            _writer.WriteXmlnsAttribute(prefixBuffer, prefixOffset, prefixLength, nsBuffer, nsOffset, nsLength);
            _signingWriter.WriteXmlnsAttribute(prefixBuffer, prefixOffset, prefixLength, nsBuffer, nsOffset, nsLength);
        }

        public override void WriteXmlnsAttribute(string? prefix, XmlDictionaryString ns)
        {
            Debug.Assert(prefix != null);
            _writer.WriteXmlnsAttribute(prefix, ns);
            _signingWriter.WriteXmlnsAttribute(prefix, ns.Value);
        }

        public override void WriteStartAttribute(string prefix, string localName)
        {
            _writer.WriteStartAttribute(prefix, localName);
            _signingWriter.WriteStartAttribute(prefix, localName);
        }

        public override void WriteStartAttribute(byte[] prefixBuffer, int prefixOffset, int prefixLength,
                                                 byte[] localNameBuffer, int localNameOffset, int localNameLength)
        {
            _writer.WriteStartAttribute(prefixBuffer, prefixOffset, prefixLength, localNameBuffer, localNameOffset, localNameLength);
            _signingWriter.WriteStartAttribute(prefixBuffer, prefixOffset, prefixLength, localNameBuffer, localNameOffset, localNameLength);
        }

        public override void WriteStartAttribute(string prefix, XmlDictionaryString localName)
        {
            _writer.WriteStartAttribute(prefix, localName);
            _signingWriter.WriteStartAttribute(prefix, localName.Value);
        }

        public override void WriteEndAttribute()
        {
            _writer.WriteEndAttribute();
            _signingWriter.WriteEndAttribute();
        }

        public override void WriteCharEntity(int ch)
        {
            _writer.WriteCharEntity(ch);
            _signingWriter.WriteCharEntity(ch);
        }

        public override void WriteEscapedText(string value)
        {
            _writer.WriteEscapedText(value);
            _signingWriter.WriteEscapedText(value);
        }

        public override void WriteEscapedText(char[] chars, int offset, int count)
        {
            _writer.WriteEscapedText(chars, offset, count);
            _signingWriter.WriteEscapedText(chars, offset, count);
        }

        public override void WriteEscapedText(XmlDictionaryString value)
        {
            _writer.WriteEscapedText(value);
            _signingWriter.WriteEscapedText(value.Value);
        }

        public override void WriteEscapedText(byte[] chars, int offset, int count)
        {
            _writer.WriteEscapedText(chars, offset, count);
            _signingWriter.WriteEscapedText(chars, offset, count);
        }

        public override void WriteText(string value)
        {
            _writer.WriteText(value);
            _signingWriter.WriteText(value);
        }

        public override void WriteText(char[] chars, int offset, int count)
        {
            _writer.WriteText(chars, offset, count);
            _signingWriter.WriteText(chars, offset, count);
        }

        public override void WriteText(byte[] chars, int offset, int count)
        {
            _writer.WriteText(chars, offset, count);
            _signingWriter.WriteText(chars, offset, count);
        }

        public override void WriteText(XmlDictionaryString value)
        {
            _writer.WriteText(value);
            _signingWriter.WriteText(value.Value);
        }

        public override void WriteInt32Text(int value)
        {
            int count = XmlConverter.ToChars(value, _chars, 0);
            if (_text)
                _writer.WriteText(_chars, 0, count);
            else
                _writer.WriteInt32Text(value);
            _signingWriter.WriteText(_chars, 0, count);
        }

        public override void WriteInt64Text(long value)
        {
            int count = XmlConverter.ToChars(value, _chars, 0);
            if (_text)
                _writer.WriteText(_chars, 0, count);
            else
                _writer.WriteInt64Text(value);
            _signingWriter.WriteText(_chars, 0, count);
        }

        public override void WriteBoolText(bool value)
        {
            int count = XmlConverter.ToChars(value, _chars, 0);
            if (_text)
                _writer.WriteText(_chars, 0, count);
            else
                _writer.WriteBoolText(value);
            _signingWriter.WriteText(_chars, 0, count);
        }

        public override void WriteUInt64Text(ulong value)
        {
            int count = XmlConverter.ToChars(value, _chars, 0);
            if (_text)
                _writer.WriteText(_chars, 0, count);
            else
                _writer.WriteUInt64Text(value);
            _signingWriter.WriteText(_chars, 0, count);
        }

        public override void WriteFloatText(float value)
        {
            int count = XmlConverter.ToChars(value, _chars, 0);
            if (_text)
                _writer.WriteText(_chars, 0, count);
            else
                _writer.WriteFloatText(value);
            _signingWriter.WriteText(_chars, 0, count);
        }

        public override void WriteDoubleText(double value)
        {
            int count = XmlConverter.ToChars(value, _chars, 0);
            if (_text)
                _writer.WriteText(_chars, 0, count);
            else
                _writer.WriteDoubleText(value);
            _signingWriter.WriteText(_chars, 0, count);
        }

        public override void WriteDecimalText(decimal value)
        {
            int count = XmlConverter.ToChars(value, _chars, 0);
            if (_text)
                _writer.WriteText(_chars, 0, count);
            else
                _writer.WriteDecimalText(value);
            _signingWriter.WriteText(_chars, 0, count);
        }

        public override void WriteDateTimeText(DateTime value)
        {
            int count = XmlConverter.ToChars(value, _chars, 0);
            if (_text)
                _writer.WriteText(_chars, 0, count);
            else
                _writer.WriteDateTimeText(value);
            _signingWriter.WriteText(_chars, 0, count);
        }

        public override void WriteUniqueIdText(UniqueId value)
        {
            string s = XmlConverter.ToString(value);
            if (_text)
                _writer.WriteText(s);
            else
                _writer.WriteUniqueIdText(value);
            _signingWriter.WriteText(s);
        }

        public override void WriteTimeSpanText(TimeSpan value)
        {
            string s = XmlConverter.ToString(value);
            if (_text)
                _writer.WriteText(s);
            else
                _writer.WriteTimeSpanText(value);
            _signingWriter.WriteText(s);
        }

        public override void WriteGuidText(Guid value)
        {
            string s = XmlConverter.ToString(value);
            if (_text)
                _writer.WriteText(s);
            else
                _writer.WriteGuidText(value);
            _signingWriter.WriteText(s);
        }

        public override void WriteStartListText()
        {
            _writer.WriteStartListText();
        }

        public override void WriteListSeparator()
        {
            _writer.WriteListSeparator();
            _signingWriter.WriteText(' ');
        }

        public override void WriteEndListText()
        {
            _writer.WriteEndListText();
        }

        public override void WriteBase64Text(byte[] trailBytes, int trailByteCount, byte[] buffer, int offset, int count)
        {
            if (trailByteCount > 0)
                WriteBase64Text(trailBytes, 0, trailByteCount);
            WriteBase64Text(buffer, offset, count);
            if (!_text)
            {
                _writer.WriteBase64Text(trailBytes, trailByteCount, buffer, offset, count);
            }
        }

        private void WriteBase64Text(byte[] buffer, int offset, int count)
        {
            if (_base64Chars == null)
                _base64Chars = new byte[512];
            Base64Encoding encoding = XmlConverter.Base64Encoding;
            while (count >= 3)
            {
                int byteCount = Math.Min(_base64Chars.Length / 4 * 3, count - count % 3);
                int charCount = byteCount / 3 * 4;
                encoding.GetChars(buffer, offset, byteCount, _base64Chars, 0);
                _signingWriter.WriteText(_base64Chars, 0, charCount);
                if (_text)
                {
                    _writer.WriteText(_base64Chars, 0, charCount);
                }
                offset += byteCount;
                count -= byteCount;
            }
            if (count > 0)
            {
                encoding.GetChars(buffer, offset, count, _base64Chars, 0);
                _signingWriter.WriteText(_base64Chars, 0, 4);
                if (_text)
                {
                    _writer.WriteText(_base64Chars, 0, 4);
                }
            }
        }

        public override void WriteQualifiedName(string prefix, XmlDictionaryString localName)
        {
            _writer.WriteQualifiedName(prefix, localName);
            if (prefix.Length != 0)
            {
                _signingWriter.WriteText(prefix);
                _signingWriter.WriteText(":");
            }
            _signingWriter.WriteText(localName.Value);
        }
    }
}
