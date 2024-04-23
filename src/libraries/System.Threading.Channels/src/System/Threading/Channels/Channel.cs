// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Threading.Channels
{
    /// <summary>Provides static methods for creating channels.</summary>
    public static partial class Channel
    {
        /// <summary>Creates an unbounded channel usable by any number of readers and writers concurrently.</summary>
        /// <returns>The created channel.</returns>
        public static Channel<T> CreateUnbounded<T>() =>
            new UnboundedChannel<T, UnboundedChannelConcurrentQueue<T>>(new(new()), runContinuationsAsynchronously: true);

        /// <summary>Creates an unbounded channel subject to the provided options.</summary>
        /// <typeparam name="T">Specifies the type of data in the channel.</typeparam>
        /// <param name="options">Options that guide the behavior of the channel.</param>
        /// <returns>The created channel.</returns>
        public static Channel<T> CreateUnbounded<T>(UnboundedChannelOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.SingleReader)
            {
                return new SingleConsumerUnboundedChannel<T>(!options.AllowSynchronousContinuations);
            }

            return new UnboundedChannel<T, UnboundedChannelConcurrentQueue<T>>(new(new()), !options.AllowSynchronousContinuations);
        }

        /// <summary>Creates a channel with the specified maximum capacity.</summary>
        /// <typeparam name="T">Specifies the type of data in the channel.</typeparam>
        /// <param name="capacity">The maximum number of items the channel may store.</param>
        /// <returns>The created channel.</returns>
        /// <remarks>
        /// Channels created with this method apply the <see cref="BoundedChannelFullMode.Wait"/>
        /// behavior and prohibit continuations from running synchronously.
        /// </remarks>
        public static Channel<T> CreateBounded<T>(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            return new BoundedChannel<T>(capacity, BoundedChannelFullMode.Wait, runContinuationsAsynchronously: true, itemDropped: null);
        }

        /// <summary>Creates a channel subject to the provided options.</summary>
        /// <typeparam name="T">Specifies the type of data in the channel.</typeparam>
        /// <param name="options">Options that guide the behavior of the channel.</param>
        /// <returns>The created channel.</returns>
        public static Channel<T> CreateBounded<T>(BoundedChannelOptions options)
        {
            return CreateBounded<T>(options, itemDropped: null);
        }

        /// <summary>Creates a channel subject to the provided options.</summary>
        /// <typeparam name="T">Specifies the type of data in the channel.</typeparam>
        /// <param name="options">Options that guide the behavior of the channel.</param>
        /// <param name="itemDropped">Delegate that will be called when item is being dropped from channel. See <see cref="BoundedChannelFullMode"/>.</param>
        /// <returns>The created channel.</returns>
        public static Channel<T> CreateBounded<T>(BoundedChannelOptions options, Action<T>? itemDropped)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return new BoundedChannel<T>(options.Capacity, options.FullMode, !options.AllowSynchronousContinuations, itemDropped);
        }

        /// <summary>Provides an <see cref="IUnboundedChannelQueue{T}"/> for a <see cref="ConcurrentQueue{T}"/>.</summary>
        private readonly struct UnboundedChannelConcurrentQueue<T>(ConcurrentQueue<T> queue) : IUnboundedChannelQueue<T>
        {
            private readonly ConcurrentQueue<T> _queue = queue;

            /// <inheritdoc/>
            public bool IsThreadSafe => true;

            /// <inheritdoc/>
            public void Enqueue(T item) => _queue.Enqueue(item);

            /// <inheritdoc/>
            public bool TryDequeue([MaybeNullWhen(false)] out T item) => _queue.TryDequeue(out item);

            /// <inheritdoc/>
            public bool TryPeek([MaybeNullWhen(false)] out T item) => _queue.TryPeek(out item);

            /// <inheritdoc/>
            public int Count => _queue.Count;

            /// <inheritdoc/>
            public bool IsEmpty => _queue.IsEmpty;

            /// <inheritdoc/>
            public IEnumerator<T> GetEnumerator() => _queue.GetEnumerator();
        }
    }
}
