// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Threading.Channels
{
    public abstract partial class ChannelReader<T>
    {
        /// <summary>Creates an <see cref="IAsyncEnumerable{T}"/> that enables reading all of the data from the channel.</summary>
        /// <remarks>
        /// Each <see cref="IAsyncEnumerator{T}.MoveNextAsync"/> call that returns <c>true</c> will read the next item out of the channel.
        /// <see cref="IAsyncEnumerator{T}.MoveNextAsync"/> will return false once no more data is or will ever be available to read.
        /// </remarks>
        /// <returns>The created async enumerable.</returns>
        public virtual IAsyncEnumerable<T> ReadAllAsync() => new AsyncEnumerable(this);

        /// <summary>Provides the async enumerable implementation for <see cref="ReadAllAsync"/>.</summary>
        private sealed class AsyncEnumerable : IAsyncEnumerable<T>
        {
            /// <summary>The reader instance whose contents should be read.</summary>
            private readonly ChannelReader<T> _reader;

            /// <summary>Initializes the enumerable.</summary>
            /// <param name="reader">The reader to read.</param>
            internal AsyncEnumerable(ChannelReader<T> reader)
            {
                Debug.Assert(reader != null);
                _reader = reader;
            }

            async IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
            {
                while (await _reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    while (_reader.TryRead(out T item))
                    {
                        yield return item;
                    }
                }
            }
        }
    }
}
