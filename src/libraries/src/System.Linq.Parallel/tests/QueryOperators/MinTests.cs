// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace Test
{
    public class MinTests
    {
        public static IEnumerable<object[]> MinData(object[] counts)
        {
            Func<int, int> min = x => 1 - x;
            foreach (object[] results in UnorderedSources.Ranges(counts.Cast<int>(), min)) yield return results;
        }

        //
        // Min
        //
        [Theory]
        [MemberData("MinData", (object)(new int[] { 1, 2, 16 }))]
        public static void Min_Int(Labeled<ParallelQuery<int>> labeled, int count, int min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(0, query.Min());
            Assert.Equal(0, query.Select(x => (int?)x).Min());
            Assert.Equal(min, query.Min(x => -x));
            Assert.Equal(min, query.Min(x => -(int?)x));
        }

        [Theory]
        [OuterLoop]
        [MemberData("MinData", (object)(new int[] { 1024 * 32, 1024 * 1024 }))]
        public static void Min_Int_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int min)
        {
            Min_Int(labeled, count, min);
        }

        [Theory]
        [MemberData("MinData", (object)(new int[] { 1, 2, 16 }))]
        public static void Min_Int_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, int min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(count / 2, query.Select(x => x >= count / 2 ? (int?)x : null).Min());
            Assert.Equal(min, query.Min(x => x >= count / 2 ? -(int?)x : null));
        }

        [Theory]
        [MemberData("MinData", (object)(new int[] { 1, 2, 16 }))]
        public static void Min_Int_AllNull(Labeled<ParallelQuery<int>> labeled, int count, int min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (int?)null).Min());
            Assert.Null(query.Min(x => (int?)null));
        }

        [Theory]
        [MemberData("MinData", (object)(new int[] { 1, 2, 16 }))]
        public static void Min_Long(Labeled<ParallelQuery<int>> labeled, int count, long min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(0, query.Select(x => (long)x).Min());
            Assert.Equal(0, query.Select(x => (long?)x).Min());
            Assert.Equal(min, query.Min(x => -(long)x));
            Assert.Equal(min, query.Min(x => -(long?)x));
        }

        [Theory]
        [OuterLoop]
        [MemberData("MinData", (object)(new int[] { 1024 * 32, 1024 * 1024 }))]
        public static void Min_Long_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, long min)
        {
            Min_Long(labeled, count, min);
        }

        [Theory]
        [MemberData("MinData", (object)(new int[] { 1, 2, 16 }))]
        public static void Min_Long_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, long min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(count / 2, query.Select(x => x >= count / 2 ? (long?)x : null).Min());
            Assert.Equal(min, query.Min(x => x >= count / 2 ? -(long?)x : null));
        }

        [Theory]
        [MemberData("MinData", (object)(new int[] { 1, 2, 16 }))]
        public static void Min_Long_AllNull(Labeled<ParallelQuery<int>> labeled, int count, long min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (long?)null).Min());
            Assert.Null(query.Min(x => (long?)null));
        }

        [Theory]
        [MemberData("MinData", (object)(new int[] { 1, 2, 16 }))]
        public static void Min_Float(Labeled<ParallelQuery<int>> labeled, int count, float min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(0, query.Select(x => (float)x).Min());
            Assert.Equal(0, query.Select(x => (float?)x).Min());
            Assert.Equal(min, query.Min(x => -(float)x));
            Assert.Equal(min, query.Min(x => -(float?)x));
        }

        [Theory]
        [OuterLoop]
        [MemberData("MinData", (object)(new int[] { 1024 * 32, 1024 * 1024 }))]
        public static void Min_Float_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, float min)
        {
            Min_Float(labeled, count, min);
        }

        [Theory]
        [MemberData("MinData", (object)(new int[] { 1, 2, 16 }))]
        public static void Min_Float_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, float min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(count / 2, query.Select(x => x >= count / 2 ? (float?)x : null).Min());
            Assert.Equal(min, query.Min(x => x >= count / 2 ? -(float?)x : null));
        }

        [Theory]
        [MemberData("MinData", (object)(new int[] { 1, 2, 16 }))]
        public static void Min_Float_AllNull(Labeled<ParallelQuery<int>> labeled, int count, float min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (float?)null).Min());
            Assert.Null(query.Min(x => (float?)null));
        }

        [Theory]
        [MemberData("MinData", (object)(new int[] { 1, 2, 16 }))]
        public static void Min_Double(Labeled<ParallelQuery<int>> labeled, int count, double min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(0, query.Select(x => (double)x).Min());
            Assert.Equal(0, query.Select(x => (double?)x).Min());
            Assert.Equal(min, query.Min(x => -(double)x));
            Assert.Equal(min, query.Min(x => -(double?)x));
        }

        [Theory]
        [OuterLoop]
        [MemberData("MinData", (object)(new int[] { 1024 * 32, 1024 * 1024 }))]
        public static void Min_Double_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, double min)
        {
            Min_Double(labeled, count, min);
        }

        [Theory]
        [MemberData("MinData", (object)(new int[] { 1, 2, 16 }))]
        public static void Min_Double_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, double min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(count / 2, query.Select(x => x >= count / 2 ? (double?)x : null).Min());
            Assert.Equal(min, query.Min(x => x >= count / 2 ? -(double?)x : null));
        }

        [Theory]
        [MemberData("MinData", (object)(new int[] { 1, 2, 16 }))]
        public static void Min_Double_AllNull(Labeled<ParallelQuery<int>> labeled, int count, double min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (double?)null).Min());
            Assert.Null(query.Min(x => (double?)null));
        }

        [Theory]
        [MemberData("MinData", (object)(new int[] { 1, 2, 16 }))]
        public static void Min_Decimal(Labeled<ParallelQuery<int>> labeled, int count, decimal min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(0, query.Select(x => (decimal)x).Min());
            Assert.Equal(0, query.Select(x => (decimal?)x).Min());
            Assert.Equal(min, query.Min(x => -(decimal)x));
            Assert.Equal(min, query.Min(x => -(decimal?)x));
        }

        [Theory]
        [OuterLoop]
        [MemberData("MinData", (object)(new int[] { 1024 * 32, 1024 * 1024 }))]
        public static void Min_Decimal_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, decimal min)
        {
            Min_Decimal(labeled, count, min);
        }

        [Theory]
        [MemberData("MinData", (object)(new int[] { 1, 2, 16 }))]
        public static void Min_Decimal_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, decimal min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(count / 2, query.Select(x => x >= count / 2 ? (decimal?)x : null).Min());
            Assert.Equal(min, query.Min(x => x >= count / 2 ? -(decimal?)x : null));
        }

        [Theory]
        [MemberData("MinData", (object)(new int[] { 1, 2, 16 }))]
        public static void Min_Decimal_AllNull(Labeled<ParallelQuery<int>> labeled, int count, decimal min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (decimal?)null).Min());
            Assert.Null(query.Min(x => (decimal?)null));
        }

        [Theory]
        [MemberData("MinData", (object)(new int[] { 1, 2, 16 }))]
        public static void Min_Other(Labeled<ParallelQuery<int>> labeled, int count, int min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(0, query.Select(x => DelgatedComparable.Delegate(x, Comparer<int>.Default)).Min().Value);
            Assert.Equal(count - 1, query.Select(x => DelgatedComparable.Delegate(x, ReverseComparer.Instance)).Min().Value);
        }

        [Theory]
        [OuterLoop]
        [MemberData("MinData", (object)(new int[] { 1024 * 32, 1024 * 1024 }))]
        public static void Min_Other_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int min)
        {
            Min_Other(labeled, count, min);
        }

        [Theory]
        [MemberData("MinData", (object)(new int[] { 1, 2, 16 }))]
        public static void Min_Other_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, int min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(count / 2, query.Min(x => x >= count / 2 ? DelgatedComparable.Delegate(x, Comparer<int>.Default) : null).Value);
            Assert.Equal(count - 1, query.Min(x => x >= count / 2 ? DelgatedComparable.Delegate(x, ReverseComparer.Instance) : null).Value);
        }

        [Theory]
        [MemberData("MinData", (object)(new int[] { 1, 2, 16 }))]
        public static void Min_Other_AllNull(Labeled<ParallelQuery<int>> labeled, int count, int min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (string)null).Min());
            Assert.Null(query.Min(x => (string)null));
        }

        [Fact]
        public static void Min_InvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Min());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Min(x => x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<long>().Min());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<long>().Min(x => x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<float>().Min());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<float>().Min(x => x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<double>().Min());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<double>().Min(x => x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<decimal>().Min());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<decimal>().Min(x => x));
            // Nullables return null when empty
            Assert.Null(ParallelEnumerable.Empty<int?>().Min());
            Assert.Null(ParallelEnumerable.Empty<int?>().Min(x => x));
            Assert.Null(ParallelEnumerable.Empty<long?>().Min());
            Assert.Null(ParallelEnumerable.Empty<long?>().Min(x => x));
            Assert.Null(ParallelEnumerable.Empty<float?>().Min());
            Assert.Null(ParallelEnumerable.Empty<float?>().Min(x => x));
            Assert.Null(ParallelEnumerable.Empty<double?>().Min());
            Assert.Null(ParallelEnumerable.Empty<double?>().Min(x => x));
            Assert.Null(ParallelEnumerable.Empty<decimal?>().Min());
            Assert.Null(ParallelEnumerable.Empty<decimal?>().Min(x => x));
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
        public static void Min_OperationCanceledException_PreCanceled(Labeled<ParallelQuery<int>> labeled, int count)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Min(x => x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Min(x => (int?)x));

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Min(x => (long)x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Min(x => (long?)x));

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Min(x => (float)x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Min(x => (float?)x));

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Min(x => (double)x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Min(x => (double?)x));

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Min(x => (decimal)x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Min(x => (decimal?)x));
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
        public static void Min_AggregateException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Min((Func<int, int>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Min((Func<int, int?>)(x => { throw new DeliberateTestException(); })));

            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Min((Func<int, long>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Min((Func<int, long?>)(x => { throw new DeliberateTestException(); })));

            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Min((Func<int, float>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Min((Func<int, float?>)(x => { throw new DeliberateTestException(); })));

            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Min((Func<int, double>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Min((Func<int, double?>)(x => { throw new DeliberateTestException(); })));

            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Min((Func<int, decimal>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Min((Func<int, decimal?>)(x => { throw new DeliberateTestException(); })));
        }

        [Fact]
        public static void Min_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).Min());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).Min((Func<int, int>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int?>)null).Min());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((int?)0, 1).Min((Func<int?, int?>)null));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<long>)null).Min());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((long)0, 1).Min((Func<long, long>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<long?>)null).Min());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((long?)0, 1).Min((Func<long?, long?>)null));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<float>)null).Min());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((float)0, 1).Min((Func<float, float>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<float?>)null).Min());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((float?)0, 1).Min((Func<float?, float>)null));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<double>)null).Min());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((double)0, 1).Min((Func<double, double>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<double?>)null).Min());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((double?)0, 1).Min((Func<double?, double>)null));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<decimal>)null).Min());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((decimal)0, 1).Min((Func<decimal, decimal>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<decimal?>)null).Min());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((decimal?)0, 1).Min((Func<decimal?, decimal>)null));
        }

        private static string GetMostZeroes(int count)
        {
            // This assumes that 0 itself shouldn't be returned...
            return "1" + string.Join("", Enumerable.Repeat(0, (int)Math.Log10(count)));
        }
    }
}
