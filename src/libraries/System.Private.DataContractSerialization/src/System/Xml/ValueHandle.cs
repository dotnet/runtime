// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Text;


namespace System.Xml
{
    internal enum ValueHandleConstStringType
    {
        String = 0,
        Number = 1,
        Array = 2,
        Object = 3,
        Boolean = 4,
        Null = 5,
    }

    internal static class ValueHandleLength
    {
        public const int Int8 = 1;
        public const int Int16 = 2;
        public const int Int32 = 4;
        public const int Int64 = 8;
        public const int UInt64 = 8;
        public const int Single = 4;
        public const int Double = 8;
        public const int Decimal = 16;
        public const int DateTime = 8;
        public const int TimeSpan = 8;
        public const int Guid = 16;
        public const int UniqueId = 16;
    }

    internal enum ValueHandleType
    {
        Empty,
        True,
        False,
        Zero,
        One,
        Int8,
        Int16,
        Int32,
        Int64,
        UInt64,
        Single,
        Double,
        Decimal,
        DateTime,
        TimeSpan,
        Guid,
        UniqueId,
        UTF8,
        EscapedUTF8,
        Base64,
        Dictionary,
        List,
        Char,
        Unicode,
        QName,
        ConstString
    }

    internal class ValueHandle
    {
        private XmlBufferReader _bufferReader;
        private ValueHandleType _type;
        private int _offset;
        private int _length;
        private static Base64Encoding s_base64Encoding;
        private static string[] s_constStrings = {
                                        "string",
                                        "number",
                                        "array",
                                        "object",
                                        "boolean",
                                        "null",
                                       };

        public ValueHandle(XmlBufferReader bufferReader)
        {
            _bufferReader = bufferReader;
            _type = ValueHandleType.Empty;
        }

        private static Base64Encoding Base64Encoding
        {
            get
            {
                if (s_base64Encoding == null)
                    s_base64Encoding = new Base64Encoding();
                return s_base64Encoding;
            }
        }
        public void SetConstantValue(ValueHandleConstStringType constStringType)
        {
            _type = ValueHandleType.ConstString;
            _offset = (int)constStringType;
        }

        public void SetValue(ValueHandleType type)
        {
            _type = type;
        }

        public void SetDictionaryValue(int key)
        {
            SetValue(ValueHandleType.Dictionary, key, 0);
        }

        public void SetCharValue(int ch)
        {
            SetValue(ValueHandleType.Char, ch, 0);
        }

        public void SetQNameValue(int prefix, int key)
        {
            SetValue(ValueHandleType.QName, key, prefix);
        }
        public void SetValue(ValueHandleType type, int offset, int length)
        {
            _type = type;
            _offset = offset;
            _length = length;
        }

        public bool IsWhitespace()
        {
            switch (_type)
            {
                case ValueHandleType.UTF8:
                    return _bufferReader.IsWhitespaceUTF8(_offset, _length);

                case ValueHandleType.Dictionary:
                    return _bufferReader.IsWhitespaceKey(_offset);

                case ValueHandleType.Char:
                    int ch = GetChar();
                    if (ch > char.MaxValue)
                        return false;
                    return XmlConverter.IsWhitespace((char)ch);

                case ValueHandleType.EscapedUTF8:
                    return _bufferReader.IsWhitespaceUTF8(_offset, _length);

                case ValueHandleType.Unicode:
                    return _bufferReader.IsWhitespaceUnicode(_offset, _length);

                case ValueHandleType.True:
                case ValueHandleType.False:
                case ValueHandleType.Zero:
                case ValueHandleType.One:
                    return false;

                case ValueHandleType.ConstString:
                    return s_constStrings[_offset].Length == 0;

                default:
                    return _length == 0;
            }
        }

