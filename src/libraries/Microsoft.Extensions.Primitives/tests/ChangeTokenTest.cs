// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
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
            ChangeToken.OnChange(() => token = new TestChangeToken(), () =>
            {
                count++;
                throw new Exception();
            });
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
            ChangeToken.OnChange(() => token = new TestChangeToken(), s =>
            {
                callbackState = s;
                count++;
                throw new Exception();
            }, state);
            Assert.Throws<Exception>(() => token.Changed());
            Assert.Equal(1, count);
            Assert.NotNull(callbackState);
            Assert.Throws<Exception>(() => token.Changed());
            Assert.Equal(2, count);
            Assert.NotNull(callbackState);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60183")]
        public void ReusingOnceFiredChangeTokenShouldNotThrow()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationChangeToken = new CancellationChangeToken(cancellationTokenSource.Token);

            Func<IChangeToken> producer = () =>
            {
                return cancellationChangeToken;
            };

            var count = 0;
            Action consumer = () => ++ count;

            using (ChangeToken.OnChange(producer, consumer))
            {
                cancellationTokenSource.Cancel();
            }

            Assert.Equal(1, count);
        }

        [Fact]
        public void RenewingFiredChangeTokenShouldFireExpectedNumberOfTimes()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationChangeToken = new CancellationChangeToken(cancellationTokenSource.Token);

            Func<IChangeToken> producer = () =>
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource = new CancellationTokenSource();
                    cancellationChangeToken = new CancellationChangeToken(cancellationTokenSource.Token);
                }

                return cancellationChangeToken;
            };

            var count = 0;
            Action consumer = () => ++ count;

            using (ChangeToken.OnChange(producer, consumer))
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Cancel();
            }

            Assert.Equal(3, count);
        }

        [Fact]
        public void OnChangeProperlyHandlesInitiallyFiredTokensAndMultiplePostFires()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationChangeToken = new CancellationChangeToken(cancellationTokenSource.Token);

            // Set the initial state as already changed.
            cancellationTokenSource.Cancel();

            var x = 0;

            Func<IChangeToken> producer = () =>
            {
                ++ x;
                if (x == 1)
                {
                    return cancellationChangeToken;
                }

                cancellationTokenSource = new CancellationTokenSource();
                cancellationChangeToken = new CancellationChangeToken(cancellationTokenSource.Token);

                return cancellationChangeToken;
            };

            var fired = 0;
            Action consumer = () => ++ fired;

            using (ChangeToken.OnChange(producer, consumer))
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Cancel();
            }

            Assert.Equal(3, fired);
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
            Assert.Equal(2, provider.RegistrationCalls);
            Assert.Equal(2, provider.DisposeCalls);
        }

        [Fact]
        public void NullTokenDisposeShouldNotThrow()
        {
            ChangeToken.OnChange(() => null, () => Assert.True(false)).Dispose();
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
