// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml
{
    internal enum StringHandleConstStringType
    {
        Type = 0,
        Root = 1,
        Item = 2
    }

    internal sealed class StringHandle : IEquatable<StringHandle>
    {
        private readonly XmlBufferReader _bufferReader;
        private StringHandleType _type;
        private int _key;
        private int _offset;
        private int _length;
        private static readonly string[] s_constStrings = {
                                            "type",
                                            "root",
                                            "item"
                                       };

        public StringHandle(XmlBufferReader bufferReader)
        {
            _bufferReader = bufferReader;
            SetValue(0, 0);
        }

        public void SetValue(int offset, int length)
        {
            _type = StringHandleType.UTF8;
            _offset = offset;
            _length = length;
        }

        public void SetConstantValue(StringHandleConstStringType constStringType)
        {
            _type = StringHandleType.ConstString;
            _key = (int)constStringType;
        }

        public void SetValue(int offset, int length, bool escaped)
        {
            _type = (escaped ? StringHandleType.EscapedUTF8 : StringHandleType.UTF8);
            _offset = offset;
            _length = length;
        }

        public void SetValue(int key)
        {
            _type = StringHandleType.Dictionary;
            _key = key;
        }

        public void SetValue(StringHandle value)
        {
            _type = value._type;
            _key = value._key;
            _offset = value._offset;
            _length = value._length;
        }

        public bool IsEmpty
        {
            get
            {
                if (_type == StringHandleType.UTF8)
                    return _length == 0;
                return Equals2(string.Empty);
            }
        }

        public bool IsXmlns
        {
            get
            {
                if (_type == StringHandleType.UTF8)
                {
                    if (_length != 5)
                        return false;
                    byte[] buffer = _bufferReader.Buffer;
                    int offset = _offset;
                    return buffer[offset + 0] == 'x' &&
                           buffer[offset + 1] == 'm' &&
                           buffer[offset + 2] == 'l' &&
                           buffer[offset + 3] == 'n' &&
                           buffer[offset + 4] == 's';
                }
                return Equals2("xmlns");
            }
        }

        public void ToPrefixHandle(PrefixHandle prefix)
        {
            Debug.Assert(_type == StringHandleType.UTF8);
            prefix.SetValue(_offset, _length);
        }

        public string GetString(XmlNameTable nameTable)
        {
            StringHandleType type = _type;
            if (type == StringHandleType.UTF8)
                return _bufferReader.GetString(_offset, _length, nameTable);
            if (type == StringHandleType.Dictionary)
                return nameTable.Add(_bufferReader.GetDictionaryString(_key).Value);
            if (type == StringHandleType.ConstString)
                return nameTable.Add(s_constStrings[_key]);
            // If none of the above, must be StringHandleType.EscapedUTF8
            Debug.Assert(type == StringHandleType.EscapedUTF8, "Should be EscapedUTF8");
            return _bufferReader.GetEscapedString(_offset, _length, nameTable);
        }

        public string GetString()
        {
            StringHandleType type = _type;
            if (type == StringHandleType.UTF8)
                return _bufferReader.GetString(_offset, _length);
            if (type == StringHandleType.Dictionary)
                return _bufferReader.GetDictionaryString(_key).Value;
            if (type == StringHandleType.ConstString)
                return s_constStrings[_key];
            // If none of the above, must be StringHandleType.EscapedUTF8
            Debug.Assert(type == StringHandleType.EscapedUTF8, "Should be EscapedUTF8");
            return _bufferReader.GetEscapedString(_offset, _length);
        }

        public byte[] GetString(out int offset, out int length)
        {
            StringHandleType type = _type;
            if (type == StringHandleType.UTF8)
            {
                offset = _offset;
                length = _length;
                return _bufferReader.Buffer;
            }
            if (type == StringHandleType.Dictionary)
            {
                byte[] buffer = _bufferReader.GetDictionaryString(_key).ToUTF8();
                offset = 0;
                length = buffer.Length;
                return buffer;
            }
            if (type == StringHandleType.ConstString)
            {
                byte[] buffer = XmlConverter.ToBytes(s_constStrings[_key]);
                offset = 0;
                length = buffer.Length;
                return buffer;
            }
            else
            {
                Debug.Assert(type == StringHandleType.EscapedUTF8);
                byte[] buffer = XmlConverter.ToBytes(_bufferReader.GetEscapedString(_offset, _length));
                offset = 0;
                length = buffer.Length;
                return buffer;
            }
        }

        public bool TryGetDictionaryString([NotNullWhen(true)] out XmlDictionaryString? value)
        {
            if (_type == StringHandleType.Dictionary)
            {
                value = _bufferReader.GetDictionaryString(_key);
                return true;
            }
            else if (IsEmpty)
            {
                value = XmlDictionaryString.Empty;
                return true;
            }

            value = null;
            return false;
        }

        public override string ToString()
        {
            return GetString();
        }

        private bool Equals2(int key2, XmlBufferReader bufferReader2)
        {
            StringHandleType type = _type;
            if (type == StringHandleType.Dictionary)
                return _bufferReader.Equals2(_key, key2, bufferReader2);
            if (type == StringHandleType.UTF8)
                return _bufferReader.Equals2(_offset, _length, bufferReader2.GetDictionaryString(key2).Value);
            Debug.Assert(type == StringHandleType.EscapedUTF8 || type == StringHandleType.ConstString);
            return GetString() == _bufferReader.GetDictionaryString(key2).Value;
        }

        private bool Equals2(XmlDictionaryString xmlString2)
        {
            StringHandleType type = _type;
            if (type == StringHandleType.Dictionary)
                return _bufferReader.Equals2(_key, xmlString2);
            if (type == StringHandleType.UTF8)
                return _bufferReader.Equals2(_offset, _length, xmlString2.ToUTF8());
            Debug.Assert(type == StringHandleType.EscapedUTF8 || type == StringHandleType.ConstString);
            return GetString() == xmlString2.Value;
        }

        private bool Equals2(string s2)
        {
            StringHandleType type = _type;
            if (type == StringHandleType.Dictionary)
                return _bufferReader.GetDictionaryString(_key).Value == s2;
            if (type == StringHandleType.UTF8)
                return _bufferReader.Equals2(_offset, _length, s2);
            Debug.Assert(type == StringHandleType.EscapedUTF8 || type == StringHandleType.ConstString);
            return GetString() == s2;
        }

        private bool Equals2(int offset2, int length2, XmlBufferReader bufferReader2)
        {
            StringHandleType type = _type;
            if (type == StringHandleType.Dictionary)
                return bufferReader2.Equals2(offset2, length2, _bufferReader.GetDictionaryString(_key).Value);
            if (type == StringHandleType.UTF8)
                return _bufferReader.Equals2(_offset, _length, bufferReader2, offset2, length2);
            Debug.Assert(type == StringHandleType.EscapedUTF8 || type == StringHandleType.ConstString);
            return GetString() == _bufferReader.GetString(offset2, length2);
        }

        public bool Equals([NotNullWhen(true)] StringHandle? other)
        {
            if (other is null)
                return false;
            StringHandleType type = other._type;
            if (type == StringHandleType.Dictionary)
                return Equals2(other._key, other._bufferReader);
            if (type == StringHandleType.UTF8)
                return Equals2(other._offset, other._length, other._bufferReader);
            Debug.Assert(type == StringHandleType.EscapedUTF8 || type == StringHandleType.ConstString);
            return Equals2(other.GetString());
        }

        public static bool operator ==(StringHandle s1, XmlDictionaryString xmlString2)
        {
            return s1.Equals2(xmlString2);
        }

        public static bool operator !=(StringHandle s1, XmlDictionaryString xmlString2)
        {
            return !s1.Equals2(xmlString2);
        }

        public static bool operator ==(StringHandle s1, string s2)
        {
            return s1.Equals2(s2);
        }

        public static bool operator !=(StringHandle s1, string s2)
        {
            return !s1.Equals2(s2);
        }

        public static bool operator ==(StringHandle s1, StringHandle s2)
        {
            return s1.Equals(s2);
        }

        public static bool operator !=(StringHandle s1, StringHandle s2)
        {
            return !s1.Equals(s2);
        }

        public int CompareTo(StringHandle that)
        {
            if (_type == StringHandleType.UTF8 && that._type == StringHandleType.UTF8)
                return _bufferReader.Compare(_offset, _length, that._offset, that._length);
            else
                return string.Compare(this.GetString(), that.GetString(), StringComparison.Ordinal);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return Equals(obj as StringHandle);
        }

        public override int GetHashCode()
        {
            return GetString().GetHashCode();
        }

        private enum StringHandleType
        {
            Dictionary,
            UTF8,
            EscapedUTF8,
            ConstString
        }
    }
}
