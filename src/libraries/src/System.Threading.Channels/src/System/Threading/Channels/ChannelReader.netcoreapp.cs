// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Threading.Channels
{
    public abstract partial class ChannelReader<T>
    {
        /// <summary>Creates an <see cref="IAsyncEnumerable{T}"/> that enables reading all of the data from the channel.</summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to cancel the enumeration.</param>
        /// <remarks>
        /// Each <see cref="IAsyncEnumerator{T}.MoveNextAsync"/> call that returns <c>true</c> will read the next item out of the channel.
        /// <see cref="IAsyncEnumerator{T}.MoveNextAsync"/> will return false once no more data is or will ever be available to read.
        /// </remarks>
        /// <returns>The created async enumerable.</returns>
        public virtual async IAsyncEnumerable<T> ReadAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (await WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                while (TryRead(out T item))
                {
                    yield return item;
                }
            }
        }
    }
}
