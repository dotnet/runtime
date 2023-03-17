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
        private void WriteTextNodeRaw<T>(XmlBinaryNodeType nodeType, T value)
            where T : unmanaged
        {
            // GetTextNodeBuffer performs bounds checks and ensures returned buffer has size of at least (1 + Unsafe.SizeOf<T>())
            byte[] buffer = GetTextNodeBuffer(1 + Unsafe.SizeOf<T>(), out int offset);

            Debug.Assert(offset >= 0 && offset + 1 + Unsafe.SizeOf<T>() <= buffer.Length, "WriteTextNodeRaw");
            ref byte bytePtr = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buffer), offset);

            bytePtr = (byte)nodeType;
            Unsafe.WriteUnaligned<T>(ref Unsafe.Add(ref bytePtr, 1), value);
            Advance(1 + Unsafe.SizeOf<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteRaw<T>(T value)
            where T : unmanaged
        {
            // GetBuffer performs bounds checks and ensures returned buffer has size of at least (Unsafe.SizeOf<T>())
            byte[] buffer = GetBuffer(Unsafe.SizeOf<T>(), out int offset);

            Debug.Assert(offset >= 0 && offset + Unsafe.SizeOf<T>() <= buffer.Length, "WriteRaw");
            ref byte bytePtr = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buffer), offset);
            Unsafe.WriteUnaligned<T>(ref bytePtr, value);
            Advance(Unsafe.SizeOf<T>());
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

        public override unsafe void WriteText(string value)
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

        public override void WriteCharEntity(int ch)
        {
            if (ch > char.MaxValue)
            {
                SurrogateChar sch = new SurrogateChar(ch);
                WriteTextImpl(stackalloc char[] { sch.HighChar, sch.LowChar });
            }
            else
            {
                char castChar = (char)ch;
                WriteTextImpl(new ReadOnlySpan<char>(in castChar));
            }
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
                Span<int> bits = stackalloc int[4];
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
                Span<int> bits = stackalloc int[4];
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

        protected override void FlushBuffer()
        {
            base.FlushBuffer();
            _textNodeOffset = -1;
        }

        public override void Close()
        {
            base.Close();
            _attributeValue.Clear();
        }

        private struct AttributeValue
        {
            private string? _captureText;
            private XmlDictionaryString? _captureXText;
            private MemoryStream? _captureStream;

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

                if (_captureText == null || _captureText.Length == 0)
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
