// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// SingleQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Single searches the input to find the sole element that satisfies the (optional)
    /// predicate.  If multiple such elements are found, the caller is responsible for
    /// producing an error.  There is some degree of cross-partition synchronization to
    /// proactively halt the search if we ever determine there are multiple elements
    /// satisfying the search in the input.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    internal sealed class SingleQueryOperator<TSource> : UnaryQueryOperator<TSource, TSource>
    {
        private readonly Func<TSource, bool>? _predicate; // The optional predicate used during the search.

        //---------------------------------------------------------------------------------------
        // Initializes a new Single operator.
        //
        // Arguments:
        //     child                - the child whose data we will reverse
        //

        internal SingleQueryOperator(IEnumerable<TSource> child, Func<TSource, bool>? predicate)
            : base(child)
        {
            Debug.Assert(child != null, "child data source cannot be null");
            _predicate = predicate;
        }

        //---------------------------------------------------------------------------------------
        // Just opens the current operator, including opening the child and wrapping it with
        // partitions as needed.
        //

        internal override QueryResults<TSource> Open(
            QuerySettings settings, bool preferStriping)
        {
            QueryResults<TSource> childQueryResults = Child.Open(settings, false);
            return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
        }

        internal override void WrapPartitionedStream<TKey>(
            PartitionedStream<TSource, TKey> inputStream, IPartitionedStreamRecipient<TSource> recipient, bool preferStriping, QuerySettings settings)
        {
            int partitionCount = inputStream.PartitionCount;
            PartitionedStream<TSource, int> outputStream = new PartitionedStream<TSource, int>(
                partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Shuffled);

            Shared<int> totalElementCount = new Shared<int>(0);
            for (int i = 0; i < partitionCount; i++)
            {
                outputStream[i] = new SingleQueryOperatorEnumerator<TKey>(inputStream[i], _predicate, totalElementCount);
            }

            recipient.Receive(outputStream);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        [ExcludeFromCodeCoverage(Justification = "This method should never be called as it is an ending operator with LimitsParallelism=false")]
        internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
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
        // The enumerator type responsible for executing the Single operation.
        //

        private sealed class SingleQueryOperatorEnumerator<TKey> : QueryOperatorEnumerator<TSource, int>
        {
            private readonly QueryOperatorEnumerator<TSource, TKey> _source; // The data source to enumerate.
            private readonly Func<TSource, bool>? _predicate; // The optional predicate used during the search.
            private bool _alreadySearched; // Whether we have searched our input already.
            private bool _yieldExtra; // Whether we found more than one element.

            // Data shared among partitions.
            private readonly Shared<int> _totalElementCount; // The total count of elements found.

            //---------------------------------------------------------------------------------------
            // Instantiates a new enumerator.
            //

            internal SingleQueryOperatorEnumerator(QueryOperatorEnumerator<TSource, TKey> source,
                                                   Func<TSource, bool>? predicate, Shared<int> totalElementCount)
            {
                Debug.Assert(source != null);
                Debug.Assert(totalElementCount != null);

                _source = source;
                _predicate = predicate;
                _totalElementCount = totalElementCount;
            }

            //---------------------------------------------------------------------------------------
            // Straightforward IEnumerator<T> methods.
            //

            internal override bool MoveNext([AllowNull] ref TSource currentElement, ref int currentKey)
            {
                Debug.Assert(_source != null);

                if (_alreadySearched)
                {
                    // If we've already searched, we will "fake out" the caller by returning an extra
                    // element at the end in the case that we've found more than one element.
                    if (_yieldExtra)
                    {
                        _yieldExtra = false;
                        currentElement = default(TSource)!;
                        currentKey = 0;
                        return true;
                    }

                    return false;
                }

                // Scan our input, looking for a match.
                bool found = false;
                TSource current = default(TSource)!;
                TKey keyUnused = default(TKey)!;

                while (_source.MoveNext(ref current!, ref keyUnused))
                {
                    // If the predicate is null or the current element satisfies it, we will remember
                    // it so that we can yield it later.  We then proceed with scanning the input
                    // in case there are multiple such elements.
                    if (_predicate == null || _predicate(current))
                    {
                        // Notify other partitions.
                        Interlocked.Increment(ref _totalElementCount.Value);

                        currentElement = current;
                        currentKey = 0;

                        if (found)
                        {
                            // Already found an element previously, we can exit.
                            _yieldExtra = true;
                            break;
                        }
                        else
                        {
                            found = true;
                        }
                    }

                    // If we've already determined there is more than one matching element in the
                    // data source, we can exit right away.
                    if (Volatile.Read(ref _totalElementCount.Value) > 1)
                    {
                        break;
                    }
                }
                _alreadySearched = true;

                return found;
            }

            protected override void Dispose(bool disposing)
            {
                _source.Dispose();
            }
        }
    }
}
