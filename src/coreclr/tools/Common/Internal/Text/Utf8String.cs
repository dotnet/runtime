// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Text;

namespace Internal.Text
{
    public readonly struct Utf8String : IEquatable<Utf8String>, IComparable<Utf8String>
    {
        private readonly byte[] _value;

        public Utf8String(byte[] underlyingArray)
        {
            _value = underlyingArray;
        }

        public Utf8String(string s)
        {
            _value = Encoding.UTF8.GetBytes(s);
        }

        public int Length => _value.Length;

        // For now, define implicit conversions between string and Utf8String to aid the transition
        // These conversions will be removed eventually
        public static implicit operator Utf8String(string s)
        {
            return new Utf8String(s);
        }

        public ReadOnlySpan<byte> AsSpan() => _value;

        public override string ToString()
        {
            return Encoding.UTF8.GetString(_value);
        }

        public override bool Equals(object obj)
        {
            return (obj is Utf8String utf8String) && Equals(utf8String);
        }

        public override unsafe int GetHashCode()
        {
            int length = _value.Length;
            uint hash = (uint)length;
            fixed (byte* ap = _value)
            {
                byte* a = ap;

                while (length >= 4)
                {
                    hash = (hash + BitOperations.RotateLeft(hash, 5)) ^ *(uint*)a;
                    a += 4; length -= 4;
                }
                if (length >= 2)
                {
                    hash = (hash + BitOperations.RotateLeft(hash, 5)) ^ *(ushort*)a;
                    a += 2; length -= 2;
                }
                if (length > 0)
                {
                    hash = (hash + BitOperations.RotateLeft(hash, 5)) ^ *a;
                }
                hash += BitOperations.RotateLeft(hash, 7);
                hash += BitOperations.RotateLeft(hash, 15);
                return (int)hash;
            }
        }

        public bool Equals(Utf8String other)
        {
            return AsSpan().SequenceEqual(other.AsSpan());
        }

        private static int Compare(Utf8String strA, Utf8String strB)
        {
            return strA.AsSpan().SequenceCompareTo(strB.AsSpan());
        }

        public int CompareTo(Utf8String other)
        {
            return Compare(this, other);
        }
    }
}
