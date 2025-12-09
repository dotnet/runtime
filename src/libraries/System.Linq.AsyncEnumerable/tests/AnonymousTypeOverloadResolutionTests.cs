// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public class AnonymousTypeOverloadResolutionTests : AsyncEnumerableTests
    {
        [Fact]
        public async Task Select_WithAnonymousType_ResolvesToCancellationTokenOverload()
        {
            var source = AsyncEnumerable.Range(1, 3);
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = source.Select(async (x, ct) => new { Value = x, Doubled = x * 2 });
            
            var list = await result.ToListAsync();
            Assert.Equal(3, list.Count);
            Assert.Equal(1, list[0].Value);
            Assert.Equal(2, list[0].Doubled);
            Assert.Equal(2, list[1].Value);
            Assert.Equal(4, list[1].Doubled);
        }

        [Fact]
        public async Task Select_WithAnonymousTypeAndIndex_ResolvesToCancellationTokenOverload()
        {
            var source = AsyncEnumerable.Range(1, 3);
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = source.Select(async (x, idx, ct) => new { Value = x, Index = idx });
            
            var list = await result.ToListAsync();
            Assert.Equal(3, list.Count);
            Assert.Equal(1, list[0].Value);
            Assert.Equal(0, list[0].Index);
            Assert.Equal(3, list[2].Value);
            Assert.Equal(2, list[2].Index);
        }

        [Fact]
        public async Task Where_WithAnonymousType_ResolvesToCancellationTokenOverload()
        {
            var source = AsyncEnumerable.Range(1, 5).Select(x => new { Value = x });
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = source.Where(async (x, ct) => x.Value % 2 == 0);
            
            var list = await result.ToListAsync();
            Assert.Equal(2, list.Count);
            Assert.Equal(2, list[0].Value);
            Assert.Equal(4, list[1].Value);
        }

        [Fact]
        public async Task Where_WithAnonymousTypeAndIndex_ResolvesToCancellationTokenOverload()
        {
            var source = AsyncEnumerable.Range(1, 5).Select(x => new { Value = x });
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = source.Where(async (x, idx, ct) => idx < 3);
            
            var list = await result.ToListAsync();
            Assert.Equal(3, list.Count);
        }

        [Fact]
        public async Task FirstAsync_WithAnonymousType_ResolvesToCancellationTokenOverload()
        {
            var source = AsyncEnumerable.Range(1, 5).Select(x => new { Value = x });
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = await source.FirstAsync(async (x, ct) => x.Value > 2);
            
            Assert.Equal(3, result.Value);
        }

        [Fact]
        public async Task FirstOrDefaultAsync_WithAnonymousType_ResolvesToCancellationTokenOverload()
        {
            var source = AsyncEnumerable.Range(1, 5).Select(x => new { Value = x });
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = await source.FirstOrDefaultAsync(async (x, ct) => x.Value > 10);
            
            Assert.Null(result);
        }

        [Fact]
        public async Task LastAsync_WithAnonymousType_ResolvesToCancellationTokenOverload()
        {
            var source = AsyncEnumerable.Range(1, 5).Select(x => new { Value = x });
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = await source.LastAsync(async (x, ct) => x.Value < 4);
            
            Assert.Equal(3, result.Value);
        }

        [Fact]
        public async Task SingleAsync_WithAnonymousType_ResolvesToCancellationTokenOverload()
        {
            var source = AsyncEnumerable.Range(1, 5).Select(x => new { Value = x });
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = await source.SingleAsync(async (x, ct) => x.Value == 3);
            
            Assert.Equal(3, result.Value);
        }

        [Fact]
        public async Task AnyAsync_WithAnonymousType_ResolvesToCancellationTokenOverload()
        {
            var source = AsyncEnumerable.Range(1, 5).Select(x => new { Value = x });
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = await source.AnyAsync(async (x, ct) => x.Value > 3);
            
            Assert.True(result);
        }

        [Fact]
        public async Task AllAsync_WithAnonymousType_ResolvesToCancellationTokenOverload()
        {
            var source = AsyncEnumerable.Range(1, 5).Select(x => new { Value = x });
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = await source.AllAsync(async (x, ct) => x.Value > 0);
            
            Assert.True(result);
        }

        [Fact]
        public async Task CountAsync_WithAnonymousType_ResolvesToCancellationTokenOverload()
        {
            var source = AsyncEnumerable.Range(1, 5).Select(x => new { Value = x });
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = await source.CountAsync(async (x, ct) => x.Value % 2 == 0);
            
            Assert.Equal(2, result);
        }

        [Fact]
        public async Task SkipWhile_WithAnonymousType_ResolvesToCancellationTokenOverload()
        {
            var source = AsyncEnumerable.Range(1, 5).Select(x => new { Value = x });
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = source.SkipWhile(async (x, ct) => x.Value < 3);
            
            var list = await result.ToListAsync();
            Assert.Equal(3, list.Count);
            Assert.Equal(3, list[0].Value);
        }

        [Fact]
        public async Task SkipWhile_WithAnonymousTypeAndIndex_ResolvesToCancellationTokenOverload()
        {
            var source = AsyncEnumerable.Range(1, 5).Select(x => new { Value = x });
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = source.SkipWhile(async (x, idx, ct) => idx < 2);
            
            var list = await result.ToListAsync();
            Assert.Equal(3, list.Count);
        }

        [Fact]
        public async Task TakeWhile_WithAnonymousType_ResolvesToCancellationTokenOverload()
        {
            var source = AsyncEnumerable.Range(1, 5).Select(x => new { Value = x });
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = source.TakeWhile(async (x, ct) => x.Value < 4);
            
            var list = await result.ToListAsync();
            Assert.Equal(3, list.Count);
            Assert.Equal(3, list[2].Value);
        }

        [Fact]
        public async Task TakeWhile_WithAnonymousTypeAndIndex_ResolvesToCancellationTokenOverload()
        {
            var source = AsyncEnumerable.Range(1, 5).Select(x => new { Value = x });
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = source.TakeWhile(async (x, idx, ct) => idx < 3);
            
            var list = await result.ToListAsync();
            Assert.Equal(3, list.Count);
        }

        [Fact]
        public async Task SelectMany_WithAnonymousType_ResolvesToCancellationTokenOverload()
        {
            var source = AsyncEnumerable.Range(1, 3);
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = source.SelectMany(async (x, ct) => new[] { new { Value = x }, new { Value = x * 10 } });
            
            var list = await result.ToListAsync();
            Assert.Equal(6, list.Count);
            Assert.Equal(1, list[0].Value);
            Assert.Equal(10, list[1].Value);
        }

        [Fact]
        public async Task OrderBy_WithAnonymousType_ResolvesToCancellationTokenOverload()
        {
            var source = AsyncEnumerable.Range(1, 3).Select(x => new { Value = 4 - x });
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = source.OrderBy(async (x, ct) => x.Value);
            
            var list = await result.ToListAsync();
            Assert.Equal(3, list.Count);
            Assert.Equal(1, list[0].Value);
            Assert.Equal(2, list[1].Value);
            Assert.Equal(3, list[2].Value);
        }

        [Fact]
        public async Task GroupBy_WithAnonymousType_ResolvesToCancellationTokenOverload()
        {
            var source = AsyncEnumerable.Range(1, 6).Select(x => new { Value = x });
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = source.GroupBy(async (x, ct) => x.Value % 2);
            
            var groups = await result.ToListAsync();
            Assert.Equal(2, groups.Count);
        }

        [Fact]
        public async Task DistinctBy_WithAnonymousType_ResolvesToCancellationTokenOverload()
        {
            var source = new[] { 1, 2, 3, 4, 5, 6 }.ToAsyncEnumerable().Select(x => new { Value = x });
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = source.DistinctBy(async (x, ct) => x.Value % 3);
            
            var list = await result.ToListAsync();
            Assert.Equal(3, list.Count);
        }

        [Fact]
        public async Task ToDictionaryAsync_WithAnonymousType_ResolvesToCancellationTokenOverload()
        {
            var source = AsyncEnumerable.Range(1, 3).Select(x => new { Value = x });
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = await source.ToDictionaryAsync(async (x, ct) => x.Value);
            
            Assert.Equal(3, result.Count);
            Assert.Equal(1, result[1].Value);
        }

        [Fact]
        public async Task ToLookupAsync_WithAnonymousType_ResolvesToCancellationTokenOverload()
        {
            var source = AsyncEnumerable.Range(1, 6).Select(x => new { Value = x });
            
            // This should resolve to the CancellationToken overload due to OverloadResolutionPriority
            var result = await source.ToLookupAsync(async (x, ct) => x.Value % 2);
            
            Assert.Equal(2, result.Count);
        }
    }
}
