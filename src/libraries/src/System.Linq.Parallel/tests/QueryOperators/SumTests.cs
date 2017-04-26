// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class SumTests
    {
        //
        // Sum
        //
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Sum_Int(int count)
        {
            Assert.Equal(Functions.SumRange(0, count), ParallelEnumerable.Range(0, count).Sum());
            Assert.Equal(Functions.SumRange(0, count), ParallelEnumerable.Range(0, count).Select(x => (int?)x).Sum());
            Assert.Equal(-Functions.SumRange(0, count), ParallelEnumerable.Range(0, count).Sum(x => -x));
            Assert.Equal(-Functions.SumRange(0, count), ParallelEnumerable.Range(0, count).Sum(x => -(int?)x));
        }

        [Theory]
        [InlineData(2)]
        [InlineData(16)]
        public static void Sum_Int_SomeNull(int count)
        {
            Assert.Equal(Functions.SumRange(0, count / 2), ParallelEnumerable.Range(0, count).Select(x => x < count / 2 ? (int?)x : null).Sum());
            Assert.Equal(-Functions.SumRange(0, count / 2), ParallelEnumerable.Range(0, count).Sum(x => x < count / 2 ? -(int?)x : null));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Sum_Int_AllNull(int count)
        {
            Assert.Equal(0, ParallelEnumerable.Repeat((int?)null, count).Sum());
            Assert.Equal(0, ParallelEnumerable.Range(0, count).Sum(x => (int?)null));
        }

        [Theory]
        [OuterLoop]
        [InlineData(1024 * 4)]
        [InlineData(1024 * 64)]
        public static void Sum_Int_Longrunning(int count)
        {
            Sum_Int(count);
        }

        [Fact]
        public static void Sum_Int_Overflow()
        {
            AssertThrows.Wrapped<OverflowException>(() => new[] { int.MaxValue, 1 }.AsParallel().Sum());
            AssertThrows.Wrapped<OverflowException>(() => new[] { (int?)int.MaxValue, 1 }.AsParallel().Sum());
            AssertThrows.Wrapped<OverflowException>(() => new[] { int.MinValue, -1 }.AsParallel().Sum());
            AssertThrows.Wrapped<OverflowException>(() => new[] { (int?)int.MinValue, -1 }.AsParallel().Sum());
            AssertThrows.Wrapped<OverflowException>(() => ParallelEnumerable.Range(0, 2).Sum(x => x == 0 ? int.MaxValue : x));
            AssertThrows.Wrapped<OverflowException>(() => ParallelEnumerable.Range(0, 2).Sum(x => x == 0 ? int.MaxValue : (int?)x));
            AssertThrows.Wrapped<OverflowException>(() => ParallelEnumerable.Range(0, 2).Sum(x => x == 0 ? int.MinValue : -x));
            AssertThrows.Wrapped<OverflowException>(() => ParallelEnumerable.Range(0, 2).Sum(x => x == 0 ? int.MinValue : -(int?)x));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Sum_Long(int count)
        {
            Assert.Equal(Functions.SumRange(0, count), ParallelEnumerable.Range(0, count).Select(x => (long)x).Sum());
            Assert.Equal(Functions.SumRange(0, count), ParallelEnumerable.Range(0, count).Select(x => (long?)x).Sum());
            Assert.Equal(-Functions.SumRange(0, count), ParallelEnumerable.Range(0, count).Sum(x => -(long)x));
            Assert.Equal(-Functions.SumRange(0, count), ParallelEnumerable.Range(0, count).Sum(x => -(long?)x));
        }

        [Theory]
        [OuterLoop]
        [InlineData(1024 * 4)]
        [InlineData(1024 * 64)]
        public static void Sum_Long_Longrunning(int count)
        {
            Sum_Long(count);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Sum_Long_SomeNull(int count)
        {
            Assert.Equal(Functions.SumRange(0, count / 2), ParallelEnumerable.Range(0, count).Select(x => x < count / 2 ? (long?)x : null).Sum());
            Assert.Equal(-Functions.SumRange(0, count / 2), ParallelEnumerable.Range(0, count).Sum(x => x < count / 2 ? -(long?)x : null));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Sum_Long_AllNull(int count)
        {
            Assert.Equal(0, ParallelEnumerable.Repeat((long?)null, count).Sum());
            Assert.Equal(0, ParallelEnumerable.Range(0, count).Sum(x => (long?)null));
        }

        [Fact]
        public static void Sum_Long_Overflow()
        {
            AssertThrows.Wrapped<OverflowException>(() => new[] { long.MaxValue, 1 }.AsParallel().Sum());
            AssertThrows.Wrapped<OverflowException>(() => new[] { (long?)long.MaxValue, 1 }.AsParallel().Sum());
            AssertThrows.Wrapped<OverflowException>(() => new[] { long.MinValue, -1 }.AsParallel().Sum());
            AssertThrows.Wrapped<OverflowException>(() => new[] { (long?)long.MinValue, -1 }.AsParallel().Sum());
            AssertThrows.Wrapped<OverflowException>(() => ParallelEnumerable.Range(0, 2).Sum(x => x == 0 ? long.MaxValue : x));
            AssertThrows.Wrapped<OverflowException>(() => ParallelEnumerable.Range(0, 2).Sum(x => x == 0 ? long.MaxValue : (long?)x));
            AssertThrows.Wrapped<OverflowException>(() => ParallelEnumerable.Range(0, 2).Sum(x => x == 0 ? long.MinValue : -x));
            AssertThrows.Wrapped<OverflowException>(() => ParallelEnumerable.Range(0, 2).Sum(x => x == 0 ? long.MinValue : -(long?)x));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Core bug fix https://github.com/dotnet/corefx/pull/1215")]
        public static void Sum_Float(int count)
        {
            Assert.Equal(Functions.SumRange(0, count), ParallelEnumerable.Range(0, count).Select(x => (float)x).Sum());
            Assert.Equal(Functions.SumRange(0, count), ParallelEnumerable.Range(0, count).Select(x => (float?)x).Sum());
            Assert.Equal(-Functions.SumRange(0, count), ParallelEnumerable.Range(0, count).Sum(x => -(float)x));
            Assert.Equal(-Functions.SumRange(0, count), ParallelEnumerable.Range(0, count).Sum(x => -(float?)x));
        }

        [Theory]
        [OuterLoop]
        [InlineData(1024 * 4)]
        [InlineData(1024 * 64)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Core bug fix https://github.com/dotnet/corefx/pull/1215")]
        public static void Sum_Float_Longrunning(int count)
        {
            Sum_Float(count);
        }

        [Fact]
        public static void Sum_Float_Overflow()
        {
            Assert.Equal(float.PositiveInfinity, ParallelEnumerable.Repeat(float.MaxValue, 2).Sum());
            Assert.Equal(float.PositiveInfinity, ParallelEnumerable.Repeat((float?)float.MaxValue, 2).Sum());
            Assert.Equal(float.PositiveInfinity, ParallelEnumerable.Range(0, 2).Sum(x => float.MaxValue));
            Assert.Equal(float.PositiveInfinity, ParallelEnumerable.Range(0, 2).Sum(x => (float?)float.MaxValue));
            Assert.Equal(float.PositiveInfinity, ParallelEnumerable.Repeat(float.PositiveInfinity, 2).Sum());
            Assert.Equal(float.PositiveInfinity, ParallelEnumerable.Repeat((float?)float.PositiveInfinity, 2).Sum());
            Assert.Equal(float.PositiveInfinity, ParallelEnumerable.Range(0, 2).Sum(x => float.PositiveInfinity));
            Assert.Equal(float.PositiveInfinity, ParallelEnumerable.Range(0, 2).Sum(x => (float?)float.PositiveInfinity));

            Assert.Equal(float.NegativeInfinity, ParallelEnumerable.Repeat(float.MinValue, 2).Sum());
            Assert.Equal(float.NegativeInfinity, ParallelEnumerable.Repeat((float?)float.MinValue, 2).Sum());
            Assert.Equal(float.NegativeInfinity, ParallelEnumerable.Range(0, 2).Sum(x => float.MinValue));
            Assert.Equal(float.NegativeInfinity, ParallelEnumerable.Range(0, 2).Sum(x => (float?)float.MinValue));
            Assert.Equal(float.NegativeInfinity, ParallelEnumerable.Repeat(float.NegativeInfinity, 2).Sum());
            Assert.Equal(float.NegativeInfinity, ParallelEnumerable.Repeat((float?)float.NegativeInfinity, 2).Sum());
            Assert.Equal(float.NegativeInfinity, ParallelEnumerable.Range(0, 2).Sum(x => float.NegativeInfinity));
            Assert.Equal(float.NegativeInfinity, ParallelEnumerable.Range(0, 2).Sum(x => (float?)float.NegativeInfinity));
        }

        [Fact]
        public static void Sum_Float_NaN()
        {
            Assert.Equal(float.NaN, new[] { float.NaN, 1F }.AsParallel().Sum());
            Assert.Equal(float.NaN, new[] { (float?)float.NaN, 1F }.AsParallel().Sum());
            Assert.Equal(float.NaN, ParallelEnumerable.Range(0, 2).Sum(x => x == 0 ? float.NaN : x));
            Assert.Equal(float.NaN, ParallelEnumerable.Range(0, 2).Sum(x => (float?)(x == 0 ? float.NaN : x)));

            Assert.Equal(float.NaN, new[] { float.NaN, -1F }.AsParallel().Sum());
            Assert.Equal(float.NaN, new[] { (float?)float.NaN, -1F }.AsParallel().Sum());
            Assert.Equal(float.NaN, ParallelEnumerable.Range(0, 2).Sum(x => x == 0 ? float.NaN : -x));
            Assert.Equal(float.NaN, ParallelEnumerable.Range(0, 2).Sum(x => (float?)(x == 0 ? float.NaN : -x)));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Sum_Float_SomeNull(int count)
        {
            Assert.Equal(Functions.SumRange(0, count / 2), ParallelEnumerable.Range(0, count).Select(x => x < count / 2 ? (float?)x : null).Sum());
            Assert.Equal(-Functions.SumRange(0, count / 2), ParallelEnumerable.Range(0, count).Sum(x => x < count / 2 ? -(float?)x : null));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Sum_Float_AllNull(int count)
        {
            Assert.Equal(0, ParallelEnumerable.Repeat((float?)null, count).Sum());
            Assert.Equal(0, ParallelEnumerable.Range(0, count).Sum(x => (float?)null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Sum_Double(int count)
        {
            Assert.Equal(Functions.SumRange(0, count), ParallelEnumerable.Range(0, count).Select(x => (double)x).Sum());
            Assert.Equal(Functions.SumRange(0, count), ParallelEnumerable.Range(0, count).Select(x => (double?)x).Sum());
            Assert.Equal(-Functions.SumRange(0, count), ParallelEnumerable.Range(0, count).Sum(x => -(double)x));
            Assert.Equal(-Functions.SumRange(0, count), ParallelEnumerable.Range(0, count).Sum(x => -(double?)x));
        }

        [Theory]
        [OuterLoop]
        [InlineData(1024 * 4)]
        [InlineData(1024 * 64)]
        public static void Sum_Double_Longrunning(int count)
        {
            Sum_Double(count);
        }

        [Fact]
        public static void Sum_Double_Overflow()
        {
            Assert.Equal(double.PositiveInfinity, ParallelEnumerable.Repeat(double.MaxValue, 2).Sum());
            Assert.Equal(double.PositiveInfinity, ParallelEnumerable.Repeat((double?)double.MaxValue, 2).Sum());
            Assert.Equal(double.PositiveInfinity, ParallelEnumerable.Range(0, 2).Sum(x => double.MaxValue));
            Assert.Equal(double.PositiveInfinity, ParallelEnumerable.Range(0, 2).Sum(x => (double?)double.MaxValue));
            Assert.Equal(double.PositiveInfinity, ParallelEnumerable.Repeat(double.PositiveInfinity, 2).Sum());
            Assert.Equal(double.PositiveInfinity, ParallelEnumerable.Repeat((double?)double.PositiveInfinity, 2).Sum());
            Assert.Equal(double.PositiveInfinity, ParallelEnumerable.Range(0, 2).Sum(x => double.PositiveInfinity));
            Assert.Equal(double.PositiveInfinity, ParallelEnumerable.Range(0, 2).Sum(x => (double?)double.PositiveInfinity));

            Assert.Equal(double.NegativeInfinity, ParallelEnumerable.Repeat(double.MinValue, 2).Sum());
            Assert.Equal(double.NegativeInfinity, ParallelEnumerable.Repeat((double?)double.MinValue, 2).Sum());
            Assert.Equal(double.NegativeInfinity, ParallelEnumerable.Range(0, 2).Sum(x => double.MinValue));
            Assert.Equal(double.NegativeInfinity, ParallelEnumerable.Range(0, 2).Sum(x => (double?)double.MinValue));
            Assert.Equal(double.NegativeInfinity, ParallelEnumerable.Repeat(double.NegativeInfinity, 2).Sum());
            Assert.Equal(double.NegativeInfinity, ParallelEnumerable.Repeat((double?)double.NegativeInfinity, 2).Sum());
            Assert.Equal(double.NegativeInfinity, ParallelEnumerable.Range(0, 2).Sum(x => double.NegativeInfinity));
            Assert.Equal(double.NegativeInfinity, ParallelEnumerable.Range(0, 2).Sum(x => (double?)double.NegativeInfinity));
        }

        [Fact]
        public static void Sum_Double_NaN()
        {
            Assert.Equal(double.NaN, new[] { double.NaN, 1F }.AsParallel().Sum());
            Assert.Equal(double.NaN, new[] { (double?)double.NaN, 1F }.AsParallel().Sum());
            Assert.Equal(double.NaN, ParallelEnumerable.Range(0, 2).Sum(x => x == 0 ? double.NaN : x));
            Assert.Equal(double.NaN, ParallelEnumerable.Range(0, 2).Sum(x => (double?)(x == 0 ? double.NaN : x)));

            Assert.Equal(double.NaN, new[] { double.NaN, -1F }.AsParallel().Sum());
            Assert.Equal(double.NaN, new[] { (double?)double.NaN, -1F }.AsParallel().Sum());
            Assert.Equal(double.NaN, ParallelEnumerable.Range(0, 2).Sum(x => x == 0 ? double.NaN : -x));
            Assert.Equal(double.NaN, ParallelEnumerable.Range(0, 2).Sum(x => (double?)(x == 0 ? double.NaN : -x)));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Sum_Double_SomeNull(int count)
        {
            Assert.Equal(Functions.SumRange(0, count / 2), ParallelEnumerable.Range(0, count).Select(x => x < count / 2 ? (double?)x : null).Sum());
            Assert.Equal(-Functions.SumRange(0, count / 2), ParallelEnumerable.Range(0, count).Sum(x => x < count / 2 ? -(double?)x : null));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Sum_Double_AllNull(int count)
        {
            Assert.Equal(0, ParallelEnumerable.Repeat((double?)null, count).Sum());
            Assert.Equal(0, ParallelEnumerable.Range(0, count).Sum(x => (double?)null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Sum_Decimal(int count)
        {
            Assert.Equal(Functions.SumRange(0, count), ParallelEnumerable.Range(0, count).Select(x => (decimal)x).Sum());
            Assert.Equal(Functions.SumRange(0, count), ParallelEnumerable.Range(0, count).Select(x => (decimal?)x).Sum());
            Assert.Equal(-Functions.SumRange(0, count), ParallelEnumerable.Range(0, count).Sum(x => -(decimal)x));
            Assert.Equal(-Functions.SumRange(0, count), ParallelEnumerable.Range(0, count).Sum(x => -(decimal?)x));
        }

        [Theory]
        [OuterLoop]
        [InlineData(1024 * 4)]
        [InlineData(1024 * 64)]
        public static void Sum_Decimal_Longrunning(int count)
        {
            Sum_Decimal(count);
        }

        [Fact]
        public static void Sum_Decimal_Overflow()
        {
            AssertThrows.Wrapped<OverflowException>(() => new[] { decimal.MaxValue, 1M }.AsParallel().Sum());
            AssertThrows.Wrapped<OverflowException>(() => new[] { (decimal?)decimal.MaxValue, 1M }.AsParallel().Sum());
            AssertThrows.Wrapped<OverflowException>(() => new[] { decimal.MinValue, -1M }.AsParallel().Sum());
            AssertThrows.Wrapped<OverflowException>(() => new[] { (decimal?)decimal.MinValue, -1M }.AsParallel().Sum());
            AssertThrows.Wrapped<OverflowException>(() => ParallelEnumerable.Range(0, 2).Sum(x => x == 0 ? decimal.MaxValue : x));
            AssertThrows.Wrapped<OverflowException>(() => ParallelEnumerable.Range(0, 2).Sum(x => x == 0 ? decimal.MaxValue : (decimal?)x));
            AssertThrows.Wrapped<OverflowException>(() => ParallelEnumerable.Range(0, 2).Sum(x => x == 0 ? decimal.MinValue : -x));
            AssertThrows.Wrapped<OverflowException>(() => ParallelEnumerable.Range(0, 2).Sum(x => x == 0 ? decimal.MinValue : -(decimal?)x));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Sum_Decimal_SomeNull(int count)
        {
            Assert.Equal(Functions.SumRange(0, count / 2), ParallelEnumerable.Range(0, count).Select(x => x < count / 2 ? (decimal?)x : null).Sum());
            Assert.Equal(-Functions.SumRange(0, count / 2), ParallelEnumerable.Range(0, count).Sum(x => x < count / 2 ? -(decimal?)x : null));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Sum_Decimal_AllNull(int count)
        {
            Assert.Equal(0, ParallelEnumerable.Repeat((decimal?)null, count).Sum());
            Assert.Equal(0, ParallelEnumerable.Range(0, count).Sum(x => (decimal?)null));
        }

        [Fact]
        public static void Sum_OperationCanceledException()
        {
            AssertThrows.EventuallyCanceled((source, canceler) => source.Sum(x => { canceler(); return x; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Sum(x => { canceler(); return (int?)x; }));

            AssertThrows.EventuallyCanceled((source, canceler) => source.Sum(x => { canceler(); return (long)x; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Sum(x => { canceler(); return (long?)x; }));

            AssertThrows.EventuallyCanceled((source, canceler) => source.Sum(x => { canceler(); return (float)x; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Sum(x => { canceler(); return (float?)x; }));

            AssertThrows.EventuallyCanceled((source, canceler) => source.Sum(x => { canceler(); return (double)x; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Sum(x => { canceler(); return (double?)x; }));

            AssertThrows.EventuallyCanceled((source, canceler) => source.Sum(x => { canceler(); return (decimal)x; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Sum(x => { canceler(); return (decimal?)x; }));
        }

        [Fact]
        public static void Sum_AggregateException_Wraps_OperationCanceledException()
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Sum(x => { canceler(); return x; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Sum(x => { canceler(); return (int?)x; }));

            AssertThrows.OtherTokenCanceled((source, canceler) => source.Sum(x => { canceler(); return (long)x; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Sum(x => { canceler(); return (long?)x; }));

            AssertThrows.OtherTokenCanceled((source, canceler) => source.Sum(x => { canceler(); return (float)x; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Sum(x => { canceler(); return (float?)x; }));

            AssertThrows.OtherTokenCanceled((source, canceler) => source.Sum(x => { canceler(); return (double)x; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Sum(x => { canceler(); return (double?)x; }));

            AssertThrows.OtherTokenCanceled((source, canceler) => source.Sum(x => { canceler(); return (decimal)x; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Sum(x => { canceler(); return (decimal?)x; }));

            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Sum(x => { canceler(); return x; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Sum(x => { canceler(); return (int?)x; }));

            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Sum(x => { canceler(); return (long)x; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Sum(x => { canceler(); return (long?)x; }));

            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Sum(x => { canceler(); return (float)x; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Sum(x => { canceler(); return (float?)x; }));

            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Sum(x => { canceler(); return (double)x; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Sum(x => { canceler(); return (double?)x; }));

            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Sum(x => { canceler(); return (decimal)x; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Sum(x => { canceler(); return (decimal?)x; }));
        }

        [Fact]
        public static void Sum_OperationCanceledException_PreCanceled()
        {
            AssertThrows.AlreadyCanceled(source => source.Sum(x => x));
            AssertThrows.AlreadyCanceled(source => source.Sum(x => (int?)x));

            AssertThrows.AlreadyCanceled(source => source.Sum(x => (long)x));
            AssertThrows.AlreadyCanceled(source => source.Sum(x => (long?)x));

            AssertThrows.AlreadyCanceled(source => source.Sum(x => (float)x));
            AssertThrows.AlreadyCanceled(source => source.Sum(x => (float?)x));

            AssertThrows.AlreadyCanceled(source => source.Sum(x => (double)x));
            AssertThrows.AlreadyCanceled(source => source.Sum(x => (double?)x));

            AssertThrows.AlreadyCanceled(source => source.Sum(x => (decimal)x));
            AssertThrows.AlreadyCanceled(source => source.Sum(x => (decimal?)x));
        }

        [Fact]
        public static void Sum_AggregateException()
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 2).Sum((Func<int, int>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 2).Sum((Func<int, int?>)(x => { throw new DeliberateTestException(); })));

            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 2).Sum((Func<int, long>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 2).Sum((Func<int, long?>)(x => { throw new DeliberateTestException(); })));

            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 2).Sum((Func<int, float>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 2).Sum((Func<int, float?>)(x => { throw new DeliberateTestException(); })));

            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 2).Sum((Func<int, double>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 2).Sum((Func<int, double?>)(x => { throw new DeliberateTestException(); })));

            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 2).Sum((Func<int, decimal>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 2).Sum((Func<int, decimal?>)(x => { throw new DeliberateTestException(); })));
        }

        [Fact]
        public static void Sum_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).Sum());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat(0, 1).Sum((Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int?>)null).Sum());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((int?)0, 1).Sum((Func<int?, int?>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<long>)null).Sum());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((long)0, 1).Sum((Func<long, long>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<long?>)null).Sum());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((long?)0, 1).Sum((Func<long?, long?>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<float>)null).Sum());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((float)0, 1).Sum((Func<float, float>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<float?>)null).Sum());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((float?)0, 1).Sum((Func<float?, float>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<double>)null).Sum());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((double)0, 1).Sum((Func<double, double>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<double?>)null).Sum());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((double?)0, 1).Sum((Func<double?, double>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<decimal>)null).Sum());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((decimal)0, 1).Sum((Func<decimal, decimal>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<decimal?>)null).Sum());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((decimal?)0, 1).Sum((Func<decimal?, decimal>)null));
        }
    }
}
