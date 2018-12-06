// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public class ConfiguredAsyncEnumerableTests
    {
        [Fact]
        public void ConfigureAwait_GetAsyncEnumerator_Default_Throws()
        {
            ConfiguredAsyncEnumerable<int> e = default;
            Assert.Throws<NullReferenceException>(() => e.GetAsyncEnumerator());

            e = ((IAsyncEnumerable<int>)null).ConfigureAwait(false);
            Assert.Throws<NullReferenceException>(() => e.GetAsyncEnumerator());
        }

        [Fact]
        public void ConfigureAwait_EnumeratorMembers_Default_Throws()
        {
            ConfiguredAsyncEnumerable<int>.Enumerator e = default;
            Assert.Throws<NullReferenceException>(() => e.MoveNextAsync());
            Assert.Throws<NullReferenceException>(() => e.Current);
            Assert.Throws<NullReferenceException>(() => e.DisposeAsync());
        }

        [Fact]
        public void ConfigureAwait_GetAsyncEnumerator_CancellationTokenPassedthrough()
        {
            var enumerable = new TrackFlagsAsyncEnumerable() { Flags = 0 };
            var cts = new CancellationTokenSource();
            ConfiguredAsyncEnumerable<int>.Enumerator enumerator = enumerable.ConfigureAwait(false).GetAsyncEnumerator(cts.Token);
            Assert.Equal(cts.Token, enumerable.CancellationToken);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ConfigureAwait_AwaitMoveNextAsync_FlagsSetAppropriately(bool continueOnCapturedContext)
        {
            var enumerable = new TrackFlagsAsyncEnumerable() { Flags = 0 };
            ConfiguredAsyncEnumerable<int>.Enumerator enumerator = enumerable.ConfigureAwait(continueOnCapturedContext).GetAsyncEnumerator();
            ConfiguredValueTaskAwaitable<bool>.ConfiguredValueTaskAwaiter moveNextAwaiter = enumerator.MoveNextAsync().GetAwaiter();
            moveNextAwaiter.UnsafeOnCompleted(() => { });
            Assert.Equal(
                continueOnCapturedContext ? ValueTaskSourceOnCompletedFlags.UseSchedulingContext : ValueTaskSourceOnCompletedFlags.None,
                enumerable.Flags);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ConfigureAwait_AwaitDisposeAsync_FlagsSetAppropriately(bool continueOnCapturedContext)
        {
            var enumerable = new TrackFlagsAsyncEnumerable() { Flags = 0 };
            ConfiguredAsyncEnumerable<int>.Enumerator enumerator = enumerable.ConfigureAwait(continueOnCapturedContext).GetAsyncEnumerator();
            ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter disposeAwaiter = enumerator.DisposeAsync().GetAwaiter();
            disposeAwaiter.UnsafeOnCompleted(() => { });
            Assert.Equal(
                continueOnCapturedContext ? ValueTaskSourceOnCompletedFlags.UseSchedulingContext : ValueTaskSourceOnCompletedFlags.None,
                enumerable.Flags);
        }

        [Fact]
        public async Task ConfigureAwait_CanBeEnumeratedWithStandardPattern()
        {
            IAsyncEnumerable<int> asyncEnumerable = new EnumerableWithDelayToAsyncEnumerable<int>(Enumerable.Range(1, 10), 1);
            int sum = 0;

            ConfiguredAsyncEnumerable<int>.Enumerator e = asyncEnumerable.ConfigureAwait(false).GetAsyncEnumerator();
            try
            {
                while (await e.MoveNextAsync())
                {
                    sum += e.Current;
                }
            }
            finally
            {
                await e.DisposeAsync();
            }

            Assert.Equal(55, sum);
        }

        private sealed class TrackFlagsAsyncEnumerable : IAsyncEnumerable<int>, IAsyncEnumerator<int>, IValueTaskSource<bool>, IValueTaskSource
        {
            public ValueTaskSourceOnCompletedFlags Flags;
            public CancellationToken CancellationToken;

            public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                CancellationToken = cancellationToken;
                return this;
            }

            public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(this, 0);
            public int Current => throw new NotImplementedException();
            public ValueTask DisposeAsync() => new ValueTask(this, 0);

            public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => Flags = flags;
            public ValueTaskSourceStatus GetStatus(short token) => ValueTaskSourceStatus.Pending;
            public bool GetResult(short token) => throw new NotImplementedException();
            void IValueTaskSource.GetResult(short token) => throw new NotImplementedException();
        }

        private sealed class EnumerableWithDelayToAsyncEnumerable<T> : IAsyncEnumerable<T>, IAsyncEnumerator<T>
        {
            private readonly int _delayMs;
            private readonly IEnumerable<T> _enumerable;
            private IEnumerator<T> _enumerator;

            public EnumerableWithDelayToAsyncEnumerable(IEnumerable<T> enumerable, int delayMs)
            {
                _enumerable = enumerable;
                _delayMs = delayMs;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                _enumerator = _enumerable.GetEnumerator();
                return this;
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                await Task.Delay(_delayMs);
                return _enumerator.MoveNext();
            }

            public T Current => _enumerator.Current;

            public async ValueTask DisposeAsync()
            {
                await Task.Delay(_delayMs);
                _enumerator.Dispose();
            }
        }
    }
}
