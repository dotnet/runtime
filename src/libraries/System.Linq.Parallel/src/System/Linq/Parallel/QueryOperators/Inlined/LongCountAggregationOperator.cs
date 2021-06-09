// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// CountAggregationOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// An inlined count aggregation and its enumerator.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    internal sealed class LongCountAggregationOperator<TSource> : InlinedAggregationOperator<TSource, long, long>
    {
        //---------------------------------------------------------------------------------------
        // Constructs a new instance of the operator.
        //

        internal LongCountAggregationOperator(IEnumerable<TSource> child) : base(child)
        {
        }

        //---------------------------------------------------------------------------------------
        // Executes the entire query tree, and aggregates the intermediate results into the
        // final result based on the binary operators and final reduction.
        //
        // Return Value:
        //     The single result of aggregation.
        //

        protected override long InternalAggregate(ref Exception? singularExceptionToThrow)
        {
            // Because the final reduction is typically much cheaper than the intermediate
            // reductions over the individual partitions, and because each parallel partition
            // will do a lot of work to produce a single output element, we prefer to turn off
            // pipelining, and process the final reductions serially.
            using (IEnumerator<long> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, true))
            {
                // We just reduce the elements in each output partition.
                long count = 0;
                while (enumerator.MoveNext())
                {
                    checked
                    {
                        count += enumerator.Current;
                    }
                }

                return count;
            }
        }

        //---------------------------------------------------------------------------------------
        // Creates an enumerator that is used internally for the final aggregation step.
        //

        protected override QueryOperatorEnumerator<long, int> CreateEnumerator<TKey>(
            int index, int count, QueryOperatorEnumerator<TSource, TKey> source, object? sharedData, CancellationToken cancellationToken)
        {
            return new LongCountAggregationOperatorEnumerator<TKey>(source, index, cancellationToken);
        }

        //---------------------------------------------------------------------------------------
        // This enumerator type encapsulates the intermediary aggregation over the underlying
        // (possibly partitioned) data source.
        //

        private sealed class LongCountAggregationOperatorEnumerator<TKey> : InlinedAggregationOperatorEnumerator<long>
        {
            private readonly QueryOperatorEnumerator<TSource, TKey> _source; // The source data.

            //---------------------------------------------------------------------------------------
            // Instantiates a new aggregation operator.
            //

            internal LongCountAggregationOperatorEnumerator(QueryOperatorEnumerator<TSource, TKey> source, int partitionIndex,
                CancellationToken cancellationToken) :
                base(partitionIndex, cancellationToken)
            {
                Debug.Assert(source != null);
                _source = source;
            }

            //---------------------------------------------------------------------------------------
            // Counts the elements in the underlying data source, walking the entire thing the first
            // time MoveNext is called on this object.
            //

            protected override bool MoveNextCore(ref long currentElement)
            {
                TSource? elementUnused = default(TSource);
                TKey? keyUnused = default(TKey);

                QueryOperatorEnumerator<TSource, TKey> source = _source;
                if (source.MoveNext(ref elementUnused, ref keyUnused))
                {
                    // We just scroll through the enumerator and keep a running count.
                    long count = 0;
                    int i = 0;
                    do
                    {
                        if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                            _cancellationToken.ThrowIfCancellationRequested();

                        checked
                        {
                            count++;
                        }
                    }
                    while (source.MoveNext(ref elementUnused, ref keyUnused));

                    currentElement = count;
                    return true;
                }

                return false;
            }

            //---------------------------------------------------------------------------------------
            // Dispose of resources associated with the underlying enumerator.
            //

            protected override void Dispose(bool disposing)
            {
                Debug.Assert(_source != null);
                _source.Dispose();
            }
        }
    }
}
