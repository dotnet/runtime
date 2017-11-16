// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading.Channels
{
    /// <summary>Provides static methods for creating channels.</summary>
    public static class Channel
    {
        /// <summary>Creates an unbounded channel usable by any number of readers and writers concurrently.</summary>
        /// <returns>The created channel.</returns>
        public static Channel<T> CreateUnbounded<T>() =>
            new UnboundedChannel<T>(runContinuationsAsynchronously: true);

        /// <summary>Creates an unbounded channel subject to the provided options.</summary>
        /// <typeparam name="T">Specifies the type of data in the channel.</typeparam>
        /// <param name="options">Options that guide the behavior of the channel.</param>
        /// <returns>The created channel.</returns>
        public static Channel<T> CreateUnbounded<T>(UnboundedChannelOptions options) =>
            options == null ? throw new ArgumentNullException(nameof(options)) :
            options.SingleReader ? new SingleConsumerUnboundedChannel<T>(!options.AllowSynchronousContinuations) :
            (Channel<T>)new UnboundedChannel<T>(!options.AllowSynchronousContinuations);

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

            return new BoundedChannel<T>(capacity, BoundedChannelFullMode.Wait, runContinuationsAsynchronously: true);
        }

        /// <summary>Creates a channel with the specified maximum capacity.</summary>
        /// <typeparam name="T">Specifies the type of data in the channel.</typeparam>
        /// <param name="options">Options that guide the behavior of the channel.</param>
        /// <returns>The created channel.</returns>
        public static Channel<T> CreateBounded<T>(BoundedChannelOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return new BoundedChannel<T>(options.Capacity, options.FullMode, !options.AllowSynchronousContinuations);
        }

        /// <summary>Creates a channel that doesn't buffer any items.</summary>
        /// <typeparam name="T">Specifies the type of data in the channel.</typeparam>
        /// <returns>The created channel.</returns>
        public static Channel<T> CreateUnbuffered<T>() =>
            new UnbufferedChannel<T>();

        /// <summary>Creates a channel that doesn't buffer any items.</summary>
        /// <typeparam name="T">Specifies the type of data in the channel.</typeparam>
        /// <param name="options">Options that guide the behavior of the channel.</param>
        /// <returns>The created channel.</returns>
        public static Channel<T> CreateUnbuffered<T>(UnbufferedChannelOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return new UnbufferedChannel<T>();
        }
    }
}
