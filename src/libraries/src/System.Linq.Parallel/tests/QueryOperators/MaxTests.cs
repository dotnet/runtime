// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class MaxTests
    {
        // Get a set of ranges from 0 to each count, having an extra parameter describing the maximum (count - 1)
        public static IEnumerable<object[]> MaxData(int[] counts)
        {
            counts = counts.DefaultIfEmpty(Sources.OuterLoopCount).ToArray();

            Func<int, int> max = x => x - 1;
            foreach (int count in counts)
            {
                yield return new object[] { Labeled.Label("Default", UnorderedSources.Default(0, count)), count, max(count) };
            }

            // A source with data explicitly created out of order
            foreach (int count in counts)
            {
                int[] data = Enumerable.Range(0, count).ToArray();
                for (int i = 0; i < count / 2; i += 2)
                {
                    int tmp = data[i];
                    data[i] = data[count - i - 1];
                    data[count - i - 1] = tmp;
                }
                yield return new object[] { Labeled.Label("Out-of-order input", data.AsParallel()), count, max(count) };
            }
        }

        //
        // Max
        //

        [Theory]
        [MemberData(nameof(MaxData), new[] { 1, 2, 16 })]
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
        [MemberData(nameof(MaxData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Max_Int_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int max)
        {
            Max_Int(labeled, count, max);
        }

        [Theory]
        [MemberData(nameof(MaxData), new[] { 1, 2, 16 })]
        public static void Max_Int_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, int max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Select(x => x >= count / 2 ? (int?)x : null).Max());
            Assert.Equal(-count / 2, query.Max(x => x >= count / 2 ? -(int?)x : null));
        }

        [Theory]
        [MemberData(nameof(MaxData), new[] { 1, 2, 16 })]
        public static void Max_Int_AllNull(Labeled<ParallelQuery<int>> labeled, int count, int max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (int?)null).Max());
            Assert.Null(query.Max(x => (int?)null));
        }

        [Theory]
        [MemberData(nameof(MaxData), new[] { 1, 2, 16 })]
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
        [MemberData(nameof(MaxData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Max_Long_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, long max)
        {
            Max_Long(labeled, count, max);
        }

        [Theory]
        [MemberData(nameof(MaxData), new[] { 1, 2, 16 })]
        public static void Max_Long_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, long max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Select(x => x >= count / 2 ? (long?)x : null).Max());
            Assert.Equal(-count / 2, query.Max(x => x >= count / 2 ? -(long?)x : null));
        }

        [Theory]
        [MemberData(nameof(MaxData), new[] { 1, 2, 16 })]
        public static void Max_Long_AllNull(Labeled<ParallelQuery<int>> labeled, int count, long max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (long?)null).Max());
            Assert.Null(query.Max(x => (long?)null));
        }

        [Theory]
        [MemberData(nameof(MaxData), new[] { 1, 2, 16 })]
        public static void Max_Float(Labeled<ParallelQuery<int>> labeled, int count, float max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Select(x => (float)x).Max());
            Assert.Equal(max, query.Select(x => (float?)x).Max());
            Assert.Equal(0, query.Max(x => -(float)x));
            Assert.Equal(0, query.Max(x => -(float?)x));
            Assert.Equal(float.PositiveInfinity, query.Select(x => x == count / 2 ? float.PositiveInfinity : x).Max());
            Assert.Equal(float.PositiveInfinity, query.Select(x => x == count / 2 ? (float?)float.PositiveInfinity : x).Max());
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(MaxData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Max_Float_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, float max)
        {
            Max_Float(labeled, count, max);
        }

        [Theory]
        [MemberData(nameof(MaxData), new[] { 1, 2, 16 })]
        public static void Max_Float_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, float max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Select(x => x >= count / 2 ? (float?)x : null).Max());
            Assert.Equal(-count / 2, query.Max(x => x >= count / 2 ? -(float?)x : null));
        }

        [Theory]
        [MemberData(nameof(MaxData), new[] { 1, 2, 16 })]
        public static void Max_Float_AllNull(Labeled<ParallelQuery<int>> labeled, int count, float max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (float?)null).Max());
            Assert.Null(query.Max(x => (float?)null));
        }

        [Theory]
        [MemberData(nameof(MaxData), new[] { 1, 2, 16 })]
        public static void Max_Double(Labeled<ParallelQuery<int>> labeled, int count, double max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Select(x => (double)x).Max());
            Assert.Equal(max, query.Select(x => (double?)x).Max());
            Assert.Equal(0, query.Max(x => -(double)x));
            Assert.Equal(0, query.Max(x => -(double?)x));
            Assert.Equal(double.PositiveInfinity, query.Select(x => x == count / 2 ? double.PositiveInfinity : x).Max());
            Assert.Equal(double.PositiveInfinity, query.Select(x => x == count / 2 ? (double?)double.PositiveInfinity : x).Max());
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(MaxData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Max_Double_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, double max)
        {
            Max_Double(labeled, count, max);
        }

        [Theory]
        [MemberData(nameof(MaxData), new[] { 1, 2, 16 })]
        public static void Max_Double_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, double max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Select(x => x >= count / 2 ? (double?)x : null).Max());
            Assert.Equal(-count / 2, query.Max(x => x >= count / 2 ? -(double?)x : null));
        }

        [Theory]
        [MemberData(nameof(MaxData), new[] { 1, 2, 16 })]
        public static void Max_Double_AllNull(Labeled<ParallelQuery<int>> labeled, int count, double max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (double?)null).Max());
            Assert.Null(query.Max(x => (double?)null));
        }

        [Theory]
        [MemberData(nameof(MaxData), new[] { 1, 2, 16 })]
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
        [MemberData(nameof(MaxData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Max_Decimal_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, decimal max)
        {
            Max_Decimal(labeled, count, max);
        }

        [Theory]
        [MemberData(nameof(MaxData), new[] { 1, 2, 16 })]
        public static void Max_Decimal_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, decimal max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Select(x => x >= count / 2 ? (decimal?)x : null).Max());
            Assert.Equal(-count / 2, query.Max(x => x >= count / 2 ? -(decimal?)x : null));
        }

        [Theory]
        [MemberData(nameof(MaxData), new[] { 1, 2, 16 })]
        public static void Max_Decimal_AllNull(Labeled<ParallelQuery<int>> labeled, int count, decimal max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (decimal?)null).Max());
            Assert.Null(query.Max(x => (decimal?)null));
        }

        [Theory]
        [MemberData(nameof(MaxData), new[] { 1, 2, 16 })]
        public static void Max_Other(Labeled<ParallelQuery<int>> labeled, int count, int max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Select(x => DelgatedComparable.Delegate(x, Comparer<int>.Default)).Max().Value);
            Assert.Equal(0, query.Select(x => DelgatedComparable.Delegate(x, ReverseComparer.Instance)).Max().Value);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(MaxData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Max_Other_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int max)
        {
            Max_Other(labeled, count, max);
        }

        [Theory]
        [MemberData(nameof(MaxData), new[] { 1 })]
        public static void Max_NotComparable(Labeled<ParallelQuery<int>> labeled, int count, int max)
        {
            NotComparable a = new NotComparable(0);
            Assert.Equal(a, labeled.Item.Max(x => a));
        }

        [Theory]
        [MemberData(nameof(MaxData), new[] { 1, 2, 16 })]
        public static void Max_Other_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, int max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(max, query.Max(x => x >= count / 2 ? DelgatedComparable.Delegate(x, Comparer<int>.Default) : null).Value);
            Assert.Equal(count / 2, query.Max(x => x >= count / 2 ? DelgatedComparable.Delegate(x, ReverseComparer.Instance) : null).Value);
        }

        [Theory]
        [MemberData(nameof(MaxData), new[] { 1, 2, 16 })]
        public static void Max_Other_AllNull(Labeled<ParallelQuery<int>> labeled, int count, int max)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (string)null).Max());
            Assert.Null(query.Max(x => (string)null));
        }

        [Fact]
        public static void Max_EmptyNullable()
        {
            Assert.Null(ParallelEnumerable.Empty<int?>().Max());
            Assert.Null(ParallelEnumerable.Empty<long?>().Max());
            Assert.Null(ParallelEnumerable.Empty<float?>().Max());
            Assert.Null(ParallelEnumerable.Empty<double?>().Max());
            Assert.Null(ParallelEnumerable.Empty<decimal?>().Max());
            Assert.Null(ParallelEnumerable.Empty<object>().Max());

            Assert.Null(ParallelEnumerable.Empty<int>().Max(x => (int?)x));
            Assert.Null(ParallelEnumerable.Empty<int>().Max(x => (long?)x));
            Assert.Null(ParallelEnumerable.Empty<int>().Max(x => (float?)x));
            Assert.Null(ParallelEnumerable.Empty<int>().Max(x => (double?)x));
            Assert.Null(ParallelEnumerable.Empty<int>().Max(x => (decimal?)x));
            Assert.Null(ParallelEnumerable.Empty<int>().Max(x => new object()));
        }

        [Fact]
        public static void Max_InvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Max());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<long>().Max());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<float>().Max());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<double>().Max());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<decimal>().Max());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<NotComparable>().Max());

            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Max(x => (int)x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Max(x => (long)x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Max(x => (float)x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Max(x => (double)x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Max(x => (decimal)x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Max(x => new NotComparable(x)));
        }

        [Fact]
        public static void Max_OperationCanceledException()
        {
            AssertThrows.EventuallyCanceled((source, canceler) => source.Max(x => { canceler(); return x; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Max(x => { canceler(); return (int?)x; }));

            AssertThrows.EventuallyCanceled((source, canceler) => source.Max(x => { canceler(); return (long)x; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Max(x => { canceler(); return (long?)x; }));

            AssertThrows.EventuallyCanceled((source, canceler) => source.Max(x => { canceler(); return (float)x; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Max(x => { canceler(); return (float?)x; }));

            AssertThrows.EventuallyCanceled((source, canceler) => source.Max(x => { canceler(); return (double)x; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Max(x => { canceler(); return (double?)x; }));

            AssertThrows.EventuallyCanceled((source, canceler) => source.Max(x => { canceler(); return (decimal)x; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Max(x => { canceler(); return (decimal?)x; }));
        }

        [Fact]
        public static void Max_AggregateException_Wraps_OperationCanceledException()
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Max(x => { canceler(); return x; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Max(x => { canceler(); return (int?)x; }));

            AssertThrows.OtherTokenCanceled((source, canceler) => source.Max(x => { canceler(); return (long)x; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Max(x => { canceler(); return (long?)x; }));

            AssertThrows.OtherTokenCanceled((source, canceler) => source.Max(x => { canceler(); return (float)x; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Max(x => { canceler(); return (float?)x; }));

            AssertThrows.OtherTokenCanceled((source, canceler) => source.Max(x => { canceler(); return (double)x; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Max(x => { canceler(); return (double?)x; }));

            AssertThrows.OtherTokenCanceled((source, canceler) => source.Max(x => { canceler(); return (decimal)x; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Max(x => { canceler(); return (decimal?)x; }));

            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Max(x => { canceler(); return x; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Max(x => { canceler(); return (int?)x; }));

            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Max(x => { canceler(); return (long)x; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Max(x => { canceler(); return (long?)x; }));

            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Max(x => { canceler(); return (float)x; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Max(x => { canceler(); return (float?)x; }));

            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Max(x => { canceler(); return (double)x; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Max(x => { canceler(); return (double?)x; }));

            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Max(x => { canceler(); return (decimal)x; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Max(x => { canceler(); return (decimal?)x; }));
        }

        [Fact]
        public static void Max_OperationCanceledException_PreCanceled()
        {
            AssertThrows.AlreadyCanceled(source => source.Max(x => x));
            AssertThrows.AlreadyCanceled(source => source.Max(x => (int?)x));

            AssertThrows.AlreadyCanceled(source => source.Max(x => (long)x));
            AssertThrows.AlreadyCanceled(source => source.Max(x => (long?)x));

            AssertThrows.AlreadyCanceled(source => source.Max(x => (float)x));
            AssertThrows.AlreadyCanceled(source => source.Max(x => (float?)x));

            AssertThrows.AlreadyCanceled(source => source.Max(x => (double)x));
            AssertThrows.AlreadyCanceled(source => source.Max(x => (double?)x));

            AssertThrows.AlreadyCanceled(source => source.Max(x => (decimal)x));
            AssertThrows.AlreadyCanceled(source => source.Max(x => (decimal?)x));

            AssertThrows.AlreadyCanceled(source => source.Max(x => new NotComparable(x)));
        }

        [Fact]
        public static void Max_AggregateException()
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Max((Func<int, int>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Max((Func<int, int?>)(x => { throw new DeliberateTestException(); })));

            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Max((Func<int, long>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Max((Func<int, long?>)(x => { throw new DeliberateTestException(); })));

            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Max((Func<int, float>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Max((Func<int, float?>)(x => { throw new DeliberateTestException(); })));

            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Max((Func<int, double>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Max((Func<int, double?>)(x => { throw new DeliberateTestException(); })));

            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Max((Func<int, decimal>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Max((Func<int, decimal?>)(x => { throw new DeliberateTestException(); })));

            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Max((Func<int, NotComparable>)(x => { throw new DeliberateTestException(); })));
        }

        [Fact]
        public static void Max_AggregateException_NotComparable()
        {
            ArgumentException e = AssertThrows.Wrapped<ArgumentException>(() => ParallelEnumerable.Repeat(new NotComparable(0), 2).Max());
            Assert.Null(e.ParamName);

            e = AssertThrows.Wrapped<ArgumentException>(() => ParallelEnumerable.Range(0, 2).Max(x => new NotComparable(x)));
            Assert.Null(e.ParamName);
        }

        [Fact]
        public static void Max_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Range(0, 1).Max((Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int?>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((int?)0, 1).Max((Func<int?, int?>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<long>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((long)0, 1).Max((Func<long, long>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<long?>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((long?)0, 1).Max((Func<long?, long?>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<float>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((float)0, 1).Max((Func<float, float>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<float?>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((float?)0, 1).Max((Func<float?, float>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<double>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((double)0, 1).Max((Func<double, double>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<double?>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((double?)0, 1).Max((Func<double?, double>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<decimal>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((decimal)0, 1).Max((Func<decimal, decimal>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<decimal?>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((decimal?)0, 1).Max((Func<decimal?, decimal>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<NotComparable>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat(0, 1).Max((Func<int, NotComparable>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<object>)null).Max());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat(new object(), 1).Max((Func<object, object>)null));
        }
    }
}
