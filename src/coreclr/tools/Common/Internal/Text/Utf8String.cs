// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Internal.Text
{
    public struct Utf8String : IEquatable<Utf8String>, IComparable<Utf8String>
    {
        private byte[] _value;

        public Utf8String(byte[] underlyingArray)
        {
            _value = underlyingArray;
        }

        public Utf8String(string s)
        {
            _value = Encoding.UTF8.GetBytes(s);
        }

        // TODO: This should return ReadOnlySpan<byte> instead once available
        public byte[] UnderlyingArray => _value;
        public int Length => _value.Length;

        // For now, define implicit conversions between string and Utf8String to aid the transition
        // These conversions will be removed eventually
        public static implicit operator Utf8String(string s)
        {
            return new Utf8String(s);
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(_value);
        }

        public override bool Equals(object obj)
        {
            return (obj is Utf8String) && Equals((Utf8String)obj);
        }

        public unsafe override int GetHashCode()
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
            int length = _value.Length;
            if (length != other.Length)
                return false;

            if (_value == other._value)
                return true;

            unsafe
            {
                fixed (byte* ap = _value) fixed (byte* bp = other._value)
                {
                    byte* a = ap;
                    byte* b = bp;

                    while (length >= 4)
                    {
                        if (*(int*)a != *(int*)b) return false;
                        a += 4; b += 4; length -= 4;
                    }
                    if (length >= 2)
                    {
                        if (*(short*)a != *(short*)b) return false;
                        a += 2; b += 2; length -= 2;
                    }
                    if (length > 0)
                    {
                        if (*a != *b) return false;
                    }
                    return true;
                }
            }
        }

        private static int Compare(Utf8String strA, Utf8String strB)
        {
            int length = Math.Min(strA.Length, strB.Length);

            unsafe
            {
                fixed (byte* ap = strA._value)
                fixed (byte* bp = strB._value)
                {
                    byte* a = ap;
                    byte* b = bp;

                    while (length > 0)
                    {
                        if (*a != *b)
                            return *a - *b;
                        a += 1;
                        b += 1;
                        length -= 1;
                    }

                    // At this point, we have compared all the characters in at least one string.
                    // The longer string will be larger.
                    // We could optimize and compare lengths before iterating strings, but we want
                    // Foo and Foo1 to be sorted adjacent to eachother.
                    return strA.Length - strB.Length;
                }
            }
        }

        public int CompareTo(Utf8String other)
        {
            return Compare(this, other);
        }
    }
}
