// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

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
        /// <param name="changeTokenConsumer">Action called when the token changes. The token is re-registered once the action returns.</param>
        /// <returns>An <see cref="IDisposable"/> that, when disposed, unregisters the consumer.</returns>
        /// <remarks>
        /// Exceptions from <paramref name="changeTokenProducer"/> are propagated to the caller of this method or to the code that triggers the change token.
        /// Exceptions from <paramref name="changeTokenConsumer"/> are propagated to the code that triggers the change token.
        /// </remarks>
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

            return new SyncChangeTokenRegistration<Action>(changeTokenProducer, static callback => callback(), changeTokenConsumer);
        }

        /// <summary>
        /// Registers the <paramref name="changeTokenConsumer"/> action to be called whenever the token produced changes.
        /// </summary>
        /// <param name="changeTokenProducer">Produces the change token.</param>
        /// <param name="changeTokenConsumer">Action called when the token changes. The token is re-registered once the action returns.</param>
        /// <param name="state">state for the consumer.</param>
        /// <returns>An <see cref="IDisposable"/> that, when disposed, unregisters the consumer.</returns>
        /// <remarks>
        /// Exceptions from <paramref name="changeTokenProducer"/> are propagated to the caller of this method or to the code that triggers the change token.
        /// Exceptions from <paramref name="changeTokenConsumer"/> are propagated to the code that triggers the change token.
        /// </remarks>
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

            return new SyncChangeTokenRegistration<TState>(changeTokenProducer, changeTokenConsumer, state);
        }

        /// <summary>
        /// Registers the <paramref name="changeTokenConsumer"/> function to be called whenever the token produced changes.
        /// </summary>
        /// <param name="changeTokenProducer">Produces the change token.</param>
        /// <param name="changeTokenConsumer">Function called when the token changes. The token is only re-registered once the returned <see cref="Task"/> completes.</param>
        /// <returns>An <see cref="IDisposable"/> that, when disposed, unregisters the consumer.</returns>
        /// <remarks>
        /// Exceptions from <paramref name="changeTokenProducer"/> are propagated to the caller of this method or to the code that triggers the change token.
        /// Synchronous exceptions from <paramref name="changeTokenConsumer"/> are propagated to the code that triggers the change token.
        /// Asynchronous exceptions from <paramref name="changeTokenConsumer"/> are left unobserved.
        /// </remarks>
        public static IDisposable OnChange(Func<IChangeToken?> changeTokenProducer, Func<Task> changeTokenConsumer)
        {
            if (changeTokenProducer is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.changeTokenProducer);
            }
            if (changeTokenConsumer is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.changeTokenConsumer);
            }

            return new AsyncChangeTokenRegistration<Func<Task>>(changeTokenProducer, static callback => callback(), changeTokenConsumer);
        }

        /// <summary>
        /// Registers the <paramref name="changeTokenConsumer"/> function to be called whenever the token produced changes.
        /// </summary>
        /// <param name="changeTokenProducer">Produces the change token.</param>
        /// <param name="changeTokenConsumer">Function called when the token changes. The token is only re-registered once the returned <see cref="Task"/> completes.</param>
        /// <param name="state">state for the consumer.</param>
        /// <returns>An <see cref="IDisposable"/> that, when disposed, unregisters the consumer.</returns>
        /// <remarks>
        /// Exceptions from <paramref name="changeTokenProducer"/> are propagated to the caller of this method or to the code that triggers the change token.
        /// Synchronous exceptions from <paramref name="changeTokenConsumer"/> are propagated to the code that triggers the change token.
        /// Asynchronous exceptions from <paramref name="changeTokenConsumer"/> are left unobserved.
        /// </remarks>
        public static IDisposable OnChange<TState>(Func<IChangeToken?> changeTokenProducer, Func<TState, Task> changeTokenConsumer, TState state)
        {
            if (changeTokenProducer is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.changeTokenProducer);
            }
            if (changeTokenConsumer is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.changeTokenConsumer);
            }

            return new AsyncChangeTokenRegistration<TState>(changeTokenProducer, changeTokenConsumer, state);
        }

        private abstract class ChangeTokenRegistration<TState>(Func<IChangeToken?> changeTokenProducer, TState state) : IDisposable
        {
            private IDisposable? _disposable;

            private static readonly NoopDisposable _disposedSentinel = new NoopDisposable();

            protected TState State { get; } = state;

            protected Func<IChangeToken?> ChangeTokenProducer { get; } = changeTokenProducer;

            protected abstract void OnChangeTokenFired();

            protected void RegisterChangeTokenCallback(IChangeToken? token)
            {
                if (token is null)
                {
                    return;
                }

                // If the registration has already been disposed, don't register again. This guards re-registration
                // after disposal: registering on a token that has already changed invokes the callback synchronously, which
                // would re-run the consumer after disposal.
                if (Volatile.Read(ref _disposable) == _disposedSentinel)
                {
                    return;
                }

                IDisposable? registration = token.RegisterChangeCallback(static s => ((ChangeTokenRegistration<TState>?)s)!.OnChangeTokenFired(), this);
                if (token.HasChanged && token.ActiveChangeCallbacks)
                {
                    registration?.Dispose();
                    return;
                }
                SetDisposable(registration);
            }

            private void SetDisposable(IDisposable? disposable)
            {
                // We don't want to transition from _disposedSentinel => anything since it's terminal
                // but we want to allow going from previously assigned disposable, to another
                // disposable.
                IDisposable? current = Volatile.Read(ref _disposable);

                // If Dispose was called, then immediately dispose the disposable
                if (current == _disposedSentinel)
                {
                    disposable?.Dispose();
                    return;
                }

                // Otherwise, try to update the disposable
                IDisposable? previous = Interlocked.CompareExchange(ref _disposable, disposable, current);

                if (previous == _disposedSentinel)
                {
                    // The subscription was disposed so we dispose immediately and return
                    disposable?.Dispose();
                }
                else if (previous == current)
                {
                    // We successfully assigned the _disposable field to disposable
                }
                else
                {
                    // Sets can never overlap with other SetDisposable calls so we should never get into this situation
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_ConcurrentDisposableSet);
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

        private sealed class SyncChangeTokenRegistration<TState> : ChangeTokenRegistration<TState>
        {
            private readonly Action<TState> _changeTokenConsumer;

            public SyncChangeTokenRegistration(Func<IChangeToken?> changeTokenProducer, Action<TState> changeTokenConsumer, TState state)
                : base(changeTokenProducer, state)
            {
                _changeTokenConsumer = changeTokenConsumer;

                RegisterChangeTokenCallback(changeTokenProducer());
            }

            protected override void OnChangeTokenFired()
            {
                // The order here is important. We need to take the token and then apply our changes BEFORE
                // registering. This prevents us from possible having two change updates to process concurrently.
                //
                // If the token changes after we take the token, then we'll process the update immediately upon
                // registering the callback.
                IChangeToken? token = ChangeTokenProducer();

                try
                {
                    _changeTokenConsumer(State);
                }
                finally
                {
                    // We always want to ensure the callback is registered
                    RegisterChangeTokenCallback(token);
                }
            }
        }

        private sealed class AsyncChangeTokenRegistration<TState> : ChangeTokenRegistration<TState>
        {
            private readonly Func<TState, Task> _changeTokenConsumer;

            public AsyncChangeTokenRegistration(Func<IChangeToken?> changeTokenProducer, Func<TState, Task> changeTokenConsumer, TState state)
                : base(changeTokenProducer, state)
            {
                _changeTokenConsumer = changeTokenConsumer;

                RegisterChangeTokenCallback(changeTokenProducer());
            }

            protected override void OnChangeTokenFired()
            {
                // The order here is important. We need to take the token and then apply our changes BEFORE
                // registering. This prevents us from possible having two change updates to process concurrently.
                //
                // If the token changes after we take the token, then we'll process the update immediately upon
                // registering the callback once the consumer's task completes.
                IChangeToken? token = ChangeTokenProducer();

                Task consumerTask;
                try
                {
                    // The consumer is invoked synchronously here, so that synchronous exceptions from it are propagated
                    // to the code that triggers the change token, just like the sync overload does.
                    consumerTask = _changeTokenConsumer(State);
                }
                catch
                {
                    // We always want to ensure the callback is registered, even when the consumer throws synchronously.
                    RegisterChangeTokenCallback(token);
                    throw;
                }

                if (consumerTask is null)
                {
                    RegisterChangeTokenCallback(token);
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_NullConsumerTask);
                }

                if (consumerTask.Status == TaskStatus.RanToCompletion)
                {
                    // The common case where the consumer completes synchronously: re-register without allocations.
                    RegisterChangeTokenCallback(token);
                }
                else
                {
                    // Asynchronous exceptions can't be propagated without blocking, so they are left unobserved
                    // (meaning they can be observed only through TaskScheduler.UnobservedTaskException).
                    _ = AwaitConsumerAndRegisterCallback(consumerTask, token);
                }
            }

            private async Task AwaitConsumerAndRegisterCallback(Task consumerTask, IChangeToken? token)
            {
                try
                {
                    await consumerTask.ConfigureAwait(false);
                }
                finally
                {
                    // We always want to ensure the callback is registered
                    RegisterChangeTokenCallback(token);
                }
            }
        }
    }
}
