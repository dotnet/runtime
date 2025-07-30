// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using Xunit;

namespace System.Linq.Tests
{
    public class SequenceTests : EnumerableTests
    {
        [Fact]
        public void InvalidArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("start", () => Enumerable.Sequence((ReferenceAddable)null!, new(1), new(2)));
            AssertExtensions.Throws<ArgumentNullException>("endInclusive", () => Enumerable.Sequence(new(1), (ReferenceAddable)null!, new(2)));
            AssertExtensions.Throws<ArgumentNullException>("step", () => Enumerable.Sequence(new(1), new(2), (ReferenceAddable)null!));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("start", () => Enumerable.Sequence(float.NaN, 1.0f, 1.0f));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("endInclusive", () => Enumerable.Sequence(1.0f, float.NaN, 1.0f));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("step", () => Enumerable.Sequence(1.0f, 1.0f, float.NaN));
        }

        [Fact]
        public void EndOutOfRange_Throws()
        {
            ValidateUnsigned<byte>();
            ValidateUnsigned<ushort>();
            ValidateUnsigned<char>();
            ValidateUnsigned<uint>();
            ValidateUnsigned<ulong>();
            ValidateUnsigned<nuint>();
            ValidateUnsigned<UInt128>();

            ValidateSigned<sbyte>();
            ValidateSigned<short>();
            ValidateSigned<int>();
            ValidateSigned<long>();
            ValidateSigned<nint>();
            ValidateSigned<Int128>();
            ValidateSigned<BigInteger>();

            ValidateSigned<Half>();
            ValidateSigned<float>();
            ValidateSigned<double>();

            static void ValidateSigned<T>() where T : INumber<T>
            {
                ValidateUnsigned<T>();

                for (int i = 1; i < 3; i++)
                {
                    Assert.NotNull(Enumerable.Sequence(T.CreateTruncating(123), T.CreateTruncating(122), T.CreateTruncating(-i)));
                }

                ValidateThrows(T.CreateTruncating(123), T.CreateTruncating(124), T.CreateTruncating(-2));
            }

            static void ValidateUnsigned<T>() where T : INumber<T>
            {
                for (int i = 0; i < 3; i++)
                {
                    Assert.NotNull(Enumerable.Sequence(T.CreateTruncating(123), T.CreateTruncating(123), T.CreateTruncating(i)));
                }

                for (int i = 1; i < 3; i++)
                {
                    Assert.NotNull(Enumerable.Sequence(T.CreateTruncating(123), T.CreateTruncating(124), T.CreateTruncating(i)));
                }

                ValidateThrows(T.CreateTruncating(123), T.CreateTruncating(122), T.CreateTruncating(2));
            }

            static void ValidateThrows<T>(T start, T endInclusive, T step) where T : INumber<T>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("endInclusive", () => Enumerable.Sequence(start, endInclusive, step));
            }
        }

        [Fact]
        public void MultipleGetEnumeratorCalls_ReturnsUniqueInstances()
        {
            IEnumerable<int> sequence = Enumerable.Sequence(0, 4, 2);

            using IEnumerator<int> enumerator1 = sequence.GetEnumerator();
            using IEnumerator<int> enumerator2 = sequence.GetEnumerator();
            Assert.NotSame(enumerator1, enumerator2);
        }

        [Fact]
        public void Step1_MatchesRange()
        {
            Validate<byte>(0, 10);
            Validate<ushort>(0, 10);
            Validate<char>(0, 10);
            Validate<uint>(0, 10);
            Validate<ulong>(0, 10);
            Validate<nuint>(0, 10);
            Validate<UInt128>(0, 10);

            Validate<sbyte>(-10, 10);
            Validate<short>(-10, 10);
            Validate<int>(-10, 10);
            Validate<long>(-10, 10);
            Validate<nint>(-10, 10);
            Validate<Int128>(-10, 10);
            Validate<BigInteger>(-10, 10);

            Validate<Half>(-10, 10);
            Validate<float>(-10, 10);
            Validate<double>(-10, 10);

            void Validate<T>(int startStart, int startEnd) where T : INumber<T>
            {
                for (int start = startStart; start <= startEnd; start++)
                {
                    for (int count = 1; count <= 20; count++)
                    {
                        Assert.Equal(
                            Enumerable.Range(start, count).Select(i => T.CreateTruncating(i)),
                            Enumerable.Sequence<T>(T.CreateTruncating(start), T.CreateTruncating(start + count - 1), T.One));
                    }
                }
            }
        }

        [Fact]
        public void Numbers_ProduceExpectedSequence()
        {
            ValidateUnsigned<byte>();
            ValidateUnsigned<ushort>();
            ValidateUnsigned<uint>();
            ValidateUnsigned<ulong>();
            ValidateUnsigned<nuint>();
            ValidateUnsigned<UInt128>();

            ValidateSigned<sbyte>();
            ValidateSigned<short>();
            ValidateSigned<int>();
            ValidateSigned<long>();
            ValidateSigned<long>();
            ValidateSigned<nint>();
            ValidateSigned<Int128>();

            ValidateSigned<Half>();
            ValidateSigned<float>();
            ValidateSigned<double>();

            void ValidateUnsigned<T>() where T : INumber<T>, IMinMaxValue<T>
            {
                Assert.Equal([T.Zero], Enumerable.Sequence(T.Zero, T.Zero, T.One));

                Assert.Equal([T.Zero, T.One, T.CreateTruncating(2), T.CreateTruncating(3), T.CreateTruncating(4)], Enumerable.Sequence(T.Zero, T.CreateTruncating(4), T.One));
                Assert.Equal([T.Zero, T.CreateTruncating(2), T.CreateTruncating(4)], Enumerable.Sequence(T.Zero, T.CreateTruncating(4), T.CreateTruncating(2)));
                Assert.Equal([T.Zero, T.CreateTruncating(3)], Enumerable.Sequence(T.Zero, T.CreateTruncating(4), T.CreateTruncating(3)));
                Assert.Equal([T.Zero, T.CreateTruncating(4)], Enumerable.Sequence(T.Zero, T.CreateTruncating(4), T.CreateTruncating(4)));
                Assert.Equal([T.Zero], Enumerable.Sequence(T.Zero, T.CreateTruncating(4), T.CreateTruncating(5)));

                Assert.Equal([T.CreateTruncating(42), T.CreateTruncating(45), T.CreateTruncating(48)], Enumerable.Sequence(T.CreateTruncating(42), T.CreateTruncating(50), T.CreateTruncating(3)));
                Assert.Equal([T.CreateTruncating(42), T.CreateTruncating(45), T.CreateTruncating(48), T.CreateTruncating(51)], Enumerable.Sequence(T.CreateTruncating(42), T.CreateTruncating(51), T.CreateTruncating(3)));
                Assert.Equal([T.MaxValue], Enumerable.Sequence(T.MaxValue, T.MaxValue, T.One));
                Assert.Equal([T.MaxValue], Enumerable.Sequence(T.MaxValue, T.MaxValue, T.MaxValue));
            }

            void ValidateSigned<T>() where T : INumber<T>, IMinMaxValue<T>
            {
                ValidateUnsigned<T>();

                Assert.Equal([T.One, T.Zero, T.CreateTruncating(-1), T.CreateTruncating(-2), T.CreateTruncating(-3), T.CreateTruncating(-4)], Enumerable.Sequence(T.One, T.CreateTruncating(-4), T.CreateTruncating(-1)));
                Assert.Equal([T.One, T.CreateTruncating(-1), T.CreateTruncating(-3)], Enumerable.Sequence(T.One, T.CreateTruncating(-4), T.CreateTruncating(-2)));
                Assert.Equal([T.One, T.CreateTruncating(-2)], Enumerable.Sequence(T.One, T.CreateTruncating(-4), T.CreateTruncating(-3)));
                Assert.Equal([T.One, T.CreateTruncating(-3)], Enumerable.Sequence(T.One, T.CreateTruncating(-4), T.CreateTruncating(-4)));
                Assert.Equal([T.One, T.CreateTruncating(-4)], Enumerable.Sequence(T.One, T.CreateTruncating(-4), T.CreateTruncating(-5)));
                Assert.Equal([T.One], Enumerable.Sequence(T.One, T.CreateTruncating(-4), T.CreateTruncating(-6)));

                Assert.Equal([T.MinValue], Enumerable.Sequence(T.MinValue, T.MinValue, T.CreateTruncating(-11)));
            }
        }

        [Fact]
        public void FloatingPoints_ProduceExpectedSequence()
        {
            float[] results;

            results = Enumerable.Sequence(0.123f, 0.456f, 0.123f).ToArray();
            Assert.Equal(3, results.Length);
            Assert.Equal(0.123f, results[0], 3);
            Assert.Equal(0.246f, results[1], 3);
            Assert.Equal(0.369f, results[2], 3);

            results = Enumerable.Sequence(0.123f, -0.456f, -0.123f).ToArray();
            Assert.Equal(5, results.Length);
            Assert.Equal(0.123f, results[0], 3);
            Assert.Equal(0.0f, results[1], 3);
            Assert.Equal(-0.123f, results[2], 3);
            Assert.Equal(-0.246f, results[3], 3);
            Assert.Equal(-0.369f, results[4], 3);

            results = Enumerable.Sequence(16_777_216f, float.MaxValue, 1.0f).ToArray();
            Assert.Equal(1, results.Length);
            Assert.Equal(16_777_216f, results[0], 3);

            results = Enumerable.Sequence(-16_777_217f, float.MinValue, -1.0f).ToArray();
            Assert.Equal(1, results.Length);
            Assert.Equal(-16_777_216f, results[0], 3);
        }

        [Fact]
        public void SmallIntegers_ProducesFullRange()
        {
            byte[] bytes = Enumerable.Sequence(byte.MinValue, byte.MaxValue, (byte)1).ToArray();
            Assert.Equal(256, bytes.Length);
            for (int i = 0; i < bytes.Length; i++)
            {
                Assert.Equal((byte)i, bytes[i]);
            }

            sbyte[] sbytes = Enumerable.Sequence(sbyte.MinValue, sbyte.MaxValue, (sbyte)1).ToArray();
            Assert.Equal(256, sbytes.Length);
            for (int i = 0; i < sbytes.Length; i++)
            {
                Assert.Equal((sbyte)(i - 128), sbytes[i]);
            }

            ushort[] ushorts = Enumerable.Sequence(ushort.MinValue, ushort.MaxValue, (ushort)1).ToArray();
            Assert.Equal(65536, ushorts.Length);
            for (int i = 0; i < ushorts.Length; i++)
            {
                Assert.Equal((ushort)i, ushorts[i]);
            }

            short[] shorts = Enumerable.Sequence(short.MinValue, short.MaxValue, (short)1).ToArray();
            Assert.Equal(65536, shorts.Length);
            for (int i = 0; i < shorts.Length; i++)
            {
                Assert.Equal((short)(i - 32768), shorts[i]);
            }
        }

        private sealed class ReferenceAddable(int value) : INumber<ReferenceAddable>
        {
            public static ReferenceAddable One => throw new NotImplementedException();
            public static int Radix => throw new NotImplementedException();
            public static ReferenceAddable Zero => throw new NotImplementedException();
            public static ReferenceAddable AdditiveIdentity => throw new NotImplementedException();
            public static ReferenceAddable MultiplicativeIdentity => throw new NotImplementedException();
            public static ReferenceAddable Abs(ReferenceAddable value) => throw new NotImplementedException();
            public static bool IsCanonical(ReferenceAddable value) => throw new NotImplementedException();
            public static bool IsComplexNumber(ReferenceAddable value) => throw new NotImplementedException();
            public static bool IsEvenInteger(ReferenceAddable value) => throw new NotImplementedException();
            public static bool IsFinite(ReferenceAddable value) => throw new NotImplementedException();
            public static bool IsImaginaryNumber(ReferenceAddable value) => throw new NotImplementedException();
            public static bool IsInfinity(ReferenceAddable value) => throw new NotImplementedException();
            public static bool IsInteger(ReferenceAddable value) => throw new NotImplementedException();
            public static bool IsNaN(ReferenceAddable value) => false;
            public static bool IsNegative(ReferenceAddable value) => false;
            public static bool IsNegativeInfinity(ReferenceAddable value) => throw new NotImplementedException();
            public static bool IsNormal(ReferenceAddable value) => throw new NotImplementedException();
            public static bool IsOddInteger(ReferenceAddable value) => throw new NotImplementedException();
            public static bool IsPositive(ReferenceAddable value) => false;
            public static bool IsPositiveInfinity(ReferenceAddable value) => throw new NotImplementedException();
            public static bool IsRealNumber(ReferenceAddable value) => throw new NotImplementedException();
            public static bool IsSubnormal(ReferenceAddable value) => throw new NotImplementedException();
            public static bool IsZero(ReferenceAddable value) => false;
            public static ReferenceAddable MaxMagnitude(ReferenceAddable x, ReferenceAddable y) => throw new NotImplementedException();
            public static ReferenceAddable MaxMagnitudeNumber(ReferenceAddable x, ReferenceAddable y) => throw new NotImplementedException();
            public static ReferenceAddable MinMagnitude(ReferenceAddable x, ReferenceAddable y) => throw new NotImplementedException();
            public static ReferenceAddable MinMagnitudeNumber(ReferenceAddable x, ReferenceAddable y) => throw new NotImplementedException();
            public static ReferenceAddable Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
            public static ReferenceAddable Parse(string s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
            public static ReferenceAddable Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => throw new NotImplementedException();
            public static ReferenceAddable Parse(string s, IFormatProvider? provider) => throw new NotImplementedException();
            public static bool TryConvertFromChecked<TOther>(TOther value, [MaybeNullWhen(false)] out ReferenceAddable result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
            public static bool TryConvertFromSaturating<TOther>(TOther value, [MaybeNullWhen(false)] out ReferenceAddable result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
            public static bool TryConvertFromTruncating<TOther>(TOther value, [MaybeNullWhen(false)] out ReferenceAddable result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
            public static bool TryConvertToChecked<TOther>(ReferenceAddable value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
            public static bool TryConvertToSaturating<TOther>(ReferenceAddable value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
            public static bool TryConvertToTruncating<TOther>(ReferenceAddable value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther> => throw new NotImplementedException();
            public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out ReferenceAddable result) => throw new NotImplementedException();
            public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out ReferenceAddable result) => throw new NotImplementedException();
            public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out ReferenceAddable result) => throw new NotImplementedException();
            public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out ReferenceAddable result) => throw new NotImplementedException();
            public int CompareTo(object? obj) => throw new NotImplementedException();
            public int CompareTo(ReferenceAddable? other) => throw new NotImplementedException();
            public bool Equals(ReferenceAddable? other) => throw new NotImplementedException();
            public string ToString(string? format, IFormatProvider? formatProvider) => value.ToString(format, formatProvider);
            public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => throw new NotImplementedException();
            public static ReferenceAddable operator +(ReferenceAddable value) => throw new NotImplementedException();
            public static ReferenceAddable operator +(ReferenceAddable left, ReferenceAddable right) => throw new NotImplementedException();
            public static ReferenceAddable operator -(ReferenceAddable value) => throw new NotImplementedException();
            public static ReferenceAddable operator -(ReferenceAddable left, ReferenceAddable right) => throw new NotImplementedException();
            public static ReferenceAddable operator ++(ReferenceAddable value) => throw new NotImplementedException();
            public static ReferenceAddable operator --(ReferenceAddable value) => throw new NotImplementedException();
            public static ReferenceAddable operator *(ReferenceAddable left, ReferenceAddable right) => throw new NotImplementedException();
            public static ReferenceAddable operator /(ReferenceAddable left, ReferenceAddable right) => throw new NotImplementedException();
            public static ReferenceAddable operator %(ReferenceAddable left, ReferenceAddable right) => throw new NotImplementedException();
            public static bool operator ==(ReferenceAddable? left, ReferenceAddable? right) => throw new NotImplementedException();
            public static bool operator !=(ReferenceAddable? left, ReferenceAddable? right) => throw new NotImplementedException();
            public static bool operator <(ReferenceAddable left, ReferenceAddable right) => throw new NotImplementedException();
            public static bool operator >(ReferenceAddable left, ReferenceAddable right) => throw new NotImplementedException();
            public static bool operator <=(ReferenceAddable left, ReferenceAddable right) => throw new NotImplementedException();
            public static bool operator >=(ReferenceAddable left, ReferenceAddable right) => throw new NotImplementedException();
            public override bool Equals(object obj) => throw new NotImplementedException();
            public override int GetHashCode() => throw new NotImplementedException();
        }
    }
}
