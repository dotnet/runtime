// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

namespace Internal.Text
{
    public readonly ref struct Utf8Span
    {
        private readonly ReadOnlySpan<byte> _value;

        public Utf8Span(ReadOnlySpan<byte> value) => _value = value;

        public int Length => _value.Length;

        public bool IsEmpty => _value.IsEmpty;

        public ReadOnlySpan<byte> AsSpan() => _value;

        public byte[] ToArray() => _value.ToArray();

        public bool StartsWith(Utf8Span value) => _value.StartsWith(value.AsSpan());

        public bool EndsWith(Utf8Span value) => _value.EndsWith(value.AsSpan());

        // This is deliberately not a == operator because we don't want to make it easy
        // to accidentally do UTF-8 vs UTF-16 string comparisons.
        public bool StringEquals(string value)
        {
            if (_value.Length < value.Length)
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                int ch = _value[i];
                if (ch > 0x7F)
                    return Encoding.UTF8.GetString(_value) == value;

                // We are assuming here that valid UTF8 encoded byte > 0x7F cannot map to a character with code point <= 0x7F
                if (ch != value[i])
                    return false;
            }

            return _value.Length == value.Length; // All char ANSI, all matching
        }

        public override bool Equals(object obj) => false;

        public override int GetHashCode()
        {
            HashCode h = default;
            h.AddBytes(_value);
            return h.ToHashCode();
        }

        public override string ToString() => Encoding.UTF8.GetString(_value);

        public static implicit operator Utf8Span(ReadOnlySpan<byte> s) => new Utf8Span(s);

        public static bool operator ==(Utf8Span left, Utf8Span right)
            => left._value.SequenceEqual(right._value);

        public static bool operator !=(Utf8Span left, Utf8Span right)
            => !left._value.SequenceEqual(right._value);
    }
}
