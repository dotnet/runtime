// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public class MinTests
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
        public void MinDecimal()
        {
            var one = Enumerable.Range(1, 10).Select(i => (decimal)i).ToArray();
            var minusTen = new[] { -1M, -10, 10, 200, 1000 };
            var hundred = new[] { 3000M, 100, 200, 1000 };
            Assert.Equal(42M, Enumerable.Repeat(42M, 1).Min());
            Assert.Equal(1M, one.Min());
            Assert.Equal(-10M, minusTen.Min());
            Assert.Equal(100M, hundred.Min());
            Assert.Equal(decimal.MinValue, one.Concat(Enumerable.Repeat(decimal.MinValue, 1)).Min());
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<decimal>().Min());
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
            var nanWithNull = new[] { default(float?), float.NaN, default(float?) };
            Assert.True(float.IsNaN(nanWithNull.Min().Value));
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
            var nanWithNull = new[] { default(double?), double.NaN, default(double?) };
            Assert.True(double.IsNaN(nanWithNull.Min().Value));
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
        public void MinDecimalWithSelector()
        {
            var one = Enumerable.Range(1, 10).Select(i => (decimal)i).ToArray();
            var minusTen = new[] { -1M, -10, 10, 200, 1000 };
            var hundred = new[] { 3000M, 100, 200, 1000 };
            Assert.Equal(42M, Enumerable.Repeat(42M, 1).Min(x => x));
            Assert.Equal(1M, one.Min(x => x));
            Assert.Equal(-10M, minusTen.Min(x => x));
            Assert.Equal(100M, hundred.Min(x => x));
            Assert.Equal(decimal.MinValue, one.Concat(Enumerable.Repeat(decimal.MinValue, 1)).Min(x => x));
            Assert.Throws<InvalidOperationException>(() => Enumerable.Empty<decimal>().Min(x => x));
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
            var nanWithNull = new[] { default(float?), float.NaN, default(float?) };
            Assert.True(float.IsNaN(nanWithNull.Min(x => x).Value));
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
            var nanWithNull = new[] { default(double?), double.NaN, default(double?) };
            Assert.True(double.IsNaN(nanWithNull.Min(x => x).Value));
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
    }
}