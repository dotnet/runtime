// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Threading.Tasks.Tests
{
    public sealed class ParallelForEachAsyncTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void InvalidArguments_ThrowsException()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => { Parallel.ForEachAsync((IEnumerable<int>)null, (item, cancellationToken) => default); });
            AssertExtensions.Throws<ArgumentNullException>("source", () => { Parallel.ForEachAsync((IEnumerable<int>)null, CancellationToken.None, (item, cancellationToken) => default); });
            AssertExtensions.Throws<ArgumentNullException>("source", () => { Parallel.ForEachAsync((IEnumerable<int>)null, new ParallelOptions(), (item, cancellationToken) => default); });

            AssertExtensions.Throws<ArgumentNullException>("source", () => { Parallel.ForEachAsync((IAsyncEnumerable<int>)null, (item, cancellationToken) => default); });
            AssertExtensions.Throws<ArgumentNullException>("source", () => { Parallel.ForEachAsync((IAsyncEnumerable<int>)null, CancellationToken.None, (item, cancellationToken) => default); });
            AssertExtensions.Throws<ArgumentNullException>("source", () => { Parallel.ForEachAsync((IAsyncEnumerable<int>)null, new ParallelOptions(), (item, cancellationToken) => default); });

            AssertExtensions.Throws<ArgumentNullException>("parallelOptions", () => { Parallel.ForEachAsync(Enumerable.Range(1, 10), null, (item, cancellationToken) => default); });
            AssertExtensions.Throws<ArgumentNullException>("parallelOptions", () => { Parallel.ForEachAsync(EnumerableRangeAsync(1, 10), null, (item, cancellationToken) => default); });

            AssertExtensions.Throws<ArgumentNullException>("body", () => { Parallel.ForEachAsync(Enumerable.Range(1, 10), null); });
            AssertExtensions.Throws<ArgumentNullException>("body", () => { Parallel.ForEachAsync(Enumerable.Range(1, 10), CancellationToken.None, null); });
            AssertExtensions.Throws<ArgumentNullException>("body", () => { Parallel.ForEachAsync(Enumerable.Range(1, 10), new ParallelOptions(), null); });

            AssertExtensions.Throws<ArgumentNullException>("body", () => { Parallel.ForEachAsync(EnumerableRangeAsync(1, 10), null); });
            AssertExtensions.Throws<ArgumentNullException>("body", () => { Parallel.ForEachAsync(EnumerableRangeAsync(1, 10), CancellationToken.None, null); });
            AssertExtensions.Throws<ArgumentNullException>("body", () => { Parallel.ForEachAsync(EnumerableRangeAsync(1, 10), new ParallelOptions(), null); });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void PreCanceled_CancelsSynchronously()
        {
            var box = new StrongBox<bool>(false);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            void AssertCanceled(Task t)
            {
                Assert.True(t.IsCanceled);
                var oce = Assert.ThrowsAny<OperationCanceledException>(() => t.GetAwaiter().GetResult());
                Assert.Equal(cts.Token, oce.CancellationToken);
            }

            Func<int, CancellationToken, ValueTask> body = (item, cancellationToken) =>
            {
                Assert.False(true, "Should not have been invoked");
                return default;
            };

            AssertCanceled(Parallel.ForEachAsync(MarkStart(box), cts.Token, body));
            AssertCanceled(Parallel.ForEachAsync(MarkStartAsync(box), cts.Token, body));

            AssertCanceled(Parallel.ForEachAsync(MarkStart(box), new ParallelOptions { CancellationToken = cts.Token }, body));
            AssertCanceled(Parallel.ForEachAsync(MarkStartAsync(box), new ParallelOptions { CancellationToken = cts.Token }, body));

            Assert.False(box.Value);

            static IEnumerable<int> MarkStart(StrongBox<bool> box)
            {
                Assert.False(box.Value);
                box.Value = true;
                yield return 0;
            }

            static async IAsyncEnumerable<int> MarkStartAsync(StrongBox<bool> box)
            {
                Assert.False(box.Value);
                box.Value = true;
                yield return 0;

                await Task.Yield();
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(-1)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(128)]
        public async Task Dop_WorkersCreatedRespectingLimit_Sync(int dop)
        {
            static IEnumerable<int> IterateUntilSet(StrongBox<bool> box)
            {
                int counter = 0;
                while (!box.Value)
                {
                    yield return counter++;
                }
            }

            var box = new StrongBox<bool>(false);

            int activeWorkers = 0;
            var block = new TaskCompletionSource();

            Task t = Parallel.ForEachAsync(IterateUntilSet(box), new ParallelOptions { MaxDegreeOfParallelism = dop }, async (item, cancellationToken) =>
            {
                Interlocked.Increment(ref activeWorkers);
                await block.Task;
            });
            Assert.False(t.IsCompleted);

            await Task.Delay(20); // give the loop some time to run

            box.Value = true;
            block.SetResult();
            await t;

            Assert.InRange(activeWorkers, 0, dop == -1 ? Environment.ProcessorCount : dop);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(-1)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(128)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50566", TestPlatforms.Android)]
        public async Task Dop_WorkersCreatedRespectingLimitAndTaskScheduler_Sync(int dop)
        {
            static IEnumerable<int> IterateUntilSet(StrongBox<bool> box)
            {
                int counter = 0;
                while (!box.Value)
                {
                    yield return counter++;
                }
            }

            var box = new StrongBox<bool>(false);

            int activeWorkers = 0;
            var block = new TaskCompletionSource();

            const int MaxSchedulerLimit = 2;

            Task t = Parallel.ForEachAsync(IterateUntilSet(box), new ParallelOptions { MaxDegreeOfParallelism = dop, TaskScheduler = new MaxConcurrencyLevelPassthroughTaskScheduler(MaxSchedulerLimit) }, async (item, cancellationToken) =>
            {
                Interlocked.Increment(ref activeWorkers);
                await block.Task;
            });
            Assert.False(t.IsCompleted);

            await Task.Delay(20); // give the loop some time to run

            box.Value = true;
            block.SetResult();
            await t;

            Assert.InRange(activeWorkers, 0, Math.Min(MaxSchedulerLimit, dop == -1 ? Environment.ProcessorCount : dop));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Dop_NegativeTaskSchedulerLimitTreatedAsDefault_Sync()
        {
            static IEnumerable<int> IterateUntilSet(StrongBox<bool> box)
            {
                int counter = 0;
                while (!box.Value)
                {
                    yield return counter++;
                }
            }

            var box = new StrongBox<bool>(false);

            int activeWorkers = 0;
            var block = new TaskCompletionSource();

            Task t = Parallel.ForEachAsync(IterateUntilSet(box), new ParallelOptions { TaskScheduler = new MaxConcurrencyLevelPassthroughTaskScheduler(-42) }, async (item, cancellationToken) =>
            {
                Interlocked.Increment(ref activeWorkers);
                await block.Task;
            });
            Assert.False(t.IsCompleted);

            await Task.Delay(20); // give the loop some time to run

            box.Value = true;
            block.SetResult();
            await t;

            Assert.InRange(activeWorkers, 0, Environment.ProcessorCount);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Dop_NegativeTaskSchedulerLimitTreatedAsDefault_Async()
        {
            static async IAsyncEnumerable<int> IterateUntilSet(StrongBox<bool> box)
            {
                int counter = 0;
                while (!box.Value)
                {
                    await Task.Yield();
                    yield return counter++;
                }
            }

            var box = new StrongBox<bool>(false);

            int activeWorkers = 0;
            var block = new TaskCompletionSource();

            Task t = Parallel.ForEachAsync(IterateUntilSet(box), new ParallelOptions { TaskScheduler = new MaxConcurrencyLevelPassthroughTaskScheduler(-42) }, async (item, cancellationToken) =>
            {
                Interlocked.Increment(ref activeWorkers);
                await block.Task;
            });
            Assert.False(t.IsCompleted);

            await Task.Delay(20); // give the loop some time to run

            box.Value = true;
            block.SetResult();
            await t;

            Assert.InRange(activeWorkers, 0, Environment.ProcessorCount);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task RunsAsynchronously_EvenForEntirelySynchronousWork_Sync()
        {
            static IEnumerable<int> Iterate()
            {
                while (true) yield return 0;
            }

            var cts = new CancellationTokenSource();

            Task t = Parallel.ForEachAsync(Iterate(), cts.Token, (item, cancellationToken) => default);
            Assert.False(t.IsCompleted);

            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task RunsAsynchronously_EvenForEntirelySynchronousWork_Async()
        {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
            static async IAsyncEnumerable<int> IterateAsync()
#pragma warning restore CS1998
            {
                while (true) yield return 0;
            }

            var cts = new CancellationTokenSource();

            Task t = Parallel.ForEachAsync(IterateAsync(), cts.Token, (item, cancellationToken) => default);
            Assert.False(t.IsCompleted);

            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(-1)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        [InlineData(128)]
        public async Task Dop_WorkersCreatedRespectingLimit_Async(int dop)
        {
            static async IAsyncEnumerable<int> IterateUntilSetAsync(StrongBox<bool> box)
            {
                int counter = 0;
                while (!box.Value)
                {
                    await Task.Yield();
                    yield return counter++;
                }
            }

            var box = new StrongBox<bool>(false);

            int activeWorkers = 0;
            var block = new TaskCompletionSource();

            Task t = Parallel.ForEachAsync(IterateUntilSetAsync(box), new ParallelOptions { MaxDegreeOfParallelism = dop }, async (item, cancellationToken) =>
            {
                Interlocked.Increment(ref activeWorkers);
                await block.Task;
            });
            Assert.False(t.IsCompleted);

            await Task.Delay(20); // give the loop some time to run

            box.Value = true;
            block.SetResult();
            await t;

            Assert.InRange(activeWorkers, 0, dop == -1 ? Environment.ProcessorCount : dop);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task EmptySource_Sync()
        {
            int counter = 0;
            await Parallel.ForEachAsync(Enumerable.Range(0, 0), (item, cancellationToken) =>
            {
                Interlocked.Increment(ref counter);
                return default;
            });

            Assert.Equal(0, counter);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task EmptySource_Async()
        {
            int counter = 0;
            await Parallel.ForEachAsync(EnumerableRangeAsync(0, 0), (item, cancellationToken) =>
            {
                Interlocked.Increment(ref counter);
                return default;
            });

            Assert.Equal(0, counter);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AllItemsEnumeratedOnce_Sync(bool yield)
        {
            const int Start = 10, Count = 100;

            var set = new HashSet<int>();

            await Parallel.ForEachAsync(Enumerable.Range(Start, Count), async (item, cancellationToken) =>
            {
                lock (set)
                {
                    Assert.True(set.Add(item));
                }

                if (yield)
                {
                    await Task.Yield();
                }
            });

            for (int i = Start; i < Start + Count; i++)
            {
                Assert.True(set.Contains(i));
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AllItemsEnumeratedOnce_Async(bool yield)
        {
            const int Start = 10, Count = 100;

            var set = new HashSet<int>();

            await Parallel.ForEachAsync(EnumerableRangeAsync(Start, Count, yield), async (item, cancellationToken) =>
            {
                lock (set)
                {
                    Assert.True(set.Add(item));
                }

                if (yield)
                {
                    await Task.Yield();
                }
            });

            for (int i = Start; i < Start + Count; i++)
            {
                Assert.True(set.Contains(i));
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TaskScheduler_AllCodeExecutedOnCorrectScheduler_Sync(bool defaultScheduler)
        {
            TaskScheduler scheduler = defaultScheduler ?
                TaskScheduler.Default :
                new ConcurrentExclusiveSchedulerPair().ConcurrentScheduler;

            TaskScheduler otherScheduler = new ConcurrentExclusiveSchedulerPair().ConcurrentScheduler;

            IEnumerable<int> Iterate()
            {
                Assert.Same(scheduler, TaskScheduler.Current);
                for (int i = 1; i <= 100; i++)
                {
                    yield return i;
                    Assert.Same(scheduler, TaskScheduler.Current);
                }
            }

            var cq = new ConcurrentQueue<int>();

            await Parallel.ForEachAsync(Iterate(), new ParallelOptions { TaskScheduler = scheduler }, async (item, cancellationToken) =>
            {
                Assert.Same(scheduler, TaskScheduler.Current);
                await Task.Yield();
                cq.Enqueue(item);

                if (item % 10 == 0)
                {
                    await new SwitchTo(otherScheduler);
                }
            });

            Assert.Equal(Enumerable.Range(1, 100), cq.OrderBy(i => i));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TaskScheduler_AllCodeExecutedOnCorrectScheduler_Async(bool defaultScheduler)
        {
            TaskScheduler scheduler = defaultScheduler ?
                TaskScheduler.Default :
                new ConcurrentExclusiveSchedulerPair().ConcurrentScheduler;

            TaskScheduler otherScheduler = new ConcurrentExclusiveSchedulerPair().ConcurrentScheduler;

            async IAsyncEnumerable<int> Iterate()
            {
                Assert.Same(scheduler, TaskScheduler.Current);
                for (int i = 1; i <= 100; i++)
                {
                    await Task.Yield();
                    yield return i;
                    Assert.Same(scheduler, TaskScheduler.Current);
                }
            }

            var cq = new ConcurrentQueue<int>();

            await Parallel.ForEachAsync(Iterate(), new ParallelOptions { TaskScheduler = scheduler }, async (item, cancellationToken) =>
            {
                Assert.Same(scheduler, TaskScheduler.Current);
                await Task.Yield();
                cq.Enqueue(item);

                if (item % 10 == 0)
                {
                    await new SwitchTo(otherScheduler);
                }
            });

            Assert.Equal(Enumerable.Range(1, 100), cq.OrderBy(i => i));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Cancellation_CancelsIterationAndReturnsCanceledTask_Sync()
        {
            static async IAsyncEnumerable<int> Infinite()
            {
                int i = 0;
                while (true)
                {
                    await Task.Yield();
                    yield return i++;
                }
            }

            using var cts = new CancellationTokenSource(10);
            OperationCanceledException oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Parallel.ForEachAsync(Infinite(), cts.Token, async (item, cancellationToken) =>
            {
                await Task.Yield();
            }));
            Assert.Equal(cts.Token, oce.CancellationToken);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Cancellation_CancelsIterationAndReturnsCanceledTask_Async()
        {
            static async IAsyncEnumerable<int> InfiniteAsync()
            {
                int i = 0;
                while (true)
                {
                    await Task.Yield();
                    yield return i++;
                }
            }

            using var cts = new CancellationTokenSource(10);
            OperationCanceledException oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Parallel.ForEachAsync(InfiniteAsync(), cts.Token, async (item, cancellationToken) =>
            {
                await Task.Yield();
            }));
            Assert.Equal(cts.Token, oce.CancellationToken);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Cancellation_CorrectTokenPassedToAsyncEnumerator()
        {
            static async IAsyncEnumerable<CancellationToken> YieldTokenAsync([EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.Yield();
                yield return cancellationToken;
            }

            await Parallel.ForEachAsync(YieldTokenAsync(default), (item, cancellationToken) =>
            {
                Assert.Equal(cancellationToken, item);
                return default;
            });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Cancellation_SameTokenPassedToEveryInvocation_Sync()
        {
            var cq = new ConcurrentQueue<CancellationToken>();

            await Parallel.ForEachAsync(Enumerable.Range(1, 100), async (item, cancellationToken) =>
            {
                cq.Enqueue(cancellationToken);
                await Task.Yield();
            });

            Assert.Equal(100, cq.Count);
            Assert.Equal(1, cq.Distinct().Count());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Cancellation_SameTokenPassedToEveryInvocation_Async()
        {
            var cq = new ConcurrentQueue<CancellationToken>();

            await Parallel.ForEachAsync(EnumerableRangeAsync(1, 100), async (item, cancellationToken) =>
            {
                cq.Enqueue(cancellationToken);
                await Task.Yield();
            });

            Assert.Equal(100, cq.Count);
            Assert.Equal(1, cq.Distinct().Count());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Cancellation_HasPriorityOverExceptions_Sync()
        {
            static IEnumerable<int> Iterate()
            {
                int counter = 0;
                while (true) yield return counter++;
            }

            var tcs = new TaskCompletionSource();
            var cts = new CancellationTokenSource();

            Task t = Parallel.ForEachAsync(Iterate(), new ParallelOptions { CancellationToken = cts.Token, MaxDegreeOfParallelism = 2 }, async (item, cancellationToken) =>
            {
                if (item == 0)
                {
                    await tcs.Task;
                    cts.Cancel();
                    throw new FormatException();
                }
                else
                {
                    tcs.TrySetResult();
                    await Task.Yield();
                }
            });

            OperationCanceledException oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
            Assert.Equal(cts.Token, oce.CancellationToken);
            Assert.True(t.IsCanceled);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Cancellation_HasPriorityOverExceptions_Async()
        {
            static async IAsyncEnumerable<int> Iterate()
            {
                int counter = 0;
                while (true)
                {
                    await Task.Yield();
                    yield return counter++;
                }
            }

            var tcs = new TaskCompletionSource();
            var cts = new CancellationTokenSource();

            Task t = Parallel.ForEachAsync(Iterate(), new ParallelOptions { CancellationToken = cts.Token, MaxDegreeOfParallelism = 2 }, async (item, cancellationToken) =>
            {
                if (item == 0)
                {
                    await tcs.Task;
                    cts.Cancel();
                    throw new FormatException();
                }
                else
                {
                    tcs.TrySetResult();
                    await Task.Yield();
                }
            });

            OperationCanceledException oce = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
            Assert.Equal(cts.Token, oce.CancellationToken);
            Assert.True(t.IsCanceled);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Exception_FromGetEnumerator_Sync()
        {
            Task t = Parallel.ForEachAsync((IEnumerable<int>)new ThrowsFromGetEnumerator(), (item, cancellationToken) => default);
            Assert.True(t.IsFaulted);
            Assert.Equal(1, t.Exception.InnerExceptions.Count);
            Assert.IsType<FormatException>(t.Exception.InnerException);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Exception_FromGetEnumerator_Async()
        {
            Task t = Parallel.ForEachAsync((IAsyncEnumerable<int>)new ThrowsFromGetEnumerator(), (item, cancellationToken) => default);
            Assert.True(t.IsFaulted);
            Assert.Equal(1, t.Exception.InnerExceptions.Count);
            Assert.IsType<DivideByZeroException>(t.Exception.InnerException);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Exception_NullFromGetEnumerator_Sync()
        {
            Task t = Parallel.ForEachAsync((IEnumerable<int>)new ReturnsNullFromGetEnumerator(), (item, cancellationToken) => default);
            Assert.True(t.IsFaulted);
            Assert.Equal(1, t.Exception.InnerExceptions.Count);
            Assert.IsType<InvalidOperationException>(t.Exception.InnerException);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void Exception_NullFromGetEnumerator_Async()
        {
            Task t = Parallel.ForEachAsync((IAsyncEnumerable<int>)new ReturnsNullFromGetEnumerator(), (item, cancellationToken) => default);
            Assert.True(t.IsFaulted);
            Assert.Equal(1, t.Exception.InnerExceptions.Count);
            Assert.IsType<InvalidOperationException>(t.Exception.InnerException);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Exception_FromMoveNext_Sync()
        {
            static IEnumerable<int> Iterate()
            {
                for (int i = 0; i < 10; i++)
                {
                    if (i == 4)
                    {
                        throw new FormatException();
                    }
                    yield return i;
                }
            }

            Task t = Parallel.ForEachAsync(Iterate(), (item, cancellationToken) => default);
            await Assert.ThrowsAsync<FormatException>(() => t);
            Assert.True(t.IsFaulted);
            Assert.Equal(1, t.Exception.InnerExceptions.Count);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Exception_FromMoveNext_Async()
        {
            static async IAsyncEnumerable<int> Iterate()
            {
                for (int i = 0; i < 10; i++)
                {
                    await Task.Yield();
                    if (i == 4)
                    {
                        throw new FormatException();
                    }
                    yield return i;
                }
            }

            Task t = Parallel.ForEachAsync(Iterate(), (item, cancellationToken) => default);
            await Assert.ThrowsAsync<FormatException>(() => t);
            Assert.True(t.IsFaulted);
            Assert.Equal(1, t.Exception.InnerExceptions.Count);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Exception_FromLoopBody_Sync()
        {
            static IEnumerable<int> Iterate()
            {
                yield return 1;
                yield return 2;
            }

            var barrier = new Barrier(2);
            Task t = Parallel.ForEachAsync(Iterate(), new ParallelOptions { MaxDegreeOfParallelism = barrier.ParticipantCount }, (item, cancellationToken) =>
            {
                barrier.SignalAndWait();
                throw item switch
                {
                    1 => new FormatException(),
                    2 => new InvalidTimeZoneException(),
                    _ => new Exception()
                };
            });
            await Assert.ThrowsAnyAsync<Exception>(() => t);
            Assert.True(t.IsFaulted);
            Assert.Equal(2, t.Exception.InnerExceptions.Count);
            Assert.Contains(t.Exception.InnerExceptions, e => e is FormatException);
            Assert.Contains(t.Exception.InnerExceptions, e => e is InvalidTimeZoneException);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Exception_FromLoopBody_Async()
        {
            static async IAsyncEnumerable<int> Iterate()
            {
                await Task.Yield();
                yield return 1;
                yield return 2;
                yield return 3;
                yield return 4;
            }

            int remaining = 4;
            var tcs = new TaskCompletionSource();

            Task t = Parallel.ForEachAsync(Iterate(), new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (item, cancellationToken) =>
            {
                if (Interlocked.Decrement(ref remaining) == 0)
                {
                    tcs.SetResult();
                }
                await tcs.Task;

                throw item switch
                {
                    1 => new FormatException(),
                    2 => new InvalidTimeZoneException(),
                    3 => new ArithmeticException(),
                    4 => new DivideByZeroException(),
                    _ => new Exception()
                };
            });
            await Assert.ThrowsAnyAsync<Exception>(() => t);
            Assert.True(t.IsFaulted);
            Assert.Equal(4, t.Exception.InnerExceptions.Count);
            Assert.Contains(t.Exception.InnerExceptions, e => e is FormatException);
            Assert.Contains(t.Exception.InnerExceptions, e => e is InvalidTimeZoneException);
            Assert.Contains(t.Exception.InnerExceptions, e => e is ArithmeticException);
            Assert.Contains(t.Exception.InnerExceptions, e => e is DivideByZeroException);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Exception_FromDispose_Sync()
        {
            Task t = Parallel.ForEachAsync((IEnumerable<int>)new ThrowsExceptionFromDispose(), (item, cancellationToken) => default);
            await Assert.ThrowsAsync<FormatException>(() => t);
            Assert.True(t.IsFaulted);
            Assert.Equal(1, t.Exception.InnerExceptions.Count);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Exception_FromDispose_Async()
        {
            Task t = Parallel.ForEachAsync((IAsyncEnumerable<int>)new ThrowsExceptionFromDispose(), (item, cancellationToken) => default);
            await Assert.ThrowsAsync<DivideByZeroException>(() => t);
            Assert.True(t.IsFaulted);
            Assert.Equal(1, t.Exception.InnerExceptions.Count);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Exception_ImplicitlyCancelsOtherWorkers_Sync()
        {
            static IEnumerable<int> Iterate()
            {
                int i = 0;
                while (true)
                {
                    yield return i++;
                }
            }

            await Assert.ThrowsAsync<Exception>(() => Parallel.ForEachAsync(Iterate(), async (item, cancellationToken) =>
            {
                await Task.Yield();
                if (item == 1000)
                {
                    throw new Exception();
                }
            }));

            await Assert.ThrowsAsync<FormatException>(() => Parallel.ForEachAsync(Iterate(), new ParallelOptions { MaxDegreeOfParallelism = 2 }, async (item, cancellationToken) =>
            {
                if (item == 0)
                {
                    throw new FormatException();
                }
                else
                {
                    Assert.Equal(1, item);
                    var tcs = new TaskCompletionSource();
                    cancellationToken.Register(() => tcs.SetResult());
                    await tcs.Task;
                }
            }));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task Exception_ImplicitlyCancelsOtherWorkers_Async()
        {
            static async IAsyncEnumerable<int> Iterate()
            {
                int i = 0;
                while (true)
                {
                    await Task.Yield();
                    yield return i++;
                }
            }

            await Assert.ThrowsAsync<Exception>(() => Parallel.ForEachAsync(Iterate(), async (item, cancellationToken) =>
            {
                await Task.Yield();
                if (item == 1000)
                {
                    throw new Exception();
                }
            }));

            await Assert.ThrowsAsync<FormatException>(() => Parallel.ForEachAsync(Iterate(), new ParallelOptions { MaxDegreeOfParallelism = 2 }, async (item, cancellationToken) =>
            {
                if (item == 0)
                {
                    throw new FormatException();
                }
                else
                {
                    Assert.Equal(1, item);
                    var tcs = new TaskCompletionSource();
                    cancellationToken.Register(() => tcs.SetResult());
                    await tcs.Task;
                }
            }));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ExecutionContext_FlowsToWorkerBodies_Sync(bool defaultScheduler)
        {
            TaskScheduler scheduler = defaultScheduler ?
                TaskScheduler.Default :
                new ConcurrentExclusiveSchedulerPair().ConcurrentScheduler;

            static IEnumerable<int> Iterate()
            {
                for (int i = 0; i < 100; i++)
                {
                    yield return i;
                }
            }

            var al = new AsyncLocal<int>();
            al.Value = 42;
            await Parallel.ForEachAsync(Iterate(), async (item, cancellationToken) =>
            {
                await Task.Yield();
                Assert.Equal(42, al.Value);
            });
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task ExecutionContext_FlowsToWorkerBodies_Async(bool defaultScheduler, bool flowContext)
        {
            TaskScheduler scheduler = defaultScheduler ?
                TaskScheduler.Default :
                new ConcurrentExclusiveSchedulerPair().ConcurrentScheduler;

            static async IAsyncEnumerable<int> Iterate()
            {
                for (int i = 0; i < 100; i++)
                {
                    await Task.Yield();
                    yield return i;
                }
            }

            var al = new AsyncLocal<int>();
            al.Value = 42;

            if (!flowContext)
            {
                ExecutionContext.SuppressFlow();
            }

            Task t = Parallel.ForEachAsync(Iterate(), async (item, cancellationToken) =>
            {
                await Task.Yield();
                Assert.Equal(flowContext ? 42 : 0, al.Value);
            });

            if (!flowContext)
            {
                ExecutionContext.RestoreFlow();
            }

            await t;
        }

        private static async IAsyncEnumerable<int> EnumerableRangeAsync(int start, int count, bool yield = true)
        {
            for (int i = start; i < start + count; i++)
            {
                if (yield)
                {
                    await Task.Yield();
                }

                yield return i;
            }
        }

        private sealed class ThrowsFromGetEnumerator : IAsyncEnumerable<int>, IEnumerable<int>
        {
            public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default) => throw new DivideByZeroException();
            public IEnumerator<int> GetEnumerator() => throw new FormatException();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class ReturnsNullFromGetEnumerator : IAsyncEnumerable<int>, IEnumerable<int>
        {
            public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default) => null;
            public IEnumerator<int> GetEnumerator() => null;
            IEnumerator IEnumerable.GetEnumerator() => null;
        }

        private sealed class ThrowsExceptionFromDispose : IAsyncEnumerable<int>, IEnumerable<int>, IAsyncEnumerator<int>, IEnumerator<int>
        {
            public int Current => throw new NotImplementedException();
            object IEnumerator.Current => throw new NotImplementedException();

            public void Dispose() => throw new FormatException();
            public ValueTask DisposeAsync() => throw new DivideByZeroException();

            public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default) => this;
            public IEnumerator<int> GetEnumerator() => this;
            IEnumerator IEnumerable.GetEnumerator() => this;

            public bool MoveNext() => false;
            public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(false);

            public void Reset() => throw new NotImplementedException();
        }

        private sealed class SwitchTo : INotifyCompletion
        {
            private readonly TaskScheduler _scheduler;

            public SwitchTo(TaskScheduler scheduler) => _scheduler = scheduler;

            public SwitchTo GetAwaiter() => this;
            public bool IsCompleted => false;
            public void GetResult() { }
            public void OnCompleted(Action continuation) => Task.Factory.StartNew(continuation, CancellationToken.None, TaskCreationOptions.None, _scheduler);
        }

        private sealed class MaxConcurrencyLevelPassthroughTaskScheduler : TaskScheduler
        {
            public MaxConcurrencyLevelPassthroughTaskScheduler(int maximumConcurrencyLevel) =>
                MaximumConcurrencyLevel = maximumConcurrencyLevel;

            protected override IEnumerable<Task> GetScheduledTasks() => Array.Empty<Task>();
            protected override void QueueTask(Task task) => ThreadPool.QueueUserWorkItem(_ => TryExecuteTask(task));
            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => TryExecuteTask(task);

            public override int MaximumConcurrencyLevel { get; }
        }
    }
}
