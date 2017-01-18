// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using System.Diagnostics;


namespace System.Xml
{
    internal enum PrefixHandleType
    {
        Empty,
        A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Buffer,
        Max,
    }

    internal class PrefixHandle : IEquatable<PrefixHandle>
    {
        private XmlBufferReader _bufferReader;
        private PrefixHandleType _type;
        private int _offset;
        private int _length;
        private static string[] s_prefixStrings = { "", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };
        private static byte[] s_prefixBuffer = { (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f', (byte)'g', (byte)'h', (byte)'i', (byte)'j', (byte)'k', (byte)'l', (byte)'m', (byte)'n', (byte)'o', (byte)'p', (byte)'q', (byte)'r', (byte)'s', (byte)'t', (byte)'u', (byte)'v', (byte)'w', (byte)'x', (byte)'y', (byte)'z' };

        public PrefixHandle(XmlBufferReader bufferReader)
        {
            _bufferReader = bufferReader;
        }

        public void SetValue(PrefixHandleType type)
        {
            DiagnosticUtility.DebugAssert(type != PrefixHandleType.Buffer, "");
            _type = type;
        }

        public void SetValue(PrefixHandle prefix)
        {
            _type = prefix._type;
            _offset = prefix._offset;
            _length = prefix._length;
        }

        public void SetValue(int offset, int length)
        {
            if (length == 0)
            {
                SetValue(PrefixHandleType.Empty);
                return;
            }

            if (length == 1)
            {
                byte ch = _bufferReader.GetByte(offset);
                if (ch >= 'a' && ch <= 'z')
                {
                    SetValue(GetAlphaPrefix(ch - 'a'));
                    return;
                }
            }

            _type = PrefixHandleType.Buffer;
            _offset = offset;
            _length = length;
        }

        public bool IsEmpty
        {
            get
            {
                return _type == PrefixHandleType.Empty;
            }
        }

        public bool IsXmlns
        {
            get
            {
                if (_type != PrefixHandleType.Buffer)
                    return false;
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
        }

        public bool IsXml
        {
            get
            {
                if (_type != PrefixHandleType.Buffer)
                    return false;
                if (_length != 3)
                    return false;
                byte[] buffer = _bufferReader.Buffer;
                int offset = _offset;
                return buffer[offset + 0] == 'x' &&
                       buffer[offset + 1] == 'm' &&
                       buffer[offset + 2] == 'l';
            }
        }

        public bool TryGetShortPrefix(out PrefixHandleType type)
        {
            type = _type;
            return (type != PrefixHandleType.Buffer);
        }

        public static string GetString(PrefixHandleType type)
        {
            DiagnosticUtility.DebugAssert(type != PrefixHandleType.Buffer, "");
            return s_prefixStrings[(int)type];
        }

        public static PrefixHandleType GetAlphaPrefix(int index)
        {
            DiagnosticUtility.DebugAssert(index >= 0 && index < 26, "");
            return (PrefixHandleType)(PrefixHandleType.A + index);
        }

        public static byte[] GetString(PrefixHandleType type, out int offset, out int length)
        {
            DiagnosticUtility.DebugAssert(type != PrefixHandleType.Buffer, "");
            if (type == PrefixHandleType.Empty)
            {
                offset = 0;
                length = 0;
            }
            else
            {
                length = 1;
                offset = (int)(type - PrefixHandleType.A);
            }
            return s_prefixBuffer;
        }

        public string GetString(XmlNameTable nameTable)
        {
            PrefixHandleType type = _type;
            if (type != PrefixHandleType.Buffer)
                return GetString(type);
            else
                return _bufferReader.GetString(_offset, _length, nameTable);
        }

        public string GetString()
        {
            PrefixHandleType type = _type;
            if (type != PrefixHandleType.Buffer)
                return GetString(type);
            else
                return _bufferReader.GetString(_offset, _length);
        }

        public byte[] GetString(out int offset, out int length)
        {
            PrefixHandleType type = _type;
            if (type != PrefixHandleType.Buffer)
                return GetString(type, out offset, out length);
            else
            {
                offset = _offset;
                length = _length;
                return _bufferReader.Buffer;
            }
        }
        public int CompareTo(PrefixHandle that)
        {
            return GetString().CompareTo(that.GetString());
        }

        public bool Equals(PrefixHandle prefix2)
        {
            if (ReferenceEquals(prefix2, null))
                return false;
            PrefixHandleType type1 = _type;
            PrefixHandleType type2 = prefix2._type;
            if (type1 != type2)
                return false;
            if (type1 != PrefixHandleType.Buffer)
                return true;
            if (_bufferReader == prefix2._bufferReader)
                return _bufferReader.Equals2(_offset, _length, prefix2._offset, prefix2._length);
            else
                return _bufferReader.Equals2(_offset, _length, prefix2._bufferReader, prefix2._offset, prefix2._length);
        }

        private bool Equals2(string prefix2)
        {
            PrefixHandleType type = _type;
            if (type != PrefixHandleType.Buffer)
                return GetString(type) == prefix2;
            return _bufferReader.Equals2(_offset, _length, prefix2);
        }

        private bool Equals2(XmlDictionaryString prefix2)
        {
            return Equals2(prefix2.Value);
        }
        public static bool operator ==(PrefixHandle prefix1, string prefix2)
        {
            return prefix1.Equals2(prefix2);
        }

        public static bool operator !=(PrefixHandle prefix1, string prefix2)
        {
            return !prefix1.Equals2(prefix2);
        }

        public static bool operator ==(PrefixHandle prefix1, XmlDictionaryString prefix2)
        {
            return prefix1.Equals2(prefix2);
        }

        public static bool operator !=(PrefixHandle prefix1, XmlDictionaryString prefix2)
        {
            return !prefix1.Equals2(prefix2);
        }

        public static bool operator ==(PrefixHandle prefix1, PrefixHandle prefix2)
        {
            return prefix1.Equals(prefix2);
        }

        public static bool operator !=(PrefixHandle prefix1, PrefixHandle prefix2)
        {
            return !prefix1.Equals(prefix2);
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as PrefixHandle);
        }

        public override string ToString()
        {
            return GetString();
        }

        public override int GetHashCode()
        {
            return GetString().GetHashCode();
        }
    }
}
