// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Xunit;

namespace System.Linq.Tests
{
    public class InfiniteSequenceTests : EnumerableTests
    {
        [Fact]
        public void NullArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("start", () => Enumerable.InfiniteSequence((ReferenceAddable)null!, new()));
            AssertExtensions.Throws<ArgumentNullException>("step", () => Enumerable.InfiniteSequence(new(), (ReferenceAddable)null!));
        }

        [Fact]
        public void MultipleGetEnumeratorCalls_ReturnsUniqueInstances()
        {
            var sequence = Enumerable.InfiniteSequence(0, 1);

            var enumerator1 = sequence.GetEnumerator();
            var enumerator2 = sequence.GetEnumerator();
            Assert.NotSame(enumerator1, enumerator2);
            enumerator1.Dispose();
            enumerator2.Dispose();
        }

        [Fact]
        public void InfiniteSequence_AllZeroes_MatchesExpectedOutput()
        {
            Assert.Equal(Enumerable.Repeat(0, 10), Enumerable.InfiniteSequence(0, 0).Take(10));
            Assert.Equal(Enumerable.Repeat(0, 10).Select(i => (char)i), Enumerable.InfiniteSequence((char)0, (char)0).Take(10));
            Assert.Equal(Enumerable.Repeat(0, 10).Select(i => (BigInteger)i), Enumerable.InfiniteSequence(BigInteger.Zero, BigInteger.Zero).Take(10));
            Assert.Equal(Enumerable.Repeat(0, 10).Select(i => (float)i), Enumerable.InfiniteSequence((float)0, 0).Take(10));
        }

        [Fact]
        public void InfiniteSequence_ProducesExpectedSequence()
        {
            Validate<sbyte>(0, 1);
            Validate<sbyte>(sbyte.MaxValue - 3, 2);
            Validate<sbyte>(sbyte.MinValue, sbyte.MaxValue / 2);

            Validate<int>(0, 1);
            Validate<int>(4, -3);
            Validate<int>(int.MaxValue - 3, 2);
            Validate<int>(int.MinValue, int.MaxValue / 2);

            Validate<long>(0L, 1L);
            Validate<long>(-4L, -3L);
            Validate<long>(long.MaxValue - 3L, 2L);
            Validate<long>(long.MinValue, long.MaxValue / 2L);

            Validate<float>(0f, 1f);
            Validate<float>(0f, -1f);
            Validate<float>(float.MaxValue, 1f);
            Validate<float>(float.MinValue, float.MaxValue / 2f);

            Validate<BigInteger>(new BigInteger(long.MaxValue) * 3, (BigInteger)12345);
            Validate<BigInteger>(new BigInteger(long.MaxValue) * 3, (BigInteger)(-12345));

            void Validate<T>(T start, T step) where T : INumber<T>
            {
                var sequence = Enumerable.InfiniteSequence(start, step);

                for (int trial = 0; trial < 2; trial++)
                {
                    using var e = sequence.GetEnumerator();

                    T expected = start;
                    for (int i = 0; i < 10; i++)
                    {
                        Assert.True(e.MoveNext());
                        Assert.Equal(expected, e.Current);

                        expected += step;
                    }
                }
            }
        }

        private sealed class ReferenceAddable : IAdditionOperators<ReferenceAddable, ReferenceAddable, ReferenceAddable>
        {
            public static ReferenceAddable operator +(ReferenceAddable left, ReferenceAddable right) => left;
            public static ReferenceAddable operator checked +(ReferenceAddable left, ReferenceAddable right) => left;
        }
    }
}
