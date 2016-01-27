// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ListChunk.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A linked list of array chunks. Allows direct access to its arrays.
    /// </summary>
    /// <typeparam name="TInputOutput">The elements held within.</typeparam>
    internal class ListChunk<TInputOutput> : IEnumerable<TInputOutput>
    {
        internal TInputOutput[] _chunk;
        private int _chunkCount;
        private ListChunk<TInputOutput> _nextChunk;
        private ListChunk<TInputOutput> _tailChunk;

        /// <summary>
        /// Allocates a new root chunk of a particular size.
        /// </summary>
        internal ListChunk(int size)
        {
            Debug.Assert(size > 0);
            _chunk = new TInputOutput[size];
            _chunkCount = 0;
            _tailChunk = this;
        }

        /// <summary>
        /// Adds an element to this chunk.  Only ever called on the root.
        /// </summary>
        /// <param name="e">The new element.</param>
        internal void Add(TInputOutput e)
        {
            ListChunk<TInputOutput> tail = _tailChunk;
            if (tail._chunkCount == tail._chunk.Length)
            {
                _tailChunk = new ListChunk<TInputOutput>(tail._chunkCount * 2);
                tail = (tail._nextChunk = _tailChunk);
            }

            tail._chunk[tail._chunkCount++] = e;
        }

        /// <summary>
        /// The next chunk in the linked chain.
        /// </summary>
        internal ListChunk<TInputOutput> Next
        {
            get { return _nextChunk; }
        }

        /// <summary>
        /// The number of elements contained within this particular chunk.
        /// </summary>
        internal int Count
        {
            get { return _chunkCount; }
        }

        /// <summary>
        /// Fetches an enumerator to walk the elements in all chunks rooted from this one.
        /// </summary>
        public IEnumerator<TInputOutput> GetEnumerator()
        {
            ListChunk<TInputOutput> curr = this;
            while (curr != null)
            {
                for (int i = 0; i < curr._chunkCount; i++)
                {
                    yield return curr._chunk[i];
                }
                Debug.Assert(curr._chunkCount == curr._chunk.Length || curr._nextChunk == null);
                curr = curr._nextChunk;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TInputOutput>)this).GetEnumerator();
        }
    }
}
