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
        /// <returns></returns>
        public static IDisposable OnChange(Func<IChangeToken?> changeTokenProducer, Action changeTokenConsumer)
        {
            if (changeTokenProducer is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.changeTokenProducer);
            }
            if (changeTokenConsumer is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.changeTokenConsumer);
            }

            return new ChangeTokenRegistration<Action>(changeTokenProducer, callback => callback(), changeTokenConsumer);
        }

        /// <summary>
        /// Registers the <paramref name="changeTokenConsumer"/> action to be called whenever the token produced changes.
        /// </summary>
        /// <param name="changeTokenProducer">Produces the change token.</param>
        /// <param name="changeTokenConsumer">Action called when the token changes.</param>
        /// <param name="state">state for the consumer.</param>
        /// <returns></returns>
        public static IDisposable OnChange<TState>(Func<IChangeToken?> changeTokenProducer, Action<TState> changeTokenConsumer, TState state)
        {
            if (changeTokenProducer is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.changeTokenProducer);
            }
            if (changeTokenConsumer is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.changeTokenConsumer);
            }

            return new ChangeTokenRegistration<TState>(changeTokenProducer, changeTokenConsumer, state);
        }

        private sealed class ChangeTokenRegistration<TState> : IDisposable
        {
            private const int MaxConsecutiveSynchronousChanges = 4096;
            private const string ConsecutiveChangesErrorMessage = "The change token producer returned an already changed token too many consecutive times. Ensure the producer eventually returns a fresh, non-signaled token instance.";

            private readonly Func<IChangeToken?> _changeTokenProducer;
            private readonly Action<TState> _changeTokenConsumer;
            private readonly TState _state;
            private IDisposable? _disposable;
            private int _callbackProcessing;
            private int _callbackPending;

            private static readonly NoopDisposable _disposedSentinel = new NoopDisposable();

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
                Volatile.Write(ref _callbackPending, 1);

                ProcessCallbacks();
            }

            private void ProcessCallbacks()
            {
                while (Interlocked.CompareExchange(ref _callbackProcessing, 1, 0) is 0)
                {
                    int consecutiveSynchronousChanges = 0;

                    try
                    {
                        while (Interlocked.Exchange(ref _callbackPending, 0) is not 0)
                        {
                            // The order here is important. We need to take the token and then apply our changes BEFORE
                            // registering. This prevents us from possibly having two change updates to process concurrently.
                            //
                            // If the token changes after we take the token, then we'll process the update immediately upon
                            // registering the callback.
                            IChangeToken? token = _changeTokenProducer();

                            try
                            {
                                _changeTokenConsumer(_state);
                            }
                            finally
                            {
                                // We always want to ensure the callback is registered.
                                if (RegisterChangeTokenCallback(token))
                                {
                                    if (++consecutiveSynchronousChanges >= MaxConsecutiveSynchronousChanges)
                                    {
                                        throw new InvalidOperationException(ConsecutiveChangesErrorMessage);
                                    }

                                    Volatile.Write(ref _callbackPending, 1);
                                }
                                else
                                {
                                    consecutiveSynchronousChanges = 0;
                                }
                            }
                        }
                    }
                    finally
                    {
                        Volatile.Write(ref _callbackProcessing, 0);
                    }

                    if (Volatile.Read(ref _callbackPending) is 0)
                    {
                        return;
                    }
                }
            }

            private bool RegisterChangeTokenCallback(IChangeToken? token)
            {
                if (token is null)
                {
                    return false;
                }

                IDisposable registration = token.RegisterChangeCallback(s => ((ChangeTokenRegistration<TState>?)s)!.OnChangeTokenFired(), this);
                if (token.HasChanged && token.ActiveChangeCallbacks)
                {
                    registration.Dispose();
                    return true;
                }

                SetDisposable(registration);
                return false;
            }

            private void SetDisposable(IDisposable disposable)
            {
                while (true)
                {
                    // We don't want to transition from _disposedSentinel => anything since it's terminal,
                    // but we want to allow going from previously assigned disposable to another disposable.
                    IDisposable? current = Volatile.Read(ref _disposable);

                    // If Dispose was called, then immediately dispose the disposable.
                    if (current == _disposedSentinel)
                    {
                        disposable.Dispose();
                        return;
                    }

                    IDisposable? previous = Interlocked.CompareExchange(ref _disposable, disposable, current);
                    if (previous == current)
                    {
                        current?.Dispose();
                        return;
                    }

                    if (previous == _disposedSentinel)
                    {
                        disposable.Dispose();
                        return;
                    }
                }
            }

            public void Dispose()
            {
                // If the previous value is disposable then dispose it, otherwise,
                // now we've set the disposed sentinel
                Interlocked.Exchange(ref _disposable, _disposedSentinel)?.Dispose();
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