        public Type ToType()
        {
            switch (_type)
            {
                case ValueHandleType.False:
                case ValueHandleType.True:
                    return typeof(bool);
                case ValueHandleType.Zero:
                case ValueHandleType.One:
                case ValueHandleType.Int8:
                case ValueHandleType.Int16:
                case ValueHandleType.Int32:
                    return typeof(int);
                case ValueHandleType.Int64:
                    return typeof(long);
                case ValueHandleType.UInt64:
                    return typeof(ulong);
                case ValueHandleType.Single:
                    return typeof(float);
                case ValueHandleType.Double:
                    return typeof(double);
                case ValueHandleType.Decimal:
                    return typeof(decimal);
                case ValueHandleType.DateTime:
                    return typeof(DateTime);
                case ValueHandleType.Empty:
                case ValueHandleType.UTF8:
                case ValueHandleType.Unicode:
                case ValueHandleType.EscapedUTF8:
                case ValueHandleType.Dictionary:
                case ValueHandleType.Char:
                case ValueHandleType.QName:
                case ValueHandleType.ConstString:
                    return typeof(string);
                case ValueHandleType.Base64:
                    return typeof(byte[]);
                case ValueHandleType.List:
                    return typeof(object[]);
                case ValueHandleType.UniqueId:
                    return typeof(UniqueId);
                case ValueHandleType.Guid:
                    return typeof(Guid);
                case ValueHandleType.TimeSpan:
                    return typeof(TimeSpan);
                default:
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException());
            }
        }

        public bool ToBoolean()
        {
            ValueHandleType type = _type;
            if (type == ValueHandleType.False)
                return false;
            if (type == ValueHandleType.True)
                return true;
            if (type == ValueHandleType.UTF8)
                return XmlConverter.ToBoolean(_bufferReader.Buffer, _offset, _length);
            if (type == ValueHandleType.Int8)
            {
                int value = GetInt8();
                if (value == 0)
                    return false;
                if (value == 1)
                    return true;
            }
            return XmlConverter.ToBoolean(GetString());
        }

        public int ToInt()
        {
            ValueHandleType type = _type;
            if (type == ValueHandleType.Zero)
                return 0;
            if (type == ValueHandleType.One)
                return 1;
            if (type == ValueHandleType.Int8)
                return GetInt8();
            if (type == ValueHandleType.Int16)
                return GetInt16();
            if (type == ValueHandleType.Int32)
                return GetInt32();
            if (type == ValueHandleType.Int64)
            {
                long value = GetInt64();
                if (value >= int.MinValue && value <= int.MaxValue)
                {
                    return (int)value;
                }
            }
            if (type == ValueHandleType.UInt64)
            {
                ulong value = GetUInt64();
                if (value <= int.MaxValue)
                {
                    return (int)value;
                }
            }
            if (type == ValueHandleType.UTF8)
                return XmlConverter.ToInt32(_bufferReader.Buffer, _offset, _length);
            return XmlConverter.ToInt32(GetString());
        }

        public long ToLong()
        {
            ValueHandleType type = _type;
            if (type == ValueHandleType.Zero)
                return 0;
            if (type == ValueHandleType.One)
                return 1;
            if (type == ValueHandleType.Int8)
                return GetInt8();
            if (type == ValueHandleType.Int16)
                return GetInt16();
            if (type == ValueHandleType.Int32)
                return GetInt32();
            if (type == ValueHandleType.Int64)
                return GetInt64();
            if (type == ValueHandleType.UInt64)
            {
                ulong value = GetUInt64();
                if (value <= long.MaxValue)
                {
                    return (long)value;
                }
            }
            if (type == ValueHandleType.UTF8)
            {
                return XmlConverter.ToInt64(_bufferReader.Buffer, _offset, _length);
            }
            return XmlConverter.ToInt64(GetString());
        }

        public ulong ToULong()
        {
            ValueHandleType type = _type;
            if (type == ValueHandleType.Zero)
                return 0;
            if (type == ValueHandleType.One)
                return 1;
            if (type >= ValueHandleType.Int8 && type <= ValueHandleType.Int64)
            {
                long value = ToLong();
                if (value >= 0)
                    return (ulong)value;
            }
            if (type == ValueHandleType.UInt64)
                return GetUInt64();
            if (type == ValueHandleType.UTF8)
                return XmlConverter.ToUInt64(_bufferReader.Buffer, _offset, _length);
            return XmlConverter.ToUInt64(GetString());
        }

