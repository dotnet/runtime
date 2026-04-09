// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class InfiniteSequenceTests : AsyncEnumerableTests
    {
        [Fact]
        public void NullArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("start", () => AsyncEnumerable.InfiniteSequence((ReferenceAddable)null!, new()));
            AssertExtensions.Throws<ArgumentNullException>("step", () => AsyncEnumerable.InfiniteSequence(new(), (ReferenceAddable)null!));
        }

        [Fact]
        public async Task MultipleGetEnumeratorCalls_ReturnsUniqueInstances()
        {
            var sequence = AsyncEnumerable.InfiniteSequence(0, 1);

            var enumerator1 = sequence.GetAsyncEnumerator();
            var enumerator2 = sequence.GetAsyncEnumerator();
            Assert.NotSame(enumerator1, enumerator2);
            await enumerator1.DisposeAsync();
            await enumerator2.DisposeAsync();
        }

        [Fact]
        public async Task InfiniteSequence_AllZeroes_MatchesExpectedOutput()
        {
            await AssertEqual(AsyncEnumerable.Repeat(0, 10), AsyncEnumerable.InfiniteSequence(0, 0).Take(10));
            await AssertEqual(AsyncEnumerable.Repeat(0, 10).Select(i => (char)i), AsyncEnumerable.InfiniteSequence((char)0, (char)0).Take(10));
            await AssertEqual(AsyncEnumerable.Repeat(0, 10).Select(i => (BigInteger)i), AsyncEnumerable.InfiniteSequence(BigInteger.Zero, BigInteger.Zero).Take(10));
            await AssertEqual(AsyncEnumerable.Repeat(0, 10).Select(i => (float)i), AsyncEnumerable.InfiniteSequence((float)0, 0).Take(10));
        }

        [Fact]
        public async Task InfiniteSequence_ProducesExpectedSequence()
        {
            await ValidateAsync<sbyte>(0, 1);
            await ValidateAsync<sbyte>(sbyte.MaxValue - 3, 2);
            await ValidateAsync<sbyte>(sbyte.MinValue, sbyte.MaxValue / 2);

            await ValidateAsync<int>(0, 1);
            await ValidateAsync<int>(4, -3);
            await ValidateAsync<int>(int.MaxValue - 3, 2);
            await ValidateAsync<int>(int.MinValue, int.MaxValue / 2);

            await ValidateAsync<long>(0L, 1L);
            await ValidateAsync<long>(-4L, -3L);
            await ValidateAsync<long>(long.MaxValue - 3L, 2L);
            await ValidateAsync<long>(long.MinValue, long.MaxValue / 2L);

            await ValidateAsync<float>(0f, 1f);
            await ValidateAsync<float>(0f, -1f);
            await ValidateAsync<float>(float.MaxValue, 1f);
            await ValidateAsync<float>(float.MinValue, float.MaxValue / 2f);

            await ValidateAsync<BigInteger>(new BigInteger(long.MaxValue) * 3, (BigInteger)12345);
            await ValidateAsync<BigInteger>(new BigInteger(long.MaxValue) * 3, (BigInteger)(-12345));

            async Task ValidateAsync<T>(T start, T step) where T : INumber<T>
            {
                var sequence = AsyncEnumerable.InfiniteSequence(start, step);

                for (int trial = 0; trial < 2; trial++)
                {
                    await using var e = sequence.GetAsyncEnumerator();

                    T expected = start;
                    for (int i = 0; i < 10; i++)
                    {
                        ValueTask<bool> moveNext = e.MoveNextAsync();
                        Assert.True(moveNext.IsCompletedSuccessfully);
                        Assert.True(moveNext.Result);

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
