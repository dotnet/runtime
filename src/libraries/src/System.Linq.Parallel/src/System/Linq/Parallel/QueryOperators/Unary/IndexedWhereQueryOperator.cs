// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// IndexedWhereQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A variant of the Where operator that supplies element index while performing the
    /// filtering operation. This requires cooperation with partitioning and merging to
    /// guarantee ordering is preserved.
    ///
    /// </summary>
    /// <typeparam name="TInputOutput"></typeparam>
    internal sealed class IndexedWhereQueryOperator<TInputOutput> : UnaryQueryOperator<TInputOutput, TInputOutput>
    {
        // Predicate function. Used to filter out non-matching elements during execution.
        private Func<TInputOutput, int, bool> _predicate;
        private bool _prematureMerge = false; // Whether to prematurely merge the input of this operator.
        private bool _limitsParallelism = false; // Whether this operator limits parallelism

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

        internal IndexedWhereQueryOperator(IEnumerable<TInputOutput> child,
                                           Func<TInputOutput, int, bool> predicate)
            : base(child)
        {
            Debug.Assert(child != null, "child data source cannot be null");
            Debug.Assert(predicate != null, "need a filter function");

            _predicate = predicate;

            // In an indexed Select, elements must be returned in the order in which
            // indices were assigned.
            _outputOrdered = true;

            InitOrdinalIndexState();
        }

        private void InitOrdinalIndexState()
        {
            OrdinalIndexState childIndexState = Child.OrdinalIndexState;
            if (ExchangeUtilities.IsWorseThan(childIndexState, OrdinalIndexState.Correct))
            {
                _prematureMerge = true;
                _limitsParallelism = childIndexState != OrdinalIndexState.Shuffled;
            }

            SetOrdinalIndexState(OrdinalIndexState.Increasing);
        }


        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TInputOutput> Open(
            QuerySettings settings, bool preferStriping)
        {
            QueryResults<TInputOutput> childQueryResults = Child.Open(settings, preferStriping);
            return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
        }

        internal override void WrapPartitionedStream<TKey>(
            PartitionedStream<TInputOutput, TKey> inputStream, IPartitionedStreamRecipient<TInputOutput> recipient, bool preferStriping, QuerySettings settings)
        {
            int partitionCount = inputStream.PartitionCount;

            // If the index is not correct, we need to reindex.
            PartitionedStream<TInputOutput, int> inputStreamInt;
            if (_prematureMerge)
            {
                ListQueryResults<TInputOutput> listResults = ExecuteAndCollectResults(inputStream, partitionCount, Child.OutputOrdered, preferStriping, settings);
                inputStreamInt = listResults.GetPartitionedStream();
            }
            else
            {
                Debug.Assert(typeof(TKey) == typeof(int));
                inputStreamInt = (PartitionedStream<TInputOutput, int>)(object)inputStream;
            }

            // Since the index is correct, the type of the index must be int
            PartitionedStream<TInputOutput, int> outputStream =
                new PartitionedStream<TInputOutput, int>(partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState);

            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new IndexedWhereQueryOperatorEnumerator(inputStreamInt[i], _predicate, settings.CancellationState.MergedCancellationToken);
            }

            recipient.Receive(outputStream);
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
            get { return _limitsParallelism; }
        }

        //-----------------------------------------------------------------------------------
        // An enumerator that implements the filtering logic.
        //

        private class IndexedWhereQueryOperatorEnumerator : QueryOperatorEnumerator<TInputOutput, int>
        {
            private readonly QueryOperatorEnumerator<TInputOutput, int> _source; // The data source to enumerate.
            private readonly Func<TInputOutput, int, bool> _predicate; // The predicate used for filtering.
            private CancellationToken _cancellationToken;
            private Shared<int> _outputLoopCount;
            //-----------------------------------------------------------------------------------
            // Instantiates a new enumerator.
            //

            internal IndexedWhereQueryOperatorEnumerator(QueryOperatorEnumerator<TInputOutput, int> source, Func<TInputOutput, int, bool> predicate,
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

            internal override bool MoveNext(ref TInputOutput currentElement, ref int currentKey)
            {
                Debug.Assert(_predicate != null, "expected a compiled operator");

                // Iterate through the input until we reach the end of the sequence or find
                // an element matching the predicate.

                if (_outputLoopCount == null)
                    _outputLoopCount = new Shared<int>(0);

                while (_source.MoveNext(ref currentElement, ref currentKey))
                {
                    if ((_outputLoopCount.Value++ & CancellationState.POLL_INTERVAL) == 0)
                        CancellationState.ThrowIfCanceled(_cancellationToken);

                    if (_predicate(currentElement, currentKey))
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
