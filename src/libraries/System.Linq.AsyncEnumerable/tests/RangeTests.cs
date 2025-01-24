// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class RangeTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => AsyncEnumerable.Range(-1, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => AsyncEnumerable.Range(2, int.MaxValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => AsyncEnumerable.Range(int.MaxValue - 1, 3));

#if NET
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => AsyncEnumerable.Range<int>(-1, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => AsyncEnumerable.Range<int>(2, int.MaxValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => AsyncEnumerable.Range<int>(int.MaxValue - 1, 3));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => AsyncEnumerable.Range<byte>(255, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => AsyncEnumerable.Range<byte>(2, byte.MaxValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => AsyncEnumerable.Range<byte>(byte.MaxValue - 1, 3));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => AsyncEnumerable.Range<long>(-1, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => AsyncEnumerable.Range<long>(long.MaxValue - int.MaxValue + 2, int.MaxValue));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => AsyncEnumerable.Range<long>(long.MaxValue - 1, 3));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => AsyncEnumerable.Range<BigInteger>(-1, -1));
#endif
        }

        [Fact]
        public async Task VariousValues_MatchesEnumerable()
        {
            foreach (int start in new[] { int.MinValue, -1, 0, 1, int.MaxValue - 9 })
            {
                foreach (int count in new[] { 0, 1, 3, 10 })
                {
                    await AssertEqual(
                        Enumerable.Range(start, count),
                        AsyncEnumerable.Range(start, count));
                }
            }

#if NET
            foreach (int start in new[] { int.MinValue, -1, 0, 1, int.MaxValue - 9 })
            {
                foreach (int count in new[] { 0, 1, 3, 10 })
                {
                    await AssertEqual(
                        Enumerable.Range<int>(start, count),
                        AsyncEnumerable.Range<int>(start, count));
                }
            }

            foreach (byte start in new[] { byte.MinValue, 1, byte.MaxValue - 9 })
            {
                foreach (int count in new[] { 0, 1, 3, 10 })
                {
                    await AssertEqual(
                        Enumerable.Range<byte>(start, count),
                        AsyncEnumerable.Range<byte>(start, count));
                }
            }

            foreach (long start in new[] { long.MinValue, -1, 0, 1, long.MaxValue - 9 })
            {
                foreach (int count in new[] { 0, 1, 3, 10 })
                {
                    await AssertEqual(
                        Enumerable.Range<long>(start, count),
                        AsyncEnumerable.Range<long>(start, count));
                }
            }

            foreach (BigInteger start in new[] { -BigInteger.Pow(2, 1024), -1, 0, 1, BigInteger.Pow(2, 1024) })
            {
                foreach (int count in new[] { 0, 1, 3, 10 })
                {
                    await AssertEqual(
                        Enumerable.Range<BigInteger>(start, count),
                        AsyncEnumerable.Range<BigInteger>(start, count));
                }

                await AssertEqual(
                    Enumerable.Range<BigInteger>(start, int.MaxValue).Skip(10).Take(10),
                    AsyncEnumerable.Range<BigInteger>(start, int.MaxValue).Skip(10).Take(10));
            }

#endif
        }
    }
}
