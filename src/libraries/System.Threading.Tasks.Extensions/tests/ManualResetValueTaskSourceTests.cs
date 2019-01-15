// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Threading.Tasks.Sources.Tests
{
    public class ManualResetValueTaskSourceTests
    {
        [Fact]
        public async Task ReuseInstanceWithResets_Success()
        {
            var mrvts = new ManualResetValueTaskSource<int>();

            for (short i = 42; i < 48; i++)
            {
                var ignored = Task.Delay(1).ContinueWith(_ => mrvts.SetResult(i));
                Assert.Equal(i, await new ValueTask<int>(mrvts, mrvts.Version));
                Assert.Equal(i, await new ValueTask<int>(mrvts, mrvts.Version)); // can use multiple times until it's reset

                mrvts.Reset();
            }
        }

        [Fact]
        public void AccessWrongVersion_Fails()
        {
            var mrvts = new ManualResetValueTaskSource<int>();
            mrvts.Reset();

            Assert.Throws<InvalidOperationException>(() => mrvts.GetResult(0));
            Assert.Throws<InvalidOperationException>(() => mrvts.GetStatus(0));
            Assert.Throws<InvalidOperationException>(() => mrvts.OnCompleted(_ => { }, new object(), 0, ValueTaskSourceOnCompletedFlags.None));

            Assert.Throws<InvalidOperationException>(() => mrvts.GetResult(2));
            Assert.Throws<InvalidOperationException>(() => mrvts.GetStatus(2));
            Assert.Throws<InvalidOperationException>(() => mrvts.OnCompleted(_ => { }, new object(), 2, ValueTaskSourceOnCompletedFlags.None));
        }

        [Fact]
        public void SetTwice_Fails()
        {
            var mrvts = new ManualResetValueTaskSource<int>();

            mrvts.SetResult(42);
            Assert.Throws<InvalidOperationException>(() => mrvts.SetResult(42));
            Assert.Throws<InvalidOperationException>(() => mrvts.SetException(new Exception()));

            mrvts.Reset();
            mrvts.SetException(new Exception());
            Assert.Throws<InvalidOperationException>(() => mrvts.SetResult(42));
            Assert.Throws<InvalidOperationException>(() => mrvts.SetException(new Exception()));
        }

        [Fact]
        public void GetResult_BeforeCompleted_Fails()
        {
            var mrvts = new ManualResetValueTaskSource<int>();
            Assert.Throws<InvalidOperationException>(() => mrvts.GetResult(0));
        }

        [Fact]
        public void SetResult_BeforeOnCompleted_ResultAvailableSynchronously()
        {
            var mrvts = new ManualResetValueTaskSource<int>();
            mrvts.Reset();
            mrvts.Reset();
            Assert.Equal(2, mrvts.Version);

            mrvts.SetResult(42);

            Assert.Equal(ValueTaskSourceStatus.Succeeded, mrvts.GetStatus(2));
            Assert.Equal(42, mrvts.GetResult(2));

            var mres = new ManualResetEventSlim();
            mrvts.OnCompleted(s => ((ManualResetEventSlim)s).Set(), mres, 2, ValueTaskSourceOnCompletedFlags.None);
            mres.Wait();

            Assert.Equal(2, mrvts.Version);
        }

        [Fact]
        public async Task SetResult_AfterOnCompleted_ResultAvailableAsynchronously()
        {
            var mrvts = new ManualResetValueTaskSource<int>();
            mrvts.Reset();
            mrvts.Reset();
            Assert.Equal(2, mrvts.Version);

            Assert.Equal(ValueTaskSourceStatus.Pending, mrvts.GetStatus(2));
            Assert.Throws<InvalidOperationException>(() => mrvts.GetResult(2));

            var onCompletedRan = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            mrvts.OnCompleted(s => ((TaskCompletionSource<bool>)s).SetResult(true), onCompletedRan, 2, ValueTaskSourceOnCompletedFlags.None);

            Assert.False(onCompletedRan.Task.IsCompleted);
            await Task.Delay(1);
            Assert.False(onCompletedRan.Task.IsCompleted);

            mrvts.SetResult(42);
            Assert.Equal(ValueTaskSourceStatus.Succeeded, mrvts.GetStatus(2));
            Assert.Equal(42, mrvts.GetResult(2));

            await onCompletedRan.Task;

            Assert.Equal(2, mrvts.Version);
        }

        [Fact]
        public void SetException_BeforeOnCompleted_ResultAvailableSynchronously()
        {
            var mrvts = new ManualResetValueTaskSource<int>();
            mrvts.Reset();
            mrvts.Reset();
            Assert.Equal(2, mrvts.Version);

            var e = new FormatException();
            mrvts.SetException(e);

            Assert.Equal(ValueTaskSourceStatus.Faulted, mrvts.GetStatus(2));
            Assert.Same(e, Assert.Throws<FormatException>(() => mrvts.GetResult(2)));

            var mres = new ManualResetEventSlim();
            mrvts.OnCompleted(s => ((ManualResetEventSlim)s).Set(), mres, 2, ValueTaskSourceOnCompletedFlags.None);
            mres.Wait();

            Assert.Equal(2, mrvts.Version);
        }

        [Fact]
        public async Task SetException_AfterOnCompleted_ResultAvailableAsynchronously()
        {
            var mrvts = new ManualResetValueTaskSource<int>();
            mrvts.Reset();
            mrvts.Reset();
            Assert.Equal(2, mrvts.Version);

            Assert.Equal(ValueTaskSourceStatus.Pending, mrvts.GetStatus(2));
            Assert.Throws<InvalidOperationException>(() => mrvts.GetResult(2));

            var onCompletedRan = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            mrvts.OnCompleted(s => ((TaskCompletionSource<bool>)s).SetResult(true), onCompletedRan, 2, ValueTaskSourceOnCompletedFlags.None);

            Assert.False(onCompletedRan.Task.IsCompleted);
            await Task.Delay(1);
            Assert.False(onCompletedRan.Task.IsCompleted);

            var e = new FormatException();
            mrvts.SetException(e);

            Assert.Equal(ValueTaskSourceStatus.Faulted, mrvts.GetStatus(2));
            Assert.Same(e, Assert.Throws<FormatException>(() => mrvts.GetResult(2)));

            await onCompletedRan.Task;

            Assert.Equal(2, mrvts.Version);
        }

        [Fact]
        public void SetException_OperationCanceledException_StatusIsCanceled()
        {
            var mrvts = new ManualResetValueTaskSource<int>();
            var e = new OperationCanceledException();
            mrvts.SetException(e);
            Assert.Equal(ValueTaskSourceStatus.Canceled, mrvts.GetStatus(0));
            Assert.Same(e, Assert.Throws<OperationCanceledException>(() => mrvts.GetResult(0)));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FlowContext_SetBeforeOnCompleted_FlowsIfExpected(bool flowContext)
        {
            var mres = new ManualResetEventSlim();
            var mrvts = new ManualResetValueTaskSource<int>();

            mrvts.RunContinuationsAsynchronously = true;

            mrvts.SetResult(1);

            var al = new AsyncLocal<int>();
            al.Value = 42;
            mrvts.OnCompleted(
                _ => { Assert.Equal(flowContext ? 42 : 0, al.Value); mres.Set(); },
                null,
                0,
                flowContext ? ValueTaskSourceOnCompletedFlags.FlowExecutionContext : ValueTaskSourceOnCompletedFlags.None);

            mres.Wait();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FlowContext_SetAfterOnCompleted_FlowsIfExpected(bool flowContext)
        {
            var mres = new ManualResetEventSlim();
            var mrvts = new ManualResetValueTaskSource<int>();

            mrvts.RunContinuationsAsynchronously = true;

            var al = new AsyncLocal<int>();
            al.Value = 42;
            mrvts.OnCompleted(
                _ => { Assert.Equal(flowContext ? 42 : 0, al.Value); mres.Set(); },
                null,
                0,
                flowContext ? ValueTaskSourceOnCompletedFlags.FlowExecutionContext : ValueTaskSourceOnCompletedFlags.None);

            mrvts.SetResult(1);

            mres.Wait();
        }

        [Fact]
        public void OnCompleted_NullDelegate_Throws()
        {
            var mrvts = new ManualResetValueTaskSource<int>();
            AssertExtensions.Throws<ArgumentNullException>("continuation", () => mrvts.OnCompleted(null, new object(), 0, ValueTaskSourceOnCompletedFlags.None));
        }

        [Fact]
        public void OnCompleted_UsedTwiceBeforeCompletion_Throws()
        {
            var mrvts = new ManualResetValueTaskSource<int>();
            mrvts.OnCompleted(_ => { }, null, 0, ValueTaskSourceOnCompletedFlags.None);
            Assert.Throws<InvalidOperationException>(() => mrvts.OnCompleted(_ => { }, null, 0, ValueTaskSourceOnCompletedFlags.None));
        }

        [Fact]
        public void OnCompleted_UnknownFlagsIgnored()
        {
            var mrvts = new ManualResetValueTaskSource<int>();
            mrvts.OnCompleted(_ => { }, new object(), 0, (ValueTaskSourceOnCompletedFlags)int.MaxValue);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void OnCompleted_ContinuationAlwaysInvokedAsynchronously(bool runContinuationsAsynchronously)
        {
            var mres = new ManualResetEventSlim();
            var mrvts = new ManualResetValueTaskSource<int>() { RunContinuationsAsynchronously = runContinuationsAsynchronously };
            for (short i = 0; i < 10; i++)
            {
                int threadId = Environment.CurrentManagedThreadId;
                mrvts.SetResult(42);
                mrvts.OnCompleted(
                    _ =>
                    {
                        Assert.NotEqual(threadId, Environment.CurrentManagedThreadId);
                        mres.Set();
                    },
                    null,
                    i,
                    ValueTaskSourceOnCompletedFlags.None);
                mrvts.Reset();
                mres.Wait();
                mres.Reset();
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SetResult_RunContinuationsAsynchronously_ContinuationInvokedAccordingly(bool runContinuationsAsynchronously)
        {
            var mres = new ManualResetEventSlim();
            var mrvts = new ManualResetValueTaskSource<int>() { RunContinuationsAsynchronously = runContinuationsAsynchronously };
            for (short i = 0; i < 10; i++)
            {
                int threadId = Environment.CurrentManagedThreadId;
                mrvts.OnCompleted(
                    _ =>
                    {
                        Assert.Equal(!runContinuationsAsynchronously, threadId == Environment.CurrentManagedThreadId);
                        mres.Set();
                    },
                    null,
                    i,
                    ValueTaskSourceOnCompletedFlags.None);
                mrvts.SetResult(42);
                mres.Wait();
                mrvts.Reset();
                mres.Reset();
            }
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, false)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public async Task SynchronizationContext_CaptureIfRequested(
            bool runContinuationsAsynchronously, bool captureSyncCtx, bool setBeforeOnCompleted)
        {
            await Task.Run(async () => // escape xunit sync ctx
            {
                var mrvts = new ManualResetValueTaskSource<int>() { RunContinuationsAsynchronously = runContinuationsAsynchronously };

                if (setBeforeOnCompleted)
                {
                    mrvts.SetResult(42);
                }

                var tcs = new TaskCompletionSource<bool>();
                var sc = new TrackingSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(sc);
                Assert.Equal(0, sc.Posts);
                mrvts.OnCompleted(
                    _ => tcs.SetResult(true),
                    null,
                    0,
                    captureSyncCtx ? ValueTaskSourceOnCompletedFlags.UseSchedulingContext : ValueTaskSourceOnCompletedFlags.None);
                SynchronizationContext.SetSynchronizationContext(null);

                if (!setBeforeOnCompleted)
                {
                    mrvts.SetResult(42);
                }

                await tcs.Task;
                Assert.Equal(captureSyncCtx ? 1 : 0, sc.Posts);
            });
        }

        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, false)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public async Task TaskScheduler_CaptureIfRequested(
            bool runContinuationsAsynchronously, bool captureTaskScheduler, bool setBeforeOnCompleted)
        {
            await Task.Run(async () => // escape xunit sync ctx
            {
                var mrvts = new ManualResetValueTaskSource<int>() { RunContinuationsAsynchronously = runContinuationsAsynchronously };

                if (setBeforeOnCompleted)
                {
                    mrvts.SetResult(42);
                }

                var tcs = new TaskCompletionSource<bool>();
                var ts = new TrackingTaskScheduler();
                Assert.Equal(0, ts.QueueTasks);
                await Task.Factory.StartNew(() =>
                {
                    mrvts.OnCompleted(
                        _ => tcs.SetResult(true),
                        null,
                        0,
                        captureTaskScheduler ? ValueTaskSourceOnCompletedFlags.UseSchedulingContext : ValueTaskSourceOnCompletedFlags.None);
                }, CancellationToken.None, TaskCreationOptions.None, ts);

                if (!setBeforeOnCompleted)
                {
                    mrvts.SetResult(42);
                }

                await tcs.Task;
                Assert.Equal(captureTaskScheduler ? 2 : 1, ts.QueueTasks);
            });
        }

        private sealed class TrackingSynchronizationContext : SynchronizationContext
        {
            public int Posts;

            public override void Post(SendOrPostCallback d, object state)
            {
                Interlocked.Increment(ref Posts);
                base.Post(d, state);
            }
        }

        private sealed class TrackingTaskScheduler : TaskScheduler
        {
            public int QueueTasks;

            protected override void QueueTask(Task task)
            {
                QueueTasks++;
                ThreadPool.QueueUserWorkItem(_ => TryExecuteTask(task));
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => false;
            protected override IEnumerable<Task> GetScheduledTasks() => null;
        }

        [Fact]
        public async Task AsyncEnumerable_Success()
        {
            // Equivalent to:
            //   int total = 0;
            //   foreach async(int i in CountAsync(20))
            //   {
            //       total += i;
            //   }
            //   Assert.Equal(190, i);

            IAsyncEnumerator<int> enumerator = CountAsync(20).GetAsyncEnumerator();
            try
            {
                int total = 0;
                while (await enumerator.MoveNextAsync())
                {
                    total += enumerator.Current;
                }
                Assert.Equal(190, total);
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
        }

        // Approximate compiler-generated code for:
        //     internal static AsyncEnumerable<int> CountAsync(int items)
        //     {
        //         for (int i = 0; i < items; i++)
        //         {
        //             await Task.Delay(i).ConfigureAwait(false);
        //             yield return i;
        //         }
        //     }
        internal static IAsyncEnumerable<int> CountAsync(int items) =>
            new CountAsyncEnumerable(items);

        private sealed class CountAsyncEnumerable :
            IAsyncEnumerable<int>,  // used as the enumerable itself
            IAsyncEnumerator<int>,  // used as the enumerator returned from first call to enumerable's GetAsyncEnumerator
            IValueTaskSource<bool>, // used as the backing store behind the ValueTask<bool> returned from each MoveNextAsync
            IAsyncStateMachine // uses existing builder's support for ExecutionContext, optimized awaits, etc.
        {
            // This implementation will generally incur only two allocations of overhead
            // for the entire enumeration:
            // - The CountAsyncEnumerable object itself.
            // - A throw-away task object inside of _builder.
            // The task built by the builder isn't necessary, but using the _builder allows
            // this implementation to a) avoid needing to be concerned with ExecutionContext
            // flowing, and b) enables the implementation to take advantage of optimizations
            // such as avoiding Action allocation when all awaited types are known to corelib.

            private const int StateStart = -1;
            private const int StateDisposed = -2;
            private const int StateCtor = -3;

            /// <summary>Current state of the state machine.</summary>
            private int _state = StateCtor;
            /// <summary>All of the logic for managing the IValueTaskSource implementation</summary>
            private ManualResetValueTaskSourceCore<bool> _vts; // mutable struct; do not make this readonly
            /// <summary>Builder used for efficiently waiting and appropriately managing ExecutionContext.</summary>
            private AsyncIteratorMethodBuilder _builder = AsyncIteratorMethodBuilder.Create(); // mutable struct; do not make this readonly

            private readonly int _param_items;

            private int _local_items;
            private int _local_i;
            private TaskAwaiter _awaiter0;
            private CancellationToken _cancellationToken;

            public CountAsyncEnumerable(int items)
            {
                _local_items = _param_items = items;
            }

            public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                CountAsyncEnumerable cae = Interlocked.CompareExchange(ref _state, StateStart, StateCtor) == StateCtor ?
                    this :
                    new CountAsyncEnumerable(_param_items) { _state = StateStart };
                cae._cancellationToken = cancellationToken;
                return cae;
            }

            public ValueTask<bool> MoveNextAsync()
            {
                _vts.Reset();

                CountAsyncEnumerable inst = this;
                _builder.MoveNext(ref inst); // invokes MoveNext, protected by ExecutionContext guards

                switch (_vts.GetStatus(_vts.Version))
                {
                    case ValueTaskSourceStatus.Succeeded:
                        return new ValueTask<bool>(_vts.GetResult(_vts.Version));
                    default:
                        return new ValueTask<bool>(this, _vts.Version);
                }
            }

            public ValueTask DisposeAsync()
            {
                _vts.Reset();
                _state = StateDisposed;
                return default;
            }

            public int Current { get; private set; }

            public void MoveNext()
            {
                try
                {
                    switch (_state)
                    {
                        case StateStart:
                            _local_i = 0;
                            goto case 0;

                        case 0:
                            if (_local_i < _local_items)
                            {
                                _awaiter0 = Task.Delay(_local_i, _cancellationToken).GetAwaiter();
                                if (!_awaiter0.IsCompleted)
                                {
                                    _state = 1;
                                    CountAsyncEnumerable inst = this;
                                    _builder.AwaitUnsafeOnCompleted(ref _awaiter0, ref inst);
                                    return;
                                }
                                goto case 1;
                            }
                            _state = int.MaxValue;
                            _builder.Complete();
                            _vts.SetResult(false);
                            return;

                        case 1:
                            _awaiter0.GetResult();
                            _awaiter0 = default;

                            Current = _local_i;
                            _state = 2;
                            _vts.SetResult(true);
                            return;

                        case 2:
                            _local_i++;
                            _state = 0;
                            goto case 0;

                        default:
                            throw new InvalidOperationException();
                    }
                }
                catch (Exception e)
                {
                    _state = int.MaxValue;
                    _builder.Complete();
                    _vts.SetException(e); // see https://github.com/dotnet/roslyn/issues/26567; we may want to move this out of the catch
                    return;
                }
            }
            void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine) { }

            bool IValueTaskSource<bool>.GetResult(short token) => _vts.GetResult(token);
            ValueTaskSourceStatus IValueTaskSource<bool>.GetStatus(short token) => _vts.GetStatus(token);
            void IValueTaskSource<bool>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) =>
                _vts.OnCompleted(continuation, state, token, flags);
        }
    }
}
