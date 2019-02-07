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
    public class ConfiguredCancelableAsyncEnumerableTests
    {
        [Fact]
        public void Default_GetAsyncEnumerator_Throws()
        {
            ConfiguredCancelableAsyncEnumerable<int> e = default;
            Assert.Throws<NullReferenceException>(() => e.GetAsyncEnumerator());

            e = ((IAsyncEnumerable<int>)null).ConfigureAwait(false);
            Assert.Throws<NullReferenceException>(() => e.GetAsyncEnumerator());
        }

        [Fact]
        public void Default_EnumeratorMembers_Throws()
        {
            ConfiguredCancelableAsyncEnumerable<int>.Enumerator e = default;
            Assert.Throws<NullReferenceException>(() => e.MoveNextAsync());
            Assert.Throws<NullReferenceException>(() => e.Current);
            Assert.Throws<NullReferenceException>(() => e.DisposeAsync());
        }

        [Fact]
        public void Default_WithCancellation_ConfigureAwait_NoThrow()
        {
            ConfiguredCancelableAsyncEnumerable<int> e = TaskExtensions.WithCancellation((IAsyncEnumerable<int>)null, default);
            e = e.ConfigureAwait(false);
            e = e.WithCancellation(default);
            Assert.Throws<NullReferenceException>(() => e.GetAsyncEnumerator());
        }

        [Fact]
        public void Default_ConfigureAwait_WithCancellation_NoThrow()
        {
            ConfiguredCancelableAsyncEnumerable<int> e = TaskExtensions.ConfigureAwait((IAsyncEnumerable<int>)null, false);
            e = e.WithCancellation(default);
            e = e.ConfigureAwait(false);
            Assert.Throws<NullReferenceException>(() => e.GetAsyncEnumerator());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ConfigureAwait_AwaitMoveNextAsync_FlagsSetAppropriately(bool continueOnCapturedContext)
        {
            TrackFlagsAsyncEnumerable enumerable;
            CancellationToken token = new CancellationTokenSource().Token;

            // Single ConfigureAwait call
            enumerable = new TrackFlagsAsyncEnumerable() { Flags = 0 };
            enumerable.ConfigureAwait(continueOnCapturedContext).GetAsyncEnumerator().MoveNextAsync().GetAwaiter().UnsafeOnCompleted(() => { });
            Assert.Equal(
                continueOnCapturedContext ? ValueTaskSourceOnCompletedFlags.UseSchedulingContext : ValueTaskSourceOnCompletedFlags.None,
                enumerable.Flags);

            // Unnecessary multiple calls; only last one is used
            enumerable = new TrackFlagsAsyncEnumerable() { Flags = 0 };
            enumerable.ConfigureAwait(!continueOnCapturedContext).ConfigureAwait(continueOnCapturedContext).GetAsyncEnumerator().MoveNextAsync().GetAwaiter().UnsafeOnCompleted(() => { });
            Assert.Equal(
                continueOnCapturedContext ? ValueTaskSourceOnCompletedFlags.UseSchedulingContext : ValueTaskSourceOnCompletedFlags.None,
                enumerable.Flags);

            // After WithCancellation
            enumerable = new TrackFlagsAsyncEnumerable() { Flags = 0 };
            enumerable.WithCancellation(token).ConfigureAwait(continueOnCapturedContext).GetAsyncEnumerator().MoveNextAsync().GetAwaiter().UnsafeOnCompleted(() => { });
            Assert.Equal(
                continueOnCapturedContext ? ValueTaskSourceOnCompletedFlags.UseSchedulingContext : ValueTaskSourceOnCompletedFlags.None,
                enumerable.Flags);

            // Before WithCancellation
            enumerable = new TrackFlagsAsyncEnumerable() { Flags = 0 };
            enumerable.ConfigureAwait(continueOnCapturedContext).WithCancellation(token).GetAsyncEnumerator().MoveNextAsync().GetAwaiter().UnsafeOnCompleted(() => { });
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
            enumerable.ConfigureAwait(continueOnCapturedContext).GetAsyncEnumerator().DisposeAsync().GetAwaiter().UnsafeOnCompleted(() => { });
            Assert.Equal(
                continueOnCapturedContext ? ValueTaskSourceOnCompletedFlags.UseSchedulingContext : ValueTaskSourceOnCompletedFlags.None,
                enumerable.Flags);
        }

        [Fact]
        public async Task CanBeEnumeratedWithStandardPattern()
        {
            IAsyncEnumerable<int> asyncEnumerable = new EnumerableWithDelayToAsyncEnumerable<int>(Enumerable.Range(1, 10), 1);
            int sum = 0;

            ConfiguredCancelableAsyncEnumerable<int>.Enumerator e = asyncEnumerable.ConfigureAwait(false).WithCancellation(new CancellationTokenSource().Token).GetAsyncEnumerator();
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

        [Fact]
        public void WithCancellation_TokenPassedThrough()
        {
            TrackFlagsAsyncEnumerable enumerable;
            CancellationToken token1 = new CancellationTokenSource().Token;
            CancellationToken token2 = new CancellationTokenSource().Token;

            // No WithCancellation call
            enumerable = new TrackFlagsAsyncEnumerable();
            enumerable.GetAsyncEnumerator();
            Assert.Equal(CancellationToken.None, enumerable.CancellationToken);

            // Only ConfigureAwait call
            enumerable = new TrackFlagsAsyncEnumerable();
            enumerable.ConfigureAwait(false).GetAsyncEnumerator();
            Assert.Equal(CancellationToken.None, enumerable.CancellationToken);

            // Single WithCancellation call
            enumerable = new TrackFlagsAsyncEnumerable();
            enumerable.WithCancellation(token1).GetAsyncEnumerator();
            Assert.Equal(token1, enumerable.CancellationToken);

            // Unnecessary multiple calls; only last one is used
            enumerable = new TrackFlagsAsyncEnumerable();
            enumerable.WithCancellation(token1).WithCancellation(token2).GetAsyncEnumerator();
            Assert.Equal(token2, enumerable.CancellationToken);

            // Before ConfigureAwait
            enumerable = new TrackFlagsAsyncEnumerable();
            enumerable.WithCancellation(token1).ConfigureAwait(false).GetAsyncEnumerator();
            Assert.Equal(token1, enumerable.CancellationToken);

            // After ConfigureAwait
            enumerable = new TrackFlagsAsyncEnumerable();
            enumerable.ConfigureAwait(false).WithCancellation(token1).GetAsyncEnumerator();
            Assert.Equal(token1, enumerable.CancellationToken);
        }

        private sealed class TrackFlagsAsyncEnumerable : IAsyncEnumerable<int>
        {
            public ValueTaskSourceOnCompletedFlags Flags;
            public CancellationToken CancellationToken;

            public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                CancellationToken = cancellationToken;
                return new Enumerator(this);
            }

            private sealed class Enumerator : IAsyncEnumerator<int>, IValueTaskSource<bool>, IValueTaskSource
            {
                private readonly TrackFlagsAsyncEnumerable _enumerable;

                public Enumerator(TrackFlagsAsyncEnumerable enumerable) => _enumerable = enumerable;

                public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(this, 0);
                public int Current => throw new NotImplementedException();
                public ValueTask DisposeAsync() => new ValueTask(this, 0);

                public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _enumerable.Flags = flags;
                public ValueTaskSourceStatus GetStatus(short token) => ValueTaskSourceStatus.Pending;
                public bool GetResult(short token) => throw new NotImplementedException();
                void IValueTaskSource.GetResult(short token) => throw new NotImplementedException();
            }
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
