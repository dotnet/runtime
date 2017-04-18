// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class MinTests
    {
        // Get a set of ranges from 0 to each count, with an extra parameter for a minimum where each item is negated (-x).
        public static IEnumerable<object[]> MinData(int[] counts)
        {
            counts = counts.DefaultIfEmpty(Sources.OuterLoopCount).ToArray();

            Func<int, int> min = x => 1 - x;
            foreach (int count in counts)
            {
                yield return new object[] { Labeled.Label("Default", UnorderedSources.Default(count)), count, min(count) };
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
                yield return new object[] { Labeled.Label("Out-of-order input", data.AsParallel()), count, min(count) };
            }
        }

        //
        // Min
        //
        [Theory]
        [MemberData(nameof(MinData), new[] { 1, 2, 16 })]
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
        [MemberData(nameof(MinData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Min_Int_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int min)
        {
            Min_Int(labeled, count, min);
        }

        [Theory]
        [MemberData(nameof(MinData), new[] { 1, 2, 16 })]
        public static void Min_Int_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, int min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(count / 2, query.Select(x => x >= count / 2 ? (int?)x : null).Min());
            Assert.Equal(min, query.Min(x => x >= count / 2 ? -(int?)x : null));
        }

        [Theory]
        [MemberData(nameof(MinData), new[] { 1, 2, 16 })]
        public static void Min_Int_AllNull(Labeled<ParallelQuery<int>> labeled, int count, int min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (int?)null).Min());
            Assert.Null(query.Min(x => (int?)null));
        }

        [Theory]
        [MemberData(nameof(MinData), new[] { 1, 2, 16 })]
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
        [MemberData(nameof(MinData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Min_Long_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, long min)
        {
            Min_Long(labeled, count, min);
        }

        [Theory]
        [MemberData(nameof(MinData), new[] { 1, 2, 16 })]
        public static void Min_Long_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, long min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(count / 2, query.Select(x => x >= count / 2 ? (long?)x : null).Min());
            Assert.Equal(min, query.Min(x => x >= count / 2 ? -(long?)x : null));
        }

        [Theory]
        [MemberData(nameof(MinData), new[] { 1, 2, 16 })]
        public static void Min_Long_AllNull(Labeled<ParallelQuery<int>> labeled, int count, long min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (long?)null).Min());
            Assert.Null(query.Min(x => (long?)null));
        }

        [Theory]
        [MemberData(nameof(MinData), new[] { 1, 2, 16 })]
        public static void Min_Float(Labeled<ParallelQuery<int>> labeled, int count, float min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(0, query.Select(x => (float)x).Min());
            Assert.Equal(0, query.Select(x => (float?)x).Min());
            Assert.Equal(min, query.Min(x => -(float)x));
            Assert.Equal(min, query.Min(x => -(float?)x));
            Assert.Equal(float.NegativeInfinity, query.Select(x => x == count / 2 ? float.NegativeInfinity : x).Min());
            Assert.Equal(float.NegativeInfinity, query.Select(x => x == count / 2 ? (float?)float.NegativeInfinity : x).Min());
            Assert.Equal(float.NaN, query.Select(x => x == count / 2 ? float.NaN : x).Min());
            Assert.Equal(float.NaN, query.Select(x => x == count / 2 ? (float?)float.NaN : x).Min());
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(MinData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Min_Float_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, float min)
        {
            Min_Float(labeled, count, min);
        }

        [Theory]
        [MemberData(nameof(MinData), new[] { 1, 2, 16 })]
        public static void Min_Float_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, float min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(count / 2, query.Select(x => x >= count / 2 ? (float?)x : null).Min());
            Assert.Equal(min, query.Min(x => x >= count / 2 ? -(float?)x : null));
        }

        [Theory]
        [MemberData(nameof(MinData), new[] { 3 })]
        public static void Min_Float_Special(Labeled<ParallelQuery<int>> labeled, int count, float min)
        {
            // Null is defined as 'least' when ordered, but is not the minimum.
            Func<int, float?> translate = x =>
                x % 3 == 0 ? (float?)null :
                x % 3 == 1 ? float.MinValue :
                float.NaN;

            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(float.NaN, query.Select(x => x == count / 2 ? float.NaN : float.MinValue).Min());
            Assert.Equal(float.NaN, query.Select(translate).Min());
        }

        [Theory]
        [MemberData(nameof(MinData), new[] { 1, 2, 16 })]
        public static void Min_Float_AllNull(Labeled<ParallelQuery<int>> labeled, int count, float min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (float?)null).Min());
            Assert.Null(query.Min(x => (float?)null));
        }

        [Theory]
        [MemberData(nameof(MinData), new[] { 1, 2, 16 })]
        public static void Min_Double(Labeled<ParallelQuery<int>> labeled, int count, double min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(0, query.Select(x => (double)x).Min());
            Assert.Equal(0, query.Select(x => (double?)x).Min());
            Assert.Equal(min, query.Min(x => -(double)x));
            Assert.Equal(min, query.Min(x => -(double?)x));
            Assert.Equal(double.NegativeInfinity, query.Select(x => x == count / 2 ? double.NegativeInfinity : x).Min());
            Assert.Equal(double.NegativeInfinity, query.Select(x => x == count / 2 ? (double?)double.NegativeInfinity : x).Min());
            Assert.Equal(double.NaN, query.Select(x => x == count / 2 ? double.NaN : x).Min());
            Assert.Equal(double.NaN, query.Select(x => x == count / 2 ? (double?)double.NaN : x).Min());
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(MinData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Min_Double_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, double min)
        {
            Min_Double(labeled, count, min);
        }

        [Theory]
        [MemberData(nameof(MinData), new[] { 3 })]
        public static void Min_Double_Special(Labeled<ParallelQuery<int>> labeled, int count, double min)
        {
            // Null is defined as 'least' when ordered, but is not the minimum.
            Func<int, double?> translate = x =>
                x % 3 == 0 ? (double?)null :
                x % 3 == 1 ? double.MinValue :
                double.NaN;

            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(double.NaN, query.Select(x => x == count / 2 ? double.NaN : double.MinValue).Min());
            Assert.Equal(double.NaN, query.Select(translate).Min());
        }

        [Theory]
        [MemberData(nameof(MinData), new[] { 1, 2, 16 })]
        public static void Min_Double_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, double min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(count / 2, query.Select(x => x >= count / 2 ? (double?)x : null).Min());
            Assert.Equal(min, query.Min(x => x >= count / 2 ? -(double?)x : null));
        }

        [Theory]
        [MemberData(nameof(MinData), new[] { 1, 2, 16 })]
        public static void Min_Double_AllNull(Labeled<ParallelQuery<int>> labeled, int count, double min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (double?)null).Min());
            Assert.Null(query.Min(x => (double?)null));
        }

        [Theory]
        [MemberData(nameof(MinData), new[] { 1, 2, 16 })]
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
        [MemberData(nameof(MinData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Min_Decimal_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, decimal min)
        {
            Min_Decimal(labeled, count, min);
        }

        [Theory]
        [MemberData(nameof(MinData), new[] { 1, 2, 16 })]
        public static void Min_Decimal_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, decimal min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(count / 2, query.Select(x => x >= count / 2 ? (decimal?)x : null).Min());
            Assert.Equal(min, query.Min(x => x >= count / 2 ? -(decimal?)x : null));
        }

        [Theory]
        [MemberData(nameof(MinData), new[] { 1, 2, 16 })]
        public static void Min_Decimal_AllNull(Labeled<ParallelQuery<int>> labeled, int count, decimal min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (decimal?)null).Min());
            Assert.Null(query.Min(x => (decimal?)null));
        }

        [Theory]
        [MemberData(nameof(MinData), new[] { 1, 2, 16 })]
        public static void Min_Other(Labeled<ParallelQuery<int>> labeled, int count, int min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(0, query.Select(x => DelgatedComparable.Delegate(x, Comparer<int>.Default)).Min().Value);
            Assert.Equal(count - 1, query.Select(x => DelgatedComparable.Delegate(x, ReverseComparer.Instance)).Min().Value);
        }

        [Theory]
        [OuterLoop]
        [MemberData(nameof(MinData), new int[] { /* Sources.OuterLoopCount */ })]
        public static void Min_Other_Longrunning(Labeled<ParallelQuery<int>> labeled, int count, int min)
        {
            Min_Other(labeled, count, min);
        }

        [Theory]
        [MemberData(nameof(MinData), new[] { 1 })]
        public static void Min_NotComparable(Labeled<ParallelQuery<int>> labeled, int count, int min)
        {
            NotComparable a = new NotComparable(0);
            Assert.Equal(a, labeled.Item.Min(x => a));
        }

        [Theory]
        [MemberData(nameof(MinData), new[] { 1, 2, 16 })]
        public static void Min_Other_SomeNull(Labeled<ParallelQuery<int>> labeled, int count, int min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Equal(count / 2, query.Min(x => x >= count / 2 ? DelgatedComparable.Delegate(x, Comparer<int>.Default) : null).Value);
            Assert.Equal(count - 1, query.Min(x => x >= count / 2 ? DelgatedComparable.Delegate(x, ReverseComparer.Instance) : null).Value);
        }

        [Theory]
        [MemberData(nameof(MinData), new[] { 1, 2, 16 })]
        public static void Min_Other_AllNull(Labeled<ParallelQuery<int>> labeled, int count, int min)
        {
            ParallelQuery<int> query = labeled.Item;
            Assert.Null(query.Select(x => (string)null).Min());
            Assert.Null(query.Min(x => (string)null));
        }

        [Fact]
        public static void Min_EmptyNullable()
        {
            Assert.Null(ParallelEnumerable.Empty<int?>().Min());
            Assert.Null(ParallelEnumerable.Empty<long?>().Min());
            Assert.Null(ParallelEnumerable.Empty<float?>().Min());
            Assert.Null(ParallelEnumerable.Empty<double?>().Min());
            Assert.Null(ParallelEnumerable.Empty<decimal?>().Min());
            Assert.Null(ParallelEnumerable.Empty<object>().Min());

            Assert.Null(ParallelEnumerable.Empty<int>().Min(x => (int?)x));
            Assert.Null(ParallelEnumerable.Empty<int>().Min(x => (long?)x));
            Assert.Null(ParallelEnumerable.Empty<int>().Min(x => (float?)x));
            Assert.Null(ParallelEnumerable.Empty<int>().Min(x => (double?)x));
            Assert.Null(ParallelEnumerable.Empty<int>().Min(x => (decimal?)x));
            Assert.Null(ParallelEnumerable.Empty<int>().Min(x => new object()));
        }

        [Fact]
        public static void Min_InvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Min());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<long>().Min());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<float>().Min());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<double>().Min());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<decimal>().Min());
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<NotComparable>().Min());

            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Min(x => (int)x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Min(x => (long)x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Min(x => (float)x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Min(x => (double)x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Min(x => (decimal)x));
            Assert.Throws<InvalidOperationException>(() => ParallelEnumerable.Empty<int>().Min(x => new NotComparable(x)));
        }

        [Fact]
        public static void Min_OperationCanceledException()
        {
            AssertThrows.EventuallyCanceled((source, canceler) => source.Min(x => { canceler(); return x; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Min(x => { canceler(); return (int?)x; }));

            AssertThrows.EventuallyCanceled((source, canceler) => source.Min(x => { canceler(); return (long)x; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Min(x => { canceler(); return (long?)x; }));

            AssertThrows.EventuallyCanceled((source, canceler) => source.Min(x => { canceler(); return (float)x; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Min(x => { canceler(); return (float?)x; }));

            AssertThrows.EventuallyCanceled((source, canceler) => source.Min(x => { canceler(); return (double)x; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Min(x => { canceler(); return (double?)x; }));

            AssertThrows.EventuallyCanceled((source, canceler) => source.Min(x => { canceler(); return (decimal)x; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Min(x => { canceler(); return (decimal?)x; }));
        }

        [Fact]
        public static void Min_AggregateException_Wraps_OperationCanceledException()
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Min(x => { canceler(); return x; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Min(x => { canceler(); return (int?)x; }));

            AssertThrows.OtherTokenCanceled((source, canceler) => source.Min(x => { canceler(); return (long)x; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Min(x => { canceler(); return (long?)x; }));

            AssertThrows.OtherTokenCanceled((source, canceler) => source.Min(x => { canceler(); return (float)x; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Min(x => { canceler(); return (float?)x; }));

            AssertThrows.OtherTokenCanceled((source, canceler) => source.Min(x => { canceler(); return (double)x; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Min(x => { canceler(); return (double?)x; }));

            AssertThrows.OtherTokenCanceled((source, canceler) => source.Min(x => { canceler(); return (decimal)x; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Min(x => { canceler(); return (decimal?)x; }));

            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Min(x => { canceler(); return x; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Min(x => { canceler(); return (int?)x; }));

            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Min(x => { canceler(); return (long)x; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Min(x => { canceler(); return (long?)x; }));

            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Min(x => { canceler(); return (float)x; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Min(x => { canceler(); return (float?)x; }));

            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Min(x => { canceler(); return (double)x; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Min(x => { canceler(); return (double?)x; }));

            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Min(x => { canceler(); return (decimal)x; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Min(x => { canceler(); return (decimal?)x; }));
        }

        [Fact]
        public static void Min_OperationCanceledException_PreCanceled()
        {
            AssertThrows.AlreadyCanceled(source => source.Min(x => x));
            AssertThrows.AlreadyCanceled(source => source.Min(x => (int?)x));

            AssertThrows.AlreadyCanceled(source => source.Min(x => (long)x));
            AssertThrows.AlreadyCanceled(source => source.Min(x => (long?)x));

            AssertThrows.AlreadyCanceled(source => source.Min(x => (float)x));
            AssertThrows.AlreadyCanceled(source => source.Min(x => (float?)x));

            AssertThrows.AlreadyCanceled(source => source.Min(x => (double)x));
            AssertThrows.AlreadyCanceled(source => source.Min(x => (double?)x));

            AssertThrows.AlreadyCanceled(source => source.Min(x => (decimal)x));
            AssertThrows.AlreadyCanceled(source => source.Min(x => (decimal?)x));

            AssertThrows.AlreadyCanceled(source => source.Min(x => new NotComparable(x)));
        }

        [Fact]
        public static void Min_AggregateException()
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Min((Func<int, int>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Min((Func<int, int?>)(x => { throw new DeliberateTestException(); })));

            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Min((Func<int, long>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Min((Func<int, long?>)(x => { throw new DeliberateTestException(); })));

            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Min((Func<int, float>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Min((Func<int, float?>)(x => { throw new DeliberateTestException(); })));

            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Min((Func<int, double>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Min((Func<int, double?>)(x => { throw new DeliberateTestException(); })));

            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Min((Func<int, decimal>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Min((Func<int, decimal?>)(x => { throw new DeliberateTestException(); })));

            AssertThrows.Wrapped<DeliberateTestException>(() => ParallelEnumerable.Range(0, 1).Min((Func<int, NotComparable>)(x => { throw new DeliberateTestException(); })));
        }

        [Fact]
        public static void Min_AggregateException_NotComparable()
        {
            ArgumentException e = AssertThrows.Wrapped<ArgumentException>(() => ParallelEnumerable.Repeat(new NotComparable(0), 2).Min());
            Assert.Null(e.ParamName);

            e = AssertThrows.Wrapped<ArgumentException>(() => ParallelEnumerable.Range(0, 2).Min(x => new NotComparable(x)));
            Assert.Null(e.ParamName);
        }

        [Fact]
        public static void Min_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Range(0, 1).Min((Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int?>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((int?)0, 1).Min((Func<int?, int?>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<long>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((long)0, 1).Min((Func<long, long>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<long?>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((long?)0, 1).Min((Func<long?, long?>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<float>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((float)0, 1).Min((Func<float, float>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<float?>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((float?)0, 1).Min((Func<float?, float>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<double>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((double)0, 1).Min((Func<double, double>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<double?>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((double?)0, 1).Min((Func<double?, double>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<decimal>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((decimal)0, 1).Min((Func<decimal, decimal>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<decimal?>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((decimal?)0, 1).Min((Func<decimal?, decimal>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<NotComparable>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat(0, 1).Min((Func<int, NotComparable>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<object>)null).Min());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat(new object(), 1).Min((Func<object, object>)null));
        }
    }
}
