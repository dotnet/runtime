// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Linq.Tests
{
    public abstract class AsyncEnumerableTests
    {
        protected static IAsyncEnumerable<T> CreateSource<T>(params T[] items) =>
            items.ToAsyncEnumerable().Yield();

        protected static IEnumerable<IAsyncEnumerable<T>> CreateSources<T>(params T[] items)
        {
            if (items.Length == 0)
            {
                yield return Enumerable.Empty<T>().ToAsyncEnumerable();
                yield return AsyncEnumerable.Empty<T>();
            }

            yield return items.ToAsyncEnumerable();
            yield return items.ToAsyncEnumerable().Yield();
        }

        protected static async Task ConsumeAsync<T>(IAsyncEnumerable<T> source)
        {
            await foreach (T item in source) { }
        }

        protected static async Task ConsumeAsync<T>(ConfiguredCancelableAsyncEnumerable<T> source)
        {
            await foreach (T item in source) { }
        }

        protected static void FillRandom(Random rand, int[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = rand.Next();
            }
        }

        protected static void FillRandom(Random rand, string[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                string s = Guid.NewGuid().ToString("N");
                values[i] = s.Substring(0, rand.Next(0, s.Length));
            }
        }

        protected static async Task AssertEqual<T>(IEnumerable<T> expected, IAsyncEnumerable<T> actual)
        {
            Assert.Equal(
                expected.ToArray(),
                await actual.ToArrayAsync());
        }

        protected static async Task AssertEqual<T>(IAsyncEnumerable<T> expected, IAsyncEnumerable<T> actual)
        {
            await using IAsyncEnumerator<T> e1 = expected.GetAsyncEnumerator();
            await using IAsyncEnumerator<T> e2 = actual.GetAsyncEnumerator();

            while (await e1.MoveNextAsync())
            {
                Assert.True(await e2.MoveNextAsync());
                Assert.Equal(e1.Current, e2.Current);
            }

            Assert.False(await e2.MoveNextAsync());
        }

        protected static IEqualityComparer<T> CreateEqualityComparer<T>(Func<T, T, bool> equals, Func<T, int> getHashCode) =>
            new DelegateEqualityComparer<T>(equals, getHashCode);

        protected static IEqualityComparer<int> OddEvenComparer { get; } = CreateEqualityComparer<int>((x, y) => x % 2 == y % 2, x => x % 2);

        protected static IEqualityComparer<string> LengthComparer { get; } = CreateEqualityComparer<string>((x, y) => x.Length == y.Length, x => x.Length);

        protected static bool[] TrueFalseBools { get; } = [true, false];

        private sealed class DelegateEqualityComparer<T>(Func<T, T, bool> equals, Func<T, int> getHashCode) : IEqualityComparer<T>
        {
            public bool Equals(T x, T y) => equals(x, y);
            public int GetHashCode(T obj) => getHashCode(obj);
        }
    }

    public static class AsyncEnumerableTestExtensions
    {
        public static async IAsyncEnumerable<T> Yield<T>(
            this IAsyncEnumerable<T> source, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (T item in source.WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return item;
            }
        }

        public static TrackingAsyncEnumerable<T> Track<T>(this IAsyncEnumerable<T> source) =>
            new TrackingAsyncEnumerable<T>(source);

        public static async IAsyncEnumerable<T> AppendException<T>(this IAsyncEnumerable<T> source, Exception exception, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (T item in source.WithCancellation(cancellationToken))
            {
                yield return item;
            }

            throw exception;
        }
    }

    public sealed class TrackingAsyncEnumerable<T>(IAsyncEnumerable<T> source) : IAsyncEnumerable<T>
    {
        private readonly IAsyncEnumerable<T> _source = source;

        public int MoveNextAsyncCount { get; set; }

        public int CurrentCount { get; set; }

        public int DisposeAsyncCount { get; set; }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new TrackDisposeAsyncEnumerator(_source.GetAsyncEnumerator(cancellationToken), this);

        private sealed class TrackDisposeAsyncEnumerator(IAsyncEnumerator<T> source, TrackingAsyncEnumerable<T> parent) : IAsyncEnumerator<T>
        {
            public T Current
            {
                get
                {
                    parent.CurrentCount++;
                    return source.Current;
                }
            }

            public ValueTask<bool> MoveNextAsync()
            {
                parent.MoveNextAsyncCount++;
                return source.MoveNextAsync();
            }

            public ValueTask DisposeAsync()
            {
                parent.DisposeAsyncCount++;
                return source.DisposeAsync();
            }
        }
    }
}
