// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.NETCore.Client
{
    /// <summary>
    /// A collection of objects that allows for observability and mutability using handlers.
    /// </summary>
    internal class HandleableCollection<T> : IEnumerable<T>, IDisposable
    {
        public delegate bool Handler(T item, out bool removeItem);

        /// <summary>
        /// Accepts the first item it encounters and requests that the item is removed from the collection.
        /// </summary>
        private static readonly Handler DefaultHandler = (T item, out bool removeItem) => { removeItem = true; return true; };

        private readonly List<T> _items = new List<T>();
        private readonly List<Tuple<TaskCompletionSource<T>, Handler>> _handlers = new List<Tuple<TaskCompletionSource<T>, Handler>>();

        private bool _disposed = false;

        /// <summary>
        /// Returns an enumerator that iterates through the underlying collection.
        /// </summary>
        /// <remarks>
        /// The returned enumerator is over a copy of the underlying collection so that there are no concurrency issues.
        /// </remarks>
        IEnumerator IEnumerable.GetEnumerator()
        {
            IList<T> copy;
            lock (_items)
            {
                VerifyNotDisposed();
                copy = _items.ToList();
            }
            return copy.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the underlying collection.
        /// </summary>
        /// <remarks>
        /// The returned enumerator is over a copy of the underlying collection so that there are no concurrency issues.
        /// </remarks>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            IList<T> copy;
            lock (_items)
            {
                VerifyNotDisposed();
                copy = _items.ToList();
            }
            return copy.GetEnumerator();
        }

        /// <summary>
        /// Disposes the <see cref="HandleableCollection{T}"/> by clearing all items and handlers.
        /// </summary>
        /// <remarks>
        /// All pending handlers with throw <see cref="ObjectDisposedException"/>.
        /// </remarks>
        public void Dispose()
        {
            lock (_items)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
            }

            RemoveAndDisposeItems();

            foreach (Tuple<TaskCompletionSource<T>, Handler> tuple in _handlers)
            {
                tuple.Item1.TrySetException(new ObjectDisposedException(nameof(HandleableCollection<T>)));
            }
            _handlers.Clear();
        }

        /// <summary>
        /// Adds an item so that it may be observed by a handler.
        /// </summary>
        /// <param name="item">Item to add to the collection.</param>
        /// <remarks>
        /// The item may be immediately consumed if a handler removes the item, thus it may
        /// not be stored into the underlying list.
        /// </remarks>
        public void Add(in T item)
        {
            lock (_items)
            {
                VerifyNotDisposed();

                bool handledValue = false;
                for (int i = 0; !handledValue && i < _handlers.Count; i++)
                {
                    Tuple<TaskCompletionSource<T>, Handler> handler = _handlers[i];

                    if (TryHandler(item, handler.Item2, handler.Item1, out handledValue))
                    {
                        _handlers.RemoveAt(i);
                        i--;
                    }
                }

                if (!handledValue)
                {
                    _items.Add(item);
                }
            }
        }

        /// <summary>
        /// Returns the first item offered to the handler
        /// or waits for a future item if no item is immediately available.
        /// </summary>
        /// <param name="timeout">The amount of time to wait before cancelling the handler.</param>
        /// <returns>The first item offered to the handler.</returns>
        public T Handle(TimeSpan timeout) => Handle(DefaultHandler, timeout);

        /// <summary>
        /// Returns the item on which the handler completes or waits for future items
        /// if the handler does not immediately complete.
        /// </summary>
        /// <param name="handler">The handler that determines on which item to complete.</param>
        /// <param name="timeout">The amount of time to wait before cancelling the handler.</param>
        /// <returns>The item on which the handler completes.</returns>
        public T Handle(Handler handler, TimeSpan timeout)
        {
            using var cancellation = new CancellationTokenSource(timeout);

            var completionSource = new TaskCompletionSource<T>();
            using var _ = cancellation.Token.Register(
                () => completionSource.TrySetException(new TimeoutException()));

            RunOrQueueHandler(handler, completionSource);

            try
            {
                return completionSource.Task.Result;
            }
            catch (AggregateException ex)
            {
                throw ex.GetBaseException();
            }
        }

        /// <summary>
        /// Returns the first item offered to the handler
        /// or waits for a future item if no item is immediately available.
        /// </summary>
        /// <param name="token">The token to monitor for cancellation requests.</param>
        /// <returns>A task that completes with the first item offered to the handler.</returns>
        public Task<T> HandleAsync(CancellationToken token) => HandleAsync(DefaultHandler, token);

        /// <summary>
        /// Returns the item on which the handler completes and waits for future items
        /// if the handler does not immediately complete.
        /// </summary>
        /// <param name="handler">The handler that determines on which item to complete.</param>
        /// <param name="token">The token to monitor for cancellation requests.</param>
        /// <returns>A task that completes with the item on which the handler completes.</returns>
        public async Task<T> HandleAsync(Handler handler, CancellationToken token)
        {
            var completionSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var _ = token.Register(() => completionSource.TrySetCanceled(token));

            RunOrQueueHandler(handler, completionSource);

            return await completionSource.Task.ConfigureAwait(false);
        }

        private void RunOrQueueHandler(Handler handler, TaskCompletionSource<T> completionSource)
        {
            lock (_items)
            {
                VerifyNotDisposed();

                OnHandlerBegin();

                bool stopHandling = false;
                for (int i = 0; !stopHandling && i < _items.Count; i++)
                {
                    T item = _items[i];

                    stopHandling = TryHandler(item, handler, completionSource, out bool removeItem);

                    if (removeItem)
                    {
                        _items.RemoveAt(i);
                        i--;
                    }
                }

                if (!stopHandling)
                {
                    _handlers.Add(Tuple.Create(completionSource, handler));
                }
            }
        }

        private static bool TryHandler(in T item, Handler handler, TaskCompletionSource<T> completionSource, out bool removeItem)
        {
            removeItem = false;
            if (completionSource.Task.IsCompleted)
            {
                return true;
            }

            bool stopHandling = false;
            try
            {
                stopHandling = handler(item, out removeItem);
            }
            catch (Exception ex)
            {
                completionSource.TrySetException(ex);
                return true;
            }

            if (stopHandling)
            {
                completionSource.TrySetResult(item);
            }

            return stopHandling;
        }

        /// <summary>
        /// Clears all items from the collection.
        /// </summary>
        public void ClearItems()
        {
            lock (_items)
            {
                VerifyNotDisposed();

                RemoveAndDisposeItems();
            }
        }

        private void RemoveAndDisposeItems()
        {
            foreach (T item in _items)
            {
                if (item is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _items.Clear();
        }

        private void VerifyNotDisposed()
        {
            Debug.Assert(Monitor.IsEntered(_items));

            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HandleableCollection<T>));
            }
        }

        protected virtual void OnHandlerBegin()
        {
        }
    }
}