        public float ToSingle()
        {
            ValueHandleType type = _type;
            if (type == ValueHandleType.Single)
                return GetSingle();
            if (type == ValueHandleType.Double)
            {
                double value = GetDouble();

                if ((value >= float.MinValue && value <= float.MaxValue) || !double.IsFinite(value))
                {
                    return (float)value;
                }
            }
            if (type == ValueHandleType.Zero)
                return 0;
            if (type == ValueHandleType.One)
                return 1;
            if (type == ValueHandleType.Int8)
                return GetInt8();
            if (type == ValueHandleType.Int16)
                return GetInt16();
            if (type == ValueHandleType.UTF8)
                return XmlConverter.ToSingle(_bufferReader.Buffer, _offset, _length);
            return XmlConverter.ToSingle(GetString());
        }

        public double ToDouble()
        {
            ValueHandleType type = _type;
            if (type == ValueHandleType.Double)
                return GetDouble();
            if (type == ValueHandleType.Single)
                return GetSingle();
            if (type == ValueHandleType.Zero)
                return 0;
            if (type == ValueHandleType.One)
                return 1;
            if (type == ValueHandleType.Int8)
                return GetInt8();
            if (type == ValueHandleType.Int16)
                return GetInt16();
            if (type == ValueHandleType.Int32)
                return GetInt32();
            if (type == ValueHandleType.UTF8)
                return XmlConverter.ToDouble(_bufferReader.Buffer, _offset, _length);
            return XmlConverter.ToDouble(GetString());
        }

        public decimal ToDecimal()
        {
            ValueHandleType type = _type;
            if (type == ValueHandleType.Decimal)
                return GetDecimal();
            if (type == ValueHandleType.Zero)
                return 0;
            if (type == ValueHandleType.One)
                return 1;
            if (type >= ValueHandleType.Int8 && type <= ValueHandleType.Int64)
                return ToLong();
            if (type == ValueHandleType.UInt64)
                return GetUInt64();
            if (type == ValueHandleType.UTF8)
                return XmlConverter.ToDecimal(_bufferReader.Buffer, _offset, _length);
            return XmlConverter.ToDecimal(GetString());
        }

        public DateTime ToDateTime()
        {
            if (_type == ValueHandleType.DateTime)
            {
                return XmlConverter.ToDateTime(GetInt64());
            }
            if (_type == ValueHandleType.UTF8)
            {
                return XmlConverter.ToDateTime(_bufferReader.Buffer, _offset, _length);
            }
            return XmlConverter.ToDateTime(GetString());
        }

        public UniqueId ToUniqueId()
        {
            if (_type == ValueHandleType.UniqueId)
                return GetUniqueId();
            if (_type == ValueHandleType.UTF8)
                return XmlConverter.ToUniqueId(_bufferReader.Buffer, _offset, _length);
            return XmlConverter.ToUniqueId(GetString());
        }

        public TimeSpan ToTimeSpan()
        {
            if (_type == ValueHandleType.TimeSpan)
                return new TimeSpan(GetInt64());
            if (_type == ValueHandleType.UTF8)
                return XmlConverter.ToTimeSpan(_bufferReader.Buffer, _offset, _length);
            return XmlConverter.ToTimeSpan(GetString());
        }

        public Guid ToGuid()
        {
            if (_type == ValueHandleType.Guid)
                return GetGuid();
            if (_type == ValueHandleType.UTF8)
                return XmlConverter.ToGuid(_bufferReader.Buffer, _offset, _length);
            return XmlConverter.ToGuid(GetString());
        }
        public override string ToString()
        {
            return GetString();
        }

