// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// BinaryQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq.Parallel
{
    /// <summary>
    /// The base class from which all binary query operators derive, that is, those that
    /// have two child operators. This introduces some convenience methods for those
    /// classes, as well as any state common to all subclasses. 
    /// </summary>
    /// <typeparam name="TLeftInput"></typeparam>
    /// <typeparam name="TRightInput"></typeparam>
    /// <typeparam name="TOutput"></typeparam>
    internal abstract class BinaryQueryOperator<TLeftInput, TRightInput, TOutput> : QueryOperator<TOutput>
    {
        // A set of child operators for the current node.
        private readonly QueryOperator<TLeftInput> _leftChild;
        private readonly QueryOperator<TRightInput> _rightChild;
        private OrdinalIndexState _indexState = OrdinalIndexState.Shuffled;

        //---------------------------------------------------------------------------------------
        // Stores a set of child operators on this query node.
        //

        internal BinaryQueryOperator(ParallelQuery<TLeftInput> leftChild, ParallelQuery<TRightInput> rightChild)
            : this(QueryOperator<TLeftInput>.AsQueryOperator(leftChild), QueryOperator<TRightInput>.AsQueryOperator(rightChild))
        {
        }

        internal BinaryQueryOperator(QueryOperator<TLeftInput> leftChild, QueryOperator<TRightInput> rightChild)
            : base(false, leftChild.SpecifiedQuerySettings.Merge(rightChild.SpecifiedQuerySettings))
        {
            Debug.Assert(leftChild != null && rightChild != null);
            _leftChild = leftChild;
            _rightChild = rightChild;
        }

        internal QueryOperator<TLeftInput> LeftChild
        {
            get { return _leftChild; }
        }

        internal QueryOperator<TRightInput> RightChild
        {
            get { return _rightChild; }
        }

        internal override sealed OrdinalIndexState OrdinalIndexState
        {
            get { return _indexState; }
        }

        protected void SetOrdinalIndex(OrdinalIndexState indexState)
        {
            _indexState = indexState;
        }


        //---------------------------------------------------------------------------------------
        // This method wraps accepts two child partitioned streams, and constructs an output
        // partitioned stream. However, instead of returning the transformed partitioned
        // stream, we pass it to a recipient object by calling recipient.Give<TNewKey>(..). That
        // way, we can "return" a partitioned stream that uses an order key selected by the operator.
        //
        public abstract void WrapPartitionedStream<TLeftKey, TRightKey>(
            PartitionedStream<TLeftInput, TLeftKey> leftPartitionedStream, PartitionedStream<TRightInput, TRightKey> rightPartitionedStream,
            IPartitionedStreamRecipient<TOutput> outputRecipient, bool preferStriping, QuerySettings settings);

        //---------------------------------------------------------------------------------------
        // Implementation of QueryResults for a binary operator. The results will not be indexable
        // unless a derived class provides that functionality.        
        //

        internal class BinaryQueryOperatorResults : QueryResults<TOutput>
        {
            protected QueryResults<TLeftInput> _leftChildQueryResults; // Results of the left child query
            protected QueryResults<TRightInput> _rightChildQueryResults; // Results of the right child query
            private BinaryQueryOperator<TLeftInput, TRightInput, TOutput> _op; // Operator that generated these results
            private QuerySettings _settings; // Settings collected from the query
            private bool _preferStriping; // If the results are indexable, should we use striping when partitioning them

            internal BinaryQueryOperatorResults(
                QueryResults<TLeftInput> leftChildQueryResults, QueryResults<TRightInput> rightChildQueryResults,
                BinaryQueryOperator<TLeftInput, TRightInput, TOutput> op, QuerySettings settings,
                bool preferStriping)
            {
                _leftChildQueryResults = leftChildQueryResults;
                _rightChildQueryResults = rightChildQueryResults;
                _op = op;
                _settings = settings;
                _preferStriping = preferStriping;
            }

            internal override void GivePartitionedStream(IPartitionedStreamRecipient<TOutput> recipient)
            {
                Debug.Assert(IsIndexible == (_op.OrdinalIndexState == OrdinalIndexState.Indexable));

                if (_settings.ExecutionMode.Value == ParallelExecutionMode.Default && _op.LimitsParallelism)
                {
                    // We need to run the query sequentially up to and including this operator
                    IEnumerable<TOutput> opSequential = _op.AsSequentialQuery(_settings.CancellationState.ExternalCancellationToken);
                    PartitionedStream<TOutput, int> result = ExchangeUtilities.PartitionDataSource(
                        opSequential, _settings.DegreeOfParallelism.Value, _preferStriping);
                    recipient.Receive<int>(result);
                }
                else if (IsIndexible)
                {
                    // The output of this operator is indexable. Pass the partitioned output into the IPartitionedStreamRecipient.
                    PartitionedStream<TOutput, int> result = ExchangeUtilities.PartitionDataSource(this, _settings.DegreeOfParallelism.Value, _preferStriping);
                    recipient.Receive<int>(result);
                }
                else
                {
                    // The common case: get partitions from the child and wrap each partition.
                    _leftChildQueryResults.GivePartitionedStream(new LeftChildResultsRecipient(recipient, this, _preferStriping, _settings));
                }
            }

            //---------------------------------------------------------------------------------------
            // LeftChildResultsRecipient is a recipient of a partitioned stream. It receives a partitioned
            // stream from the left child operator, and passes the results along to a
            // RightChildResultsRecipient.
            //

            private class LeftChildResultsRecipient : IPartitionedStreamRecipient<TLeftInput>
            {
                private IPartitionedStreamRecipient<TOutput> _outputRecipient;
                private BinaryQueryOperatorResults _results;
                private bool _preferStriping;
                private QuerySettings _settings;

                internal LeftChildResultsRecipient(IPartitionedStreamRecipient<TOutput> outputRecipient, BinaryQueryOperatorResults results,
                                                   bool preferStriping, QuerySettings settings)
                {
                    _outputRecipient = outputRecipient;
                    _results = results;
                    _preferStriping = preferStriping;
                    _settings = settings;
                }

                public void Receive<TLeftKey>(PartitionedStream<TLeftInput, TLeftKey> source)
                {
                    RightChildResultsRecipient<TLeftKey> rightChildRecipient =
                        new RightChildResultsRecipient<TLeftKey>(_outputRecipient, _results._op, source, _preferStriping, _settings);
                    _results._rightChildQueryResults.GivePartitionedStream(rightChildRecipient);
                }
            }

            //---------------------------------------------------------------------------------------
            // RightChildResultsRecipient receives a partitioned from the right child operator. Also,
            // the partitioned stream from the left child operator is passed into the constructor.
            // So, Receive has partitioned streams for both child operators, and also is called in
            // a context where it has access to both TLeftKey and TRightKey. Then, it passes both
            // streams (as arguments) and key types (as type arguments) to the operator's
            // WrapPartitionedStream method.
            //

            private class RightChildResultsRecipient<TLeftKey> : IPartitionedStreamRecipient<TRightInput>
            {
                private IPartitionedStreamRecipient<TOutput> _outputRecipient;
                private PartitionedStream<TLeftInput, TLeftKey> _leftPartitionedStream;
                private BinaryQueryOperator<TLeftInput, TRightInput, TOutput> _op;
                private bool _preferStriping;
                private QuerySettings _settings;

                internal RightChildResultsRecipient(
                    IPartitionedStreamRecipient<TOutput> outputRecipient, BinaryQueryOperator<TLeftInput, TRightInput, TOutput> op,
                    PartitionedStream<TLeftInput, TLeftKey> leftPartitionedStream, bool preferStriping, QuerySettings settings)
                {
                    _outputRecipient = outputRecipient;
                    _op = op;
                    _preferStriping = preferStriping;
                    _leftPartitionedStream = leftPartitionedStream;
                    _settings = settings;
                }

                public void Receive<TRightKey>(PartitionedStream<TRightInput, TRightKey> rightPartitionedStream)
                {
                    _op.WrapPartitionedStream(_leftPartitionedStream, rightPartitionedStream, _outputRecipient, _preferStriping, _settings);
                }
            }
        }
    }
}
