// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// AssociativeAggregationOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// The aggregation operator is a little unique, in that the enumerators it returns
    /// yield intermediate results instead of the final results. That's because there is
    /// one last Aggregate operation that must occur in order to perform the final reduction
    /// over the intermediate streams. In other words, the intermediate enumerators produced
    /// by this operator are never seen by other query operators or consumers directly.
    ///
    /// An aggregation performs parallel prefixing internally. Given a binary operator O,
    /// it will generate intermediate results by folding O across partitions; then it
    /// performs a final reduction by folding O across the intermediate results. The
    /// analysis engine knows about associativity and commutativity, and will ensure the
    /// style of partitioning inserted into the tree is compatible with the operator.
    ///
    /// For instance, say O is + (meaning it is AC), our input is {1,2,...,8}, and we
    /// use 4 partitions to calculate the aggregation. Sequentially this would look
    /// like this O(O(O(1,2),...),8), in other words ((1+2)+...)+8. The parallel prefix
    /// of this (w/ 4 partitions) instead calculates the intermediate aggregations, i.e.:
    /// t1 = O(1,2), t2 = O(3,4), ... t4 = O(7,8), aka t1 = 1+2, t2 = 3+4, t4 = 7+8.
    /// The final step is to aggregate O over these intermediaries, i.e.
    /// O(O(O(t1,t2),t3),t4), or ((t1+t2)+t3)+t4. This generalizes to any binary operator.
    ///
    /// Because some aggregations use a different input, intermediate, and output types,
    /// we support an even more generalized aggregation type. In this model, we have
    /// three operators, an intermediate (used for the incremental aggregations), a
    /// final (used for the final summary of intermediate results), and a result selector
    /// (used to perform whatever transformation is needed on the final summary).
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TIntermediate"></typeparam>
    /// <typeparam name="TOutput"></typeparam>
    internal sealed class AssociativeAggregationOperator<TInput, TIntermediate, TOutput> : UnaryQueryOperator<TInput, TIntermediate>
    {
        private readonly TIntermediate _seed; // A seed used during aggregation.
        private readonly bool _seedIsSpecified; // Whether a seed was specified. If not, the first element will be used.
        private readonly bool _throwIfEmpty; // Whether to throw an exception if the data source is empty.

        // An intermediate reduction function.
        private readonly Func<TIntermediate, TInput, TIntermediate> _intermediateReduce;

        // A final reduction function.
        private readonly Func<TIntermediate, TIntermediate, TIntermediate> _finalReduce;

        // The result selector function.
        private readonly Func<TIntermediate, TOutput> _resultSelector;

        // A function that constructs seed instances
        private readonly Func<TIntermediate>? _seedFactory;

        //---------------------------------------------------------------------------------------
        // Constructs a new instance of an associative operator.
        //
        // Assumptions:
        //     This operator must be associative.
        //

        internal AssociativeAggregationOperator(IEnumerable<TInput> child, TIntermediate seed, Func<TIntermediate>? seedFactory, bool seedIsSpecified,
                                                Func<TIntermediate, TInput, TIntermediate> intermediateReduce,
                                                Func<TIntermediate, TIntermediate, TIntermediate> finalReduce,
                                                Func<TIntermediate, TOutput> resultSelector, bool throwIfEmpty, QueryAggregationOptions options)
            : base(child)
        {
            Debug.Assert(child != null, "child data source cannot be null");
            Debug.Assert(intermediateReduce != null, "need an intermediate reduce function");
            Debug.Assert(finalReduce != null, "need a final reduce function");
            Debug.Assert(resultSelector != null, "need a result selector function");
            Debug.Assert(options.IsValidQueryAggregationOption(), "enum out of valid range");
            Debug.Assert((options & QueryAggregationOptions.Associative) == QueryAggregationOptions.Associative, "expected an associative operator");
            Debug.Assert(typeof(TIntermediate) == typeof(TInput) || seedIsSpecified, "seed must be specified if TIntermediate differs from TInput");

            _seed = seed;
            _seedFactory = seedFactory;
            _seedIsSpecified = seedIsSpecified;
            _intermediateReduce = intermediateReduce;
            _finalReduce = finalReduce;
            _resultSelector = resultSelector;
            _throwIfEmpty = throwIfEmpty;
        }

        //---------------------------------------------------------------------------------------
        // Executes the entire query tree, and aggregates the intermediate results into the
        // final result based on the binary operators and final reduction.
        //
        // Return Value:
        //     The single result of aggregation.
        //

        internal TOutput Aggregate()
        {
            Debug.Assert(_finalReduce != null);
            Debug.Assert(_resultSelector != null);

            TIntermediate accumulator = default(TIntermediate)!;
            bool hadElements = false;

            // Because the final reduction is typically much cheaper than the intermediate
            // reductions over the individual partitions, and because each parallel partition
            // will do a lot of work to produce a single output element, we prefer to turn off
            // pipelining, and process the final reductions serially.
            using (IEnumerator<TIntermediate> enumerator = GetEnumerator(ParallelMergeOptions.FullyBuffered, true))
            {
                // We just reduce the elements in each output partition. If the operation is associative,
                // this will yield the correct answer. If not, we should never be calling this routine.
                while (enumerator.MoveNext())
                {
                    if (hadElements)
                    {
                        // Accumulate results by passing the current accumulation and current element to
                        // the reduction operation.
                        try
                        {
                            accumulator = _finalReduce(accumulator, enumerator.Current);
                        }
                        catch (Exception ex)
                        {
                            // We need to wrap all exceptions into an aggregate.
                            throw new AggregateException(ex);
                        }
                    }
                    else
                    {
                        // This is the first element. Just set the accumulator to the first element.
                        accumulator = enumerator.Current;
                        hadElements = true;
                    }
                }

                // If there were no elements, we must throw an exception.
                if (!hadElements)
                {
                    if (_throwIfEmpty)
                    {
                        throw new InvalidOperationException(SR.NoElements);
                    }
                    else
                    {
                        accumulator = _seedFactory == null ? _seed : _seedFactory();
                    }
                }
            }

            // Finally, run the selection routine to yield the final element.
            try
            {
                return _resultSelector(accumulator);
            }
            catch (Exception ex)
            {
                // We need to wrap all exceptions into an aggregate.
                throw new AggregateException(ex);
            }
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TIntermediate> Open(QuerySettings settings, bool preferStriping)
        {
            // We just open the child operator.
            QueryResults<TInput> childQueryResults = Child.Open(settings, preferStriping);
            return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
        }

        internal override void WrapPartitionedStream<TKey>(
            PartitionedStream<TInput, TKey> inputStream, IPartitionedStreamRecipient<TIntermediate> recipient,
            bool preferStriping, QuerySettings settings)
        {
            int partitionCount = inputStream.PartitionCount;
            PartitionedStream<TIntermediate, int> outputStream = new PartitionedStream<TIntermediate, int>(
                partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Correct);

            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new AssociativeAggregationOperatorEnumerator<TKey>(inputStream[i], this, i, settings.CancellationState.MergedCancellationToken);
            }

            recipient.Receive(outputStream);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        [ExcludeFromCodeCoverage(Justification = "This method should never be called. Associative aggregation can always be parallelized")]
        internal override IEnumerable<TIntermediate> AsSequentialQuery(CancellationToken token)
        {
            Debug.Fail("This method should never be called. Associative aggregation can always be parallelized.");
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
        // This enumerator type encapsulates the intermediary aggregation over the underlying
        // (possibly partitioned) data source.
        //

        private sealed class AssociativeAggregationOperatorEnumerator<TKey> : QueryOperatorEnumerator<TIntermediate, int>
        {
            private readonly QueryOperatorEnumerator<TInput, TKey> _source; // The source data.
            private readonly AssociativeAggregationOperator<TInput, TIntermediate, TOutput> _reduceOperator; // The operator.
            private readonly int _partitionIndex; // The index of this partition.
            private readonly CancellationToken _cancellationToken;
            private bool _accumulated; // Whether we've accumulated already. (false-sharing risk, but only written once)


            //---------------------------------------------------------------------------------------
            // Instantiates a new aggregation operator.
            //

            internal AssociativeAggregationOperatorEnumerator(QueryOperatorEnumerator<TInput, TKey> source,
                                                              AssociativeAggregationOperator<TInput, TIntermediate, TOutput> reduceOperator, int partitionIndex,
                                                              CancellationToken cancellationToken)
            {
                Debug.Assert(source != null);
                Debug.Assert(reduceOperator != null);

                _source = source;
                _reduceOperator = reduceOperator;
                _partitionIndex = partitionIndex;
                _cancellationToken = cancellationToken;
            }

            //---------------------------------------------------------------------------------------
            // This API, upon the first time calling it, walks the entire source query tree. It begins
            // with an accumulator value set to the aggregation operator's seed, and always passes
            // the accumulator along with the current element from the data source to the binary
            // intermediary aggregation operator. The return value is kept in the accumulator. At
            // the end, we will have our intermediate result, ready for final aggregation.
            //

            internal override bool MoveNext([MaybeNullWhen(false), AllowNull] ref TIntermediate currentElement, ref int currentKey)
            {
                Debug.Assert(_reduceOperator != null);
                Debug.Assert(_reduceOperator._intermediateReduce != null, "expected a compiled operator");

                // Only produce a single element.  Return false if MoveNext() was already called before.
                if (_accumulated)
                {
                    return false;
                }
                _accumulated = true;

                bool hadNext = false;
                TIntermediate accumulator = default(TIntermediate)!;

                // Initialize the accumulator.
                if (_reduceOperator._seedIsSpecified)
                {
                    // If the seed is specified, initialize accumulator to the seed value.
                    accumulator = _reduceOperator._seedFactory == null
                                      ? _reduceOperator._seed
                                      : _reduceOperator._seedFactory();
                }
                else
                {
                    // If the seed is not specified, then we take the first element as the seed.
                    // Seed may be unspecified only if TInput is the same as TIntermediate.
                    Debug.Assert(typeof(TInput) == typeof(TIntermediate));

                    TInput acc = default(TInput)!;
                    TKey accKeyUnused = default(TKey)!;
                    if (!_source.MoveNext(ref acc!, ref accKeyUnused)) return false;
                    hadNext = true;
                    accumulator = (TIntermediate)((object?)acc!);
                }

                // Scan through the source and accumulate the result.
                TInput input = default(TInput)!;
                TKey keyUnused = default(TKey)!;
                int i = 0;
                while (_source.MoveNext(ref input!, ref keyUnused))
                {
                    if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                        _cancellationToken.ThrowIfCancellationRequested();
                    hadNext = true;
                    accumulator = _reduceOperator._intermediateReduce(accumulator, input);
                }

                if (hadNext)
                {
                    currentElement = accumulator;
                    currentKey = _partitionIndex; // A reduction's "index" is just its partition number.
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
