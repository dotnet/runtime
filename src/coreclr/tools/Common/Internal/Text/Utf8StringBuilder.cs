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

        public int Length => _length;

        public ReadOnlySpan<byte> AsSpan() => _buffer.AsSpan(0, _length);

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
            return Append(value.AsSpan());
        }

        public Utf8StringBuilder Append(ReadOnlySpan<byte> value)
        {
            Ensure(value.Length);
            value.CopyTo(_buffer.AsSpan(_length));
            _length += value.Length;
            return this;
        }

        public Utf8StringBuilder Append(char value)
        {
            Debug.Assert(Ascii.IsValid(value));

            Ensure(1);
            _buffer[_length++] = (byte)value;
            return this;
        }

        public Utf8StringBuilder Append(string value)
        {
            int length = Encoding.UTF8.GetByteCount(value);
            Ensure(length);

            Encoding.UTF8.GetBytes(value, _buffer.AsSpan(_length));
            _length += length;

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
            return new Utf8String(AsSpan().ToArray());
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
            AsSpan().CopyTo(newBuffer);
            _buffer = newBuffer;
        }
    }
}
