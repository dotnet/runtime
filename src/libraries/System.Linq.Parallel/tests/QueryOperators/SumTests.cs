// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class SumTests
    {
        public static IEnumerable<object[]> SumData(int[] counts)
        {
            foreach (object[] results in UnorderedSources.Ranges(counts.Cast<int>(), x => Functions.SumRange(0L, x))) yield return results;
        }

        //
        // Sum
        //
        [Theory]
        [MemberData("SumData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Sum_Int(Labeled<ParallelQuery<int>> labeled, int count, int sum)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(sum, query.Sum());
            Assert.Equal(sum, query.Select(x => (int?)x).Sum());
            Assert.Equal(default(int), query.Select(x => (int?)null).Sum());
            Assert.Equal(-sum, query.Sum(x => -x));
            Assert.Equal(-sum, query.Sum(x => -(int?)x));
            Assert.Equal(default(int), query.Sum(x => (int?)null));
        }

        [Theory]
        [MemberData("SumData", (object)(new int[] { 1, 2, 16 }))]
        public static void Sum_Int_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, int sum)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(Functions.SumRange(0, count / 2), query.Select(x => x < count / 2 ? (int?)x : null).Sum());
            Assert.Equal(-Functions.SumRange(0, count / 2), query.Sum(x => x < count / 2 ? -(int?)x : null));
        }

        [Theory]
        [MemberData("SumData", (object)(new int[] { 1, 2, 16 }))]
        public static void Sum_Int_AllNull(Labeled<ParallelQuery<int>> labeled, int count, int sum)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(0, query.Select(x => (int?)null).Sum());
            Assert.Equal(0, query.Sum(x => (int?)null));
        }

        [Theory]
        [OuterLoop]
        [MemberData("SumData", (object)(new int[] { 1024 * 4, 1024 * 64 }))]
        public static void Sum_Int_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int sum)
        {
            Sum_Int(labeled, count, sum);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 2 }), MemberType = typeof(UnorderedSources))]
        public static void Sum_Int_Overflow(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Functions.AssertThrowsWrapped<OverflowException>(() => labeled.Item.Select(x => x == 0 ? int.MaxValue : x).Sum());
            Functions.AssertThrowsWrapped<OverflowException>(() => labeled.Item.Select(x => x == 0 ? int.MaxValue : (int?)x).Sum());
            Functions.AssertThrowsWrapped<OverflowException>(() => labeled.Item.Sum(x => x == 0 ? int.MinValue : -x));
            Functions.AssertThrowsWrapped<OverflowException>(() => labeled.Item.Sum(x => x == 0 ? int.MinValue : -(int?)x));
        }

        [Theory]
        [MemberData("SumData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Sum_Long(Labeled<ParallelQuery<int>> labeled, int count, long sum)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(sum, query.Select(x => (long)x).Sum());
            Assert.Equal(sum, query.Select(x => (long?)x).Sum());
            Assert.Equal(default(long), query.Select(x => (long?)null).Sum());
            Assert.Equal(-sum, query.Sum(x => -(long)x));
            Assert.Equal(-sum, query.Sum(x => -(long?)x));
            Assert.Equal(default(long), query.Sum(x => (long?)null));
        }

        [Theory]
        [OuterLoop]
        [MemberData("SumData", (object)(new int[] { 1024 * 4, 1024 * 1024 }))]
        public static void Sum_Long_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, long sum)
        {
            Sum_Long(labeled, count, sum);
        }

        [Theory]
        [MemberData("SumData", (object)(new int[] { 1, 2, 16 }))]
        public static void Sum_Long_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, long sum)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(Functions.SumRange(0L, count / 2), query.Select(x => x < count / 2 ? (long?)x : null).Sum());
            Assert.Equal(-Functions.SumRange(0L, count / 2), query.Sum(x => x < count / 2 ? -(long?)x : null));
        }

        [Theory]
        [MemberData("SumData", (object)(new int[] { 1, 2, 16 }))]
        public static void Sum_Long_AllNull(Labeled<ParallelQuery<int>> labeled, int count, long sum)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(0, query.Select(x => (long?)null).Sum());
            Assert.Equal(0, query.Sum(x => (long?)null));
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 2 }), MemberType = typeof(UnorderedSources))]
        public static void Sum_Long_Overflow(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Functions.AssertThrowsWrapped<OverflowException>(() => labeled.Item.Select(x => x == 0 ? long.MaxValue : x).Sum());
            Functions.AssertThrowsWrapped<OverflowException>(() => labeled.Item.Select(x => x == 0 ? long.MaxValue : (long?)x).Sum());
            Functions.AssertThrowsWrapped<OverflowException>(() => labeled.Item.Sum(x => x == 0 ? long.MinValue : -x));
            Functions.AssertThrowsWrapped<OverflowException>(() => labeled.Item.Sum(x => x == 0 ? long.MinValue : -(long?)x));
        }

        [Theory]
        [MemberData("SumData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Sum_Float(Labeled<ParallelQuery<int>> labeled, int count, float sum)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(sum, query.Select(x => (float)x).Sum());
            Assert.Equal(sum, query.Select(x => (float?)x).Sum());
            Assert.Equal(default(float), query.Select(x => (float?)null).Sum());
            Assert.Equal(-sum, query.Sum(x => -(float)x));
            Assert.Equal(-sum, query.Sum(x => -(float?)x));
            Assert.Equal(default(float), query.Sum(x => (float?)null));
        }

        [Theory]
        [OuterLoop]
        [MemberData("SumData", (object)(new int[] { 1024 * 4, 1024 * 1024 }))]
        public static void Sum_Float_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, float sum)
        {
            Sum_Float(labeled, count, sum);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 2 }), MemberType = typeof(UnorderedSources))]
        public static void Sum_Float_Overflow(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Assert.Equal(float.PositiveInfinity, labeled.Item.Select(x => float.MaxValue).Sum());
            Assert.Equal(float.PositiveInfinity, labeled.Item.Select(x => (float?)float.MaxValue).Sum().Value);
            Assert.Equal(float.PositiveInfinity, labeled.Item.Sum(x => float.MaxValue));
            Assert.Equal(float.PositiveInfinity, labeled.Item.Sum(x => (float?)float.MaxValue).Value);
            Assert.Equal(float.PositiveInfinity, labeled.Item.Select(x => float.PositiveInfinity).Sum());
            Assert.Equal(float.PositiveInfinity, labeled.Item.Select(x => (float?)float.PositiveInfinity).Sum().Value);
            Assert.Equal(float.PositiveInfinity, labeled.Item.Sum(x => float.PositiveInfinity));
            Assert.Equal(float.PositiveInfinity, labeled.Item.Sum(x => (float?)float.PositiveInfinity).Value);

            Assert.Equal(float.NegativeInfinity, labeled.Item.Select(x => float.MinValue).Sum());
            Assert.Equal(float.NegativeInfinity, labeled.Item.Select(x => (float?)float.MinValue).Sum().Value);
            Assert.Equal(float.NegativeInfinity, labeled.Item.Sum(x => float.MinValue));
            Assert.Equal(float.NegativeInfinity, labeled.Item.Sum(x => (float?)float.MinValue).Value);
            Assert.Equal(float.NegativeInfinity, labeled.Item.Select(x => float.NegativeInfinity).Sum());
            Assert.Equal(float.NegativeInfinity, labeled.Item.Select(x => (float?)float.NegativeInfinity).Sum().Value);
            Assert.Equal(float.NegativeInfinity, labeled.Item.Sum(x => float.NegativeInfinity));
            Assert.Equal(float.NegativeInfinity, labeled.Item.Sum(x => (float?)float.NegativeInfinity).Value);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 2 }), MemberType = typeof(UnorderedSources))]
        public static void Sum_Float_NaN(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Assert.Equal(float.NaN, labeled.Item.Select(x => x == 0 ? float.NaN : x).Sum());
            Assert.Equal(float.NaN, labeled.Item.Select(x => (float?)(x == 0 ? float.NaN : x)).Sum().Value);
            Assert.Equal(float.NaN, labeled.Item.Sum(x => x == 0 ? float.NaN : x));
            Assert.Equal(float.NaN, labeled.Item.Sum(x => (float?)(x == 0 ? float.NaN : x)).Value);
            Assert.Equal(float.NaN, labeled.Item.Select(x => x == 0 ? float.NaN : x).Sum());
            Assert.Equal(float.NaN, labeled.Item.Select(x => (float?)(x == 0 ? float.NaN : x)).Sum().Value);
            Assert.Equal(float.NaN, labeled.Item.Sum(x => x == 0 ? float.NaN : x));
            Assert.Equal(float.NaN, labeled.Item.Sum(x => (float?)(x == 0 ? float.NaN : x)).Value);

            Assert.Equal(float.NaN, labeled.Item.Select(x => x == 0 ? float.NaN : -x).Sum());
            Assert.Equal(float.NaN, labeled.Item.Select(x => (float?)(x == 0 ? float.NaN : x)).Sum().Value);
            Assert.Equal(float.NaN, labeled.Item.Sum(x => x == 0 ? float.NaN : -x));
            Assert.Equal(float.NaN, labeled.Item.Sum(x => (float?)(x == 0 ? float.NaN : -x)).Value);
            Assert.Equal(float.NaN, labeled.Item.Select(x => x == 0 ? float.NaN : x).Sum());
            Assert.Equal(float.NaN, labeled.Item.Select(x => (float?)(x == 0 ? float.NaN : x)).Sum().Value);
            Assert.Equal(float.NaN, labeled.Item.Sum(x => x == 0 ? float.NaN : x));
            Assert.Equal(float.NaN, labeled.Item.Sum(x => (float?)(x == 0 ? float.NaN : x)).Value);
        }

        [Theory]
        [MemberData("SumData", (object)(new int[] { 1, 2, 16 }))]
        public static void Sum_Float_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, float sum)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(Functions.SumRange(0, count / 2), query.Select(x => x < count / 2 ? (float?)x : null).Sum());
            Assert.Equal(-Functions.SumRange(0, count / 2), query.Sum(x => x < count / 2 ? -(float?)x : null));
        }

        [Theory]
        [MemberData("SumData", (object)(new int[] { 1, 2, 16 }))]
        public static void Sum_Float_AllNull(Labeled<ParallelQuery<int>> labeled, int count, float sum)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(0, query.Select(x => (float?)null).Sum());
            Assert.Equal(0, query.Sum(x => (float?)null));
        }

        [Theory]
        [MemberData("SumData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Sum_Double(Labeled<ParallelQuery<int>> labeled, int count, double sum)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(sum, query.Select(x => (double)x).Sum());
            Assert.Equal(sum, query.Select(x => (double?)x).Sum());
            Assert.Equal(default(double), query.Select(x => (double?)null).Sum());
            Assert.Equal(-sum, query.Sum(x => -(double)x));
            Assert.Equal(-sum, query.Sum(x => -(double?)x));
            Assert.Equal(default(double), query.Sum(x => (double?)null));
        }

        [Theory]
        [OuterLoop]
        [MemberData("SumData", (object)(new int[] { 1024 * 4, 1024 * 1024 }))]
        public static void Sum_Double_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, double sum)
        {
            Sum_Double(labeled, count, sum);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 2 }), MemberType = typeof(UnorderedSources))]
        public static void Sum_Double_Overflow(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Assert.Equal(double.PositiveInfinity, labeled.Item.Select(x => double.MaxValue).Sum());
            Assert.Equal(double.PositiveInfinity, labeled.Item.Select(x => (double?)double.MaxValue).Sum().Value);
            Assert.Equal(double.PositiveInfinity, labeled.Item.Sum(x => double.MaxValue));
            Assert.Equal(double.PositiveInfinity, labeled.Item.Sum(x => (double?)double.MaxValue).Value);
            Assert.Equal(double.PositiveInfinity, labeled.Item.Select(x => double.PositiveInfinity).Sum());
            Assert.Equal(double.PositiveInfinity, labeled.Item.Select(x => (double?)double.PositiveInfinity).Sum().Value);
            Assert.Equal(double.PositiveInfinity, labeled.Item.Sum(x => double.PositiveInfinity));
            Assert.Equal(double.PositiveInfinity, labeled.Item.Sum(x => (double?)double.PositiveInfinity).Value);

            Assert.Equal(double.NegativeInfinity, labeled.Item.Select(x => double.MinValue).Sum());
            Assert.Equal(double.NegativeInfinity, labeled.Item.Select(x => (double?)double.MinValue).Sum().Value);
            Assert.Equal(double.NegativeInfinity, labeled.Item.Sum(x => double.MinValue));
            Assert.Equal(double.NegativeInfinity, labeled.Item.Sum(x => (double?)double.MinValue).Value);
            Assert.Equal(double.NegativeInfinity, labeled.Item.Select(x => double.NegativeInfinity).Sum());
            Assert.Equal(double.NegativeInfinity, labeled.Item.Select(x => (double?)double.NegativeInfinity).Sum().Value);
            Assert.Equal(double.NegativeInfinity, labeled.Item.Sum(x => double.NegativeInfinity));
            Assert.Equal(double.NegativeInfinity, labeled.Item.Sum(x => (double?)double.NegativeInfinity).Value);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 2 }), MemberType = typeof(UnorderedSources))]
        public static void Sum_Double_NaN(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Assert.Equal(double.NaN, labeled.Item.Select(x => x == 0 ? double.NaN : x).Sum());
            Assert.Equal(double.NaN, labeled.Item.Select(x => (double?)(x == 0 ? double.NaN : x)).Sum().Value);
            Assert.Equal(double.NaN, labeled.Item.Sum(x => x == 0 ? double.NaN : x));
            Assert.Equal(double.NaN, labeled.Item.Sum(x => (double?)(x == 0 ? double.NaN : x)).Value);
            Assert.Equal(double.NaN, labeled.Item.Select(x => x == 0 ? double.NaN : x).Sum());
            Assert.Equal(double.NaN, labeled.Item.Select(x => (double?)(x == 0 ? double.NaN : x)).Sum().Value);
            Assert.Equal(double.NaN, labeled.Item.Sum(x => x == 0 ? double.NaN : x));
            Assert.Equal(double.NaN, labeled.Item.Sum(x => (double?)(x == 0 ? double.NaN : x)).Value);

            Assert.Equal(double.NaN, labeled.Item.Select(x => x == 0 ? double.NaN : -x).Sum());
            Assert.Equal(double.NaN, labeled.Item.Select(x => (double?)(x == 0 ? double.NaN : x)).Sum().Value);
            Assert.Equal(double.NaN, labeled.Item.Sum(x => x == 0 ? double.NaN : -x));
            Assert.Equal(double.NaN, labeled.Item.Sum(x => (double?)(x == 0 ? double.NaN : -x)).Value);
            Assert.Equal(double.NaN, labeled.Item.Select(x => x == 0 ? double.NaN : x).Sum());
            Assert.Equal(double.NaN, labeled.Item.Select(x => (double?)(x == 0 ? double.NaN : x)).Sum().Value);
            Assert.Equal(double.NaN, labeled.Item.Sum(x => x == 0 ? double.NaN : x));
            Assert.Equal(double.NaN, labeled.Item.Sum(x => (double?)(x == 0 ? double.NaN : x)).Value);
        }

        [Theory]
        [MemberData("SumData", (object)(new int[] { 1, 2, 16 }))]
        public static void Sum_Double_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, double sum)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(Functions.SumRange(0, count / 2), query.Select(x => x < count / 2 ? (double?)x : null).Sum());
            Assert.Equal(-Functions.SumRange(0, count / 2), query.Sum(x => x < count / 2 ? -(double?)x : null));
        }

        [Theory]
        [MemberData("SumData", (object)(new int[] { 1, 2, 16 }))]
        public static void Sum_Double_AllNull(Labeled<ParallelQuery<int>> labeled, int count, double sum)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(0, query.Select(x => (double?)null).Sum());
            Assert.Equal(0, query.Sum(x => (double?)null));
        }

        [Theory]
        [MemberData("SumData", (object)(new int[] { 0, 1, 2, 16 }))]
        public static void Sum_Decimal(Labeled<ParallelQuery<int>> labeled, int count, decimal sum)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(sum, query.Select(x => (decimal)x).Sum());
            Assert.Equal(sum, query.Select(x => (decimal?)x).Sum());
            Assert.Equal(default(decimal), query.Select(x => (decimal?)null).Sum());
            Assert.Equal(-sum, query.Sum(x => -(decimal)x));
            Assert.Equal(default(decimal), query.Sum(x => (decimal?)null));
        }

        [Theory]
        [OuterLoop]
        [MemberData("SumData", (object)(new int[] { 1024 * 4, 1024 * 1024 }))]
        public static void Sum_Decimal_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, decimal sum)
        {
            Sum_Decimal(labeled, count, sum);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 2 }), MemberType = typeof(UnorderedSources))]
        public static void Sum_Decimal_Overflow(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Functions.AssertThrowsWrapped<OverflowException>(() => labeled.Item.Select(x => x == 0 ? decimal.MaxValue : x).Sum());
            Functions.AssertThrowsWrapped<OverflowException>(() => labeled.Item.Select(x => x == 0 ? decimal.MaxValue : (decimal?)x).Sum());
            Functions.AssertThrowsWrapped<OverflowException>(() => labeled.Item.Sum(x => x == 0 ? decimal.MinValue : -x));
            Functions.AssertThrowsWrapped<OverflowException>(() => labeled.Item.Sum(x => x == 0 ? decimal.MinValue : -(decimal?)x));
        }

        [Theory]
        [MemberData("SumData", (object)(new int[] { 1, 2, 16 }))]
        public static void Sum_Decimal_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, decimal sum)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(Functions.SumRange(0, count / 2), query.Select(x => x < count / 2 ? (decimal?)x : null).Sum());
            Assert.Equal(-Functions.SumRange(0, count / 2), query.Sum(x => x < count / 2 ? -(decimal?)x : null));
        }

        [Theory]
        [MemberData("SumData", (object)(new int[] { 1, 2, 16 }))]
        public static void Sum_Decimal_AllNull(Labeled<ParallelQuery<int>> labeled, int count, decimal sum)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(0, query.Select(x => (decimal?)null).Sum());
            Assert.Equal(0, query.Sum(x => (decimal?)null));
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 2 }), MemberType = typeof(UnorderedSources))]
        public static void Sum_OperationCanceledException_PreCanceled(Labeled<ParallelQuery<int>> labeled, int count)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Sum(x => x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Sum(x => (int?)x));

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Sum(x => (long)x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Sum(x => (long?)x));

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Sum(x => (float)x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Sum(x => (float?)x));

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Sum(x => (double)x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Sum(x => (double?)x));

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Sum(x => (decimal)x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Sum(x => (decimal?)x));
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 2 }), MemberType = typeof(UnorderedSources))]
        public static void Sum_AggregateException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Sum((Func<int, int>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Sum((Func<int, int?>)(x => { throw new DeliberateTestException(); })));

            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Sum((Func<int, long>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Sum((Func<int, long?>)(x => { throw new DeliberateTestException(); })));

            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Sum((Func<int, float>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Sum((Func<int, float?>)(x => { throw new DeliberateTestException(); })));

            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Sum((Func<int, double>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Sum((Func<int, double?>)(x => { throw new DeliberateTestException(); })));

            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Sum((Func<int, decimal>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Sum((Func<int, decimal?>)(x => { throw new DeliberateTestException(); })));
        }

        [Fact]
        public static void Sum_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).Sum());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat(0, 1).Sum((Func<int, int>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int?>)null).Sum());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((int?)0, 1).Sum((Func<int?, int?>)null));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<long>)null).Sum());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((long)0, 1).Sum((Func<long, long>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<long?>)null).Sum());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((long?)0, 1).Sum((Func<long?, long?>)null));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<float>)null).Sum());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((float)0, 1).Sum((Func<float, float>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<float?>)null).Sum());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((float?)0, 1).Sum((Func<float?, float>)null));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<double>)null).Sum());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((double)0, 1).Sum((Func<double, double>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<double?>)null).Sum());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((double?)0, 1).Sum((Func<double?, double>)null));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<decimal>)null).Sum());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((decimal)0, 1).Sum((Func<decimal, decimal>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<decimal?>)null).Sum());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((decimal?)0, 1).Sum((Func<decimal?, decimal>)null));
        }
    }
}
