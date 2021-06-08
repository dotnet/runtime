// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// PartitionerQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Linq.Parallel;
using System.Diagnostics;
using System.Threading;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A QueryOperator that represents the output of the query partitioner.AsParallel().
    /// </summary>
    internal sealed class PartitionerQueryOperator<TElement> : QueryOperator<TElement>
    {
        private readonly Partitioner<TElement> _partitioner; // The partitioner to use as data source.

        internal PartitionerQueryOperator(Partitioner<TElement> partitioner)
            : base(false, QuerySettings.Empty)
        {
            _partitioner = partitioner;
        }

        internal bool Orderable
        {
            get { return _partitioner is OrderablePartitioner<TElement>; }
        }

        internal override QueryResults<TElement> Open(QuerySettings settings, bool preferStriping)
        {
            // Notice that the preferStriping argument is not used. Partitioner<T> does not support
            // striped partitioning.

            return new PartitionerQueryOperatorResults(_partitioner, settings);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TElement> AsSequentialQuery(CancellationToken token)
        {
            using (IEnumerator<TElement> enumerator = _partitioner.GetPartitions(1)[0])
            {
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
            }
        }

        //---------------------------------------------------------------------------------------
        // The state of the order index of the results returned by this operator.
        //

        internal override OrdinalIndexState OrdinalIndexState
        {
            get { return GetOrdinalIndexState(_partitioner); }
        }

        /// <summary>
        /// Determines the OrdinalIndexState for a partitioner
        /// </summary>
        internal static OrdinalIndexState GetOrdinalIndexState(Partitioner<TElement> partitioner)
        {
            OrderablePartitioner<TElement>? orderablePartitioner = partitioner as OrderablePartitioner<TElement>;

            if (orderablePartitioner == null)
            {
                return OrdinalIndexState.Shuffled;
            }

            if (orderablePartitioner.KeysOrderedInEachPartition)
            {
                if (orderablePartitioner.KeysNormalized)
                {
                    return OrdinalIndexState.Correct;
                }
                else
                {
                    return OrdinalIndexState.Increasing;
                }
            }
            else
            {
                return OrdinalIndexState.Shuffled;
            }
        }


        //---------------------------------------------------------------------------------------
        // Whether this operator performs a premature merge that would not be performed in
        // a similar sequential operation (i.e., in LINQ to Objects).
        //

        internal override bool LimitsParallelism
        {
            get { return false; }
        }


        /// <summary>
        /// QueryResults for a PartitionerQueryOperator
        /// </summary>
        private sealed class PartitionerQueryOperatorResults : QueryResults<TElement>
        {
            private readonly Partitioner<TElement> _partitioner; // The data source for the query

            private QuerySettings _settings; // Settings collected from the query

            internal PartitionerQueryOperatorResults(Partitioner<TElement> partitioner, QuerySettings settings)
            {
                _partitioner = partitioner;
                _settings = settings;
            }

            internal override void GivePartitionedStream(IPartitionedStreamRecipient<TElement> recipient)
            {
                Debug.Assert(_settings.DegreeOfParallelism.HasValue);
                int partitionCount = _settings.DegreeOfParallelism.Value;

                OrderablePartitioner<TElement>? orderablePartitioner = _partitioner as OrderablePartitioner<TElement>;

                // If the partitioner is not orderable, it will yield zeros as order keys. The order index state
                // is irrelevant.
                OrdinalIndexState indexState = (orderablePartitioner != null)
                    ? GetOrdinalIndexState(orderablePartitioner)
                    : OrdinalIndexState.Shuffled;

                PartitionedStream<TElement, int> partitions = new PartitionedStream<TElement, int>(
                    partitionCount,
                    Util.GetDefaultComparer<int>(),
                    indexState);

                if (orderablePartitioner != null)
                {
                    IList<IEnumerator<KeyValuePair<long, TElement>>> partitionerPartitions =
                        orderablePartitioner.GetOrderablePartitions(partitionCount);

                    if (partitionerPartitions == null)
                    {
                        throw new InvalidOperationException(SR.PartitionerQueryOperator_NullPartitionList);
                    }

                    if (partitionerPartitions.Count != partitionCount)
                    {
                        throw new InvalidOperationException(SR.PartitionerQueryOperator_WrongNumberOfPartitions);
                    }

                    for (int i = 0; i < partitionCount; i++)
                    {
                        IEnumerator<KeyValuePair<long, TElement>> partition = partitionerPartitions[i];
                        if (partition == null)
                        {
                            throw new InvalidOperationException(SR.PartitionerQueryOperator_NullPartition);
                        }

                        partitions[i] = new OrderablePartitionerEnumerator(partition);
                    }
                }
                else
                {
                    IList<IEnumerator<TElement>> partitionerPartitions =
                        _partitioner.GetPartitions(partitionCount);

                    if (partitionerPartitions == null)
                    {
                        throw new InvalidOperationException(SR.PartitionerQueryOperator_NullPartitionList);
                    }

                    if (partitionerPartitions.Count != partitionCount)
                    {
                        throw new InvalidOperationException(SR.PartitionerQueryOperator_WrongNumberOfPartitions);
                    }

                    for (int i = 0; i < partitionCount; i++)
                    {
                        IEnumerator<TElement> partition = partitionerPartitions[i];
                        if (partition == null)
                        {
                            throw new InvalidOperationException(SR.PartitionerQueryOperator_NullPartition);
                        }

                        partitions[i] = new PartitionerEnumerator(partition);
                    }
                }

                recipient.Receive<int>(partitions);
            }
        }

        /// <summary>
        /// Enumerator that converts an enumerator over key-value pairs exposed by a partitioner
        /// to a QueryOperatorEnumerator used by PLINQ internally.
        /// </summary>
        private sealed class OrderablePartitionerEnumerator : QueryOperatorEnumerator<TElement, int>
        {
            private readonly IEnumerator<KeyValuePair<long, TElement>> _sourceEnumerator;

            internal OrderablePartitionerEnumerator(IEnumerator<KeyValuePair<long, TElement>> sourceEnumerator)
            {
                _sourceEnumerator = sourceEnumerator;
            }

            internal override bool MoveNext([MaybeNullWhen(false), AllowNull] ref TElement currentElement, ref int currentKey)
            {
                if (!_sourceEnumerator.MoveNext()) return false;

                KeyValuePair<long, TElement> current = _sourceEnumerator.Current;
                currentElement = current.Value;

                checked
                {
                    currentKey = (int)current.Key;
                }

                return true;
            }

            protected override void Dispose(bool disposing)
            {
                Debug.Assert(_sourceEnumerator != null);
                _sourceEnumerator.Dispose();
            }
        }

        /// <summary>
        /// Enumerator that converts an enumerator over key-value pairs exposed by a partitioner
        /// to a QueryOperatorEnumerator used by PLINQ internally.
        /// </summary>
        private sealed class PartitionerEnumerator : QueryOperatorEnumerator<TElement, int>
        {
            private readonly IEnumerator<TElement> _sourceEnumerator;

            internal PartitionerEnumerator(IEnumerator<TElement> sourceEnumerator)
            {
                _sourceEnumerator = sourceEnumerator;
            }

            internal override bool MoveNext([MaybeNullWhen(false), AllowNull] ref TElement currentElement, ref int currentKey)
            {
                if (!_sourceEnumerator.MoveNext()) return false;

                currentElement = _sourceEnumerator.Current;
                currentKey = 0;

                return true;
            }

            protected override void Dispose(bool disposing)
            {
                Debug.Assert(_sourceEnumerator != null);
                _sourceEnumerator.Dispose();
            }
        }
    }
}
