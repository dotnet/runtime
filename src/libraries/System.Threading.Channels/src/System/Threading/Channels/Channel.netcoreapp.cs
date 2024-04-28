// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Threading.Channels
{
    /// <summary>Provides static methods for creating channels.</summary>
    public static partial class Channel
    {
        /// <summary>Creates an unbounded prioritized channel usable by any number of readers and writers concurrently.</summary>
        /// <typeparam name="T">Specifies the type of data in the channel.</typeparam>
        /// <returns>The created channel.</returns>
        /// <remarks>
        /// <see cref="Comparer{T}.Default"/> is used to determine priority of elements.
        /// The next item read from the channel will be the element available in the channel with the lowest priority value.
        /// </remarks>
        public static Channel<T> CreateUnboundedPrioritized<T>() =>
            new UnboundedChannel<T, UnboundedChannelPriorityQueue<T>>(new(new()), runContinuationsAsynchronously: true);

        /// <summary>Creates an unbounded prioritized channel subject to the provided options.</summary>
        /// <typeparam name="T">Specifies the type of data in the channel.</typeparam>
        /// <param name="options">Options that guide the behavior of the channel.</param>
        /// <returns>The created channel.</returns>
        /// <remarks>
        /// The supplied <paramref name="options"/>' <see cref="UnboundedPrioritizedChannelOptions{T}.Comparer"/> is used to determine priority of elements,
        /// or <see cref="Comparer{T}.Default"/> if the provided comparer is null.
        /// The next item read from the channel will be the element available in the channel with the lowest priority value.
        /// </remarks>
        public static Channel<T> CreateUnboundedPrioritized<T>(UnboundedPrioritizedChannelOptions<T> options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return new UnboundedChannel<T, UnboundedChannelPriorityQueue<T>>(new(new(options.Comparer)), !options.AllowSynchronousContinuations);
        }

        /// <summary>Provides an <see cref="IUnboundedChannelQueue{T}"/> for a <see cref="PriorityQueue{TElement, TPriority}"/>.</summary>
        private readonly struct UnboundedChannelPriorityQueue<T>(PriorityQueue<bool, T> queue) : IUnboundedChannelQueue<T>
        {
            private readonly PriorityQueue<bool, T> _queue = queue;

            /// <inheritdoc/>
            public bool IsThreadSafe => false;

            /// <inheritdoc/>
            public void Enqueue(T item) => _queue.Enqueue(true, item);

            /// <inheritdoc/>
            public bool TryDequeue([MaybeNullWhen(false)] out T item) => _queue.TryDequeue(out _, out item);

            /// <inheritdoc/>
            public bool TryPeek([MaybeNullWhen(false)] out T item) => _queue.TryPeek(out _, out item);

            /// <inheritdoc/>
            public int Count => _queue.Count;

            /// <inheritdoc/>
            public bool IsEmpty => _queue.Count == 0;

            /// <inheritdoc/>
            public IEnumerator<T> GetEnumerator()
            {
                List<T> list = [];
                foreach ((bool _, T Priority) item in _queue.UnorderedItems)
                {
                    list.Add(item.Priority);
                }

                list.Sort(_queue.Comparer);

                return list.GetEnumerator();
            }
        }
    }
}
