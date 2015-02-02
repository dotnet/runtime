// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace Test
{
    public class MaxTests
    {
        public static IEnumerable<object[]> MaxData(object[] counts)
        {
            Func<int, int> max = x => x - 1;
            foreach (object[] results in UnorderedSources.Ranges(counts.Cast<int>(), max)) yield return results;
        }

        //
        // Max
        //

        [Theory]
        [MemberData("MaxData", (object)(new int[] { 1, 2, 16 }))]
        public static void Max_Int(Labeled<ParallelQuery<int>> labeled, int count, int max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Max());
            Assert.Equal(max, query.Select(x => (int?)x).Max());
            Assert.Equal(0, query.Max(x => -x));
            Assert.Equal(0, query.Max(x => -(int?)x));
        }

        [Theory]
        [OuterLoop]
        [MemberData("MaxData", (object)(new int[] { 1024 * 32, 1024 * 1024 }))]
        public static void Max_Int_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int max)
        {
            Max_Int(labeled, count, max);
        }

        [Theory]
        [MemberData("MaxData", (object)(new int[] { 1, 2, 16 }))]
        public static void Max_Int_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, int max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Select(x => x >= count / 2 ? (int?)x : null).Max());
            Assert.Equal(-count / 2, query.Max(x => x >= count / 2 ? -(int?)x : null));
        }

        [Theory]
        [MemberData("MaxData", (object)(new int[] { 1, 2, 16 }))]
        public static void Max_Int_AllNull(Labeled<ParallelQuery<int>> labeled, int count, int max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (int?)null).Max());
            Assert.Null(query.Max(x => (int?)null));
        }

        [Theory]
        [MemberData("MaxData", (object)(new int[] { 1, 2, 16 }))]
        public static void Max_Long(Labeled<ParallelQuery<int>> labeled, int count, long max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Select(x => (long)x).Max());
            Assert.Equal(max, query.Select(x => (long?)x).Max());
            Assert.Equal(0, query.Max(x => -(long)x));
            Assert.Equal(0, query.Max(x => -(long?)x));
        }

        [Theory]
        [OuterLoop]
        [MemberData("MaxData", (object)(new int[] { 1024 * 32, 1024 * 1024 }))]
        public static void Max_Long_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, long max)
        {
            Max_Long(labeled, count, max);
        }

        [Theory]
        [MemberData("MaxData", (object)(new int[] { 1, 2, 16 }))]
        public static void Max_Long_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, long max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Select(x => x >= count / 2 ? (long?)x : null).Max());
            Assert.Equal(-count / 2, query.Max(x => x >= count / 2 ? -(long?)x : null));
        }

        [Theory]
        [MemberData("MaxData", (object)(new int[] { 1, 2, 16 }))]
        public static void Max_Long_AllNull(Labeled<ParallelQuery<int>> labeled, int count, long max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (long?)null).Max());
            Assert.Null(query.Max(x => (long?)null));
        }

        [Theory]
        [MemberData("MaxData", (object)(new int[] { 1, 2, 16 }))]
        public static void Max_Float(Labeled<ParallelQuery<int>> labeled, int count, float max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Select(x => (float)x).Max());
            Assert.Equal(max, query.Select(x => (float?)x).Max());
            Assert.Equal(0, query.Max(x => -(float)x));
            Assert.Equal(0, query.Max(x => -(float?)x));
        }

        [Theory]
        [OuterLoop]
        [MemberData("MaxData", (object)(new int[] { 1024 * 32, 1024 * 1024 }))]
        public static void Max_Float_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, float max)
        {
            Max_Float(labeled, count, max);
        }

        [Theory]
        [MemberData("MaxData", (object)(new int[] { 1, 2, 16 }))]
        public static void Max_Float_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, float max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Select(x => x >= count / 2 ? (float?)x : null).Max());
            Assert.Equal(-count / 2, query.Max(x => x >= count / 2 ? -(float?)x : null));
        }

        [Theory]
        [MemberData("MaxData", (object)(new int[] { 1, 2, 16 }))]
        public static void Max_Float_AllNull(Labeled<ParallelQuery<int>> labeled, int count, float max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (float?)null).Max());
            Assert.Null(query.Max(x => (float?)null));
        }

        [Theory]
        [MemberData("MaxData", (object)(new int[] { 1, 2, 16 }))]
        public static void Max_Double(Labeled<ParallelQuery<int>> labeled, int count, double max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Select(x => (double)x).Max());
            Assert.Equal(max, query.Select(x => (double?)x).Max());
            Assert.Equal(0, query.Max(x => -(double)x));
            Assert.Equal(0, query.Max(x => -(double?)x));
        }

        [Theory]
        [OuterLoop]
        [MemberData("MaxData", (object)(new int[] { 1024 * 32, 1024 * 1024 }))]
        public static void Max_Double_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, double max)
        {
            Max_Double(labeled, count, max);
        }

        [Theory]
        [MemberData("MaxData", (object)(new int[] { 1, 2, 16 }))]
        public static void Max_Double_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, double max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Select(x => x >= count / 2 ? (double?)x : null).Max());
            Assert.Equal(-count / 2, query.Max(x => x >= count / 2 ? -(double?)x : null));
        }

        [Theory]
        [MemberData("MaxData", (object)(new int[] { 1, 2, 16 }))]
        public static void Max_Double_AllNull(Labeled<ParallelQuery<int>> labeled, int count, double max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (double?)null).Max());
            Assert.Null(query.Max(x => (double?)null));
        }

        [Theory]
        [MemberData("MaxData", (object)(new int[] { 1, 2, 16 }))]
        public static void Max_Decimal(Labeled<ParallelQuery<int>> labeled, int count, decimal max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Select(x => (decimal)x).Max());
            Assert.Equal(max, query.Select(x => (decimal?)x).Max());
            Assert.Equal(0, query.Max(x => -(decimal)x));
            Assert.Equal(0, query.Max(x => -(decimal?)x));
        }

        [Theory]
        [OuterLoop]
        [MemberData("MaxData", (object)(new int[] { 1024 * 32, 1024 * 1024 }))]
        public static void Max_Decimal_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, decimal max)
        {
            Max_Decimal(labeled, count, max);
        }

        [Theory]
        [MemberData("MaxData", (object)(new int[] { 1, 2, 16 }))]
        public static void Max_Decimal_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, decimal max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Select(x => x >= count / 2 ? (decimal?)x : null).Max());
            Assert.Equal(-count / 2, query.Max(x => x >= count / 2 ? -(decimal?)x : null));
        }

        [Theory]
        [MemberData("MaxData", (object)(new int[] { 1, 2, 16 }))]
        public static void Max_Decimal_AllNull(Labeled<ParallelQuery<int>> labeled, int count, decimal max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (decimal?)null).Max());
            Assert.Null(query.Max(x => (decimal?)null));
        }

        [Theory]
        [MemberData("MaxData", (object)(new int[] { 1, 2, 16 }))]
        public static void Max_Other(Labeled<ParallelQuery<int>> labeled, int count, int max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Select(x => DelgatedComparable.Delegate(x, Comparer<int>.Default)).Max().Value);
            Assert.Equal(0, query.Select(x => DelgatedComparable.Delegate(x, ReverseComparer.Instance)).Max().Value);
        }

        [Theory]
        [OuterLoop]
        [MemberData("MaxData", (object)(new int[] { 1024 * 32, 1024 * 1024 }))]
        public static void Max_Other_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int max)
        {
            Max_Other(labeled, count, max);
        }

        [Theory]
        [MemberData("MaxData", (object)(new int[] { 1, 2, 16 }))]
        public static void Max_Other_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, int max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Max(x => x >= count / 2 ? DelgatedComparable.Delegate(x, Comparer<int>.Default) : null).Value);
            Assert.Equal(count / 2, query.Max(x => x >= count / 2 ? DelgatedComparable.Delegate(x, ReverseComparer.Instance) : null).Value);
        }

        [Theory]
        [MemberData("MaxData", (object)(new int[] { 1, 2, 16 }))]
        public static void Max_Other_AllNull(Labeled<ParallelQuery<int>> labeled, int count, int max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (string)null).Max());
            Assert.Null(query.Max(x => (string)null));
        }

        [Fact]
        public static void Max_InvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Max());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Max(x => x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<long>().Max());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<long>().Max(x => x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<float>().Max());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<float>().Max(x => x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<double>().Max());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<double>().Max(x => x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<decimal>().Max());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<decimal>().Max(x => x));
            // Nullables return null when empty
            Assert.Null(ParallelEnumerable.Empty<int?>().Max());
            Assert.Null(ParallelEnumerable.Empty<int?>().Max(x => x));
            Assert.Null(ParallelEnumerable.Empty<long?>().Max());
            Assert.Null(ParallelEnumerable.Empty<long?>().Max(x => x));
            Assert.Null(ParallelEnumerable.Empty<float?>().Max());
            Assert.Null(ParallelEnumerable.Empty<float?>().Max(x => x));
            Assert.Null(ParallelEnumerable.Empty<double?>().Max());
            Assert.Null(ParallelEnumerable.Empty<double?>().Max(x => x));
            Assert.Null(ParallelEnumerable.Empty<decimal?>().Max());
            Assert.Null(ParallelEnumerable.Empty<decimal?>().Max(x => x));
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
        public static void Max_OperationCanceledException_PreCanceled(Labeled<ParallelQuery<int>> labeled, int count)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Max(x => x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Max(x => (int?)x));

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Max(x => (long)x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Max(x => (long?)x));

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Max(x => (float)x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Max(x => (float?)x));

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Max(x => (double)x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Max(x => (double?)x));

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Max(x => (decimal)x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Max(x => (decimal?)x));
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
        public static void Max_AggregateException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Max((Func<int, int>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Max((Func<int, int?>)(x => { throw new DeliberateTestException(); })));

            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Max((Func<int, long>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Max((Func<int, long?>)(x => { throw new DeliberateTestException(); })));

            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Max((Func<int, float>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Max((Func<int, float?>)(x => { throw new DeliberateTestException(); })));

            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Max((Func<int, double>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Max((Func<int, double?>)(x => { throw new DeliberateTestException(); })));

            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Max((Func<int, decimal>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Max((Func<int, decimal?>)(x => { throw new DeliberateTestException(); })));
        }

        [Fact]
        public static void Max_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).Max());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Range(0, 1).Max((Func<int, int>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int?>)null).Max());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((int?)0, 1).Max((Func<int?, int?>)null));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<long>)null).Max());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((long)0, 1).Max((Func<long, long>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<long?>)null).Max());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((long?)0, 1).Max((Func<long?, long?>)null));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<float>)null).Max());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((float)0, 1).Max((Func<float, float>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<float?>)null).Max());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((float?)0, 1).Max((Func<float?, float>)null));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<double>)null).Max());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((double)0, 1).Max((Func<double, double>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<double?>)null).Max());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((double?)0, 1).Max((Func<double?, double>)null));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<decimal>)null).Max());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((decimal)0, 1).Max((Func<decimal, decimal>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<decimal?>)null).Max());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((decimal?)0, 1).Max((Func<decimal?, decimal>)null));
        }

        private static string GetMostNines(int max)
        {
            if (max < 10)
            {
                return max.ToString();
            }

            // This fails if the maximum's most significant digit is (the text representation starts with) a '9'
            return string.Join("", Enumerable.Repeat(9, (int)Math.Log10(max)));
        }
    }
}
