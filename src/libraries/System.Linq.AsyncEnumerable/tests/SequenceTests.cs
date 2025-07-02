// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class SequenceTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("start", () => AsyncEnumerable.Sequence((ReferenceAddable)null!, new(1), new(2)));
            AssertExtensions.Throws<ArgumentNullException>("endInclusive", () => AsyncEnumerable.Sequence(new(1), (ReferenceAddable)null!, new(2)));
            AssertExtensions.Throws<ArgumentNullException>("step", () => AsyncEnumerable.Sequence(new(1), new(2), (ReferenceAddable)null!));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("start", () => AsyncEnumerable.Sequence(float.NaN, 1.0f, 1.0f));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("endInclusive", () => AsyncEnumerable.Sequence(1.0f, float.NaN, 1.0f));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("step", () => AsyncEnumerable.Sequence(1.0f, 1.0f, float.NaN));
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
                    Assert.NotNull(AsyncEnumerable.Sequence(T.CreateTruncating(123), T.CreateTruncating(122), T.CreateTruncating(-i)));
                }

                ValidateThrows(T.CreateTruncating(123), T.CreateTruncating(124), T.CreateTruncating(-2));
            }

            static void ValidateUnsigned<T>() where T : INumber<T>
            {
                for (int i = 0; i < 3; i++)
                {
                    Assert.NotNull(AsyncEnumerable.Sequence(T.CreateTruncating(123), T.CreateTruncating(123), T.CreateTruncating(i)));
                }

                for (int i = 1; i < 3; i++)
                {
                    Assert.NotNull(AsyncEnumerable.Sequence(T.CreateTruncating(123), T.CreateTruncating(124), T.CreateTruncating(i)));
                }

                ValidateThrows(T.CreateTruncating(123), T.CreateTruncating(122), T.CreateTruncating(2));
            }

            static void ValidateThrows<T>(T start, T endInclusive, T step) where T : INumber<T>
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("endInclusive", () => AsyncEnumerable.Sequence(start, endInclusive, step));
            }
        }

        [Fact]
        public async Task MultipleGetEnumeratorCalls_ReturnsUniqueInstances()
        {
            IAsyncEnumerable<int> sequence = AsyncEnumerable.Sequence(0, 4, 2);

            await using IAsyncEnumerator<int> enumerator1 = sequence.GetAsyncEnumerator();
            await using IAsyncEnumerator<int> enumerator2 = sequence.GetAsyncEnumerator();
            Assert.NotSame(enumerator1, enumerator2);
        }

        [Fact]
        public async Task Step1_MatchesRange()
        {
            await ValidateAsync<byte>(0, 10);
            await ValidateAsync<ushort>(0, 10);
            await ValidateAsync<char>(0, 10);
            await ValidateAsync<uint>(0, 10);
            await ValidateAsync<ulong>(0, 10);
            await ValidateAsync<nuint>(0, 10);
            await ValidateAsync<UInt128>(0, 10);

            await ValidateAsync<sbyte>(-10, 10);
            await ValidateAsync<short>(-10, 10);
            await ValidateAsync<int>(-10, 10);
            await ValidateAsync<long>(-10, 10);
            await ValidateAsync<nint>(-10, 10);
            await ValidateAsync<Int128>(-10, 10);
            await ValidateAsync<BigInteger>(-10, 10);

            await ValidateAsync<Half>(-10, 10);
            await ValidateAsync<float>(-10, 10);
            await ValidateAsync<double>(-10, 10);

            async Task ValidateAsync<T>(int startStart, int startEnd) where T : INumber<T>
            {
                for (int start = startStart; start <= startEnd; start++)
                {
                    for (int count = 1; count <= 20; count++)
                    {
                        await AssertEqual(
                            AsyncEnumerable.Range(start, count).Select(i => T.CreateTruncating(i)),
                            AsyncEnumerable.Sequence<T>(T.CreateTruncating(start), T.CreateTruncating(start + count - 1), T.One));
                    }
                }
            }
        }

        [Fact]
        public async Task Numbers_ProduceExpectedSequence()
        {
            await ValidateUnsignedAsync<byte>();
            await ValidateUnsignedAsync<ushort>();
            await ValidateUnsignedAsync<uint>();
            await ValidateUnsignedAsync<ulong>();
            await ValidateUnsignedAsync<nuint>();
            await ValidateUnsignedAsync<UInt128>();

            await ValidateSignedAsync<sbyte>();
            await ValidateSignedAsync<short>();
            await ValidateSignedAsync<int>();
            await ValidateSignedAsync<long>();
            await ValidateSignedAsync<long>();
            await ValidateSignedAsync<nint>();
            await ValidateSignedAsync<Int128>();

            await ValidateSignedAsync<Half>();
            await ValidateSignedAsync<float>();
            await ValidateSignedAsync<double>();

            async Task ValidateUnsignedAsync<T>() where T : INumber<T>, IMinMaxValue<T>
            {
                await AssertEqual([T.Zero], AsyncEnumerable.Sequence(T.Zero, T.Zero, T.One));

                await AssertEqual([T.Zero, T.One, T.CreateTruncating(2), T.CreateTruncating(3), T.CreateTruncating(4)], AsyncEnumerable.Sequence(T.Zero, T.CreateTruncating(4), T.One));
                await AssertEqual([T.Zero, T.CreateTruncating(2), T.CreateTruncating(4)], AsyncEnumerable.Sequence(T.Zero, T.CreateTruncating(4), T.CreateTruncating(2)));
                await AssertEqual([T.Zero, T.CreateTruncating(3)], AsyncEnumerable.Sequence(T.Zero, T.CreateTruncating(4), T.CreateTruncating(3)));
                await AssertEqual([T.Zero, T.CreateTruncating(4)], AsyncEnumerable.Sequence(T.Zero, T.CreateTruncating(4), T.CreateTruncating(4)));
                await AssertEqual([T.Zero], AsyncEnumerable.Sequence(T.Zero, T.CreateTruncating(4), T.CreateTruncating(5)));

                await AssertEqual([T.CreateTruncating(42), T.CreateTruncating(45), T.CreateTruncating(48)], AsyncEnumerable.Sequence(T.CreateTruncating(42), T.CreateTruncating(50), T.CreateTruncating(3)));
                await AssertEqual([T.CreateTruncating(42), T.CreateTruncating(45), T.CreateTruncating(48), T.CreateTruncating(51)], AsyncEnumerable.Sequence(T.CreateTruncating(42), T.CreateTruncating(51), T.CreateTruncating(3)));
                await AssertEqual([T.MaxValue], AsyncEnumerable.Sequence(T.MaxValue, T.MaxValue, T.One));
                await AssertEqual([T.MaxValue], AsyncEnumerable.Sequence(T.MaxValue, T.MaxValue, T.MaxValue));
            }

            async Task ValidateSignedAsync<T>() where T : INumber<T>, IMinMaxValue<T>
            {
                await ValidateUnsignedAsync<T>();

                await AssertEqual([T.One, T.Zero, T.CreateTruncating(-1), T.CreateTruncating(-2), T.CreateTruncating(-3), T.CreateTruncating(-4)], AsyncEnumerable.Sequence(T.One, T.CreateTruncating(-4), T.CreateTruncating(-1)));
                await AssertEqual([T.One, T.CreateTruncating(-1), T.CreateTruncating(-3)], AsyncEnumerable.Sequence(T.One, T.CreateTruncating(-4), T.CreateTruncating(-2)));
                await AssertEqual([T.One, T.CreateTruncating(-2)], AsyncEnumerable.Sequence(T.One, T.CreateTruncating(-4), T.CreateTruncating(-3)));
                await AssertEqual([T.One, T.CreateTruncating(-3)], AsyncEnumerable.Sequence(T.One, T.CreateTruncating(-4), T.CreateTruncating(-4)));
                await AssertEqual([T.One, T.CreateTruncating(-4)], AsyncEnumerable.Sequence(T.One, T.CreateTruncating(-4), T.CreateTruncating(-5)));
                await AssertEqual([T.One], AsyncEnumerable.Sequence(T.One, T.CreateTruncating(-4), T.CreateTruncating(-6)));

                await AssertEqual([T.MinValue], AsyncEnumerable.Sequence(T.MinValue, T.MinValue, T.CreateTruncating(-11)));
            }
        }

        [Fact]
        public async Task FloatingPoints_ProduceExpectedSequence()
        {
            float[] results;

            results = await AsyncEnumerable.Sequence(0.123f, 0.456f, 0.123f).ToArrayAsync();
            Assert.Equal(3, results.Length);
            Assert.Equal(0.123f, results[0], 3);
            Assert.Equal(0.246f, results[1], 3);
            Assert.Equal(0.369f, results[2], 3);

            results = await AsyncEnumerable.Sequence(0.123f, -0.456f, -0.123f).ToArrayAsync();
            Assert.Equal(5, results.Length);
            Assert.Equal(0.123f, results[0], 3);
            Assert.Equal(0.0f, results[1], 3);
            Assert.Equal(-0.123f, results[2], 3);
            Assert.Equal(-0.246f, results[3], 3);
            Assert.Equal(-0.369f, results[4], 3);

            results = await AsyncEnumerable.Sequence(16_777_216f, float.MaxValue, 1.0f).ToArrayAsync();
            Assert.Equal(1, results.Length);
            Assert.Equal(16_777_216f, results[0], 3);

            results = await AsyncEnumerable.Sequence(-16_777_217f, float.MinValue, -1.0f).ToArrayAsync();
            Assert.Equal(1, results.Length);
            Assert.Equal(-16_777_216f, results[0], 3);
        }

        [Fact]
        public async Task SmallIntegers_ProducesFullRange()
        {
            byte[] bytes = await AsyncEnumerable.Sequence(byte.MinValue, byte.MaxValue, (byte)1).ToArrayAsync();
            Assert.Equal(256, bytes.Length);
            for (int i = 0; i < bytes.Length; i++)
            {
                Assert.Equal((byte)i, bytes[i]);
            }

            sbyte[] sbytes = await AsyncEnumerable.Sequence(sbyte.MinValue, sbyte.MaxValue, (sbyte)1).ToArrayAsync();
            Assert.Equal(256, sbytes.Length);
            for (int i = 0; i < sbytes.Length; i++)
            {
                Assert.Equal((sbyte)(i - 128), sbytes[i]);
            }

            ushort[] ushorts = await AsyncEnumerable.Sequence(ushort.MinValue, ushort.MaxValue, (ushort)1).ToArrayAsync();
            Assert.Equal(65536, ushorts.Length);
            for (int i = 0; i < ushorts.Length; i++)
            {
                Assert.Equal((ushort)i, ushorts[i]);
            }

            short[] shorts = await AsyncEnumerable.Sequence(short.MinValue, short.MaxValue, (short)1).ToArrayAsync();
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
