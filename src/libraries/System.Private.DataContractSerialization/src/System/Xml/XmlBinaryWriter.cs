// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// Uncomment to turn on logging of non-dictionary strings written to binary writers.
// This can help identify element/attribute name/ns that could be written as XmlDictionaryStrings to get better compactness and performance.
// #define LOG_NON_DICTIONARY_WRITES

using System.IO;
using System.Text;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Globalization;
using System.Collections.Generic;

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
            DiagnosticUtility.DebugAssert(XmlBinaryNodeType.MaxAttribute < XmlBinaryNodeType.MinElement &&
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
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(SR.XmlOnlySingleValue));
            _wroteAttributeValue = true;
        }

        private void WriteTextNode(XmlBinaryNodeType nodeType)
        {
            if (_inAttribute)
                WroteAttributeValue();
            DiagnosticUtility.DebugAssert(nodeType >= XmlBinaryNodeType.MinText && nodeType <= XmlBinaryNodeType.MaxText && ((byte)nodeType & 1) == 0, "Invalid nodeType");
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
            DiagnosticUtility.DebugAssert(nodeType == XmlBinaryNodeType.Chars8Text || nodeType == XmlBinaryNodeType.Bytes8Text || nodeType == XmlBinaryNodeType.UnicodeChars8Text, "");
            int offset;
            byte[] buffer = GetTextNodeBuffer(5, out offset);
            if (length < 256)
            {
                buffer[offset + 0] = (byte)nodeType;
                buffer[offset + 1] = (byte)length;
                Advance(2);
            }
            else if (length < 65536)
            {
                buffer[offset + 0] = (byte)(nodeType + 1 * 2); // WithEndElements interleave
                buffer[offset + 1] = unchecked((byte)length);
                length >>= 8;
                buffer[offset + 2] = (byte)length;
                Advance(3);
            }
            else
            {
                buffer[offset + 0] = (byte)(nodeType + 2 * 2); // WithEndElements interleave
                buffer[offset + 1] = (byte)length;
                length >>= 8;
                buffer[offset + 2] = (byte)length;
                length >>= 8;
                buffer[offset + 3] = (byte)length;
                length >>= 8;
                buffer[offset + 4] = (byte)length;
                Advance(5);
            }
        }

        private void WriteTextNodeWithInt64(XmlBinaryNodeType nodeType, long value)
        {
            int offset;
            byte[] buffer = GetTextNodeBuffer(9, out offset);
            buffer[offset + 0] = (byte)nodeType;
            buffer[offset + 1] = (byte)value;
            value >>= 8;
            buffer[offset + 2] = (byte)value;
            value >>= 8;
            buffer[offset + 3] = (byte)value;
            value >>= 8;
            buffer[offset + 4] = (byte)value;
            value >>= 8;
            buffer[offset + 5] = (byte)value;
            value >>= 8;
            buffer[offset + 6] = (byte)value;
            value >>= 8;
            buffer[offset + 7] = (byte)value;
            value >>= 8;
            buffer[offset + 8] = (byte)value;
            Advance(9);
        }

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

                if (prefix.Length == 1 && ch >= 'a' && ch <= 'z')
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
                    WriteDictionaryString(localName, key);
                }
                else
                {
                    char ch = prefix[0];

                    if (prefix.Length == 1 && ch >= 'a' && ch <= 'z')
                    {
                        WritePrefixNode(XmlBinaryNodeType.PrefixDictionaryElementA, ch - 'a');
                        WriteDictionaryString(localName, key);
                    }
                    else
                    {
                        WriteNode(XmlBinaryNodeType.DictionaryElement);
                        WriteName(prefix);
                        WriteDictionaryString(localName, key);
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
                DiagnosticUtility.DebugAssert(nodeType >= XmlBinaryNodeType.MinText && nodeType <= XmlBinaryNodeType.MaxText && ((byte)nodeType & 1) == 0, "");
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
                if (prefix.Length == 1 && ch >= 'a' && ch <= 'z')
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
                    WriteDictionaryString(localName, key);
                }
                else
                {
                    char ch = prefix[0];
                    if (prefix.Length == 1 && ch >= 'a' && ch <= 'z')
                    {
                        WritePrefixNode(XmlBinaryNodeType.PrefixDictionaryAttributeA, ch - 'a');
                        WriteDictionaryString(localName, key);
                    }
                    else
                    {
                        WriteNode(XmlBinaryNodeType.DictionaryAttribute);
                        WriteName(prefix);
                        WriteDictionaryString(localName, key);
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
                    WriteDictionaryString(ns, key);
                }
                else
                {
                    WriteNode(XmlBinaryNodeType.DictionaryXmlnsAttribute);
                    WriteName(prefix);
                    WriteDictionaryString(ns, key);
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
                DiagnosticUtility.DebugAssert(t.Dictionary == _dictionary, "");
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

        private void WriteDictionaryString(XmlDictionaryString s, int key)
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
                DiagnosticUtility.DebugAssert(length < 128, "");
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
            if (value >= -128 && value < 128)
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
                    int offset;
                    byte[] buffer = GetTextNodeBuffer(2, out offset);
                    buffer[offset + 0] = (byte)XmlBinaryNodeType.Int8Text;
                    buffer[offset + 1] = (byte)value;
                    Advance(2);
                }
            }
            else if (value >= -32768 && value < 32768)
            {
                int offset;
                byte[] buffer = GetTextNodeBuffer(3, out offset);
                buffer[offset + 0] = (byte)XmlBinaryNodeType.Int16Text;
                buffer[offset + 1] = (byte)value;
                value >>= 8;
                buffer[offset + 2] = (byte)value;
                Advance(3);
            }
            else
            {
                int offset;
                byte[] buffer = GetTextNodeBuffer(5, out offset);
                buffer[offset + 0] = (byte)XmlBinaryNodeType.Int32Text;
                buffer[offset + 1] = (byte)value;
                value >>= 8;
                buffer[offset + 2] = (byte)value;
                value >>= 8;
                buffer[offset + 3] = (byte)value;
                value >>= 8;
                buffer[offset + 4] = (byte)value;
                Advance(5);
            }
        }

        public override void WriteInt64Text(long value)
        {
            if (value >= int.MinValue && value <= int.MaxValue)
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
        {
            int offset;
            byte[] buffer = GetBuffer(8, out offset);
            buffer[offset + 0] = (byte)value;
            value >>= 8;
            buffer[offset + 1] = (byte)value;
            value >>= 8;
            buffer[offset + 2] = (byte)value;
            value >>= 8;
            buffer[offset + 3] = (byte)value;
            value >>= 8;
            buffer[offset + 4] = (byte)value;
            value >>= 8;
            buffer[offset + 5] = (byte)value;
            value >>= 8;
            buffer[offset + 6] = (byte)value;
            value >>= 8;
            buffer[offset + 7] = (byte)value;
            Advance(8);
        }

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
                    WriteDictionaryString(value, key);
                }
            }
        }

        public unsafe override void WriteText(string value)
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

        public unsafe override void WriteText(char[] chars, int offset, int count)
        {
            if (_inAttribute)
            {
                _attributeValue.WriteText(new string(chars, offset, count));
            }
            else
            {
                if (count > 0)
                {
                    fixed (char* pch = &chars[offset])
                    {
                        UnsafeWriteText(pch, count);
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
            DiagnosticUtility.DebugAssert(charCount > 0, "");

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
                DiagnosticUtility.DebugAssert(length <= byte.MaxValue, "");
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
                char[] chars = new char[2] { sch.HighChar, sch.LowChar, };
                WriteText(chars, 0, 2);
            }
            else
            {
                char[] chars = new char[1] { (char)ch };
                WriteText(chars, 0, 1);
            }
        }

        public unsafe override void WriteFloatText(float f)
        {
            long l;
            if (f >= long.MinValue && f <= long.MaxValue && (l = (long)f) == f)
            {
                WriteInt64Text(l);
            }
            else
            {
                int offset;
                byte[] buffer = GetTextNodeBuffer(1 + sizeof(float), out offset);
                byte* bytes = (byte*)&f;
                buffer[offset + 0] = (byte)XmlBinaryNodeType.FloatText;
                buffer[offset + 1] = bytes[0];
                buffer[offset + 2] = bytes[1];
                buffer[offset + 3] = bytes[2];
                buffer[offset + 4] = bytes[3];
                Advance(1 + sizeof(float));
            }
        }

        public unsafe override void WriteDoubleText(double d)
        {
            float f;
            if (d >= float.MinValue && d <= float.MaxValue && (f = (float)d) == d)
            {
                WriteFloatText(f);
            }
            else
            {
                int offset;
                byte[] buffer = GetTextNodeBuffer(1 + sizeof(double), out offset);
                byte* bytes = (byte*)&d;
                buffer[offset + 0] = (byte)XmlBinaryNodeType.DoubleText;
                buffer[offset + 1] = bytes[0];
                buffer[offset + 2] = bytes[1];
                buffer[offset + 3] = bytes[2];
                buffer[offset + 4] = bytes[3];
                buffer[offset + 5] = bytes[4];
                buffer[offset + 6] = bytes[5];
                buffer[offset + 7] = bytes[6];
                buffer[offset + 8] = bytes[7];
                Advance(1 + sizeof(double));
            }
        }

        public unsafe override void WriteDecimalText(decimal d)
        {
            int offset;
            byte[] buffer = GetTextNodeBuffer(1 + sizeof(decimal), out offset);
            byte* bytes = (byte*)&d;
            buffer[offset++] = (byte)XmlBinaryNodeType.DecimalText;
            for (int i = 0; i < sizeof(decimal); i++)
            {
                buffer[offset++] = bytes[i];
            }
            Advance(1 + sizeof(decimal));
        }

        public override void WriteDateTimeText(DateTime dt)
        {
            WriteTextNodeWithInt64(XmlBinaryNodeType.DateTimeText, ToBinary(dt));
        }
        private static long ToBinary(DateTime dt)
        {
            long temp = 0;
            switch (dt.Kind)
            {
                case DateTimeKind.Local:
                    temp = temp | -9223372036854775808L; // 0x8000000000000000
                    temp = temp | dt.ToUniversalTime().Ticks;
                    break;
                case DateTimeKind.Utc:
                    temp = temp | 0x4000000000000000L;
                    temp = temp | dt.Ticks;
                    break;
                case DateTimeKind.Unspecified:
                    temp = dt.Ticks;
                    break;
            }
            return temp;
        }


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
            int offset;
            byte[] buffer = GetTextNodeBuffer(17, out offset);
            buffer[offset] = (byte)XmlBinaryNodeType.GuidText;
            Buffer.BlockCopy(guid.ToByteArray(), 0, buffer, offset + 1, 16);
            Advance(17);
        }

        public override void WriteTimeSpanText(TimeSpan value)
        {
            WriteTextNodeWithInt64(XmlBinaryNodeType.TimeSpanText, value.Ticks);
        }

        public override void WriteStartListText()
        {
            DiagnosticUtility.DebugAssert(!_inList, "");
            _inList = true;
            WriteNode(XmlBinaryNodeType.StartListText);
        }

        public override void WriteListSeparator()
        {
        }

        public override void WriteEndListText()
        {
            DiagnosticUtility.DebugAssert(_inList, "");
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

        public unsafe void UnsafeWriteArray(XmlBinaryNodeType nodeType, int count, byte* array, byte* arrayMax)
        {
            WriteArrayInfo(nodeType, count);
            UnsafeWriteArray(array, (int)(arrayMax - array));
        }

        private unsafe void UnsafeWriteArray(byte* array, int byteCount)
        {
            base.UnsafeWriteBytes(array, byteCount);
        }

        public void WriteDateTimeArray(DateTime[] array, int offset, int count)
        {
            WriteArrayInfo(XmlBinaryNodeType.DateTimeTextWithEndElement, count);
            for (int i = 0; i < count; i++)
            {
                WriteInt64(ToBinary(array[offset + i]));
            }
        }

        public void WriteGuidArray(Guid[] array, int offset, int count)
        {
            WriteArrayInfo(XmlBinaryNodeType.GuidTextWithEndElement, count);
            for (int i = 0; i < count; i++)
            {
                byte[] buffer = array[offset + i].ToByteArray();
                WriteBytes(buffer, 0, 16);
            }
        }

        public void WriteTimeSpanArray(TimeSpan[] array, int offset, int count)
        {
            WriteArrayInfo(XmlBinaryNodeType.TimeSpanTextWithEndElement, count);
            for (int i = 0; i < count; i++)
            {
                WriteInt64(array[offset + i].Ticks);
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
                if (prefix.Length == 1 && (ch >= 'a' && ch <= 'z') && TryGetKey(localName, out key))
                {
                    WriteTextNode(XmlBinaryNodeType.QNameDictionaryText);
                    WriteByte((byte)(ch - 'a'));
                    WriteDictionaryString(localName, key);
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
                    DiagnosticUtility.DebugAssert(result, "");
                    _captureText = XmlConverter.Base64Encoding.GetString(arraySegment.Array!, arraySegment.Offset, arraySegment.Count);
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
                        WriteText(XmlConverter.Base64Encoding.GetString(trailBytes!, 0, trailByteCount));
                    }
                    WriteText(XmlConverter.Base64Encoding.GetString(buffer, offset, count));
                }
                else
                {
                    if (_captureStream == null)
                        _captureStream = new MemoryStream();

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
                    DiagnosticUtility.DebugAssert(result, "");
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


        public void SetOutput(Stream stream!!, IXmlDictionary? dictionary, XmlBinaryWriterSession? session, bool ownsStream)
        {
            if (_writer == null)
                _writer = new XmlBinaryNodeWriter();
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
                        if (_chars == null)
                        {
                            _chars = new char[256];
                        }
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
                    if (_bytes == null)
                    {
                        _bytes = new byte[384];
                    }
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

        private unsafe void UnsafeWriteArray(string? prefix, string localName, string? namespaceUri,
                               XmlBinaryNodeType nodeType, int count, byte* array, byte* arrayMax)
        {
            WriteStartArray(prefix, localName, namespaceUri, count);
            _writer.UnsafeWriteArray(nodeType, count, array, arrayMax);
            // WriteEndArray();
        }

        private unsafe void UnsafeWriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri,
                               XmlBinaryNodeType nodeType, int count, byte* array, byte* arrayMax)
        {
            WriteStartArray(prefix, localName, namespaceUri, count);
            _writer.UnsafeWriteArray(nodeType, count, array, arrayMax);
            // WriteEndArray();
        }

        private static void CheckArray(Array array!!, int offset, int count)
        {
            if (offset < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.ValueMustBeNonNegative));
            if (offset > array.Length)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(offset), SR.Format(SR.OffsetExceedsBufferSize, array.Length)));
            if (count < 0)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.ValueMustBeNonNegative));
            if (count > array.Length - offset)
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.SizeExceedsRemainingBufferSpace, array.Length - offset)));
        }

        public unsafe override void WriteArray(string? prefix, string localName, string? namespaceUri, bool[] array, int offset, int count)
        {
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    fixed (bool* items = &array[offset])
                    {
                        UnsafeWriteArray(prefix, localName, namespaceUri, XmlBinaryNodeType.BoolTextWithEndElement, count, (byte*)items, (byte*)&items[count]);
                    }
                }
            }
        }

        public unsafe override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, bool[] array, int offset, int count)
        {
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    fixed (bool* items = &array[offset])
                    {
                        UnsafeWriteArray(prefix, localName, namespaceUri, XmlBinaryNodeType.BoolTextWithEndElement, count, (byte*)items, (byte*)&items[count]);
                    }
                }
            }
        }

        public unsafe override void WriteArray(string? prefix, string localName, string? namespaceUri, short[] array, int offset, int count)
        {
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    fixed (short* items = &array[offset])
                    {
                        UnsafeWriteArray(prefix, localName, namespaceUri, XmlBinaryNodeType.Int16TextWithEndElement, count, (byte*)items, (byte*)&items[count]);
                    }
                }
            }
        }

        public unsafe override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, short[] array, int offset, int count)
        {
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    fixed (short* items = &array[offset])
                    {
                        UnsafeWriteArray(prefix, localName, namespaceUri, XmlBinaryNodeType.Int16TextWithEndElement, count, (byte*)items, (byte*)&items[count]);
                    }
                }
            }
        }

        public unsafe override void WriteArray(string? prefix, string localName, string? namespaceUri, int[] array, int offset, int count)
        {
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    fixed (int* items = &array[offset])
                    {
                        UnsafeWriteArray(prefix, localName, namespaceUri, XmlBinaryNodeType.Int32TextWithEndElement, count, (byte*)items, (byte*)&items[count]);
                    }
                }
            }
        }

        public unsafe override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, int[] array, int offset, int count)
        {
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    fixed (int* items = &array[offset])
                    {
                        UnsafeWriteArray(prefix, localName, namespaceUri, XmlBinaryNodeType.Int32TextWithEndElement, count, (byte*)items, (byte*)&items[count]);
                    }
                }
            }
        }

        public unsafe override void WriteArray(string? prefix, string localName, string? namespaceUri, long[] array, int offset, int count)
        {
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    fixed (long* items = &array[offset])
                    {
                        UnsafeWriteArray(prefix, localName, namespaceUri, XmlBinaryNodeType.Int64TextWithEndElement, count, (byte*)items, (byte*)&items[count]);
                    }
                }
            }
        }

        public unsafe override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, long[] array, int offset, int count)
        {
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    fixed (long* items = &array[offset])
                    {
                        UnsafeWriteArray(prefix, localName, namespaceUri, XmlBinaryNodeType.Int64TextWithEndElement, count, (byte*)items, (byte*)&items[count]);
                    }
                }
            }
        }

        public unsafe override void WriteArray(string? prefix, string localName, string? namespaceUri, float[] array, int offset, int count)
        {
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    fixed (float* items = &array[offset])
                    {
                        UnsafeWriteArray(prefix, localName, namespaceUri, XmlBinaryNodeType.FloatTextWithEndElement, count, (byte*)items, (byte*)&items[count]);
                    }
                }
            }
        }

        public unsafe override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, float[] array, int offset, int count)
        {
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    fixed (float* items = &array[offset])
                    {
                        UnsafeWriteArray(prefix, localName, namespaceUri, XmlBinaryNodeType.FloatTextWithEndElement, count, (byte*)items, (byte*)&items[count]);
                    }
                }
            }
        }

        public unsafe override void WriteArray(string? prefix, string localName, string? namespaceUri, double[] array, int offset, int count)
        {
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    fixed (double* items = &array[offset])
                    {
                        UnsafeWriteArray(prefix, localName, namespaceUri, XmlBinaryNodeType.DoubleTextWithEndElement, count, (byte*)items, (byte*)&items[count]);
                    }
                }
            }
        }

        public unsafe override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, double[] array, int offset, int count)
        {
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    fixed (double* items = &array[offset])
                    {
                        UnsafeWriteArray(prefix, localName, namespaceUri, XmlBinaryNodeType.DoubleTextWithEndElement, count, (byte*)items, (byte*)&items[count]);
                    }
                }
            }
        }

        public unsafe override void WriteArray(string? prefix, string localName, string? namespaceUri, decimal[] array, int offset, int count)
        {
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    fixed (decimal* items = &array[offset])
                    {
                        UnsafeWriteArray(prefix, localName, namespaceUri, XmlBinaryNodeType.DecimalTextWithEndElement, count, (byte*)items, (byte*)&items[count]);
                    }
                }
            }
        }

        public unsafe override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, decimal[] array, int offset, int count)
        {
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    fixed (decimal* items = &array[offset])
                    {
                        UnsafeWriteArray(prefix, localName, namespaceUri, XmlBinaryNodeType.DecimalTextWithEndElement, count, (byte*)items, (byte*)&items[count]);
                    }
                }
            }
        }

        // DateTime
        public override void WriteArray(string? prefix, string localName, string? namespaceUri, DateTime[] array, int offset, int count)
        {
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    WriteStartArray(prefix, localName, namespaceUri, count);
                    _writer.WriteDateTimeArray(array, offset, count);
                    // WriteEndArray();
                }
            }
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, DateTime[] array, int offset, int count)
        {
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    WriteStartArray(prefix, localName, namespaceUri, count);
                    _writer.WriteDateTimeArray(array, offset, count);
                    // WriteEndArray();
                }
            }
        }

        // Guid
        public override void WriteArray(string? prefix, string localName, string? namespaceUri, Guid[] array, int offset, int count)
        {
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    WriteStartArray(prefix, localName, namespaceUri, count);
                    _writer.WriteGuidArray(array, offset, count);
                    // WriteEndArray();
                }
            }
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, Guid[] array, int offset, int count)
        {
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    WriteStartArray(prefix, localName, namespaceUri, count);
                    _writer.WriteGuidArray(array, offset, count);
                    // WriteEndArray();
                }
            }
        }

        // TimeSpan
        public override void WriteArray(string? prefix, string localName, string? namespaceUri, TimeSpan[] array, int offset, int count)
        {
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    WriteStartArray(prefix, localName, namespaceUri, count);
                    _writer.WriteTimeSpanArray(array, offset, count);
                    // WriteEndArray();
                }
            }
        }

        public override void WriteArray(string? prefix, XmlDictionaryString localName, XmlDictionaryString? namespaceUri, TimeSpan[] array, int offset, int count)
        {
            {
                CheckArray(array, offset, count);
                if (count > 0)
                {
                    WriteStartArray(prefix, localName, namespaceUri, count);
                    _writer.WriteTimeSpanArray(array, offset, count);
                    // WriteEndArray();
                }
            }
        }
    }
}
