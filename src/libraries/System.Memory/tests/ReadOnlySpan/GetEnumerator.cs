// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Linq;
using System.Collections.Generic;

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
        }

        [Fact]
        public static void GetEnumerator_MoveNextOnDefault_ReturnsFalse()
        {
            Assert.False(default(ReadOnlySpan<int>.Enumerator).MoveNext());
            Assert.ThrowsAny<Exception>(() => default(ReadOnlySpan<int>.Enumerator).Current);
        }
    }
}
