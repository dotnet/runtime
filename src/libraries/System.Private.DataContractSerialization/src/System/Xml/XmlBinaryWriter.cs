// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// Uncomment to turn on logging of non-dictionary strings written to binary writers.
// This can help identify element/attribute name/ns that could be written as XmlDictionaryStrings to get better compactness and performance.
// #define LOG_NON_DICTIONARY_WRITES

using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace System.Xml
{
    public interface IXmlBinaryWriterInitializer
    {
        void SetOutput(Stream stream, IXmlDictionary? dictionary, XmlBinaryWriterSession? session, bool ownsStream);
    }

    internal sealed class XmlBinaryNodeWriter : XmlStreamNodeWriter
    {
        private IXmlDictionary? _dictionary;
        private XmlBinaryWriterSession? _session;
        private bool _inAttribute;
        private bool _inList;
        private bool _wroteAttributeValue;
        private AttributeValue _attributeValue;
        private const int maxBytesPerChar = 3;
        private int _textNodeOffset;

        internal bool CanWriteBufferedContractElements => _dictionary is null && _session is null;

        internal static long GetNameMaximumBytes(string value) => 5 + (long)MaxUtf8BytesPerChar * value.Length;

        internal static long GetElementPrefixMaximumBytes(string prefix) =>
            prefix.Length == 0 || prefix.Length == 1 && char.IsAsciiLetterLower(prefix[0]) ? 0 : GetNameMaximumBytes(prefix);

        internal void WriteBufferedStartElement(string? prefix, byte[] localName)
        {
            Debug.Assert(CanWriteBufferedContractElements);
            if (localName.Length < 128 &&
                (string.IsNullOrEmpty(prefix) || prefix.Length == 1 && char.IsAsciiLetterLower(prefix[0])))
            {
                byte[] buffer = GetBuffer(2 + localName.Length, out int offset);
                buffer[offset] = string.IsNullOrEmpty(prefix)
                    ? (byte)XmlBinaryNodeType.ShortElement
                    : (byte)((int)XmlBinaryNodeType.PrefixElementA + prefix[0] - 'a');
                buffer[offset + 1] = (byte)localName.Length;
                localName.AsSpan().CopyTo(buffer.AsSpan(offset + 2));
                Advance(2 + localName.Length);
                _textNodeOffset = -1;
                return;
            }
            if (string.IsNullOrEmpty(prefix))
            {
                WriteNode(XmlBinaryNodeType.ShortElement);
            }
            else if (prefix.Length == 1 && char.IsAsciiLetterLower(prefix[0]))
            {
                WritePrefixNode(XmlBinaryNodeType.PrefixElementA, prefix[0] - 'a');
            }
            else
            {
                WriteNode(XmlBinaryNodeType.Element);
                WriteName(prefix);
            }
            WriteMultiByteInt32(localName.Length);
            WriteBytes(localName, 0, localName.Length);
        }

        internal static int GetPrimitiveBufferSize<T>() where T : struct =>
            typeof(T) == typeof(bool) ? 1 :
            XmlDictionaryWriter.IsInt32Primitive<T>() || typeof(T) == typeof(float) ? 1 + sizeof(int) :
            typeof(T) == typeof(uint) || typeof(T) == typeof(long) || typeof(T) == typeof(double) ||
            typeof(T) == typeof(DateTime) ? 1 + sizeof(long) :
            typeof(T) == typeof(decimal) ? 1 + sizeof(decimal) : 0;

        internal void WritePrimitiveValue<TPrimitive, TValue>(TValue value) where TPrimitive : struct
        {
            if (typeof(TPrimitive) == typeof(bool)) WriteBoolText((bool)(object)value!);
            else if (XmlDictionaryWriter.IsInt32Primitive<TPrimitive>()) WriteInt32Text(XmlDictionaryWriter.GetInt32Primitive<TPrimitive, TValue>(value));
            else if (typeof(TPrimitive) == typeof(uint)) WriteInt64Text((uint)(object)value!);
            else if (typeof(TPrimitive) == typeof(long)) WriteInt64Text((long)(object)value!);
            else if (typeof(TPrimitive) == typeof(float)) WriteFloatText((float)(object)value!);
            else if (typeof(TPrimitive) == typeof(double)) WriteDoubleText((double)(object)value!);
            else if (typeof(TPrimitive) == typeof(decimal)) WriteDecimalText((decimal)(object)value!);
            else if (typeof(TPrimitive) == typeof(DateTime)) WriteDateTimeText((DateTime)(object)value!);
            else throw new UnreachableException();
        }

        public XmlBinaryNodeWriter()
        {
            // Sanity check on node values
            Debug.Assert(XmlBinaryNodeType.MaxAttribute < XmlBinaryNodeType.MinElement &&
                                          XmlBinaryNodeType.MaxElement < XmlBinaryNodeType.MinText &&
                                          (int)XmlBinaryNodeType.MaxText < 256, "NodeTypes enumeration messed up");
        }

        public void SetOutput(Stream stream, IXmlDictionary? dictionary, XmlBinaryWriterSession? session, bool ownsStream)
        {
            _dictionary = dictionary;
            _session = session;
            _inAttribute = false;
            _inList = false;
            _attributeValue.Clear();
            _textNodeOffset = -1;
            SetOutput(stream, ownsStream, null);
        }

        private void WriteNode(XmlBinaryNodeType nodeType)
        {
            WriteByte((byte)nodeType);
            _textNodeOffset = -1;
        }

        private async Task WriteNodeAsync(XmlBinaryNodeType nodeType)
        {
            await WriteByteAsync((byte)nodeType).ConfigureAwait(false);
            _textNodeOffset = -1;
        }

        private async Task WriteMultiByteInt32Async(int value)
        {
            await EnsureBufferAsync(5).ConfigureAwait(false);
            WriteMultiByteInt32(value);
        }

        private async Task WriteNameAsync(string value)
        {
            if (value.Length < 128 / maxBytesPerChar)
            {
                await EnsureBufferAsync(1 + value.Length * maxBytesPerChar).ConfigureAwait(false);
                WriteName(value);
            }
            else
            {
                int byteCount = DataContractSerializer.ValidatingUTF8.GetByteCount(value);
                await WriteMultiByteInt32Async(byteCount).ConfigureAwait(false);
                await WriteUTF8CharsAsync(value.AsMemory()).ConfigureAwait(false);
            }
        }

        private void WroteAttributeValue()
        {
            if (_wroteAttributeValue && !_inList)
                throw new InvalidOperationException(SR.XmlOnlySingleValue);
            _wroteAttributeValue = true;
        }

        private void WriteTextNode(XmlBinaryNodeType nodeType)
        {
            if (_inAttribute)
                WroteAttributeValue();
            Debug.Assert(nodeType >= XmlBinaryNodeType.MinText && nodeType <= XmlBinaryNodeType.MaxText && ((byte)nodeType & 1) == 0, "Invalid nodeType");
            WriteByte((byte)nodeType);
            _textNodeOffset = this.BufferOffset - 1;
        }

        private byte[] GetTextNodeBuffer(int size, out int offset)
        {
            if (_inAttribute)
                WroteAttributeValue();
            byte[] buffer = GetBuffer(size, out offset);
            _textNodeOffset = offset;
            return buffer;
        }

        private void WriteTextNodeWithLength(XmlBinaryNodeType nodeType, int length)
        {
            Debug.Assert(nodeType == XmlBinaryNodeType.Chars8Text || nodeType == XmlBinaryNodeType.Bytes8Text || nodeType == XmlBinaryNodeType.UnicodeChars8Text);
            if (length < 256)
            {
                WriteTextNodeWithInt8(nodeType, unchecked((byte)length));
            }
            else if (length < 65536)
            {
                WriteTextNodeWithInt16(nodeType + /* WithEndElements interleave */ 1 * 2, unchecked((short)length));
            }
            else
            {
                WriteTextNodeWithInt32(nodeType + /* WithEndElements interleave */ 2 * 2, length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteTextNodeRaw<T>(XmlBinaryNodeType nodeType, T value)
            where T : unmanaged
        {
            // GetTextNodeBuffer performs bounds checks and ensures returned buffer has size of at least (1 + sizeof(T))
            byte[] buffer = GetTextNodeBuffer(1 + sizeof(T), out int offset);

            Debug.Assert(offset >= 0 && offset + 1 + sizeof(T) <= buffer.Length, "WriteTextNodeRaw");
            ref byte bytePtr = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buffer), offset);

            bytePtr = (byte)nodeType;
            Unsafe.WriteUnaligned<T>(ref Unsafe.Add(ref bytePtr, 1), value);
            Advance(1 + sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void WriteRaw<T>(T value)
            where T : unmanaged
        {
            // GetBuffer performs bounds checks and ensures returned buffer has size of at least (sizeof(T))
            byte[] buffer = GetBuffer(sizeof(T), out int offset);

            Debug.Assert(offset >= 0 && offset + sizeof(T) <= buffer.Length, "WriteRaw");
            ref byte bytePtr = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buffer), offset);
            Unsafe.WriteUnaligned<T>(ref bytePtr, value);
            Advance(sizeof(T));
        }

        private void WriteTextNodeWithInt8(XmlBinaryNodeType nodeType, byte value)
            => WriteTextNodeRaw<byte>(nodeType, value);

        private void WriteTextNodeWithInt16(XmlBinaryNodeType nodeType, short value)
           => WriteTextNodeRaw<short>(nodeType, BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness((value)));

        private void WriteTextNodeWithInt32(XmlBinaryNodeType nodeType, int value)
           => WriteTextNodeRaw<int>(nodeType, BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness((value)));

        private void WriteTextNodeWithInt64(XmlBinaryNodeType nodeType, long value)
           => WriteTextNodeRaw<long>(nodeType, BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness((value)));

        public override void WriteDeclaration()
        {
        }

        public override Task WriteDeclarationAsync() => Task.CompletedTask;

        public override async Task WriteStartElementAsync(string? prefix, string localName)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                await WriteNodeAsync(XmlBinaryNodeType.ShortElement).ConfigureAwait(false);
            }
            else if (prefix.Length == 1 && char.IsAsciiLetterLower(prefix[0]))
            {
                await WriteNodeAsync((XmlBinaryNodeType)((int)XmlBinaryNodeType.PrefixElementA + prefix[0] - 'a')).ConfigureAwait(false);
            }
            else
            {
                await WriteNodeAsync(XmlBinaryNodeType.Element).ConfigureAwait(false);
                await WriteNameAsync(prefix).ConfigureAwait(false);
            }
            await WriteNameAsync(localName).ConfigureAwait(false);
        }

        public override async Task WriteStartElementAsync(string? prefix, XmlDictionaryString localName)
        {
            if (!TryGetKey(localName, out int key))
            {
                await WriteStartElementAsync(prefix, localName.Value).ConfigureAwait(false);
                return;
            }
            if (string.IsNullOrEmpty(prefix))
            {
                await WriteNodeAsync(XmlBinaryNodeType.ShortDictionaryElement).ConfigureAwait(false);
            }
            else if (prefix.Length == 1 && char.IsAsciiLetterLower(prefix[0]))
            {
                await WriteNodeAsync((XmlBinaryNodeType)((int)XmlBinaryNodeType.PrefixDictionaryElementA + prefix[0] - 'a')).ConfigureAwait(false);
            }
            else
            {
                await WriteNodeAsync(XmlBinaryNodeType.DictionaryElement).ConfigureAwait(false);
                await WriteNameAsync(prefix).ConfigureAwait(false);
            }
            await WriteMultiByteInt32Async(key).ConfigureAwait(false);
        }

        public override void WriteStartElement(string? prefix, string localName)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                WriteNode(XmlBinaryNodeType.ShortElement);
                WriteName(localName);
            }
            else
            {
                char ch = prefix[0];

                if (prefix.Length == 1 && char.IsAsciiLetterLower(ch))
                {
                    WritePrefixNode(XmlBinaryNodeType.PrefixElementA, ch - 'a');
                    WriteName(localName);
                }
                else
                {
                    WriteNode(XmlBinaryNodeType.Element);
                    WriteName(prefix);
                    WriteName(localName);
                }
            }
        }

        private void WritePrefixNode(XmlBinaryNodeType nodeType, int ch)
        {
            WriteNode((XmlBinaryNodeType)((int)nodeType + ch));
        }

        public override void WriteStartElement(string? prefix, XmlDictionaryString localName)
        {
            int key;
            if (!TryGetKey(localName, out key))
            {
                WriteStartElement(prefix, localName.Value);
            }
            else
            {
                if (string.IsNullOrEmpty(prefix))
                {
                    WriteNode(XmlBinaryNodeType.ShortDictionaryElement);
                    WriteDictionaryString(key);
                }
                else
                {
                    char ch = prefix[0];

                    if (prefix.Length == 1 && char.IsAsciiLetterLower(ch))
                    {
                        WritePrefixNode(XmlBinaryNodeType.PrefixDictionaryElementA, ch - 'a');
                        WriteDictionaryString(key);
                    }
                    else
                    {
                        WriteNode(XmlBinaryNodeType.DictionaryElement);
                        WriteName(prefix);
                        WriteDictionaryString(key);
                    }
                }
            }
        }

        public override void WriteEndStartElement(bool isEmpty)
        {
            if (isEmpty)
            {
                WriteEndElement();
            }
        }

        public override void WriteEndElement(string? prefix, string localName)
        {
            WriteEndElement();
        }

        private void WriteEndElement()
        {
            if (_textNodeOffset != -1)
            {
                byte[] buffer = this.StreamBuffer;
                XmlBinaryNodeType nodeType = (XmlBinaryNodeType)buffer[_textNodeOffset];
                Debug.Assert(nodeType >= XmlBinaryNodeType.MinText && nodeType <= XmlBinaryNodeType.MaxText && ((byte)nodeType & 1) == 0);
                buffer[_textNodeOffset] = (byte)(nodeType + 1);
                _textNodeOffset = -1;
            }
            else
            {
                WriteNode(XmlBinaryNodeType.EndElement);
            }
        }

        public override Task WriteEndStartElementAsync(bool isEmpty) =>
            isEmpty ? WriteEndElementAsync(null, string.Empty) : Task.CompletedTask;

        public override Task WriteEndElementAsync(string? prefix, string localName)
        {
            if (_textNodeOffset != -1)
            {
                WriteEndElement();
                return Task.CompletedTask;
            }
            return WriteNodeAsync(XmlBinaryNodeType.EndElement);
        }

        public override void WriteStartAttribute(string prefix, string localName)
        {
            if (prefix.Length == 0)
            {
                WriteNode(XmlBinaryNodeType.ShortAttribute);
                WriteName(localName);
            }
            else
            {
                char ch = prefix[0];
                if (prefix.Length == 1 && char.IsAsciiLetterLower(ch))
                {
                    WritePrefixNode(XmlBinaryNodeType.PrefixAttributeA, ch - 'a');
                    WriteName(localName);
                }
                else
                {
                    WriteNode(XmlBinaryNodeType.Attribute);
                    WriteName(prefix);
                    WriteName(localName);
                }
            }
            _inAttribute = true;
            _wroteAttributeValue = false;
        }

        public override void WriteStartAttribute(string prefix, XmlDictionaryString localName)
        {
            int key;
            if (!TryGetKey(localName, out key))
            {
                WriteStartAttribute(prefix, localName.Value);
            }
            else
            {
                if (prefix.Length == 0)
                {
                    WriteNode(XmlBinaryNodeType.ShortDictionaryAttribute);
                    WriteDictionaryString(key);
                }
                else
                {
                    char ch = prefix[0];
                    if (prefix.Length == 1 && char.IsAsciiLetterLower(ch))
                    {
                        WritePrefixNode(XmlBinaryNodeType.PrefixDictionaryAttributeA, ch - 'a');
                        WriteDictionaryString(key);
                    }
                    else
                    {
                        WriteNode(XmlBinaryNodeType.DictionaryAttribute);
                        WriteName(prefix);
                        WriteDictionaryString(key);
                    }
                }
                _inAttribute = true;
                _wroteAttributeValue = false;
            }
        }

        public override void WriteEndAttribute()
        {
            _inAttribute = false;
            if (!_wroteAttributeValue)
            {
                _attributeValue.WriteTo(this);
            }
            _textNodeOffset = -1;
        }

        public override async Task WriteStartAttributeAsync(string prefix, string localName)
        {
            if (prefix.Length == 0)
            {
                await WriteNodeAsync(XmlBinaryNodeType.ShortAttribute).ConfigureAwait(false);
            }
            else if (prefix.Length == 1 && char.IsAsciiLetterLower(prefix[0]))
            {
                await WriteNodeAsync((XmlBinaryNodeType)((int)XmlBinaryNodeType.PrefixAttributeA + prefix[0] - 'a')).ConfigureAwait(false);
            }
            else
            {
                await WriteNodeAsync(XmlBinaryNodeType.Attribute).ConfigureAwait(false);
                await WriteNameAsync(prefix).ConfigureAwait(false);
            }
            await WriteNameAsync(localName).ConfigureAwait(false);
            _inAttribute = true;
            _wroteAttributeValue = false;
        }

        public override async Task WriteStartAttributeAsync(string prefix, XmlDictionaryString localName)
        {
            if (!TryGetKey(localName, out int key))
            {
                await WriteStartAttributeAsync(prefix, localName.Value).ConfigureAwait(false);
                return;
            }
            if (prefix.Length == 0)
            {
                await WriteNodeAsync(XmlBinaryNodeType.ShortDictionaryAttribute).ConfigureAwait(false);
            }
            else if (prefix.Length == 1 && char.IsAsciiLetterLower(prefix[0]))
            {
                await WriteNodeAsync((XmlBinaryNodeType)((int)XmlBinaryNodeType.PrefixDictionaryAttributeA + prefix[0] - 'a')).ConfigureAwait(false);
            }
            else
            {
                await WriteNodeAsync(XmlBinaryNodeType.DictionaryAttribute).ConfigureAwait(false);
                await WriteNameAsync(prefix).ConfigureAwait(false);
            }
            await WriteMultiByteInt32Async(key).ConfigureAwait(false);
            _inAttribute = true;
            _wroteAttributeValue = false;
        }

        public override async Task WriteEndAttributeAsync()
        {
            _inAttribute = false;
            if (!_wroteAttributeValue)
            {
                if (_attributeValue.Text is string text)
                    await WriteTextAsync(text).ConfigureAwait(false);
                else if (_attributeValue.DictionaryText is XmlDictionaryString dictionaryText)
                    await WriteTextAsync(dictionaryText).ConfigureAwait(false);
                else if (_attributeValue.TryGetBytes(out ArraySegment<byte> bytes))
                    await WriteBase64TextAsync(Array.Empty<byte>(), 0, bytes.Array!, bytes.Offset, bytes.Count).ConfigureAwait(false);
                else
                {
                    await EnsureBufferAsync(1).ConfigureAwait(false);
                    WriteEmptyText();
                }
                _attributeValue.Clear();
            }
            _textNodeOffset = -1;
        }

        public override void WriteXmlnsAttribute(string? prefix, string ns)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                WriteNode(XmlBinaryNodeType.ShortXmlnsAttribute);
                WriteName(ns);
            }
            else
            {
                WriteNode(XmlBinaryNodeType.XmlnsAttribute);
                WriteName(prefix);
                WriteName(ns);
            }
        }

        public override void WriteXmlnsAttribute(string? prefix, XmlDictionaryString ns)
        {
            int key;
            if (!TryGetKey(ns, out key))
            {
                WriteXmlnsAttribute(prefix, ns.Value);
            }
            else
            {
                if (string.IsNullOrEmpty(prefix))
                {
                    WriteNode(XmlBinaryNodeType.ShortDictionaryXmlnsAttribute);
                    WriteDictionaryString(key);
                }
                else
                {
                    WriteNode(XmlBinaryNodeType.DictionaryXmlnsAttribute);
                    WriteName(prefix);
                    WriteDictionaryString(key);
                }
            }
        }

        public override async Task WriteXmlnsAttributeAsync(string? prefix, string ns)
        {
            await WriteNodeAsync(string.IsNullOrEmpty(prefix) ? XmlBinaryNodeType.ShortXmlnsAttribute : XmlBinaryNodeType.XmlnsAttribute).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(prefix))
                await WriteNameAsync(prefix).ConfigureAwait(false);
            await WriteNameAsync(ns).ConfigureAwait(false);
        }

        public override async Task WriteXmlnsAttributeAsync(string? prefix, XmlDictionaryString ns)
        {
            if (!TryGetKey(ns, out int key))
            {
                await WriteXmlnsAttributeAsync(prefix, ns.Value).ConfigureAwait(false);
                return;
            }
            await WriteNodeAsync(string.IsNullOrEmpty(prefix) ? XmlBinaryNodeType.ShortDictionaryXmlnsAttribute : XmlBinaryNodeType.DictionaryXmlnsAttribute).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(prefix))
                await WriteNameAsync(prefix).ConfigureAwait(false);
            await WriteMultiByteInt32Async(key).ConfigureAwait(false);
        }

        private bool TryGetKey(XmlDictionaryString s, out int key)
        {
            key = -1;
            if (s.Dictionary == _dictionary)
            {
                key = s.Key * 2;
                return true;
            }
            XmlDictionaryString? t;
            if (_dictionary != null && _dictionary.TryLookup(s, out t))
            {
                Debug.Assert(t.Dictionary == _dictionary);
                key = t.Key * 2;
                return true;
            }

            if (_session == null)
                return false;
            int sessionKey;
            if (!_session.TryLookup(s, out sessionKey))
            {
                if (!_session.TryAdd(s, out sessionKey))
                    return false;
            }
            key = sessionKey * 2 + 1;
            return true;
        }

        private void WriteDictionaryString(int key)
        {
            WriteMultiByteInt32(key);
        }

        private unsafe void WriteName(string s)
        {
            int length = s.Length;
            if (length == 0)
            {
                WriteByte(0);
            }
            else
            {
                fixed (char* pch = s)
                {
                    UnsafeWriteName(pch, length);
                }
            }
        }

        private unsafe void UnsafeWriteName(char* chars, int charCount)
        {
            if (charCount < 128 / maxBytesPerChar)
            {
                // Optimize if we know we can fit the converted string in the buffer
                // so we don't have to make a pass to count the bytes

                // 1 byte for the length
                int offset;
                byte[] buffer = GetBuffer(1 + charCount * maxBytesPerChar, out offset);
                int length = UnsafeGetUTF8Chars(chars, charCount, buffer, offset + 1);
                Debug.Assert(length < 128);
                buffer[offset] = (byte)length;
                Advance(1 + length);
            }
            else
            {
                int byteCount = UnsafeGetUTF8Length(chars, charCount);
                WriteMultiByteInt32(byteCount);
                UnsafeWriteUTF8Chars(chars, charCount);
            }
        }

        private void WriteMultiByteInt32(int i)
        {
            int offset;
            byte[] buffer = GetBuffer(5, out offset);

            int startOffset = offset;
            while ((i & 0xFFFFFF80) != 0)
            {
                buffer[offset++] = (byte)((i & 0x7F) | 0x80);
                i >>= 7;
            }
            buffer[offset++] = (byte)i;
            Advance(offset - startOffset);
        }

        public override void WriteComment(string value)
        {
            WriteNode(XmlBinaryNodeType.Comment);
            WriteName(value);
        }

        public override void WriteCData(string value)
        {
            WriteText(value);
        }

        public override async Task WriteCommentAsync(string text)
        {
            await WriteNodeAsync(XmlBinaryNodeType.Comment).ConfigureAwait(false);
            await WriteNameAsync(text).ConfigureAwait(false);
        }

        public override Task WriteCDataAsync(string text) => WriteTextAsync(text);

        private void WriteEmptyText()
        {
            WriteTextNode(XmlBinaryNodeType.EmptyText);
        }

        public override void WriteBoolText(bool value)
        {
            if (value)
            {
                WriteTextNode(XmlBinaryNodeType.TrueText);
            }
            else
            {
                WriteTextNode(XmlBinaryNodeType.FalseText);
            }
        }

        public override void WriteInt32Text(int value)
        {
            if (value == (sbyte)value)
            {
                if (value == 0)
                {
                    WriteTextNode(XmlBinaryNodeType.ZeroText);
                }
                else if (value == 1)
                {
                    WriteTextNode(XmlBinaryNodeType.OneText);
                }
                else
                {
                    WriteTextNodeWithInt8(XmlBinaryNodeType.Int8Text, (byte)value);
                }
            }
            else if (value == (short)value)
            {
                WriteTextNodeWithInt16(XmlBinaryNodeType.Int16Text, (short)value);
            }
            else
            {
                WriteTextNodeWithInt32(XmlBinaryNodeType.Int32Text, value);
            }
        }

        public override void WriteInt64Text(long value)
        {
            if (value == (int)(value))
            {
                WriteInt32Text((int)value);
            }
            else
            {
                WriteTextNodeWithInt64(XmlBinaryNodeType.Int64Text, value);
            }
        }

        public override void WriteUInt64Text(ulong value)
        {
            if (value <= long.MaxValue)
            {
                WriteInt64Text((long)value);
            }
            else
            {
                WriteTextNodeWithInt64(XmlBinaryNodeType.UInt64Text, (long)value);
            }
        }

        private void WriteInt64(long value)
            => WriteRaw<long>(BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value));

        public override void WriteBase64Text(byte[]? trailBytes, int trailByteCount, byte[] base64Buffer, int base64Offset, int base64Count)
        {
            if (_inAttribute)
            {
                _attributeValue.WriteBase64Text(trailBytes, trailByteCount, base64Buffer, base64Offset, base64Count);
            }
            else
            {
                int length = trailByteCount + base64Count;
                if (length > 0)
                {
                    WriteTextNodeWithLength(XmlBinaryNodeType.Bytes8Text, length);
                    if (trailByteCount > 0)
                    {
                        int offset;
                        byte[] buffer = GetBuffer(trailByteCount, out offset);
                        for (int i = 0; i < trailByteCount; i++)
                            buffer[offset + i] = trailBytes![i];
                        Advance(trailByteCount);
                    }
                    if (base64Count > 0)
                    {
                        WriteBytes(base64Buffer, base64Offset, base64Count);
                    }
                }
                else
                {
                    WriteEmptyText();
                }
            }
        }

        private async Task WriteTextNodeWithLengthAsync(XmlBinaryNodeType nodeType, int length)
        {
            await EnsureBufferAsync(length < 256 ? 2 : length < 65536 ? 3 : 5).ConfigureAwait(false);
            WriteTextNodeWithLength(nodeType, length);
        }

        public override async Task WriteBase64TextAsync(byte[] trailBytes, int trailByteCount, byte[] buffer, int offset, int count)
        {
            if (_inAttribute)
            {
                _attributeValue.WriteBase64Text(trailBytes, trailByteCount, buffer, offset, count);
                return;
            }
            int length = checked(trailByteCount + count);
            if (length == 0)
            {
                await EnsureBufferAsync(1).ConfigureAwait(false);
                WriteEmptyText();
                return;
            }
            await WriteTextNodeWithLengthAsync(XmlBinaryNodeType.Bytes8Text, length).ConfigureAwait(false);
            if (trailByteCount != 0)
                await WriteBytesAsync(trailBytes.AsMemory(0, trailByteCount)).ConfigureAwait(false);
            if (count != 0)
                await WriteBytesAsync(buffer.AsMemory(offset, count)).ConfigureAwait(false);
        }

        public override void WriteText(XmlDictionaryString value)
        {
            if (_inAttribute)
            {
                _attributeValue.WriteText(value);
            }
            else
            {
                int key;
                if (!TryGetKey(value, out key))
                {
                    WriteText(value.Value);
                }
                else
                {
                    WriteTextNode(XmlBinaryNodeType.DictionaryText);
                    WriteDictionaryString(key);
                }
            }
        }

        public override async Task WriteTextAsync(XmlDictionaryString value)
        {
            if (_inAttribute)
            {
                _attributeValue.WriteText(value);
                return;
            }
            if (!TryGetKey(value, out int key))
            {
                await WriteTextAsync(value.Value).ConfigureAwait(false);
                return;
            }
            await EnsureBufferAsync(1).ConfigureAwait(false);
            WriteTextNode(XmlBinaryNodeType.DictionaryText);
            await WriteMultiByteInt32Async(key).ConfigureAwait(false);
        }

        public override async Task WriteTextAsync(ReadOnlyMemory<char> value)
        {
            if (_inAttribute)
            {
                _attributeValue.WriteText(value.ToString());
                return;
            }
            if (value.Length <= byte.MaxValue / maxBytesPerChar)
            {
                int size = value.Length == 0 || (value.Length == 1 && value.Span[0] is '0' or '1')
                    ? 1 : 2 + value.Length * maxBytesPerChar;
                await EnsureBufferAsync(size).ConfigureAwait(false);
                WriteTextImpl(value.Span);
                return;
            }
            int byteCount = DataContractSerializer.ValidatingUTF8.GetByteCount(value.Span);
            if (byteCount / 2 > value.Length)
            {
                await WriteTextNodeWithLengthAsync(XmlBinaryNodeType.UnicodeChars8Text, checked(value.Length * sizeof(char))).ConfigureAwait(false);
                await WriteUnicodeCharsAsync(value).ConfigureAwait(false);
            }
            else
            {
                await WriteTextNodeWithLengthAsync(XmlBinaryNodeType.Chars8Text, byteCount).ConfigureAwait(false);
                await WriteUTF8CharsAsync(value).ConfigureAwait(false);
            }
        }

        public override async Task WriteTextAsync(ReadOnlyMemory<byte> value)
        {
            await WriteTextNodeWithLengthAsync(XmlBinaryNodeType.Chars8Text, value.Length).ConfigureAwait(false);
            await WriteBytesAsync(value).ConfigureAwait(false);
        }

        public override void WriteText(string value)
            => WriteTextImpl(value);

        public override void WriteText(char[] chars, int offset, int count)
           => WriteTextImpl(chars.AsSpan(offset, count));

        // We need a separate implementation method which is not an override
        // so that overload resolution will call string based method instead of ReadOnlySpan
        // when called with a string argument
        private unsafe void WriteTextImpl(string value)
        {
            if (_inAttribute)
            {
                _attributeValue.WriteText(value);
            }
            else
            {
                if (value.Length > 0)
                {
                    fixed (char* pch = value)
                    {
                        UnsafeWriteText(pch, value.Length);
                    }
                }
                else
                {
                    WriteEmptyText();
                }
            }
        }

        private unsafe void WriteTextImpl(ReadOnlySpan<char> chars)
        {
            if (_inAttribute)
            {
                _attributeValue.WriteText(chars.ToString());
            }
            else
            {
                if (chars.Length > 0)
                {
                    fixed (char* pch = &MemoryMarshal.GetReference(chars))
                    {
                        UnsafeWriteText(pch, chars.Length);
                    }
                }
                else
                {
                    WriteEmptyText();
                }
            }
        }

        public override void WriteText(byte[] chars, int charOffset, int charCount)
        {
            WriteTextNodeWithLength(XmlBinaryNodeType.Chars8Text, charCount);
            WriteBytes(chars, charOffset, charCount);
        }

        private unsafe void UnsafeWriteText(char* chars, int charCount)
        {
            // Callers should handle zero
            Debug.Assert(charCount > 0);

            if (charCount == 1)
            {
                char ch = chars[0];
                if (ch == '0')
                {
                    WriteTextNode(XmlBinaryNodeType.ZeroText);
                    return;
                }
                if (ch == '1')
                {
                    WriteTextNode(XmlBinaryNodeType.OneText);
                    return;
                }
            }

            if (charCount <= byte.MaxValue / maxBytesPerChar)
            {
                // Optimize if we know we can fit the converted string in the buffer
                // so we don't have to make a pass to count the bytes

                int offset;
                byte[] buffer = GetBuffer(1 + 1 + charCount * maxBytesPerChar, out offset);
                int length = UnsafeGetUTF8Chars(chars, charCount, buffer, offset + 2);

                if (length / 2 <= charCount)
                {
                    buffer[offset] = (byte)XmlBinaryNodeType.Chars8Text;
                }
                else
                {
                    buffer[offset] = (byte)XmlBinaryNodeType.UnicodeChars8Text;
                    length = UnsafeGetUnicodeChars(chars, charCount, buffer, offset + 2);
                }
                _textNodeOffset = offset;
                Debug.Assert(length <= byte.MaxValue);
                buffer[offset + 1] = (byte)length;
                Advance(2 + length);
            }
            else
            {
                int byteCount = UnsafeGetUTF8Length(chars, charCount);
                if (byteCount / 2 > charCount)
                {
                    WriteTextNodeWithLength(XmlBinaryNodeType.UnicodeChars8Text, charCount * 2);
                    UnsafeWriteUnicodeChars(chars, charCount);
                }
                else
                {
                    WriteTextNodeWithLength(XmlBinaryNodeType.Chars8Text, byteCount);
                    UnsafeWriteUTF8Chars(chars, charCount);
                }
            }
        }

        public override void WriteEscapedText(string value)
        {
            WriteText(value);
        }

        public override void WriteEscapedText(XmlDictionaryString value)
        {
            WriteText(value);
        }

        public override void WriteEscapedText(char[] chars, int offset, int count)
        {
            WriteText(chars, offset, count);
        }

        public override void WriteEscapedText(byte[] chars, int offset, int count)
        {
            WriteText(chars, offset, count);
        }

        public override Task WriteEscapedTextAsync(ReadOnlyMemory<char> value) => WriteTextAsync(value);
        public override Task WriteEscapedTextAsync(ReadOnlyMemory<byte> value) => WriteTextAsync(value);
        public override Task WriteEscapedTextAsync(XmlDictionaryString value) => WriteTextAsync(value);

        public override void WriteCharEntity(int ch)
        {
            if (ch > char.MaxValue)
            {
                SurrogateChar sch = new SurrogateChar(ch);
                WriteTextImpl([sch.HighChar, sch.LowChar]);
            }
            else
            {
                char castChar = (char)ch;
                WriteTextImpl(new ReadOnlySpan<char>(in castChar));
            }
        }

        public override Task WriteCharEntityAsync(int ch)
        {
            if (ch > char.MaxValue)
            {
                _ = new SurrogateChar(ch);
            }
            return WriteCharEntityCoreAsync(ch);
        }

        private async Task WriteCharEntityCoreAsync(int ch)
        {
            if (!_inAttribute)
            {
                int size = ch is '0' or '1' ? 1 : 2 + (ch > char.MaxValue ? 2 : 1) * maxBytesPerChar;
                await EnsureBufferAsync(size).ConfigureAwait(false);
            }
            WriteCharEntity(ch);
        }

        private static int GetInt32Size(int value) =>
            value is 0 or 1 ? 1 : value == (sbyte)value ? 2 : value == (short)value ? 3 : 5;

        private static int GetInt64Size(long value) => value == (int)value ? GetInt32Size((int)value) : 9;

        private static int GetFloatSize(float value) =>
            value >= short.MinValue && value <= short.MaxValue && (int)value == value ? GetInt32Size((int)value) : 5;

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
            Guid => WritePrimitiveValueAsync<Guid>(value),
            TimeSpan => WritePrimitiveValueAsync<TimeSpan>(value),
            UniqueId uniqueId when uniqueId.IsGuid => WriteUniqueIdValueAsync(uniqueId),
            _ => WriteTextAsync(XmlDictionaryWriter.GetValueText(value))
        };

        private async Task WriteUniqueIdValueAsync(UniqueId value)
        {
            await EnsureBufferAsync(17).ConfigureAwait(false);
            WriteUniqueIdText(value);
        }

        public override async Task WritePrimitiveValueAsync<T>(object value)
        {
            int size = typeof(T) == typeof(bool) ? 1
                : XmlDictionaryWriter.IsInt32Primitive<T>() ? GetInt32Size(XmlDictionaryWriter.GetInt32Primitive<T>(value))
                : typeof(T) == typeof(uint) ? GetInt64Size((uint)value)
                : typeof(T) == typeof(long) ? GetInt64Size((long)value)
                : typeof(T) == typeof(ulong) ? (ulong)value <= long.MaxValue ? GetInt64Size((long)(ulong)value) : 9
                : typeof(T) == typeof(float) ? GetFloatSize((float)value)
                : typeof(T) == typeof(double) ? (float)(double)value == (double)value ? GetFloatSize((float)(double)value) : 9
                : typeof(T) == typeof(decimal) || typeof(T) == typeof(Guid) ? 17
                : typeof(T) == typeof(DateTime) || typeof(T) == typeof(TimeSpan) ? 9
                : 0;
            if (size == 0)
            {
                await WriteTextAsync(XmlDictionaryWriter.GetPrimitiveValueText<T>(value)).ConfigureAwait(false);
                return;
            }
            await EnsureBufferAsync(size).ConfigureAwait(false);
            if (typeof(T) == typeof(bool)) WriteBoolText((bool)value);
            else if (XmlDictionaryWriter.IsInt32Primitive<T>()) WriteInt32Text(XmlDictionaryWriter.GetInt32Primitive<T>(value));
            else if (typeof(T) == typeof(uint)) WriteInt64Text((uint)value);
            else if (typeof(T) == typeof(long)) WriteInt64Text((long)value);
            else if (typeof(T) == typeof(ulong)) WriteUInt64Text((ulong)value);
            else if (typeof(T) == typeof(float)) WriteFloatText((float)value);
            else if (typeof(T) == typeof(double)) WriteDoubleText((double)value);
            else if (typeof(T) == typeof(decimal)) WriteDecimalText((decimal)value);
            else if (typeof(T) == typeof(DateTime)) WriteDateTimeText((DateTime)value);
            else if (typeof(T) == typeof(Guid)) WriteGuidText((Guid)value);
            else if (typeof(T) == typeof(TimeSpan)) WriteTimeSpanText((TimeSpan)value);
        }

        public override void WriteFloatText(float f)
        {
            int i;
            // Only write as an integer-type if it would save space. A full Int32 value doesn't.
            if (f >= short.MinValue && f <= short.MaxValue && (i = (int)f) == f)
            {
                WriteInt32Text(i);
            }
            else
            {
                if (BitConverter.IsLittleEndian)
                {
                    WriteTextNodeRaw(XmlBinaryNodeType.FloatText, f);
                }
                else
                {
                    Span<byte> span = GetTextNodeBuffer(1 + sizeof(float), out int offset).AsSpan(offset, 1 + sizeof(float));
                    span[0] = (byte)XmlBinaryNodeType.FloatText;
                    BinaryPrimitives.WriteSingleLittleEndian(span.Slice(1), f);
                    Advance(1 + sizeof(float));
                }
            }
        }

        public override void WriteDoubleText(double d)
        {
            float f;

            // If we can save space by writing as a 32-bit float, do that.
            if ((f = (float)d) == d)
            {
                WriteFloatText(f);
            }
            else
            {
                if (BitConverter.IsLittleEndian)
                {
                    WriteTextNodeRaw(XmlBinaryNodeType.DoubleText, d);
                }
                else
                {
                    Span<byte> span = GetTextNodeBuffer(1 + sizeof(double), out int offset).AsSpan(offset, 1 + sizeof(double));
                    span[0] = (byte)XmlBinaryNodeType.DoubleText;
                    BinaryPrimitives.WriteDoubleLittleEndian(span.Slice(1), d);
                    Advance(1 + sizeof(double));
                }
            }
        }

        public override void WriteDecimalText(decimal d)
        {
            if (BitConverter.IsLittleEndian)
            {
                WriteTextNodeRaw(XmlBinaryNodeType.DecimalText, d);
            }
            else
            {
                Span<int> bits = [0, 0, 0, 0];
                decimal.TryGetBits(d, bits, out int intsWritten);
                Debug.Assert(intsWritten == 4);

                Span<byte> span = GetTextNodeBuffer(1 + sizeof(decimal), out int offset).AsSpan(offset, 1 + sizeof(decimal));
                span[0] = (byte)XmlBinaryNodeType.DecimalText;
                BinaryPrimitives.WriteInt32LittleEndian(span.Slice(0 + 1), bits[3]);
                BinaryPrimitives.WriteInt32LittleEndian(span.Slice(4 + 1), bits[2]);
                BinaryPrimitives.WriteInt32LittleEndian(span.Slice(8 + 1), bits[0]);
                BinaryPrimitives.WriteInt32LittleEndian(span.Slice(12 + 1), bits[1]);
                Advance(1 + sizeof(decimal));
            }
        }

        public override void WriteDateTimeText(DateTime dt)
            => WriteTextNodeWithInt64(XmlBinaryNodeType.DateTimeText, dt.ToBinary());


        public override void WriteUniqueIdText(UniqueId value)
        {
            if (value.IsGuid)
            {
                int offset;
                byte[] buffer = GetTextNodeBuffer(17, out offset);
                buffer[offset] = (byte)XmlBinaryNodeType.UniqueIdText;
                value.TryGetGuid(buffer, offset + 1);
                Advance(17);
            }
            else
            {
                WriteText(value.ToString());
            }
        }

        public override void WriteGuidText(Guid guid)
        {
            Span<byte> span = GetTextNodeBuffer(17, out int offset).AsSpan(offset, 17);
            span[0] = (byte)XmlBinaryNodeType.GuidText;
            guid.TryWriteBytes(span.Slice(1));
            Advance(17);
        }

        public override void WriteTimeSpanText(TimeSpan value)
        {
            WriteTextNodeWithInt64(XmlBinaryNodeType.TimeSpanText, value.Ticks);
        }

        public override void WriteStartListText()
        {
            Debug.Assert(!_inList);
            _inList = true;
            WriteNode(XmlBinaryNodeType.StartListText);
        }

        public override void WriteListSeparator()
        {
        }

        public override void WriteEndListText()
        {
            Debug.Assert(_inList);
            _inList = false;
            _wroteAttributeValue = true;
            WriteNode(XmlBinaryNodeType.EndListText);
        }

        public override async Task WriteStartListTextAsync()
        {
            await EnsureBufferAsync(1).ConfigureAwait(false);
            WriteStartListText();
        }

        public override Task WriteListSeparatorAsync() => Task.CompletedTask;

        public override async Task WriteEndListTextAsync()
        {
            await EnsureBufferAsync(1).ConfigureAwait(false);
            WriteEndListText();
        }

        public Task WriteArrayNodeAsync() => WriteNodeAsync(XmlBinaryNodeType.Array);

        public Task WriteArrayAsync(Array array, int offset, int count)
        {
            (XmlBinaryNodeType nodeType, int size) = GetArrayTypeInfo(array);
            if ((long)count * size + 6 <= StreamBuffer.Length - BufferOffset)
            {
                WriteBufferedArray(array, offset, count, nodeType);
                return Task.CompletedTask;
            }

            return WriteArrayAsyncCore(array, offset, count, nodeType, size);
        }

        internal static (XmlBinaryNodeType NodeType, int Size) GetArrayTypeInfo(Array array) =>
            array switch
            {
                bool[] => (XmlBinaryNodeType.BoolTextWithEndElement, sizeof(byte)),
                short[] => (XmlBinaryNodeType.Int16TextWithEndElement, sizeof(short)),
                int[] => (XmlBinaryNodeType.Int32TextWithEndElement, sizeof(int)),
                long[] => (XmlBinaryNodeType.Int64TextWithEndElement, sizeof(long)),
                float[] => (XmlBinaryNodeType.FloatTextWithEndElement, sizeof(float)),
                double[] => (XmlBinaryNodeType.DoubleTextWithEndElement, sizeof(double)),
                decimal[] => (XmlBinaryNodeType.DecimalTextWithEndElement, 16),
                DateTime[] => (XmlBinaryNodeType.DateTimeTextWithEndElement, sizeof(long)),
                Guid[] => (XmlBinaryNodeType.GuidTextWithEndElement, 16),
                TimeSpan[] => (XmlBinaryNodeType.TimeSpanTextWithEndElement, sizeof(long)),
                _ => throw new NotSupportedException(SR.Format(SR.XmlMethodNotSupported, nameof(WriteArrayAsync)))
            };

        internal void WriteBufferedArray(Array array, int offset, int count, XmlBinaryNodeType nodeType)
        {
            WriteArrayInfo(nodeType, count);
            WriteArrayBlock(array, offset, count);
        }

        private async Task WriteArrayAsyncCore(Array array, int offset, int count, XmlBinaryNodeType nodeType, int size)
        {
            await WriteNodeAsync(nodeType).ConfigureAwait(false);
            await WriteMultiByteInt32Async(count).ConfigureAwait(false);
            while (count > 0)
            {
                int chunk = Math.Min(StreamBuffer.Length / size, count);
                await EnsureBufferAsync(chunk * size).ConfigureAwait(false);
                WriteArrayBlock(array, offset, chunk);
                offset += chunk;
                count -= chunk;
            }
        }

        private void WriteRawArrayBlock<T>(T[] array, int offset, int count) where T : unmanaged
        {
            ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(array.AsSpan(offset, count));
            byte[] destination = GetBuffer(bytes.Length, out int destinationOffset);
            bytes.CopyTo(destination.AsSpan(destinationOffset));
            Advance(bytes.Length);
        }

        private void WriteArrayBlock(Array array, int offset, int count)
        {
            switch (array)
            {
                case bool[] values: WriteRawArrayBlock(values, offset, count); break;
                case short[] values when BitConverter.IsLittleEndian: WriteRawArrayBlock(values, offset, count); break;
                case int[] values when BitConverter.IsLittleEndian: WriteRawArrayBlock(values, offset, count); break;
                case long[] values when BitConverter.IsLittleEndian: WriteRawArrayBlock(values, offset, count); break;
                case float[] values when BitConverter.IsLittleEndian: WriteRawArrayBlock(values, offset, count); break;
                case double[] values when BitConverter.IsLittleEndian: WriteRawArrayBlock(values, offset, count); break;
                case decimal[] values when BitConverter.IsLittleEndian: WriteRawArrayBlock(values, offset, count); break;
                case Guid[] values when BitConverter.IsLittleEndian: WriteRawArrayBlock(values, offset, count); break;
                case short[] values:
                    foreach (short value in values.AsSpan(offset, count))
                        WriteRaw(BinaryPrimitives.ReverseEndianness(value));
                    break;
                case int[] values:
                    foreach (int value in values.AsSpan(offset, count))
                        WriteRaw(BinaryPrimitives.ReverseEndianness(value));
                    break;
                case long[] values:
                    foreach (long value in values.AsSpan(offset, count))
                        WriteInt64(value);
                    break;
                case float[] values:
                    foreach (float value in values.AsSpan(offset, count))
                    {
                        BinaryPrimitives.WriteSingleLittleEndian(GetBuffer(sizeof(float), out int position).AsSpan(position), value);
                        Advance(sizeof(float));
                    }
                    break;
                case double[] values:
                    foreach (double value in values.AsSpan(offset, count))
                    {
                        BinaryPrimitives.WriteDoubleLittleEndian(GetBuffer(sizeof(double), out int position).AsSpan(position), value);
                        Advance(sizeof(double));
                    }
                    break;
                case decimal[] values:
                    Span<int> bits = stackalloc int[4];
                    foreach (decimal value in values.AsSpan(offset, count))
                    {
                        decimal.TryGetBits(value, bits, out _);
                        Span<byte> destination = GetBuffer(16, out int position).AsSpan(position, 16);
                        BinaryPrimitives.WriteInt32LittleEndian(destination, bits[3]);
                        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(4), bits[2]);
                        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(8), bits[0]);
                        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(12), bits[1]);
                        Advance(16);
                    }
                    break;
                case DateTime[] values:
                    foreach (DateTime value in values.AsSpan(offset, count))
                        WriteInt64(value.ToBinary());
                    break;
                case Guid[] values:
                    foreach (Guid value in values.AsSpan(offset, count))
                    {
                        value.TryWriteBytes(GetBuffer(16, out int position).AsSpan(position, 16));
                        Advance(16);
                    }
                    break;
                case TimeSpan[] values:
                    foreach (TimeSpan value in values.AsSpan(offset, count))
                        WriteInt64(value.Ticks);
                    break;
            }
        }

        public void WriteArrayNode()
        {
            WriteNode(XmlBinaryNodeType.Array);
        }

        private void WriteArrayInfo(XmlBinaryNodeType nodeType, int count)
        {
            WriteNode(nodeType);
            WriteMultiByteInt32(count);
        }

        public void WriteArray(XmlBinaryNodeType nodeType, int count, ReadOnlySpan<byte> bytes)
        {
            WriteArrayInfo(nodeType, count);
            base.WriteBytes(bytes);
        }

        public void WriteBoolArray(ReadOnlySpan<bool> items)
        {
            WriteArray(XmlBinaryNodeType.BoolTextWithEndElement, items.Length, MemoryMarshal.AsBytes(items));
        }

        public void WriteInt16Array(ReadOnlySpan<short> items)
        {
            if (BitConverter.IsLittleEndian)
            {
                WriteArray(XmlBinaryNodeType.Int16TextWithEndElement, items.Length, MemoryMarshal.AsBytes(items));
            }
            else
            {
                WriteArrayInfo(XmlBinaryNodeType.Int16TextWithEndElement, items.Length);
                foreach (short s in items)
                {
                    WriteRaw(BinaryPrimitives.ReverseEndianness(s));
                }
            }
        }

        public void WriteInt32Array(ReadOnlySpan<int> items)
        {
            if (BitConverter.IsLittleEndian)
            {
                WriteArray(XmlBinaryNodeType.Int32TextWithEndElement, items.Length, MemoryMarshal.AsBytes(items));
            }
            else
            {
                WriteArrayInfo(XmlBinaryNodeType.Int32TextWithEndElement, items.Length);
                foreach (int i in items)
                {
                    WriteRaw(BinaryPrimitives.ReverseEndianness(i));
                }
            }
        }

        public void WriteInt64Array(ReadOnlySpan<long> items)
        {
            if (BitConverter.IsLittleEndian)
            {
                WriteArray(XmlBinaryNodeType.Int64TextWithEndElement, items.Length, MemoryMarshal.AsBytes(items));
            }
            else
            {
                WriteArrayInfo(XmlBinaryNodeType.Int64TextWithEndElement, items.Length);
                foreach (long l in items)
                {
                    WriteRaw(BinaryPrimitives.ReverseEndianness(l));
                }
            }
        }

        public void WriteFloatArray(ReadOnlySpan<float> items)
        {
            if (BitConverter.IsLittleEndian)
            {
                WriteArray(XmlBinaryNodeType.FloatTextWithEndElement, items.Length, MemoryMarshal.AsBytes(items));
            }
            else
            {
                WriteArrayInfo(XmlBinaryNodeType.FloatTextWithEndElement, items.Length);
                foreach (float f in items)
                {
                    Span<byte> span = GetBuffer(sizeof(float), out int bufferOffset).AsSpan(bufferOffset, sizeof(float));
                    BinaryPrimitives.WriteSingleLittleEndian(span, f);
                    Advance(sizeof(float));
                }
            }
        }

        public void WriteDoubleArray(ReadOnlySpan<double> items)
        {
            if (BitConverter.IsLittleEndian)
            {
                WriteArray(XmlBinaryNodeType.DoubleTextWithEndElement, items.Length, MemoryMarshal.AsBytes(items));
            }
            else
            {
                WriteArrayInfo(XmlBinaryNodeType.DoubleTextWithEndElement, items.Length);
                foreach (double d in items)
                {
                    Span<byte> span = GetBuffer(sizeof(double), out int bufferOffset).AsSpan(bufferOffset, sizeof(double));
                    BinaryPrimitives.WriteDoubleLittleEndian(span, d);
                    Advance(sizeof(double));
                }
            }
        }

        public void WriteDecimalArray(ReadOnlySpan<decimal> items)
        {
            if (BitConverter.IsLittleEndian)
            {
                WriteArray(XmlBinaryNodeType.DecimalTextWithEndElement, items.Length, MemoryMarshal.AsBytes(items));
            }
            else
            {
                Span<int> bits = [0, 0, 0, 0];
                WriteArrayInfo(XmlBinaryNodeType.DecimalTextWithEndElement, items.Length);
                foreach (ref readonly decimal d in items)
                {
                    decimal.TryGetBits(d, bits, out int intsWritten);
                    Debug.Assert(intsWritten == 4);

                    Span<byte> span = GetBuffer(16, out int bufferOffset).AsSpan(bufferOffset, 16);
                    BinaryPrimitives.WriteInt32LittleEndian(span, bits[3]);
                    BinaryPrimitives.WriteInt32LittleEndian(span.Slice(4), bits[2]);
                    BinaryPrimitives.WriteInt32LittleEndian(span.Slice(8), bits[0]);
                    BinaryPrimitives.WriteInt32LittleEndian(span.Slice(12), bits[1]);
                    Advance(16);
                }
            }
        }

        public void WriteDateTimeArray(ReadOnlySpan<DateTime> items)
        {
            WriteArrayInfo(XmlBinaryNodeType.DateTimeTextWithEndElement, items.Length);
            foreach (DateTime dateTime in items)
            {
                WriteInt64(dateTime.ToBinary());
            }
        }

        public void WriteGuidArray(ReadOnlySpan<Guid> items)
        {
            if (BitConverter.IsLittleEndian)
            {
                WriteArray(XmlBinaryNodeType.GuidTextWithEndElement, items.Length, MemoryMarshal.AsBytes(items));
            }
            else
            {

                WriteArrayInfo(XmlBinaryNodeType.GuidTextWithEndElement, items.Length);
                foreach (ref readonly Guid guid in items)
                {
                    Span<byte> bytes = GetBuffer(16, out int bufferOffset).AsSpan(bufferOffset, 16);
                    guid.TryWriteBytes(bytes);
                    Advance(16);
                }
            }
        }

        public void WriteTimeSpanArray(ReadOnlySpan<TimeSpan> items)
        {
            WriteArrayInfo(XmlBinaryNodeType.TimeSpanTextWithEndElement, items.Length);
            foreach (ref readonly TimeSpan timespan in items)
            {
                WriteInt64(timespan.Ticks);
            }
        }

        public override void WriteQualifiedName(string prefix, XmlDictionaryString localName)
        {
            if (prefix.Length == 0)
            {
                WriteText(localName);
            }
            else
            {
                char ch = prefix[0];
                int key;
                if (prefix.Length == 1 && char.IsAsciiLetterLower(ch) && TryGetKey(localName, out key))
                {
                    WriteTextNodeWithInt8(XmlBinaryNodeType.QNameDictionaryText, (byte)(ch - 'a'));
                    WriteDictionaryString(key);
                }
                else
                {
                    WriteText(prefix);
                    WriteText(":");
                    WriteText(localName);
                }
            }
        }

        public override async Task WriteQualifiedNameAsync(string prefix, XmlDictionaryString localName)
        {
            if (prefix.Length == 0)
            {
                await WriteTextAsync(localName).ConfigureAwait(false);
            }
            else if (prefix.Length == 1 && char.IsAsciiLetterLower(prefix[0]) && TryGetKey(localName, out int key))
            {
                await EnsureBufferAsync(2).ConfigureAwait(false);
                WriteTextNodeWithInt8(XmlBinaryNodeType.QNameDictionaryText, (byte)(prefix[0] - 'a'));
                await WriteMultiByteInt32Async(key).ConfigureAwait(false);
            }
            else
            {
                await WriteTextAsync(prefix).ConfigureAwait(false);
                await WriteTextAsync(":").ConfigureAwait(false);
                await WriteTextAsync(localName).ConfigureAwait(false);
            }
        }

        protected override void FlushBuffer()
        {
            base.FlushBuffer();
            _textNodeOffset = -1;
        }

        protected override Task FlushBufferAsync()
        {
            Task operation = base.FlushBufferAsync();
            if (!operation.IsCompletedSuccessfully)
                return AwaitFlushBufferAsync(operation);

            _textNodeOffset = -1;
            return operation;
        }

        private async Task AwaitFlushBufferAsync(Task operation)
        {
            await operation.ConfigureAwait(false);
            _textNodeOffset = -1;
        }

        public override void Close()
        {
            base.Close();
            _attributeValue.Clear();
        }

        public override async ValueTask CloseAsync()
        {
            try
            {
                await base.CloseAsync().ConfigureAwait(false);
            }
            finally
            {
                _attributeValue.Clear();
            }
        }

        private struct AttributeValue
        {
            private string? _captureText;
            private XmlDictionaryString? _captureXText;
            private MemoryStream? _captureStream;

            public string? Text => _captureText;
            public XmlDictionaryString? DictionaryText => _captureXText;
            public bool TryGetBytes(out ArraySegment<byte> bytes)
            {
                bytes = default;
                return _captureStream is not null && _captureStream.TryGetBuffer(out bytes);
            }

            public void Clear()
            {
                _captureText = null;
                _captureXText = null;
                _captureStream = null;
            }

            public void WriteText(string s)
            {
                if (_captureStream != null)
                {
                    ArraySegment<byte> arraySegment;
                    bool result = _captureStream.TryGetBuffer(out arraySegment);
                    Debug.Assert(result);
                    _captureText = DataContractSerializer.Base64Encoding.GetString(arraySegment.Array!, arraySegment.Offset, arraySegment.Count);
                    _captureStream = null;
                }

                if (_captureXText != null)
                {
                    _captureText = _captureXText.Value;
                    _captureXText = null;
                }

                if (string.IsNullOrEmpty(_captureText))
                {
                    _captureText = s;
                }
                else
                {
                    _captureText += s;
                }
            }

            public void WriteText(XmlDictionaryString s)
            {
                if (_captureText != null || _captureStream != null)
                {
                    WriteText(s.Value);
                }
                else
                {
                    _captureXText = s;
                }
            }

            public void WriteBase64Text(byte[]? trailBytes, int trailByteCount, byte[] buffer, int offset, int count)
            {
                if (_captureText != null || _captureXText != null)
                {
                    if (trailByteCount > 0)
                    {
                        WriteText(DataContractSerializer.Base64Encoding.GetString(trailBytes!, 0, trailByteCount));
                    }
                    WriteText(DataContractSerializer.Base64Encoding.GetString(buffer, offset, count));
                }
                else
                {
                    _captureStream ??= new MemoryStream();

                    if (trailByteCount > 0)
                        _captureStream.Write(trailBytes!, 0, trailByteCount);

                    _captureStream.Write(buffer, offset, count);
                }
            }

            public void WriteTo(XmlBinaryNodeWriter writer)
            {
                if (_captureText != null)
                {
                    writer.WriteText(_captureText);
                    _captureText = null;
                }
                else if (_captureXText != null)
                {
                    writer.WriteText(_captureXText);
                    _captureXText = null;
                }
                else if (_captureStream != null)
                {
                    ArraySegment<byte> arraySegment;
                    bool result = _captureStream.TryGetBuffer(out arraySegment);
                    Debug.Assert(result);
                    writer.WriteBase64Text(null, 0, arraySegment.Array!, arraySegment.Offset, arraySegment.Count);
                    _captureStream = null;
                }
                else
                {
                    writer.WriteEmptyText();
                }
            }
        }
    }

    internal sealed class XmlBinaryWriter : XmlBaseWriter, IXmlBinaryWriterInitializer
    {
        private XmlBinaryNodeWriter _writer = null!; // initialized in SetOutput
        private char[]? _chars;
        private byte[]? _bytes;


        public void SetOutput(Stream stream, IXmlDictionary? dictionary, XmlBinaryWriterSession? session, bool ownsStream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            _writer ??= new XmlBinaryNodeWriter();
            _writer.SetOutput(stream, dictionary, session, ownsStream);
            SetOutput(_writer);
        }

        protected override XmlSigningNodeWriter CreateSigningNodeWriter()
        {
            return new XmlSigningNodeWriter(false);
        }

        protected override void WriteTextNode(XmlDictionaryReader reader, bool attribute)
        {
            Type type = reader.ValueType;
            if (type == typeof(string))
            {
                XmlDictionaryString? value;
                if (reader.TryGetValueAsDictionaryString(out value))
                {
                    WriteString(value);
                }
                else
                {
                    if (reader.CanReadValueChunk)
                    {
                        _chars ??= new char[256];
                        int count;
                        while ((count = reader.ReadValueChunk(_chars, 0, _chars.Length)) > 0)
                        {
                            this.WriteChars(_chars, 0, count);
                        }
                    }
                    else
                    {
                        WriteString(reader.Value);
                    }
                }
                if (!attribute)
                {
                    reader.Read();
                }
            }
            else if (type == typeof(byte[]))
            {
                if (reader.CanReadBinaryContent)
                {
                    // Its best to read in buffers that are a multiple of 3 so we don't break base64 boundaries when converting text
                    _bytes ??= new byte[384];
                    int count;
                    while ((count = reader.ReadValueAsBase64(_bytes, 0, _bytes.Length)) > 0)
                    {
                        this.WriteBase64(_bytes, 0, count);
                    }
                }
                else
                {
                    WriteString(reader.Value);
                }
                if (!attribute)
                {
                    reader.Read();
                }
            }
            else if (type == typeof(int))
                WriteValue(reader.ReadContentAsInt());
            else if (type == typeof(long))
                WriteValue(reader.ReadContentAsLong());
            else if (type == typeof(bool))
                WriteValue(reader.ReadContentAsBoolean());
            else if (type == typeof(double))
                WriteValue(reader.ReadContentAsDouble());
            else if (type == typeof(DateTime))
                WriteValue(reader.ReadContentAsDateTimeOffset().DateTime);
            else if (type == typeof(float))
                WriteValue(reader.ReadContentAsFloat());
            else if (type == typeof(decimal))
                WriteValue(reader.ReadContentAsDecimal());
            else if (type == typeof(UniqueId))
                WriteValue(reader.ReadContentAsUniqueId());
            else if (type == typeof(Guid))
                WriteValue(reader.ReadContentAsGuid());
            else if (type == typeof(TimeSpan))
                WriteValue(reader.ReadContentAsTimeSpan());
            else
                WriteValue(reader.ReadContentAsObject());
        }

        private void WriteStartArray(string? prefix, string localName, string? namespaceUri, int count)
        {
            StartArray(count);
            _writer.WriteArrayNode();
            WriteStartElement(prefix, localName, namespaceUri);
            WriteEndElement();
        }

        private void WriteStartArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, int count)
        {
            StartArray(count);
            _writer.WriteArrayNode();
            WriteStartElement(prefix, localName, namespaceUri);
            WriteEndElement();
        }

        internal override async Task WriteArrayAsync(string? prefix, string localName, string? namespaceUri, Array array, int offset, int count)
        {
            if (Signing)
            {
                await base.WriteArrayAsync(prefix, localName, namespaceUri, array, offset, count).ConfigureAwait(false);
                return;
            }
            CheckArrayBounds(array, offset, count);
            if (count > 0)
            {
                await StartArrayAsync(count).ConfigureAwait(false);
                await _writer.WriteArrayNodeAsync().ConfigureAwait(false);
                await WriteStartElementAsync(prefix, localName, namespaceUri).ConfigureAwait(false);
                await WriteEndElementAsync().ConfigureAwait(false);
                await _writer.WriteArrayAsync(array, offset, count).ConfigureAwait(false);
            }
        }

        internal override Task WriteArrayAsync(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, Array array, int offset, int count)
        {
            if (!Signing)
            {
                CheckArrayBounds(array, offset, count);
                if (count > 0)
                {
                    (XmlBinaryNodeType nodeType, int size) = XmlBinaryNodeWriter.GetArrayTypeInfo(array);
                    if (TryGetBufferedBinaryArraySpace(prefix, localName, namespaceUri, 6L + (long)count * size, out int maximumBytes))
                    {
                        if (_writer.HasBufferSpace(maximumBytes))
                        {
                            WriteBufferedArray(prefix, localName, namespaceUri, array, offset, count, nodeType);
                            return Task.CompletedTask;
                        }
                        return WriteBufferedArrayAsync(prefix, localName, namespaceUri, array, offset, count, nodeType, maximumBytes);
                    }
                }
            }

            return WriteArrayAsyncCore(prefix, localName, namespaceUri, array, offset, count);
        }

        private void WriteBufferedArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri,
            Array array, int offset, int count, XmlBinaryNodeType nodeType)
        {
            WriteStartArray(prefix, localName, namespaceUri, count);
            _writer.WriteBufferedArray(array, offset, count, nodeType);
        }

        private async Task WriteBufferedArrayAsync(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri,
            Array array, int offset, int count, XmlBinaryNodeType nodeType, int maximumBytes)
        {
            await _writer.EnsureBufferAsync(maximumBytes).ConfigureAwait(false);
            WriteBufferedArray(prefix, localName, namespaceUri, array, offset, count, nodeType);
        }

        private async Task WriteArrayAsyncCore(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, Array array, int offset, int count)
        {
            if (Signing)
            {
                await base.WriteArrayAsync(prefix, localName, namespaceUri, array, offset, count).ConfigureAwait(false);
                return;
            }
            CheckArrayBounds(array, offset, count);
            if (count > 0)
            {
                await StartArrayAsync(count).ConfigureAwait(false);
                await _writer.WriteArrayNodeAsync().ConfigureAwait(false);
                await WriteStartElementAsync(prefix, localName, namespaceUri).ConfigureAwait(false);
                await WriteEndElementAsync().ConfigureAwait(false);
                await _writer.WriteArrayAsync(array, offset, count).ConfigureAwait(false);
            }
        }

        private void WriteArray(string? prefix, string localName, string? namespaceUri,
                               XmlBinaryNodeType nodeType, int count, ReadOnlySpan<byte> bytes)
        {
            WriteStartArray(prefix, localName, namespaceUri, count);
            _writer.WriteArray(nodeType, count, bytes);
        }

        private void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri,
                               XmlBinaryNodeType nodeType, int count, ReadOnlySpan<byte> bytes)
        {
            WriteStartArray(prefix, localName, namespaceUri, count);
            _writer.WriteArray(nodeType, count, bytes);
        }

        private static void CheckArray(Array array, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(array);

            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            if (offset > array.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, array.Length));
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (count > array.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, array.Length - offset));
        }

        // Bool
        public override void WriteArray(string? prefix, string localName, string? namespaceUri, bool[] array, int offset, int count)
        {
            if (Signing)
            {
                base.WriteArray(prefix, localName, namespaceUri, array, offset, count);
            }
            else
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    WriteStartArray(prefix, localName, namespaceUri, count);
                    _writer.WriteBoolArray(array.AsSpan(offset, count));
                }
            }
        }

        // Int16
        public override void WriteArray(string? prefix, string localName, string? namespaceUri, short[] array, int offset, int count)
        {
            if (Signing)
            {
                base.WriteArray(prefix, localName, namespaceUri, array, offset, count);
            }
            else
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    WriteStartArray(prefix, localName, namespaceUri, count);
                    _writer.WriteInt16Array(array.AsSpan(offset, count));
                }
            }
        }

        // Int32
        public override void WriteArray(string? prefix, string localName, string? namespaceUri, int[] array, int offset, int count)
        {
            if (Signing)
            {
                base.WriteArray(prefix, localName, namespaceUri, array, offset, count);
            }
            else
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    WriteStartArray(prefix, localName, namespaceUri, count);
                    _writer.WriteInt32Array(array.AsSpan(offset, count));
                }
            }
        }

        // Int64
        public override void WriteArray(string? prefix, string localName, string? namespaceUri, long[] array, int offset, int count)
        {
            if (Signing)
            {
                base.WriteArray(prefix, localName, namespaceUri, array, offset, count);
            }
            else
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    WriteStartArray(prefix, localName, namespaceUri, count);
                    _writer.WriteInt64Array(array.AsSpan(offset, count));
                }
            }
        }

        // Float
        public override void WriteArray(string? prefix, string localName, string? namespaceUri, float[] array, int offset, int count)
        {
            if (Signing)
            {
                base.WriteArray(prefix, localName, namespaceUri, array, offset, count);
            }
            else
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    WriteStartArray(prefix, localName, namespaceUri, count);
                    _writer.WriteFloatArray(array.AsSpan(offset, count));
                }
            }
        }

        // Double
        public override void WriteArray(string? prefix, string localName, string? namespaceUri, double[] array, int offset, int count)
        {
            if (Signing)
            {
                base.WriteArray(prefix, localName, namespaceUri, array, offset, count);
            }
            else
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    WriteStartArray(prefix, localName, namespaceUri, count);
                    _writer.WriteDoubleArray(array.AsSpan(offset, count));
                }
            }
        }

        // Decimal
        public override void WriteArray(string? prefix, string localName, string? namespaceUri, decimal[] array, int offset, int count)
        {
            if (Signing)
            {
                base.WriteArray(prefix, localName, namespaceUri, array, offset, count);
            }
            else
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    WriteStartArray(prefix, localName, namespaceUri, count);
                    _writer.WriteDecimalArray(array.AsSpan(offset, count));
                }
            }
        }

        // DateTime
        public override void WriteArray(string? prefix, string localName, string? namespaceUri, DateTime[] array, int offset, int count)
        {
            if (Signing)
            {
                base.WriteArray(prefix, localName, namespaceUri, array, offset, count);
            }
            else
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    WriteStartArray(prefix, localName, namespaceUri, count);
                    _writer.WriteDateTimeArray(array.AsSpan(offset, count));
                }
            }
        }

        // Guid
        public override void WriteArray(string? prefix, string localName, string? namespaceUri, Guid[] array, int offset, int count)
        {
            if (Signing)
            {
                base.WriteArray(prefix, localName, namespaceUri, array, offset, count);
            }
            else
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    WriteStartArray(prefix, localName, namespaceUri, count);
                    _writer.WriteGuidArray(array.AsSpan(offset, count));
                }
            }
        }

        // TimeSpan
        public override void WriteArray(string? prefix, string localName, string? namespaceUri, TimeSpan[] array, int offset, int count)
        {
            if (Signing)
            {
                base.WriteArray(prefix, localName, namespaceUri, array, offset, count);
            }
            else
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    WriteStartArray(prefix, localName, namespaceUri, count);
                    _writer.WriteTimeSpanArray(array.AsSpan(offset, count));
                }
            }
        }
    }
}
