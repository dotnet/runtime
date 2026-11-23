// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
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

        internal async ValueTask SetOutputAsync(Stream stream, Encoding encoding, bool ownsStream, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(encoding);
            cancellationToken.ThrowIfCancellationRequested();
            if (encoding.WebName != Encoding.UTF8.WebName)
            {
                stream = await EncodingStreamWrapper.CreateWriterAsync(stream, encoding, emitBOM: true, cancellationToken).ConfigureAwait(false);
            }
            _writer ??= new XmlUTF8NodeWriter();
            _writer.SetOutput(stream, ownsStream, encoding);
            SetOutput(_writer);
        }

        internal Task WriteTextArrayAsync<T>(string? prefix, XmlDictionaryString localName,
            XmlDictionaryString? namespaceUri, T[] array, int offset, int count) where T : struct
        {
            CheckArrayBounds(array, offset, count);
            if (count == 0)
                return Task.CompletedTask;
            if (TryWriteBufferedPrimitiveArray(prefix, localName, namespaceUri, array, offset, count, out Task? operation))
                return operation;

            return WriteArrayItemsAsync(prefix, localName, namespaceUri, array, offset, count);
        }

        private Task WriteArrayItemsAsync<T>(string? prefix, XmlDictionaryString localName,
            XmlDictionaryString? namespaceUri, T[] array, int offset, int count) where T : struct
        {
            for (int i = offset; i < offset + count; i++)
            {
                Task operation = WriteTypedPrimitiveElementAsync(prefix, localName, namespaceUri, array[i]);
                if (!operation.IsCompletedSuccessfully)
                    return AwaitArrayItems(operation, prefix, localName, namespaceUri, array, i + 1, offset + count);
                operation.GetAwaiter().GetResult();
            }

            return Task.CompletedTask;
        }

        private async Task AwaitArrayItems<T>(Task pending, string? prefix, XmlDictionaryString localName,
            XmlDictionaryString? namespaceUri, T[] array, int next, int end) where T : struct
        {
            await pending.ConfigureAwait(false);
            for (int i = next; i < end; i++)
                await WriteTypedPrimitiveElementAsync(prefix, localName, namespaceUri, array[i]).ConfigureAwait(false);
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
        private readonly bool[] _isEscapedAttributeChar;
        private readonly bool[] _isEscapedElementChar;
        internal bool CanWriteBufferedContractElements { get; private set; }
        private bool _inAttribute;
        private const int bufferLength = 512;
        private const int maxEntityLength = 32;
        internal const int MaxEscapedBytesPerChar = 8;
        private Encoding? _encoding;

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
            CanWriteBufferedContractElements = GetType() == typeof(XmlUTF8NodeWriter);
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
            CanWriteBufferedContractElements = GetType() == typeof(XmlUTF8NodeWriter) && CanUseBufferedUtf8Encoding;
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

        public override async Task WriteDeclarationAsync()
        {
            if (_encoding is null)
            {
                await WriteTextAsync("<?xml version=\"1.0\" encoding=\"utf-8\"?>").ConfigureAwait(false);
            }
            else
            {
                await WriteTextAsync("<?xml version=\"1.0\" encoding=\"").ConfigureAwait(false);
                await WriteTextAsync(_encoding.WebName == Encoding.BigEndianUnicode.WebName ? "utf-16BE" : _encoding.WebName).ConfigureAwait(false);
                await WriteTextAsync("\"?>").ConfigureAwait(false);
            }
        }

        public override void WriteCData(string text)
        {
            WriteUTF8Bytes("<![CDATA["u8);
            WriteUTF8Chars(text);
            WriteUTF8Bytes("]]>"u8);
        }

        public override async Task WriteCDataAsync(string text)
        {
            await WriteTextAsync("<![CDATA[").ConfigureAwait(false);
            await WriteTextAsync(text).ConfigureAwait(false);
            await WriteTextAsync("]]>").ConfigureAwait(false);
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

        public override async Task WriteCommentAsync(string text)
        {
            await WriteTextAsync("<!--").ConfigureAwait(false);
            await WriteTextAsync(text).ConfigureAwait(false);
            await WriteTextAsync("-->").ConfigureAwait(false);
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

        private bool CanWriteNameInBuffer(string? prefix, string localName, int markupLength)
        {
            long maximumBytes = markupLength + (long)MaxUtf8BytesPerChar * ((prefix?.Length ?? 0) + (long)localName.Length);
            return CanUseBufferedUtf8Encoding && maximumBytes <= bufferLength && HasBufferSpace((int)maximumBytes);
        }

        internal void WriteBufferedStartElement(string prefix, byte[] localName)
        {
            WriteBufferedByte((byte)'<');
            WriteBufferedPrefix(prefix);
            WriteBufferedBytes(localName);
        }

        internal void WriteBufferedEndElement(string prefix, byte[] localName)
        {
            WriteBufferedBytes("</"u8);
            WriteBufferedPrefix(prefix);
            WriteBufferedBytes(localName);
            WriteBufferedByte((byte)'>');
        }

        internal void WriteBufferedPrimitiveElement<TPrimitive, TValue>(string prefix, byte[] localName, TValue value)
            where TPrimitive : struct
        {
            Debug.Assert(typeof(TPrimitive) != typeof(ReflectionPlan.StringValue));
            Debug.Assert(prefix.Length == 0 || prefix.Length == 1 && prefix[0] < 0x80);
            byte[] buffer = StreamBuffer;
            int offset = BufferOffset;
            int start = offset;
            buffer[offset++] = (byte)'<';
            if (prefix.Length != 0)
            {
                buffer[offset++] = (byte)prefix[0];
                buffer[offset++] = (byte)':';
            }
            localName.CopyTo(buffer.AsSpan(offset));
            offset += localName.Length;
            buffer[offset++] = (byte)'>';
            offset += FormatPrimitiveValue<TPrimitive, TValue>(value, buffer, offset);
            buffer[offset++] = (byte)'<';
            buffer[offset++] = (byte)'/';
            if (prefix.Length != 0)
            {
                buffer[offset++] = (byte)prefix[0];
                buffer[offset++] = (byte)':';
            }
            localName.CopyTo(buffer.AsSpan(offset));
            offset += localName.Length;
            buffer[offset++] = (byte)'>';
            Advance(offset - start);
        }

        private void WriteBufferedPrefix(string prefix)
        {
            if (prefix.Length != 0)
            {
                if (prefix.Length == 1 && prefix[0] < 0x80)
                    WriteBufferedByte((byte)prefix[0]);
                else
                    WriteBufferedUTF8Chars(prefix);
                WriteBufferedByte((byte)':');
            }
        }

        public override Task WriteStartElementAsync(string? prefix, string localName)
        {
            if (CanWriteNameInBuffer(prefix, localName, 2))
            {
                WriteStartElement(prefix, localName);
                return Task.CompletedTask;
            }

            return WriteStartElementAsyncCore(prefix, localName);
        }

        private async Task WriteStartElementAsyncCore(string? prefix, string localName)
        {
            await WriteByteAsync('<').ConfigureAwait(false);
            if (!string.IsNullOrEmpty(prefix))
            {
                await WriteUTF8CharsAsync(prefix.AsMemory()).ConfigureAwait(false);
                await WriteByteAsync(':').ConfigureAwait(false);
            }

            await WriteUTF8CharsAsync(localName.AsMemory()).ConfigureAwait(false);
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

        public override Task WriteEndStartElementAsync(bool isEmpty) =>
            isEmpty ? WriteBytesAsync('/', '>') : WriteByteAsync('>');

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

        public override Task WriteEndElementAsync(string? prefix, string localName)
        {
            if (CanWriteNameInBuffer(prefix, localName, 4))
            {
                WriteEndElement(prefix, localName);
                return Task.CompletedTask;
            }

            return WriteEndElementAsyncCore(prefix, localName);
        }

        private async Task WriteEndElementAsyncCore(string? prefix, string localName)
        {
            await WriteBytesAsync('<', '/').ConfigureAwait(false);
            if (!string.IsNullOrEmpty(prefix))
            {
                await WriteUTF8CharsAsync(prefix.AsMemory()).ConfigureAwait(false);
                await WriteByteAsync(':').ConfigureAwait(false);
            }
            await WriteUTF8CharsAsync(localName.AsMemory()).ConfigureAwait(false);
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

        public override Task WriteXmlnsAttributeAsync(string? prefix, string ns)
        {
            long maximumBytes = 10 + (long)MaxUtf8BytesPerChar * (prefix?.Length ?? 0) + GetEscapedTextMaximumBytes(ns, inAttribute: true);
            if (CanUseBufferedUtf8Encoding && maximumBytes <= bufferLength && HasBufferSpace((int)maximumBytes))
            {
                WriteXmlnsAttribute(prefix, ns);
                return Task.CompletedTask;
            }

            return WriteXmlnsAttributeAsyncCore(prefix, ns);
        }

        private async Task WriteXmlnsAttributeAsyncCore(string? prefix, string ns)
        {
            await WriteTextAsync(" xmlns").ConfigureAwait(false);
            _inAttribute = true;
            if (!string.IsNullOrEmpty(prefix))
            {
                await WriteByteAsync(':').ConfigureAwait(false);
                await WriteTextAsync(prefix).ConfigureAwait(false);
            }
            await WriteBytesAsync('=', '"').ConfigureAwait(false);
            await WriteEscapedTextAsync(ns).ConfigureAwait(false);
            await WriteEndAttributeAsync().ConfigureAwait(false);
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

        public override Task WriteStartAttributeAsync(string prefix, string localName)
        {
            if (CanWriteNameInBuffer(prefix, localName, 4))
            {
                WriteStartAttribute(prefix, localName);
                return Task.CompletedTask;
            }

            return WriteStartAttributeAsyncCore(prefix, localName);
        }

        private async Task WriteStartAttributeAsyncCore(string prefix, string localName)
        {
            await WriteByteAsync(' ').ConfigureAwait(false);
            if (prefix.Length != 0)
            {
                await WriteTextAsync(prefix).ConfigureAwait(false);
                await WriteByteAsync(':').ConfigureAwait(false);
            }
            await WriteTextAsync(localName).ConfigureAwait(false);
            await WriteBytesAsync('=', '"').ConfigureAwait(false);
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

        public override Task WriteEndAttributeAsync()
        {
            if (HasBufferSpace(1))
            {
                WriteEndAttribute();
                return Task.CompletedTask;
            }

            return WriteEndAttributeAsyncCore();
        }

        private async Task WriteEndAttributeAsyncCore()
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

        public override Task WriteEscapedTextAsync(string value)
        {
            long maximumBytes = GetEscapedTextMaximumBytes(value, _inAttribute);
            if (CanUseBufferedUtf8Encoding && maximumBytes <= bufferLength && HasBufferSpace((int)maximumBytes))
            {
                WriteEscapedText(value);
                return Task.CompletedTask;
            }

            return WriteEscapedTextAsync(value.AsMemory());
        }

        internal long GetEscapedTextMaximumBytes(string value, bool inAttribute)
        {
            long escapedMaximum = (long)MaxEscapedBytesPerChar * value.Length;
            if (escapedMaximum <= StreamBuffer.Length - BufferOffset)
                return escapedMaximum;
            long maximumBytes = (long)MaxUtf8BytesPerChar * value.Length;
            if (maximumBytes > bufferLength)
                return maximumBytes;
            bool[] escaped = inAttribute ? _isEscapedAttributeChar : _isEscapedElementChar;
            foreach (char ch in value)
            {
                if ((ch < escaped.Length && escaped[ch]) || ch >= 0xFFFE)
                    maximumBytes += MaxEscapedBytesPerChar - MaxUtf8BytesPerChar;
            }

            return maximumBytes;
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

        public override void WriteEscapedText(char[] s, int offset, int count) =>
            WriteEscapedText(s.AsSpan(offset, count));

        internal unsafe void WriteEscapedText(ReadOnlySpan<char> value)
        {
            if (!value.IsEmpty)
            {
                fixed (char* chars = value)
                {
                    UnsafeWriteEscapedText(chars, value.Length);
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

        public override void WriteEscapedText(byte[] chars, int offset, int count) =>
            WriteEscapedText(chars.AsSpan(offset, count));

        internal void WriteEscapedText(ReadOnlySpan<byte> chars)
        {
            bool[] isEscapedChar = (_inAttribute ? _isEscapedAttributeChar : _isEscapedElementChar);
            int isEscapedCharLength = isEscapedChar.Length;
            int i = 0;
            for (int j = 0; j < chars.Length; j++)
            {
                byte ch = chars[j];
                if (ch < isEscapedCharLength && isEscapedChar[ch])
                {
                    WriteUTF8Bytes(chars.Slice(i, j - i));
                    WriteCharEntity(ch);
                    i = j + 1;
                }
                else if (ch == 239 && j + 2 < chars.Length)
                {
                    // 0xFFFE and 0xFFFF must be written as char entities
                    // UTF8(239, 191, 190) = (char) 0xFFFE
                    // UTF8(239, 191, 191) = (char) 0xFFFF
                    byte ch2 = chars[j + 1];
                    byte ch3 = chars[j + 2];
                    if (ch2 == 191 && (ch3 == 190 || ch3 == 191))
                    {
                        WriteUTF8Bytes(chars.Slice(i, j - i));
                        WriteCharEntity(ch3 == 190 ? (char)0xFFFE : (char)0xFFFF);
                        j += 2;
                        i = j + 1;
                    }
                }
            }
            WriteUTF8Bytes(chars.Slice(i));
        }

        public override async Task WriteEscapedTextAsync(ReadOnlyMemory<char> value)
        {
            bool[] escaped = _inAttribute ? _isEscapedAttributeChar : _isEscapedElementChar;
            int start = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char ch = value.Span[i];
                if ((ch < escaped.Length && escaped[ch]) || ch >= 0xFFFE)
                {
                    await WriteUTF8CharsAsync(value.Slice(start, i - start)).ConfigureAwait(false);
                    await WriteCharEntityAsync(ch).ConfigureAwait(false);
                    start = i + 1;
                }
            }
            await WriteUTF8CharsAsync(value.Slice(start)).ConfigureAwait(false);
        }

        public override async Task WriteEscapedTextAsync(ReadOnlyMemory<byte> value)
        {
            bool[] escaped = _inAttribute ? _isEscapedAttributeChar : _isEscapedElementChar;
            int start = 0;
            for (int i = 0; i < value.Length; i++)
            {
                byte ch = value.Span[i];
                int entity = ch;
                int width = 1;
                bool mustEscape = ch < escaped.Length && escaped[ch];
                if (ch == 239 && i + 2 < value.Length && value.Span[i + 1] == 191 && value.Span[i + 2] is 190 or 191)
                {
                    entity = value.Span[i + 2] == 190 ? 0xFFFE : 0xFFFF;
                    width = 3;
                    mustEscape = true;
                }
                if (mustEscape)
                {
                    await WriteBytesAsync(value.Slice(start, i - start)).ConfigureAwait(false);
                    await WriteCharEntityAsync(entity).ConfigureAwait(false);
                    i += width - 1;
                    start = i + 1;
                }
            }
            await WriteBytesAsync(value.Slice(start)).ConfigureAwait(false);
        }

        public void WriteText(int ch)
        {
            WriteUTF8Char(ch);
        }

        public override void WriteText(byte[] chars, int offset, int count)
        {
            WriteText(chars.AsSpan(offset, count));
        }

        internal void WriteText(ReadOnlySpan<byte> chars) => WriteUTF8Bytes(chars);

        public override void WriteText(char[] chars, int offset, int count) =>
            WriteText(chars.AsSpan(offset, count));

        internal unsafe void WriteText(ReadOnlySpan<char> chars)
        {
            if (!chars.IsEmpty)
            {
                fixed (char* pch = chars)
                {
                    UnsafeWriteUTF8Chars(pch, chars.Length);
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

        public override Task WriteTextAsync(ReadOnlyMemory<char> value) => WriteUTF8CharsAsync(value);

        public override Task WriteTextAsync(ReadOnlyMemory<byte> value) => WriteBytesAsync(value);

        public override Task WriteTextAsync(string value)
        {
            if (CanUseBufferedUtf8Encoding && value.Length <= bufferLength / MaxUtf8BytesPerChar && HasBufferSpace(value.Length * MaxUtf8BytesPerChar))
            {
                WriteText(value);
                return Task.CompletedTask;
            }

            return WriteUTF8CharsAsync(value.AsMemory());
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
            Span<byte> chars = stackalloc byte[maxEntityLength];
            int offset = maxEntityLength;
            chars[--offset] = (byte)';';
            offset -= ToBase16(chars, offset, (uint)ch);
            chars[--offset] = (byte)'x';
            chars[--offset] = (byte)'#';
            chars[--offset] = (byte)'&';
            WriteUTF8Bytes(chars.Slice(offset));
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

        public override Task WriteCharEntityAsync(int ch)
        {
            if (HasBufferSpace(maxEntityLength))
            {
                WriteCharEntity(ch);
                return Task.CompletedTask;
            }

            return WriteCharEntityAsyncCore(ch);
        }

        private async Task WriteCharEntityAsyncCore(int ch)
        {
            await EnsureBufferAsync(maxEntityLength).ConfigureAwait(false);
            WriteCharEntity(ch);
        }

        private static int ToBase16(Span<byte> chars, int offset, uint value)
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
                const int GuidTextLength = 45;
                Debug.Assert(value.CharArrayLength == GuidTextLength);
                Span<char> chars = stackalloc char[GuidTextLength];
                value.ToSpan(chars);
                WriteText(chars);
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

        public override Task WriteValueAsync(object value) => value switch
        {
            bool => WritePrimitiveValueAsync<bool>(value),
            int => WritePrimitiveValueAsync<int>(value),
            long => WritePrimitiveValueAsync<long>(value),
            ulong => WritePrimitiveValueAsync<ulong>(value),
            float => WritePrimitiveValueAsync<float>(value),
            double => WritePrimitiveValueAsync<double>(value),
            decimal => WritePrimitiveValueAsync<decimal>(value),
            DateTime => WritePrimitiveValueAsync<DateTime>(value),
            UniqueId uniqueId when !uniqueId.IsGuid => WriteEscapedTextAsync(uniqueId.ToString()),
            _ => WriteTextAsync(XmlDictionaryWriter.GetValueText(value))
        };

        public override Task WritePrimitiveValueAsync<T>(object value)
        {
            int bufferSize = GetPrimitiveBufferSize<T>();
            if (bufferSize == 0)
                return WriteTextAsync(XmlDictionaryWriter.GetPrimitiveValueText<T>(value));
            if (HasBufferSpace(bufferSize))
            {
                WritePrimitiveValue<T>(value);
                return Task.CompletedTask;
            }

            return WritePrimitiveValueAsyncCore<T>(value, bufferSize);
        }

        internal static int GetPrimitiveBufferSize<T>() where T : struct =>
            typeof(T) == typeof(bool) ? XmlConverter.MaxBoolChars
                : XmlDictionaryWriter.IsInt32Primitive<T>() ? XmlConverter.MaxInt32Chars
                : typeof(T) == typeof(uint) ? XmlConverter.MaxInt64Chars
                : typeof(T) == typeof(long) ? XmlConverter.MaxInt64Chars
                : typeof(T) == typeof(ulong) ? XmlConverter.MaxUInt64Chars
                : typeof(T) == typeof(float) ? XmlConverter.MaxFloatChars
                : typeof(T) == typeof(double) ? XmlConverter.MaxDoubleChars
                : typeof(T) == typeof(decimal) ? XmlConverter.MaxDecimalChars
                : typeof(T) == typeof(DateTime) ? XmlConverter.MaxDateTimeChars
                : 0;

        private async Task WritePrimitiveValueAsyncCore<T>(object value, int bufferSize) where T : struct
        {
            await EnsureBufferAsync(bufferSize).ConfigureAwait(false);
            WritePrimitiveValue<T>(value);
        }

        internal void WritePrimitiveValue<T>(object value) where T : struct => WritePrimitiveValue<T, object>(value);

        internal void WritePrimitiveValue<TPrimitive, TValue>(TValue value) where TPrimitive : struct
        {
            if (typeof(TPrimitive) == typeof(ReflectionPlan.StringValue))
            {
                WriteEscapedText((string)(object)value!);
                return;
            }

            Debug.Assert(GetPrimitiveBufferSize<TPrimitive>() <= StreamBuffer.Length - BufferOffset);
            Advance(FormatPrimitiveValue<TPrimitive, TValue>(value, StreamBuffer, BufferOffset));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FormatPrimitiveValue<TPrimitive, TValue>(TValue value, byte[] buffer, int offset)
            where TPrimitive : struct =>
            typeof(TPrimitive) == typeof(bool) ? XmlConverter.ToChars((bool)(object)value!, buffer, offset)
                : XmlDictionaryWriter.IsInt32Primitive<TPrimitive>() ? XmlConverter.ToChars(XmlDictionaryWriter.GetInt32Primitive<TPrimitive, TValue>(value), buffer, offset)
                : typeof(TPrimitive) == typeof(uint) ? XmlConverter.ToChars((long)(uint)(object)value!, buffer, offset)
                : typeof(TPrimitive) == typeof(long) ? XmlConverter.ToChars((long)(object)value!, buffer, offset)
                : typeof(TPrimitive) == typeof(ulong) ? XmlConverter.ToChars((ulong)(object)value!, buffer, offset)
                : typeof(TPrimitive) == typeof(float) ? XmlConverter.ToChars((float)(object)value!, buffer, offset)
                : typeof(TPrimitive) == typeof(double) ? XmlConverter.ToChars((double)(object)value!, buffer, offset)
                : typeof(TPrimitive) == typeof(decimal) ? XmlConverter.ToChars((decimal)(object)value!, buffer, offset)
                : typeof(TPrimitive) == typeof(DateTime) ? XmlConverter.ToChars((DateTime)(object)value!, buffer, offset)
                : throw new UnreachableException();

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

        public override Task WriteStartListTextAsync() => Task.CompletedTask;

        public override void WriteListSeparator()
        {
            WriteByte(' ');
        }

        public override Task WriteListSeparatorAsync() => WriteByteAsync(' ');

        public override void WriteEndListText()
        {
        }

        public override Task WriteEndListTextAsync() => Task.CompletedTask;

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
