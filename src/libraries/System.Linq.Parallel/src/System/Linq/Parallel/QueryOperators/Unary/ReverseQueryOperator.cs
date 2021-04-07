// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ReverseQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Reverse imposes ordinal order preservation. There are normally two phases to this
    /// operator's execution.  Each partition first builds a buffer containing all of its
    /// elements, and then proceeds to yielding the elements in reverse.  There is a
    /// 'barrier' (but not a blocking barrier) in between these two steps, at which point the largest index becomes
    /// known.  This is necessary so that when elements from the buffer are yielded, the
    /// CurrentIndex can be reported as the largest index minus the original index (thereby
    /// reversing the indices as well as the elements themselves).  If the largest index is
    /// known a priori, because we have an array for example, we can avoid the barrier in
    /// between the steps.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    internal sealed class ReverseQueryOperator<TSource> : UnaryQueryOperator<TSource, TSource>
    {
        //---------------------------------------------------------------------------------------
        // Initializes a new reverse operator.
        //
        // Arguments:
        //     child                - the child whose data we will reverse
        //

        internal ReverseQueryOperator(IEnumerable<TSource> child)
            : base(child)
        {
            Debug.Assert(child != null, "child data source cannot be null");

            if (Child.OrdinalIndexState == OrdinalIndexState.Indexable)
            {
                SetOrdinalIndexState(OrdinalIndexState.Indexable);
            }
            else
            {
                SetOrdinalIndexState(OrdinalIndexState.Shuffled);
            }
        }

        internal override void WrapPartitionedStream<TKey>(
            PartitionedStream<TSource, TKey> inputStream, IPartitionedStreamRecipient<TSource> recipient, bool preferStriping, QuerySettings settings)
        {
            Debug.Assert(Child.OrdinalIndexState != OrdinalIndexState.Indexable, "Don't take this code path if the child is indexable.");

            int partitionCount = inputStream.PartitionCount;
            PartitionedStream<TSource, TKey> outputStream = new PartitionedStream<TSource, TKey>(
                partitionCount, new ReverseComparer<TKey>(inputStream.KeyComparer), OrdinalIndexState.Shuffled);

            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new ReverseQueryOperatorEnumerator<TKey>(inputStream[i], settings.CancellationState.MergedCancellationToken);
            }
            recipient.Receive(outputStream);
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
        {
            QueryResults<TSource> childQueryResults = Child.Open(settings, false);
            return ReverseQueryOperatorResults.NewResults(childQueryResults, this, settings, preferStriping);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
        {
            IEnumerable<TSource> wrappedChild = CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token);
            return wrappedChild.Reverse();
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
        // The enumerator type responsible for executing the reverse operation.
        //

        private sealed class ReverseQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TSource, TKey>
        {
            private readonly QueryOperatorEnumerator<TSource, TKey> _source; // The data source to reverse.
            private readonly CancellationToken _cancellationToken;
            private List<Pair<TSource, TKey>>? _buffer; // Our buffer. [allocate in moveNext to avoid false-sharing]
            private Shared<int>? _bufferIndex; // Our current index within the buffer. [allocate in moveNext to avoid false-sharing]

            //---------------------------------------------------------------------------------------
            // Instantiates a new select enumerator.
            //

            internal ReverseQueryOperatorEnumerator(QueryOperatorEnumerator<TSource, TKey> source,
                CancellationToken cancellationToken)
            {
                Debug.Assert(source != null);
                _source = source;
                _cancellationToken = cancellationToken;
            }

            //---------------------------------------------------------------------------------------
            // Straightforward IEnumerator<T> methods.
            //

            internal override bool MoveNext([MaybeNullWhen(false), AllowNull] ref TSource currentElement, [AllowNull] ref TKey currentKey)
            {
                // If the buffer has not been created, we will generate it lazily on demand.
                if (_buffer == null)
                {
                    _bufferIndex = new Shared<int>(0);
                    // Buffer all of our data.
                    _buffer = new List<Pair<TSource, TKey>>();
                    TSource current = default(TSource)!;
                    TKey key = default(TKey)!;
                    int i = 0;
                    while (_source.MoveNext(ref current!, ref key))
                    {
                        if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                            _cancellationToken.ThrowIfCancellationRequested();

                        _buffer.Add(new Pair<TSource, TKey>(current, key));
                        _bufferIndex.Value++;
                    }
                }
                Debug.Assert(_bufferIndex != null);

                // Continue yielding elements from our buffer.
                if (--_bufferIndex.Value >= 0)
                {
                    currentElement = _buffer[_bufferIndex.Value].First;
                    currentKey = _buffer[_bufferIndex.Value].Second;
                    return true;
                }

                return false;
            }

            protected override void Dispose(bool disposing)
            {
                _source.Dispose();
            }
        }

        //-----------------------------------------------------------------------------------
        // Query results for a Reverse operator. The results are indexable if the child
        // results were indexable.
        //

        private sealed class ReverseQueryOperatorResults : UnaryQueryOperatorResults
        {
            private readonly int _count; // The number of elements in child results

            public static QueryResults<TSource> NewResults(
                QueryResults<TSource> childQueryResults, ReverseQueryOperator<TSource> op,
                QuerySettings settings, bool preferStriping)
            {
                if (childQueryResults.IsIndexible)
                {
                    return new ReverseQueryOperatorResults(
                        childQueryResults, op, settings, preferStriping);
                }
                else
                {
                    return new UnaryQueryOperatorResults(
                        childQueryResults, op, settings, preferStriping);
                }
            }

            private ReverseQueryOperatorResults(
                QueryResults<TSource> childQueryResults, ReverseQueryOperator<TSource> op,
                QuerySettings settings, bool preferStriping)
                : base(childQueryResults, op, settings, preferStriping)
            {
                Debug.Assert(_childQueryResults.IsIndexible);
                _count = _childQueryResults.ElementsCount;
            }

            internal override bool IsIndexible
            {
                get { return true; }
            }

            internal override int ElementsCount
            {
                get
                {
                    Debug.Assert(_count >= 0);
                    return _count;
                }
            }

            internal override TSource GetElement(int index)
            {
                Debug.Assert(_count >= 0);
                Debug.Assert(index >= 0);
                Debug.Assert(index < _count);

                return _childQueryResults.GetElement(_count - index - 1);
            }
        }
    }
}
