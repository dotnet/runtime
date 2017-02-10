// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ContainsSearchOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Contains is quite similar to the any/all operator above. Each partition searches a
    /// subset of elements for a match, and the first one to find a match signals to the rest
    /// of the partitions to stop searching.
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    internal sealed class ContainsSearchOperator<TInput> : UnaryQueryOperator<TInput, bool>
    {
        private readonly TInput _searchValue; // The value for which we are searching.
        private readonly IEqualityComparer<TInput> _comparer; // The comparer to use for equality tests.

        //---------------------------------------------------------------------------------------
        // Constructs a new instance of the contains search operator.
        //
        // Arguments:
        //     child       - the child tree to enumerate.
        //     searchValue - value we are searching for.
        //     comparer    - a comparison routine used to test equality.
        //

        internal ContainsSearchOperator(IEnumerable<TInput> child, TInput searchValue, IEqualityComparer<TInput> comparer)
            : base(child)
        {
            Debug.Assert(child != null, "child data source cannot be null");

            _searchValue = searchValue;

            if (comparer == null)
            {
                _comparer = EqualityComparer<TInput>.Default;
            }
            else
            {
                _comparer = comparer;
            }
        }

        //---------------------------------------------------------------------------------------
        // Executes the entire query tree, and aggregates the individual partition results to
        // form an overall answer to the search operation.
        //

        internal bool Aggregate()
        {
            // Because the final reduction is typically much cheaper than the intermediate 
            // reductions over the individual partitions, and because each parallel partition
            // could do a lot of work to produce a single output element, we prefer to turn off
            // pipelining, and process the final reductions serially.
            using (IEnumerator<bool> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, true))
            {
                // Any value of true means the element was found. We needn't consult all partitions
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<bool> Open(QuerySettings settings, bool preferStriping)
        {
            QueryResults<TInput> childQueryResults = Child.Open(settings, preferStriping);
            return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
        }

        internal override void WrapPartitionedStream<TKey>(
            PartitionedStream<TInput, TKey> inputStream, IPartitionedStreamRecipient<bool> recipient, bool preferStriping, QuerySettings settings)
        {
            int partitionCount = inputStream.PartitionCount;
            PartitionedStream<bool, int> outputStream = new PartitionedStream<bool, int>(partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Correct);

            // Create a shared cancellation variable
            Shared<bool> resultFoundFlag = new Shared<bool>(false);
            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new ContainsSearchOperatorEnumerator<TKey>(inputStream[i], _searchValue, _comparer, i, resultFoundFlag,
                    settings.CancellationState.MergedCancellationToken);
            }

            recipient.Receive(outputStream);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        [ExcludeFromCodeCoverage]
        internal override IEnumerable<bool> AsSequentialQuery(CancellationToken token)
        {
            Debug.Fail("This method should never be called as it is an ending operator with LimitsParallelism=false.");
            throw new NotSupportedException();
        }

        //---------------------------------------------------------------------------------------
        // Whether this operator performs a premature merge that would not be performed in
        // a similar sequential operation (i.e., in LINQ to Objects).
        //

        internal override bool LimitsParallelism
        {
            get { return false; }
        }

        //---------------------------------------------------------------------------------------
        // This enumerator performs the search over its input data source. It also cancels peer
        // enumerators when an answer was found, and polls this cancellation flag to stop when
        // requested.
        //

        class ContainsSearchOperatorEnumerator<TKey> : QueryOperatorEnumerator<bool, int>
        {
            private readonly QueryOperatorEnumerator<TInput, TKey> _source; // The source data.
            private readonly TInput _searchValue; // The value for which we are searching.
            private readonly IEqualityComparer<TInput> _comparer; // The comparer to use for equality tests.
            private readonly int _partitionIndex; // This partition's unique index.
            private readonly Shared<bool> _resultFoundFlag; // Whether to cancel the operation.
            private CancellationToken _cancellationToken;

            //---------------------------------------------------------------------------------------
            // Instantiates a new any/all search operator.
            //

            internal ContainsSearchOperatorEnumerator(QueryOperatorEnumerator<TInput, TKey> source, TInput searchValue,
                                                      IEqualityComparer<TInput> comparer, int partitionIndex, Shared<bool> resultFoundFlag,
                CancellationToken cancellationToken)
            {
                Debug.Assert(source != null);
                Debug.Assert(comparer != null);
                Debug.Assert(resultFoundFlag != null);

                _source = source;
                _searchValue = searchValue;
                _comparer = comparer;
                _partitionIndex = partitionIndex;
                _resultFoundFlag = resultFoundFlag;
                _cancellationToken = cancellationToken;
            }

            //---------------------------------------------------------------------------------------
            // This enumerates the entire input source to perform the search. If another peer
            // partition finds an answer before us, we will voluntarily return (propagating the
            // peer's result).
            //

            internal override bool MoveNext(ref bool currentElement, ref int currentKey)
            {
                Debug.Assert(_comparer != null);

                // Avoid enumerating if we've already found an answer.
                if (_resultFoundFlag.Value)
                    return false;

                // We just scroll through the enumerator and accumulate the result.
                TInput element = default(TInput);
                TKey keyUnused = default(TKey);
                if (_source.MoveNext(ref element, ref keyUnused))
                {
                    currentElement = false;
                    currentKey = _partitionIndex;

                    // Continue walking the data so long as we haven't found an item that satisfies
                    // the condition we are searching for.
                    int i = 0;
                    do
                    {
                        if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                            CancellationState.ThrowIfCanceled(_cancellationToken);

                        if (_resultFoundFlag.Value)
                        {
                            // If cancellation occurred, it's because a successful answer was found.
                            return false;
                        }

                        if (_comparer.Equals(element, _searchValue))
                        {
                            // We have found an item that satisfies the search. Cancel other
                            // workers that are concurrently searching, and return.
                            _resultFoundFlag.Value = true;
                            currentElement = true;
                            break;
                        }
                    }
                    while (_source.MoveNext(ref element, ref keyUnused));

                    return true;
                }

                return false;
            }

            protected override void Dispose(bool disposing)
            {
                Debug.Assert(_source != null);
                _source.Dispose();
            }
        }
    }
}
