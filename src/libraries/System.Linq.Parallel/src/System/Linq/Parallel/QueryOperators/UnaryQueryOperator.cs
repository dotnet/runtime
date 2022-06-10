// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// UnaryQueryOperator.cs
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
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TOutput"></typeparam>
    internal abstract class UnaryQueryOperator<TInput, TOutput> : QueryOperator<TOutput>
    {
        // The single child operator for the current node.
        private readonly QueryOperator<TInput> _child;

        // The state of the order index of the output of this operator.
        private OrdinalIndexState _indexState = OrdinalIndexState.Shuffled;

        //---------------------------------------------------------------------------------------
        // Constructors
        //

        internal UnaryQueryOperator(IEnumerable<TInput> child)
            : this(QueryOperator<TInput>.AsQueryOperator(child))
        {
        }

        internal UnaryQueryOperator(IEnumerable<TInput> child, bool outputOrdered)
            : this(QueryOperator<TInput>.AsQueryOperator(child), outputOrdered)
        {
        }

        private UnaryQueryOperator(QueryOperator<TInput> child)
            : this(child, child.OutputOrdered, child.SpecifiedQuerySettings)
        {
        }

        internal UnaryQueryOperator(QueryOperator<TInput> child, bool outputOrdered)
            : this(child, outputOrdered, child.SpecifiedQuerySettings)
        {
        }

        private UnaryQueryOperator(QueryOperator<TInput> child, bool outputOrdered, QuerySettings settings)
            : base(outputOrdered, settings)
        {
            _child = child;
        }

        internal QueryOperator<TInput> Child
        {
            get { return _child; }
        }

        internal sealed override OrdinalIndexState OrdinalIndexState
        {
            get { return _indexState; }
        }

        protected void SetOrdinalIndexState(OrdinalIndexState indexState)
        {
            _indexState = indexState;
        }

        //---------------------------------------------------------------------------------------
        // This method wraps each enumerator in inputStream with an enumerator performing this
        // operator's transformation. However, instead of returning the transformed partitioned
        // stream, we pass it to a recipient object by calling recipient.Give<TNewKey>(..). That
        // way, we can "return" a partitioned stream that potentially uses a different order key
        // from the order key of the input stream.
        //

        internal abstract void WrapPartitionedStream<TKey>(
            PartitionedStream<TInput, TKey> inputStream, IPartitionedStreamRecipient<TOutput> recipient,
            bool preferStriping, QuerySettings settings);


        //---------------------------------------------------------------------------------------
        // Implementation of QueryResults for an unary operator. The results will not be indexable
        // unless a derived class provides that functionality.
        //

        internal class UnaryQueryOperatorResults : QueryResults<TOutput>
        {
            protected QueryResults<TInput> _childQueryResults; // Results of the child query
            private readonly UnaryQueryOperator<TInput, TOutput> _op; // Operator that generated these results
            private QuerySettings _settings; // Settings collected from the query
            private readonly bool _preferStriping; // If the results are indexable, should we use striping when partitioning them

            internal UnaryQueryOperatorResults(QueryResults<TInput> childQueryResults, UnaryQueryOperator<TInput, TOutput> op, QuerySettings settings, bool preferStriping)
            {
                _childQueryResults = childQueryResults;
                _op = op;
                _settings = settings;
                _preferStriping = preferStriping;
            }

            internal override void GivePartitionedStream(IPartitionedStreamRecipient<TOutput> recipient)
            {
                Debug.Assert(IsIndexible == (_op.OrdinalIndexState == OrdinalIndexState.Indexable));

                Debug.Assert(_settings.ExecutionMode != null && _settings.DegreeOfParallelism != null);
                if (_settings.ExecutionMode.Value == ParallelExecutionMode.Default && _op.LimitsParallelism)
                {
                    // We need to run the query sequentially, up to and including this operator
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
                    _childQueryResults.GivePartitionedStream(new ChildResultsRecipient(recipient, _op, _preferStriping, _settings));
                }
            }

            //---------------------------------------------------------------------------------------
            // ChildResultsRecipient is a recipient of a partitioned stream. It receives a partitioned
            // stream from the child operator, wraps the enumerators with the transformation for this
            // operator, and passes the partitioned stream along to the next recipient (the parent
            // operator).
            //

            private sealed class ChildResultsRecipient : IPartitionedStreamRecipient<TInput>
            {
                private readonly IPartitionedStreamRecipient<TOutput> _outputRecipient;
                private readonly UnaryQueryOperator<TInput, TOutput> _op;
                private readonly bool _preferStriping;
                private QuerySettings _settings;

                internal ChildResultsRecipient(
                    IPartitionedStreamRecipient<TOutput> outputRecipient, UnaryQueryOperator<TInput, TOutput> op, bool preferStriping, QuerySettings settings)
                {
                    _outputRecipient = outputRecipient;
                    _op = op;
                    _preferStriping = preferStriping;
                    _settings = settings;
                }

                public void Receive<TKey>(PartitionedStream<TInput, TKey> inputStream)
                {
                    // Call WrapPartitionedStream on our operator, which will wrap the input
                    // partitioned stream, and pass the result along to _outputRecipient.
                    _op.WrapPartitionedStream(inputStream, _outputRecipient, _preferStriping, _settings);
                }
            }
        }
    }
}