        public byte[] ToByteArray()
        {
            if (_type == ValueHandleType.Base64)
            {
                byte[] buffer = new byte[_length];
                GetBase64(buffer, 0, _length);
                return buffer;
            }
            if (_type == ValueHandleType.UTF8 && (_length % 4) == 0)
            {
                try
                {
                    int expectedLength = _length / 4 * 3;
                    if (_length > 0)
                    {
                        if (_bufferReader.Buffer[_offset + _length - 1] == '=')
                        {
                            expectedLength--;
                            if (_bufferReader.Buffer[_offset + _length - 2] == '=')
                                expectedLength--;
                        }
                    }
                    byte[] buffer = new byte[expectedLength];
                    int actualLength = Base64Encoding.GetBytes(_bufferReader.Buffer, _offset, _length, buffer, 0);
                    if (actualLength != buffer.Length)
                    {
                        byte[] newBuffer = new byte[actualLength];
                        Buffer.BlockCopy(buffer, 0, newBuffer, 0, actualLength);
                        buffer = newBuffer;
                    }
                    return buffer;
                }
                catch (FormatException)
                {
                    // Something unhappy with the characters, fall back to the hard way
                }
            }
            try
            {
                return Base64Encoding.GetBytes(XmlConverter.StripWhitespace(GetString()));
            }
            catch (FormatException exception)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new XmlException(exception.Message, exception.InnerException));
            }
        }

        public string GetString()
        {
            ValueHandleType type = _type;
            if (type == ValueHandleType.UTF8)
                return GetCharsText();

            switch (type)
            {
                case ValueHandleType.False:
                    return "false";
                case ValueHandleType.True:
                    return "true";
                case ValueHandleType.Zero:
                    return "0";
                case ValueHandleType.One:
                    return "1";
                case ValueHandleType.Int8:
                case ValueHandleType.Int16:
                case ValueHandleType.Int32:
                    return XmlConverter.ToString(ToInt());
                case ValueHandleType.Int64:
                    return XmlConverter.ToString(GetInt64());
                case ValueHandleType.UInt64:
                    return XmlConverter.ToString(GetUInt64());
                case ValueHandleType.Single:
                    return XmlConverter.ToString(GetSingle());
                case ValueHandleType.Double:
                    return XmlConverter.ToString(GetDouble());
                case ValueHandleType.Decimal:
                    return XmlConverter.ToString(GetDecimal());
                case ValueHandleType.DateTime:
                    return XmlConverter.ToString(ToDateTime());
                case ValueHandleType.Empty:
                    return string.Empty;
                case ValueHandleType.Unicode:
                    return GetUnicodeCharsText();
                case ValueHandleType.EscapedUTF8:
                    return GetEscapedCharsText();
                case ValueHandleType.Char:
                    return GetCharText();
                case ValueHandleType.Dictionary:
                    return GetDictionaryString().Value;
                case ValueHandleType.Base64:
                    byte[] bytes = ToByteArray();
                    DiagnosticUtility.DebugAssert(bytes != null, "");
                    return Base64Encoding.GetString(bytes, 0, bytes.Length);
                case ValueHandleType.List:
                    return XmlConverter.ToString(ToList());
                case ValueHandleType.UniqueId:
                    return XmlConverter.ToString(ToUniqueId());
                case ValueHandleType.Guid:
                    return XmlConverter.ToString(ToGuid());
                case ValueHandleType.TimeSpan:
                    return XmlConverter.ToString(ToTimeSpan());
                case ValueHandleType.QName:
                    return GetQNameDictionaryText();
                case ValueHandleType.ConstString:
                    return s_constStrings[_offset];
                default:
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException());
            }
        }

        // ASSUMPTION (Microsoft): all chars in str will be ASCII
        public bool Equals2(string str, bool checkLower)
        {
            if (_type != ValueHandleType.UTF8)
                return GetString() == str;

            if (_length != str.Length)
                return false;

            byte[] buffer = _bufferReader.Buffer;
            for (int i = 0; i < _length; ++i)
            {
                DiagnosticUtility.DebugAssert(str[i] < 128, "");
                byte ch = buffer[i + _offset];
                if (ch == str[i])
                    continue;

                if (checkLower && char.ToLowerInvariant((char)ch) == str[i])
                    continue;

                return false;
            }

            return true;
        }

        public void Sign(XmlSigningNodeWriter writer)
        {
            switch (_type)
            {
                case ValueHandleType.Int8:
                case ValueHandleType.Int16:
                case ValueHandleType.Int32:
                    writer.WriteInt32Text(ToInt());
                    break;
                case ValueHandleType.Int64:
                    writer.WriteInt64Text(GetInt64());
                    break;
                case ValueHandleType.UInt64:
                    writer.WriteUInt64Text(GetUInt64());
                    break;
                case ValueHandleType.Single:
                    writer.WriteFloatText(GetSingle());
                    break;
                case ValueHandleType.Double:
                    writer.WriteDoubleText(GetDouble());
                    break;
                case ValueHandleType.Decimal:
                    writer.WriteDecimalText(GetDecimal());
                    break;
                case ValueHandleType.DateTime:
                    writer.WriteDateTimeText(ToDateTime());
                    break;
                case ValueHandleType.Empty:
                    break;
                case ValueHandleType.UTF8:
                    writer.WriteEscapedText(_bufferReader.Buffer, _offset, _length);
                    break;
                case ValueHandleType.Base64:
                    writer.WriteBase64Text(_bufferReader.Buffer, 0, _bufferReader.Buffer, _offset, _length);
                    break;
                case ValueHandleType.UniqueId:
                    writer.WriteUniqueIdText(ToUniqueId());
                    break;
                case ValueHandleType.Guid:
                    writer.WriteGuidText(ToGuid());
                    break;
                case ValueHandleType.TimeSpan:
                    writer.WriteTimeSpanText(ToTimeSpan());
                    break;
                default:
                    writer.WriteEscapedText(GetString());
                    break;
            }
        }

        public object[] ToList()
        {
            return _bufferReader.GetList(_offset, _length);
        }

        public object ToObject()
        {
            switch (_type)
            {
                case ValueHandleType.False:
                case ValueHandleType.True:
                    return ToBoolean();
                case ValueHandleType.Zero:
                case ValueHandleType.One:
                case ValueHandleType.Int8:
                case ValueHandleType.Int16:
                case ValueHandleType.Int32:
                    return ToInt();
                case ValueHandleType.Int64:
                    return ToLong();
                case ValueHandleType.UInt64:
                    return GetUInt64();
                case ValueHandleType.Single:
                    return ToSingle();
                case ValueHandleType.Double:
                    return ToDouble();
                case ValueHandleType.Decimal:
                    return ToDecimal();
                case ValueHandleType.DateTime:
                    return ToDateTime();
                case ValueHandleType.Empty:
                case ValueHandleType.UTF8:
                case ValueHandleType.Unicode:
                case ValueHandleType.EscapedUTF8:
                case ValueHandleType.Dictionary:
                case ValueHandleType.Char:
                case ValueHandleType.ConstString:
                    return ToString();
                case ValueHandleType.Base64:
                    return ToByteArray();
                case ValueHandleType.List:
                    return ToList();
                case ValueHandleType.UniqueId:
                    return ToUniqueId();
                case ValueHandleType.Guid:
                    return ToGuid();
                case ValueHandleType.TimeSpan:
                    return ToTimeSpan();
                default:
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException());
            }
        }

        public bool TryReadBase64(byte[] buffer, int offset, int count, out int actual)
        {
            if (_type == ValueHandleType.Base64)
            {
                actual = Math.Min(_length, count);
                GetBase64(buffer, offset, actual);
                _offset += actual;
                _length -= actual;
                return true;
            }
            if (_type == ValueHandleType.UTF8 && count >= 3 && (_length % 4) == 0)
            {
                try
                {
                    int charCount = Math.Min(count / 3 * 4, _length);
                    actual = Base64Encoding.GetBytes(_bufferReader.Buffer, _offset, charCount, buffer, offset);
                    _offset += charCount;
                    _length -= charCount;
                    return true;
                }
                catch (FormatException)
                {
                    // Something unhappy with the characters, fall back to the hard way
                }
            }
            actual = 0;
            return false;
        }

        public bool TryReadChars(char[] chars, int offset, int count, out int actual)
        {
            DiagnosticUtility.DebugAssert(offset + count <= chars.Length, string.Format("offset '{0}' + count '{1}' MUST BE <= chars.Length '{2}'", offset, count, chars.Length));

            if (_type == ValueHandleType.Unicode)
                return TryReadUnicodeChars(chars, offset, count, out actual);

            if (_type != ValueHandleType.UTF8)
            {
                actual = 0;
                return false;
            }

            int charOffset = offset;
            int charCount = count;
            byte[] bytes = _bufferReader.Buffer;
            int byteOffset = _offset;
            int byteCount = _length;
            bool insufficientSpaceInCharsArray = false;

            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            while (true)
            {
                while (charCount > 0 && byteCount > 0)
                {
                    // fast path for codepoints U+0000 - U+007F
                    byte b = bytes[byteOffset];
                    if (b >= 0x80)
                        break;
                    chars[charOffset] = (char)b;
                    byteOffset++;
                    byteCount--;
                    charOffset++;
                    charCount--;
                }

                if (charCount == 0 || byteCount == 0 || insufficientSpaceInCharsArray)
                    break;

                int actualByteCount;
                int actualCharCount;

                try
                {
                    // If we're asking for more than are possibly available, or more than are truly available then we can return the entire thing
                    if (charCount >= encoding.GetMaxCharCount(byteCount) || charCount >= encoding.GetCharCount(bytes, byteOffset, byteCount))
                    {
                        actualCharCount = encoding.GetChars(bytes, byteOffset, byteCount, chars, charOffset);
                        actualByteCount = byteCount;
                    }
                    else
                    {
                        Decoder decoder = encoding.GetDecoder();

                        // Since x bytes can never generate more than x characters this is a safe estimate as to what will fit
                        actualByteCount = Math.Min(charCount, byteCount);

                        // We use a decoder so we don't error if we fall across a character boundary
                        actualCharCount = decoder.GetChars(bytes, byteOffset, actualByteCount, chars, charOffset);

                        // We might have gotten zero characters though if < 4 bytes were requested because
                        // codepoints from U+0000 - U+FFFF can be up to 3 bytes in UTF-8, and represented as ONE char
                        // codepoints from U+10000 - U+10FFFF (last Unicode codepoint representable in UTF-8) are represented by up to 4 bytes in UTF-8 
                        //                                    and represented as TWO chars (high+low surrogate)
                        // (e.g. 1 char requested, 1 char in the buffer represented in 3 bytes)
                        while (actualCharCount == 0)
                        {
                            // Note the by the time we arrive here, if actualByteCount == 3, the next decoder.GetChars() call will read the 4th byte
                            // if we don't bail out since the while loop will advance actualByteCount only after reading the byte. 
                            if (actualByteCount >= 3 && charCount < 2)
                            {
                                // If we reach here, it means that we're: 
                                // - trying to decode more than 3 bytes and, 
                                // - there is only one char left of charCount where we're stuffing decoded characters. 
                                // In this case, we need to back off since decoding > 3 bytes in UTF-8 means that we will get 2 16-bit chars 
                                // (a high surrogate and a low surrogate) - the Decoder will attempt to provide both at once 
                                // and an ArgumentException will be thrown complaining that there's not enough space in the output char array.  

                                // actualByteCount = 0 when the while loop is broken out of; decoder goes out of scope so its state no longer matters

                                insufficientSpaceInCharsArray = true;
                                break;
                            }
                            else
                            {
                                DiagnosticUtility.DebugAssert(byteOffset + actualByteCount < bytes.Length,
                                    string.Format("byteOffset {0} + actualByteCount {1} MUST BE < bytes.Length {2}", byteOffset, actualByteCount, bytes.Length));

                                // Request a few more bytes to get at least one character
                                actualCharCount = decoder.GetChars(bytes, byteOffset + actualByteCount, 1, chars, charOffset);
                                actualByteCount++;
                            }
                        }

                        // Now that we actually retrieved some characters, figure out how many bytes it actually was
                        actualByteCount = encoding.GetByteCount(chars, charOffset, actualCharCount);
                    }
                }
                catch (FormatException exception)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlExceptionHelper.CreateEncodingException(bytes, byteOffset, byteCount, exception));
                }

                // Advance
                byteOffset += actualByteCount;
                byteCount -= actualByteCount;

                charOffset += actualCharCount;
                charCount -= actualCharCount;
            }

            _offset = byteOffset;
            _length = byteCount;

            actual = (count - charCount);
            return true;
        }

        private bool TryReadUnicodeChars(char[] chars, int offset, int count, out int actual)
        {
            int charCount = Math.Min(count, _length / sizeof(char));
            for (int i = 0; i < charCount; i++)
            {
                chars[offset + i] = (char)_bufferReader.GetInt16(_offset + i * sizeof(char));
            }
            _offset += charCount * sizeof(char);
            _length -= charCount * sizeof(char);
            actual = charCount;
            return true;
        }

        public bool TryGetDictionaryString(out XmlDictionaryString value)
        {
            if (_type == ValueHandleType.Dictionary)
            {
                value = GetDictionaryString();
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public bool TryGetByteArrayLength(out int length)
        {
            if (_type == ValueHandleType.Base64)
            {
                length = _length;
                return true;
            }
            length = 0;
            return false;
        }
        private string GetCharsText()
        {
            DiagnosticUtility.DebugAssert(_type == ValueHandleType.UTF8, "");
            if (_length == 1 && _bufferReader.GetByte(_offset) == '1')
                return "1";
            return _bufferReader.GetString(_offset, _length);
        }

        private string GetUnicodeCharsText()
        {
            DiagnosticUtility.DebugAssert(_type == ValueHandleType.Unicode, "");
            return _bufferReader.GetUnicodeString(_offset, _length);
        }

        private string GetEscapedCharsText()
        {
            DiagnosticUtility.DebugAssert(_type == ValueHandleType.EscapedUTF8, "");
            return _bufferReader.GetEscapedString(_offset, _length);
        }
        private string GetCharText()
        {
            int ch = GetChar();
            if (ch > char.MaxValue)
            {
                SurrogateChar surrogate = new SurrogateChar(ch);
                char[] chars = new char[2];
                chars[0] = surrogate.HighChar;
                chars[1] = surrogate.LowChar;
                return new string(chars, 0, 2);
            }
            else
            {
                return ((char)ch).ToString();
            }
        }

        private int GetChar()
        {
            DiagnosticUtility.DebugAssert(_type == ValueHandleType.Char, "");
            return _offset;
        }

        private int GetInt8()
        {
            DiagnosticUtility.DebugAssert(_type == ValueHandleType.Int8, "");
            return _bufferReader.GetInt8(_offset);
        }

        private int GetInt16()
        {
            DiagnosticUtility.DebugAssert(_type == ValueHandleType.Int16, "");
            return _bufferReader.GetInt16(_offset);
        }

        private int GetInt32()
        {
            DiagnosticUtility.DebugAssert(_type == ValueHandleType.Int32, "");
            return _bufferReader.GetInt32(_offset);
        }

        private long GetInt64()
        {
            DiagnosticUtility.DebugAssert(_type == ValueHandleType.Int64 || _type == ValueHandleType.TimeSpan || _type == ValueHandleType.DateTime, "");
            return _bufferReader.GetInt64(_offset);
        }

        private ulong GetUInt64()
        {
            DiagnosticUtility.DebugAssert(_type == ValueHandleType.UInt64, "");
            return _bufferReader.GetUInt64(_offset);
        }

        private float GetSingle()
        {
            DiagnosticUtility.DebugAssert(_type == ValueHandleType.Single, "");
            return _bufferReader.GetSingle(_offset);
        }

        private double GetDouble()
        {
            DiagnosticUtility.DebugAssert(_type == ValueHandleType.Double, "");
            return _bufferReader.GetDouble(_offset);
        }

        private decimal GetDecimal()
        {
            DiagnosticUtility.DebugAssert(_type == ValueHandleType.Decimal, "");
            return _bufferReader.GetDecimal(_offset);
        }

        private UniqueId GetUniqueId()
        {
            DiagnosticUtility.DebugAssert(_type == ValueHandleType.UniqueId, "");
            return _bufferReader.GetUniqueId(_offset);
        }

        private Guid GetGuid()
        {
            DiagnosticUtility.DebugAssert(_type == ValueHandleType.Guid, "");
            return _bufferReader.GetGuid(_offset);
        }

        private void GetBase64(byte[] buffer, int offset, int count)
        {
            DiagnosticUtility.DebugAssert(_type == ValueHandleType.Base64, "");
            _bufferReader.GetBase64(_offset, buffer, offset, count);
        }

        private XmlDictionaryString GetDictionaryString()
        {
            DiagnosticUtility.DebugAssert(_type == ValueHandleType.Dictionary, "");
            return _bufferReader.GetDictionaryString(_offset);
        }

        private string GetQNameDictionaryText()
        {
            DiagnosticUtility.DebugAssert(_type == ValueHandleType.QName, "");
            return string.Concat(PrefixHandle.GetString(PrefixHandle.GetAlphaPrefix(_length)), ":", _bufferReader.GetDictionaryString(_offset));
        }
    }
}
