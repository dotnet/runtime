// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Buffers.Binary;

namespace System.Xml
{
    internal sealed class XmlBufferReader
    {
        private readonly XmlDictionaryReader _reader;
        private Stream? _stream;
        private byte[]? _streamBuffer;
        private byte[] _buffer = null!; // initialized by SetBuffer
        private int _offsetMin;
        private int _offsetMax;
        private IXmlDictionary? _dictionary;
        private XmlBinaryReaderSession? _session;
        private int _offset;
        private const int maxBytesPerChar = 3;
        private char[]? _chars;
        private int _windowOffset;
        private int _windowOffsetMax;
        private ValueHandle? _listValue;
        private static readonly XmlBufferReader s_empty = new XmlBufferReader(Array.Empty<byte>());

        public XmlBufferReader(XmlDictionaryReader reader)
        {
            _reader = reader;
        }

        public XmlBufferReader(byte[] buffer)
        {
            _reader = null!; // this ctor is only used in 2 places internally, which will never touch _reader
            _buffer = buffer;
        }

        public static XmlBufferReader Empty
        {
            get
            {
                return s_empty;
            }
        }

        public byte[] Buffer
        {
            get
            {
                return _buffer;
            }
        }

        public bool IsStreamed
        {
            get
            {
                return _stream != null;
            }
        }


        public void SetBuffer(Stream stream, IXmlDictionary? dictionary, XmlBinaryReaderSession? session)
        {
            _streamBuffer ??= new byte[128];
            SetBuffer(stream, _streamBuffer, 0, 0, dictionary, session);
            _windowOffset = 0;
            _windowOffsetMax = _streamBuffer.Length;
        }

        public void SetBuffer(byte[] buffer, int offset, int count, IXmlDictionary? dictionary, XmlBinaryReaderSession? session)
        {
            SetBuffer(null, buffer, offset, count, dictionary, session);
        }

        private void SetBuffer(Stream? stream, byte[] buffer, int offset, int count, IXmlDictionary? dictionary, XmlBinaryReaderSession? session)
        {
            _stream = stream;
            _buffer = buffer;
            _offsetMin = offset;
            _offset = offset;
            _offsetMax = offset + count;
            _dictionary = dictionary;
            _session = session;
        }

        public void Close()
        {
            if (_streamBuffer != null && _streamBuffer.Length > 4096)
            {
                _streamBuffer = null;
            }
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
            _buffer = Array.Empty<byte>();
            _offset = 0;
            _offsetMax = 0;
            _windowOffset = 0;
            _windowOffsetMax = 0;
            _dictionary = null;
            _session = null;
        }

        public bool EndOfFile
        {
            get
            {
                return _offset == _offsetMax && !TryEnsureByte();
            }
        }

        public byte GetByte()
        {
            int offset = _offset;
            if (offset < _offsetMax)
                return _buffer[offset];
            else
                return GetByteHard();
        }

        public void SkipByte()
        {
            Advance(1);
        }

        private byte GetByteHard()
        {
            EnsureByte();
            return _buffer[_offset];
        }

        public byte[] GetBuffer(int count, out int offset)
        {
            offset = _offset;
            if (offset <= _offsetMax - count)
                return _buffer;
            return GetBufferHard(count, out offset);
        }

        public byte[] GetBuffer(int count, out int offset, out int offsetMax)
        {
            offset = _offset;
            if (offset <= _offsetMax - count)
            {
                offsetMax = _offset + count;
            }
            else
            {
                TryEnsureBytes(Math.Min(count, _windowOffsetMax - offset));
                offsetMax = _offsetMax;
            }
            return _buffer;
        }

        public byte[] GetBuffer(out int offset, out int offsetMax)
        {
            offset = _offset;
            offsetMax = _offsetMax;
            return _buffer;
        }

        private byte[] GetBufferHard(int count, out int offset)
        {
            offset = _offset;
            EnsureBytes(count);
            return _buffer;
        }

        private void EnsureByte()
        {
            if (!TryEnsureByte())
                XmlExceptionHelper.ThrowUnexpectedEndOfFile(_reader);
        }

        private bool TryEnsureByte()
        {
            if (_stream == null)
                return false;
            Debug.Assert(_offsetMax < _windowOffsetMax);
            if (_offsetMax >= _buffer.Length)
                return TryEnsureBytes(1);
            int b = _stream.ReadByte();
            if (b == -1)
                return false;
            _buffer[_offsetMax++] = (byte)b;
            return true;
        }

        private void EnsureBytes(int count)
        {
            if (!TryEnsureBytes(count))
                XmlExceptionHelper.ThrowUnexpectedEndOfFile(_reader);
        }

