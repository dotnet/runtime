// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public void AccessAfterReset_Fails()
        {
            var mrvts = new ManualResetValueTaskSource<int>();
            mrvts.Reset();
            Assert.Throws<InvalidOperationException>(() => mrvts.GetResult(0));
            Assert.Throws<InvalidOperationException>(() => mrvts.GetStatus(0));
            Assert.Throws<InvalidOperationException>(() => mrvts.OnCompleted(_ => { }, new object(), 0, ValueTaskSourceOnCompletedFlags.None));
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

        // The following is a sketch of an implementation to explore the IAsyncEnumerable feature.
        // This should be replaced with the real implementation when available.
        // https://github.com/dotnet/csharplang/issues/43

        internal interface IAsyncEnumerable<out T>
        {
            IAsyncEnumerator<T> GetAsyncEnumerator();
        }

        internal interface IAsyncEnumerator<out T> : IAsyncDisposable
        {
            // One of two potential shapes for IAsyncEnumerator; another is
            //     ValueTask<bool> WaitForNextAsync();
            //     bool TryGetNext(out T current);
            // which has several advantages, including that while the next
            // result is available synchronously, it incurs only one interface
            // call rather than two, and doesn't incur any boilerplate related
            // to await.

            ValueTask<bool> MoveNextAsync();
            T Current { get; }
        }

        internal interface IAsyncDisposable
        {
            ValueTask DisposeAsync();
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
            IStrongBox<ManualResetValueTaskSourceLogic<bool>>, // exposes its ValueTaskSource logic implementation
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
            private ManualResetValueTaskSourceLogic<bool> _vts; // mutable struct; do not make this readonly
            /// <summary>Builder used for efficiently waiting and appropriately managing ExecutionContext.</summary>
            private AsyncTaskMethodBuilder _builder = AsyncTaskMethodBuilder.Create(); // mutable struct; do not make this readonly

            private readonly int _param_items;

            private int _local_items;
            private int _local_i;
            private TaskAwaiter _awaiter0;

            public CountAsyncEnumerable(int items)
            {
                _local_items = _param_items = items;
                _vts = new ManualResetValueTaskSourceLogic<bool>(this);
            }

            ref ManualResetValueTaskSourceLogic<bool> IStrongBox<ManualResetValueTaskSourceLogic<bool>>.Value => ref _vts;

            public IAsyncEnumerator<int> GetAsyncEnumerator() =>
                Interlocked.CompareExchange(ref _state, StateStart, StateCtor) == StateCtor ?
                    this :
                    new CountAsyncEnumerable(_param_items) { _state = StateStart };

            public ValueTask<bool> MoveNextAsync()
            {
                _vts.Reset();

                CountAsyncEnumerable inst = this;
                _builder.Start(ref inst); // invokes MoveNext, protected by ExecutionContext guards

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
                                _awaiter0 = Task.Delay(_local_i).GetAwaiter();
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
