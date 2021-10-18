// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.Extensions.Primitives
{
    /// <summary>
    /// Propagates notifications that a change has occurred.
    /// </summary>
    public static class ChangeToken
    {
        /// <summary>
        /// Registers the <paramref name="changeTokenConsumer"/> action to be called whenever the token produced changes.
        /// </summary>
        /// <param name="changeTokenProducer">Produces the change token.</param>
        /// <param name="changeTokenConsumer">Action called when the token changes.</param>
        /// <returns>An <see cref="IDisposable"/> instance that represents the subscription. To unsubscribe, call <see cref="IDisposable.Dispose"/>.</returns>
        public static IDisposable OnChange(Func<IChangeToken?> changeTokenProducer, Action changeTokenConsumer)
        {
            if (changeTokenProducer == null)
            {
                throw new ArgumentNullException(nameof(changeTokenProducer));
            }
            if (changeTokenConsumer == null)
            {
                throw new ArgumentNullException(nameof(changeTokenConsumer));
            }

            return new ChangeTokenRegistration<Action>(changeTokenProducer, callback => callback(), changeTokenConsumer);
        }

        /// <summary>
        /// Registers the <paramref name="changeTokenConsumer"/> action to be called whenever the token produced changes.
        /// </summary>
        /// <param name="changeTokenProducer">Produces the change token.</param>
        /// <param name="changeTokenConsumer">Action called when the token changes.</param>
        /// <param name="state">state for the consumer.</param>
        /// <returns>An <see cref="IDisposable"/> instance that represents the subscription. To unsubscribe, call <see cref="IDisposable.Dispose"/>.</returns>
        public static IDisposable OnChange<TState>(Func<IChangeToken?> changeTokenProducer, Action<TState> changeTokenConsumer, TState state)
        {
            if (changeTokenProducer == null)
            {
                throw new ArgumentNullException(nameof(changeTokenProducer));
            }
            if (changeTokenConsumer == null)
            {
                throw new ArgumentNullException(nameof(changeTokenConsumer));
            }

            return new ChangeTokenRegistration<TState>(changeTokenProducer, changeTokenConsumer, state);
        }

        private sealed class ChangeTokenRegistration<TState> : IDisposable
        {
            private readonly Func<IChangeToken?> _changeTokenProducer;
            private readonly Action<TState> _changeTokenConsumer;
            private readonly TState _state;

            private IChangeToken? _oldToken;
            private IDisposable? _disposable;

            private static readonly NoopDisposable s_disposedSentinel = new();

            public ChangeTokenRegistration(Func<IChangeToken?> changeTokenProducer, Action<TState> changeTokenConsumer, TState state)
            {
                _changeTokenProducer = changeTokenProducer;
                _changeTokenConsumer = changeTokenConsumer;
                _state = state;

                IChangeToken? token = changeTokenProducer();
                RegisterChangeTokenCallback(token);
            }

            private void OnChangeTokenFired()
            {
                while (true)
                {
                    // The order here is important. We need to take the token and then apply our changes BEFORE
                    // registering. This prevents us from possibly having two change updates to process concurrently.
                    //
                    // If the token changes after we take the token, then we'll process the update immediately upon
                    // registering the callback.
                    IChangeToken? newToken = _changeTokenProducer();

                    try
                    {
                        _changeTokenConsumer(_state);
                    }
                    finally
                    {
                        if (Equals(_oldToken, newToken))
                        {
                            throw new NotSupportedException("The change token provider should return a new token.");
                        }

                        if (newToken is { HasChanged: true })
                        {
                            // The new token has already fired, so we can jump to the top of the loop,
                            // get a new token and fire the consumer.
                        }
                        else
                        {
                            RegisterChangeTokenCallback(newToken);
                            _oldToken = newToken;
                        }
                    }

                    break;
                }
            }

            private void RegisterChangeTokenCallback(IChangeToken? token)
            {
                if (token is null)
                {
                    return;
                }

                IDisposable registraton = token.RegisterChangeCallback(s => ((ChangeTokenRegistration<TState>?)s)!.OnChangeTokenFired(), this);

                SetDisposable(registraton);
            }

            private void SetDisposable(IDisposable disposable)
            {
                // We don't want to transition from _disposedSentinel => anything since it's terminal
                // but we want to allow going from previously assigned disposable, to another
                // disposable.
                IDisposable? current = Volatile.Read(ref _disposable);

                // If Dispose was called, then immediately dispose the disposable
                if (current == s_disposedSentinel)
                {
                    disposable.Dispose();
                    return;
                }

                // Otherwise, try to update the disposable
                IDisposable? previous = Interlocked.CompareExchange(ref _disposable, disposable, current);

                if (previous == s_disposedSentinel)
                {
                    // The subscription was disposed so we dispose immediately and return
                    disposable.Dispose();
                }
                else if (previous == current)
                {
                    // We successfully assigned the _disposable field to disposable
                }
                else
                {
                    // Sets can never overlap with other SetDisposable calls so we should never get into this situation
                    throw new InvalidOperationException("Somebody else set the _disposable field");
                }
            }

            public void Dispose()
            {
                // If the previous value is disposable then dispose it, otherwise,
                // now we've set the disposed sentinel
                Interlocked.Exchange(ref _disposable, s_disposedSentinel)?.Dispose();
            }

            private sealed class NoopDisposable : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
    }
}
