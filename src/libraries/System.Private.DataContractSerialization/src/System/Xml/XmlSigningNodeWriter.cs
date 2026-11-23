// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        internal override CancellationToken CancellationToken
        {
            get => base.CancellationToken;
            set
            {
                base.CancellationToken = value;
                _writer.CancellationToken = value;
                _signingWriter.CancellationToken = value;
            }
        }

        public void SetOutput(XmlNodeWriter writer, Stream stream, bool includeComments, string[]? inclusivePrefixes)
        {
            _writer = writer;
            _signingWriter ??= new XmlCanonicalWriter();
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

        public override async Task FlushAsync()
        {
            await _writer.FlushAsync().ConfigureAwait(false);
            await _signingWriter.FlushAsync().ConfigureAwait(false);
        }

        public override void Close()
        {
            _writer.Close();
            _signingWriter.Close();
        }

        public override async ValueTask CloseAsync()
        {
            try
            {
                await _writer.CloseAsync().ConfigureAwait(false);
            }
            finally
            {
                _signingWriter.Close();
            }
        }

        public override Task WriteDeclarationAsync() => _writer.WriteDeclarationAsync();

        public override async Task WriteCommentAsync(string text)
        {
            await _writer.WriteCommentAsync(text).ConfigureAwait(false);
            await _signingWriter.WriteCommentAsync(text).ConfigureAwait(false);
        }

        public override async Task WriteCDataAsync(string text)
        {
            await _writer.WriteCDataAsync(text).ConfigureAwait(false);
            await _signingWriter.WriteEscapedTextAsync(text).ConfigureAwait(false);
        }

        public override async Task WriteStartElementAsync(string? prefix, string localName)
        {
            Debug.Assert(prefix is not null);
            await _writer.WriteStartElementAsync(prefix, localName).ConfigureAwait(false);
            _signingWriter.WriteStartElement(prefix, localName);
        }

        public override async Task WriteStartElementAsync(string? prefix, XmlDictionaryString localName)
        {
            Debug.Assert(prefix is not null);
            await _writer.WriteStartElementAsync(prefix, localName).ConfigureAwait(false);
            _signingWriter.WriteStartElement(prefix, localName.Value);
        }

        public override async Task WriteEndStartElementAsync(bool isEmpty)
        {
            await _writer.WriteEndStartElementAsync(isEmpty).ConfigureAwait(false);
            await _signingWriter.WriteEndStartElementAsync(isEmpty).ConfigureAwait(false);
        }

        public override async Task WriteEndElementAsync(string? prefix, string localName)
        {
            Debug.Assert(prefix is not null);
            await _writer.WriteEndElementAsync(prefix, localName).ConfigureAwait(false);
            await _signingWriter.WriteEndElementAsync(prefix, localName).ConfigureAwait(false);
        }

        public override async Task WriteXmlnsAttributeAsync(string? prefix, string ns)
        {
            Debug.Assert(prefix is not null);
            await _writer.WriteXmlnsAttributeAsync(prefix, ns).ConfigureAwait(false);
            _signingWriter.WriteXmlnsAttribute(prefix, ns);
        }

        public override async Task WriteXmlnsAttributeAsync(string? prefix, XmlDictionaryString ns)
        {
            Debug.Assert(prefix is not null);
            await _writer.WriteXmlnsAttributeAsync(prefix, ns).ConfigureAwait(false);
            _signingWriter.WriteXmlnsAttribute(prefix, ns.Value);
        }

        public override async Task WriteStartAttributeAsync(string prefix, string localName)
        {
            await _writer.WriteStartAttributeAsync(prefix, localName).ConfigureAwait(false);
            _signingWriter.WriteStartAttribute(prefix, localName);
        }

        public override async Task WriteStartAttributeAsync(string prefix, XmlDictionaryString localName)
        {
            await _writer.WriteStartAttributeAsync(prefix, localName).ConfigureAwait(false);
            _signingWriter.WriteStartAttribute(prefix, localName.Value);
        }

        public override async Task WriteEndAttributeAsync()
        {
            await _writer.WriteEndAttributeAsync().ConfigureAwait(false);
            _signingWriter.WriteEndAttribute();
        }

        public override async Task WriteCharEntityAsync(int ch)
        {
            await _writer.WriteCharEntityAsync(ch).ConfigureAwait(false);
            await _signingWriter.WriteCharEntityAsync(ch).ConfigureAwait(false);
        }

        public override async Task WriteEscapedTextAsync(string value)
        {
            await _writer.WriteEscapedTextAsync(value).ConfigureAwait(false);
            await _signingWriter.WriteEscapedTextAsync(value).ConfigureAwait(false);
        }

        public override async Task WriteEscapedTextAsync(XmlDictionaryString value)
        {
            await _writer.WriteEscapedTextAsync(value).ConfigureAwait(false);
            await _signingWriter.WriteEscapedTextAsync(value.Value).ConfigureAwait(false);
        }

        public override async Task WriteEscapedTextAsync(char[] chars, int offset, int count)
        {
            await _writer.WriteEscapedTextAsync(chars, offset, count).ConfigureAwait(false);
            await _signingWriter.WriteEscapedTextAsync(chars, offset, count).ConfigureAwait(false);
        }

        public override async Task WriteEscapedTextAsync(byte[] bytes, int offset, int count)
        {
            await _writer.WriteEscapedTextAsync(bytes, offset, count).ConfigureAwait(false);
            await _signingWriter.WriteEscapedTextAsync(bytes, offset, count).ConfigureAwait(false);
        }

        public override async Task WriteEscapedTextAsync(ReadOnlyMemory<char> value)
        {
            await _writer.WriteEscapedTextAsync(value).ConfigureAwait(false);
            await _signingWriter.WriteEscapedTextAsync(value).ConfigureAwait(false);
        }

        public override async Task WriteEscapedTextAsync(ReadOnlyMemory<byte> value)
        {
            await _writer.WriteEscapedTextAsync(value).ConfigureAwait(false);
            await _signingWriter.WriteEscapedTextAsync(value).ConfigureAwait(false);
        }

        public override async Task WriteTextAsync(string value)
        {
            await _writer.WriteTextAsync(value).ConfigureAwait(false);
            await _signingWriter.WriteTextAsync(value).ConfigureAwait(false);
        }

        public override async Task WriteTextAsync(XmlDictionaryString value)
        {
            await _writer.WriteTextAsync(value).ConfigureAwait(false);
            await _signingWriter.WriteTextAsync(value.Value).ConfigureAwait(false);
        }

        public override async Task WriteTextAsync(char[] chars, int offset, int count)
        {
            await _writer.WriteTextAsync(chars, offset, count).ConfigureAwait(false);
            await _signingWriter.WriteTextAsync(chars, offset, count).ConfigureAwait(false);
        }

        public override async Task WriteTextAsync(byte[] bytes, int offset, int count)
        {
            await _writer.WriteTextAsync(bytes, offset, count).ConfigureAwait(false);
            await _signingWriter.WriteTextAsync(bytes, offset, count).ConfigureAwait(false);
        }

        public override async Task WriteTextAsync(ReadOnlyMemory<char> value)
        {
            await _writer.WriteTextAsync(value).ConfigureAwait(false);
            await _signingWriter.WriteTextAsync(value).ConfigureAwait(false);
        }

        public override async Task WriteTextAsync(ReadOnlyMemory<byte> value)
        {
            await _writer.WriteTextAsync(value).ConfigureAwait(false);
            await _signingWriter.WriteTextAsync(value).ConfigureAwait(false);
        }

        public override async Task WriteValueAsync(object value)
        {
            string text = XmlDictionaryWriter.GetValueText(value);
            if (_text)
                await _writer.WriteTextAsync(text).ConfigureAwait(false);
            else
                await _writer.WriteValueAsync(value).ConfigureAwait(false);
            await _signingWriter.WriteTextAsync(text).ConfigureAwait(false);
        }

        public override async Task WritePrimitiveValueAsync<T>(object value)
        {
            string text = XmlDictionaryWriter.GetPrimitiveValueText<T>(value);
            if (_text)
                await _writer.WriteTextAsync(text).ConfigureAwait(false);
            else
                await _writer.WritePrimitiveValueAsync<T>(value).ConfigureAwait(false);
            await _signingWriter.WriteTextAsync(text).ConfigureAwait(false);
        }

        public override Task WriteStartListTextAsync() => _writer.WriteStartListTextAsync();

        public override async Task WriteListSeparatorAsync()
        {
            await _writer.WriteListSeparatorAsync().ConfigureAwait(false);
            await _signingWriter.WriteTextAsync(" ").ConfigureAwait(false);
        }

        public override Task WriteEndListTextAsync() => _writer.WriteEndListTextAsync();

        public override async Task WriteBase64TextAsync(byte[] trailBytes, int trailByteCount, byte[] buffer, int offset, int count)
        {
            if (trailByteCount > 0)
                await WriteBase64TextAsync(trailBytes, 0, trailByteCount).ConfigureAwait(false);
            await WriteBase64TextAsync(buffer, offset, count).ConfigureAwait(false);
            if (!_text)
                await _writer.WriteBase64TextAsync(trailBytes, trailByteCount, buffer, offset, count).ConfigureAwait(false);
        }

        private async Task WriteBase64TextAsync(byte[] buffer, int offset, int count)
        {
            _base64Chars ??= new byte[512];
            Base64Encoding encoding = DataContractSerializer.Base64Encoding;
            while (count > 0)
            {
                int byteCount = Math.Min(_base64Chars.Length / 4 * 3, count);
                if (byteCount >= 3)
                    byteCount -= byteCount % 3;
                int charCount = encoding.GetChars(buffer, offset, byteCount, _base64Chars, 0);
                await _signingWriter.WriteTextAsync(_base64Chars, 0, charCount).ConfigureAwait(false);
                if (_text)
                    await _writer.WriteTextAsync(_base64Chars, 0, charCount).ConfigureAwait(false);
                offset += byteCount;
                count -= byteCount;
            }
        }

        public override async Task WriteQualifiedNameAsync(string prefix, XmlDictionaryString localName)
        {
            await _writer.WriteQualifiedNameAsync(prefix, localName).ConfigureAwait(false);
            if (prefix.Length != 0)
            {
                await _signingWriter.WriteTextAsync(prefix).ConfigureAwait(false);
                await _signingWriter.WriteTextAsync(":").ConfigureAwait(false);
            }
            await _signingWriter.WriteTextAsync(localName.Value).ConfigureAwait(false);
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
            _base64Chars ??= new byte[512];
            Base64Encoding encoding = DataContractSerializer.Base64Encoding;
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
