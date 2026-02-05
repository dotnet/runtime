// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class ContainsAsyncTests : AsyncEnumerableTests
    {
        [Fact]
        public void InvalidInputs_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => AsyncEnumerable.ContainsAsync(null, 42));
        }

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new int[] { 1 })]
        [InlineData(new int[] { 2, 4, 8 })]
        [InlineData(new int[] { -1, 5, 6, 7, 8, 2})]
        public async Task VariousValues_MatchesEnumerable_Int32(int[] values)
        {
            foreach (IAsyncEnumerable<int> source in CreateSources(values))
            {
                Assert.Equal(
                    values.Contains(2),
                    await source.ContainsAsync(2));

                Assert.Equal(
                    values.Contains(-2, OddEvenComparer),
                    await source.ContainsAsync(-2, OddEvenComparer));
            }
        }

        public static IEnumerable<object[]> VariousValues_MatchesEnumerable_String_MemberData()
        {
            yield return new object[] { new string[0] };
            yield return new object[] { new string[] { "1" } };
            yield return new object[] { new string[] { "2", "4", "8" } };
            yield return new object[] { new string[] { "-1", "5", "6", "7", "8", "2", "12" } };
        }

        [Theory]
        [MemberData(nameof(VariousValues_MatchesEnumerable_String_MemberData))]
        public async Task VariousValues_MatchesEnumerable_String(string[] values)
        {
            foreach (IAsyncEnumerable<string> source in CreateSources(values))
            {
                Assert.Equal(
                    values.Contains("2"),
                    await source.ContainsAsync("2"));

                Assert.Equal(
                    values.Contains("00", LengthComparer),
                    await source.ContainsAsync("00", LengthComparer));
            }
        }

        [Fact]
        public async Task Cancellation_Cancels()
        {
            IAsyncEnumerable<int> source = CreateSource(1, 3, 5);
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await source.ContainsAsync(5, comparer: null, new CancellationToken(true)));
        }

        [Fact]
        public async Task InterfaceCalls_ExpectedCounts()
        {
            TrackingAsyncEnumerable<int> source;

            source = CreateSource(1, 3, 5).Track();
            Assert.False(await source.ContainsAsync(6));
            Assert.Equal(4, source.MoveNextAsyncCount);
            Assert.Equal(3, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);

            source = CreateSource(1, 3, 5).Track();
            Assert.True(await source.ContainsAsync(1));
            Assert.Equal(1, source.MoveNextAsyncCount);
            Assert.Equal(1, source.CurrentCount);
            Assert.Equal(1, source.DisposeAsyncCount);
        }
    }
}
