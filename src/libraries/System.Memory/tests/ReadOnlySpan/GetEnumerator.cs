// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.SpanTests
{
    public static partial class ReadOnlySpanTests
    {
        public static IEnumerable<object[]> IntegerArrays()
        {
            yield return new object[] { new int[0] };
            yield return new object[] { new int[] { 42 } };
            yield return new object[] { new int[] { 42, 43, 44, 45 } };
        }

        [Theory]
        [MemberData(nameof(IntegerArrays))]
        public static void GetEnumerator_ForEach_AllValuesReturnedCorrectly(int[] array)
        {
            ReadOnlySpan<int> span = array;

            int sum = 0;
            foreach (int i in span)
            {
                sum += i;
            }

            Assert.Equal(Enumerable.Sum(array), sum);
            Assert.Equal(Enumerable.Sum(array), SumGI(span.GetEnumerator()));
            Assert.Equal(Enumerable.Sum(array), SumI(span.GetEnumerator()));
        }

        [Theory]
        [MemberData(nameof(IntegerArrays))]
        public static void GetEnumerator_Manual_AllValuesReturnedCorrectly(int[] array)
        {
            ReadOnlySpan<int> span = array;

            int sum = 0;
            ReadOnlySpan<int>.Enumerator e = span.GetEnumerator();
            while (e.MoveNext())
            {
                ref readonly int i = ref e.Current;
                sum += i;
                Assert.Equal(e.Current, e.Current);
            }
            Assert.False(e.MoveNext());

            Assert.Equal(Enumerable.Sum(array), sum);
            Assert.Equal(Enumerable.Sum(array), SumGI(span.GetEnumerator()));
            Assert.Equal(Enumerable.Sum(array), SumI(span.GetEnumerator()));
        }

        [Fact]
        public static void GetEnumerator_MoveNextOnDefault_ReturnsFalse()
        {
            Assert.False(default(ReadOnlySpan<int>.Enumerator).MoveNext());
            Assert.ThrowsAny<Exception>(() => default(ReadOnlySpan<int>.Enumerator).Current);

            TestGI(default(ReadOnlySpan<int>.Enumerator));
            TestI(default(ReadOnlySpan<int>.Enumerator));

            static void TestGI<TEnumerator>(TEnumerator enumerator) where TEnumerator : IEnumerator<int>, allows ref struct
            {
                Assert.False(enumerator.MoveNext());
                enumerator.Dispose();
                enumerator.Reset();
                Assert.False(enumerator.MoveNext());
            }

            static void TestI<TEnumerator>(TEnumerator enumerator) where TEnumerator : IEnumerator, allows ref struct
            {
                Assert.False(enumerator.MoveNext());
                enumerator.Reset();
                Assert.False(enumerator.MoveNext());
            }
        }

        private static int SumGI<TEnumerator>(TEnumerator enumerator) where TEnumerator : IEnumerator<int>, allows ref struct
        {
            int sum1 = 0;
            enumerator.Dispose();
            while (enumerator.MoveNext())
            {
                sum1 += enumerator.Current;
                enumerator.Dispose();
            }
            Assert.False(enumerator.MoveNext());

            int sum2 = 0;
            enumerator.Reset();
            enumerator.Dispose();
            while (enumerator.MoveNext())
            {
                sum2 += enumerator.Current;
                enumerator.Dispose();
            }
            Assert.False(enumerator.MoveNext());

            Assert.Equal(sum1, sum2);
            return sum2;
        }

        private static int SumI<TEnumerator>(TEnumerator enumerator) where TEnumerator : IEnumerator, allows ref struct
        {
            int sum1 = 0;
            while (enumerator.MoveNext())
            {
                sum1 += (int)enumerator.Current;
            }
            Assert.False(enumerator.MoveNext());

            int sum2 = 0;
            enumerator.Reset();
            while (enumerator.MoveNext())
            {
                sum2 += (int)enumerator.Current;
            }
            Assert.False(enumerator.MoveNext());

            Assert.Equal(sum1, sum2);
            return sum2;
        }
    }
}
