// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    public class AsyncEnumerableToBlockingEnumerableTests
    {

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void EmptyAsyncEnumerable()
        {
            var source = new InstrumentedAsyncEnumerable<int>(CreateSourceEnumerable());

            IEnumerable<int> blockingEnumerable = source.ToBlockingEnumerable();
            Assert.Equal(0, source.TotalGetAsyncEnumeratorCalls);

            IEnumerable<int> expected = Enumerable.Empty<int>();
            Assert.Equal(expected, blockingEnumerable);

            Assert.Equal(1, source.TotalGetAsyncEnumeratorCalls);
            Assert.Equal(1, source.TotalMoveNextAsyncCalls);
            Assert.Equal(1, source.TotalDisposeAsyncCalls);

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            static async IAsyncEnumerable<int> CreateSourceEnumerable()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                yield break;
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void SimpleAsyncEnumerable()
        {
            var source = new InstrumentedAsyncEnumerable<int>(CreateSourceEnumerable());

            IEnumerable<int> blockingEnumerable = source.ToBlockingEnumerable();
            IEnumerator<int> enumerator = blockingEnumerable.GetEnumerator();
            Assert.Equal(0, source.TotalGetAsyncEnumeratorCalls); // will not be invoked before the first MoveNext() call

            for (int i = 0; i < 10; i++)
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal(1, source.TotalGetAsyncEnumeratorCalls);
                Assert.Equal(i + 1, source.TotalMoveNextAsyncCalls);

                Assert.Equal(i, enumerator.Current);
            }

            Assert.False(enumerator.MoveNext());
            Assert.Equal(11, source.TotalMoveNextAsyncCalls);

            enumerator.Dispose();
            Assert.Equal(1, source.TotalDisposeAsyncCalls);

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            static async IAsyncEnumerable<int> CreateSourceEnumerable()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            {
                for (int i = 0; i < 10; i++)
                {
                    yield return i;
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void AsyncEnumerableWithDelays()
        {
            var source = new InstrumentedAsyncEnumerable<int>(CreateSourceEnumerable());

            IEnumerable<int> blockingEnumerable = source.ToBlockingEnumerable();
            IEnumerator<int> enumerator = blockingEnumerable.GetEnumerator();
            Assert.Equal(0, source.TotalGetAsyncEnumeratorCalls); // will not be invoked before the first MoveNext() call

            for (int i = 0; i < 5; i++)
            {
                Assert.True(enumerator.MoveNext());
                Assert.Equal(1, source.TotalGetAsyncEnumeratorCalls);
                Assert.Equal(i + 1, source.TotalMoveNextAsyncCalls);

                Assert.Equal(i, enumerator.Current);
            }

            Assert.False(enumerator.MoveNext());
            Assert.Equal(6, source.TotalMoveNextAsyncCalls);

            enumerator.Dispose();
            Assert.Equal(1, source.TotalDisposeAsyncCalls);

            static async IAsyncEnumerable<int> CreateSourceEnumerable()
            {
                for (int i = 0; i < 5; i++)
                {
                    await Task.Delay(200);
                    yield return i;
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void AsyncEnumerableWithException()
        {
            var source = new InstrumentedAsyncEnumerable<int>(CreateSourceEnumerable());

            IEnumerable<int> blockingEnumerable = source.ToBlockingEnumerable();
            Assert.Equal(0, source.TotalGetAsyncEnumeratorCalls);

            using IEnumerator<int> enumerator = blockingEnumerable.GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(0, enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(1, enumerator.Current);

            Assert.Throws<NotImplementedException>(() => enumerator.MoveNext());

            static async IAsyncEnumerable<int> CreateSourceEnumerable()
            {
                await Task.Delay(100);
                yield return 0;
                await Task.Delay(100);
                yield return 1;
                await Task.Delay(100);
                throw new NotImplementedException();
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public static void AsyncEnumerableWithCancellation()
        {
            var source = new InstrumentedAsyncEnumerable<string>(CreateSourceEnumerable());

            using var cts = new CancellationTokenSource(millisecondsDelay: 1000);
            IEnumerable<string> blockingEnumerable = source.ToBlockingEnumerable(cts.Token);

            Assert.Throws<TaskCanceledException>(() => blockingEnumerable.Count());

            Assert.Equal(1, source.TotalGetAsyncEnumeratorCalls);
            Assert.Equal(1, source.TotalDisposeAsyncCalls);

            static async IAsyncEnumerable<string> CreateSourceEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                while (true)
                {
                    await Task.Delay(50, cancellationToken);
                    yield return "y";
                }
            }
        }

        public class InstrumentedAsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            private readonly IAsyncEnumerable<T> _source;

            public InstrumentedAsyncEnumerable(IAsyncEnumerable<T> source)
            {
                _source = source;
            }

            public int TotalGetAsyncEnumeratorCalls { get; private set; }
            public int TotalDisposeAsyncCalls { get; private set; }
            public int TotalMoveNextAsyncCalls { get; private set; }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                TotalGetAsyncEnumeratorCalls++;
                return new InstrumentedAsyncEnumerator(this, _source.GetAsyncEnumerator(cancellationToken));
            }

            private class InstrumentedAsyncEnumerator : IAsyncEnumerator<T>
            {
                private readonly InstrumentedAsyncEnumerable<T> _parent;
                private readonly IAsyncEnumerator<T> _enumerator;

                public InstrumentedAsyncEnumerator(InstrumentedAsyncEnumerable<T> parent, IAsyncEnumerator<T> enumerator)
                {
                    _parent = parent;
                    _enumerator = enumerator;
                }

                public T Current => _enumerator.Current;

                public async ValueTask DisposeAsync()
                {
                    try
                    {
                        await _enumerator.DisposeAsync();
                    }
                    finally
                    {
                        _parent.TotalDisposeAsyncCalls++;
                    }
                }

                public async ValueTask<bool> MoveNextAsync()
                {
                    try
                    {
                        return await _enumerator.MoveNextAsync();
                    }
                    finally
                    {
                        _parent.TotalMoveNextAsyncCalls++;
                    }
                }
            }
        }
    }
}
