// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Primitives
{
    public class ChangeTokenTests
    {
        public class TestChangeToken : IChangeToken
        {
            private Action _callback;

            public bool ActiveChangeCallbacks { get; set; }
            public bool HasChanged { get; set; }

            public IDisposable RegisterChangeCallback(Action<object> callback, object state)
            {
                _callback = () => callback(state);
                return null;
            }

            public void Changed()
            {
                HasChanged = true;
                _callback();
            }
        }

        // A change token whose registration succeeds but whose HasChanged throws.
        private sealed class ThrowOnHasChangedChangeToken : IChangeToken
        {
            public int RegisterCalls { get; private set; }

            public bool ActiveChangeCallbacks => true;

            public bool HasChanged => throw new InvalidTimeZoneException();

            public IDisposable RegisterChangeCallback(Action<object> callback, object state)
            {
                RegisterCalls++;
                return null;
            }
        }

        [Fact]
        public void HasChangeFiresChange()
        {
            var token = new TestChangeToken();
            bool fired = false;
            ChangeToken.OnChange(() => token, () => fired = true);
            Assert.False(fired);
            token.Changed();
            Assert.True(fired);
        }

        [Fact]
        public void ChangesFireAfterExceptions()
        {
            TestChangeToken token = null;
            var count = 0;
            // The lambda is explicitly typed as Action so it binds to the synchronous overload rather than the Func<Task> one.
            ChangeToken.OnChange(() => token = new TestChangeToken(), (Action)(() =>
            {
                count++;
                throw new Exception();
            }));
            Assert.Throws<Exception>(() => token.Changed());
            Assert.Equal(1, count);
            Assert.Throws<Exception>(() => token.Changed());
            Assert.Equal(2, count);
        }

        [Fact]
        public void HasChangeFiresChangeWithState()
        {
            var token = new TestChangeToken();
            object state = new object();
            object callbackState = null;
            ChangeToken.OnChange(() => token, s => callbackState = s, state);
            Assert.Null(callbackState);
            token.Changed();
            Assert.Equal(state, callbackState);
        }

        [Fact]
        public void ChangesFireAfterExceptionsWithState()
        {
            TestChangeToken token = null;
            var count = 0;
            object state = new object();
            object callbackState = null;
            // The lambda is explicitly typed as Action<object> so it binds to the synchronous overload rather than the Func<TState, Task> one.
            ChangeToken.OnChange(() => token = new TestChangeToken(), (Action<object>)(s =>
            {
                callbackState = s;
                count++;
                throw new Exception();
            }), state);
            Assert.Throws<Exception>(() => token.Changed());
            Assert.Equal(1, count);
            Assert.NotNull(callbackState);
            Assert.Throws<Exception>(() => token.Changed());
            Assert.Equal(2, count);
            Assert.NotNull(callbackState);
        }

        [Fact]
        public void AsyncLocalsNotCapturedAndRestored()
        {
            // Capture clean context
            var executionContext = ExecutionContext.Capture();

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var cancellationChangeToken = new CancellationChangeToken(cancellationToken);
            var executed = false;

            // Set AsyncLocal
            var asyncLocal = new AsyncLocal<int>();
            asyncLocal.Value = 1;

            // Register Callback
            cancellationChangeToken.RegisterChangeCallback(al =>
            {
                // AsyncLocal not set, when run on clean context
                // A suppressed flow runs in current context, rather than restoring the captured context
                Assert.Equal(0, ((AsyncLocal<int>)al).Value);
                executed = true;
            }, asyncLocal);

            // AsyncLocal should still be set
            Assert.Equal(1, asyncLocal.Value);

            // Check AsyncLocal is not restored by running on clean context
            ExecutionContext.Run(executionContext, cts => ((CancellationTokenSource)cts).Cancel(), cancellationTokenSource);

            // AsyncLocal should still be set
            Assert.Equal(1, asyncLocal.Value);
            Assert.True(executed);
        }

        [Fact]
        public void DisposingChangeTokenRegistrationDoesNotRaiseConsumerCallback()
        {
            var provider = new ResettableChangeTokenProvider();
            var count = 0;
            var reg = ChangeToken.OnChange(provider.GetChangeToken, () =>
            {
                count++;
            });

            for (int i = 0; i < 5; i++)
            {
                provider.Changed();
            }

            Assert.Equal(5, count);

            reg.Dispose();

            for (int i = 0; i < 5; i++)
            {
                provider.Changed();
            }

            Assert.Equal(5, count);
        }

        [Fact]
        public void DisposingChangeTokenRegistrationDoesNotRaiseConsumerCallbackStateOverload()
        {
            var provider = new ResettableChangeTokenProvider();
            var count = 0;
            var reg = ChangeToken.OnChange<object>(provider.GetChangeToken, state =>
            {
                count++;
            },
            null);

            for (int i = 0; i < 5; i++)
            {
                provider.Changed();
            }

            Assert.Equal(5, count);

            reg.Dispose();

            for (int i = 0; i < 5; i++)
            {
                provider.Changed();
            }

            Assert.Equal(5, count);
        }

        [Fact]
        public void DisposingChangeTokenRegistrationDoesNotRaiseConsumerIfTokenProviderReturnsCancelledToken()
        {
            var provider = new ResettableChangeTokenProvider();
            Func<Func<IChangeToken>> changeTokenProviderFactory = () =>
            {
                int n = 0;
                return () =>
                {
                    var token = provider.GetChangeToken();
                    if (n++ is 0) provider.Changed();
                    return token;
                };
            };
            int count = 0;
            var reg = ChangeToken.OnChange(changeTokenProviderFactory(), () => count++);
            reg.Dispose();
            provider.Changed();

            Assert.Equal(1, count);
        }

        [Fact]
        public void DisposingChangeTokenRegistrationDuringCallbackWorks()
        {
            var provider = new ResettableChangeTokenProvider();
            var count = 0;

            IDisposable reg = null;

            reg = ChangeToken.OnChange<object>(provider.GetChangeToken, state =>
            {
                count++;
                reg.Dispose();
            },
            null);

            provider.Changed();

            Assert.Equal(1, count);

            provider.Changed();

            Assert.Equal(1, count);
        }

        [Fact]
        public void DoubleDisposeDisposesOnce()
        {
            var provider = new TrackableChangeTokenProvider();
            var count = 0;

            IDisposable reg = null;

            reg = ChangeToken.OnChange<object>(provider.GetChangeToken, state =>
            {
                count++;
                reg.Dispose();
            },
            null);

            provider.Changed();

            Assert.Equal(1, count);
            Assert.Equal(1, provider.RegistrationCalls);
            Assert.Equal(1, provider.DisposeCalls);

            reg.Dispose();

            provider.Changed();

            Assert.Equal(1, count);
            Assert.Equal(1, provider.RegistrationCalls);
            Assert.Equal(1, provider.DisposeCalls);
        }

        [Fact]
        public void NullTokenDisposeShouldNotThrow()
        {
            ChangeToken.OnChange(() => null, () => Assert.Fail()).Dispose();
        }

        [Fact]
        public void AsyncOnChangeThrowsForNullArguments()
        {
            Assert.Throws<ArgumentNullException>(() => ChangeToken.OnChange(null, () => Task.CompletedTask));
            Assert.Throws<ArgumentNullException>(() => ChangeToken.OnChange(() => new TestChangeToken(), (Func<Task>)null));
            Assert.Throws<ArgumentNullException>(() => ChangeToken.OnChange<object>(null, _ => Task.CompletedTask, null));
            Assert.Throws<ArgumentNullException>(() => ChangeToken.OnChange<object>(() => new TestChangeToken(), (Func<object, Task>)null, null));
        }

        [Fact]
        public void AsyncHasChangeFiresChange()
        {
            var provider = new ResettableChangeTokenProvider();
            int count = 0;
            ChangeToken.OnChange(provider.GetChangeToken, () =>
            {
                count++;
                return Task.CompletedTask;
            });

            Assert.Equal(0, count);
            provider.Changed();
            Assert.Equal(1, count);
            provider.Changed();
            Assert.Equal(2, count);
        }

        [Fact]
        public void AsyncHasChangeFiresChangeWithState()
        {
            var provider = new ResettableChangeTokenProvider();
            object state = new object();
            object callbackState = null;
            ChangeToken.OnChange(provider.GetChangeToken, s =>
            {
                callbackState = s;
                return Task.CompletedTask;
            }, state);

            Assert.Null(callbackState);
            provider.Changed();
            Assert.Same(state, callbackState);
        }

        [Fact]
        public void AsyncDisposingChangeTokenRegistrationDoesNotRaiseConsumerCallback()
        {
            var provider = new ResettableChangeTokenProvider();
            int count = 0;
            IDisposable reg = ChangeToken.OnChange(provider.GetChangeToken, () =>
            {
                count++;
                return Task.CompletedTask;
            });

            for (int i = 0; i < 5; i++)
            {
                provider.Changed();
            }

            Assert.Equal(5, count);

            reg.Dispose();

            for (int i = 0; i < 5; i++)
            {
                provider.Changed();
            }

            Assert.Equal(5, count);
        }

        [Fact]
        public async Task AsyncDoesNotReregisterUntilConsumerTaskCompletes()
        {
            var provider = new ResettableChangeTokenProvider();
            int invocations = 0;
            TaskCompletionSource<bool> started = NewTcs();
            TaskCompletionSource<bool> gate = NewTcs();

            // Plain Task-returning consumer (no async lambda) so no continuation is captured on
            // xunit's SynchronizationContext, keeping the test isolated from other tests.
            IDisposable reg = ChangeToken.OnChange(provider.GetChangeToken, () =>
            {
                Interlocked.Increment(ref invocations);
                Volatile.Read(ref started).SetResult(true);
                return Volatile.Read(ref gate).Task;
            });

            // First change starts the consumer synchronously; it then blocks awaiting its gate.
            TaskCompletionSource<bool> firstGate = gate;
            provider.Changed();
            await started.Task.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(1, invocations);

            // Arm gates for the next invocation before triggering another change. The consumer re-runs on a
            // different thread (the awaited gate's continuation), so publish these with Volatile.Write.
            Volatile.Write(ref started, NewTcs());
            TaskCompletionSource<bool> secondGate = NewTcs();
            Volatile.Write(ref gate, secondGate);

            // A change while the consumer is running must not start another invocation, because the
            // token is only re-registered once the consumer's task completes.
            provider.Changed();
            Assert.Equal(1, invocations);

            // Completing the first consumer re-registers and coalesces the pending change into a single invocation.
            firstGate.SetResult(true);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(2, invocations);

            // Drain the second invocation and unsubscribe.
            secondGate.SetResult(true);
            reg.Dispose();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AsyncDisposeDuringConsumerSuppressesSynchronousReinvocationWhenTokenAlreadyChanged(bool useStateOverload)
        {
            var provider = new ResettableChangeTokenProvider();
            int invocations = 0;

            // Plain (synchronous-continuation) TCS: completing it runs the deferred re-registration inline on the
            // completing thread. The gate is completed from a thread-pool thread (below) so that, with the product
            // code's ConfigureAwait(false), that continuation runs synchronously there rather than being posted to
            // xunit's SynchronizationContext, keeping the test deterministic without arbitrary delays.
            var gate = new TaskCompletionSource<bool>();

            IDisposable reg;
            if (useStateOverload)
            {
                reg = ChangeToken.OnChange(provider.GetChangeToken, _ =>
                {
                    Interlocked.Increment(ref invocations);
                    return gate.Task;
                }, new object());
            }
            else
            {
                reg = ChangeToken.OnChange(provider.GetChangeToken, () =>
                {
                    Interlocked.Increment(ref invocations);
                    return gate.Task;
                });
            }

            // The first change starts the consumer; it stays in flight awaiting the gate, so the change token
            // captured for re-registration is held until the consumer completes.
            provider.Changed();
            Assert.Equal(1, invocations);

            // Dispose while the consumer is still in flight.
            reg.Dispose();

            await Task.Run(() =>
            {
                // A change while disposed cancels the captured token, so it is now already changed. Registering a
                // callback on an already-changed CancellationChangeToken would invoke the callback synchronously.
                provider.Changed();

                // Completing the consumer drives the deferred re-registration inline on this thread. It must observe
                // the disposal and skip registering; otherwise registering on the already-changed token invokes the
                // callback synchronously and re-runs the consumer after Dispose. Completing from a thread-pool thread
                // (rather than directly here) is required for determinism: with the product code's
                // ConfigureAwait(false), the continuation runs synchronously on this thread instead of being posted to
                // xunit's SynchronizationContext, so the (buggy) re-invocation would be observed before the assert.
                gate.SetResult(true);
            });

            Assert.Equal(1, invocations);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SyncDisposeAndChangeDuringConsumerSuppressesSynchronousReinvocation(bool useStateOverload)
        {
            var provider = new ResettableChangeTokenProvider();
            int invocations = 0;

            IDisposable reg = null;
            void Consume()
            {
                if (Interlocked.Increment(ref invocations) == 1)
                {
                    // Dispose, then change the captured token, so the synchronous re-registration in the consumer's
                    // finally block would target an already-changed token (which invokes the callback synchronously).
                    reg.Dispose();
                    provider.Changed();
                }
            }

            reg = useStateOverload
                ? ChangeToken.OnChange(provider.GetChangeToken, _ => Consume(), new object())
                : ChangeToken.OnChange(provider.GetChangeToken, Consume);

            provider.Changed();

            // The registration was disposed during the consumer, so the synchronous re-registration must be
            // suppressed; the consumer must not be invoked again even though the token it would re-register on
            // has already changed.
            Assert.Equal(1, invocations);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AsyncReRegistrationFailureIsNotRetriedAndPropagates(bool useStateOverload)
        {
            var firstToken = new TestChangeToken();
            var throwingToken = new ThrowOnHasChangedChangeToken();
            int produced = 0;
            Func<IChangeToken> producer = () => produced++ == 0 ? firstToken : throwingToken;

            if (useStateOverload)
            {
                ChangeToken.OnChange(producer, _ => Task.CompletedTask, new object());
            }
            else
            {
                ChangeToken.OnChange(producer, () => Task.CompletedTask);
            }

            // When the token fires, the synchronously-completed consumer triggers re-registration on the next token,
            // whose HasChanged throws during registration. That exception must propagate to the code that triggers
            // the change, and re-registration must be attempted exactly once.
            Assert.Throws<InvalidTimeZoneException>(firstToken.Changed);
            Assert.Equal(1, throwingToken.RegisterCalls);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DisposingDuringConsumerSuppressesReRegistration(bool useAsyncConsumer)
        {
            var provider = new ResettableChangeTokenProvider();
            int invocations = 0;

            // Plain (synchronous-continuation) TCS so completing it from a thread-pool thread (below) drives the
            // deferred re-registration inline there rather than posting to xunit's SynchronizationContext.
            var gate = new TaskCompletionSource<bool>();

            // The consumer disposes its own registration mid-invocation. The re-registration that follows the
            // consumer (synchronously for the sync overload, after the returned task completes for the async one)
            // must observe the disposal and not arm a live callback. Disposal happens on the thread running the
            // consumer, so it doesn't block on the in-progress change callback.
            IDisposable reg = null;
            if (useAsyncConsumer)
            {
                reg = ChangeToken.OnChange(provider.GetChangeToken, () =>
                {
                    Interlocked.Increment(ref invocations);
                    reg.Dispose();

                    // Stay in flight by returning an incomplete task; re-registration is deferred until it completes.
                    return gate.Task;
                });
            }
            else
            {
                reg = ChangeToken.OnChange(provider.GetChangeToken, () =>
                {
                    Interlocked.Increment(ref invocations);
                    reg.Dispose();
                });
            }

            // Fire the change. The consumer runs synchronously and disposes itself.
            provider.Changed();

            if (useAsyncConsumer)
            {
                // Complete the in-flight consumer from a thread-pool thread so the deferred re-registration runs
                // synchronously and completes before the subsequent changes below (the product code awaits with
                // ConfigureAwait(false)), keeping the test deterministic without arbitrary delays.
                await Task.Run(() => gate.SetResult(true));
            }

            Assert.Equal(1, invocations);

            // Subsequent changes must not invoke the consumer again, because disposal suppressed re-registration.
            for (int i = 0; i < 5; i++)
            {
                provider.Changed();
            }

            Assert.Equal(1, invocations);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DisposingFromAnotherThreadWhileConsumerExecutingSuppressesReRegistration(bool useAsyncConsumer)
        {
            var provider = new ResettableChangeTokenProvider();
            int invocations = 0;
            TaskCompletionSource<bool> started = NewTcs();

            // Plain (synchronous-continuation) TCS so completing it from a thread-pool thread drives the deferred
            // re-registration inline there (the product code awaits with ConfigureAwait(false)).
            var gate = new TaskCompletionSource<bool>();

            IDisposable reg;
            if (useAsyncConsumer)
            {
                reg = ChangeToken.OnChange(provider.GetChangeToken, () =>
                {
                    Interlocked.Increment(ref invocations);
                    started.SetResult(true);

                    // Stay in flight by returning an incomplete task; re-registration is deferred until it completes.
                    return gate.Task;
                });
            }
            else
            {
                reg = ChangeToken.OnChange(provider.GetChangeToken, () =>
                {
                    Interlocked.Increment(ref invocations);
                    started.SetResult(true);

                    // Stay in flight by blocking the triggering thread until the gate is released.
                    gate.Task.GetAwaiter().GetResult();
                });
            }

            // A synchronous consumer blocks the triggering thread, so fire the change on a background thread.
            Task trigger = Task.Run(provider.Changed);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(1, invocations);

            if (useAsyncConsumer)
            {
                // The change callback already returned (the consumer is in flight via its task), so Dispose does
                // not block. Dispose from another thread, then complete the consumer from a thread-pool thread so
                // the deferred re-registration runs synchronously there and must observe the disposal.
                await Task.Run(reg.Dispose).WaitAsync(TimeSpan.FromSeconds(30));
                await Task.Run(() => gate.SetResult(true));
                await trigger.WaitAsync(TimeSpan.FromSeconds(30));
            }
            else
            {
                // The consumer is running on the triggering thread, so Dispose blocks until it returns
                // (CancellationTokenRegistration.Dispose waits for the in-progress callback). Run it on a
                // separate thread, then release the consumer so both can complete.
                Task dispose = Task.Run(reg.Dispose);
                gate.SetResult(true);
                await dispose.WaitAsync(TimeSpan.FromSeconds(30));
                await trigger.WaitAsync(TimeSpan.FromSeconds(30));
            }

            Assert.Equal(1, invocations);

            // Subsequent changes must not invoke the consumer again, because disposal suppressed re-registration.
            for (int i = 0; i < 5; i++)
            {
                provider.Changed();
            }

            Assert.Equal(1, invocations);
        }

        private static TaskCompletionSource<bool> NewTcs() =>
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AsyncConsumerSynchronousExceptionPropagatesToProducerAndSubscriptionSurvives(bool useStateOverload)
        {
            var provider = new ResettableChangeTokenProvider();
            int count = 0;
            object expectedState = new object();
            object observedState = null;

            Func<Task> faulting = () =>
            {
                count++;
                throw new InvalidTimeZoneException();
            };

            if (useStateOverload)
            {
                ChangeToken.OnChange(provider.GetChangeToken, s =>
                {
                    observedState = s;
                    return faulting();
                }, expectedState);
            }
            else
            {
                ChangeToken.OnChange(provider.GetChangeToken, faulting);
            }

            // A synchronous exception from the consumer is propagated to the code that triggers the change token,
            // just like the synchronous overload. CancellationTokenSource.Cancel wraps it in an AggregateException.
            AggregateException ex = Assert.Throws<AggregateException>(provider.Changed);
            Assert.IsType<InvalidTimeZoneException>(ex.InnerException);
            Assert.Equal(1, count);

            // The subscription survives a throwing consumer, so a later change invokes (and throws from) it again.
            Assert.Throws<AggregateException>(provider.Changed);
            Assert.Equal(2, count);

            if (useStateOverload)
            {
                Assert.Same(expectedState, observedState);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AsyncConsumerAsynchronousFaultIsNotPropagatedToProducerAndSubscriptionSurvives(bool useStateOverload)
        {
            var provider = new ResettableChangeTokenProvider();
            int count = 0;
            object expectedState = new object();
            object observedState = null;

            Func<Task> faulting = () =>
            {
                count++;
                return Task.FromException(new InvalidTimeZoneException());
            };

            if (useStateOverload)
            {
                ChangeToken.OnChange(provider.GetChangeToken, s =>
                {
                    observedState = s;
                    return faulting();
                }, expectedState);
            }
            else
            {
                ChangeToken.OnChange(provider.GetChangeToken, faulting);
            }

            // An asynchronous fault from the consumer's task cannot be propagated without blocking, so it is left
            // unobserved (observable only through TaskScheduler.UnobservedTaskException) and is not surfaced to the
            // code that triggers the change token.
            provider.Changed();
            Assert.Equal(1, count);

            // The subscription survives a faulted consumer, so later changes still invoke it.
            provider.Changed();
            Assert.Equal(2, count);

            if (useStateOverload)
            {
                Assert.Same(expectedState, observedState);
            }
        }

        // Subscribes a no-op consumer using each of the four OnChange overloads, selected by index.
        private static void Subscribe(int overload, Func<IChangeToken> producer)
        {
            switch (overload)
            {
                case 0:
                    ChangeToken.OnChange(producer, () => { });
                    break;
                case 1:
                    ChangeToken.OnChange(producer, _ => { }, new object());
                    break;
                case 2:
                    ChangeToken.OnChange(producer, () => Task.CompletedTask);
                    break;
                case 3:
                    ChangeToken.OnChange(producer, _ => Task.CompletedTask, new object());
                    break;
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ProducerExceptionDuringInitialRegistrationPropagatesToCaller(int overload)
        {
            Func<IChangeToken> producer = () => throw new InvalidTimeZoneException();

            // The producer is invoked while registering, so its exception is propagated to the caller of OnChange.
            Assert.Throws<InvalidTimeZoneException>(() => Subscribe(overload, producer));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ProducerExceptionWhenTokenFiresPropagatesToTrigger(int overload)
        {
            TestChangeToken token = null;
            int calls = 0;
            Func<IChangeToken> producer = () =>
            {
                if (calls++ == 0)
                {
                    return token = new TestChangeToken();
                }
                throw new InvalidTimeZoneException();
            };

            Subscribe(overload, producer);

            // When the token fires, the producer is invoked again to obtain the next token; its exception is
            // propagated to the code that triggers the change token.
            Assert.Throws<InvalidTimeZoneException>(() => token.Changed());
        }

        public class TrackableChangeTokenProvider
        {
            private TrackableChangeToken _cts = new TrackableChangeToken();

            public int RegistrationCalls { get; set; }
            public int DisposeCalls { get; set; }

            public IChangeToken GetChangeToken() => _cts;

            public void Changed()
            {
                var previous = _cts;
                _cts = new TrackableChangeToken();
                previous.Execute();

                RegistrationCalls += previous.RegistrationCalls;
                DisposeCalls += previous.DisposeCalls;
            }
        }

        public class TrackableChangeToken : IChangeToken
        {
            private CancellationTokenSource _cts = new CancellationTokenSource();

            public int RegistrationCalls { get; set; }
            public int DisposeCalls { get; set; }

            public bool HasChanged => _cts.IsCancellationRequested;

            public bool ActiveChangeCallbacks => true;

            public void Execute()
            {
                _cts.Cancel();
            }

            public IDisposable RegisterChangeCallback(Action<object> callback, object state)
            {
                var registration = _cts.Token.Register(callback, state);
                RegistrationCalls++;

                return new DisposableAction(() =>
                {
                    DisposeCalls++;
                    registration.Dispose();
                });
            }

            private class DisposableAction : IDisposable
            {
                private Action _action;

                public DisposableAction(Action action)
                {
                    _action = action;
                }

                public void Dispose()
                {
                    _action?.Invoke();
                }
            }
        }

        public class ResettableChangeTokenProvider
        {
            private CancellationTokenSource _cts = new CancellationTokenSource();

            public IChangeToken GetChangeToken() => new CancellationChangeToken(_cts.Token);

            public void Changed()
            {
                var previous = _cts;
                _cts = new CancellationTokenSource();
                previous.Cancel();
            }
        }
    }
}