        private bool TryEnsureBytes(int count)
        {
            if (_stream == null)
                return false;

            Debug.Assert(_offset <= int.MaxValue - count);

            // The data could be coming from an untrusted source, so we use a standard
            // "multiply by 2" growth algorithm to avoid overly large memory utilization.
            // Constant value of 256 comes from MemoryStream implementation.

            do
            {
                int newOffsetMax = _offset + count;
                if (newOffsetMax <= _offsetMax)
                    return true;
                Debug.Assert(newOffsetMax <= _windowOffsetMax);
                if (newOffsetMax > _buffer.Length)
                {
                    byte[] newBuffer = new byte[Math.Max(256, _buffer.Length * 2)];
                    System.Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _offsetMax);
                    newOffsetMax = Math.Min(newOffsetMax, newBuffer.Length);
                    _buffer = newBuffer;
                    _streamBuffer = newBuffer;
                }
                int needed = newOffsetMax - _offsetMax;
                Debug.Assert(needed > 0);
                int read = _stream.ReadAtLeast(_buffer.AsSpan(_offsetMax, needed), needed, throwOnEndOfStream: false);
                _offsetMax += read;

                if (read < needed)
                {
                    return false;
                }
            } while (true);
        }

        public void Advance(int count)
        {
            Debug.Assert(_offset + count <= _offsetMax);
            _offset += count;
        }

