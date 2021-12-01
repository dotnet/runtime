// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// WhereQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// The operator type for Where statements. This operator filters out elements that
    /// don't match a filter function (supplied at instantiation time).
    /// </summary>
    /// <typeparam name="TInputOutput"></typeparam>
    internal sealed class WhereQueryOperator<TInputOutput> : UnaryQueryOperator<TInputOutput, TInputOutput>
    {
        // Predicate function. Used to filter out non-matching elements during execution.
        private readonly Func<TInputOutput, bool> _predicate;

        //---------------------------------------------------------------------------------------
        // Initializes a new where operator.
        //
        // Arguments:
        //    child         - the child operator or data source from which to pull data
        //    predicate     - a delegate representing the predicate function
        //
        // Assumptions:
        //    predicate must be non null.
        //

        internal WhereQueryOperator(IEnumerable<TInputOutput> child, Func<TInputOutput, bool> predicate)
            : base(child)
        {
            Debug.Assert(child != null, "child data source cannot be null");
            Debug.Assert(predicate != null, "need a filter function");

            SetOrdinalIndexState(
                ExchangeUtilities.Worse(Child.OrdinalIndexState, OrdinalIndexState.Increasing));

            _predicate = predicate;
        }

        internal override void WrapPartitionedStream<TKey>(
            PartitionedStream<TInputOutput, TKey> inputStream, IPartitionedStreamRecipient<TInputOutput> recipient, bool preferStriping, QuerySettings settings)
        {
            PartitionedStream<TInputOutput, TKey> outputStream = new PartitionedStream<TInputOutput, TKey>(
                inputStream.PartitionCount, inputStream.KeyComparer, OrdinalIndexState);
            for (int i = 0; i < inputStream.PartitionCount; i++)
            {
                outputStream[i] = new WhereQueryOperatorEnumerator<TKey>(inputStream[i], _predicate,
                    settings.CancellationState.MergedCancellationToken);
            }

            recipient.Receive(outputStream);
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TInputOutput> Open(QuerySettings settings, bool preferStriping)
        {
            // We just open the child operator.
            QueryResults<TInputOutput> childQueryResults = Child.Open(settings, preferStriping);

            // And then return the query results
            return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TInputOutput> AsSequentialQuery(CancellationToken token)
        {
            IEnumerable<TInputOutput> wrappedChild = CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token);
            return wrappedChild.Where(_predicate);
        }

        //---------------------------------------------------------------------------------------
        // Whether this operator performs a premature merge that would not be performed in
        // a similar sequential operation (i.e., in LINQ to Objects).
        //

        internal override bool LimitsParallelism
        {
            get { return false; }
        }

        //-----------------------------------------------------------------------------------
        // An enumerator that implements the filtering logic.
        //

        private sealed class WhereQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TInputOutput, TKey>
        {
            private readonly QueryOperatorEnumerator<TInputOutput, TKey> _source; // The data source to enumerate.
            private readonly Func<TInputOutput, bool> _predicate; // The predicate used for filtering.
            private readonly CancellationToken _cancellationToken;
            private Shared<int>? _outputLoopCount;

            //-----------------------------------------------------------------------------------
            // Instantiates a new enumerator.
            //

            internal WhereQueryOperatorEnumerator(QueryOperatorEnumerator<TInputOutput, TKey> source, Func<TInputOutput, bool> predicate,
                CancellationToken cancellationToken)
            {
                Debug.Assert(source != null);
                Debug.Assert(predicate != null);

                _source = source;
                _predicate = predicate;
                _cancellationToken = cancellationToken;
            }

            //-----------------------------------------------------------------------------------
            // Moves to the next matching element in the underlying data stream.
            //

            internal override bool MoveNext([MaybeNullWhen(false), AllowNull] ref TInputOutput currentElement, [AllowNull] ref TKey currentKey)
            {
                Debug.Assert(_predicate != null, "expected a compiled operator");

                // Iterate through the input until we reach the end of the sequence or find
                // an element matching the predicate.

                if (_outputLoopCount == null)
                    _outputLoopCount = new Shared<int>(0);

                while (_source.MoveNext(ref currentElement!, ref currentKey))
                {
                    if ((_outputLoopCount.Value++ & CancellationState.POLL_INTERVAL) == 0)
                        _cancellationToken.ThrowIfCancellationRequested();

                    if (_predicate(currentElement))
                    {
                        return true;
                    }
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
