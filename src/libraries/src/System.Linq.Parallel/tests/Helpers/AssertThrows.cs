// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Xunit;

namespace System.Linq.Parallel.Tests
{
    internal static class AssertThrows
    {
        private const int EventualCancellationSize = 128;

        public static void AlreadyCanceled(Action<ParallelQuery<int>> query)
        {
            ParallelQuery<int> s = Enumerables<int>.ThrowOnEnumeration(new ShouldNotBeInvokedException(), 2).AsParallel().WithCancellation(new CancellationToken(canceled: true));
            OperationCanceledException oce = Assert.Throws<OperationCanceledException>(() => query(s));
        }

        public static void EventuallyCanceled(Action<ParallelQuery<int>, Action> query)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            int countdown = 4;
            Action cancel = () =>
            {
                if (Interlocked.Decrement(ref countdown) == 0)
                {
                    source.Cancel();
                }
            };

            ParallelQuery<int> s = ParallelEnumerable.Range(0, EventualCancellationSize).WithCancellation(source.Token);
            OperationCanceledException oce = Assert.Throws<OperationCanceledException>(() => query(s, cancel));
            Assert.Equal(source.Token, oce.CancellationToken);
        }

        public static void OtherTokenCanceled(Action<ParallelQuery<int>, Action> query)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationTokenSource alternate = new CancellationTokenSource();
            int countdown = 4;
            Action cancel = () =>
            {
                if (Interlocked.Decrement(ref countdown) == 0)
                {
                    // Only cancel/throw alternate.
                    alternate.Cancel();
                    alternate.Token.ThrowIfCancellationRequested();
                }
            };

            ParallelQuery<int> s = ParallelEnumerable.Range(0, EventualCancellationSize).WithCancellation(source.Token);
            OperationCanceledException oce = Wrapped<OperationCanceledException>(() => query(s, cancel));
            Assert.NotEqual(source.Token, oce.CancellationToken);
        }

        public static void SameTokenNotCanceled(Action<ParallelQuery<int>, Action> query)
        {
            CancellationTokenSource source = new CancellationTokenSource();
            int countdown = 4;
            Action cancel = () =>
            {
                if (Interlocked.Decrement(ref countdown) == 0)
                {
                    throw new OperationCanceledException(source.Token);
                }
            };

            ParallelQuery<int> s = ParallelEnumerable.Range(0, EventualCancellationSize).WithCancellation(source.Token);
            OperationCanceledException oce = Wrapped<OperationCanceledException>(() => query(s, cancel));
            Assert.Equal(source.Token, oce.CancellationToken);
        }

        public static T Wrapped<T>(Action action) where T : Exception
        {
            AggregateException ae = Assert.Throws<AggregateException>(action);
            Assert.All(ae.InnerExceptions, e => Assert.IsType<T>(e));
            return ae.InnerExceptions.Cast<T>().First();
        }
    }
}
