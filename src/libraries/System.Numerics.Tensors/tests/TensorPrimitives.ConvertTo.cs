// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Numerics.Tensors.Tests
{
    public class VectorizedConvertTests
    {
        public static IEnumerable<object[]> Lengths()
        {
            for (int length = 0; length <= 129; length++)
            {
                yield return [length];
            }

            yield return [257];
            yield return [1025];
        }

        [Theory]
        [MemberData(nameof(Lengths))]
        public void FloatingPointToInteger(int length)
        {
            float[] singles =
            [
                float.NaN, BitConverter.Int32BitsToSingle(0x7FC00001), float.PositiveInfinity, float.NegativeInfinity,
                float.MinValue, float.MaxValue, 0, float.NegativeZero, float.Epsilon, -float.Epsilon,
                -1, -1.75f, -0.75f, 0.75f, 1.75f,
                int.MinValue, float.BitDecrement(int.MinValue), float.BitIncrement(int.MinValue),
                int.MaxValue, float.BitDecrement(int.MaxValue), float.BitIncrement(int.MaxValue),
                uint.MaxValue, float.BitDecrement(uint.MaxValue), float.BitIncrement(uint.MaxValue),
            ];
            double[] doubles =
            [
                double.NaN, BitConverter.Int64BitsToDouble(0x7FF8000000000001), double.PositiveInfinity, double.NegativeInfinity,
                double.MinValue, double.MaxValue, 0, double.NegativeZero, double.Epsilon, -double.Epsilon,
                -1, -1.75, -0.75, 0.75, 1.75,
                long.MinValue, double.BitDecrement(long.MinValue), double.BitIncrement(long.MinValue),
                long.MaxValue, double.BitDecrement(long.MaxValue), double.BitIncrement(long.MaxValue),
                ulong.MaxValue, double.BitDecrement(ulong.MaxValue), double.BitIncrement(ulong.MaxValue),
            ];

            Validate<float, int>(length, singles);
            Validate<float, uint>(length, singles);
            Validate<double, long>(length, doubles);
            Validate<double, ulong>(length, doubles);
            Validate<float, nint>(length, singles);
            Validate<float, nuint>(length, singles);
            Validate<double, nint>(length, doubles);
            Validate<double, nuint>(length, doubles);
        }

        [Theory]
        [MemberData(nameof(Lengths))]
        public void IntegerToInteger(int length)
        {
            ValidateInteger<short, sbyte>(length);
            ValidateInteger<int, short>(length);
            ValidateInteger<long, int>(length);
            ValidateInteger<ushort, byte>(length);
            ValidateInteger<uint, ushort>(length);
            ValidateInteger<ulong, uint>(length);

            ValidateInteger<short, byte>(length);
            ValidateInteger<int, ushort>(length);
            ValidateInteger<long, uint>(length);
            ValidateInteger<ushort, sbyte>(length);
            ValidateInteger<uint, short>(length);
            ValidateInteger<ulong, int>(length);

            ValidateInteger<sbyte, byte>(length);
            ValidateInteger<byte, sbyte>(length);
            ValidateInteger<short, ushort>(length);
            ValidateInteger<ushort, short>(length);
            ValidateInteger<int, uint>(length);
            ValidateInteger<uint, int>(length);
            ValidateInteger<long, ulong>(length);
            ValidateInteger<ulong, long>(length);

            ValidateInteger<nint, short>(length);
            ValidateInteger<nuint, ushort>(length);
            ValidateInteger<nint, ushort>(length);
            ValidateInteger<nuint, short>(length);
            ValidateInteger<nint, int>(length);
            ValidateInteger<nuint, uint>(length);
            ValidateInteger<nint, uint>(length);
            ValidateInteger<nuint, int>(length);
            ValidateInteger<long, nint>(length);
            ValidateInteger<ulong, nuint>(length);
            ValidateInteger<long, nuint>(length);
            ValidateInteger<ulong, nint>(length);
            ValidateInteger<nint, nuint>(length);
            ValidateInteger<nuint, nint>(length);
        }

        [Theory]
        [MemberData(nameof(Lengths))]
        public void Universal(int length)
        {
            ValidateInteger<sbyte, int>(length, checkChecked: true);
            ValidateInteger<sbyte, float>(length, checkChecked: true);
            ValidateInteger<short, float>(length, checkChecked: true);
            ValidateInteger<ushort, float>(length, checkChecked: true);
            ValidateInteger<int, double>(length, checkChecked: true);
            ValidateInteger<uint, double>(length, checkChecked: true);
            ValidateInteger<nint, double>(length, checkChecked: true);
            ValidateInteger<nuint, double>(length, checkChecked: true);
        }

        private static void ValidateInteger<TFrom, TTo>(int length, bool checkChecked = false)
            where TFrom : unmanaged, IBinaryInteger<TFrom>, IMinMaxValue<TFrom>
            where TTo : unmanaged, INumberBase<TTo>
        {
            Int128[] boundaries =
            [
                -1, 0, 1,
                sbyte.MinValue - 1, sbyte.MinValue, sbyte.MinValue + 1, sbyte.MaxValue - 1, sbyte.MaxValue, sbyte.MaxValue + 1,
                byte.MaxValue - 1, byte.MaxValue, byte.MaxValue + 1,
                short.MinValue - 1, short.MinValue, short.MinValue + 1, short.MaxValue - 1, short.MaxValue, short.MaxValue + 1,
                ushort.MaxValue - 1, ushort.MaxValue, ushort.MaxValue + 1,
                (Int128)int.MinValue - 1, int.MinValue, (Int128)int.MinValue + 1, int.MaxValue - 1, int.MaxValue, (Int128)int.MaxValue + 1,
                (Int128)uint.MaxValue - 1, uint.MaxValue, (Int128)uint.MaxValue + 1,
                (Int128)long.MinValue - 1, long.MinValue, (Int128)long.MinValue + 1, long.MaxValue - 1, long.MaxValue, (Int128)long.MaxValue + 1,
                (Int128)ulong.MaxValue - 1, ulong.MaxValue, (Int128)ulong.MaxValue + 1,
            ];
            TFrom[] values = new TFrom[boundaries.Length + 2];
            values[0] = TFrom.MinValue;
            values[1] = TFrom.MaxValue;
            for (int i = 0; i < boundaries.Length; i++)
            {
                values[i + 2] = TFrom.CreateTruncating(boundaries[i]);
            }

            Validate<TFrom, TTo>(length, values, checkChecked);
        }

        private static void Validate<TFrom, TTo>(int length, TFrom[] values, bool checkChecked = false)
            where TFrom : unmanaged, INumberBase<TFrom>
            where TTo : unmanaged, INumberBase<TTo>
        {
            using BoundedMemory<TFrom> source = BoundedMemory.Allocate<TFrom>(length);
            using BoundedMemory<TTo> destination = BoundedMemory.Allocate<TTo>(length + 1);
            using BoundedMemory<TTo> exactDestination = BoundedMemory.Allocate<TTo>(length);
            TTo sentinel = TTo.CreateChecked(42);
            for (int i = 0; i < length; i++)
            {
                source[i] = values[i % values.Length];
            }

            for (int mode = 0; mode < (checkChecked ? 3 : 2); mode++)
            {
                destination.Span.Fill(sentinel);
                Convert<TFrom, TTo>(source.Span, destination.Span, mode);
                for (int i = 0; i < length; i++)
                {
                    TTo expected = mode switch
                    {
                        0 => TTo.CreateSaturating(source[i]),
                        1 => TTo.CreateTruncating(source[i]),
                        _ => TTo.CreateChecked(source[i]),
                    };
                    Assert.Equal(expected, destination[i]);
                }
                Assert.Equal(sentinel, destination[length]);

                Convert<TFrom, TTo>(source.Span, exactDestination.Span, mode);
                Assert.Equal(destination.Span.Slice(0, length).ToArray(), exactDestination.Span.ToArray());
                if (length > 0)
                {
                    AssertExtensions.Throws<ArgumentException>("destination", () =>
                        Convert<TFrom, TTo>(source.Span, destination.Span.Slice(0, length - 1), mode));
                }
            }
        }

        private static void Convert<TFrom, TTo>(ReadOnlySpan<TFrom> source, Span<TTo> destination, int mode)
            where TFrom : INumberBase<TFrom>
            where TTo : INumberBase<TTo>
        {
            switch (mode)
            {
                case 0: TensorPrimitives.ConvertSaturating(source, destination); break;
                case 1: TensorPrimitives.ConvertTruncating(source, destination); break;
                case 2: TensorPrimitives.ConvertChecked(source, destination); break;
                default: throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        [Theory]
        [MemberData(nameof(Lengths))]
        public void CheckedFloatingPointToIntegerStillThrows(int length)
        {
            ValidateCheckedExceptions<float, int>(length, [float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.MinValue, float.MaxValue]);
            ValidateCheckedExceptions<float, uint>(length, [float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.MinValue, float.MaxValue]);
            ValidateCheckedExceptions<double, long>(length, [double.NaN, double.PositiveInfinity, double.NegativeInfinity, double.MinValue, double.MaxValue]);
            ValidateCheckedExceptions<double, ulong>(length, [double.NaN, double.PositiveInfinity, double.NegativeInfinity, double.MinValue, double.MaxValue]);
        }

        private static void ValidateCheckedExceptions<TFrom, TTo>(int length, TFrom[] invalidValues)
            where TFrom : unmanaged, INumberBase<TFrom>
            where TTo : unmanaged, INumberBase<TTo>
        {
            using BoundedMemory<TFrom> source = BoundedMemory.Allocate<TFrom>(length);
            using BoundedMemory<TTo> destination = BoundedMemory.Allocate<TTo>(length);
            if (length == 0)
            {
                TensorPrimitives.ConvertChecked<TFrom, TTo>(source.Span, destination.Span);
                return;
            }

            foreach (TFrom invalid in invalidValues)
            {
                foreach (int position in new[] { 0, length / 2, length - 1 })
                {
                    source.Span.Fill(TFrom.One);
                    source[position] = invalid;
                    Assert.Throws<OverflowException>(() => TensorPrimitives.ConvertChecked<TFrom, TTo>(source.Span, destination.Span));
                }
            }
        }

        [Theory]
        [MemberData(nameof(Lengths))]
        public void SameWidthSaturatingInPlace(int length)
        {
            ValidateInPlace<sbyte, byte>(length);
            ValidateInPlace<byte, sbyte>(length);
            ValidateInPlace<short, ushort>(length);
            ValidateInPlace<ushort, short>(length);
            ValidateInPlace<int, uint>(length);
            ValidateInPlace<uint, int>(length);
            ValidateInPlace<long, ulong>(length);
            ValidateInPlace<ulong, long>(length);
            ValidateInPlace<nint, nuint>(length);
            ValidateInPlace<nuint, nint>(length);
        }

        private static void ValidateInPlace<TFrom, TTo>(int length)
            where TFrom : unmanaged, IBinaryInteger<TFrom>
            where TTo : unmanaged, IBinaryInteger<TTo>
        {
            using BoundedMemory<TFrom> buffer = BoundedMemory.Allocate<TFrom>(length);
            TTo[] expected = new TTo[length];
            for (int i = 0; i < length; i++)
            {
                buffer[i] = TFrom.CreateTruncating((i * 17) - 65);
                expected[i] = TTo.CreateSaturating(buffer[i]);
            }

            Span<TTo> destination = MemoryMarshal.Cast<TFrom, TTo>(buffer.Span);
            TensorPrimitives.ConvertSaturating<TFrom, TTo>(buffer.Span, destination);
            Assert.Equal(expected, destination.ToArray());
        }

        [Theory]
        [MemberData(nameof(Lengths))]
        public void NarrowingInPlace(int length)
        {
            ValidateNarrowingInPlace<short, sbyte>(length);
            ValidateNarrowingInPlace<int, short>(length);
            ValidateNarrowingInPlace<long, int>(length);
            ValidateNarrowingInPlace<ushort, byte>(length);
            ValidateNarrowingInPlace<uint, ushort>(length);
            ValidateNarrowingInPlace<ulong, uint>(length);
            ValidateNarrowingInPlace<short, byte>(length);
            ValidateNarrowingInPlace<int, ushort>(length);
            ValidateNarrowingInPlace<long, uint>(length);
            ValidateNarrowingInPlace<ushort, sbyte>(length);
            ValidateNarrowingInPlace<uint, short>(length);
            ValidateNarrowingInPlace<ulong, int>(length);
        }

        private static void ValidateNarrowingInPlace<TFrom, TTo>(int length)
            where TFrom : unmanaged, IBinaryInteger<TFrom>, IMinMaxValue<TFrom>
            where TTo : unmanaged, IBinaryInteger<TTo>
        {
            using BoundedMemory<TFrom> buffer = BoundedMemory.Allocate<TFrom>(length);
            Span<TTo> destination = MemoryMarshal.Cast<TFrom, TTo>(buffer.Span).Slice(0, length);
            TTo[] expected = new TTo[length];
            for (int mode = 0; mode < 2; mode++)
            {
                for (int i = 0; i < length; i++)
                {
                    buffer[i] = (i % 4) switch
                    {
                        0 => TFrom.MinValue,
                        1 => TFrom.MaxValue,
                        _ => TFrom.CreateTruncating((i * 127) - 65),
                    };
                    expected[i] = mode == 0 ? TTo.CreateSaturating(buffer[i]) : TTo.CreateTruncating(buffer[i]);
                }

                Convert<TFrom, TTo>(buffer.Span, destination, mode);
                Assert.Equal(expected, destination.ToArray());
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(65)]
        public void CustomNumberUsesMatchingContract(int length)
        {
            float[] source = new float[length];
            ConversionNumber[] numbers = new ConversionNumber[length];
            int[] destination = new int[length];
            for (int i = 0; i < length; i++)
            {
                source[i] = i;
            }

            for (int mode = 0; mode < 3; mode++)
            {
                Convert<float, ConversionNumber>(source, numbers, mode);
                Convert<ConversionNumber, int>(numbers, destination, mode);
                int adjustment = mode switch { 0 => 2, 1 => 3, _ => 1 };
                for (int i = 0; i < length; i++)
                {
                    Assert.Equal(i + adjustment, numbers[i].Value);
                    Assert.Equal(i + (2 * adjustment), destination[i]);
                }
            }
        }

        private readonly struct ConversionNumber(int value) : INumberBase<ConversionNumber>
        {
            public int Value { get; } = value;
            public bool Equals(ConversionNumber other) => Value == other.Value;

            static bool INumberBase<ConversionNumber>.TryConvertFromChecked<TOther>(TOther value, out ConversionNumber result)
            {
                result = new(int.CreateChecked(value) + 1);
                return true;
            }
            static bool INumberBase<ConversionNumber>.TryConvertFromSaturating<TOther>(TOther value, out ConversionNumber result)
            {
                result = new(int.CreateSaturating(value) + 2);
                return true;
            }
            static bool INumberBase<ConversionNumber>.TryConvertFromTruncating<TOther>(TOther value, out ConversionNumber result)
            {
                result = new(int.CreateTruncating(value) + 3);
                return true;
            }
            static bool INumberBase<ConversionNumber>.TryConvertToChecked<TOther>(ConversionNumber value, out TOther result)
            {
                result = TOther.CreateChecked(value.Value + 1);
                return true;
            }
            static bool INumberBase<ConversionNumber>.TryConvertToSaturating<TOther>(ConversionNumber value, out TOther result)
            {
                result = TOther.CreateSaturating(value.Value + 2);
                return true;
            }
            static bool INumberBase<ConversionNumber>.TryConvertToTruncating<TOther>(ConversionNumber value, out TOther result)
            {
                result = TOther.CreateTruncating(value.Value + 3);
                return true;
            }

            public static ConversionNumber One => throw new NotImplementedException();
            public static ConversionNumber Zero => throw new NotImplementedException();
            public static int Radix => throw new NotImplementedException();
            public static ConversionNumber AdditiveIdentity => throw new NotImplementedException();
            public static ConversionNumber MultiplicativeIdentity => throw new NotImplementedException();
            public static ConversionNumber Abs(ConversionNumber value) => throw new NotImplementedException();
            public static bool IsCanonical(ConversionNumber value) => throw new NotImplementedException();
            public static bool IsComplexNumber(ConversionNumber value) => throw new NotImplementedException();
            public static bool IsEvenInteger(ConversionNumber value) => throw new NotImplementedException();
            public static bool IsFinite(ConversionNumber value) => throw new NotImplementedException();
            public static bool IsImaginaryNumber(ConversionNumber value) => throw new NotImplementedException();
            public static bool IsInfinity(ConversionNumber value) => throw new NotImplementedException();
            public static bool IsInteger(ConversionNumber value) => throw new NotImplementedException();
            public static bool IsNaN(ConversionNumber value) => throw new NotImplementedException();
            public static bool IsNegative(ConversionNumber value) => throw new NotImplementedException();
            public static bool IsNegativeInfinity(ConversionNumber value) => throw new NotImplementedException();
            public static bool IsNormal(ConversionNumber value) => throw new NotImplementedException();
            public static bool IsOddInteger(ConversionNumber value) => throw new NotImplementedException();
            public static bool IsPositive(ConversionNumber value) => throw new NotImplementedException();
            public static bool IsPositiveInfinity(ConversionNumber value) => throw new NotImplementedException();
            public static bool IsRealNumber(ConversionNumber value) => throw new NotImplementedException();
            public static bool IsSubnormal(ConversionNumber value) => throw new NotImplementedException();
            public static bool IsZero(ConversionNumber value) => throw new NotImplementedException();
            public static ConversionNumber MaxMagnitude(ConversionNumber x, ConversionNumber y) => throw new NotImplementedException();
            public static ConversionNumber MaxMagnitudeNumber(ConversionNumber x, ConversionNumber y) => throw new NotImplementedException();
            public static ConversionNumber MinMagnitude(ConversionNumber x, ConversionNumber y) => throw new NotImplementedException();
            public static ConversionNumber MinMagnitudeNumber(ConversionNumber x, ConversionNumber y) => throw new NotImplementedException();
            public static ConversionNumber Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
            public static ConversionNumber Parse(string s, NumberStyles style, IFormatProvider? provider) => throw new NotImplementedException();
            public static ConversionNumber Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => throw new NotImplementedException();
            public static ConversionNumber Parse(string s, IFormatProvider? provider) => throw new NotImplementedException();
            public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out ConversionNumber result) => throw new NotImplementedException();
            public static bool TryParse(string? s, NumberStyles style, IFormatProvider? provider, out ConversionNumber result) => throw new NotImplementedException();
            public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out ConversionNumber result) => throw new NotImplementedException();
            public static bool TryParse(string? s, IFormatProvider? provider, out ConversionNumber result) => throw new NotImplementedException();
            public string ToString(string? format, IFormatProvider? provider) => throw new NotImplementedException();
            public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => throw new NotImplementedException();
            public static ConversionNumber operator +(ConversionNumber x) => throw new NotImplementedException();
            public static ConversionNumber operator -(ConversionNumber x) => throw new NotImplementedException();
            public static ConversionNumber operator +(ConversionNumber x, ConversionNumber y) => throw new NotImplementedException();
            public static ConversionNumber operator -(ConversionNumber x, ConversionNumber y) => throw new NotImplementedException();
            public static ConversionNumber operator *(ConversionNumber x, ConversionNumber y) => throw new NotImplementedException();
            public static ConversionNumber operator /(ConversionNumber x, ConversionNumber y) => throw new NotImplementedException();
            public static ConversionNumber operator ++(ConversionNumber x) => throw new NotImplementedException();
            public static ConversionNumber operator --(ConversionNumber x) => throw new NotImplementedException();
            public static bool operator ==(ConversionNumber x, ConversionNumber y) => x.Equals(y);
            public static bool operator !=(ConversionNumber x, ConversionNumber y) => !x.Equals(y);
            public override bool Equals(object? obj) => obj is ConversionNumber other && Equals(other);
            public override int GetHashCode() => Value.GetHashCode();
        }
    }

    public class ConvertToHalfTests
    {
        private readonly Random _random = new Random(42);

        [Fact]
        public void ConvertToHalf()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<float> source = CreateAndFillSingleTensor(tensorLength);
                foreach (int destLength in new[] { source.Length, source.Length + 1 })
                {
                    using BoundedMemory<Half> destination = BoundedMemory.Allocate<Half>(destLength);
                    destination.Span.Fill(Half.Zero);

                    TensorPrimitives.ConvertToHalf(source, destination);

                    for (int i = 0; i < source.Length; i++)
                    {
                        Assert.Equal((Half)source[i], destination[i]);
                    }

                    if (destination.Length > source.Length)
                    {
                        for (int i = source.Length; i < destination.Length; i++)
                        {
                            Assert.Equal(Half.Zero, destination[i]);
                        }
                    }
                }
            });
        }

        [Fact]
        public void ConvertToHalf_SpecialValues()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<float> source = CreateAndFillSingleTensor(tensorLength);
                using BoundedMemory<Half> destination = BoundedMemory.Allocate<Half>(tensorLength);

                // NaN, infinities, and 0s
                source[_random.Next(source.Length)] = float.NaN;
                source[_random.Next(source.Length)] = float.PositiveInfinity;
                source[_random.Next(source.Length)] = float.NegativeInfinity;
                source[_random.Next(source.Length)] = 0;
                source[_random.Next(source.Length)] = float.NegativeZero;

                TensorPrimitives.ConvertToHalf(source, destination);

                for (int i = 0; i < source.Length; i++)
                {
                    Assert.Equal((Half)source[i], destination[i]);
                }
            });
        }

        [Fact]
        public void ConvertToHalf_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<float> source = CreateAndFillSingleTensor(tensorLength);
                Half[] destination = new Half[source.Length - 1];

                AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.ConvertToHalf(source, destination));
            });
        }

        [Fact]
        public void ConvertToSingle()
        {
            Assert.All(Helpers.TensorLengthsIncluding0, tensorLength =>
            {
                using BoundedMemory<Half> source = BoundedMemory.Allocate<Half>(tensorLength);
                for (int i = 0; i < source.Length; i++)
                {
                    source[i] = (Half)_random.NextSingle();
                }

                foreach (int destLength in new[] { source.Length, source.Length + 1 })
                {
                    using BoundedMemory<float> destination = CreateSingleTensor(destLength);
                    destination.Span.Fill(0f);

                    TensorPrimitives.ConvertToSingle(source, destination);

                    for (int i = 0; i < source.Length; i++)
                    {
                        Assert.Equal((float)source[i], destination[i]);
                    }

                    if (destination.Length > source.Length)
                    {
                        for (int i = source.Length; i < destination.Length; i++)
                        {
                            Assert.Equal(0f, destination[i]);
                        }
                    }
                }
            });
        }

        [Fact]
        public void ConvertToSingle_SpecialValues()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                using BoundedMemory<Half> source = BoundedMemory.Allocate<Half>(tensorLength);
                for (int i = 0; i < source.Length; i++)
                {
                    source[i] = (Half)_random.NextSingle();
                }

                using BoundedMemory<float> destination = CreateSingleTensor(tensorLength);

                // NaN, infinities, and 0s
                source[_random.Next(source.Length)] = Half.NaN;
                source[_random.Next(source.Length)] = Half.PositiveInfinity;
                source[_random.Next(source.Length)] = Half.NegativeInfinity;
                source[_random.Next(source.Length)] = Half.Zero;
                source[_random.Next(source.Length)] = Half.NegativeZero;

                TensorPrimitives.ConvertToSingle(source, destination);

                for (int i = 0; i < source.Length; i++)
                {
                    Assert.Equal((float)source[i], destination[i]);
                }
            });
        }

        [Fact]
        public void ConvertToSingle_ThrowsForTooShortDestination()
        {
            Assert.All(Helpers.TensorLengths, tensorLength =>
            {
                Half[] source = new Half[tensorLength];
                using BoundedMemory<float> destination = CreateSingleTensor(source.Length - 1);

                AssertExtensions.Throws<ArgumentException>("destination", () => TensorPrimitives.ConvertToSingle(source, destination));
            });
        }

        public BoundedMemory<float> CreateSingleTensor(int size) => BoundedMemory.Allocate<float>(size);

        public BoundedMemory<float> CreateAndFillSingleTensor(int size)
        {
            BoundedMemory<float> tensor = CreateSingleTensor(size);
            Span<float> span = tensor;
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = (float)((_random.NextDouble() * 2) - 1); // For testing purposes, get a mix of negative and positive values.
            }
            return tensor;
        }
    }
}
