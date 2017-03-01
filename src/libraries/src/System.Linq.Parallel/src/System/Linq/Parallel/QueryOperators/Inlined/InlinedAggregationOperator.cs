// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// InlinedAggregationOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// This class is common to all of the "inlined" versions of various aggregations.  The
    /// inlined operators ensure that real MSIL instructions are used to perform elementary
    /// operations versus general purpose delegate-based binary operators.  For obvious reasons
    /// this is a quite bit more efficient, although it does lead to a fair bit of unfortunate
    /// code duplication. 
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TIntermediate"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    internal abstract class InlinedAggregationOperator<TSource, TIntermediate, TResult> :
        UnaryQueryOperator<TSource, TIntermediate>
    {
        //---------------------------------------------------------------------------------------
        // Constructs a new instance of an inlined sum associative operator.
        //

        internal InlinedAggregationOperator(IEnumerable<TSource> child)
            : base(child)
        {
            Debug.Assert(child != null, "child data source cannot be null");
        }

        //---------------------------------------------------------------------------------------
        // Executes the entire query tree, and aggregates the intermediate results into the
        // final result based on the binary operators and final reduction.
        //
        // Return Value:
        //     The single result of aggregation.
        //

        internal TResult Aggregate()
        {
            TResult tr;
            Exception toThrow = null;

            try
            {
                tr = InternalAggregate(ref toThrow);
            }
#if SUPPORT_THREAD_ABORT
            catch (ThreadAbortException)
            {
                // Do not wrap ThreadAbortExceptions
                throw;
            }
#endif
            catch (Exception ex)
            {
                // If the exception is not an aggregate, we must wrap it up and throw that instead.
                if (!(ex is AggregateException))
                {
                    //
                    // Special case: if the query has been canceled, we do not want to wrap the
                    // OperationCanceledException with an AggregateException.
                    //
                    // The query has been canceled iff these conditions hold:
                    // -  The exception thrown is OperationCanceledException
                    // -  We find the external CancellationToken for this query in the OperationCanceledException
                    // -  The externalToken is actually in the canceled state.

                    OperationCanceledException cancelEx = ex as OperationCanceledException;
                    if (cancelEx != null
                        && cancelEx.CancellationToken == SpecifiedQuerySettings.CancellationState.ExternalCancellationToken
                        && SpecifiedQuerySettings.CancellationState.ExternalCancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }

                    throw new AggregateException(ex);
                }

                // Else, just rethrow the current active exception.
                throw;
            }

            // If the aggregation requested that we throw a singular exception, throw it.
            if (toThrow != null)
            {
                throw toThrow;
            }

            return tr;
        }

        //---------------------------------------------------------------------------------------
        // Performs the operator-specific aggregation.
        //
        // Arguments:
        //     singularExceptionToThrow - if the aggregate exception should throw an exception
        //                                without aggregating, this ref-param should be set
        //
        // Return Value:
        //     The single result of aggregation.
        //

        protected abstract TResult InternalAggregate(ref Exception singularExceptionToThrow);

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TIntermediate> Open(
            QuerySettings settings, bool preferStriping)
        {
            QueryResults<TSource> childQueryResults = Child.Open(settings, preferStriping);
            return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
        }

        internal override void WrapPartitionedStream<TKey>(
            PartitionedStream<TSource, TKey> inputStream, IPartitionedStreamRecipient<TIntermediate> recipient,
            bool preferStriping, QuerySettings settings)
        {
            int partitionCount = inputStream.PartitionCount;
            PartitionedStream<TIntermediate, int> outputStream = new PartitionedStream<TIntermediate, int>(
                partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Correct);

            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = CreateEnumerator<TKey>(i, partitionCount, inputStream[i], null, settings.CancellationState.MergedCancellationToken);
            }
            recipient.Receive(outputStream);
        }

        protected abstract QueryOperatorEnumerator<TIntermediate, int> CreateEnumerator<TKey>(
            int index, int count, QueryOperatorEnumerator<TSource, TKey> source, object sharedData, CancellationToken cancellationToken);

        [ExcludeFromCodeCoverage]
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
    }
}
