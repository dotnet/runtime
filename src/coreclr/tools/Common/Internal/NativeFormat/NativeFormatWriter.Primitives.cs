// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Diagnostics;

namespace Internal.NativeFormat
{
    internal struct NativePrimitiveEncoder
    {
        private byte[] _buffer;
        private int _size;

        public void Init()
        {
            _buffer = new byte[128];
            _size = 0;
        }

        public int Size { get { return _size; } }
        public void Clear() { _size = 0; }
        public void RollbackTo(int offset) { _size = offset; }

        public void WriteByte(byte b)
        {
            if (_buffer.Length == _size)
                Array.Resize(ref _buffer, 2 * _buffer.Length);
            _buffer[_size++] = b;
        }

        public void WriteUInt8(byte value)
        {
            WriteByte(value);
        }

        public void WriteUInt16(ushort value)
        {
            WriteByte((byte)value);
            WriteByte((byte)(value >> 8));
        }

        public void WriteUInt32(uint value)
        {
            WriteByte((byte)value);
            WriteByte((byte)(value >> 8));
            WriteByte((byte)(value >> 16));
            WriteByte((byte)(value >> 24));
        }

        public void WriteUInt64(ulong value)
        {
            WriteUInt32((uint)value);
            WriteUInt32((uint)(value >> 32));
        }

        public unsafe void WriteFloat(float value)
        {
            WriteUInt32(*((uint*)&value));
        }

        public unsafe void WriteDouble(double value)
        {
            WriteUInt64(*((ulong*)&value));
        }

        //
        // Same encoding as what's used by CTL
        //
        public void WriteUnsigned(uint d)
        {
            if (d < 128)
            {
                WriteByte((byte)(d * 2 + 0));
            }
            else if (d < 128 * 128)
            {
                WriteByte((byte)(d * 4 + 1));
                WriteByte((byte)(d >> 6));
            }
            else if (d < 128 * 128 * 128)
            {
                WriteByte((byte)(d * 8 + 3));
                WriteByte((byte)(d >> 5));
                WriteByte((byte)(d >> 13));
            }
            else if (d < 128 * 128 * 128 * 128)
            {
                WriteByte((byte)(d * 16 + 7));
                WriteByte((byte)(d >> 4));
                WriteByte((byte)(d >> 12));
                WriteByte((byte)(d >> 20));
            }
            else
            {
                WriteByte((byte)15);
                WriteUInt32(d);
            }
        }

        public static int GetUnsignedEncodingSize(uint d)
        {
            if (d < 128) return 1;
            if (d < 128 * 128) return 2;
            if (d < 128 * 128 * 128) return 3;
            if (d < 128 * 128 * 128 * 128) return 4;
            return 5;
        }

        public void WriteSigned(int i)
        {
            uint d = (uint)i;
            if (d + 64 < 128)
            {
                WriteByte((byte)(d * 2 + 0));
            }
            else if (d + 64 * 128 < 128 * 128)
            {
                WriteByte((byte)(d * 4 + 1));
                WriteByte((byte)(d >> 6));
            }
            else if (d + 64 * 128 * 128 < 128 * 128 * 128)
            {
                WriteByte((byte)(d * 8 + 3));
                WriteByte((byte)(d >> 5));
                WriteByte((byte)(d >> 13));
            }
            else if (d + 64 * 128 * 128 * 128 < 128 * 128 * 128 * 128)
            {
                WriteByte((byte)(d * 16 + 7));
                WriteByte((byte)(d >> 4));
                WriteByte((byte)(d >> 12));
                WriteByte((byte)(d >> 20));
            }
            else
            {
                WriteByte((byte)15);
                WriteUInt32(d);
            }
        }

        public void WriteUnsignedLong(ulong i)
        {
            if ((uint)i == i)
            {
                WriteUnsigned((uint)i);
                return;
            }

            WriteByte((byte)31);
            WriteUInt64(i);
        }

        public void WriteSignedLong(long i)
        {
            if ((int)i == i)
            {
                WriteSigned((int)i);
                return;
            }

            WriteByte((byte)31);
            WriteUInt64((ulong)i);
        }

        public void PatchByteAt(int offset, byte value)
        {
            Debug.Assert(offset < _size);
            _buffer[offset] = value;
        }

        public void Save(Stream stream)
        {
            stream.Write(_buffer, 0, _size);
        }

        public unsafe bool Save(byte* stream, int streamLength)
        {
            if (streamLength < _size)
            {
                Debug.Assert(false);
                return false;
            }
            for (int i = 0; i < _size; i++)
                stream[i] = _buffer[i];
            return true;
        }

        public byte[] GetBytes()
        {
            byte[] retBuffer = new byte[_size];
            for (int i = 0; i < _size; i++)
                retBuffer[i] = _buffer[i];
            return retBuffer;
        }
    }
}
