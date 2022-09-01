// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Diagnostics;

namespace Internal.Text
{
    public class Utf8StringBuilder
    {
        private byte[] _buffer = Array.Empty<byte>();
        private int _length;

        public Utf8StringBuilder()
        {
        }

        // TODO: This should return ReadOnlySpan<byte> instead once available
        public byte[] UnderlyingArray => _buffer;
        public int Length => _length;

        public Utf8StringBuilder Clear()
        {
            _length = 0;
            return this;
        }

        public Utf8StringBuilder Truncate(int newLength)
        {
            Debug.Assert(newLength <= _length);
            _length = newLength;
            return this;
        }

        public Utf8StringBuilder Append(Utf8String value)
        {
            return Append(value.UnderlyingArray);
        }

        public Utf8StringBuilder Append(byte[] value)
        {
            Ensure(value.Length);
            Buffer.BlockCopy(value, 0, _buffer, _length, value.Length);
            _length += value.Length;
            return this;
        }

        public Utf8StringBuilder Append(char value)
        {
            Ensure(1);
            if (value > 0x7F)
                return Append(Encoding.UTF8.GetBytes(new char[] { value }));
            _buffer[_length++] = (byte)value;
            return this;
        }

        public Utf8StringBuilder Append(string value)
        {
            Ensure(value.Length);

            byte[] buffer = _buffer;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c > 0x7F)
                    return Append(Encoding.UTF8.GetBytes(value));
                buffer[_length+i] = (byte)c;
            }
            _length += value.Length;

            return this;
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(_buffer, 0, _length);
        }

        public string ToString(int start)
        {
            return Encoding.UTF8.GetString(_buffer, start, _length - start);
        }

        public Utf8String ToUtf8String()
        {
            var ret = new byte[_length];
            Buffer.BlockCopy(_buffer, 0, ret, 0, _length);
            return new Utf8String(ret);
        }

        private void Ensure(int extraSpace)
        {
            if ((uint)(_length + extraSpace) > (uint)_buffer.Length)
                Grow(extraSpace);
        }

        private void Grow(int extraSpace)
        {
            int newSize = Math.Max(2 * _buffer.Length, _length + extraSpace);
            byte[] newBuffer = new byte[newSize];
            Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _length);
            _buffer = newBuffer;
        }

        // Find the boundary of the last character prior to a position
        // If pos points to the last byte of a char, then return pos; Otherwise,
        // return the position of the last byte of the preceding char.
        public int LastCharBoundary(int pos)
        {
            Debug.Assert(pos < _length);

            if (_buffer[pos] < 128 /*10000000*/)
            {
                // This is a single byte character
                return pos;
            }

            int origPos = pos;

            // Skip following bytes of a multi-byte character until the first byte is seen
            while (_buffer[pos] < 192 /*11000000*/)
            {
                pos--;
            }

            if (pos == origPos - 3)
            {
                // We just skipped a four-byte character
                Debug.Assert(_buffer[pos] >= 240 /*11110000*/);
                return origPos;
            }

            if (pos == origPos - 2 && _buffer[pos] < 240 && _buffer[pos] >= 224 /*11100000*/)
            {
                // We just skipped a three-byte character
                return origPos;
            }

            if (pos == origPos - 1 && _buffer[pos] < 224)
            {
                // We just skipped a two-byte character
                Debug.Assert(_buffer[pos] >= 192 /*11000000*/);
                return origPos;
            }

            // We were in the middle of a multi-byte character
            return pos - 1;
        }
    }
}
