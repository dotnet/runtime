// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// SortQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// The query operator for OrderBy and ThenBy.
    /// </summary>
    /// <typeparam name="TInputOutput"></typeparam>
    /// <typeparam name="TSortKey"></typeparam>
    internal sealed class SortQueryOperator<TInputOutput, TSortKey> :
        UnaryQueryOperator<TInputOutput, TInputOutput>, IOrderedEnumerable<TInputOutput>
    {
        private readonly Func<TInputOutput, TSortKey> _keySelector; // Key selector used when sorting.
        private readonly IComparer<TSortKey> _comparer; // Key comparison logic to use during sorting.

        //---------------------------------------------------------------------------------------
        // Instantiates a new sort operator.
        //

        internal SortQueryOperator(IEnumerable<TInputOutput> source, Func<TInputOutput, TSortKey> keySelector,
                                   IComparer<TSortKey>? comparer, bool descending)
            : base(source, true)
        {
            Debug.Assert(keySelector != null, "key selector must not be null");

            _keySelector = keySelector;

            // If a comparer wasn't supplied, we use the default one for the key type.
            _comparer = comparer ?? Util.GetDefaultComparer<TSortKey>();
            if (descending)
            {
                _comparer = new ReverseComparer<TSortKey>(_comparer);
            }

            SetOrdinalIndexState(OrdinalIndexState.Shuffled);
        }

        //---------------------------------------------------------------------------------------
        // IOrderedEnumerable method for nesting an order by operator inside another.
        //

        IOrderedEnumerable<TInputOutput> IOrderedEnumerable<TInputOutput>.CreateOrderedEnumerable<TKey2>(
            Func<TInputOutput, TKey2> key2Selector, IComparer<TKey2>? key2Comparer, bool descending)
        {
            key2Comparer ??= Util.GetDefaultComparer<TKey2>();

            if (descending)
            {
                key2Comparer = new ReverseComparer<TKey2>(key2Comparer);
            }

            IComparer<Pair<TSortKey, TKey2>> pairComparer = new PairComparer<TSortKey, TKey2>(_comparer, key2Comparer);
            Func<TInputOutput, Pair<TSortKey, TKey2>> pairKeySelector =
                (TInputOutput elem) => new Pair<TSortKey, TKey2>(_keySelector(elem), key2Selector(elem));

            return new SortQueryOperator<TInputOutput, Pair<TSortKey, TKey2>>(Child, pairKeySelector, pairComparer, false);
        }

        //---------------------------------------------------------------------------------------
        // Opens the current operator. This involves opening the child operator tree, enumerating
        // the results, sorting them, and then returning an enumerator that walks the result.
        //

        internal override QueryResults<TInputOutput> Open(QuerySettings settings, bool preferStriping)
        {
            QueryResults<TInputOutput> childQueryResults = Child.Open(settings, false);
            return new SortQueryOperatorResults<TInputOutput, TSortKey>(childQueryResults, this, settings);
        }


        internal override void WrapPartitionedStream<TKey>(
            PartitionedStream<TInputOutput, TKey> inputStream, IPartitionedStreamRecipient<TInputOutput> recipient, bool preferStriping, QuerySettings settings)
        {
            PartitionedStream<TInputOutput, TSortKey> outputStream =
                new PartitionedStream<TInputOutput, TSortKey>(inputStream.PartitionCount, this._comparer, OrdinalIndexState);

            for (int i = 0; i < outputStream.PartitionCount; i++)
            {
                outputStream[i] = new SortQueryOperatorEnumerator<TInputOutput, TKey, TSortKey>(
                    inputStream[i], _keySelector);
            }

            recipient.Receive<TSortKey>(outputStream);
        }

        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TInputOutput> AsSequentialQuery(CancellationToken token)
        {
            IEnumerable<TInputOutput> wrappedChild = CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token);
            return wrappedChild.OrderBy(_keySelector, _comparer);
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

    internal sealed class SortQueryOperatorResults<TInputOutput, TSortKey> : QueryResults<TInputOutput>
    {
        private QueryResults<TInputOutput> _childQueryResults; // Results of the child query
        private readonly SortQueryOperator<TInputOutput, TSortKey> _op; // Operator that generated these results
        private QuerySettings _settings; // Settings collected from the query

        internal SortQueryOperatorResults(
            QueryResults<TInputOutput> childQueryResults, SortQueryOperator<TInputOutput, TSortKey> op,
            QuerySettings settings)
        {
            _childQueryResults = childQueryResults;
            _op = op;
            _settings = settings;
        }

        internal override bool IsIndexible
        {
            get { return false; }
        }

        internal override void GivePartitionedStream(IPartitionedStreamRecipient<TInputOutput> recipient)
        {
            _childQueryResults.GivePartitionedStream(new ChildResultsRecipient(recipient, _op, _settings));
        }

        private sealed class ChildResultsRecipient : IPartitionedStreamRecipient<TInputOutput>
        {
            private readonly IPartitionedStreamRecipient<TInputOutput> _outputRecipient;
            private readonly SortQueryOperator<TInputOutput, TSortKey> _op;
            private QuerySettings _settings;

            internal ChildResultsRecipient(IPartitionedStreamRecipient<TInputOutput> outputRecipient, SortQueryOperator<TInputOutput, TSortKey> op, QuerySettings settings)
            {
                _outputRecipient = outputRecipient;
                _op = op;
                _settings = settings;
            }

            public void Receive<TKey>(PartitionedStream<TInputOutput, TKey> childPartitionedStream)
            {
                _op.WrapPartitionedStream(childPartitionedStream, _outputRecipient, false, _settings);
            }
        }
    }

    //---------------------------------------------------------------------------------------
    // This enumerator performs sorting based on a key selection and comparison routine.
    //

    internal sealed class SortQueryOperatorEnumerator<TInputOutput, TKey, TSortKey> : QueryOperatorEnumerator<TInputOutput, TSortKey>
    {
        private readonly QueryOperatorEnumerator<TInputOutput, TKey>? _source; // Data source to sort.
        private readonly Func<TInputOutput, TSortKey> _keySelector; // Key selector used when sorting.

        //---------------------------------------------------------------------------------------
        // Instantiates a new sort operator enumerator.
        //

        internal SortQueryOperatorEnumerator(QueryOperatorEnumerator<TInputOutput, TKey> source,
            Func<TInputOutput, TSortKey> keySelector)
        {
            Debug.Assert(source != null);
            Debug.Assert(keySelector != null, "need a key comparer");

            _source = source;
            _keySelector = keySelector;
        }

        //---------------------------------------------------------------------------------------
        // Moves to the next element in the sorted output. When called for the first time, the
        // descendents in the sort's child tree are executed entirely, the results accumulated
        // in memory, and the data sorted.
        //

        internal override bool MoveNext([MaybeNullWhen(false), AllowNull] ref TInputOutput currentElement, [AllowNull] ref TSortKey currentKey)
        {
            Debug.Assert(_source != null);

            TKey keyUnused = default(TKey)!;
            if (!_source.MoveNext(ref currentElement!, ref keyUnused))
            {
                return false;
            }

            currentKey = _keySelector(currentElement);
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            Debug.Assert(_source != null);
            _source.Dispose();
        }
    }
}
