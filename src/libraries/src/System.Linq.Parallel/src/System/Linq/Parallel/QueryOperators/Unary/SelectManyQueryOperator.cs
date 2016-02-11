// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// SelectManyQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// SelectMany is effectively a nested loops join. It is given two data sources, an
    /// outer and an inner -- actually, the inner is sometimes calculated by invoking a
    /// function for each outer element -- and we walk the outer, walking the entire
    /// inner enumerator for each outer element. There is an optional result selector
    /// function which can transform the output before yielding it as a result element.
    ///
    /// Notes:
    ///     Although select many takes two enumerable objects as input, it appears to the
    ///     query analysis infrastructure as a unary operator. That's because it works a
    ///     little differently than the other binary operators: it has to re-open the right
    ///     child every time an outer element is walked. The right child is NOT partitioned. 
    /// </summary>
    /// <typeparam name="TLeftInput"></typeparam>
    /// <typeparam name="TRightInput"></typeparam>
    /// <typeparam name="TOutput"></typeparam>
    internal sealed class SelectManyQueryOperator<TLeftInput, TRightInput, TOutput> : UnaryQueryOperator<TLeftInput, TOutput>
    {
        private readonly Func<TLeftInput, IEnumerable<TRightInput>> _rightChildSelector; // To select a new child each iteration.
        private readonly Func<TLeftInput, int, IEnumerable<TRightInput>> _indexedRightChildSelector; // To select a new child each iteration.
        private readonly Func<TLeftInput, TRightInput, TOutput> _resultSelector; // An optional result selection function.
        private bool _prematureMerge = false; // Whether to prematurely merge the input of this operator.
        private bool _limitsParallelism = false; // Whether to prematurely merge the input of this operator.

        //---------------------------------------------------------------------------------------
        // Initializes a new select-many operator.
        //
        // Arguments:
        //    leftChild             - the left data source from which to pull data.
        //    rightChild            - the right data source from which to pull data.
        //    rightChildSelector    - if no right data source was supplied, the selector function
        //                            will generate a new right child for every unique left element.
        //    resultSelector        - a selection function for creating output elements.
        //

        internal SelectManyQueryOperator(IEnumerable<TLeftInput> leftChild,
                                         Func<TLeftInput, IEnumerable<TRightInput>> rightChildSelector,
                                         Func<TLeftInput, int, IEnumerable<TRightInput>> indexedRightChildSelector,
                                         Func<TLeftInput, TRightInput, TOutput> resultSelector)
            : base(leftChild)
        {
            Debug.Assert(leftChild != null, "left child data source cannot be null");
            Debug.Assert(rightChildSelector != null || indexedRightChildSelector != null,
                            "either right child data or selector must be supplied");
            Debug.Assert(rightChildSelector == null || indexedRightChildSelector == null,
                            "either indexed- or non-indexed child selector must be supplied (not both)");
            Debug.Assert(typeof(TRightInput) == typeof(TOutput) || resultSelector != null,
                            "right input and output must be the same types, otherwise the result selector may not be null");

            _rightChildSelector = rightChildSelector;
            _indexedRightChildSelector = indexedRightChildSelector;
            _resultSelector = resultSelector;

            // If the SelectMany is indexed, elements must be returned in the order in which
            // indices were assigned.
            _outputOrdered = Child.OutputOrdered || indexedRightChildSelector != null;

            InitOrderIndex();
        }

        private void InitOrderIndex()
        {
            OrdinalIndexState childIndexState = Child.OrdinalIndexState;

            if (_indexedRightChildSelector != null)
            {
                // If this is an indexed SelectMany, we need the order keys to be Correct, so that we can pass them
                // into the user delegate.

                _prematureMerge = ExchangeUtilities.IsWorseThan(childIndexState, OrdinalIndexState.Correct);
                _limitsParallelism = _prematureMerge && childIndexState != OrdinalIndexState.Shuffled;
            }
            else
            {
                if (OutputOrdered)
                {
                    // If the output of this SelectMany is ordered, the input keys must be at least increasing. The
                    // SelectMany algorithm assumes that there will be no duplicate order keys, so if the order keys
                    // are Shuffled, we need to merge prematurely.
                    _prematureMerge = ExchangeUtilities.IsWorseThan(childIndexState, OrdinalIndexState.Increasing);
                }
            }

            SetOrdinalIndexState(OrdinalIndexState.Increasing);
        }

        internal override void WrapPartitionedStream<TLeftKey>(
            PartitionedStream<TLeftInput, TLeftKey> inputStream, IPartitionedStreamRecipient<TOutput> recipient, bool preferStriping, QuerySettings settings)
        {
            int partitionCount = inputStream.PartitionCount;

            if (_indexedRightChildSelector != null)
            {
                PartitionedStream<TLeftInput, int> inputStreamInt;

                // If the index is not correct, we need to reindex.
                if (_prematureMerge)
                {
                    ListQueryResults<TLeftInput> listResults =
                        QueryOperator<TLeftInput>.ExecuteAndCollectResults(inputStream, partitionCount, OutputOrdered, preferStriping, settings);
                    inputStreamInt = listResults.GetPartitionedStream();
                }
                else
                {
                    inputStreamInt = (PartitionedStream<TLeftInput, int>)(object)inputStream;
                }
                WrapPartitionedStreamIndexed(inputStreamInt, recipient, settings);
                return;
            }

            //
            // 
            if (_prematureMerge)
            {
                PartitionedStream<TLeftInput, int> inputStreamInt =
                    QueryOperator<TLeftInput>.ExecuteAndCollectResults(inputStream, partitionCount, OutputOrdered, preferStriping, settings)
                    .GetPartitionedStream();
                WrapPartitionedStreamNotIndexed(inputStreamInt, recipient, settings);
            }
            else
            {
                WrapPartitionedStreamNotIndexed(inputStream, recipient, settings);
            }
        }

        /// <summary>
        /// A helper method for WrapPartitionedStream. We use the helper to reuse a block of code twice, but with
        /// a different order key type. (If premature merge occurred, the order key type will be "int". Otherwise, 
        /// it will be the same type as "TLeftKey" in WrapPartitionedStream.)
        /// </summary>
        private void WrapPartitionedStreamNotIndexed<TLeftKey>(
            PartitionedStream<TLeftInput, TLeftKey> inputStream, IPartitionedStreamRecipient<TOutput> recipient, QuerySettings settings)
        {
            int partitionCount = inputStream.PartitionCount;
            var keyComparer = new PairComparer<TLeftKey, int>(inputStream.KeyComparer, Util.GetDefaultComparer<int>());
            var outputStream = new PartitionedStream<TOutput, Pair<TLeftKey, int>>(partitionCount, keyComparer, OrdinalIndexState);
            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new SelectManyQueryOperatorEnumerator<TLeftKey>(inputStream[i], this, settings.CancellationState.MergedCancellationToken);
            }

            recipient.Receive(outputStream);
        }

        /// <summary>
        /// Similar helper method to WrapPartitionedStreamNotIndexed, except that this one is for the indexed variant
        /// of SelectMany (i.e., the SelectMany that passes indices into the user sequence-generating delegate)
        /// </summary>
        private void WrapPartitionedStreamIndexed(
            PartitionedStream<TLeftInput, int> inputStream, IPartitionedStreamRecipient<TOutput> recipient, QuerySettings settings)
        {
            var keyComparer = new PairComparer<int, int>(inputStream.KeyComparer, Util.GetDefaultComparer<int>());

            var outputStream = new PartitionedStream<TOutput, Pair<int, int>>(inputStream.PartitionCount, keyComparer, OrdinalIndexState);

            for (int i = 0; i < inputStream.PartitionCount; i++)
            {
                outputStream[i] = new IndexedSelectManyQueryOperatorEnumerator(inputStream[i], this, settings.CancellationState.MergedCancellationToken);
            }

            recipient.Receive(outputStream);
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the left child and wrapping with a
        // partition if needed. The right child is not opened yet -- this is always done on demand
        // as the outer elements are enumerated.
        //

        internal override QueryResults<TOutput> Open(QuerySettings settings, bool preferStriping)
        {
            QueryResults<TLeftInput> childQueryResults = Child.Open(settings, preferStriping);
            return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TOutput> AsSequentialQuery(CancellationToken token)
        {
            if (_rightChildSelector != null)
            {
                if (_resultSelector != null)
                {
                    return CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token).SelectMany(_rightChildSelector, _resultSelector);
                }
                return (IEnumerable<TOutput>)CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token).SelectMany(_rightChildSelector);
            }
            else
            {
                Debug.Assert(_indexedRightChildSelector != null);
                if (_resultSelector != null)
                {
                    return CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token).SelectMany(_indexedRightChildSelector, _resultSelector);
                }

                return (IEnumerable<TOutput>)CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token).SelectMany(_indexedRightChildSelector);
            }
        }


        //---------------------------------------------------------------------------------------
        // Whether this operator performs a premature merge that would not be performed in
        // a similar sequential operation (i.e., in LINQ to Objects).
        //

        internal override bool LimitsParallelism
        {
            get { return _limitsParallelism; }
        }

        //---------------------------------------------------------------------------------------
        // The enumerator type responsible for executing the SelectMany logic.
        //

        class IndexedSelectManyQueryOperatorEnumerator : QueryOperatorEnumerator<TOutput, Pair<int, int>>
        {
            private readonly QueryOperatorEnumerator<TLeftInput, int> _leftSource; // The left data source to enumerate.
            private readonly SelectManyQueryOperator<TLeftInput, TRightInput, TOutput> _selectManyOperator; // The select many operator to use.
            private IEnumerator<TRightInput> _currentRightSource; // The current enumerator we're using.
            private IEnumerator<TOutput> _currentRightSourceAsOutput; // If we need to access the enumerator for output directly (no result selector).
            private Mutables _mutables; // bag of frequently mutated value types [allocate in moveNext to avoid false-sharing]
            private readonly CancellationToken _cancellationToken;

            private class Mutables
            {
                internal int _currentRightSourceIndex = -1; // The index for the right data source.
                internal TLeftInput _currentLeftElement; // The current element in the left data source.
                internal int _currentLeftSourceIndex; // The current key in the left data source.
                internal int _lhsCount; //counts the number of lhs elements enumerated. used for cancellation testing. 
            }


            //---------------------------------------------------------------------------------------
            // Instantiates a new select-many enumerator. Notice that the right data source is an
            // enumera*BLE* not an enumera*TOR*. It is re-opened for every single element in the left
            // data source.
            //

            internal IndexedSelectManyQueryOperatorEnumerator(QueryOperatorEnumerator<TLeftInput, int> leftSource,
                                                              SelectManyQueryOperator<TLeftInput, TRightInput, TOutput> selectManyOperator,
                CancellationToken cancellationToken)
            {
                Debug.Assert(leftSource != null);
                Debug.Assert(selectManyOperator != null);

                _leftSource = leftSource;
                _selectManyOperator = selectManyOperator;
                _cancellationToken = cancellationToken;
            }

            //---------------------------------------------------------------------------------------
            // Straightforward IEnumerator<T> methods.
            //

            internal override bool MoveNext(ref TOutput currentElement, ref Pair<int, int> currentKey)
            {
                while (true)
                {
                    if (_currentRightSource == null)
                    {
                        _mutables = new Mutables();

                        // Check cancellation every few lhs-enumerations in case none of them are producing
                        // any outputs.  Otherwise, we rely on the consumer of this operator to be performing the checks.
                        if ((_mutables._lhsCount++ & CancellationState.POLL_INTERVAL) == 0)
                            CancellationState.ThrowIfCanceled(_cancellationToken);

                        // We don't have a "current" right enumerator to use. We have to fetch the next
                        // one. If the left has run out of elements, however, we're done and just return
                        // false right away.
                        if (!_leftSource.MoveNext(ref _mutables._currentLeftElement, ref _mutables._currentLeftSourceIndex))
                        {
                            return false;
                        }

                        // Use the source selection routine to create a right child.
                        IEnumerable<TRightInput> rightChild =
                            _selectManyOperator._indexedRightChildSelector(_mutables._currentLeftElement, _mutables._currentLeftSourceIndex);

                        Debug.Assert(rightChild != null);
                        _currentRightSource = rightChild.GetEnumerator();

                        Debug.Assert(_currentRightSource != null);

                        // If we have no result selector, we will need to access the Current element of the right
                        // data source as though it is a TOutput. Unfortunately, we know that TRightInput must
                        // equal TOutput (we check it during operator construction), but the type system doesn't.
                        // Thus we would have to cast the result of invoking Current from type TRightInput to
                        // TOutput. This is no good, since the results could be value types. Instead, we save the
                        // enumerator object as an IEnumerator<TOutput> and access that later on.
                        if (_selectManyOperator._resultSelector == null)
                        {
                            _currentRightSourceAsOutput = (IEnumerator<TOutput>)_currentRightSource;
                            Debug.Assert(_currentRightSourceAsOutput == _currentRightSource,
                                            "these must be equal, otherwise the surrounding logic will be broken");
                        }
                    }

                    if (_currentRightSource.MoveNext())
                    {
                        _mutables._currentRightSourceIndex++;

                        // If the inner data source has an element, we can yield it.
                        if (_selectManyOperator._resultSelector != null)
                        {
                            // In the case of a selection function, use that to yield the next element.
                            currentElement = _selectManyOperator._resultSelector(_mutables._currentLeftElement, _currentRightSource.Current);
                        }
                        else
                        {
                            // Otherwise, the right input and output types must be the same. We use the
                            // casted copy of the current right source and just return its current element.
                            Debug.Assert(_currentRightSourceAsOutput != null);
                            currentElement = _currentRightSourceAsOutput.Current;
                        }
                        currentKey = new Pair<int, int>(_mutables._currentLeftSourceIndex, _mutables._currentRightSourceIndex);

                        return true;
                    }
                    else
                    {
                        // Otherwise, we have exhausted the right data source. Loop back around and try
                        // to get the next left element, then its right, and so on.
                        _currentRightSource.Dispose();
                        _currentRightSource = null;
                        _currentRightSourceAsOutput = null;
                    }
                }
            }

            protected override void Dispose(bool disposing)
            {
                _leftSource.Dispose();
                if (_currentRightSource != null)
                {
                    _currentRightSource.Dispose();
                }
            }
        }

        //---------------------------------------------------------------------------------------
        // The enumerator type responsible for executing the SelectMany logic.
        //

        class SelectManyQueryOperatorEnumerator<TLeftKey> : QueryOperatorEnumerator<TOutput, Pair<TLeftKey, int>>
        {
            private readonly QueryOperatorEnumerator<TLeftInput, TLeftKey> _leftSource; // The left data source to enumerate.
            private readonly SelectManyQueryOperator<TLeftInput, TRightInput, TOutput> _selectManyOperator; // The select many operator to use.
            private IEnumerator<TRightInput> _currentRightSource; // The current enumerator we're using.
            private IEnumerator<TOutput> _currentRightSourceAsOutput; // If we need to access the enumerator for output directly (no result selector).
            private Mutables _mutables; // bag of frequently mutated value types [allocate in moveNext to avoid false-sharing]
            private readonly CancellationToken _cancellationToken;

            private class Mutables
            {
                internal int _currentRightSourceIndex = -1; // The index for the right data source.
                internal TLeftInput _currentLeftElement; // The current element in the left data source.
                internal TLeftKey _currentLeftKey; // The current key in the left data source.
                internal int _lhsCount; // Counts the number of lhs elements enumerated. used for cancellation testing. 
            }


            //---------------------------------------------------------------------------------------
            // Instantiates a new select-many enumerator. Notice that the right data source is an
            // enumera*BLE* not an enumera*TOR*. It is re-opened for every single element in the left
            // data source.
            //

            internal SelectManyQueryOperatorEnumerator(QueryOperatorEnumerator<TLeftInput, TLeftKey> leftSource,
                                                       SelectManyQueryOperator<TLeftInput, TRightInput, TOutput> selectManyOperator,
                                                       CancellationToken cancellationToken)
            {
                Debug.Assert(leftSource != null);
                Debug.Assert(selectManyOperator != null);

                _leftSource = leftSource;
                _selectManyOperator = selectManyOperator;
                _cancellationToken = cancellationToken;
            }

            //---------------------------------------------------------------------------------------
            // Straightforward IEnumerator<T> methods.
            //

            internal override bool MoveNext(ref TOutput currentElement, ref Pair<TLeftKey, int> currentKey)
            {
                while (true)
                {
                    if (_currentRightSource == null)
                    {
                        _mutables = new Mutables();

                        // Check cancellation every few lhs-enumerations in case none of them are producing
                        // any outputs.  Otherwise, we rely on the consumer of this operator to be performing the checks.
                        if ((_mutables._lhsCount++ & CancellationState.POLL_INTERVAL) == 0)
                            CancellationState.ThrowIfCanceled(_cancellationToken);

                        // We don't have a "current" right enumerator to use. We have to fetch the next
                        // one. If the left has run out of elements, however, we're done and just return
                        // false right away.

                        if (!_leftSource.MoveNext(ref _mutables._currentLeftElement, ref _mutables._currentLeftKey))
                        {
                            return false;
                        }

                        // Use the source selection routine to create a right child.
                        IEnumerable<TRightInput> rightChild = _selectManyOperator._rightChildSelector(_mutables._currentLeftElement);

                        Debug.Assert(rightChild != null);
                        _currentRightSource = rightChild.GetEnumerator();

                        Debug.Assert(_currentRightSource != null);

                        // If we have no result selector, we will need to access the Current element of the right
                        // data source as though it is a TOutput. Unfortunately, we know that TRightInput must
                        // equal TOutput (we check it during operator construction), but the type system doesn't.
                        // Thus we would have to cast the result of invoking Current from type TRightInput to
                        // TOutput. This is no good, since the results could be value types. Instead, we save the
                        // enumerator object as an IEnumerator<TOutput> and access that later on.
                        if (_selectManyOperator._resultSelector == null)
                        {
                            _currentRightSourceAsOutput = (IEnumerator<TOutput>)_currentRightSource;
                            Debug.Assert(_currentRightSourceAsOutput == _currentRightSource,
                                            "these must be equal, otherwise the surrounding logic will be broken");
                        }
                    }

                    if (_currentRightSource.MoveNext())
                    {
                        _mutables._currentRightSourceIndex++;

                        // If the inner data source has an element, we can yield it.
                        if (_selectManyOperator._resultSelector != null)
                        {
                            // In the case of a selection function, use that to yield the next element.
                            currentElement = _selectManyOperator._resultSelector(_mutables._currentLeftElement, _currentRightSource.Current);
                        }
                        else
                        {
                            // Otherwise, the right input and output types must be the same. We use the
                            // casted copy of the current right source and just return its current element.
                            Debug.Assert(_currentRightSourceAsOutput != null);
                            currentElement = _currentRightSourceAsOutput.Current;
                        }
                        currentKey = new Pair<TLeftKey, int>(_mutables._currentLeftKey, _mutables._currentRightSourceIndex);

                        return true;
                    }
                    else
                    {
                        // Otherwise, we have exhausted the right data source. Loop back around and try
                        // to get the next left element, then its right, and so on.
                        _currentRightSource.Dispose();
                        _currentRightSource = null;
                        _currentRightSourceAsOutput = null;
                    }
                }
            }

            protected override void Dispose(bool disposing)
            {
                _leftSource.Dispose();
                if (_currentRightSource != null)
                {
                    _currentRightSource.Dispose();
                }
            }
        }
    }
}
