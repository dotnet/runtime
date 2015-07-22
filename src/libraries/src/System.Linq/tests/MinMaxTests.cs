// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class MinMaxTests
    {
        [Fact]
        public void MinInt32()
        {
            var one = Enumerable.Range(1, 10).ToArray();
            var minusTen = new[] { -1, -10, 10, 200, 1000 };
            var hundred = new[] { 3000, 100, 200, 1000 };
            Assert.Equal(42, Enumerable.Repeat(42, 1).Min());
            Assert.Equal(1, one.Min());
            Assert.Equal(-10, minusTen.Min());
            Assert.Equal(100, hundred.Min());
            Assert.Equal(int.MinValue, one.Concat(Enumerable.Repeat(int.MinValue, 1)).Min());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Min());
        }

        [Fact]
        public void MaxInt32()
        {
            var ten = Enumerable.Range(1, 10).ToArray();
            var minusTen = new[] { -100, -15, -50, -10 };
            var thousand = new[] { -16, 0, 50, 100, 1000 };
            Assert.Equal(42, Enumerable.Repeat(42, 1).Max());
            Assert.Equal(10, ten.Max());
            Assert.Equal(-10, minusTen.Max());
            Assert.Equal(1000, thousand.Max());
            Assert.Equal(int.MaxValue, thousand.Concat(Enumerable.Repeat(int.MaxValue, 1)).Max());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Max());
        }

        [Fact]
        public void MinInt64()
        {
            var one = Enumerable.Range(1, 10).Select(i => (long)i).ToArray();
            var minusTen = new[] { -1L, -10, 10, 200, 1000 };
            var hundred = new[] { 3000L, 100, 200, 1000 };
            Assert.Equal(42, Enumerable.Repeat(42L, 1).Min());
            Assert.Equal(1, one.Min());
            Assert.Equal(-10, minusTen.Min());
            Assert.Equal(100, hundred.Min());
            Assert.Equal(long.MinValue, one.Concat(Enumerable.Repeat(long.MinValue, 1)).Min());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<long>().Min());
        }

        [Fact]
        public void MaxInt64()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (long)i).ToArray();
            var minusTen = new[] { -100L, -15, -50, -10 };
            var thousand = new[] { -16L, 0, 50, 100, 1000 };
            Assert.Equal(42, Enumerable.Repeat(42L, 1).Max());
            Assert.Equal(10, ten.Max());
            Assert.Equal(-10, minusTen.Max());
            Assert.Equal(1000, thousand.Max());
            Assert.Equal(long.MaxValue, thousand.Concat(Enumerable.Repeat(long.MaxValue, 1)).Max());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<long>().Max());
        }

        [Fact]
        public void MinSingle()
        {
            var one = Enumerable.Range(1, 10).Select(i => (float)i).ToArray();
            var minusTen = new[] { -1F, -10, 10, 200, 1000 };
            var hundred = new[] { 3000F, 100, 200, 1000 };
            Assert.Equal(42F, Enumerable.Repeat(42F, 1).Min());
            Assert.Equal(1L, one.Min());
            Assert.Equal(-10L, minusTen.Min());
            Assert.Equal(100L, hundred.Min());
            Assert.Equal(float.MinValue, one.Concat(Enumerable.Repeat(float.MinValue, 1)).Min());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<float>().Min());
        }

        [Fact]
        public void MaxSingle()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (float)i).ToArray();
            var minusTen = new[] { -100F, -15, -50, -10 };
            var thousand = new[] { -16F, 0, 50, 100, 1000 };
            Assert.Equal(42F, Enumerable.Repeat(42F, 1).Max());
            Assert.Equal(10F, ten.Max());
            Assert.Equal(-10F, minusTen.Max());
            Assert.Equal(1000F, thousand.Max());
            Assert.Equal(float.MaxValue, thousand.Concat(Enumerable.Repeat(float.MaxValue, 1)).Max());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<float>().Max());
        }

        [Fact]
        public void MinDouble()
        {
            var one = Enumerable.Range(1, 10).Select(i => (double)i).ToArray();
            var minusTen = new[] { -1D, -10, 10, 200, 1000 };
            var hundred = new[] { 3000D, 100, 200, 1000 };
            Assert.Equal(42D, Enumerable.Repeat(42D, 1).Min());
            Assert.Equal(1D, one.Min());
            Assert.Equal(-10D, minusTen.Min());
            Assert.Equal(100D, hundred.Min());
            Assert.Equal(double.MinValue, one.Concat(Enumerable.Repeat(double.MinValue, 1)).Min());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<double>().Min());
        }

        [Fact]
        public void MaxDouble()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (double)i).ToArray();
            var minusTen = new[] { -100D, -15, -50, -10 };
            var thousand = new[] { -16D, 0, 50, 100, 1000 };
            Assert.Equal(42D, Enumerable.Repeat(42D, 1).Max());
            Assert.Equal(10D, ten.Max());
            Assert.Equal(-10D, minusTen.Max());
            Assert.Equal(1000D, thousand.Max());
            Assert.Equal(double.MaxValue, thousand.Concat(Enumerable.Repeat(double.MaxValue, 1)).Max());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<double>().Max());
        }

        [Fact]
        public void MinDecimal()
        {
            var one = Enumerable.Range(1, 10).Select(i => (decimal)i).ToArray();
            var minusTen = new[] { -1M, -10, 10, 200, 1000 };
            var hundred = new[] { 3000M, 100, 200, 1000 };
            Assert.Equal(42M, Enumerable.Repeat(42M, 1).Max());
            Assert.Equal(1M, one.Min());
            Assert.Equal(-10M, minusTen.Min());
            Assert.Equal(100M, hundred.Min());
            Assert.Equal(decimal.MinValue, one.Concat(Enumerable.Repeat(decimal.MinValue, 1)).Min());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<decimal>().Min());
        }

        [Fact]
        public void MaxDecimal()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (decimal)i).ToArray();
            var minusTen = new[] { -100M, -15, -50, -10 };
            var thousand = new[] { -16M, 0, 50, 100, 1000 };
            Assert.Equal(42M, Enumerable.Repeat(42M, 1).Max());
            Assert.Equal(10M, ten.Max());
            Assert.Equal(-10M, minusTen.Max());
            Assert.Equal(1000M, thousand.Max());
            Assert.Equal(decimal.MaxValue, thousand.Concat(Enumerable.Repeat(decimal.MaxValue, 1)).Max());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<decimal>().Max());
        }

        [Fact]
        public void MinNullableInt32()
        {
            var one = Enumerable.Range(1, 10).Select(i => (int?)i).ToArray();
            var minusTen = new[] { default(int?), -1, -10, 10, 200, 1000 };
            var hundred = new[] { default(int?), 3000, 100, 200, 1000 };
            Assert.Equal(42, Enumerable.Repeat((int?)42, 1).Min());
            Assert.Equal(1, one.Min());
            Assert.Equal(-10, minusTen.Min());
            Assert.Equal(100, hundred.Min());
            Assert.Equal(null, Enumerable.Empty<int?>().Min());
            Assert.Equal(int.MinValue, one.Concat(Enumerable.Repeat((int?)int.MinValue, 1)).Min());
            Assert.Equal(null, Enumerable.Repeat(default(int?), 100).Min());
        }

        [Fact]
        public void MaxNullableInt32()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (int?)i).ToArray();
            var minusTen = new[] { default(int?), -100, -15, -50, -10 };
            var thousand = new[] { default(int?), -16, 0, 50, 100, 1000 };
            Assert.Equal(42, Enumerable.Repeat((int?)42, 1).Max());
            Assert.Equal(10, ten.Max());
            Assert.Equal(-10, minusTen.Max());
            Assert.Equal(1000, thousand.Max());
            Assert.Equal(int.MaxValue, thousand.Concat(Enumerable.Repeat((int?)int.MaxValue, 1)).Max());
            Assert.Equal(null, Enumerable.Empty<int?>().Max());
            Assert.Equal(null, Enumerable.Repeat(default(int?), 100).Max());
        }

        [Fact]
        public void MinNullableInt64()
        {
            var one = Enumerable.Range(1, 10).Select(i => (long?)i).ToArray();
            var minusTen = new[] { default(long?), -1L, -10, 10, 200, 1000 };
            var hundred = new[] { default(long?), 3000L, 100, 200, 1000 };
            Assert.Equal(42, Enumerable.Repeat((long?)42, 1).Min());
            Assert.Equal(1, one.Min());
            Assert.Equal(-10, minusTen.Min());
            Assert.Equal(100, hundred.Min());
            Assert.Equal(null, Enumerable.Empty<long?>().Min());
            Assert.Equal(long.MinValue, one.Concat(Enumerable.Repeat((long?)long.MinValue, 1)).Min());
            Assert.Equal(null, Enumerable.Repeat(default(long?), 100).Min());
        }

        [Fact]
        public void MaxNullableInt64()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (long?)i).ToArray();
            var minusTen = new[] { default(long?), -100L, -15, -50, -10 };
            var thousand = new[] { default(long?), -16L, 0, 50, 100, 1000 };
            Assert.Equal(42, Enumerable.Repeat((long?)42, 1).Max());
            Assert.Equal(10, ten.Max());
            Assert.Equal(-10, minusTen.Max());
            Assert.Equal(1000, thousand.Max());
            Assert.Equal(long.MaxValue, thousand.Concat(Enumerable.Repeat((long?)long.MaxValue, 1)).Max());
            Assert.Equal(null, Enumerable.Empty<long?>().Max());
            Assert.Equal(null, Enumerable.Repeat(default(long?), 100).Max());
        }

        [Fact]
        public void MinNullableSingle()
        {
            var one = Enumerable.Range(1, 10).Select(i => (float?)i).ToArray();
            var minusTen = new[] { default(float?), -1F, -10, 10, 200, 1000 };
            var hundred = new[] { default(float?), 3000F, 100, 200, 1000 };
            Assert.Equal(42F, Enumerable.Repeat((float?)42, 1).Min());
            Assert.Equal(1F, one.Min());
            Assert.Equal(-10F, minusTen.Min());
            Assert.Equal(100F, hundred.Min());
            Assert.Equal(null, Enumerable.Empty<float?>().Min());
            Assert.Equal(float.MinValue, one.Concat(Enumerable.Repeat((float?)float.MinValue, 1)).Min());
            Assert.Equal(null, Enumerable.Repeat(default(float?), 100).Min());
        }

        [Fact]
        public void MaxNullableSingle()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (float?)i).ToArray();
            var minusTen = new[] { default(float?), -100F, -15, -50, -10 };
            var thousand = new[] { default(float?), -16F, 0, 50, 100, 1000 };
            Assert.Equal(42F, Enumerable.Repeat((float?)42, 1).Max());
            Assert.Equal(10F, ten.Max());
            Assert.Equal(-10F, minusTen.Max());
            Assert.Equal(1000F, thousand.Max());
            Assert.Equal(float.MaxValue, thousand.Concat(Enumerable.Repeat((float?)float.MaxValue, 1)).Max());
            Assert.Equal(null, Enumerable.Empty<float?>().Max());
            Assert.Equal(null, Enumerable.Repeat(default(float?), 100).Max());
        }

        [Fact]
        public void MinNullableDouble()
        {
            var one = Enumerable.Range(1, 10).Select(i => (double?)i).ToArray();
            var minusTen = new[] { default(double?), -1D, -10, 10, 200, 1000 };
            var hundred = new[] { default(double?), 3000D, 100, 200, 1000 };
            Assert.Equal(42D, Enumerable.Repeat((double?)42, 1).Min());
            Assert.Equal(1D, one.Min());
            Assert.Equal(-10D, minusTen.Min());
            Assert.Equal(100D, hundred.Min());
            Assert.Equal(double.MinValue, one.Concat(Enumerable.Repeat((double?)double.MinValue, 1)).Min());
            Assert.Equal(null, Enumerable.Empty<double?>().Min());
            Assert.Equal(null, Enumerable.Repeat(default(double?), 100).Min());
        }

        [Fact]
        public void MaxNullableDouble()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (double?)i).ToArray();
            var minusTen = new[] { default(double?), -100D, -15, -50, -10 };
            var thousand = new[] { default(double?), -16D, 0, 50, 100, 1000 };
            Assert.Equal(42D, Enumerable.Repeat((double?)42, 1).Max());
            Assert.Equal(10D, ten.Max());
            Assert.Equal(-10D, minusTen.Max());
            Assert.Equal(1000D, thousand.Max());
            Assert.Equal(double.MaxValue, thousand.Concat(Enumerable.Repeat((double?)double.MaxValue, 1)).Max());
            Assert.Equal(null, Enumerable.Empty<double?>().Max());
            Assert.Equal(null, Enumerable.Repeat(default(double?), 100).Max());
        }

        [Fact]
        public void MinNullableDecimal()
        {
            var one = Enumerable.Range(1, 10).Select(i => (decimal?)i).ToArray();
            var minusTen = new[] { default(decimal?), -1M, -10, 10, 200, 1000 };
            var hundred = new[] { default(decimal?), 3000M, 100, 200, 1000 };
            Assert.Equal(42M, Enumerable.Repeat((decimal?)42, 1).Min());
            Assert.Equal(1M, one.Min());
            Assert.Equal(-10M, minusTen.Min());
            Assert.Equal(100M, hundred.Min());
            Assert.Equal(decimal.MinValue, one.Concat(Enumerable.Repeat((decimal?)decimal.MinValue, 1)).Min());
            Assert.Equal(null, Enumerable.Empty<decimal?>().Min());
            Assert.Equal(null, Enumerable.Repeat(default(decimal?), 100).Min());
        }

        [Fact]
        public void MaxNullableDecimal()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (decimal?)i).ToArray();
            var minusTen = new[] { default(decimal?), -100M, -15, -50, -10 };
            var thousand = new[] { default(decimal?), -16M, 0, 50, 100, 1000 };
            Assert.Equal(42M, Enumerable.Repeat((decimal?)42, 1).Max());
            Assert.Equal(10M, ten.Max());
            Assert.Equal(-10M, minusTen.Max());
            Assert.Equal(1000M, thousand.Max());
            Assert.Equal(decimal.MaxValue, thousand.Concat(Enumerable.Repeat((decimal?)decimal.MaxValue, 1)).Max());
            Assert.Equal(null, Enumerable.Empty<decimal?>().Max());
            Assert.Equal(null, Enumerable.Repeat(default(decimal?), 100).Max());
        }

        // Normally NaN < anything is false, as is anything < NaN
        // However, this leads to some irksome outcomes in Min and Max.
        // If we use those semantics then Min(NaN, 5.0) is NaN, but
        // Min(5.0, NaN) is 5.0!  To fix this, we impose a total
        // ordering where NaN is smaller than every value, including
        // negative infinity.
        // This behaviour must be confirmed as happening.
        // Note that further to this, null is taken as not being in a sequence at all
        // (Null will be returned if the only element in a sequence, but this will also
        // happen with an empty sequence).

        [Fact]
        public void NaNFirstSingle()
        {
            var nanThenOne = Enumerable.Range(1, 10).Select(i => (float)i).Concat(Enumerable.Repeat(float.NaN, 1)).ToArray();
            var nanThenMinusTen = new[] { -1F, -10, float.NaN, 10, 200, 1000 };
            var nanThenMinValue = new[] { float.MinValue, 3000F, 100, 200, float.NaN, 1000 };
            Assert.True(float.IsNaN(nanThenOne.Min()));
            Assert.True(float.IsNaN(nanThenMinusTen.Min()));
            Assert.True(float.IsNaN(nanThenMinValue.Min()));
            Assert.False(float.IsNaN(nanThenOne.Max()));
            Assert.False(float.IsNaN(nanThenMinusTen.Max()));
            Assert.False(float.IsNaN(nanThenMinValue.Max()));
            var nanWithNull = new[] { default(float?), float.NaN, default(float?) };
            Assert.True(float.IsNaN(nanWithNull.Min().Value));
            Assert.True(float.IsNaN(nanWithNull.Max().Value));
        }

        [Fact]
        public void NaNFirstDouble()
        {
            var nanThenOne = Enumerable.Range(1, 10).Select(i => (double)i).Concat(Enumerable.Repeat(double.NaN, 1)).ToArray();
            var nanThenMinusTen = new[] { -1F, -10, double.NaN, 10, 200, 1000 };
            var nanThenMinValue = new[] { double.MinValue, 3000F, 100, 200, double.NaN, 1000 };
            Assert.True(double.IsNaN(nanThenOne.Min()));
            Assert.True(double.IsNaN(nanThenMinusTen.Min()));
            Assert.True(double.IsNaN(nanThenMinValue.Min()));
            Assert.False(double.IsNaN(nanThenOne.Max()));
            Assert.False(double.IsNaN(nanThenMinusTen.Max()));
            Assert.False(double.IsNaN(nanThenMinValue.Max()));
            var nanWithNull = new[] { default(double?), double.NaN, default(double?) };
            Assert.True(double.IsNaN(nanWithNull.Min().Value));
            Assert.True(double.IsNaN(nanWithNull.Max().Value));
        }

        [Fact]
        public void MinDateTime()
        {
            var one = Enumerable.Range(1, 10).Select(i => new DateTime(2000, 1, i)).ToArray();
            var newYears = new[]
            {
                new DateTime(2000, 12, 1),
                new DateTime(2000, 1, 1),
                new DateTime(2000, 1, 12)
            };
            var hundred = new[]
            {
                new DateTime(3000, 1, 1),
                new DateTime(100, 1, 1),
                new DateTime(200, 1, 1),
                new DateTime(1000, 1, 1)
            };
            Assert.Equal(DateTime.MinValue, one.Concat(Enumerable.Repeat(DateTime.MinValue, 1)).Min());
            Assert.Equal(new DateTime(2000, 1, 1), one.Min());
            Assert.Equal(new DateTime(2000, 1, 1), newYears.Min());
            Assert.Equal(new DateTime(100, 1, 1), hundred.Min());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<DateTime>().Min());
        }

        [Fact]
        public void MaxDateTime()
        {
            var ten = Enumerable.Range(1, 10).Select(i => new DateTime(2000, 1, i)).ToArray();
            var newYearsEve = new[]
            {
                new DateTime(2000, 12, 1),
                new DateTime(2000, 12, 31),
                new DateTime(2000, 1, 12)
            };
            var threeThousand = new[]
            {
                new DateTime(3000, 1, 1),
                new DateTime(100, 1, 1),
                new DateTime(200, 1, 1),
                new DateTime(1000, 1, 1)
            };
            Assert.Equal(new DateTime(2000, 1, 10), ten.Max());
            Assert.Equal(new DateTime(2000, 12, 31), newYearsEve.Max());
            Assert.Equal(new DateTime(3000, 1, 1), threeThousand.Max());
            Assert.Equal(DateTime.MaxValue, threeThousand.Concat(Enumerable.Repeat(DateTime.MaxValue, 1)).Max());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<DateTime>().Max());
        }

        [Fact]
        public void MinString()
        {
            var one = Enumerable.Range(1, 10).Select(i => i.ToString()).ToArray();
            var agents = new[] { "Alice", "Bob", "Charlie", "Eve", "Mallory", "Trent", "Victor"};
            var confusedAgents = new[] { null, "Charlie", null, "Victor", "Trent", null, "Eve", "Alice", "Mallory", "Bob" };
            Assert.Equal("1", one.Min());
            Assert.Equal("Alice", agents.Min());
            Assert.Equal("Alice", confusedAgents.Min());
            Assert.Null(Enumerable.Empty<string>().Min());
        }

        [Fact]
        public void MaxString()
        {
            var nine = Enumerable.Range(1, 10).Select(i => i.ToString()).ToArray();
            var agents = new[] { "Alice", "Bob", "Charlie", "Eve", "Mallory", "Victor", "Trent" };
            var confusedAgents = new[] { null, "Charlie", null, "Victor", "Trent", null, "Eve", "Alice", "Mallory", "Bob" };
            Assert.Equal("9", nine.Max());
            Assert.Equal("Victor", agents.Max());
            Assert.Equal("Victor", confusedAgents.Max());
            Assert.Null(Enumerable.Empty<string>().Max());
        }
        [Fact]

        public void MinInt32WithSelector()
        {
            var one = Enumerable.Range(1, 10).ToArray();
            var minusTen = new[] { -1, -10, 10, 200, 1000 };
            var hundred = new[] { 3000, 100, 200, 1000 };
            Assert.Equal(42, Enumerable.Repeat(42, 1).Min(x => x));
            Assert.Equal(1, one.Min(x => x));
            Assert.Equal(-10, minusTen.Min(x => x));
            Assert.Equal(100, hundred.Min(x => x));
            Assert.Equal(int.MinValue, one.Concat(Enumerable.Repeat(int.MinValue, 1)).Min(x => x));
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Min(x => x));
        }

        [Fact]
        public void MaxInt32WithSelector()
        {
            var ten = Enumerable.Range(1, 10).ToArray();
            var minusTen = new[] { -100, -15, -50, -10 };
            var thousand = new[] { -16, 0, 50, 100, 1000 };
            Assert.Equal(42, Enumerable.Repeat(42, 1).Max(x => x));
            Assert.Equal(10, ten.Max(x => x));
            Assert.Equal(-10, minusTen.Max(x => x));
            Assert.Equal(1000, thousand.Max(x => x));
            Assert.Equal(int.MaxValue, thousand.Concat(Enumerable.Repeat(int.MaxValue, 1)).Max(x => x));
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<int>().Max(x => x));
        }

        [Fact]
        public void MinInt64WithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => (long)i).ToArray();
            var minusTen = new[] { -1L, -10, 10, 200, 1000 };
            var hundred = new[] { 3000L, 100, 200, 1000 };
            Assert.Equal(42, Enumerable.Repeat(42L, 1).Min(x => x));
            Assert.Equal(1, one.Min(x => x));
            Assert.Equal(-10, minusTen.Min(x => x));
            Assert.Equal(100, hundred.Min(x => x));
            Assert.Equal(long.MinValue, one.Concat(Enumerable.Repeat(long.MinValue, 1)).Min(x => x));
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<long>().Min(x => x));
        }

        [Fact]
        public void MaxInt64WithSelector()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (long)i).ToArray();
            var minusTen = new[] { -100L, -15, -50, -10 };
            var thousand = new[] { -16L, 0, 50, 100, 1000 };
            Assert.Equal(42, Enumerable.Repeat(42L, 1).Max(x => x));
            Assert.Equal(10, ten.Max(x => x));
            Assert.Equal(-10, minusTen.Max(x => x));
            Assert.Equal(1000, thousand.Max(x => x));
            Assert.Equal(long.MaxValue, thousand.Concat(Enumerable.Repeat(long.MaxValue, 1)).Max(x => x));
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<long>().Max(x => x));
        }

        [Fact]
        public void MinSingleWithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => (float)i).ToArray();
            var minusTen = new[] { -1F, -10, 10, 200, 1000 };
            var hundred = new[] { 3000F, 100, 200, 1000 };
            Assert.Equal(42F, Enumerable.Repeat(42F, 1).Min(x => x));
            Assert.Equal(1L, one.Min(x => x));
            Assert.Equal(-10L, minusTen.Min(x => x));
            Assert.Equal(100L, hundred.Min(x => x));
            Assert.Equal(float.MinValue, one.Concat(Enumerable.Repeat(float.MinValue, 1)).Min(x => x));
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<float>().Min(x => x));
        }

        [Fact]
        public void MaxSingleWithSelector()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (float)i).ToArray();
            var minusTen = new[] { -100F, -15, -50, -10 };
            var thousand = new[] { -16F, 0, 50, 100, 1000 };
            Assert.Equal(42F, Enumerable.Repeat(42F, 1).Max(x => x));
            Assert.Equal(10F, ten.Max(x => x));
            Assert.Equal(-10F, minusTen.Max(x => x));
            Assert.Equal(1000F, thousand.Max(x => x));
            Assert.Equal(float.MaxValue, thousand.Concat(Enumerable.Repeat(float.MaxValue, 1)).Max(x => x));
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<float>().Max(x => x));
        }

        [Fact]
        public void MinDoubleWithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => (double)i).ToArray();
            var minusTen = new[] { -1D, -10, 10, 200, 1000 };
            var hundred = new[] { 3000D, 100, 200, 1000 };
            Assert.Equal(42D, Enumerable.Repeat(42D, 1).Min(x => x));
            Assert.Equal(1D, one.Min(x => x));
            Assert.Equal(-10D, minusTen.Min(x => x));
            Assert.Equal(100D, hundred.Min(x => x));
            Assert.Equal(double.MinValue, one.Concat(Enumerable.Repeat(double.MinValue, 1)).Min(x => x));
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<double>().Min(x => x));
        }

        [Fact]
        public void MaxDoubleWithSelector()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (double)i).ToArray();
            var minusTen = new[] { -100D, -15, -50, -10 };
            var thousand = new[] { -16D, 0, 50, 100, 1000 };
            Assert.Equal(42D, Enumerable.Repeat(42D, 1).Max(x => x));
            Assert.Equal(10D, ten.Max(x => x));
            Assert.Equal(-10D, minusTen.Max(x => x));
            Assert.Equal(1000D, thousand.Max(x => x));
            Assert.Equal(double.MaxValue, thousand.Concat(Enumerable.Repeat(double.MaxValue, 1)).Max(x => x));
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<double>().Max(x => x));
        }

        [Fact]
        public void MinDecimalWithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => (decimal)i).ToArray();
            var minusTen = new[] { -1M, -10, 10, 200, 1000 };
            var hundred = new[] { 3000M, 100, 200, 1000 };
            Assert.Equal(42M, Enumerable.Repeat(42M, 1).Max(x => x));
            Assert.Equal(1M, one.Min(x => x));
            Assert.Equal(-10M, minusTen.Min(x => x));
            Assert.Equal(100M, hundred.Min(x => x));
            Assert.Equal(decimal.MinValue, one.Concat(Enumerable.Repeat(decimal.MinValue, 1)).Min(x => x));
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<decimal>().Min(x => x));
        }

        [Fact]
        public void MaxDecimalWithSelector()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (decimal)i).ToArray();
            var minusTen = new[] { -100M, -15, -50, -10 };
            var thousand = new[] { -16M, 0, 50, 100, 1000 };
            Assert.Equal(42M, Enumerable.Repeat(42M, 1).Max(x => x));
            Assert.Equal(10M, ten.Max(x => x));
            Assert.Equal(-10M, minusTen.Max(x => x));
            Assert.Equal(1000M, thousand.Max(x => x));
            Assert.Equal(decimal.MaxValue, thousand.Concat(Enumerable.Repeat(decimal.MaxValue, 1)).Max(x => x));
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<decimal>().Max(x => x));
        }

        [Fact]
        public void MinNullableInt32WithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => (int?)i).ToArray();
            var minusTen = new[] { default(int?), -1, -10, 10, 200, 1000 };
            var hundred = new[] { default(int?), 3000, 100, 200, 1000 };
            Assert.Equal(42, Enumerable.Repeat((int?)42, 1).Min(x => x));
            Assert.Equal(1, one.Min(x => x));
            Assert.Equal(-10, minusTen.Min(x => x));
            Assert.Equal(100, hundred.Min(x => x));
            Assert.Equal(null, Enumerable.Empty<int?>().Min(x => x));
            Assert.Equal(int.MinValue, one.Concat(Enumerable.Repeat((int?)int.MinValue, 1)).Min(x => x));
            Assert.Equal(null, Enumerable.Repeat(default(int?), 100).Min(x => x));
        }

        [Fact]
        public void MaxNullableInt32WithSelector()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (int?)i).ToArray();
            var minusTen = new[] { default(int?), -100, -15, -50, -10 };
            var thousand = new[] { default(int?), -16, 0, 50, 100, 1000 };
            Assert.Equal(42, Enumerable.Repeat((int?)42, 1).Max(x => x));
            Assert.Equal(10, ten.Max(x => x));
            Assert.Equal(-10, minusTen.Max(x => x));
            Assert.Equal(1000, thousand.Max(x => x));
            Assert.Equal(int.MaxValue, thousand.Concat(Enumerable.Repeat((int?)int.MaxValue, 1)).Max(x => x));
            Assert.Equal(null, Enumerable.Empty<int?>().Max(x => x));
            Assert.Equal(null, Enumerable.Repeat(default(int?), 100).Max(x => x));
        }

        [Fact]
        public void MinNullableInt64WithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => (long?)i).ToArray();
            var minusTen = new[] { default(long?), -1L, -10, 10, 200, 1000 };
            var hundred = new[] { default(long?), 3000L, 100, 200, 1000 };
            Assert.Equal(42, Enumerable.Repeat((long?)42, 1).Min(x => x));
            Assert.Equal(1, one.Min(x => x));
            Assert.Equal(-10, minusTen.Min(x => x));
            Assert.Equal(100, hundred.Min(x => x));
            Assert.Equal(null, Enumerable.Empty<long?>().Min(x => x));
            Assert.Equal(long.MinValue, one.Concat(Enumerable.Repeat((long?)long.MinValue, 1)).Min(x => x));
            Assert.Equal(null, Enumerable.Repeat(default(long?), 100).Min(x => x));
        }

        [Fact]
        public void MaxNullableInt64WithSelector()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (long?)i).ToArray();
            var minusTen = new[] { default(long?), -100L, -15, -50, -10 };
            var thousand = new[] { default(long?), -16L, 0, 50, 100, 1000 };
            Assert.Equal(42, Enumerable.Repeat((long?)42, 1).Max(x => x));
            Assert.Equal(10, ten.Max(x => x));
            Assert.Equal(-10, minusTen.Max(x => x));
            Assert.Equal(1000, thousand.Max(x => x));
            Assert.Equal(long.MaxValue, thousand.Concat(Enumerable.Repeat((long?)long.MaxValue, 1)).Max(x => x));
            Assert.Equal(null, Enumerable.Empty<long?>().Max(x => x));
            Assert.Equal(null, Enumerable.Repeat(default(long?), 100).Max(x => x));
        }

        [Fact]
        public void MinNullableSingleWithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => (float?)i).ToArray();
            var minusTen = new[] { default(float?), -1F, -10, 10, 200, 1000 };
            var hundred = new[] { default(float?), 3000F, 100, 200, 1000 };
            Assert.Equal(42F, Enumerable.Repeat((float?)42, 1).Min(x => x));
            Assert.Equal(1F, one.Min(x => x));
            Assert.Equal(-10F, minusTen.Min(x => x));
            Assert.Equal(100F, hundred.Min(x => x));
            Assert.Equal(null, Enumerable.Empty<float?>().Min(x => x));
            Assert.Equal(float.MinValue, one.Concat(Enumerable.Repeat((float?)float.MinValue, 1)).Min(x => x));
            Assert.Equal(null, Enumerable.Repeat(default(float?), 100).Min(x => x));
        }

        [Fact]
        public void MaxNullableSingleWithSelector()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (float?)i).ToArray();
            var minusTen = new[] { default(float?), -100F, -15, -50, -10 };
            var thousand = new[] { default(float?), -16F, 0, 50, 100, 1000 };
            Assert.Equal(42F, Enumerable.Repeat((float?)42, 1).Max(x => x));
            Assert.Equal(10F, ten.Max(x => x));
            Assert.Equal(-10F, minusTen.Max(x => x));
            Assert.Equal(1000F, thousand.Max(x => x));
            Assert.Equal(float.MaxValue, thousand.Concat(Enumerable.Repeat((float?)float.MaxValue, 1)).Max(x => x));
            Assert.Equal(null, Enumerable.Empty<float?>().Max(x => x));
            Assert.Equal(null, Enumerable.Repeat(default(float?), 100).Max(x => x));
        }

        [Fact]
        public void MinNullableDoubleWithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => (double?)i).ToArray();
            var minusTen = new[] { default(double?), -1D, -10, 10, 200, 1000 };
            var hundred = new[] { default(double?), 3000D, 100, 200, 1000 };
            Assert.Equal(42D, Enumerable.Repeat((double?)42, 1).Min(x => x));
            Assert.Equal(1D, one.Min(x => x));
            Assert.Equal(-10D, minusTen.Min(x => x));
            Assert.Equal(100D, hundred.Min(x => x));
            Assert.Equal(double.MinValue, one.Concat(Enumerable.Repeat((double?)double.MinValue, 1)).Min(x => x));
            Assert.Equal(null, Enumerable.Empty<double?>().Min(x => x));
            Assert.Equal(null, Enumerable.Repeat(default(double?), 100).Min(x => x));
        }

        [Fact]
        public void MaxNullableDoubleWithSelector()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (double?)i).ToArray();
            var minusTen = new[] { default(double?), -100D, -15, -50, -10 };
            var thousand = new[] { default(double?), -16D, 0, 50, 100, 1000 };
            Assert.Equal(42D, Enumerable.Repeat((double?)42, 1).Max(x => x));
            Assert.Equal(10D, ten.Max(x => x));
            Assert.Equal(-10D, minusTen.Max(x => x));
            Assert.Equal(1000D, thousand.Max(x => x));
            Assert.Equal(double.MaxValue, thousand.Concat(Enumerable.Repeat((double?)double.MaxValue, 1)).Max(x => x));
            Assert.Equal(null, Enumerable.Empty<double?>().Max(x => x));
            Assert.Equal(null, Enumerable.Repeat(default(double?), 100).Max(x => x));
        }

        [Fact]
        public void MinNullableDecimalWithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => (decimal?)i).ToArray();
            var minusTen = new[] { default(decimal?), -1M, -10, 10, 200, 1000 };
            var hundred = new[] { default(decimal?), 3000M, 100, 200, 1000 };
            Assert.Equal(42M, Enumerable.Repeat((decimal?)42, 1).Min(x => x));
            Assert.Equal(1M, one.Min(x => x));
            Assert.Equal(-10M, minusTen.Min(x => x));
            Assert.Equal(100M, hundred.Min(x => x));
            Assert.Equal(decimal.MinValue, one.Concat(Enumerable.Repeat((decimal?)decimal.MinValue, 1)).Min(x => x));
            Assert.Equal(null, Enumerable.Empty<decimal?>().Min(x => x));
            Assert.Equal(null, Enumerable.Repeat(default(decimal?), 100).Min(x => x));
        }

        [Fact]
        public void MaxNullableDecimalWithSelector()
        {
            var ten = Enumerable.Range(1, 10).Select(i => (decimal?)i).ToArray();
            var minusTen = new[] { default(decimal?), -100M, -15, -50, -10 };
            var thousand = new[] { default(decimal?), -16M, 0, 50, 100, 1000 };
            Assert.Equal(42M, Enumerable.Repeat((decimal?)42, 1).Max(x => x));
            Assert.Equal(10M, ten.Max(x => x));
            Assert.Equal(-10M, minusTen.Max(x => x));
            Assert.Equal(1000M, thousand.Max(x => x));
            Assert.Equal(decimal.MaxValue, thousand.Concat(Enumerable.Repeat((decimal?)decimal.MaxValue, 1)).Max(x => x));
            Assert.Equal(null, Enumerable.Empty<decimal?>().Max(x => x));
            Assert.Equal(null, Enumerable.Repeat(default(decimal?), 100).Max(x => x));
        }

        // Normally NaN < anything is false, as is anything < NaN
        // However, this leads to some irksome outcomes in Min and Max.
        // If we use those semantics then Min(NaN, 5.0) is NaN, but
        // Min(5.0, NaN) is 5.0!  To fix this, we impose a total
        // ordering where NaN is smaller than every value, including
        // negative infinity.
        // This behaviour must be confirmed as happening.
        // Note that further to this, null is taken as not being in a sequence at all
        // (Null will be returned if the only element in a sequence, but this will also
        // happen with an empty sequence).

        [Fact]
        public void NaNFirstSingleWithSelector()
        {
            var nanThenOne = Enumerable.Range(1, 10).Select(i => (float)i).Concat(Enumerable.Repeat(float.NaN, 1)).ToArray();
            var nanThenMinusTen = new[] { -1F, -10, float.NaN, 10, 200, 1000 };
            var nanThenMinValue = new[] { float.MinValue, 3000F, 100, 200, float.NaN, 1000 };
            Assert.True(float.IsNaN(nanThenOne.Min(x => x)));
            Assert.True(float.IsNaN(nanThenMinusTen.Min(x => x)));
            Assert.True(float.IsNaN(nanThenMinValue.Min(x => x)));
            Assert.False(float.IsNaN(nanThenOne.Max(x => x)));
            Assert.False(float.IsNaN(nanThenMinusTen.Max(x => x)));
            Assert.False(float.IsNaN(nanThenMinValue.Max(x => x)));
            var nanWithNull = new[] { default(float?), float.NaN, default(float?) };
            Assert.True(float.IsNaN(nanWithNull.Min(x => x).Value));
            Assert.True(float.IsNaN(nanWithNull.Max(x => x).Value));
        }

        [Fact]
        public void NaNFirstDoubleWithSelector()
        {
            var nanThenOne = Enumerable.Range(1, 10).Select(i => (double)i).Concat(Enumerable.Repeat(double.NaN, 1)).ToArray();
            var nanThenMinusTen = new[] { -1F, -10, double.NaN, 10, 200, 1000 };
            var nanThenMinValue = new[] { double.MinValue, 3000F, 100, 200, double.NaN, 1000 };
            Assert.True(double.IsNaN(nanThenOne.Min(x => x)));
            Assert.True(double.IsNaN(nanThenMinusTen.Min(x => x)));
            Assert.True(double.IsNaN(nanThenMinValue.Min(x => x)));
            Assert.False(double.IsNaN(nanThenOne.Max(x => x)));
            Assert.False(double.IsNaN(nanThenMinusTen.Max(x => x)));
            Assert.False(double.IsNaN(nanThenMinValue.Max(x => x)));
            var nanWithNull = new[] { default(double?), double.NaN, default(double?) };
            Assert.True(double.IsNaN(nanWithNull.Min(x => x).Value));
            Assert.True(double.IsNaN(nanWithNull.Max(x => x).Value));
        }

        [Fact]
        public void MinDateTimeWithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => new DateTime(2000, 1, i)).ToArray();
            var newYears = new[]
            {
                new DateTime(2000, 12, 1),
                new DateTime(2000, 1, 1),
                new DateTime(2000, 1, 12)
            };
            var hundred = new[]
            {
                new DateTime(3000, 1, 1),
                new DateTime(100, 1, 1),
                new DateTime(200, 1, 1),
                new DateTime(1000, 1, 1)
            };
            Assert.Equal(DateTime.MinValue, one.Concat(Enumerable.Repeat(DateTime.MinValue, 1)).Min(x => x));
            Assert.Equal(new DateTime(2000, 1, 1), one.Min(x => x));
            Assert.Equal(new DateTime(2000, 1, 1), newYears.Min(x => x));
            Assert.Equal(new DateTime(100, 1, 1), hundred.Min(x => x));
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<DateTime>().Min(x => x));
        }

        [Fact]
        public void MaxDateTimeWithSelector()
        {
            var ten = Enumerable.Range(1, 10).Select(i => new DateTime(2000, 1, i)).ToArray();
            var newYearsEve = new[]
            {
                new DateTime(2000, 12, 1),
                new DateTime(2000, 12, 31),
                new DateTime(2000, 1, 12)
            };
            var threeThousand = new[]
            {
                new DateTime(3000, 1, 1),
                new DateTime(100, 1, 1),
                new DateTime(200, 1, 1),
                new DateTime(1000, 1, 1)
            };
            Assert.Equal(new DateTime(2000, 1, 10), ten.Max(x => x));
            Assert.Equal(new DateTime(2000, 12, 31), newYearsEve.Max(x => x));
            Assert.Equal(new DateTime(3000, 1, 1), threeThousand.Max(x => x));
            Assert.Equal(DateTime.MaxValue, threeThousand.Concat(Enumerable.Repeat(DateTime.MaxValue, 1)).Max(x => x));
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<DateTime>().Max(x => x));
        }

        [Fact]
        public void MinStringWithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => i.ToString()).ToArray();
            var agents = new[] { "Alice", "Bob", "Charlie", "Eve", "Mallory", "Trent", "Victor" };
            var confusedAgents = new[] { null, "Charlie", null, "Victor", "Trent", null, "Eve", "Alice", "Mallory", "Bob" };
            Assert.Equal("1", one.Min(x => x));
            Assert.Equal("Alice", agents.Min(x => x));
            Assert.Equal("Alice", confusedAgents.Min(x => x));
            Assert.Null(Enumerable.Empty<string>().Min(x => x));
        }

        [Fact]
        public void MaxStringWithSelector()
        {
            var nine = Enumerable.Range(1, 10).Select(i => i.ToString()).ToArray();
            var agents = new[] { "Alice", "Bob", "Charlie", "Eve", "Mallory", "Victor", "Trent" };
            var confusedAgents = new[] { null, "Charlie", null, "Victor", "Trent", null, "Eve", "Alice", "Mallory", "Bob" };
            Assert.Equal("9", nine.Max(x => x));
            Assert.Equal("Victor", agents.Max(x => x));
            Assert.Equal("Victor", confusedAgents.Max(x => x));
            Assert.Null(Enumerable.Empty<string>().Max(x => x));
        }
    }
}