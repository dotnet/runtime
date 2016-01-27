// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// PartitionedStream.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A partitioned stream just partitions some data source using an extensible 
    /// partitioning algorithm and exposes a set of N enumerators that are consumed by
    /// their ordinal index [0..N). It is used to build up a set of streaming computations.
    /// At instantiation time, the actual data source to be partitioned is supplied; and
    /// then the caller will layer on top additional enumerators to represent phases in the
    /// computation. Eventually, a merge can then schedule enumeration of all of the
    /// individual partitions in parallel by obtaining references to the individual
    /// partition streams.
    ///
    /// This type has a set of subclasses which implement different partitioning algorithms,
    /// allowing us to easily plug in different partitioning techniques as needed. The type
    /// supports wrapping IEnumerables and IEnumerators alike, with some preference for the
    /// former as many partitioning algorithms are more intelligent for certain data types.
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    internal class PartitionedStream<TElement, TKey>
    {
        protected QueryOperatorEnumerator<TElement, TKey>[] _partitions; // Partitions exposed by this object.
        private readonly IComparer<TKey> _keyComparer; // Comparer for order keys.
        private readonly OrdinalIndexState _indexState; // State of the order keys.

        internal PartitionedStream(int partitionCount, IComparer<TKey> keyComparer, OrdinalIndexState indexState)
        {
            Debug.Assert(partitionCount > 0);
            _partitions = new QueryOperatorEnumerator<TElement, TKey>[partitionCount];
            _keyComparer = keyComparer;
            _indexState = indexState;
        }

        //---------------------------------------------------------------------------------------
        // Retrieves or sets a partition for the given index.
        //
        // Assumptions:
        //    The index falls within the legal range of the enumerator, i.e. 0 <= value < count.
        //

        internal QueryOperatorEnumerator<TElement, TKey> this[int index]
        {
            get
            {
                Debug.Assert(_partitions != null);
                Debug.Assert(0 <= index && index < _partitions.Length, "index out of bounds");
                return _partitions[index];
            }
            set
            {
                Debug.Assert(_partitions != null);
                Debug.Assert(value != null);
                Debug.Assert(0 <= index && index < _partitions.Length, "index out of bounds");
                _partitions[index] = value;
            }
        }

        //---------------------------------------------------------------------------------------
        // Retrieves the number of partitions.
        //

        public int PartitionCount
        {
            get
            {
                Debug.Assert(_partitions != null);
                return _partitions.Length;
            }
        }

        //---------------------------------------------------------------------------------------
        // The comparer for the order keys.
        //

        internal IComparer<TKey> KeyComparer
        {
            get { return _keyComparer; }
        }

        //---------------------------------------------------------------------------------------
        // State of the order keys.
        //

        internal OrdinalIndexState OrdinalIndexState
        {
            get { return _indexState; }
        }
    }
}
