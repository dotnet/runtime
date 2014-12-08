// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// WhereQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
        private Func<TInputOutput, bool> _predicate;

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
            Contract.Assert(child != null, "child data source cannot be null");
            Contract.Assert(predicate != null, "need a filter function");

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

        private class WhereQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TInputOutput, TKey>
        {
            private readonly QueryOperatorEnumerator<TInputOutput, TKey> _source; // The data source to enumerate.
            private readonly Func<TInputOutput, bool> _predicate; // The predicate used for filtering.
            private CancellationToken _cancellationToken;
            private Shared<int> _outputLoopCount;

            //-----------------------------------------------------------------------------------
            // Instantiates a new enumerator.
            //

            internal WhereQueryOperatorEnumerator(QueryOperatorEnumerator<TInputOutput, TKey> source, Func<TInputOutput, bool> predicate,
                CancellationToken cancellationToken)
            {
                Contract.Assert(source != null);
                Contract.Assert(predicate != null);

                _source = source;
                _predicate = predicate;
                _cancellationToken = cancellationToken;
            }

            //-----------------------------------------------------------------------------------
            // Moves to the next matching element in the underlying data stream.
            //

            internal override bool MoveNext(ref TInputOutput currentElement, ref TKey currentKey)
            {
                Contract.Assert(_predicate != null, "expected a compiled operator");

                // Iterate through the input until we reach the end of the sequence or find
                // an element matching the predicate.

                if (_outputLoopCount == null)
                    _outputLoopCount = new Shared<int>(0);

                while (_source.MoveNext(ref currentElement, ref currentKey))
                {
                    if ((_outputLoopCount.Value++ & CancellationState.POLL_INTERVAL) == 0)
                        CancellationState.ThrowIfCanceled(_cancellationToken);

                    if (_predicate(currentElement))
                    {
                        return true;
                    }
                }
                return false;
            }

            protected override void Dispose(bool disposing)
            {
                Contract.Assert(_source != null);
                _source.Dispose();
            }
        }
    }
}