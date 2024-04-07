// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics;
using Xunit;

namespace System.Linq.Tests
{
    public class MaxTests : EnumerableTests
    {
        public static IEnumerable<object[]> Max_AllTypes_TestData()
        {
            for (int length = 2; length < 65; length++)
            {
                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (byte)i)), (byte)(length + length - 1) };
                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (byte)i).ToArray()), (byte)(length + length - 1) };

                // Unit Tests does +T.One so we should generate data up to one value below sbyte.MaxValue
                if ((length + length) < sbyte.MaxValue) {
                    yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (sbyte)i)), (sbyte)(length + length - 1) };
                    yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (sbyte)i).ToArray()), (sbyte)(length + length - 1) };
                }

                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (ushort)i)), (ushort)(length + length - 1) };
                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (ushort)i).ToArray()), (ushort)(length + length - 1) };

                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (short)i)), (short)(length + length - 1) };
                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (short)i).ToArray()), (short)(length + length - 1) };

                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (char)i)), (char)(length + length - 1) };
                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (char)i).ToArray()), (char)(length + length - 1) };

                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (uint)i)), (uint)(length + length - 1) };
                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (uint)i).ToArray()), (uint)(length + length - 1) };

                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (int)i)), (int)(length + length - 1) };
                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (int)i).ToArray()), (int)(length + length - 1) };

                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (ulong)i)), (ulong)(length + length - 1) };
                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (ulong)i).ToArray()), (ulong)(length + length - 1) };

                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (long)i)), (long)(length + length - 1) };
                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (long)i).ToArray()), (long)(length + length - 1) };

                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (float)i)), (float)(length + length - 1) };
                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (float)i).ToArray()), (float)(length + length - 1) };

                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (double)i)), (double)(length + length - 1) };
                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (double)i).ToArray()), (double)(length + length - 1) };

                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (decimal)i)), (decimal)(length + length - 1) };
                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (decimal)i).ToArray()), (decimal)(length + length - 1) };

                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (nuint)i)), (nuint)(length + length - 1) };
                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (nuint)i).ToArray()), (nuint)(length + length - 1) };

                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (nint)i)), (nint)(length + length - 1) };
                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (nint)i).ToArray()), (nint)(length + length - 1) };

                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (Int128)i)), (Int128)(length + length - 1) };
                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (Int128)i).ToArray()), (Int128)(length + length - 1) };

                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (UInt128)i)), (UInt128)(length + length - 1) };
                yield return new object[] { Shuffler.Shuffle(Enumerable.Range(length, length).Select(i => (UInt128)i).ToArray()), (UInt128)(length + length - 1) };
            }
        }

        [Theory]
        [MemberData(nameof(Max_AllTypes_TestData))]
        public void Max_AllTypes<T>(IEnumerable<T> source, T expected) where T : INumber<T>
        {
            Assert.Equal(expected, source.Max());

            Assert.Equal(expected, source.Max(comparer: null));
            Assert.Equal(expected, source.Max(Comparer<T>.Default));
            Assert.Equal(expected, source.Max(Comparer<T>.Create(Comparer<T>.Default.Compare)));

            T first = source.First();
            Assert.Equal(first, source.Max(Comparer<T>.Create((x, y) => x == first ? 1 : -1)));

            Assert.Equal(expected + T.One, source.Max(x => x + T.One));
        }

        [Fact]
        public void SameResultsRepeatCallsIntQuery()
        {
            var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                    where x > int.MinValue
                    select x;

            Assert.Equal(q.Max(), q.Max());
        }

        [Fact]
        public void SameResultsRepeatCallsStringQuery()
        {
            var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", string.Empty }
                    where !string.IsNullOrEmpty(x)
                    select x;

            Assert.Equal(q.Max(), q.Max());
        }

        [Fact]
        public void Max_Int_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).Max(i => i));
        }

        [Fact]
        public void Max_Int_EmptySource_ThrowsInvalidOpertionException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Max());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Max(x => x));
            Assert.Throws<InvalidOperationException>(() => Array.Empty<int>().Max());
            Assert.Throws<InvalidOperationException>(() => new List<int>().Max());
        }

        public static IEnumerable<object[]> Max_Int_TestData()
        {
            foreach ((int[] array, long expected) in new[]
            {
                (new[] { 42 }, 42),
                (Enumerable.Range(1, 10).ToArray(), 10),
                (new int[] { -100, -15, -50, -10 }, -10),
                (new int[] { -16, 0, 50, 100, 1000 }, 1000),
                (new int[] { -16, 0, 50, 100, 1000 }.Concat(Enumerable.Repeat(int.MaxValue, 1)).ToArray(), int.MaxValue),

                (new[] { 20 }, 20),
                (Enumerable.Repeat(-2, 5).ToArray(), -2),
                (new int[] { 16, 9, 10, 7, 8 }, 16),
                (new int[] { 6, 9, 10, 0, 50 }, 50),
                (new int[] { -6, 0, -9, 0, -10, 0 }, 0),
            })
            {
                yield return new object[] { new TestEnumerable<int>(array), expected };
                yield return new object[] { array, expected };
            }
        }

        [Theory]
        [MemberData(nameof(Max_Int_TestData))]
        public void Max_Int(IEnumerable<int> source, int expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        public static IEnumerable<object[]> Max_Long_TestData()
        {
            foreach ((long[] array, long expected) in new[]
            {
                (new[] { 42L }, 42L),
                (Enumerable.Range(1, 10).Select(i => (long)i).ToArray(), 10L),
                (new long[] { -100, -15, -50, -10 }, -10L),
                (new long[] { -16, 0, 50, 100, 1000 }, 1000L),
                (new long[] { -16, 0, 50, 100, 1000 }.Concat(Enumerable.Repeat(long.MaxValue, 1)).ToArray(), long.MaxValue),

                (new[] { int.MaxValue + 10L }, int.MaxValue + 10L),
                (Enumerable.Repeat(500L, 5).ToArray(), 500L),
                (new long[] { 250, 49, 130, 47, 28 }, 250L),
                (new long[] { 6, 9, 10, 0, int.MaxValue + 50L }, int.MaxValue + 50L),
                (new long[] { 6, 50, 9, 50, 10, 50 }, 50L),
            })
            {
                yield return new object[] { new TestEnumerable<long>(array), expected };
                yield return new object[] { array, expected };
            }
        }

        [Theory]
        [MemberData(nameof(Max_Long_TestData))]
        public void Max_Long(IEnumerable<long> source, long expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Fact]
        public void Max_Long_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<long>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<long>)null).Max(i => i));
        }

        [Fact]
        public void Max_Long_EmptySource_ThrowsInvalidOpertionException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<long>().Max());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<long>().Max(x => x));
            Assert.Throws<InvalidOperationException>(() => Array.Empty<long>().Max());
            Assert.Throws<InvalidOperationException>(() => new List<long>().Max());
        }

        public static IEnumerable<object[]> Max_Float_TestData()
        {
            foreach ((float[] array, float expected) in new[]
            {
                (new[] { 42f }, 42f),
                (Enumerable.Range(1, 10).Select(i => (float)i).ToArray(), 10f),
                (new float[] { -100, -15, -50, -10 }, -10f),
                (new float[] { -16, 0, 50, 100, 1000 }, 1000f),
                (new float[] { -16, 0, 50, 100, 1000 }.Concat(Enumerable.Repeat(float.MaxValue, 1)).ToArray(), float.MaxValue),

                (new[] { 5.5f }, 5.5f),
                (new float[] { 112.5f, 4.9f, 30f, 4.7f, 28f }, 112.5f),
                (new float[] { 6.8f, 9.4f, -10f, 0f, float.NaN, 53.6f }, 53.6f),
                (new float[] { -5.5f, float.PositiveInfinity, 9.9f, float.PositiveInfinity }, float.PositiveInfinity),

                (Enumerable.Repeat(float.NaN, 5).ToArray(), float.NaN),
                (new float[] { float.NaN, 6.8f, 9.4f, 10f, 0, -5.6f }, 10f),
                (new float[] { 6.8f, 9.4f, 10f, 0, -5.6f, float.NaN }, 10f),
                (new float[] { float.NaN, float.NegativeInfinity }, float.NegativeInfinity),
                (new float[] { float.NegativeInfinity, float.NaN }, float.NegativeInfinity),
                
                // Normally NaN < anything and anything < NaN returns false
                // However, this leads to some irksome outcomes in Min and Max.
                // If we use those semantics then Min(NaN, 5.0) is NaN, but
                // Min(5.0, NaN) is 5.0!  To fix this, we impose a total
                // ordering where NaN is smaller than every value, including
                // negative infinity.
                (Enumerable.Range(1, 10).Select(i => (float)i).Concat(Enumerable.Repeat(float.NaN, 1)).ToArray(), 10f),
                (new float[] { -1f, -10, float.NaN, 10, 200, 1000 }, 1000f),
                (new float[] { float.MinValue, 3000f, 100, 200, float.NaN, 1000 }, 3000f),
            })
            {
                yield return new object[] { new TestEnumerable<float>(array), expected };
                yield return new object[] { array, expected };
            }
        }

        [Theory]
        [MemberData(nameof(Max_Float_TestData))]
        public void Max_Float(IEnumerable<float> source, float expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Fact]
        public void Max_Float_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<float>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<float>)null).Max(i => i));
        }

        [Fact]
        public void Max_Float_EmptySource_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<float>().Max());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<float>().Max(x => x));
            Assert.Throws<InvalidOperationException>(() => ForceNotCollection(Enumerable.Empty<float>()).Max());
            Assert.Throws<InvalidOperationException>(() => ForceNotCollection(Enumerable.Empty<float>()).Max(x => x));
            Assert.Throws<InvalidOperationException>(() => Array.Empty<float>().Max());
            Assert.Throws<InvalidOperationException>(() => new List<float>().Max());
        }

        [Fact]
        public void Max_Float_SeveralNaNWithSelector()
        {
            Assert.True(float.IsNaN(Enumerable.Repeat(float.NaN, 5).Max(i => i)));
        }

        [Fact]
        public void Max_NullableFloat_SeveralNaNOrNullWithSelector()
        {
            float?[] source = new float?[] { float.NaN, null, float.NaN, null };
            Assert.True(float.IsNaN(source.Max(i => i).Value));
        }

        [Fact]
        public void Max_Float_NaNAtStartWithSelector()
        {
            float[] source = { float.NaN, 6.8f, 9.4f, 10f, 0, -5.6f };
            Assert.Equal(10f, source.Max(i => i));
        }

        public static IEnumerable<object[]> Max_Double_TestData()
        {
            foreach ((double[] array, double expected) in new[]
            {
                (new[] { 42.0 }, 42.0),
                (Enumerable.Range(1, 10).Select(i => (double)i).ToArray(), 10.0),
                (new double[] { -100, -15, -50, -10 }, -10.0),
                (new double[] { -16, 0, 50, 100, 1000 }, 1000.0),
                (new double[] { -16, 0, 50, 100, 1000 }.Concat(Enumerable.Repeat(double.MaxValue, 1)).ToArray(), double.MaxValue),

                (Enumerable.Repeat(5.5, 1).ToArray(), 5.5),
                (Enumerable.Repeat(double.NaN, 5).ToArray(), double.NaN),
                (new double[] { 112.5, 4.9, 30, 4.7, 28 }, 112.5),
                (new double[] { 6.8, 9.4, -10, 0, double.NaN, 53.6 }, 53.6),
                (new double[] { -5.5, double.PositiveInfinity, 9.9, double.PositiveInfinity }, double.PositiveInfinity),
                (new double[] { double.NaN, 6.8, 9.4, 10.5, 0, -5.6 }, 10.5),
                (new double[] { 6.8, 9.4, 10.5, 0, -5.6, double.NaN }, 10.5),
                (new double[] { double.NaN, double.NegativeInfinity }, double.NegativeInfinity),
                (new double[] { double.NegativeInfinity, double.NaN }, double.NegativeInfinity),

                // Normally NaN < anything and anything < NaN returns false
                // However, this leads to some irksome outcomes in Min and Max.
                // If we use those semantics then Min(NaN, 5.0) is NaN, but
                // Min(5.0, NaN) is 5.0!  To fix this, we impose a total
                // ordering where NaN is smaller than every value, including
                // negative infinity.
                (Enumerable.Range(1, 10).Select(i => (double)i).Concat(Enumerable.Repeat(double.NaN, 1)).ToArray(), 10.0),
                (new double[] { -1F, -10, double.NaN, 10, 200, 1000 }, 1000.0),
                (new double[] { double.MinValue, 3000F, 100, 200, double.NaN, 1000 }, 3000.0),
            })
            {
                yield return new object[] { new TestEnumerable<double>(array), expected };
                yield return new object[] { array, expected };
            }
        }

        [Theory]
        [MemberData(nameof(Max_Double_TestData))]
        public void Max_Double(IEnumerable<double> source, double expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Fact]
        public void Max_Double_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<double>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<double>)null).Max(i => i));
        }

        [Fact]
        public void Max_Double_EmptySource_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<double>().Max());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<double>().Max(x => x));
            Assert.Throws<InvalidOperationException>(() => ForceNotCollection(Enumerable.Empty<double>()).Max());
            Assert.Throws<InvalidOperationException>(() => ForceNotCollection(Enumerable.Empty<double>()).Max(x => x));
            Assert.Throws<InvalidOperationException>(() => Array.Empty<double>().Max());
            Assert.Throws<InvalidOperationException>(() => new List<double>().Max());
        }

        [Fact]
        public void Max_Double_AllNaNWithSelector()
        {
            Assert.True(double.IsNaN(Enumerable.Repeat(double.NaN, 5).Max(i => i)));
        }

        [Fact]
        public void Max_Double_SeveralNaNOrNullWithSelector()
        {
            double?[] source = new double?[] { double.NaN, null, double.NaN, null };
            Assert.True(double.IsNaN(source.Max(i => i).Value));
        }

        [Fact]
        public void Max_Double_NaNThenNegativeInfinityWithSelector()
        {
            double[] source = { double.NaN, double.NegativeInfinity };
            Assert.True(double.IsNegativeInfinity(source.Max(i => i)));
        }

        public static IEnumerable<object[]> Max_Decimal_TestData()
        {
            foreach ((decimal[] array, decimal expected) in new[]
            {
                (new[] { 42m }, 42m),
                (Enumerable.Range(1, 10).Select(i => (decimal)i).ToArray(), 10m),
                (new decimal[] { -100, -15, -50, -10 }, -10m),
                (new decimal[] { -16, 0, 50, 100, 1000 }, 1000m),
                (new decimal[] { -16, 0, 50, 100, 1000 }.Concat(Enumerable.Repeat(decimal.MaxValue, 1)).ToArray(), decimal.MaxValue),

                (new decimal[] { 5.5m }, 5.5m),
                (Enumerable.Repeat(-3.4m, 5).ToArray(), -3.4m),
                (new decimal[] { 122.5m, 4.9m, 10m, 4.7m, 28m }, 122.5m),
                (new decimal[] { 6.8m, 9.4m, 10m, 0m, 0m, decimal.MaxValue }, decimal.MaxValue),
                (new decimal[] { -5.5m, 0m, 9.9m, -5.5m, 9.9m }, 9.9m),
            })
            {
                yield return new object[] { new TestEnumerable<decimal>(array), expected };
                yield return new object[] { array, expected };
            }
        }

        [Theory]
        [MemberData(nameof(Max_Decimal_TestData))]
        public void Max_Decimal(IEnumerable<decimal> source, decimal expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Fact]
        public void Max_Decimal_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal>)null).Max(i => i));
        }

        [Fact]
        public void Max_Decimal_EmptySource_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<decimal>().Max());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<decimal>().Max(x => x));
            Assert.Throws<InvalidOperationException>(() => ForceNotCollection(Enumerable.Empty<decimal>()).Max());
            Assert.Throws<InvalidOperationException>(() => ForceNotCollection(Enumerable.Empty<decimal>()).Max(x => x));
            Assert.Throws<InvalidOperationException>(() => Array.Empty<decimal>().Max());
            Assert.Throws<InvalidOperationException>(() => new List<decimal>().Max(x => x));
        }

        public static IEnumerable<object[]> Max_NullableInt_TestData()
        {
            yield return new object[] { Enumerable.Repeat((int?)42, 1), 42 };
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (int?)i).ToArray(), 10 };
            yield return new object[] { new int?[] { null, -100, -15, -50, -10 }, -10 };
            yield return new object[] { new int?[] { null, -16, 0, 50, 100, 1000 }, 1000 };
            yield return new object[] { new int?[] { null, -16, 0, 50, 100, 1000 }.Concat(Enumerable.Repeat((int?)int.MaxValue, 1)), int.MaxValue };
            yield return new object[] { Enumerable.Repeat(default(int?), 100), null };

            yield return new object[] { Enumerable.Empty<int?>(), null };
            yield return new object[] { Enumerable.Repeat((int?)-20, 1), -20 };
            yield return new object[] { new int?[] { -6, null, -9, -10, null, -17, -18 }, -6 };
            yield return new object[] { new int?[] { null, null, null, null, null, -5 }, -5 };
            yield return new object[] { new int?[] { 6, null, null, 100, 9, 100, 10, 100 }, 100 };
            yield return new object[] { Enumerable.Repeat(default(int?), 5), null };
        }

        [Theory]
        [MemberData(nameof(Max_NullableInt_TestData))]
        public void Max_NullableInt(IEnumerable<int?> source, int? expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Theory, MemberData(nameof(Max_NullableInt_TestData))]
        public void Max_NullableIntRunOnce(IEnumerable<int?> source, int? expected)
        {
            Assert.Equal(expected, source.RunOnce().Max());
            Assert.Equal(expected, source.RunOnce().Max(x => x));
        }

        [Fact]
        public void Max_NullableInt_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int?>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int?>)null).Max(i => i));
        }

        public static IEnumerable<object[]> Max_NullableLong_TestData()
        {
            yield return new object[] { Enumerable.Repeat((long?)42, 1), 42L };
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (long?)i).ToArray(), 10L };
            yield return new object[] { new long?[] { null, -100, -15, -50, -10 }, -10L };
            yield return new object[] { new long?[] { null, -16, 0, 50, 100, 1000 }, 1000L };
            yield return new object[] { new long?[] { null, -16, 0, 50, 100, 1000 }.Concat(Enumerable.Repeat((long?)long.MaxValue, 1)), long.MaxValue };
            yield return new object[] { Enumerable.Repeat(default(long?), 100), null };

            yield return new object[] { Enumerable.Empty<long?>(), null };
            yield return new object[] { Enumerable.Repeat((long?)long.MaxValue, 1), long.MaxValue };
            yield return new object[] { Enumerable.Repeat(default(long?), 5), null };
            yield return new object[] { new long?[] { long.MaxValue, null, 9, 10, null, 7, 8 }, long.MaxValue };
            yield return new object[] { new long?[] { null, null, null, null, null, -long.MaxValue }, -long.MaxValue };
            yield return new object[] { new long?[] { -6, null, null, 0, -9, 0, -10, -30 }, 0L };
        }

        [Theory]
        [MemberData(nameof(Max_NullableLong_TestData))]
        public void Max_NullableLong(IEnumerable<long?> source, long? expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Fact]
        public void Max_NullableLong_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<long?>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<long?>)null).Max(i => i));
        }

        public static IEnumerable<object[]> Max_NullableFloat_TestData()
        {
            yield return new object[] { Enumerable.Repeat((float?)42, 1), 42f };
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (float?)i).ToArray(), 10f };
            yield return new object[] { new float?[] { null, -100, -15, -50, -10 }, -10f };
            yield return new object[] { new float?[] { null, -16, 0, 50, 100, 1000 }, 1000f };
            yield return new object[] { new float?[] { null, -16, 0, 50, 100, 1000 }.Concat(Enumerable.Repeat((float?)float.MaxValue, 1)), float.MaxValue };
            yield return new object[] { Enumerable.Repeat(default(float?), 100), null };

            yield return new object[] { Enumerable.Empty<float?>(), null };
            yield return new object[] { Enumerable.Repeat((float?)float.MinValue, 1), float.MinValue };
            yield return new object[] { Enumerable.Repeat(default(float?), 5), null };
            yield return new object[] { new float?[] { 14.50f, null, float.NaN, 10.98f, null, 7.5f, 8.6f }, 14.50f };
            yield return new object[] { new float?[] { null, null, null, null, null, 0f }, 0f };
            yield return new object[] { new float?[] { -6.4f, null, null, -0.5f, -9.4f, -0.5f, -10.9f, -0.5f }, -0.5f };

            yield return new object[] { new float?[] { float.NaN, 6.8f, 9.4f, 10f, 0, null, -5.6f }, 10f };
            yield return new object[] { new float?[] { 6.8f, 9.4f, 10f, 0, null, -5.6f, float.NaN }, 10f };
            yield return new object[] { new float?[] { float.NaN, float.NegativeInfinity }, float.NegativeInfinity };
            yield return new object[] { new float?[] { float.NegativeInfinity, float.NaN }, float.NegativeInfinity };
            yield return new object[] { Enumerable.Repeat((float?)float.NaN, 3), float.NaN };
            yield return new object[] { new float?[] { float.NaN, null, null, null }, float.NaN };
            yield return new object[] { new float?[] { null, null, null, float.NaN }, float.NaN };
            yield return new object[] { new float?[] { null, float.NaN, null }, float.NaN };
        }

        [Theory]
        [MemberData(nameof(Max_NullableFloat_TestData))]
        public void Max_NullableFloat(IEnumerable<float?> source, float? expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Fact]
        public void Max_NullableFloat_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<float?>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<float?>)null).Max(i => i));
        }

        public static IEnumerable<object[]> Max_NullableDouble_TestData()
        {
            yield return new object[] { Enumerable.Repeat((double?)42, 1), 42.0 };
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (double?)i).ToArray(), 10.0 };
            yield return new object[] { new double?[] { null, -100, -15, -50, -10 }, -10.0 };
            yield return new object[] { new double?[] { null, -16, 0, 50, 100, 1000 }, 1000.0 };
            yield return new object[] { new double?[] { null, -16, 0, 50, 100, 1000 }.Concat(Enumerable.Repeat((double?)double.MaxValue, 1)), double.MaxValue };
            yield return new object[] { Enumerable.Repeat(default(double?), 100), null };

            yield return new object[] { Enumerable.Empty<double?>(), null };
            yield return new object[] { Enumerable.Repeat((double?)double.MinValue, 1), double.MinValue };
            yield return new object[] { Enumerable.Repeat(default(double?), 5), null };
            yield return new object[] { new double?[] { 14.50, null, double.NaN, 10.98, null, 7.5, 8.6 }, 14.50 };
            yield return new object[] { new double?[] { null, null, null, null, null, 0 }, 0.0 };
            yield return new object[] { new double?[] { -6.4, null, null, -0.5, -9.4, -0.5, -10.9, -0.5 }, -0.5 };

            yield return new object[] { new double?[] { double.NaN, 6.8, 9.4, 10.5, 0, null, -5.6 }, 10.5 };
            yield return new object[] { new double?[] { 6.8, 9.4, 10.8, 0, null, -5.6, double.NaN }, 10.8 };
            yield return new object[] { new double?[] { double.NaN, double.NegativeInfinity }, double.NegativeInfinity };
            yield return new object[] { new double?[] { double.NegativeInfinity, double.NaN }, double.NegativeInfinity };
            yield return new object[] { Enumerable.Repeat((double?)double.NaN, 3), double.NaN };
            yield return new object[] { new double?[] { double.NaN, null, null, null }, double.NaN };
            yield return new object[] { new double?[] { null, null, null, double.NaN }, double.NaN };
            yield return new object[] { new double?[] { null, double.NaN, null }, double.NaN };
        }

        [Theory]
        [MemberData(nameof(Max_NullableDouble_TestData))]
        public void Max_NullableDouble(IEnumerable<double?> source, double? expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Fact]
        public void Max_NullableDouble_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<double?>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<double?>)null).Max(i => i));
        }

        public static IEnumerable<object[]> Max_NullableDecimal_TestData()
        {
            yield return new object[] { Enumerable.Repeat((decimal?)42, 1), 42m };
            yield return new object[] { Enumerable.Range(1, 10).Select(i => (decimal?)i).ToArray(), 10m };
            yield return new object[] { new decimal?[] { null, -100M, -15, -50, -10 }, -10m };
            yield return new object[] { new decimal?[] { null, -16M, 0, 50, 100, 1000 }, 1000m };
            yield return new object[] { new decimal?[] { null, -16M, 0, 50, 100, 1000 }.Concat(Enumerable.Repeat((decimal?)decimal.MaxValue, 1)), decimal.MaxValue };
            yield return new object[] { Enumerable.Repeat(default(decimal?), 100), null };

            yield return new object[] { Enumerable.Empty<decimal?>(), null };
            yield return new object[] { Enumerable.Repeat((decimal?)decimal.MaxValue, 1), decimal.MaxValue };
            yield return new object[] { Enumerable.Repeat(default(decimal?), 5), null };
            yield return new object[] { new decimal?[] { 14.50m, null, null, 10.98m, null, 7.5m, 8.6m }, 14.50m };
            yield return new object[] { new decimal?[] { null, null, null, null, null, 0m }, 0m };
            yield return new object[] { new decimal?[] { 6.4m, null, null, decimal.MaxValue, 9.4m, decimal.MaxValue, 10.9m, decimal.MaxValue }, decimal.MaxValue };
        }

        [Theory]
        [MemberData(nameof(Max_NullableDecimal_TestData))]
        public void Max_NullableDecimal(IEnumerable<decimal?> source, decimal? expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Fact]
        public void Max_NullableDecimal_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal?>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<decimal?>)null).Max(i => i));
        }

        public static IEnumerable<object[]> Max_DateTime_TestData()
        {
            yield return new object[] { Enumerable.Range(1, 10).Select(i => new DateTime(2000, 1, i)).ToArray(), new DateTime(2000, 1, 10) };
            yield return new object[] { new DateTime[] { new DateTime(2000, 12, 1), new DateTime(2000, 12, 31), new DateTime(2000, 1, 12) }, new DateTime(2000, 12, 31) };

            DateTime[] threeThousand = new DateTime[]
            {
                new DateTime(3000, 1, 1),
                new DateTime(100, 1, 1),
                new DateTime(200, 1, 1),
                new DateTime(1000, 1, 1)
            };
            yield return new object[] { threeThousand, new DateTime(3000, 1, 1) };
            yield return new object[] { threeThousand.Concat(Enumerable.Repeat(DateTime.MaxValue, 1)), DateTime.MaxValue };
        }

        [Theory]
        [MemberData(nameof(Max_DateTime_TestData))]
        public void Max_DateTime(IEnumerable<DateTime> source, DateTime expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Fact]
        public void Max_DateTime_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<DateTime>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<DateTime>)null).Max(i => i));
        }

        [Fact]
        public void Max_DateTime_EmptySource_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<DateTime>().Max());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<DateTime>().Max(i => i));
            Assert.Throws<InvalidOperationException>(() => ForceNotCollection(Enumerable.Empty<DateTime>()).Max());
            Assert.Throws<InvalidOperationException>(() => ForceNotCollection(Enumerable.Empty<DateTime>()).Max(i => i));
        }

        public static IEnumerable<object[]> Max_String_TestData()
        {
            yield return new object[] { Enumerable.Range(1, 10).Select(i => i.ToString()).ToArray(), "9" };
            yield return new object[] { new string[] { "Alice", "Bob", "Charlie", "Eve", "Mallory", "Victor", "Trent" }, "Victor" };
            yield return new object[] { new string[] { null, "Charlie", null, "Victor", "Trent", null, "Eve", "Alice", "Mallory", "Bob" }, "Victor" };

            yield return new object[] { Enumerable.Empty<string>(), null };
            yield return new object[] { Enumerable.Repeat("Hello", 1), "Hello" };
            yield return new object[] { Enumerable.Repeat("hi", 5), "hi" };
            yield return new object[] { new string[] { "zzz", "aaa", "abcd", "bark", "temp", "cat" }, "zzz" };
            yield return new object[] { new string[] { null, null, null, null, "aAa" }, "aAa" };
            yield return new object[] { new string[] { "ooo", "ccc", "ccc", "ooo", "ooo", "nnn" }, "ooo" };
            yield return new object[] { Enumerable.Repeat(default(string), 5), null };
        }

        [Theory]
        [MemberData(nameof(Max_String_TestData))]
        public void Max_String(IEnumerable<string> source, string expected)
        {
            Assert.Equal(expected, source.Max());
            Assert.Equal(expected, source.Max(x => x));
        }

        [Theory, MemberData(nameof(Max_String_TestData))]
        public void Max_StringRunOnce(IEnumerable<string> source, string expected)
        {
            Assert.Equal(expected, source.RunOnce().Max());
            Assert.Equal(expected, source.RunOnce().Max(x => x));
        }

        [Fact]
        public void Max_String_NullSource_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<string>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<string>)null).Max(i => i));
        }

        [Fact]
        public void Max_Int_NullSelector_ThrowsArgumentNullException()
        {
            Func<int, int> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<int>().Max(selector));
        }

        [Fact]
        public void Max_Int_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name="Tim", num=10 },
                new { name="John", num=-105 },
                new { name="Bob", num=30 }
            };

            Assert.Equal(30, source.Max(e => e.num));
        }

        [Fact]
        public void Max_Long_NullSelector_ThrowsArgumentNullException()
        {
            Func<long, long> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<long>().Max(selector));
        }

        [Fact]
        public void Max_Long_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name="Tim", num=10L },
                new { name="John", num=-105L },
                new { name="Bob", num=long.MaxValue }
            };
            Assert.Equal(long.MaxValue, source.Max(e => e.num));
        }

        [Fact]
        public void Max_Float_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name = "Tim", num = 40.5f },
                new { name = "John", num = -10.25f },
                new { name = "Bob", num = 100.45f }
            };

            Assert.Equal(100.45f, source.Select(e => e.num).Max());
        }

        [Fact]
        public void Max_Float_NullSelector_ThrowsArgumentNullException()
        {
            Func<float, float> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<float>().Max(selector));
        }

        [Fact]
        public void Max_Double_NullSelector_ThrowsArgumentNullException()
        {
            Func<double, double> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<double>().Max(selector));
        }

        [Fact]
        public void Max_Double_WithSelectorAccessingField()
        {
            var source = new[]
            {
                new { name="Tim", num=40.5 },
                new { name="John", num=-10.25 },
                new { name="Bob", num=100.45 }
            };
            Assert.Equal(100.45, source.Max(e => e.num));
        }

        [Fact]
        public void Max_Decimal_NullSelector_ThrowsArgumentNullException()
        {
            Func<decimal, decimal> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<decimal>().Max(selector));
        }

        [Fact]
        public void Max_Decimal_WithSelectorAccessingProperty()
        {
            var source = new[]{
                new { name="Tim", num=420.5m },
                new { name="John", num=900.25m },
                new { name="Bob", num=10.45m }
            };
            Assert.Equal(900.25m, source.Max(e => e.num));
        }

        [Fact]
        public void Max_NullableInt_NullSelector_ThrowsArgumentNullException()
        {
            Func<int?, int?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<int?>().Max(selector));
        }

        [Fact]
        public void Max_NullableInt_WithSelectorAccessingField()
        {
            var source = new[]{
                new { name="Tim", num=(int?)10 },
                new { name="John", num=(int?)-105 },
                new { name="Bob", num=(int?)null }
            };

            Assert.Equal(10, source.Max(e => e.num));
        }

        [Fact]
        public void Max_NullableLong_NullSelector_ThrowsArgumentNullException()
        {
            Func<long?, long?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<long?>().Max(selector));
        }

        [Fact]
        public void Max_NullableLong_WithSelectorAccessingField()
        {
            var source = new[]
            {
                new {name="Tim", num=default(long?) },
                new {name="John", num=(long?)-105L },
                new {name="Bob", num=(long?)long.MaxValue }
            };
            Assert.Equal(long.MaxValue, source.Max(e => e.num));
        }

        [Fact]
        public void Max_NullableFloat_NullSelector_ThrowsArgumentNullException()
        {
            Func<float?, float?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<float?>().Max(selector));
        }

        [Fact]
        public void Max_NullableFloat_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name="Tim", num=(float?)40.5f },
                new { name="John", num=(float?)null },
                new { name="Bob", num=(float?)100.45f }
            };
            Assert.Equal(100.45f, source.Max(e => e.num));
        }

        [Fact]
        public void Max_NullableDouble_NullSelector_ThrowsArgumentNullException()
        {
            Func<double?, double?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<double?>().Max(selector));
        }

        [Fact]
        public void Max_NullableDouble_WithSelectorAccessingProperty()
        {
            var source = new []
            {
                new { name = "Tim", num = (double?)40.5},
                new { name = "John", num = default(double?)},
                new { name = "Bob", num = (double?)100.45}
            };
            Assert.Equal(100.45, source.Max(e => e.num));
        }

        [Fact]
        public void Max_NullableDecimal_NullSelector_ThrowsArgumentNullException()
        {
            Func<decimal?, decimal?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<decimal?>().Max(selector));
        }

        [Fact]
        public void Max_NullableDecimal_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name="Tim", num=(decimal?)420.5m },
                new { name="John", num=default(decimal?) },
                new { name="Bob", num=(decimal?)10.45m }
            };
            Assert.Equal(420.5m, source.Max(e => e.num));
        }

        [Fact]
        public void Max_NullableDateTime_EmptySourceWithSelector()
        {
            Assert.Null(Enumerable.Empty<DateTime?>().Max(x => x));
        }

        [Fact]
        public void Max_NullableDateTime_NullSelector_ThrowsArgumentNullException()
        {
            Func<DateTime?, DateTime?> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<DateTime?>().Max(selector));
        }

        [Fact]
        public void Max_String_NullSelector_ThrowsArgumentNullException()
        {
            Func<string, string> selector = null;
            AssertExtensions.Throws<ArgumentNullException>("selector", () => Enumerable.Empty<string>().Max(selector));
        }

        [Fact]
        public void Max_String_WithSelectorAccessingProperty()
        {
            var source = new[]
            {
                new { name="Tim", num=420.5m },
                new { name="John", num=900.25m },
                new { name="Bob", num=10.45m }
            };
            Assert.Equal("Tim", source.Max(e => e.name));
        }

        [Fact]
        public void Max_Boolean_EmptySource_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<bool>().Max());
            Assert.Throws<InvalidOperationException>(() => ForceNotCollection(Enumerable.Empty<bool>()).Max());
        }

        [Fact]
        public static void Max_Generic_NullSource_ThrowsArgumentNullException()
        {
            IEnumerable<int> source = null;

            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Max());
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Max(comparer: null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.Max(Comparer<int>.Create((_, _) => 0)));
        }

        [Fact]
        public static void Max_Generic_EmptyStructSource_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Max());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Max(comparer: null));
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Max(Comparer<int>.Create((_,_) => 0)));
        }

        [Theory]
        [MemberData(nameof(Max_Generic_TestData))]
        public static void Max_Generic_HasExpectedOutput<TSource>(IEnumerable<TSource> source, IComparer<TSource>? comparer, TSource? expected)
        {
            Assert.Equal(expected, source.Max(comparer));
        }

        [Theory]
        [MemberData(nameof(Max_Generic_TestData))]
        public static void Max_Generic_RunOnce_HasExpectedOutput<TSource>(IEnumerable<TSource> source, IComparer<TSource>? comparer, TSource? expected)
        {
            Assert.Equal(expected, source.RunOnce().Max(comparer));
        }

        public static IEnumerable<object[]> Max_Generic_TestData()
        {
            yield return WrapArgs(
                source: Enumerable.Empty<int?>(),
                comparer: null,
                expected: null);

            yield return WrapArgs(
                source: Enumerable.Empty<int?>(),
                comparer: Comparer<int?>.Create((_,_) => 0),
                expected: null);

            yield return WrapArgs(
                source: Enumerable.Range(0, 10),
                comparer: null,
                expected: 9);

            yield return WrapArgs(
                source: Enumerable.Range(0, 10),
                comparer: Comparer<int>.Create((x, y) => -x.CompareTo(y)),
                expected: 0);

            yield return WrapArgs(
                source: Enumerable.Range(0, 10),
                comparer: Comparer<int>.Create((x,y) => 0),
                expected: 0);

            yield return WrapArgs(
                source: new string[] { "Aardvark", "Zyzzyva", "Zebra", "Antelope" },
                comparer: null,
                expected: "Zyzzyva");

            yield return WrapArgs(
                source: new string[] { "Aardvark", "Zyzzyva", "Zebra", "Antelope" },
                comparer: Comparer<string>.Create((x, y) => -x.CompareTo(y)),
                expected: "Aardvark");

            object[] WrapArgs<TSource>(IEnumerable<TSource> source, IComparer<TSource>? comparer, TSource? expected)
                => new object[] { source, comparer, expected };
        }

        [Fact]
        public static void MaxBy_Generic_NullSource_ThrowsArgumentNullException()
        {
            IEnumerable<int> source = null;

            AssertExtensions.Throws<ArgumentNullException>("source", () => source.MaxBy(x => x));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.MaxBy(x => x, comparer: null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => source.MaxBy(x => x, Comparer<int>.Create((_, _) => 0)));
        }

        [Fact]
        public static void MaxBy_Generic_NullKeySelector_ThrowsArgumentNullException()
        {
            IEnumerable<int> source = Enumerable.Empty<int>();
            Func<int, int> keySelector = null;

            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.MaxBy(keySelector));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.MaxBy(keySelector, comparer: null));
            AssertExtensions.Throws<ArgumentNullException>("keySelector", () => source.MaxBy(keySelector, Comparer<int>.Create((_, _) => 0)));
        }

        [Fact]
        public static void MaxBy_Generic_EmptyStructSource_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().MaxBy(x => x.ToString()));
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().MaxBy(x => x.ToString(), comparer: null));
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().MaxBy(x => x.ToString(), Comparer<string>.Create((_, _) => 0)));
        }

        [Fact]
        public static void MaxBy_Generic_EmptyNullableSource_ReturnsNull()
        {
            Assert.Null(Enumerable.Empty<int?>().MaxBy(x => x.GetHashCode()));
            Assert.Null(Enumerable.Empty<int?>().MaxBy(x => x.GetHashCode(), comparer: null));
            Assert.Null(Enumerable.Empty<int?>().MaxBy(x => x.GetHashCode(), Comparer<int>.Create((_, _) => 0)));
        }

        [Fact]
        public static void MaxBy_Generic_EmptyReferenceSource_ReturnsNull()
        {
            Assert.Null(Enumerable.Empty<string>().MaxBy(x => x.GetHashCode()));
            Assert.Null(Enumerable.Empty<string>().MaxBy(x => x.GetHashCode(), comparer: null));
            Assert.Null(Enumerable.Empty<string>().MaxBy(x => x.GetHashCode(), Comparer<int>.Create((_, _) => 0)));
        }

        [Fact]
        public static void MaxBy_Generic_StructSourceAllKeysAreNull_ReturnsFirstElement()
        {
            Assert.Equal(0, Enumerable.Range(0, 5).MaxBy(x => default(string)));
            Assert.Equal(0, Enumerable.Range(0, 5).MaxBy(x => default(string), comparer: null));
            Assert.Equal(0, Enumerable.Range(0, 5).MaxBy(x => default(string), Comparer<string>.Create((_, _) => throw new InvalidOperationException("comparer should not be called."))));
        }

        [Fact]
        public static void MaxBy_Generic_NullableSourceAllKeysAreNull_ReturnsFirstElement()
        {
            Assert.Equal(0, Enumerable.Range(0, 5).Cast<int?>().MaxBy(x => default(int?)));
            Assert.Equal(0, Enumerable.Range(0, 5).Cast<int?>().MaxBy(x => default(int?), comparer: null));
            Assert.Equal(0, Enumerable.Range(0, 5).Cast<int?>().MaxBy(x => default(int?), Comparer<int?>.Create((_, _) => throw new InvalidOperationException("comparer should not be called."))));
        }

        [Fact]
        public static void MaxBy_Generic_ReferenceSourceAllKeysAreNull_ReturnsFirstElement()
        {
            Assert.Equal("0", Enumerable.Range(0, 5).Select(x => x.ToString()).MaxBy(x => default(string)));
            Assert.Equal("0", Enumerable.Range(0, 5).Select(x => x.ToString()).MaxBy(x => default(string), comparer: null));
            Assert.Equal("0", Enumerable.Range(0, 5).Select(x => x.ToString()).MaxBy(x => default(string), Comparer<string>.Create((_, _) => throw new InvalidOperationException("comparer should not be called."))));
        }

        [Theory]
        [MemberData(nameof(MaxBy_Generic_TestData))]
        public static void MaxBy_Generic_HasExpectedOutput<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer, TSource? expected)
        {
            Assert.Equal(expected, source.MaxBy(keySelector, comparer));
        }

        [Theory]
        [MemberData(nameof(MaxBy_Generic_TestData))]
        public static void MaxBy_Generic_RunOnce_HasExpectedOutput<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer, TSource? expected)
        {
            Assert.Equal(expected, source.RunOnce().MaxBy(keySelector, comparer));
        }

        public static IEnumerable<object[]> MaxBy_Generic_TestData()
        {
            yield return WrapArgs(
                source: Enumerable.Empty<int?>(),
                keySelector: x => x,
                comparer: null,
                expected: null);

            yield return WrapArgs(
                source: Enumerable.Empty<int?>(),
                keySelector: x => x,
                comparer: Comparer<int?>.Create((_, _) => 0),
                expected: null);

            yield return WrapArgs(
                source: Enumerable.Range(0, 10),
                keySelector: x => x,
                comparer: null,
                expected: 9);

            yield return WrapArgs(
                source: Enumerable.Range(0, 10),
                keySelector: x => x,
                comparer: Comparer<int>.Create((x, y) => -x.CompareTo(y)),
                expected: 0);

            yield return WrapArgs(
                source: Enumerable.Range(0, 10),
                keySelector: x => x,
                comparer: Comparer<int>.Create((x, y) => 0),
                expected: 0);

            yield return WrapArgs(
                source: new string[] { "Aardvark", "Zyzzyva", "Zebra", "Antelope" },
                keySelector: x => x,
                comparer: null,
                expected: "Zyzzyva");

            yield return WrapArgs(
                source: new string[] { "Aardvark", "Zyzzyva", "Zebra", "Antelope" },
                keySelector: x => x,
                comparer: Comparer<string>.Create((x, y) => -x.CompareTo(y)),
                expected: "Aardvark");

            yield return WrapArgs(
                source: new (string Name, int Age) [] { ("Tom", 43), ("Dick", 55), ("Harry", 20) },
                keySelector: x => x.Age,
                comparer: null,
                expected: (Name: "Dick", Age: 55));

            yield return WrapArgs(
                source: new (string Name, int Age)[] { ("Tom", 43), ("Dick", 55), ("Harry", 20) },
                keySelector: x => x.Age,
                comparer: Comparer<int>.Create((x, y) => -x.CompareTo(y)),
                expected: (Name: "Harry", Age: 20));

            yield return WrapArgs(
                source: new (string Name, int Age)[] { ("Tom", 43), ("Dick", 55), ("Harry", 20) },
                keySelector: x => x.Name,
                comparer: null,
                expected: (Name: "Tom", Age: 43));

            yield return WrapArgs(
                source: new (string Name, int Age)[] { ("Tom", 43), ("Dick", 55), ("Harry", 20) },
                keySelector: x => x.Name,
                comparer: Comparer<string>.Create((x, y) => -x.CompareTo(y)),
                expected: (Name: "Dick", Age: 55));

            yield return WrapArgs(
                source: new (string Name, int Age)[] { ("Tom", 43), (null, 55), ("Harry", 20) },
                keySelector: x => x.Name,
                comparer: Comparer<string>.Create((x, y) => -x.CompareTo(y)),
                expected: (Name: "Harry", Age: 20));

            object[] WrapArgs<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer, TSource? expected)
                => new object[] { source, keySelector, comparer, expected };
        }
    }
}
