// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// HashPartitionedStream.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A repartitioning stream must take input data that has already been partitioned and
    /// redistribute its contents based on a new partitioning algorithm. This is accomplished
    /// by making each partition p responsible for redistributing its input data to the
    /// correct destination partition. Some input elements may remain in p, but many will now
    /// belong to a different partition and will need to move. This requires a great deal of
    /// synchronization, but allows threads to repartition data incrementally and in parallel.
    /// Each partition will "pull" data on-demand instead of partitions "pushing" data, which
    /// allows us to reduce some amount of synchronization overhead.
    ///
    /// We currently only offer one form of repartitioning via hashing.  This used to be an
    /// abstract base class, but we have eliminated that to get rid of some virtual calls on
    /// hot code paths.  Uses a key selection algorithm with mod'ding to determine destination.
    ///
    /// </summary>
    /// <typeparam name="TInputOutput"></typeparam>
    /// <typeparam name="THashKey"></typeparam>
    /// <typeparam name="TOrderKey"></typeparam>
    internal abstract class HashRepartitionStream<TInputOutput, THashKey, TOrderKey> : PartitionedStream<Pair<TInputOutput,THashKey>, TOrderKey>
    {
        private readonly IEqualityComparer<THashKey> _keyComparer; // The optional key comparison routine.
        private readonly IEqualityComparer<TInputOutput> _elementComparer; // The optional element comparison routine.
        private readonly int _distributionMod; // The distribution value we'll use to scramble input.

        //---------------------------------------------------------------------------------------
        // Creates a new partition exchange operator.
        //

        internal HashRepartitionStream(
            int partitionsCount, IComparer<TOrderKey> orderKeyComparer, IEqualityComparer<THashKey> hashKeyComparer,
            IEqualityComparer<TInputOutput> elementComparer)
            : base(partitionsCount, orderKeyComparer, OrdinalIndexState.Shuffled)
        {
            // elementComparer is used by operators that use elements themselves as the hash keys.
            // When elements are used as keys, THashKey should be NoKeyMemoizationRequired.
            _keyComparer = hashKeyComparer;
            _elementComparer = elementComparer;

            Debug.Assert(_keyComparer == null || _elementComparer == null);
            Debug.Assert(_elementComparer == null || typeof(THashKey) == typeof(NoKeyMemoizationRequired));

            // We use the following constant when distributing hash-codes into partition streams.
            // It's an (arbitrary) prime number to account for poor hashing functions, e.g. those
            // that all the primitive types use (e.g. Int32 returns itself). The goal is to add some
            // degree of randomization to avoid predictable distributions for certain data sequences,
            // for the same reason prime numbers are frequently used in hashtable implementations.
            // For instance, if all hash-codes end up as even, we would starve half of the partitions
            // by just using the raw hash-code. This isn't perfect, of course, since a stream
            // of integers with the same value end up in the same partition, but helps.
            const int DEFAULT_HASH_MOD_DISTRIBUTION = 503;

            // We need to guarantee our distribution mod is greater than the number of partitions.
            _distributionMod = DEFAULT_HASH_MOD_DISTRIBUTION;
            while (_distributionMod < partitionsCount)
            {
                // We use checked arithmetic here.  We'll only overflow for huge numbers of partitions
                // (quite unlikely), so the remote possibility of an exception is fine.
                checked
                {
                    _distributionMod *= 2;
                }
            }
        }

        //---------------------------------------------------------------------------------------
        // Manufactures a hash code for a given value or key.
        //

        // The hash-code used for null elements.
        private const int NULL_ELEMENT_HASH_CODE = 0;

        private const int HashCodeMask = 0x7fffffff;

        internal int GetHashCode(TInputOutput element)
        {
            return
                (HashCodeMask &
                    (_elementComparer == null ?
                        (element == null ? NULL_ELEMENT_HASH_CODE : element.GetHashCode()) :
                        _elementComparer.GetHashCode(element)))
                        % _distributionMod;
        }

        internal int GetHashCode(THashKey key)
        {
            return
                (HashCodeMask &
                    (_keyComparer == null ?
                        (key == null ? NULL_ELEMENT_HASH_CODE : key.GetHashCode()) :
                        _keyComparer.GetHashCode(key))) % _distributionMod;
        }
    }
}