        public void InsertBytes(byte[] buffer, int offset, int count)
        {
            Debug.Assert(_stream != null);
            if (_offsetMax > buffer.Length - count)
            {
                byte[] newBuffer = new byte[_offsetMax + count];
                System.Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _offsetMax);
                _buffer = newBuffer;
                _streamBuffer = newBuffer;
            }
            System.Buffer.BlockCopy(_buffer, _offset, _buffer, _offset + count, _offsetMax - _offset);
            _offsetMax += count;
            System.Buffer.BlockCopy(buffer, offset, _buffer, _offset, count);
        }

        public void SetWindow(int windowOffset, int windowLength)
        {
            // [0...elementOffset-1][elementOffset..offset][offset..offsetMax-1][offsetMax..buffer.Length]
            // ^--Elements, Attributes in scope
            //                      ^-- The node just consumed
            //                                             ^--Data buffered, not consumed
            //                                                                  ^--Unused space
            if (windowOffset > int.MaxValue - windowLength)
                windowLength = int.MaxValue - windowOffset;

            if (_offset != windowOffset)
            {
                System.Buffer.BlockCopy(_buffer, _offset, _buffer, windowOffset, _offsetMax - _offset);
                _offsetMax = windowOffset + (_offsetMax - _offset);
                _offset = windowOffset;
            }
            _windowOffset = windowOffset;
            _windowOffsetMax = Math.Max(windowOffset + windowLength, _offsetMax);
        }

        public int Offset
        {
            get
            {
                return _offset;
            }
            set
            {
                Debug.Assert(value >= _offsetMin && value <= _offsetMax);
                _offset = value;
            }
        }

        public int ReadBytes(int count)
        {
            Debug.Assert(count >= 0);
            int offset = _offset;
            if (offset > _offsetMax - count)
                EnsureBytes(count);
            _offset += count;
            return offset;
        }

        public int ReadMultiByteUInt31()
        {
            int i = GetByte();
            Advance(1);
            if ((i & 0x80) == 0)
                return i;
            i &= 0x7F;

            int j = GetByte();
            Advance(1);
            i |= ((j & 0x7F) << 7);
            if ((j & 0x80) == 0)
                return i;

            int k = GetByte();
            Advance(1);
            i |= ((k & 0x7F) << 14);
            if ((k & 0x80) == 0)
                return i;

            int l = GetByte();
            Advance(1);
            i |= ((l & 0x7F) << 21);
            if ((l & 0x80) == 0)
                return i;

            int m = GetByte();
            Advance(1);
            i |= (m << 28);
            if ((m & 0xF8) != 0)
                XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);

            return i;
        }

        public int ReadUInt8()
        {
            byte b = GetByte();
            Advance(1);
            return b;
        }

        public int ReadInt8()
            => (sbyte)ReadUInt8();

        public int ReadUInt16()
            => BitConverter.IsLittleEndian ? ReadRawBytes<ushort>() : BinaryPrimitives.ReverseEndianness(ReadRawBytes<ushort>());

        public int ReadInt16()
            => (short)ReadUInt16();

        public int ReadInt32()
            => BitConverter.IsLittleEndian ? ReadRawBytes<int>() : BinaryPrimitives.ReverseEndianness(ReadRawBytes<int>());

        public int ReadUInt31()
        {
            int i = ReadInt32();
            if (i < 0)
                XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
            return i;
        }

        public long ReadInt64()
            => BitConverter.IsLittleEndian ? ReadRawBytes<long>() : BinaryPrimitives.ReverseEndianness(ReadRawBytes<long>());

        public float ReadSingle()
        {
            float f = BinaryPrimitives.ReadSingleLittleEndian(GetBuffer(sizeof(float), out int offset).AsSpan(offset, sizeof(float)));
            Advance(sizeof(float));
            return f;
        }

        public double ReadDouble()
        {
            double d = BinaryPrimitives.ReadDoubleLittleEndian(GetBuffer(sizeof(double), out int offset).AsSpan(offset, sizeof(double)));
            Advance(sizeof(double));
            return d;
        }

        public decimal ReadDecimal()
        {
            if (BitConverter.IsLittleEndian)
            {
                return ReadRawBytes<decimal>();
            }
            else
            {
                byte[] buffer = GetBuffer(ValueHandleLength.Decimal, out int offset);
                ReadOnlySpan<byte> bytes = buffer.AsSpan(offset, sizeof(decimal));
                ReadOnlySpan<int> span = stackalloc int[4]
                {
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(8, 4)),
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(12, 4)),
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(4, 4)),
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(0, 4))
                };

                Advance(ValueHandleLength.Decimal);
                return new decimal(span);
            }
        }

        public UniqueId ReadUniqueId()
        {
            int offset;
            byte[] buffer = GetBuffer(ValueHandleLength.UniqueId, out offset);
            UniqueId uniqueId = new UniqueId(buffer, offset);
            Advance(ValueHandleLength.UniqueId);
            return uniqueId;
        }

        public DateTime ReadDateTime()
        {
            long value = 0;
            try
            {
                value = ReadInt64();
                return DateTime.FromBinary(value);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value.ToString(CultureInfo.InvariantCulture), "DateTime", exception);
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value.ToString(CultureInfo.InvariantCulture), "DateTime", exception);
            }
            catch (OverflowException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value.ToString(CultureInfo.InvariantCulture), "DateTime", exception);
            }
        }

        public TimeSpan ReadTimeSpan()
        {
            long value = 0;
            try
            {
                value = ReadInt64();
                return TimeSpan.FromTicks(value);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value.ToString(CultureInfo.InvariantCulture), "TimeSpan", exception);
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value.ToString(CultureInfo.InvariantCulture), "TimeSpan", exception);
            }
            catch (OverflowException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value.ToString(CultureInfo.InvariantCulture), "TimeSpan", exception);
            }
        }

        public Guid ReadGuid()
        {
            int offset;
            _ = GetBuffer(ValueHandleLength.Guid, out offset);
            Guid guid = GetGuid(offset);
            Advance(ValueHandleLength.Guid);
            return guid;
        }

        public string ReadUTF8String(int length)
        {
            int offset;
            _ = GetBuffer(length, out offset);
            char[] chars = GetCharBuffer(length);
            int charCount = GetChars(offset, length, chars);
            string value = new string(chars, 0, charCount);
            Advance(length);
            return value;
        }

        public void ReadRawArrayBytes<T>(Span<T> dst)
            where T : unmanaged
        {
            ReadRawArrayBytes(MemoryMarshal.AsBytes(dst));
        }

        public void ReadRawArrayBytes(Span<byte> dst)
        {
            if (dst.Length > 0)
            {
                GetBuffer(dst.Length, out _offset)
                    .AsSpan(_offset, dst.Length)
                    .CopyTo(dst);

                Advance(dst.Length);
            }
        }

        private char[] GetCharBuffer(int count)
        {
            if (count > 1024)
                return new char[count];
            if (_chars == null || _chars.Length < count)
                _chars = new char[count];
            return _chars;
        }

        private int GetChars(int offset, int length, char[] chars)
        {
            byte[] buffer = _buffer;
            for (int i = 0; i < length; i++)
            {
                byte b = buffer[offset + i];
                if (b >= 0x80)
                    return i + XmlConverter.ToChars(buffer, offset + i, length - i, chars, i);
                chars[i] = (char)b;
            }
            return length;
        }

        private int GetChars(int offset, int length, char[] chars, int charOffset)
        {
            byte[] buffer = _buffer;
            for (int i = 0; i < length; i++)
            {
                byte b = buffer[offset + i];
                if (b >= 0x80)
                    return i + XmlConverter.ToChars(buffer, offset + i, length - i, chars, charOffset + i);
                chars[charOffset + i] = (char)b;
            }
            return length;
        }

        public string GetString(int offset, int length)
        {
            char[] chars = GetCharBuffer(length);
            int charCount = GetChars(offset, length, chars);
            return new string(chars, 0, charCount);
        }

        public string GetUnicodeString(int offset, int length)
        {
            return XmlConverter.ToStringUnicode(_buffer, offset, length);
        }

        public string GetString(int offset, int length, XmlNameTable nameTable)
        {
            char[] chars = GetCharBuffer(length);
            int charCount = GetChars(offset, length, chars);
            return nameTable.Add(chars, 0, charCount);
        }

        public int GetEscapedChars(int offset, int length, char[] chars)
        {
            byte[] buffer = _buffer;
            int charCount = 0;
            int textOffset = offset;
            int offsetMax = offset + length;
            while (true)
            {
                while (offset < offsetMax && IsAttrChar(buffer[offset]))
                    offset++;
                charCount += GetChars(textOffset, offset - textOffset, chars, charCount);
                if (offset == offsetMax)
                    break;
                textOffset = offset;
                if (buffer[offset] == '&')
                {
                    while (offset < offsetMax && buffer[offset] != ';')
                        offset++;
                    offset++;
                    int ch = GetCharEntity(textOffset, offset - textOffset);
                    textOffset = offset;
                    if (ch > char.MaxValue)
                    {
                        SurrogateChar surrogate = new SurrogateChar(ch);
                        chars[charCount++] = surrogate.HighChar;
                        chars[charCount++] = surrogate.LowChar;
                    }
                    else
                    {
                        chars[charCount++] = (char)ch;
                    }
                }
                else if (buffer[offset] == '\n' || buffer[offset] == '\t')
                {
                    chars[charCount++] = ' ';
                    offset++;
                    textOffset = offset;
                }
                else // '\r'
                {
                    chars[charCount++] = ' ';
                    offset++;

                    if (offset < offsetMax && buffer[offset] == '\n')
                        offset++;

                    textOffset = offset;
                }
            }
            return charCount;
        }

        private static bool IsAttrChar(int ch)
        {
            switch (ch)
            {
                case '&':
                case '\r':
                case '\t':
                case '\n':
                    return false;

                default:
                    return true;
            }
        }

        public string GetEscapedString(int offset, int length)
        {
            char[] chars = GetCharBuffer(length);
            int charCount = GetEscapedChars(offset, length, chars);
            return new string(chars, 0, charCount);
        }

        public string GetEscapedString(int offset, int length, XmlNameTable nameTable)
        {
            char[] chars = GetCharBuffer(length);
            int charCount = GetEscapedChars(offset, length, chars);
            return nameTable.Add(chars, 0, charCount);
        }

        private int GetLessThanCharEntity(int offset, int length)
        {
            byte[] buffer = _buffer;
            if (length != 4 ||
                buffer[offset + 1] != (byte)'l' ||
                buffer[offset + 2] != (byte)'t')
            {
                XmlExceptionHelper.ThrowInvalidCharRef(_reader);
            }
            return (int)'<';
        }

        private int GetGreaterThanCharEntity(int offset, int length)
        {
            byte[] buffer = _buffer;
            if (length != 4 ||
                buffer[offset + 1] != (byte)'g' ||
                buffer[offset + 2] != (byte)'t')
            {
                XmlExceptionHelper.ThrowInvalidCharRef(_reader);
            }
            return (int)'>';
        }

        private int GetQuoteCharEntity(int offset, int length)
        {
            byte[] buffer = _buffer;
            if (length != 6 ||
                buffer[offset + 1] != (byte)'q' ||
                buffer[offset + 2] != (byte)'u' ||
                buffer[offset + 3] != (byte)'o' ||
                buffer[offset + 4] != (byte)'t')
            {
                XmlExceptionHelper.ThrowInvalidCharRef(_reader);
            }
            return (int)'"';
        }

        private int GetAmpersandCharEntity(int offset, int length)
        {
            byte[] buffer = _buffer;
            if (length != 5 ||
                buffer[offset + 1] != (byte)'a' ||
                buffer[offset + 2] != (byte)'m' ||
                buffer[offset + 3] != (byte)'p')
            {
                XmlExceptionHelper.ThrowInvalidCharRef(_reader);
            }
            return (int)'&';
        }

        private int GetApostropheCharEntity(int offset, int length)
        {
            byte[] buffer = _buffer;
            if (length != 6 ||
                buffer[offset + 1] != (byte)'a' ||
                buffer[offset + 2] != (byte)'p' ||
                buffer[offset + 3] != (byte)'o' ||
                buffer[offset + 4] != (byte)'s')
            {
                XmlExceptionHelper.ThrowInvalidCharRef(_reader);
            }
            return (int)'\'';
        }

        private int GetDecimalCharEntity(int offset, int length)
        {
            byte[] buffer = _buffer;
            Debug.Assert(buffer[offset + 0] == '&');
            Debug.Assert(buffer[offset + 1] == '#');
            Debug.Assert(buffer[offset + length - 1] == ';');
            int value = 0;
            for (int i = 2; i < length - 1; i++)
            {
                byte ch = buffer[offset + i];
                if (ch < (byte)'0' || ch > (byte)'9')
                    XmlExceptionHelper.ThrowInvalidCharRef(_reader);
                value = value * 10 + (ch - '0');
                if (value > SurrogateChar.MaxValue)
                    XmlExceptionHelper.ThrowInvalidCharRef(_reader);
            }
            return value;
        }

        private int GetHexCharEntity(int offset, int length)
        {
            byte[] buffer = _buffer;
            Debug.Assert(buffer[offset + 0] == '&');
            Debug.Assert(buffer[offset + 1] == '#');
            Debug.Assert(buffer[offset + 2] == 'x');
            Debug.Assert(buffer[offset + length - 1] == ';');
            int value = 0;
            for (int i = 3; i < length - 1; i++)
            {
                byte ch = buffer[offset + i];
                int digit = HexConverter.FromChar(ch);
                if (digit == 0xFF)
                    XmlExceptionHelper.ThrowInvalidCharRef(_reader);
                Debug.Assert(digit >= 0 && digit < 16);
                value = value * 16 + digit;
                if (value > SurrogateChar.MaxValue)
                    XmlExceptionHelper.ThrowInvalidCharRef(_reader);
            }
            return value;
        }

        public int GetCharEntity(int offset, int length)
        {
            if (length < 3)
                XmlExceptionHelper.ThrowInvalidCharRef(_reader);
            byte[] buffer = _buffer;
            Debug.Assert(buffer[offset] == '&');
            Debug.Assert(buffer[offset + length - 1] == ';');
            switch (buffer[offset + 1])
            {
                case (byte)'l':
                    return GetLessThanCharEntity(offset, length);
                case (byte)'g':
                    return GetGreaterThanCharEntity(offset, length);
                case (byte)'a':
                    if (buffer[offset + 2] == (byte)'m')
                        return GetAmpersandCharEntity(offset, length);
                    else
                        return GetApostropheCharEntity(offset, length);
                case (byte)'q':
                    return GetQuoteCharEntity(offset, length);
                case (byte)'#':
                    if (buffer[offset + 2] == (byte)'x')
                        return GetHexCharEntity(offset, length);
                    else
                        return GetDecimalCharEntity(offset, length);
                default:
                    XmlExceptionHelper.ThrowInvalidCharRef(_reader);
                    return 0;
            }
        }


        public bool IsWhitespaceKey(int key)
        {
            string s = GetDictionaryString(key).Value;
            return XmlConverter.IsWhitespace(s);
        }

        public bool IsWhitespaceUTF8(int offset, int length)
        {
            return XmlConverter.IsWhitespace(_buffer.AsSpan(offset, length));
        }

        public bool IsWhitespaceUnicode(int offset, int length)
        {
            for (int i = 0; i < length; i += sizeof(char))
            {
                char ch = (char)GetInt16(offset + i);
                if (!XmlConverter.IsWhitespace(ch))
                    return false;
            }
            return true;
        }

        public bool Equals2(int key1, int key2, XmlBufferReader bufferReader2)
        {
            // If the keys aren't from the same dictionary, they still might be the same
            if (key1 == key2)
                return true;
            else
                return GetDictionaryString(key1).Value == bufferReader2.GetDictionaryString(key2).Value;
        }

        public bool Equals2(int key1, XmlDictionaryString xmlString2)
        {
            if ((key1 & 1) == 0 && xmlString2.Dictionary == _dictionary)
                return xmlString2.Key == (key1 >> 1);
            else
                return GetDictionaryString(key1).Value == xmlString2.Value;
        }

        public bool Equals2(int offset1, int length1, byte[] buffer2)
        {
            int length2 = buffer2.Length;
            if (length1 != length2)
                return false;
            byte[] buffer1 = _buffer;
            for (int i = 0; i < length1; i++)
            {
                if (buffer1[offset1 + i] != buffer2[i])
                    return false;
            }
            return true;
        }

        public bool Equals2(int offset1, int length1, XmlBufferReader bufferReader2, int offset2, int length2)
        {
            if (length1 != length2)
                return false;
            byte[] buffer1 = _buffer;
            byte[] buffer2 = bufferReader2._buffer;
            for (int i = 0; i < length1; i++)
            {
                if (buffer1[offset1 + i] != buffer2[offset2 + i])
                    return false;
            }
            return true;
        }

        public bool Equals2(int offset1, int length1, int offset2, int length2)
        {
            if (length1 != length2)
                return false;
            if (offset1 == offset2)
                return true;
            byte[] buffer = _buffer;
            for (int i = 0; i < length1; i++)
            {
                if (buffer[offset1 + i] != buffer[offset2 + i])
                    return false;
            }
            return true;
        }

        public unsafe bool Equals2(int offset1, int length1, string s2)
        {
            int byteLength = length1;
            int charLength = s2.Length;

            // N Unicode chars will be represented in at least N bytes, but
            // no more than N * 3 bytes.  If the byte count falls outside of this
            // range, then the strings cannot be equal.
            if (byteLength < charLength || byteLength > charLength * maxBytesPerChar)
                return false;

            byte[] buffer = _buffer;
            if (length1 < 8)
            {
                int length = Math.Min(byteLength, charLength);
                int offset = offset1;
                for (int i = 0; i < length; i++)
                {
                    byte b = buffer[offset + i];
                    if (b >= 0x80)
                        return XmlConverter.ToString(buffer, offset1, length1) == s2;
                    if (s2[i] != (char)b)
                        return false;
                }
                return byteLength == charLength;
            }
            else
            {
                int length = Math.Min(byteLength, charLength);
                fixed (byte* _pb = &buffer[offset1])
                {
                    byte* pb = _pb;
                    byte* pbMax = pb + length;
                    fixed (char* _pch = s2)
                    {
                        char* pch = _pch;
                        // Try to do the fast comparison in ASCII space
                        int t = 0;
                        while (pb < pbMax && *pb < 0x80)
                        {
                            t = *pb - (byte)*pch;
                            // The code generated is better if we break out then return
                            if (t != 0)
                                break;
                            pb++;
                            pch++;
                        }
                        if (t != 0)
                            return false;
                        if (pb == pbMax)
                            return (byteLength == charLength);
                    }
                }
                return XmlConverter.ToString(buffer, offset1, length1) == s2;
            }
        }

        public int Compare(int offset1, int length1, int offset2, int length2)
        {
            byte[] buffer = _buffer;
            int length = Math.Min(length1, length2);
            for (int i = 0; i < length; i++)
            {
                int s = buffer[offset1 + i] - buffer[offset2 + i];
                if (s != 0)
                    return s;
            }
            return length1 - length2;
        }

        public byte GetByte(int offset)
        {
            return _buffer[offset];
        }

        public int GetInt8(int offset)
        {
            return (sbyte)GetByte(offset);
        }

