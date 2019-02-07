// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Threading.Channels
{
    public abstract partial class ChannelReader<T>
    {
        /// <summary>Creates an <see cref="IAsyncEnumerable{T}"/> that enables reading all of the data from the channel.</summary>
        /// <remarks>
        /// Each <see cref="IAsyncEnumerator{T}.MoveNextAsync"/> call that returns <c>true</c> will read the next item out of the channel.
        /// <see cref="IAsyncEnumerator{T}.MoveNextAsync"/> will return false once no more data is or will ever be available to read.
        /// </remarks>
        /// <returns></returns>
        public virtual IAsyncEnumerable<T> ReadAllAsync() => new AsyncEnumerable(this);

        // The following provides an implementation functionally equivalent to:
        //
        //     while (await _reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        //         while (_reader.TryRead(out T item))
        //             yield return item;
        //
        // It can be replaced by a compiler-generated iterator version once a compiler with
        // the appropriate support is available in corefx.  However, this also employs some
        // optimizations that the compiler doesn't currently support, namely https://github.com/dotnet/roslyn/issues/31248,
        // and also reuses the enumerable as the enumerator (which we couldn't do with cancellation
        // with compiler support), so we may want to stick with this implementation for now, regardless.

        private sealed class AsyncEnumerable : IAsyncEnumerable<T>, IAsyncEnumerator<T>, IValueTaskSource<bool>, IAsyncStateMachine
        {
            /// <summary>The reader being read / enumerated.</summary>
            private readonly ChannelReader<T> _reader;

            /// <summary>The cancellation token used to cancel operations.</summary>
            private CancellationToken _cancellationToken;
            /// <summary>The builder that represents the iterator.</summary>
            private AsyncIteratorMethodBuilder _builder;
            
            /// <summary>Core implementation for the <see cref="IValueTaskSource{TResult}"/> implementation.</summary>
            private ManualResetValueTaskSourceCore<bool> _promise;
            /// <summary>The state of the state machine.</summary>
            private int _state;
            /// <summary>Whether an item was produced in the call to <see cref="MoveNext"/>.</summary>
            private bool _itemAvailable = false;
            /// <summary>Whether <see cref="MoveNextAsync"/> should use <see cref="_promise"/> for its return value.</summary>
            private bool _usePromiseForResult = false;
            /// <summary>The awaiter for WaitToReadAsync calls.</summary>
            private ConfiguredValueTaskAwaitable<bool>.ConfiguredValueTaskAwaiter _waitToReadAwaiter;

            /// <summary>The current item to be returned from <see cref="IAsyncEnumerator{T}.Current"/>.</summary>
            private T _current;

            private enum State
            {
                NotEnumerated = -1,
                OuterLoop = 0,
                FinishingAwait = 1,
                HaveWaitToReadResult = 2,
                TryRead = 3,
                Completing = 4,
                Done = -2
            }

            internal AsyncEnumerable(ChannelReader<T> reader)
            {
                Debug.Assert(reader != null);

                _reader = reader;
                _state = (int)State.NotEnumerated;
                _builder = AsyncIteratorMethodBuilder.Create();
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
            {
                // Use this instance if it's never been enumerated before; otherwise, since it might still be in use,
                // create a new instance.
                AsyncEnumerable inst = Interlocked.CompareExchange(ref _state, (int)State.OuterLoop, (int)State.NotEnumerated) == (int)State.NotEnumerated ?
                    this :
                    new AsyncEnumerable(_reader) { _state = (int)State.OuterLoop };
                inst._cancellationToken = cancellationToken;
                return inst;
            }

            public ValueTask<bool> MoveNextAsync()
            {
                // Reset state for the next call.
                _promise.Reset();
                _usePromiseForResult = false;
                _itemAvailable = false;

                // Push the state machine forward.
                MoveNext();

                // If the operation completed asynchronously, return a ValueTask wrapping our promise-based implementation.
                // If the operation completed synchronously, return a bool-backed ValueTask.
                return _usePromiseForResult ?
                    new ValueTask<bool>(this, _promise.Version) :
                    new ValueTask<bool>(_itemAvailable);
            }

            public void MoveNext()
            {
                try
                {
                    bool waitToReadResult = false;
                    switch ((State)_state)
                    {
                        case State.OuterLoop:
                            ValueTask<bool> waitToReadTask = _reader.WaitToReadAsync(_cancellationToken);
                            if (waitToReadTask.IsCompleted)
                            {
                                // WaitToReadAsync completed.  Get the result and jump to process it.
                                waitToReadResult = waitToReadTask.GetAwaiter().GetResult();
                                _state = (int)State.HaveWaitToReadResult;
                                goto case State.HaveWaitToReadResult;
                            }

                            // WaitToReadAsync wasn't yet complete.  Mark that the promise
                            // is being used for the result, set where we should return to when it completes,
                            // store the awaiter, and hook up the continuation.
                            _usePromiseForResult = true;
                            _state = (int)State.FinishingAwait;
                            _waitToReadAwaiter = waitToReadTask.ConfigureAwait(false).GetAwaiter();
                            AsyncEnumerable inst = this;
                            _builder.AwaitUnsafeOnCompleted(ref _waitToReadAwaiter, ref inst);
                            return;

                        case State.FinishingAwait:
                            // The await on WaitToReadAsync finished.  Get its result and process it.
                            waitToReadResult = _waitToReadAwaiter.GetResult();
                            _state = (int)State.HaveWaitToReadResult;
                            goto case State.HaveWaitToReadResult;

                        case State.HaveWaitToReadResult:
                            // We have a result from WaitToReadAsync.  If an item might be available,
                            // jump to try to read it.  If an item will never be available, complete.
                            if (waitToReadResult)
                            {
                                _state = (int)State.TryRead;
                                goto case State.TryRead;
                            }
                            else
                            {
                                _state = (int)State.Completing;
                                goto case State.Completing;
                            }

                        case State.TryRead:
                            // Do the read.  If we successfully get an item, mark that an item is
                            // available.  Then if we've already awaited as part of this MoveNextAsync,
                            // also complete the promise.
                            if (_reader.TryRead(out _current))
                            {
                                _itemAvailable = true;
                                if (_usePromiseForResult)
                                {
                                    _promise.SetResult(true);
                                }
                                return;
                            }
                            else
                            {
                                // No item was available.  Start over.
                                _state = (int)State.OuterLoop;
                                goto case State.OuterLoop;
                            }

                        case State.Completing:
                            // Cleanup.  And if there's an outstanding promise, complete it
                            // to indicate iteration is done.
                            _builder.Complete();
                            _state = (int)State.Done;
                            if (_usePromiseForResult)
                            {
                                _promise.SetResult(false);
                            }
                            return;
                    }
                }
                catch (Exception e)
                {
                    _state = (int)State.Done;
                    _builder.Complete();
                    _itemAvailable = false;
                    _usePromiseForResult = true;
                    _promise.SetException(e);
                }
            }

            public T Current => _current;

            public ValueTask DisposeAsync()
            {
                // Nothing to clean up.
                return default;
            }

            bool IValueTaskSource<bool>.GetResult(short token) => _promise.GetResult(token);
            ValueTaskSourceStatus IValueTaskSource<bool>.GetStatus(short token) => _promise.GetStatus(token);
            void IValueTaskSource<bool>.OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => _promise.OnCompleted(continuation, state, token, flags);

            void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine) { /* nop */ }
        }
    }
}
