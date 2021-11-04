// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests
{
    public static class FlattenTests
    {
        [Theory]
        [MemberData(nameof(FlattenShouldProduceExpectedOuput_Data))]
        public static void ShouldProduceExpectedOuput<T>(T[][] source, T[] expected)
        {
            Assert.Equal(expected, source.Flatten());
        }

        public static IEnumerable<object[]> FlattenShouldProduceExpectedOuput_Data()
        {
            yield return Wrap(new int[][] { }, new int[] { });
            yield return Wrap(new int[][] { new int[] { }, new int[] { } }, new int[] { });
            yield return Wrap(new int[][] { new int[] { 1, 2 }, new int[] { }, new int[] { 3, 4, 5 } }, new int[] { 1, 2, 3, 4, 5 });
            static object[] Wrap<T>(T[][] source, T[] expected) => new object[] { source, expected };
        }

        [Theory]
        [MemberData(nameof(FlattenShouldProduceIdenticalOutputToSelectManyIdentity_Data))]
        public static void ShouldProduceIdenticalOutputToSelectManyIdentity<T>(IEnumerable<IEnumerable<T>> source)
        {
            Assert.Equal(source.SelectMany(x => x), source.Flatten());
        }

        public static IEnumerable<object[]> FlattenShouldProduceIdenticalOutputToSelectManyIdentity_Data()
        {
            yield return Wrap(Enumerable.Repeat(Array.Empty<string>(), 20));
            yield return Wrap(Enumerable.Repeat(Enumerable.Range(1, 5), 5));
            static object[] Wrap<T>(IEnumerable<IEnumerable<T>> source) => new object[] { source };
        }

        [Fact]
        public static void ShouldEnumerateInnerEnumerablesOnDemand()
        {
            IEnumerable<int> source = new[] { Enumerable.Range(1, 3), FailAfter(3) }.Flatten();

            Assert.Throws<DivideByZeroException>(() => source.Count());
            Assert.Equal(6, source.Take(6).Count());

            static IEnumerable<int> FailAfter(int n)
            {
                for (int i = 0; i < n ; i++)
                {
                    yield return i;
                }

                throw new DivideByZeroException();
            }
        }

        [Fact]
        public static void NullSource()
        {
            IEnumerable<IEnumerable<int>> sources = null!;
            Assert.Throws<ArgumentNullException>("sources", () => sources.Flatten());
        }
    }
}
