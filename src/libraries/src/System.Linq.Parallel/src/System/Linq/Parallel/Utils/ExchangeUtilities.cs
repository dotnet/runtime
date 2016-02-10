// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ExchangeUtilities.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// ExchangeUtilities is a static class that contains helper functions to partition and merge
    /// streams. 
    /// </summary>
    internal static class ExchangeUtilities
    {
        //-----------------------------------------------------------------------------------
        // A factory method to construct a partitioned stream over a data source.
        //
        // Arguments:
        //    source                      - the data source to be partitioned
        //    partitionCount              - the number of partitions desired
        //    useOrdinalOrderPreservation - whether ordinal position must be tracked
        //    useStriping                 - whether striped partitioning should be used instead of range partitioning
        //

        internal static PartitionedStream<T, int> PartitionDataSource<T>(IEnumerable<T> source, int partitionCount, bool useStriping)
        {
            // The partitioned stream to return.
            PartitionedStream<T, int> returnValue;

            IParallelPartitionable<T> sourceAsPartitionable = source as IParallelPartitionable<T>;
            if (sourceAsPartitionable != null)
            {
                // The type overrides the partitioning algorithm, so we will use it instead of the default.
                // The returned enumerator must be the same size that we requested, otherwise we throw.
                QueryOperatorEnumerator<T, int>[] enumerators = sourceAsPartitionable.GetPartitions(partitionCount);
                if (enumerators == null)
                {
                    throw new InvalidOperationException(SR.ParallelPartitionable_NullReturn);
                }
                else if (enumerators.Length != partitionCount)
                {
                    throw new InvalidOperationException(SR.ParallelPartitionable_IncorretElementCount);
                }

                // Now just copy the enumerators into the stream, validating that the result is non-null.
                PartitionedStream<T, int> stream =
                    new PartitionedStream<T, int>(partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Correct);
                for (int i = 0; i < partitionCount; i++)
                {
                    QueryOperatorEnumerator<T, int> currentEnumerator = enumerators[i];
                    if (currentEnumerator == null)
                    {
                        throw new InvalidOperationException(SR.ParallelPartitionable_NullElement);
                    }
                    stream[i] = currentEnumerator;
                }

                returnValue = stream;
            }
            else
            {
                returnValue = new PartitionedDataSource<T>(source, partitionCount, useStriping);
            }

            Debug.Assert(returnValue.PartitionCount == partitionCount);

            return returnValue;
        }

        //-----------------------------------------------------------------------------------
        // Converts an enumerator or a partitioned stream into a hash-partitioned stream. In the resulting
        // partitioning, all elements with the same hash code are guaranteed to be in the same partition.
        //
        // Arguments:
        //    source                      - the data to be hash-partitioned. If it is a partitioned stream, it 
        //                                  must have partitionCount partitions 
        //    partitionCount              - the desired number of partitions
        //    useOrdinalOrderPreservation - whether ordinal order preservation is required
        //    keySelector                 - function to obtain the key given an element
        //    keyComparer                 - equality comparer for the keys
        //

        internal static PartitionedStream<Pair<TElement, THashKey>, int> HashRepartition<TElement, THashKey, TIgnoreKey>(
            PartitionedStream<TElement, TIgnoreKey> source, Func<TElement, THashKey> keySelector, IEqualityComparer<THashKey> keyComparer,
            IEqualityComparer<TElement> elementComparer, CancellationToken cancellationToken)
        {
            TraceHelpers.TraceInfo("PartitionStream<..>.HashRepartitionStream(..):: creating **RE**partitioned stream for nested operator");
            return new UnorderedHashRepartitionStream<TElement, THashKey, TIgnoreKey>(source, keySelector, keyComparer, elementComparer, cancellationToken);
        }

        internal static PartitionedStream<Pair<TElement, THashKey>, TOrderKey> HashRepartitionOrdered<TElement, THashKey, TOrderKey>(
            PartitionedStream<TElement, TOrderKey> source, Func<TElement, THashKey> keySelector, IEqualityComparer<THashKey> keyComparer,
            IEqualityComparer<TElement> elementComparer, CancellationToken cancellationToken)
        {
            TraceHelpers.TraceInfo("PartitionStream<..>.HashRepartitionStream(..):: creating **RE**partitioned stream for nested operator");
            return new OrderedHashRepartitionStream<TElement, THashKey, TOrderKey>(source, keySelector, keyComparer, elementComparer, cancellationToken);
        }

        //---------------------------------------------------------------------------------------
        // A helper method that given two OrdinalIndexState values return the "worse" one. For
        // example, if state1 is valid and state2 is increasing, we will return
        // OrdinalIndexState.Increasing.
        //

        internal static OrdinalIndexState Worse(this OrdinalIndexState state1, OrdinalIndexState state2)
        {
            return state1 > state2 ? state1 : state2;
        }

        internal static bool IsWorseThan(this OrdinalIndexState state1, OrdinalIndexState state2)
        {
            return state1 > state2;
        }
    }

    /// <summary>
    /// Used during hash partitioning, when the keys being memoized are not used for anything.
    /// </summary>
    internal struct NoKeyMemoizationRequired { }
}
