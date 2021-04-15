// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// AnyAllSearchOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// The any/all operators work the same way. They search for the occurrence of a predicate
    /// value in the data source, and upon the first occurrence of such a value, yield a
    /// particular value. Specifically:
    ///
    ///     - Any returns true if the predicate for any element evaluates to true.
    ///     - All returns false if the predicate for any element evaluates to false.
    ///
    /// This uniformity is used to apply a general purpose algorithm. Both sentences above
    /// take the form of "returns XXX if the predicate for any element evaluates to XXX."
    /// Therefore, we just parameterize on XXX, called the qualification below, and if we
    /// ever find an occurrence of XXX in the input data source, we also return XXX. Otherwise,
    /// we return !XXX. Obviously, XXX in this case is a bool.
    ///
    /// This is a search algorithm. So once any single partition finds an element, it will
    /// return so that execution can stop. This is done with a "cancellation" flag that is
    /// polled by all parallel workers. The first worker to find an answer sets it, and all
    /// other workers notice it and quit as quickly as possible.
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    internal sealed class AnyAllSearchOperator<TInput> : UnaryQueryOperator<TInput, bool>
    {
        private readonly Func<TInput, bool> _predicate; // The predicate used to test membership.
        private readonly bool _qualification; // Whether we're looking for true (any) or false (all).

        //---------------------------------------------------------------------------------------
        // Constructs a new instance of an any/all search operator.
        //
        // Arguments:
        //     child         - the child tree to enumerate.
        //     qualification - the predicate value we require for an element to be considered
        //                     a member of the set; for any this is true, for all it is false.
        //

        internal AnyAllSearchOperator(IEnumerable<TInput> child, bool qualification, Func<TInput, bool> predicate)
            : base(child)
        {
            Debug.Assert(child != null, "child data source cannot be null");
            Debug.Assert(predicate != null, "need a predicate function");

            _qualification = qualification;
            _predicate = predicate;
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
                // Any value equal to our qualification means that we've found an element matching
                // the condition, and so we return the qualification without looking in subsequent
                // partitions.
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current == _qualification)
                    {
                        return _qualification;
                    }
                }
            }

            return !_qualification;
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<bool> Open(
            QuerySettings settings, bool preferStriping)
        {
            // We just open the child operator.
            QueryResults<TInput> childQueryResults = Child.Open(settings, preferStriping);
            return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
        }

        internal override void WrapPartitionedStream<TKey>(
            PartitionedStream<TInput, TKey> inputStream, IPartitionedStreamRecipient<bool> recipient, bool preferStriping, QuerySettings settings)
        {
            // Create a shared cancellation variable and then return a possibly wrapped new enumerator.
            Shared<bool> resultFoundFlag = new Shared<bool>(false);

            int partitionCount = inputStream.PartitionCount;
            PartitionedStream<bool, int> outputStream = new PartitionedStream<bool, int>(
                partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Correct);

            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new AnyAllSearchOperatorEnumerator<TKey>(inputStream[i], _qualification, _predicate, i, resultFoundFlag,
                    settings.CancellationState.MergedCancellationToken);
            }

            recipient.Receive(outputStream);
        }


        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        [ExcludeFromCodeCoverage(Justification = "This method should never be called as it is an ending operator with LimitsParallelism=false")]
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

        private sealed class AnyAllSearchOperatorEnumerator<TKey> : QueryOperatorEnumerator<bool, int>
        {
            private readonly QueryOperatorEnumerator<TInput, TKey> _source; // The source data.
            private readonly Func<TInput, bool> _predicate; // The predicate.
            private readonly bool _qualification; // Whether this is an any (true) or all (false) operator.
            private readonly int _partitionIndex; // The partition's index.
            private readonly Shared<bool> _resultFoundFlag; // Whether to cancel the search for elements.
            private readonly CancellationToken _cancellationToken;

            //---------------------------------------------------------------------------------------
            // Instantiates a new any/all search operator.
            //

            internal AnyAllSearchOperatorEnumerator(QueryOperatorEnumerator<TInput, TKey> source, bool qualification,
                                                    Func<TInput, bool> predicate, int partitionIndex, Shared<bool> resultFoundFlag,
                                                    CancellationToken cancellationToken)
            {
                Debug.Assert(source != null);
                Debug.Assert(predicate != null);
                Debug.Assert(resultFoundFlag != null);

                _source = source;
                _qualification = qualification;
                _predicate = predicate;
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
                Debug.Assert(_predicate != null);

                // Avoid enumerating if we've already found an answer.
                if (_resultFoundFlag.Value)
                    return false;

                // We just scroll through the enumerator and accumulate the result.
                TInput element = default(TInput)!;
                TKey keyUnused = default(TKey)!;

                if (_source.MoveNext(ref element!, ref keyUnused))
                {
                    currentElement = !_qualification;
                    currentKey = _partitionIndex;

                    int i = 0;
                    // Continue walking the data so long as we haven't found an item that satisfies
                    // the condition we are searching for.
                    do
                    {
                        if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                            _cancellationToken.ThrowIfCancellationRequested();

                        if (_resultFoundFlag.Value)
                        {
                            // If cancellation occurred, it's because a successful answer was found.
                            return false;
                        }

                        if (_predicate(element) == _qualification)
                        {
                            // We have found an item that satisfies the search. Tell other
                            // workers that are concurrently searching, and return.
                            _resultFoundFlag.Value = true;
                            currentElement = _qualification;
                            break;
                        }
                    }
                    while (_source.MoveNext(ref element!, ref keyUnused));

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
