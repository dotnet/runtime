// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace System.Numerics
{
    public readonly struct Decimal64
        : IComparable,
          IEquatable<Decimal64>
    {
        public Decimal64(long significand, int exponent)
        {

        }

        public static Decimal64 Parse(string s) => Parse(s, NumberStyles.Number, provider: null);

        public static Decimal64 Parse(string s, NumberStyles style) => Parse(s, style, provider: null);

        public static Decimal64 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Number, provider);

        public static Decimal64 Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Number, provider);

        public static Decimal64 Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Number, IFormatProvider? provider = null)
        {
            throw new NotImplementedException();
        }

        public static Decimal64 Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            return Parse(s.AsSpan(), style, provider);
        }
        public int CompareTo(object? value) => throw new NotImplementedException();
        public bool Equals(Decimal64 other) => throw new NotImplementedException();

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Decimal64 && Equals((Decimal64)obj);
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}
