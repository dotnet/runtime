// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public class AverageTests
    {
        //
        // Average
        //

        // Get a set of ranges from 0 to each count, with an extra parameter containing the expected average.
        public static IEnumerable<object[]> AverageData(int[] counts)
        {
            Func<int, double> average = x => (x - 1) / 2.0;
            foreach (object[] results in UnorderedSources.Ranges(counts.Cast<int>(), average)) yield return results;
        }

        [Theory]
        [MemberData("AverageData", (object)(new int[] { 1, 2, 16 }))]
        public static void Average_Int(Labeled<ParallelQuery<int>> labeled, int count, double average)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(average, query.Average());
            Assert.Equal((double?)average, query.Select(x => (int?)x).Average());
            Assert.Equal(-average, query.Average(x => -x));
            Assert.Equal(-(double?)average, query.Average(x => -(int?)x));
        }

        [Theory]
        [OuterLoop]
        [MemberData("AverageData", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }))]
        public static void Average_Int_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, double average)
        {
            Average_Int(labeled, count, average);
        }

        [Theory]
        [MemberData("AverageData", (object)(new int[] { 1, 2, 16 }))]
        public static void Average_Int_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, double average)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(Math.Truncate(average), query.Select(x => (x % 2 == 0) ? (int?)x : null).Average());
            Assert.Equal(Math.Truncate(-average), query.Average(x => (x % 2 == 0) ? -(int?)x : null));
        }

        [Theory]
        [MemberData("AverageData", (object)(new int[] { 1, 2, 16 }))]
        public static void Average_Int_AllNull(Labeled<ParallelQuery<int>> labeled, int count, double average)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (int?)null).Average());
            Assert.Null(query.Average(x => (int?)null));
        }

        [Theory]
        [MemberData("AverageData", (object)(new int[] { 1, 2, 16 }))]
        public static void Average_Long(Labeled<ParallelQuery<int>> labeled, int count, double average)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(average, query.Select(x => (long)x).Average());
            Assert.Equal((double?)average, query.Select(x => (long?)x).Average());
            Assert.Equal(-average, query.Average(x => -(long)x));
            Assert.Equal(-(double?)average, query.Average(x => -(long?)x));
        }

        [Theory]
        [OuterLoop]
        [MemberData("AverageData", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }))]
        public static void Average_Long_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, double average)
        {
            Average_Long(labeled, count, average);
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 2 }), MemberType = typeof(UnorderedSources))]
        public static void Average_Long_Overflow(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Functions.AssertThrowsWrapped<OverflowException>(() => labeled.Item.Select(x => x == 0 ? 1 : long.MaxValue).Average());
            Functions.AssertThrowsWrapped<OverflowException>(() => labeled.Item.Select(x => x == 0 ? (long?)1 : long.MaxValue).Average());
            Functions.AssertThrowsWrapped<OverflowException>(() => labeled.Item.Average(x => x == 0 ? -1 : long.MinValue));
            Functions.AssertThrowsWrapped<OverflowException>(() => labeled.Item.Average(x => x == 0 ? (long?)-1 : long.MinValue));
        }

        [Theory]
        [MemberData("AverageData", (object)(new int[] { 1, 2, 16 }))]
        public static void Average_Long_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, double average)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(Math.Truncate(average), query.Select(x => (x % 2 == 0) ? (long?)x : null).Average());
            Assert.Equal(Math.Truncate(-average), query.Average(x => (x % 2 == 0) ? -(long?)x : null));
        }

        [Theory]
        [MemberData("AverageData", (object)(new int[] { 1, 2, 16 }))]
        public static void Average_Long_AllNull(Labeled<ParallelQuery<int>> labeled, int count, double average)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (long?)null).Average());
            Assert.Null(query.Average(x => (long?)null));
        }

        [Theory]
        [MemberData("AverageData", (object)(new int[] { 1, 2, 16 }))]
        public static void Average_Float(Labeled<ParallelQuery<int>> labeled, int count, float average)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(average, query.Select(x => (float)x).Average());
            Assert.Equal((float?)average, query.Select(x => (float?)x).Average());
            Assert.Equal(-average, query.Average(x => -(float)x));
            Assert.Equal(-(float?)average, query.Average(x => -(float?)x));
        }

        [Theory]
        [OuterLoop]
        [MemberData("AverageData", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }))]
        public static void Average_Float_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, float average)
        {
            Average_Float(labeled, count, average);
        }

        [Theory]
        [MemberData("AverageData", (object)(new int[] { 1, 2, 16 }))]
        public static void Average_Float_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, float average)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal((float?)Math.Truncate(average), query.Select(x => (x % 2 == 0) ? (float?)x : null).Average());
            Assert.Equal((float?)Math.Truncate(-average), query.Average(x => (x % 2 == 0) ? -(float?)x : null));
        }

        [Theory]
        [MemberData("AverageData", (object)(new int[] { 1, 2, 16 }))]
        public static void Average_Float_AllNull(Labeled<ParallelQuery<int>> labeled, int count, float average)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (float?)null).Average());
            Assert.Null(query.Average(x => (float?)null));
        }

        [Theory]
        [MemberData("AverageData", (object)(new int[] { 1, 2, 16 }))]
        public static void Average_Double(Labeled<ParallelQuery<int>> labeled, int count, double average)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(average, query.Select(x => (double)x).Average());
            Assert.Equal((double?)average, query.Select(x => (double?)x).Average());
            Assert.Equal(-average, query.Average(x => -(double)x));
            Assert.Equal(-(double?)average, query.Average(x => -(double?)x));
        }

        [Theory]
        [OuterLoop]
        [MemberData("AverageData", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }))]
        public static void Average_Double_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, double average)
        {
            Average_Double(labeled, count, average);
        }

        [Theory]
        [MemberData("AverageData", (object)(new int[] { 1, 2, 16 }))]
        public static void Average_Double_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, double average)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(Math.Truncate(average), query.Select(x => (x % 2 == 0) ? (double?)x : null).Average());
            Assert.Equal(Math.Truncate(-average), query.Average(x => (x % 2 == 0) ? -(double?)x : null));
        }

        [Theory]
        [MemberData("AverageData", (object)(new int[] { 1, 2, 16 }))]
        public static void Average_Double_AllNull(Labeled<ParallelQuery<int>> labeled, int count, double average)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (double?)null).Average());
            Assert.Null(query.Average(x => (double?)null));
        }

        [Theory]
        [MemberData("AverageData", (object)(new int[] { 1, 2, 16 }))]
        public static void Average_Decimal(Labeled<ParallelQuery<int>> labeled, int count, decimal average)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(average, query.Select(x => (decimal)x).Average());
            Assert.Equal((decimal?)average, query.Select(x => (decimal?)x).Average());
            Assert.Equal(-average, query.Average(x => -(decimal)x));
            Assert.Equal(-(decimal?)average, query.Average(x => -(decimal?)x));
        }

        [Theory]
        [OuterLoop]
        [MemberData("AverageData", (object)(new int[] { 1024 * 1024, 1024 * 1024 * 4 }))]
        public static void Average_Decimal_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, decimal average)
        {
            Average_Decimal(labeled, count, average);
        }

        [Theory]
        [MemberData("AverageData", (object)(new int[] { 1, 2, 16 }))]
        public static void Average_Decimal_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, decimal average)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(Math.Truncate(average), query.Select(x => (x % 2 == 0) ? (decimal?)x : null).Average());
            Assert.Equal(Math.Truncate(-average), query.Average(x => (x % 2 == 0) ? -(decimal?)x : null));
        }

        [Theory]
        [MemberData("AverageData", (object)(new int[] { 1, 2, 16 }))]
        public static void Average_Decimal_AllNull(Labeled<ParallelQuery<int>> labeled, int count, decimal average)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (decimal?)null).Average());
            Assert.Null(query.Average(x => (decimal?)null));
        }

        [Fact]
        public static void Average_InvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Average());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Average(x => x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<long>().Average());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<long>().Average(x => x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<float>().Average());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<float>().Average(x => x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<double>().Average());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<double>().Average(x => x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<decimal>().Average());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<decimal>().Average(x => x));
            // Nullables return null when empty
            Assert.Null(ParallelEnumerable.Empty<int?>().Average());
            Assert.Null(ParallelEnumerable.Empty<int?>().Average(x => x));
            Assert.Null(ParallelEnumerable.Empty<long?>().Average());
            Assert.Null(ParallelEnumerable.Empty<long?>().Average(x => x));
            Assert.Null(ParallelEnumerable.Empty<float?>().Average());
            Assert.Null(ParallelEnumerable.Empty<float?>().Average(x => x));
            Assert.Null(ParallelEnumerable.Empty<double?>().Average());
            Assert.Null(ParallelEnumerable.Empty<double?>().Average(x => x));
            Assert.Null(ParallelEnumerable.Empty<decimal?>().Average());
            Assert.Null(ParallelEnumerable.Empty<decimal?>().Average(x => x));
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
        public static void Average_OperationCanceledException_PreCanceled(Labeled<ParallelQuery<int>> labeled, int count)
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Average(x => x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Average(x => (int?)x));

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Average(x => (long)x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Average(x => (long?)x));

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Average(x => (float)x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Average(x => (float?)x));

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Average(x => (double)x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Average(x => (double?)x));

            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Average(x => (decimal)x));
            Functions.AssertIsCanceled(cs, () => labeled.Item.WithCancellation(cs.Token).Average(x => (decimal?)x));
        }

        [Theory]
        [MemberData("Ranges", (object)(new int[] { 1 }), MemberType = typeof(UnorderedSources))]
        public static void Average_AggregateException(Labeled<ParallelQuery<int>> labeled, int count)
        {
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Average((Func<int, int>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Average((Func<int, int?>)(x => { throw new DeliberateTestException(); })));

            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Average((Func<int, long>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Average((Func<int, long?>)(x => { throw new DeliberateTestException(); })));

            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Average((Func<int, float>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Average((Func<int, float?>)(x => { throw new DeliberateTestException(); })));

            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Average((Func<int, double>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Average((Func<int, double?>)(x => { throw new DeliberateTestException(); })));

            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Average((Func<int, decimal>)(x => { throw new DeliberateTestException(); })));
            Functions.AssertThrowsWrapped<DeliberateTestException>(() => labeled.Item.Average((Func<int, decimal?>)(x => { throw new DeliberateTestException(); })));
        }

        [Fact]
        public static void Average_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int>)null).Average());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat(0, 1).Average((Func<int, int>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<int?>)null).Average());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((int?)0, 1).Average((Func<int?, int?>)null));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<long>)null).Average());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((long)0, 1).Average((Func<long, long>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<long?>)null).Average());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((long?)0, 1).Average((Func<long?, long?>)null));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<float>)null).Average());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((float)0, 1).Average((Func<float, float>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<float?>)null).Average());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((float?)0, 1).Average((Func<float?, float?>)null));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<double>)null).Average());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((double)0, 1).Average((Func<double, double>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<double?>)null).Average());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((double?)0, 1).Average((Func<double?, double?>)null));

            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<decimal>)null).Average());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((decimal)0, 1).Average((Func<decimal, decimal>)null));
            Assert.Throws<ArgumentNullException>(() => ((ParallelQuery<decimal?>)null).Average());
            Assert.Throws<ArgumentNullException>(() => ParallelEnumerable.Repeat((decimal?)0, 1).Average((Func<decimal?, decimal?>)null));
        }
    }
}
