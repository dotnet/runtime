// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Xunit;

namespace System.Numerics.Tests
{
    public class DimTests_GenericMath
    {
        //
        // IBinaryNumber
        //

        [Fact]
        public static void AllBitsSetInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0xFFFF_FFFF), BinaryNumberHelper<BinaryIntegerWrapper<int>>.AllBitsSet);
            Assert.Equal((BinaryIntegerWrapper<int>)0, ~BinaryNumberHelper<BinaryIntegerWrapper<int>>.AllBitsSet);
        }

        [Fact]
        public static void AllBitsSetUInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<uint>)0xFFFF_FFFF, BinaryNumberHelper<BinaryIntegerWrapper<uint>>.AllBitsSet);
            Assert.Equal((BinaryIntegerWrapper<uint>)0U, ~BinaryNumberHelper<BinaryIntegerWrapper<uint>>.AllBitsSet);
        }

        //
        // IBinaryInteger
        //

        [Fact]
        public static void DivRemInt32Test()
        {
            Assert.Equal(((BinaryIntegerWrapper<int>)0x00000000, (BinaryIntegerWrapper<int>)0x00000000), BinaryIntegerHelper<BinaryIntegerWrapper<int>>.DivRem((int)0x00000000, 2));
            Assert.Equal(((BinaryIntegerWrapper<int>)0x00000000, (BinaryIntegerWrapper<int>)0x00000001), BinaryIntegerHelper<BinaryIntegerWrapper<int>>.DivRem((int)0x00000001, 2));
            Assert.Equal(((BinaryIntegerWrapper<int>)0x3FFFFFFF, (BinaryIntegerWrapper<int>)0x00000001), BinaryIntegerHelper<BinaryIntegerWrapper<int>>.DivRem((int)0x7FFFFFFF, 2));
            Assert.Equal(((BinaryIntegerWrapper<int>)unchecked((int)0xC0000000), (BinaryIntegerWrapper<int>)0x00000000), BinaryIntegerHelper<BinaryIntegerWrapper<int>>.DivRem(unchecked((int)0x80000000), 2));
            Assert.Equal(((BinaryIntegerWrapper<int>)0x00000000, (BinaryIntegerWrapper<int>)unchecked((int)0xFFFFFFFF)), BinaryIntegerHelper<BinaryIntegerWrapper<int>>.DivRem(unchecked((int)0xFFFFFFFF), 2));
        }

        [Fact]
        public static void DivRemUInt32Test()
        {
            Assert.Equal(((BinaryIntegerWrapper<uint>)0x00000000, (BinaryIntegerWrapper<uint>)0x00000000), BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.DivRem((uint)0x00000000, 2));
            Assert.Equal(((BinaryIntegerWrapper<uint>)0x00000000, (BinaryIntegerWrapper<uint>)0x00000001), BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.DivRem((uint)0x00000001, 2));
            Assert.Equal(((BinaryIntegerWrapper<uint>)0x3FFFFFFF, (BinaryIntegerWrapper<uint>)0x00000001), BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.DivRem((uint)0x7FFFFFFF, 2));
            Assert.Equal(((BinaryIntegerWrapper<uint>)0x40000000, (BinaryIntegerWrapper<uint>)0x00000000), BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.DivRem((uint)0x80000000, 2));
            Assert.Equal(((BinaryIntegerWrapper<uint>)0x7FFFFFFF, (BinaryIntegerWrapper<uint>)0x00000001), BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.DivRem((uint)0xFFFFFFFF, 2));
        }

        [Fact]
        public static void LeadingZeroCountInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000020, BinaryIntegerHelper<BinaryIntegerWrapper<int>>.LeadingZeroCount((int)0x00000000));
            Assert.Equal((BinaryIntegerWrapper<int>)0x0000001F, BinaryIntegerHelper<BinaryIntegerWrapper<int>>.LeadingZeroCount((int)0x00000001));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, BinaryIntegerHelper<BinaryIntegerWrapper<int>>.LeadingZeroCount((int)0x7FFFFFFF));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000000, BinaryIntegerHelper<BinaryIntegerWrapper<int>>.LeadingZeroCount(unchecked((int)0x80000000)));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000000, BinaryIntegerHelper<BinaryIntegerWrapper<int>>.LeadingZeroCount(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void LeadingZeroCountUInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000020, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.LeadingZeroCount((uint)0x00000000));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x0000001F, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.LeadingZeroCount((uint)0x00000001));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.LeadingZeroCount((uint)0x7FFFFFFF));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000000, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.LeadingZeroCount((uint)0x80000000));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000000, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.LeadingZeroCount((uint)0xFFFFFFFF));
        }

        [Fact]
        public static void RotateLeftInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000000, BinaryIntegerHelper<BinaryIntegerWrapper<int>>.RotateLeft((int)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000002, BinaryIntegerHelper<BinaryIntegerWrapper<int>>.RotateLeft((int)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0xFFFFFFFE), BinaryIntegerHelper<BinaryIntegerWrapper<int>>.RotateLeft((int)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, BinaryIntegerHelper<BinaryIntegerWrapper<int>>.RotateLeft(unchecked((int)0x80000000), 1));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0xFFFFFFFF), BinaryIntegerHelper<BinaryIntegerWrapper<int>>.RotateLeft(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void RotateLeftUInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000000, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.RotateLeft((uint)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000002, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.RotateLeft((uint)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0xFFFFFFFE, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.RotateLeft((uint)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.RotateLeft((uint)0x80000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0xFFFFFFFF, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.RotateLeft((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void RotateRightInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000000, BinaryIntegerHelper<BinaryIntegerWrapper<int>>.RotateRight((int)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0x80000000), BinaryIntegerHelper<BinaryIntegerWrapper<int>>.RotateRight((int)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0xBFFFFFFF), BinaryIntegerHelper<BinaryIntegerWrapper<int>>.RotateRight((int)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x40000000, BinaryIntegerHelper<BinaryIntegerWrapper<int>>.RotateRight(unchecked((int)0x80000000), 1));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0xFFFFFFFF), BinaryIntegerHelper<BinaryIntegerWrapper<int>>.RotateRight(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void RotateRightUInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000000, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.RotateRight((uint)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x80000000, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.RotateRight((uint)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0xBFFFFFFF, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.RotateRight((uint)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x40000000, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.RotateRight((uint)0x80000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0xFFFFFFFF, BinaryIntegerHelper<BinaryIntegerWrapper<uint>>.RotateRight((uint)0xFFFFFFFF, 1));
        }

        //
        // INumber
        //

        [Fact]
        public static void ClampInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000000, NumberHelper<BinaryIntegerWrapper<int>>.Clamp((int)0x00000000, unchecked((int)0xFFFFFFC0), 0x003F));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.Clamp((int)0x00000001, unchecked((int)0xFFFFFFC0), 0x003F));
            Assert.Equal((BinaryIntegerWrapper<int>)0x0000003F, NumberHelper<BinaryIntegerWrapper<int>>.Clamp((int)0x7FFFFFFF, unchecked((int)0xFFFFFFC0), 0x003F));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0xFFFFFFC0), NumberHelper<BinaryIntegerWrapper<int>>.Clamp(unchecked((int)0x80000000), unchecked((int)0xFFFFFFC0), 0x003F));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0xFFFFFFFF), NumberHelper<BinaryIntegerWrapper<int>>.Clamp(unchecked((int)0xFFFFFFFF), unchecked((int)0xFFFFFFC0), 0x003F));
        }

        [Fact]
        public static void ClampUInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.Clamp((uint)0x00000000, 0x0001, 0x003F));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.Clamp((uint)0x00000001, 0x0001, 0x003F));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x0000003F, NumberHelper<BinaryIntegerWrapper<uint>>.Clamp((uint)0x7FFFFFFF, 0x0001, 0x003F));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x0000003F, NumberHelper<BinaryIntegerWrapper<uint>>.Clamp((uint)0x80000000, 0x0001, 0x003F));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x0000003F, NumberHelper<BinaryIntegerWrapper<uint>>.Clamp((uint)0xFFFFFFFF, 0x0001, 0x003F));
        }

        [Fact]
        public static void MaxInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.Max((int)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.Max((int)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x7FFFFFFF, NumberHelper<BinaryIntegerWrapper<int>>.Max((int)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.Max(unchecked((int)0x80000000), 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.Max(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void MaxUInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.Max((uint)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.Max((uint)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x7FFFFFFF, NumberHelper<BinaryIntegerWrapper<uint>>.Max((uint)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x80000000, NumberHelper<BinaryIntegerWrapper<uint>>.Max((uint)0x80000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0xFFFFFFFF, NumberHelper<BinaryIntegerWrapper<uint>>.Max((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void MaxNumberInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.MaxNumber((int)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.MaxNumber((int)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x7FFFFFFF, NumberHelper<BinaryIntegerWrapper<int>>.MaxNumber((int)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.MaxNumber(unchecked((int)0x80000000), 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.MaxNumber(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void MaxNumberUInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.MaxNumber((uint)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.MaxNumber((uint)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x7FFFFFFF, NumberHelper<BinaryIntegerWrapper<uint>>.MaxNumber((uint)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x80000000, NumberHelper<BinaryIntegerWrapper<uint>>.MaxNumber((uint)0x80000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0xFFFFFFFF, NumberHelper<BinaryIntegerWrapper<uint>>.MaxNumber((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void MinInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000000, NumberHelper<BinaryIntegerWrapper<int>>.Min((int)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.Min((int)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.Min((int)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0x80000000), NumberHelper<BinaryIntegerWrapper<int>>.Min(unchecked((int)0x80000000), 1));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0xFFFFFFFF), NumberHelper<BinaryIntegerWrapper<int>>.Min(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void MinUInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000000, NumberHelper<BinaryIntegerWrapper<uint>>.Min((uint)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.Min((uint)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.Min((uint)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.Min((uint)0x80000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.Min((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void MinNumberInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000000, NumberHelper<BinaryIntegerWrapper<int>>.MinNumber((int)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.MinNumber((int)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)0x00000001, NumberHelper<BinaryIntegerWrapper<int>>.MinNumber((int)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0x80000000), NumberHelper<BinaryIntegerWrapper<int>>.MinNumber(unchecked((int)0x80000000), 1));
            Assert.Equal((BinaryIntegerWrapper<int>)unchecked((int)0xFFFFFFFF), NumberHelper<BinaryIntegerWrapper<int>>.MinNumber(unchecked((int)0xFFFFFFFF), 1));
        }

        [Fact]
        public static void MinNumberUInt32Test()
        {
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000000, NumberHelper<BinaryIntegerWrapper<uint>>.MinNumber((uint)0x00000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.MinNumber((uint)0x00000001, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.MinNumber((uint)0x7FFFFFFF, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.MinNumber((uint)0x80000000, 1));
            Assert.Equal((BinaryIntegerWrapper<uint>)0x00000001, NumberHelper<BinaryIntegerWrapper<uint>>.MinNumber((uint)0xFFFFFFFF, 1));
        }

        [Fact]
        public static void SignInt32Test()
        {
            Assert.Equal(0, NumberHelper<BinaryIntegerWrapper<int>>.Sign((int)0x00000000));
            Assert.Equal(1, NumberHelper<BinaryIntegerWrapper<int>>.Sign((int)0x00000001));
            Assert.Equal(1, NumberHelper<BinaryIntegerWrapper<int>>.Sign((int)0x7FFFFFFF));
            Assert.Equal(-1, NumberHelper<BinaryIntegerWrapper<int>>.Sign(unchecked((int)0x80000000)));
            Assert.Equal(-1, NumberHelper<BinaryIntegerWrapper<int>>.Sign(unchecked((int)0xFFFFFFFF)));
        }

        [Fact]
        public static void SignUInt32Test()
        {
            Assert.Equal(0, NumberHelper<BinaryIntegerWrapper<uint>>.Sign((uint)0x00000000));
            Assert.Equal(1, NumberHelper<BinaryIntegerWrapper<uint>>.Sign((uint)0x00000001));
            Assert.Equal(1, NumberHelper<BinaryIntegerWrapper<uint>>.Sign((uint)0x7FFFFFFF));
            Assert.Equal(1, NumberHelper<BinaryIntegerWrapper<uint>>.Sign((uint)0x80000000));
            Assert.Equal(1, NumberHelper<BinaryIntegerWrapper<uint>>.Sign((uint)0xFFFFFFFF));
        }

        public struct BinaryIntegerWrapper<T> : IBinaryInteger<BinaryIntegerWrapper<T>>
            where T : IBinaryInteger<T>
        {
            public T Value;

            public BinaryIntegerWrapper(T value)
            {
                Value = value;
            }

            public static implicit operator BinaryIntegerWrapper<T>(T value) => new BinaryIntegerWrapper<T>(value);

            public static implicit operator T(BinaryIntegerWrapper<T> value) => value.Value;

            // Required Generic Math Surface Area

            public static BinaryIntegerWrapper<T> One => T.One;

            public static int Radix => T.Radix;

            public static BinaryIntegerWrapper<T> Zero => T.Zero;

            public static BinaryIntegerWrapper<T> AdditiveIdentity => T.AdditiveIdentity;

            public static BinaryIntegerWrapper<T> MultiplicativeIdentity => T.MultiplicativeIdentity;

            public static BinaryIntegerWrapper<T> Abs(BinaryIntegerWrapper<T> value) => T.Abs(value);
            public static bool IsCanonical(BinaryIntegerWrapper<T> value) => T.IsCanonical(value);
            public static bool IsComplexNumber(BinaryIntegerWrapper<T> value) => T.IsComplexNumber(value);
            public static bool IsEvenInteger(BinaryIntegerWrapper<T> value) => T.IsEvenInteger(value);
            public static bool IsFinite(BinaryIntegerWrapper<T> value) => T.IsFinite(value);
            public static bool IsImaginaryNumber(BinaryIntegerWrapper<T> value) => T.IsImaginaryNumber(value);
            public static bool IsInfinity(BinaryIntegerWrapper<T> value) => T.IsInfinity(value);
            public static bool IsInteger(BinaryIntegerWrapper<T> value) => T.IsInteger(value);
            public static bool IsNaN(BinaryIntegerWrapper<T> value) => T.IsNaN(value);
            public static bool IsNegative(BinaryIntegerWrapper<T> value) => T.IsNegative(value);
            public static bool IsNegativeInfinity(BinaryIntegerWrapper<T> value) => T.IsNegativeInfinity(value);
            public static bool IsNormal(BinaryIntegerWrapper<T> value) => T.IsNormal(value);
            public static bool IsOddInteger(BinaryIntegerWrapper<T> value) => T.IsOddInteger(value);
            public static bool IsPositive(BinaryIntegerWrapper<T> value) => T.IsPositive(value);
            public static bool IsPositiveInfinity(BinaryIntegerWrapper<T> value) => T.IsPositiveInfinity(value);
            public static bool IsPow2(BinaryIntegerWrapper<T> value) => T.IsPow2(value);
            public static bool IsRealNumber(BinaryIntegerWrapper<T> value) => T.IsRealNumber(value);
            public static bool IsSubnormal(BinaryIntegerWrapper<T> value) => T.IsSubnormal(value);
            public static bool IsZero(BinaryIntegerWrapper<T> value) => T.IsZero(value);
            public static BinaryIntegerWrapper<T> Log2(BinaryIntegerWrapper<T> value) => T.Log2(value);
            public static BinaryIntegerWrapper<T> MaxMagnitude(BinaryIntegerWrapper<T> x, BinaryIntegerWrapper<T> y) => T.MaxMagnitude(x, y);
            public static BinaryIntegerWrapper<T> MaxMagnitudeNumber(BinaryIntegerWrapper<T> x, BinaryIntegerWrapper<T> y) => T.MaxMagnitudeNumber(x, y);
            public static BinaryIntegerWrapper<T> MinMagnitude(BinaryIntegerWrapper<T> x, BinaryIntegerWrapper<T> y) => T.MinMagnitude(x, y);
            public static BinaryIntegerWrapper<T> MinMagnitudeNumber(BinaryIntegerWrapper<T> x, BinaryIntegerWrapper<T> y) => T.MinMagnitudeNumber(x, y);
            public static BinaryIntegerWrapper<T> Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => T.Parse(s, style, provider);
            public static BinaryIntegerWrapper<T> Parse(string s, NumberStyles style, IFormatProvider? provider) => T.Parse(s, style, provider);
            public static BinaryIntegerWrapper<T> Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => T.Parse(s, provider);
            public static BinaryIntegerWrapper<T> Parse(string s, IFormatProvider? provider) => T.Parse(s, provider);
            public static BinaryIntegerWrapper<T> PopCount(BinaryIntegerWrapper<T> value) => T.PopCount(value);
            public static BinaryIntegerWrapper<T> TrailingZeroCount(BinaryIntegerWrapper<T> value) => T.TrailingZeroCount(value);
            public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out BinaryIntegerWrapper<T> result)
            {
                var succeeded = T.TryParse(s, style, provider, out T actualResult);
                result = actualResult;
                return succeeded;
            }
            public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out BinaryIntegerWrapper<T> result)
            {
                var succeeded = T.TryParse(s, style, provider, out T actualResult);
                result = actualResult;
                return succeeded;
            }
            public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out BinaryIntegerWrapper<T> result)
            {
                var succeeded = T.TryParse(s, provider, out T actualResult);
                result = actualResult;
                return succeeded;
            }
            public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out BinaryIntegerWrapper<T> result)
            {
                var succeeded = T.TryParse(s, provider, out T actualResult);
                result = actualResult;
                return succeeded;
            }
            public static bool TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out BinaryIntegerWrapper<T> value)
            {
                var succeeded = T.TryReadBigEndian(source, isUnsigned, out T actualValue);
                value = actualValue;
                return succeeded;
            }
            public static bool TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out BinaryIntegerWrapper<T> value)
            {
                var succeeded = T.TryReadLittleEndian(source, isUnsigned, out T actualValue);
                value = actualValue;
                return succeeded;
            }
            public int CompareTo(object? obj)
            {
                if (obj is not BinaryIntegerWrapper<T> other)
                {
                    return (obj is null) ? 1 : throw new ArgumentException();
                }
                return CompareTo(other);
            }
            public int CompareTo(BinaryIntegerWrapper<T> other) => Value.CompareTo(other.Value);
            public override bool Equals([NotNullWhen(true)] object? obj) => (obj is BinaryIntegerWrapper<T> other) && Equals(other);
            public bool Equals(BinaryIntegerWrapper<T> other) => Value.Equals(other.Value);
            public int GetByteCount() => Value.GetByteCount();
            public override int GetHashCode() => Value.GetHashCode();
            public int GetShortestBitLength() => Value.GetShortestBitLength();
            public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);
            public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => Value.TryFormat(destination, out charsWritten, format, provider);
            public bool TryWriteBigEndian(Span<byte> destination, out int bytesWritten) => Value.TryWriteBigEndian(destination, out bytesWritten);
            public bool TryWriteLittleEndian(Span<byte> destination, out int bytesWritten) => Value.TryWriteLittleEndian(destination, out bytesWritten);

            static bool INumberBase<BinaryIntegerWrapper<T>>.TryConvertFromChecked<TOther>(TOther value, out BinaryIntegerWrapper<T> result)
            {
                bool succeeded = T.TryConvertFromChecked(value, out T actualResult);
                result = actualResult;
                return succeeded;

            }
            static bool INumberBase<BinaryIntegerWrapper<T>>.TryConvertFromSaturating<TOther>(TOther value, out BinaryIntegerWrapper<T> result)
            {
                bool succeeded = T.TryConvertFromSaturating(value, out T actualResult);
                result = actualResult;
                return succeeded;

            }
            static bool INumberBase<BinaryIntegerWrapper<T>>.TryConvertFromTruncating<TOther>(TOther value, out BinaryIntegerWrapper<T> result)
            {
                bool succeeded = T.TryConvertFromTruncating(value, out T actualResult);
                result = actualResult;
                return succeeded;

            }
            static bool INumberBase<BinaryIntegerWrapper<T>>.TryConvertToChecked<TOther>(BinaryIntegerWrapper<T> value, out TOther result) => T.TryConvertToChecked(value, out result);
            static bool INumberBase<BinaryIntegerWrapper<T>>.TryConvertToSaturating<TOther>(BinaryIntegerWrapper<T> value, out TOther result) => T.TryConvertToSaturating(value, out result);
            static bool INumberBase<BinaryIntegerWrapper<T>>.TryConvertToTruncating<TOther>(BinaryIntegerWrapper<T> value, out TOther result) => T.TryConvertToTruncating(value, out result);

            public static BinaryIntegerWrapper<T> operator +(BinaryIntegerWrapper<T> value) => +value.Value;
            public static BinaryIntegerWrapper<T> operator +(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value + right.Value;
            public static BinaryIntegerWrapper<T> operator -(BinaryIntegerWrapper<T> value) => -value.Value;
            public static BinaryIntegerWrapper<T> operator -(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value - right.Value;
            public static BinaryIntegerWrapper<T> operator ~(BinaryIntegerWrapper<T> value) => ~value.Value;
            public static BinaryIntegerWrapper<T> operator ++(BinaryIntegerWrapper<T> value) => value.Value++;
            public static BinaryIntegerWrapper<T> operator --(BinaryIntegerWrapper<T> value) => value.Value--;
            public static BinaryIntegerWrapper<T> operator *(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value * right.Value;
            public static BinaryIntegerWrapper<T> operator /(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value / right.Value;
            public static BinaryIntegerWrapper<T> operator %(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value % right.Value;
            public static BinaryIntegerWrapper<T> operator &(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value & right.Value;
            public static BinaryIntegerWrapper<T> operator |(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value | right.Value;
            public static BinaryIntegerWrapper<T> operator ^(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value ^ right.Value;
            public static BinaryIntegerWrapper<T> operator <<(BinaryIntegerWrapper<T> value, int shiftAmount) => value.Value << shiftAmount;
            public static BinaryIntegerWrapper<T> operator >>(BinaryIntegerWrapper<T> value, int shiftAmount) => value.Value >> shiftAmount;
            public static bool operator ==(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value == right.Value;
            public static bool operator !=(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value != right.Value;
            public static bool operator <(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value < right.Value;
            public static bool operator >(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value > right.Value;
            public static bool operator <=(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value <= right.Value;
            public static bool operator >=(BinaryIntegerWrapper<T> left, BinaryIntegerWrapper<T> right) => left.Value >= right.Value;
            public static BinaryIntegerWrapper<T> operator >>>(BinaryIntegerWrapper<T> value, int shiftAmount) => value.Value >>> shiftAmount;
        }
    }
}
