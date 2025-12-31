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

        public bool IsNull => _value == null;
        public static readonly Utf8String Empty = new Utf8String([]);

        public Utf8String(byte[] underlyingArray)
        {
            _value = underlyingArray;
        }

        public Utf8String(ReadOnlySpan<byte> underlyingSpan)
        {
            _value = underlyingSpan.ToArray();
        }

        public Utf8String(string s)
        {
            _value = Encoding.UTF8.GetBytes(s);
        }

        public int Length => _value.Length;

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

        public static Utf8String Concat(params ReadOnlySpan<Utf8String> strings)
        {
            int length = 0;
            foreach (Utf8String s in strings)
                length += s.Length;

            var result = new byte[length];
            Span<byte> resultSpan = result;

            foreach (Utf8String s in strings)
            {
                s.AsSpan().CopyTo(resultSpan);
                resultSpan = resultSpan.Slice(s.Length);
            }

            return new Utf8String(result);
        }

        public static Utf8String Concat(ReadOnlySpan<byte> s1, ReadOnlySpan<byte> s2)
        {
            var result = new byte[s1.Length + s2.Length];
            s1.CopyTo(result);
            s2.CopyTo(result.AsSpan(s1.Length));
            return new Utf8String(result);
        }

        public static Utf8String Concat(ReadOnlySpan<byte> s1, ReadOnlySpan<byte> s2, ReadOnlySpan<byte> s3)
        {
            var result = new byte[s1.Length + s2.Length + s3.Length];
            s1.CopyTo(result);
            s2.CopyTo(result.AsSpan(s1.Length));
            s3.CopyTo(result.AsSpan(s1.Length + s2.Length));
            return new Utf8String(result);
        }

        public static Utf8String Concat(ReadOnlySpan<byte> s1, ReadOnlySpan<byte> s2, ReadOnlySpan<byte> s3, ReadOnlySpan<byte> s4)
        {
            var result = new byte[s1.Length + s2.Length + s3.Length + s4.Length];
            s1.CopyTo(result);
            s2.CopyTo(result.AsSpan(s1.Length));
            s3.CopyTo(result.AsSpan(s1.Length + s2.Length));
            s4.CopyTo(result.AsSpan(s1.Length + s2.Length + s3.Length));
            return new Utf8String(result);
        }

        public static Utf8String Concat(ReadOnlySpan<byte> s1, ReadOnlySpan<byte> s2, ReadOnlySpan<byte> s3, ReadOnlySpan<byte> s4, ReadOnlySpan<byte> s5)
        {
            var result = new byte[s1.Length + s2.Length + s3.Length + s4.Length + s5.Length];
            s1.CopyTo(result);
            s2.CopyTo(result.AsSpan(s1.Length));
            s3.CopyTo(result.AsSpan(s1.Length + s2.Length));
            s4.CopyTo(result.AsSpan(s1.Length + s2.Length + s3.Length));
            s5.CopyTo(result.AsSpan(s1.Length + s2.Length + s3.Length + s4.Length));
            return new Utf8String(result);
        }
    }
}
