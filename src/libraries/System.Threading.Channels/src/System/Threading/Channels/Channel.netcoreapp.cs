// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Threading.Channels
{
    /// <summary>Provides static methods for creating channels.</summary>
    public static partial class Channel
    {
        /// <summary>Creates an unbounded prioritized channel usable by any number of readers and writers concurrently.</summary>
        /// <returns>The created channel.</returns>
        /// <remarks>
        /// <see cref="Comparer{T}.Default"/> is used to determine priority of elements.
        /// The next item read from the channel will be the element available in the channel with the lowest priority value.
        /// </remarks>
        public static Channel<T> CreateUnboundedPrioritized<T>() =>
            new UnboundedPrioritizedChannel<T>(runContinuationsAsynchronously: true, comparer: null);

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

            return new UnboundedPrioritizedChannel<T>(!options.AllowSynchronousContinuations, options.Comparer);
        }
    }
}