#pragma warning disable 8500 // sizeof of managed types
        private unsafe T ReadRawBytes<T>() where T : unmanaged
        {
            ReadOnlySpan<byte> buffer = GetBuffer(sizeof(T), out int offset)
                .AsSpan(offset, sizeof(T));
            T value = MemoryMarshal.Read<T>(buffer);

            Advance(sizeof(T));
            return value;
        }

        private unsafe T ReadRawBytes<T>(int offset) where T : unmanaged
            => MemoryMarshal.Read<T>(_buffer.AsSpan(offset, sizeof(T)));
#pragma warning restore 8500

        public int GetInt16(int offset)
            => BitConverter.IsLittleEndian ? ReadRawBytes<short>(offset) : BinaryPrimitives.ReverseEndianness(ReadRawBytes<short>(offset));

        public int GetInt32(int offset)
            => BitConverter.IsLittleEndian ? ReadRawBytes<int>(offset) : BinaryPrimitives.ReverseEndianness(ReadRawBytes<int>(offset));

        public long GetInt64(int offset)
            => BitConverter.IsLittleEndian ? ReadRawBytes<long>(offset) : BinaryPrimitives.ReverseEndianness(ReadRawBytes<long>(offset));

        public ulong GetUInt64(int offset)
            => (ulong)GetInt64(offset);

        public float GetSingle(int offset)
            => BinaryPrimitives.ReadSingleLittleEndian(_buffer.AsSpan(offset, sizeof(float)));

        public double GetDouble(int offset)
            => BinaryPrimitives.ReadDoubleLittleEndian(_buffer.AsSpan(offset, sizeof(double)));

        public decimal GetDecimal(int offset)
        {
            if (BitConverter.IsLittleEndian)
            {
                return ReadRawBytes<decimal>(offset);
            }
            else
            {
                ReadOnlySpan<byte> bytes = _buffer.AsSpan(offset, sizeof(decimal));
                ReadOnlySpan<int> span = stackalloc int[4]
                {
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(8, 4)),
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(12, 4)),
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(4, 4)),
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(0, 4))
                };

                return new decimal(span);
            }
        }

        public UniqueId GetUniqueId(int offset)
            => new UniqueId(_buffer, offset);

        public Guid GetGuid(int offset)
            => new Guid(_buffer.AsSpan(offset, ValueHandleLength.Guid));

        public void GetBase64(int srcOffset, byte[] buffer, int dstOffset, int count)
        {
            System.Buffer.BlockCopy(_buffer, srcOffset, buffer, dstOffset, count);
        }


        public XmlBinaryNodeType GetNodeType()
        {
            return (XmlBinaryNodeType)GetByte();
        }

        public void SkipNodeType()
        {
            SkipByte();
        }

        public object[] GetList(int offset, int count)
        {
            int bufferOffset = this.Offset;
            this.Offset = offset;
            try
            {
                object[] objects = new object[count];
                for (int i = 0; i < count; i++)
                {
                    XmlBinaryNodeType nodeType = GetNodeType();
                    SkipNodeType();
                    Debug.Assert(nodeType != XmlBinaryNodeType.StartListText);
                    ReadValue(nodeType, _listValue!);
                    objects[i] = _listValue!.ToObject();
                }
                return objects;
            }
            finally
            {
                this.Offset = bufferOffset;
            }
        }


        public XmlDictionaryString GetDictionaryString(int key)
        {
            IXmlDictionary keyDictionary;
            if ((key & 1) != 0)
            {
                keyDictionary = _session!;
            }
            else
            {
                keyDictionary = _dictionary!;
            }

            XmlDictionaryString? s;
            if (!keyDictionary.TryLookup(key >> 1, out s))
                XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);

            return s;
        }

        public int ReadDictionaryKey()
        {
            int key = ReadMultiByteUInt31();
            if ((key & 1) != 0)
            {
                if (_session == null)
                    XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
                int sessionKey = (key >> 1);
                if (!_session.TryLookup(sessionKey, out _))
                {
                    if (sessionKey < XmlDictionaryString.MinKey || sessionKey > XmlDictionaryString.MaxKey)
                        XmlExceptionHelper.ThrowXmlDictionaryStringIDOutOfRange(_reader);
                    XmlExceptionHelper.ThrowXmlDictionaryStringIDUndefinedSession(_reader, sessionKey);
                }
            }
            else
            {
                if (_dictionary == null)
                    XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
                int staticKey = (key >> 1);
                if (!_dictionary.TryLookup(staticKey, out _))
                {
                    if (staticKey < XmlDictionaryString.MinKey || staticKey > XmlDictionaryString.MaxKey)
                        XmlExceptionHelper.ThrowXmlDictionaryStringIDOutOfRange(_reader);
                    XmlExceptionHelper.ThrowXmlDictionaryStringIDUndefinedStatic(_reader, staticKey);
                }
            }

            return key;
        }

        public void ReadValue(XmlBinaryNodeType nodeType, ValueHandle value)
        {
            switch (nodeType)
            {
                case XmlBinaryNodeType.EmptyText:
                    value.SetValue(ValueHandleType.Empty);
                    break;
                case XmlBinaryNodeType.ZeroText:
                    value.SetValue(ValueHandleType.Zero);
                    break;
                case XmlBinaryNodeType.OneText:
                    value.SetValue(ValueHandleType.One);
                    break;
                case XmlBinaryNodeType.TrueText:
                    value.SetValue(ValueHandleType.True);
                    break;
                case XmlBinaryNodeType.FalseText:
                    value.SetValue(ValueHandleType.False);
                    break;
                case XmlBinaryNodeType.BoolText:
                    value.SetValue(ReadUInt8() != 0 ? ValueHandleType.True : ValueHandleType.False);
                    break;
                case XmlBinaryNodeType.Chars8Text:
                    ReadValue(value, ValueHandleType.UTF8, ReadUInt8());
                    break;
                case XmlBinaryNodeType.Chars16Text:
                    ReadValue(value, ValueHandleType.UTF8, ReadUInt16());
                    break;
                case XmlBinaryNodeType.Chars32Text:
                    ReadValue(value, ValueHandleType.UTF8, ReadUInt31());
                    break;
                case XmlBinaryNodeType.UnicodeChars8Text:
                    ReadUnicodeValue(value, ReadUInt8());
                    break;
                case XmlBinaryNodeType.UnicodeChars16Text:
                    ReadUnicodeValue(value, ReadUInt16());
                    break;
                case XmlBinaryNodeType.UnicodeChars32Text:
                    ReadUnicodeValue(value, ReadUInt31());
                    break;
                case XmlBinaryNodeType.Bytes8Text:
                    ReadValue(value, ValueHandleType.Base64, ReadUInt8());
                    break;
                case XmlBinaryNodeType.Bytes16Text:
                    ReadValue(value, ValueHandleType.Base64, ReadUInt16());
                    break;
                case XmlBinaryNodeType.Bytes32Text:
                    ReadValue(value, ValueHandleType.Base64, ReadUInt31());
                    break;
                case XmlBinaryNodeType.DictionaryText:
                    value.SetDictionaryValue(ReadDictionaryKey());
                    break;
                case XmlBinaryNodeType.UniqueIdText:
                    ReadValue(value, ValueHandleType.UniqueId, ValueHandleLength.UniqueId);
                    break;
                case XmlBinaryNodeType.GuidText:
                    ReadValue(value, ValueHandleType.Guid, ValueHandleLength.Guid);
                    break;
                case XmlBinaryNodeType.DecimalText:
                    ReadValue(value, ValueHandleType.Decimal, ValueHandleLength.Decimal);
                    break;
                case XmlBinaryNodeType.Int8Text:
                    ReadValue(value, ValueHandleType.Int8, ValueHandleLength.Int8);
                    break;
                case XmlBinaryNodeType.Int16Text:
                    ReadValue(value, ValueHandleType.Int16, ValueHandleLength.Int16);
                    break;
                case XmlBinaryNodeType.Int32Text:
                    ReadValue(value, ValueHandleType.Int32, ValueHandleLength.Int32);
                    break;
                case XmlBinaryNodeType.Int64Text:
                    ReadValue(value, ValueHandleType.Int64, ValueHandleLength.Int64);
                    break;
                case XmlBinaryNodeType.UInt64Text:
                    ReadValue(value, ValueHandleType.UInt64, ValueHandleLength.UInt64);
                    break;
                case XmlBinaryNodeType.FloatText:
                    ReadValue(value, ValueHandleType.Single, ValueHandleLength.Single);
                    break;
                case XmlBinaryNodeType.DoubleText:
                    ReadValue(value, ValueHandleType.Double, ValueHandleLength.Double);
                    break;
                case XmlBinaryNodeType.TimeSpanText:
                    ReadValue(value, ValueHandleType.TimeSpan, ValueHandleLength.TimeSpan);
                    break;
                case XmlBinaryNodeType.DateTimeText:
                    ReadValue(value, ValueHandleType.DateTime, ValueHandleLength.DateTime);
                    break;
                case XmlBinaryNodeType.StartListText:
                    ReadList(value);
                    break;
                case XmlBinaryNodeType.QNameDictionaryText:
                    ReadQName(value);
                    break;
                default:
                    XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
                    break;
            }
        }

        private void ReadValue(ValueHandle value, ValueHandleType type, int length)
        {
            int offset = ReadBytes(length);
            value.SetValue(type, offset, length);
        }

        private void ReadUnicodeValue(ValueHandle value, int length)
        {
            if ((length & 1) != 0)
                XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
            ReadValue(value, ValueHandleType.Unicode, length);
        }

        private void ReadList(ValueHandle value)
        {
            _listValue ??= new ValueHandle(this);
            int count = 0;
            int offset = this.Offset;
            while (true)
            {
                XmlBinaryNodeType nodeType = GetNodeType();
                SkipNodeType();
                if (nodeType == XmlBinaryNodeType.StartListText)
                    XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
                if (nodeType == XmlBinaryNodeType.EndListText)
                    break;
                ReadValue(nodeType, _listValue);
                count++;
            }
            value.SetValue(ValueHandleType.List, offset, count);
        }


        public void ReadQName(ValueHandle value)
        {
            int prefix = ReadUInt8();
            if (prefix >= 26)
                XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
            int key = ReadDictionaryKey();
            value.SetQNameValue(prefix, key);
        }

        public int[] GetRows()
        {
            if (_buffer == null)
            {
                return new int[1] { 0 };
            }

            List<int> list = new List<int>();
            list.Add(_offsetMin);
            for (int i = _offsetMin; i < _offsetMax; i++)
            {
                if (_buffer[i] == (byte)13 || _buffer[i] == (byte)10)
                {
                    if (i + 1 < _offsetMax && _buffer[i + 1] == (byte)10)
                        i++;
                    list.Add(i + 1);
                }
            }
            return list.ToArray();
        }
    }
}
