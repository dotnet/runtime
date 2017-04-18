// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    public static class AverageTests
    {
        //
        // Average
        //

        // Get a set of ranges from 0 to each count, with an extra parameter containing the expected average.
        public static IEnumerable<object[]> AverageData(int[] counts)
        {
            foreach (int count in counts)
            {
                yield return new object[] { count, (count - 1) / 2.0 };
            }
        }

        [Theory]
        [MemberData(nameof(AverageData), new[] { 1, 2, 16 })]
        public static void Average_Int(int count, double average)
        {
            Assert.Equal(average, UnorderedSources.Default(count).Average());
            Assert.Equal((double?)average, UnorderedSources.Default(count).Select(x => (int?)x).Average());
            Assert.Equal(-average, UnorderedSources.Default(count).Average(x => -x));
            Assert.Equal(-(double?)average, UnorderedSources.Default(count).Average(x => -(int?)x));
        }

        [Fact]
        [OuterLoop]
        public static void Average_Int_Longrunning()
        {
            Average_Int(Sources.OuterLoopCount, (Sources.OuterLoopCount - 1) / 2.0);
        }

        [Theory]
        [MemberData(nameof(AverageData), new[] { 1, 2, 16 })]
        public static void Average_Int_SomeNull(int count, double average)
        {
            Assert.Equal(Math.Truncate(average), UnorderedSources.Default(count).Select(x => (x % 2 == 0) ? (int?)x : null).Average());
            Assert.Equal(Math.Truncate(-average), UnorderedSources.Default(count).Average(x => (x % 2 == 0) ? -(int?)x : null));
        }

        [Theory]
        [MemberData(nameof(AverageData), new[] { 1, 2, 16 })]
        public static void Average_Int_AllNull(int count, double average)
        {
            Assert.Null(UnorderedSources.Default(count).Select(x => (int?)null).Average());
            Assert.Null(UnorderedSources.Default(count).Average(x => (int?)null));
        }

        [Theory]
        [MemberData(nameof(AverageData), new[] { 1, 2, 16 })]
        public static void Average_Long(int count, double average)
        {
            Assert.Equal(average, UnorderedSources.Default(count).Select(x => (long)x).Average());
            Assert.Equal((double?)average, UnorderedSources.Default(count).Select(x => (long?)x).Average());
            Assert.Equal(-average, UnorderedSources.Default(count).Average(x => -(long)x));
            Assert.Equal(-(double?)average, UnorderedSources.Default(count).Average(x => -(long?)x));
        }

        [Fact]
        [OuterLoop]
        public static void Average_Long_Longrunning()
        {
            Average_Long(Sources.OuterLoopCount, (Sources.OuterLoopCount - 1) / 2.0);
        }

        [Fact]
        public static void Average_Long_Overflow()
        {
            AssertThrows.Wrapped<OverflowException>(() => UnorderedSources.Default(2).Select(x => x == 0 ? 1 : long.MaxValue).Average());
            AssertThrows.Wrapped<OverflowException>(() => UnorderedSources.Default(2).Select(x => x == 0 ? (long?)1 : long.MaxValue).Average());
            AssertThrows.Wrapped<OverflowException>(() => UnorderedSources.Default(2).Average(x => x == 0 ? -1 : long.MinValue));
            AssertThrows.Wrapped<OverflowException>(() => UnorderedSources.Default(2).Average(x => x == 0 ? (long?)-1 : long.MinValue));
        }

        [Theory]
        [MemberData(nameof(AverageData), new[] { 1, 2, 16 })]
        public static void Average_Long_SomeNull(int count, double average)
        {
            Assert.Equal(Math.Truncate(average), UnorderedSources.Default(count).Select(x => (x % 2 == 0) ? (long?)x : null).Average());
            Assert.Equal(Math.Truncate(-average), UnorderedSources.Default(count).Average(x => (x % 2 == 0) ? -(long?)x : null));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Average_Long_AllNull(int count)
        {
            Assert.Null(UnorderedSources.Default(count).Select(x => (long?)null).Average());
            Assert.Null(UnorderedSources.Default(count).Average(x => (long?)null));
        }

        [Theory]
        [MemberData(nameof(AverageData), new[] { 1, 2, 16 })]
        public static void Average_Float(int count, float average)
        {
            Assert.Equal(average, UnorderedSources.Default(count).Select(x => (float)x).Average());
            Assert.Equal((float?)average, UnorderedSources.Default(count).Select(x => (float?)x).Average());
            Assert.Equal(-average, UnorderedSources.Default(count).Average(x => -(float)x));
            Assert.Equal(-(float?)average, UnorderedSources.Default(count).Average(x => -(float?)x));
        }

        [Fact]
        [OuterLoop]
        public static void Average_Float_Longrunning()
        {
            Average_Float(Sources.OuterLoopCount, (Sources.OuterLoopCount - 1) / 2.0F);
        }

        [Theory]
        [MemberData(nameof(AverageData), new[] { 1, 2, 16 })]
        public static void Average_Float_SomeNull(int count, float average)
        {
            Assert.Equal((float?)Math.Truncate(average), UnorderedSources.Default(count).Select(x => (x % 2 == 0) ? (float?)x : null).Average());
            Assert.Equal((float?)Math.Truncate(-average), UnorderedSources.Default(count).Average(x => (x % 2 == 0) ? -(float?)x : null));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Average_Float_AllNull(int count)
        {
            Assert.Null(UnorderedSources.Default(count).Select(x => (float?)null).Average());
            Assert.Null(UnorderedSources.Default(count).Average(x => (float?)null));
        }

        [Theory]
        [MemberData(nameof(AverageData), new[] { 1, 2, 16 })]
        public static void Average_Double(int count, double average)
        {
            Assert.Equal(average, UnorderedSources.Default(count).Select(x => (double)x).Average());
            Assert.Equal((double?)average, UnorderedSources.Default(count).Select(x => (double?)x).Average());
            Assert.Equal(-average, UnorderedSources.Default(count).Average(x => -(double)x));
            Assert.Equal(-(double?)average, UnorderedSources.Default(count).Average(x => -(double?)x));
        }

        [Fact]
        [OuterLoop]
        public static void Average_Double_Longrunning()
        {
            Average_Double(Sources.OuterLoopCount, (Sources.OuterLoopCount - 1) / 2.0);
        }

        [Theory]
        [MemberData(nameof(AverageData), new[] { 1, 2, 16 })]
        public static void Average_Double_SomeNull(int count, double average)
        {
            Assert.Equal(Math.Truncate(average), UnorderedSources.Default(count).Select(x => (x % 2 == 0) ? (double?)x : null).Average());
            Assert.Equal(Math.Truncate(-average), UnorderedSources.Default(count).Average(x => (x % 2 == 0) ? -(double?)x : null));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Average_Double_AllNull(int count)
        {
            Assert.Null(UnorderedSources.Default(count).Select(x => (double?)null).Average());
            Assert.Null(UnorderedSources.Default(count).Average(x => (double?)null));
        }

        [Theory]
        [MemberData(nameof(AverageData), new[] { 1, 2, 16 })]
        public static void Average_Decimal(int count, decimal average)
        {
            Assert.Equal(average, UnorderedSources.Default(count).Select(x => (decimal)x).Average());
            Assert.Equal((decimal?)average, UnorderedSources.Default(count).Select(x => (decimal?)x).Average());
            Assert.Equal(-average, UnorderedSources.Default(count).Average(x => -(decimal)x));
            Assert.Equal(-(decimal?)average, UnorderedSources.Default(count).Average(x => -(decimal?)x));
        }

        [Fact]
        [OuterLoop]
        public static void Average_Decimal_Longrunning()
        {
            Average_Decimal(Sources.OuterLoopCount, (Sources.OuterLoopCount - 1) / 2.0M);
        }

        [Theory]
        [MemberData(nameof(AverageData), new[] { 1, 2, 16 })]
        public static void Average_Decimal_SomeNull(int count, decimal average)
        {
            Assert.Equal(Math.Truncate(average), UnorderedSources.Default(count).Select(x => (x % 2 == 0) ? (decimal?)x : null).Average());
            Assert.Equal(Math.Truncate(-average), UnorderedSources.Default(count).Average(x => (x % 2 == 0) ? -(decimal?)x : null));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(16)]
        public static void Average_Decimal_AllNull(int count)
        {
            Assert.Null(UnorderedSources.Default(count).Select(x => (decimal?)null).Average());
            Assert.Null(UnorderedSources.Default(count).Average(x => (decimal?)null));
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

        [Fact]
        public static void Average_OperationCanceledException()
        {
            AssertThrows.EventuallyCanceled((source, canceler) => source.Average(x => { canceler(); return x; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Average(x => { canceler(); return (int?)x; }));

            AssertThrows.EventuallyCanceled((source, canceler) => source.Average(x => { canceler(); return (long)x; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Average(x => { canceler(); return (long?)x; }));

            AssertThrows.EventuallyCanceled((source, canceler) => source.Average(x => { canceler(); return (float)x; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Average(x => { canceler(); return (float?)x; }));

            AssertThrows.EventuallyCanceled((source, canceler) => source.Average(x => { canceler(); return (double)x; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Average(x => { canceler(); return (double?)x; }));

            AssertThrows.EventuallyCanceled((source, canceler) => source.Average(x => { canceler(); return (decimal)x; }));
            AssertThrows.EventuallyCanceled((source, canceler) => source.Average(x => { canceler(); return (decimal?)x; }));
        }

        [Fact]
        public static void Average_AggregateException_Wraps_OperationCanceledException()
        {
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Average(x => { canceler(); return x; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Average(x => { canceler(); return (int?)x; }));

            AssertThrows.OtherTokenCanceled((source, canceler) => source.Average(x => { canceler(); return (long)x; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Average(x => { canceler(); return (long?)x; }));

            AssertThrows.OtherTokenCanceled((source, canceler) => source.Average(x => { canceler(); return (float)x; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Average(x => { canceler(); return (float?)x; }));

            AssertThrows.OtherTokenCanceled((source, canceler) => source.Average(x => { canceler(); return (double)x; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Average(x => { canceler(); return (double?)x; }));

            AssertThrows.OtherTokenCanceled((source, canceler) => source.Average(x => { canceler(); return (decimal)x; }));
            AssertThrows.OtherTokenCanceled((source, canceler) => source.Average(x => { canceler(); return (decimal?)x; }));

            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Average(x => { canceler(); return x; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Average(x => { canceler(); return (int?)x; }));

            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Average(x => { canceler(); return (long)x; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Average(x => { canceler(); return (long?)x; }));

            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Average(x => { canceler(); return (float)x; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Average(x => { canceler(); return (float?)x; }));

            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Average(x => { canceler(); return (double)x; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Average(x => { canceler(); return (double?)x; }));

            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Average(x => { canceler(); return (decimal)x; }));
            AssertThrows.SameTokenNotCanceled((source, canceler) => source.Average(x => { canceler(); return (decimal?)x; }));
        }

        [Fact]
        public static void Average_OperationCanceledException_PreCanceled()
        {
            AssertThrows.AlreadyCanceled(source => source.Average(x => x));
            AssertThrows.AlreadyCanceled(source => source.Average(x => (int?)x));

            AssertThrows.AlreadyCanceled(source => source.Average(x => (long)x));
            AssertThrows.AlreadyCanceled(source => source.Average(x => (long?)x));

            AssertThrows.AlreadyCanceled(source => source.Average(x => (float)x));
            AssertThrows.AlreadyCanceled(source => source.Average(x => (float?)x));

            AssertThrows.AlreadyCanceled(source => source.Average(x => (double)x));
            AssertThrows.AlreadyCanceled(source => source.Average(x => (double?)x));

            AssertThrows.AlreadyCanceled(source => source.Average(x => (decimal)x));
            AssertThrows.AlreadyCanceled(source => source.Average(x => (decimal?)x));
        }

        [Fact]
        public static void Average_AggregateException()
        {
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(1).Average((Func<int, int>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(1).Average((Func<int, int?>)(x => { throw new DeliberateTestException(); })));

            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(1).Average((Func<int, long>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(1).Average((Func<int, long?>)(x => { throw new DeliberateTestException(); })));

            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(1).Average((Func<int, float>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(1).Average((Func<int, float?>)(x => { throw new DeliberateTestException(); })));

            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(1).Average((Func<int, double>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(1).Average((Func<int, double?>)(x => { throw new DeliberateTestException(); })));

            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(1).Average((Func<int, decimal>)(x => { throw new DeliberateTestException(); })));
            AssertThrows.Wrapped<DeliberateTestException>(() => UnorderedSources.Default(1).Average((Func<int, decimal?>)(x => { throw new DeliberateTestException(); })));
        }

        [Fact]
        public static void Average_ArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int>)null).Average());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat(0, 1).Average((Func<int, int>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<int?>)null).Average());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((int?)0, 1).Average((Func<int?, int?>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<long>)null).Average());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((long)0, 1).Average((Func<long, long>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<long?>)null).Average());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((long?)0, 1).Average((Func<long?, long?>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<float>)null).Average());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((float)0, 1).Average((Func<float, float>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<float?>)null).Average());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((float?)0, 1).Average((Func<float?, float?>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<double>)null).Average());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((double)0, 1).Average((Func<double, double>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<double?>)null).Average());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((double?)0, 1).Average((Func<double?, double?>)null));

            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<decimal>)null).Average());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((decimal)0, 1).Average((Func<decimal, decimal>)null));
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((ParallelQuery<decimal?>)null).Average());
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ParallelEnumerable.Repeat((decimal?)0, 1).Average((Func<decimal?, decimal?>)null));
        }
    }
}
