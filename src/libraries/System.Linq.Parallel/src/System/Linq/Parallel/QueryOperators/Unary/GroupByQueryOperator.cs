// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// GroupByQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using IEnumerator = System.Collections.IEnumerator;

namespace System.Linq.Parallel
{
    /// <summary>
    /// The operator type for GroupBy statements. This operator groups the input based on
    /// a key-selection routine, yielding one-to-many values of key-to-elements. The
    /// implementation is very much like the hash join operator, in which we first build
    /// a big hashtable of the input; then we just iterate over each unique key in the
    /// hashtable, yielding it plus all of the elements with the same key.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TGroupKey"></typeparam>
    /// <typeparam name="TElement"></typeparam>
    internal sealed class GroupByQueryOperator<TSource, TGroupKey, TElement> :
        UnaryQueryOperator<TSource, IGrouping<TGroupKey, TElement>>
    {
        private readonly Func<TSource, TGroupKey> _keySelector; // Key selection function.
        private readonly Func<TSource, TElement> _elementSelector; // Optional element selection function.
        private readonly IEqualityComparer<TGroupKey> _keyComparer; // An optional key comparison object.

        //---------------------------------------------------------------------------------------
        // Initializes a new group by operator.
        //
        // Arguments:
        //    child                - the child operator or data source from which to pull data
        //    keySelector          - a delegate representing the key selector function
        //    elementSelector      - a delegate representing the element selector function
        //    keyComparer          - an optional key comparison routine
        //
        // Assumptions:
        //    keySelector must be non null.
        //    elementSelector must be non null.
        //

        internal GroupByQueryOperator(IEnumerable<TSource> child,
                                      Func<TSource, TGroupKey> keySelector,
                                      Func<TSource, TElement> elementSelector,
                                      IEqualityComparer<TGroupKey> keyComparer)
            : base(child)
        {
            Debug.Assert(child != null, "child data source cannot be null");
            Debug.Assert(keySelector != null, "need a selector function");
            Debug.Assert(elementSelector != null ||
                            typeof(TSource) == typeof(TElement), "need an element function if TSource!=TElement");

            _keySelector = keySelector;
            _elementSelector = elementSelector;
            _keyComparer = keyComparer;

            SetOrdinalIndexState(OrdinalIndexState.Shuffled);
        }

        internal override void WrapPartitionedStream<TKey>(
            PartitionedStream<TSource, TKey> inputStream, IPartitionedStreamRecipient<IGrouping<TGroupKey, TElement>> recipient,
            bool preferStriping, QuerySettings settings)
        {
            // Hash-repartition the source stream
            if (Child.OutputOrdered)
            {
                WrapPartitionedStreamHelperOrdered<TKey>(
                    ExchangeUtilities.HashRepartitionOrdered<TSource, TGroupKey, TKey>(
                        inputStream, _keySelector, _keyComparer, null, settings.CancellationState.MergedCancellationToken),
                    recipient,
                    settings.CancellationState.MergedCancellationToken
                );
            }
            else
            {
                WrapPartitionedStreamHelper<TKey, int>(
                    ExchangeUtilities.HashRepartition<TSource, TGroupKey, TKey>(
                        inputStream, _keySelector, _keyComparer, null, settings.CancellationState.MergedCancellationToken),
                    recipient,
                    settings.CancellationState.MergedCancellationToken
                );
            }
        }

        //---------------------------------------------------------------------------------------
        // This is a helper method. WrapPartitionedStream decides what type TKey is going
        // to be, and then call this method with that key as a generic parameter.
        //

        private void WrapPartitionedStreamHelper<TIgnoreKey, TKey>(
            PartitionedStream<Pair<TSource, TGroupKey>, TKey> hashStream,
            IPartitionedStreamRecipient<IGrouping<TGroupKey, TElement>> recipient,
            CancellationToken cancellationToken)
        {
            int partitionCount = hashStream.PartitionCount;
            PartitionedStream<IGrouping<TGroupKey, TElement>, TKey> outputStream =
                new PartitionedStream<IGrouping<TGroupKey, TElement>, TKey>(partitionCount, hashStream.KeyComparer, OrdinalIndexState.Shuffled);

            // If there is no element selector, we return a special identity enumerator. Otherwise,
            // we return one that will apply the element selection function during enumeration.

            for (int i = 0; i < partitionCount; i++)
            {
                if (_elementSelector == null)
                {
                    Debug.Assert(typeof(TSource) == typeof(TElement));

                    var enumerator = new GroupByIdentityQueryOperatorEnumerator<TSource, TGroupKey, TKey>(
                        hashStream[i], _keyComparer, cancellationToken);

                    outputStream[i] = (QueryOperatorEnumerator<IGrouping<TGroupKey, TElement>, TKey>)(object)enumerator;
                }
                else
                {
                    outputStream[i] = new GroupByElementSelectorQueryOperatorEnumerator<TSource, TGroupKey, TElement, TKey>(
                        hashStream[i], _keyComparer, _elementSelector, cancellationToken);
                }
            }

            recipient.Receive(outputStream);
        }

        //---------------------------------------------------------------------------------------
        // This is a helper method. WrapPartitionedStream decides what type TKey is going
        // to be, and then call this method with that key as a generic parameter.
        //

        private void WrapPartitionedStreamHelperOrdered<TKey>(
            PartitionedStream<Pair<TSource, TGroupKey>, TKey> hashStream,
            IPartitionedStreamRecipient<IGrouping<TGroupKey, TElement>> recipient,
            CancellationToken cancellationToken)
        {
            int partitionCount = hashStream.PartitionCount;
            PartitionedStream<IGrouping<TGroupKey, TElement>, TKey> outputStream =
                new PartitionedStream<IGrouping<TGroupKey, TElement>, TKey>(partitionCount, hashStream.KeyComparer, OrdinalIndexState.Shuffled);

            // If there is no element selector, we return a special identity enumerator. Otherwise,
            // we return one that will apply the element selection function during enumeration.

            IComparer<TKey> orderComparer = hashStream.KeyComparer;
            for (int i = 0; i < partitionCount; i++)
            {
                if (_elementSelector == null)
                {
                    Debug.Assert(typeof(TSource) == typeof(TElement));

                    var enumerator = new OrderedGroupByIdentityQueryOperatorEnumerator<TSource, TGroupKey, TKey>(
                        hashStream[i], _keySelector, _keyComparer, orderComparer, cancellationToken);

                    outputStream[i] = (QueryOperatorEnumerator<IGrouping<TGroupKey, TElement>, TKey>)(object)enumerator;
                }
                else
                {
                    outputStream[i] = new OrderedGroupByElementSelectorQueryOperatorEnumerator<TSource, TGroupKey, TElement, TKey>(
                        hashStream[i], _keySelector, _elementSelector, _keyComparer, orderComparer,
                        cancellationToken);
                }
            }

            recipient.Receive(outputStream);
        }

        //-----------------------------------------------------------------------------------
        // Override of the query operator base class's Open method. 
        //
        internal override QueryResults<IGrouping<TGroupKey, TElement>> Open(QuerySettings settings, bool preferStriping)
        {
            // We just open our child operator. Do not propagate the preferStriping value, but instead explicitly
            // set it to false. Regardless of whether the parent prefers striping or range partitioning, the output
            // will be hash-partitioned.
            QueryResults<TSource> childResults = Child.Open(settings, false);
            return new UnaryQueryOperatorResults(childResults, this, settings, false);
        }


        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //
        internal override IEnumerable<IGrouping<TGroupKey, TElement>> AsSequentialQuery(CancellationToken token)
        {
            IEnumerable<TSource> wrappedChild = CancellableEnumerable.Wrap(Child.AsSequentialQuery(token), token);
            if (_elementSelector == null)
            {
                Debug.Assert(typeof(TElement) == typeof(TSource));
                return (IEnumerable<IGrouping<TGroupKey, TElement>>)wrappedChild.GroupBy(_keySelector, _keyComparer);
            }
            else
            {
                return wrappedChild.GroupBy(_keySelector, _elementSelector, _keyComparer);
            }
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


    //---------------------------------------------------------------------------------------
    // The enumerator type responsible for grouping elements and yielding the key-value sets.
    //
    // Assumptions:
    //     Just like the Join operator, this won't work properly at all if the analysis engine
    //     didn't choose to hash partition. We will simply not yield correct groupings.
    //
    internal abstract class GroupByQueryOperatorEnumerator<TSource, TGroupKey, TElement, TOrderKey> :
        QueryOperatorEnumerator<IGrouping<TGroupKey, TElement>, TOrderKey>
    {
        protected readonly QueryOperatorEnumerator<Pair<TSource, TGroupKey>, TOrderKey> _source; // The data source to enumerate.
        protected readonly IEqualityComparer<TGroupKey> _keyComparer; // A key comparer.
        protected readonly CancellationToken _cancellationToken;
        private Mutables _mutables; // All of the mutable state.

        class Mutables
        {
            internal HashLookup<Wrapper<TGroupKey>, ListChunk<TElement>> _hashLookup; // The lookup with key-value mappings.
            internal int _hashLookupIndex; // The current index within the lookup.
        }

        //---------------------------------------------------------------------------------------
        // Instantiates a new group by enumerator.
        //

        protected GroupByQueryOperatorEnumerator(
            QueryOperatorEnumerator<Pair<TSource, TGroupKey>, TOrderKey> source,
            IEqualityComparer<TGroupKey> keyComparer, CancellationToken cancellationToken)
        {
            Debug.Assert(source != null);

            _source = source;
            _keyComparer = keyComparer;
            _cancellationToken = cancellationToken;
        }

        //---------------------------------------------------------------------------------------
        // MoveNext will invoke the entire query sub-tree, accumulating results into a hash-
        // table, upon the first call. Then for the first call and all subsequent calls, we will
        // just enumerate the key-set from the hash-table, retrieving groupings of key-elements.
        //

        internal override bool MoveNext(ref IGrouping<TGroupKey, TElement> currentElement, ref TOrderKey currentKey)
        {
            Debug.Assert(_source != null);

            // Lazy-init the mutable state. This also means we haven't yet built our lookup of
            // groupings, so we can go ahead and do that too.
            Mutables mutables = _mutables;
            if (mutables == null)
            {
                mutables = _mutables = new Mutables();

                // Build the hash lookup and start enumerating the lookup at the beginning.
                mutables._hashLookup = BuildHashLookup();
                Debug.Assert(mutables._hashLookup != null);
                mutables._hashLookupIndex = -1;
            }

            // Now, with a hash lookup in hand, we just enumerate the keys. So long
            // as the key-value lookup has elements, we have elements.
            if (++mutables._hashLookupIndex < mutables._hashLookup.Count)
            {
                currentElement = new GroupByGrouping<TGroupKey, TElement>(
                    mutables._hashLookup[mutables._hashLookupIndex]);
                return true;
            }

            return false;
        }

        //-----------------------------------------------------------------------------------
        // Builds the hash lookup, transforming from TSource to TElement through whatever means is appropriate.
        //

        protected abstract HashLookup<Wrapper<TGroupKey>, ListChunk<TElement>> BuildHashLookup();

        protected override void Dispose(bool disposing)
        {
            _source.Dispose();
        }
    }

    //---------------------------------------------------------------------------------------
    // A specialization of the group by enumerator for yielding elements with the identity
    // function.
    //

    internal sealed class GroupByIdentityQueryOperatorEnumerator<TSource, TGroupKey, TOrderKey> :
        GroupByQueryOperatorEnumerator<TSource, TGroupKey, TSource, TOrderKey>
    {
        //---------------------------------------------------------------------------------------
        // Instantiates a new group by enumerator.
        //

        internal GroupByIdentityQueryOperatorEnumerator(
            QueryOperatorEnumerator<Pair<TSource, TGroupKey>, TOrderKey> source,
            IEqualityComparer<TGroupKey> keyComparer, CancellationToken cancellationToken)
            : base(source, keyComparer, cancellationToken)
        {
        }

        //-----------------------------------------------------------------------------------
        // Builds the hash lookup, transforming from TSource to TElement through whatever means is appropriate.
        //

        protected override HashLookup<Wrapper<TGroupKey>, ListChunk<TSource>> BuildHashLookup()
        {
            HashLookup<Wrapper<TGroupKey>, ListChunk<TSource>> hashlookup =
                new HashLookup<Wrapper<TGroupKey>, ListChunk<TSource>>(new WrapperEqualityComparer<TGroupKey>(_keyComparer));

            Pair<TSource, TGroupKey> sourceElement = default(Pair<TSource, TGroupKey>);
            TOrderKey sourceKeyUnused = default(TOrderKey);
            int i = 0;
            while (_source.MoveNext(ref sourceElement, ref sourceKeyUnused))
            {
                if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                    CancellationState.ThrowIfCanceled(_cancellationToken);

                // Generate a key and place it into the hashtable.
                Wrapper<TGroupKey> key = new Wrapper<TGroupKey>(sourceElement.Second);

                // If the key already exists, we just append it to the existing list --
                // otherwise we will create a new one and add it to that instead.
                ListChunk<TSource> currentValue = null;
                if (!hashlookup.TryGetValue(key, ref currentValue))
                {
                    const int INITIAL_CHUNK_SIZE = 2;
                    currentValue = new ListChunk<TSource>(INITIAL_CHUNK_SIZE);
                    hashlookup.Add(key, currentValue);
                }
                Debug.Assert(currentValue != null);

                // Call to the base class to yield the current value.
                currentValue.Add(sourceElement.First);
            }

            return hashlookup;
        }
    }

    //---------------------------------------------------------------------------------------
    // A specialization of the group by enumerator for yielding elements with any arbitrary
    // element selection function.
    //

    internal sealed class GroupByElementSelectorQueryOperatorEnumerator<TSource, TGroupKey, TElement, TOrderKey> :
        GroupByQueryOperatorEnumerator<TSource, TGroupKey, TElement, TOrderKey>
    {
        private readonly Func<TSource, TElement> _elementSelector; // Function to select elements.

        //---------------------------------------------------------------------------------------
        // Instantiates a new group by enumerator.
        //

        internal GroupByElementSelectorQueryOperatorEnumerator(
            QueryOperatorEnumerator<Pair<TSource, TGroupKey>, TOrderKey> source,
            IEqualityComparer<TGroupKey> keyComparer, Func<TSource, TElement> elementSelector, CancellationToken cancellationToken) :
            base(source, keyComparer, cancellationToken)
        {
            Debug.Assert(elementSelector != null);
            _elementSelector = elementSelector;
        }

        //-----------------------------------------------------------------------------------
        // Builds the hash lookup, transforming from TSource to TElement through whatever means is appropriate.
        //

        protected override HashLookup<Wrapper<TGroupKey>, ListChunk<TElement>> BuildHashLookup()
        {
            HashLookup<Wrapper<TGroupKey>, ListChunk<TElement>> hashlookup =
                new HashLookup<Wrapper<TGroupKey>, ListChunk<TElement>>(new WrapperEqualityComparer<TGroupKey>(_keyComparer));

            Pair<TSource, TGroupKey> sourceElement = default(Pair<TSource, TGroupKey>);
            TOrderKey sourceKeyUnused = default(TOrderKey);
            int i = 0;
            while (_source.MoveNext(ref sourceElement, ref sourceKeyUnused))
            {
                if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                    CancellationState.ThrowIfCanceled(_cancellationToken);

                // Generate a key and place it into the hashtable.
                Wrapper<TGroupKey> key = new Wrapper<TGroupKey>(sourceElement.Second);

                // If the key already exists, we just append it to the existing list --
                // otherwise we will create a new one and add it to that instead.
                ListChunk<TElement> currentValue = null;
                if (!hashlookup.TryGetValue(key, ref currentValue))
                {
                    const int INITIAL_CHUNK_SIZE = 2;
                    currentValue = new ListChunk<TElement>(INITIAL_CHUNK_SIZE);
                    hashlookup.Add(key, currentValue);
                }
                Debug.Assert(currentValue != null);

                // Call to the base class to yield the current value.
                currentValue.Add(_elementSelector(sourceElement.First));
            }

            return hashlookup;
        }
    }


    //---------------------------------------------------------------------------------------
    // Ordered version of the GroupBy operator.
    //

    internal abstract class OrderedGroupByQueryOperatorEnumerator<TSource, TGroupKey, TElement, TOrderKey> :
        QueryOperatorEnumerator<IGrouping<TGroupKey, TElement>, TOrderKey>
    {
        protected readonly QueryOperatorEnumerator<Pair<TSource, TGroupKey>, TOrderKey> _source; // The data source to enumerate.
        private readonly Func<TSource, TGroupKey> _keySelector; // The key selection routine.
        protected readonly IEqualityComparer<TGroupKey> _keyComparer; // The key comparison routine.
        protected readonly IComparer<TOrderKey> _orderComparer; // The comparison routine for order keys.
        protected readonly CancellationToken _cancellationToken;
        private Mutables _mutables; // All the mutable state.

        class Mutables
        {
            internal HashLookup<Wrapper<TGroupKey>, GroupKeyData> _hashLookup; // The lookup with key-value mappings.
            internal int _hashLookupIndex; // The current index within the lookup.
        }

        //---------------------------------------------------------------------------------------
        // Instantiates a new group by enumerator.
        //

        protected OrderedGroupByQueryOperatorEnumerator(QueryOperatorEnumerator<Pair<TSource, TGroupKey>, TOrderKey> source,
            Func<TSource, TGroupKey> keySelector, IEqualityComparer<TGroupKey> keyComparer, IComparer<TOrderKey> orderComparer,
            CancellationToken cancellationToken)
        {
            Debug.Assert(source != null);
            Debug.Assert(keySelector != null);

            _source = source;
            _keySelector = keySelector;
            _keyComparer = keyComparer;
            _orderComparer = orderComparer;
            _cancellationToken = cancellationToken;
        }

        //---------------------------------------------------------------------------------------
        // MoveNext will invoke the entire query sub-tree, accumulating results into a hash-
        // table, upon the first call. Then for the first call and all subsequent calls, we will
        // just enumerate the key-set from the hash-table, retrieving groupings of key-elements.
        //

        internal override bool MoveNext(ref IGrouping<TGroupKey, TElement> currentElement, ref TOrderKey currentKey)
        {
            Debug.Assert(_source != null);
            Debug.Assert(_keySelector != null);

            // Lazy-init the mutable state. This also means we haven't yet built our lookup of
            // groupings, so we can go ahead and do that too.
            Mutables mutables = _mutables;
            if (mutables == null)
            {
                mutables = _mutables = new Mutables();

                // Build the hash lookup and start enumerating the lookup at the beginning.
                mutables._hashLookup = BuildHashLookup();
                Debug.Assert(mutables._hashLookup != null);
                mutables._hashLookupIndex = -1;
            }

            // Now, with a hash lookup in hand, we just enumerate the keys. So long
            // as the key-value lookup has elements, we have elements.
            if (++mutables._hashLookupIndex < mutables._hashLookup.Count)
            {
                GroupKeyData value = mutables._hashLookup[mutables._hashLookupIndex].Value;
                currentElement = value._grouping;
                currentKey = value._orderKey;
                return true;
            }

            return false;
        }

        //-----------------------------------------------------------------------------------
        // Builds the hash lookup, transforming from TSource to TElement through whatever means is appropriate.
        //

        protected abstract HashLookup<Wrapper<TGroupKey>, GroupKeyData> BuildHashLookup();

        protected override void Dispose(bool disposing)
        {
            _source.Dispose();
        }

        //-----------------------------------------------------------------------------------
        // A data structure that holds information about elements with a particular key.
        //
        // This information includes two parts:
        //     - An order key for the grouping.
        //     - The grouping itself. The grouping consists of elements and the grouping key.
        //

        protected class GroupKeyData
        {
            internal TOrderKey _orderKey;
            internal OrderedGroupByGrouping<TGroupKey, TOrderKey, TElement> _grouping;

            internal GroupKeyData(TOrderKey orderKey, TGroupKey hashKey, IComparer<TOrderKey> orderComparer)
            {
                _orderKey = orderKey;
                _grouping = new OrderedGroupByGrouping<TGroupKey, TOrderKey, TElement>(hashKey, orderComparer);
            }
        }
    }


    //---------------------------------------------------------------------------------------
    // A specialization of the ordered GroupBy enumerator for yielding elements with the identity
    // function.
    //

    internal sealed class OrderedGroupByIdentityQueryOperatorEnumerator<TSource, TGroupKey, TOrderKey> :
        OrderedGroupByQueryOperatorEnumerator<TSource, TGroupKey, TSource, TOrderKey>
    {
        //---------------------------------------------------------------------------------------
        // Instantiates a new group by enumerator.
        //

        internal OrderedGroupByIdentityQueryOperatorEnumerator(QueryOperatorEnumerator<Pair<TSource, TGroupKey>, TOrderKey> source,
            Func<TSource, TGroupKey> keySelector, IEqualityComparer<TGroupKey> keyComparer, IComparer<TOrderKey> orderComparer,
            CancellationToken cancellationToken)
            : base(source, keySelector, keyComparer, orderComparer, cancellationToken)
        {
        }

        //-----------------------------------------------------------------------------------
        // Builds the hash lookup, transforming from TSource to TElement through whatever means is appropriate.
        //

        protected override HashLookup<Wrapper<TGroupKey>, GroupKeyData> BuildHashLookup()
        {
            HashLookup<Wrapper<TGroupKey>, GroupKeyData> hashLookup = new HashLookup<Wrapper<TGroupKey>, GroupKeyData>(
                new WrapperEqualityComparer<TGroupKey>(_keyComparer));

            Pair<TSource, TGroupKey> sourceElement = default(Pair<TSource, TGroupKey>);
            TOrderKey sourceOrderKey = default(TOrderKey);
            int i = 0;
            while (_source.MoveNext(ref sourceElement, ref sourceOrderKey))
            {
                if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                    CancellationState.ThrowIfCanceled(_cancellationToken);

                // Generate a key and place it into the hashtable.
                Wrapper<TGroupKey> key = new Wrapper<TGroupKey>(sourceElement.Second);

                // If the key already exists, we just append it to the existing list --
                // otherwise we will create a new one and add it to that instead.
                GroupKeyData currentValue = null;
                if (hashLookup.TryGetValue(key, ref currentValue))
                {
                    if (_orderComparer.Compare(sourceOrderKey, currentValue._orderKey) < 0)
                    {
                        currentValue._orderKey = sourceOrderKey;
                    }
                }
                else
                {
                    currentValue = new GroupKeyData(sourceOrderKey, key.Value, _orderComparer);

                    hashLookup.Add(key, currentValue);
                }

                Debug.Assert(currentValue != null);

                currentValue._grouping.Add(sourceElement.First, sourceOrderKey);
            }

            // Sort the elements within each group
            for (int j = 0; j < hashLookup.Count; j++)
            {
                hashLookup[j].Value._grouping.DoneAdding();
            }

            return hashLookup;
        }
    }

    //---------------------------------------------------------------------------------------
    // A specialization of the ordered GroupBy enumerator for yielding elements with any arbitrary
    // element selection function.
    //

    internal sealed class OrderedGroupByElementSelectorQueryOperatorEnumerator<TSource, TGroupKey, TElement, TOrderKey> :
        OrderedGroupByQueryOperatorEnumerator<TSource, TGroupKey, TElement, TOrderKey>
    {
        private readonly Func<TSource, TElement> _elementSelector; // Function to select elements.

        //---------------------------------------------------------------------------------------
        // Instantiates a new group by enumerator.
        //

        internal OrderedGroupByElementSelectorQueryOperatorEnumerator(QueryOperatorEnumerator<Pair<TSource, TGroupKey>, TOrderKey> source,
            Func<TSource, TGroupKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TGroupKey> keyComparer, IComparer<TOrderKey> orderComparer,
            CancellationToken cancellationToken) :
            base(source, keySelector, keyComparer, orderComparer, cancellationToken)
        {
            Debug.Assert(elementSelector != null);
            _elementSelector = elementSelector;
        }

        //-----------------------------------------------------------------------------------
        // Builds the hash lookup, transforming from TSource to TElement through whatever means is appropriate.
        //

        protected override HashLookup<Wrapper<TGroupKey>, GroupKeyData> BuildHashLookup()
        {
            HashLookup<Wrapper<TGroupKey>, GroupKeyData> hashLookup = new HashLookup<Wrapper<TGroupKey>, GroupKeyData>(
                new WrapperEqualityComparer<TGroupKey>(_keyComparer));

            Pair<TSource, TGroupKey> sourceElement = default(Pair<TSource, TGroupKey>);
            TOrderKey sourceOrderKey = default(TOrderKey);
            int i = 0;
            while (_source.MoveNext(ref sourceElement, ref sourceOrderKey))
            {
                if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                    CancellationState.ThrowIfCanceled(_cancellationToken);

                // Generate a key and place it into the hashtable.
                Wrapper<TGroupKey> key = new Wrapper<TGroupKey>(sourceElement.Second);

                // If the key already exists, we just append it to the existing list --
                // otherwise we will create a new one and add it to that instead.
                GroupKeyData currentValue = null;
                if (hashLookup.TryGetValue(key, ref currentValue))
                {
                    if (_orderComparer.Compare(sourceOrderKey, currentValue._orderKey) < 0)
                    {
                        currentValue._orderKey = sourceOrderKey;
                    }
                }
                else
                {
                    currentValue = new GroupKeyData(sourceOrderKey, key.Value, _orderComparer);

                    hashLookup.Add(key, currentValue);
                }

                Debug.Assert(currentValue != null);

                // Call to the base class to yield the current value.
                currentValue._grouping.Add(_elementSelector(sourceElement.First), sourceOrderKey);
            }

            // Sort the elements within each group
            for (int j = 0; j < hashLookup.Count; j++)
            {
                hashLookup[j].Value._grouping.DoneAdding();
            }

            return hashLookup;
        }
    }


    //---------------------------------------------------------------------------------------
    // This little type implements the IGrouping<K,T> interface, and exposes a single
    // key-to-many-values mapping.
    //

    internal class GroupByGrouping<TGroupKey, TElement> : IGrouping<TGroupKey, TElement>
    {
        private KeyValuePair<Wrapper<TGroupKey>, ListChunk<TElement>> _keyValues; // A key value pair.

        //---------------------------------------------------------------------------------------
        // Constructs a new grouping out of the key value pair.
        //

        internal GroupByGrouping(KeyValuePair<Wrapper<TGroupKey>, ListChunk<TElement>> keyValues)
        {
            Debug.Assert(keyValues.Value != null);
            _keyValues = keyValues;
        }

        //---------------------------------------------------------------------------------------
        // The key this mapping represents.
        //

        TGroupKey IGrouping<TGroupKey, TElement>.Key
        {
            get
            {
                return _keyValues.Key.Value;
            }
        }

        //---------------------------------------------------------------------------------------
        // Access to value enumerators. 
        //

        IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator()
        {
            Debug.Assert(_keyValues.Value != null);
            return _keyValues.Value.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TElement>)this).GetEnumerator();
        }
    }

    /// <summary>
    /// An ordered version of the grouping data structure. Represents an ordered group of elements that
    /// have the same grouping key.
    /// </summary>
    internal class OrderedGroupByGrouping<TGroupKey, TOrderKey, TElement> : IGrouping<TGroupKey, TElement>
    {
        private TGroupKey _groupKey; // The group key for this grouping
        private GrowingArray<TElement> _values; // Values in this group
        private GrowingArray<TOrderKey> _orderKeys; // Order keys that correspond to the values
        private IComparer<TOrderKey> _orderComparer; // Comparer for order keys
        private KeyAndValuesComparer _wrappedComparer; // Comparer that wraps the _orderComparer used for sorting key/value pairs

        /// <summary>
        /// Constructs a new grouping
        /// </summary>
        internal OrderedGroupByGrouping(
            TGroupKey groupKey,
            IComparer<TOrderKey> orderComparer)
        {
            _groupKey = groupKey;
            _values = new GrowingArray<TElement>();
            _orderKeys = new GrowingArray<TOrderKey>();
            _orderComparer = orderComparer;
            _wrappedComparer = new KeyAndValuesComparer(_orderComparer);
        }

        /// <summary>
        /// The key this grouping represents.
        /// </summary>
        TGroupKey IGrouping<TGroupKey, TElement>.Key
        {
            get
            {
                return _groupKey;
            }
        }

        IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator()
        {
            Debug.Assert(_values != null);


            int valueCount = _values.Count;
            TElement[] valueArray = _values.InternalArray;
            Debug.Assert(valueArray.Length >= valueCount); // valueArray.Length may be larger than valueCount

            for (int i = 0; i < valueCount; i++)
            {
                yield return valueArray[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<TElement>)this).GetEnumerator();
        }

        /// <summary>
        /// Add an element
        /// </summary>
        internal void Add(TElement value, TOrderKey orderKey)
        {
            Debug.Assert(_values != null);
            Debug.Assert(_orderKeys != null);

            _values.Add(value);
            _orderKeys.Add(orderKey);
        }

        /// <summary>
        /// No more elements will be added, so we can sort the group now.
        /// </summary>
        internal void DoneAdding()
        {
            Debug.Assert(_values != null);
            Debug.Assert(_orderKeys != null);

            // Create a map of key-value pair.
            // We can't use a dictionary since the keys are not necessarily unique
            List<KeyValuePair<TOrderKey, TElement>> sortedValues = new List<KeyValuePair<TOrderKey, TElement>>();
            for (int i = 0; i < _orderKeys.InternalArray.Length; i++)
            {
                sortedValues.Add(new KeyValuePair<TOrderKey, TElement>(_orderKeys.InternalArray[i], _values.InternalArray[i]));
            }

            // Sort the values by using the _orderComparer wrapped in a Tuple comparer
            sortedValues.Sort(0, _values.Count, _wrappedComparer);

            // Unpack the values from the list back into the 2 separate arrays
            for (int i = 0; i < _values.InternalArray.Length; i++)
            {
                _orderKeys.InternalArray[i] = sortedValues[i].Key;
                _values.InternalArray[i] = sortedValues[i].Value;
            }
#if DEBUG
            _orderKeys = null; // Any future calls to Add() or DoneAdding() will fail
#endif
        }

        private class KeyAndValuesComparer : IComparer<KeyValuePair<TOrderKey, TElement>>
        {
            private IComparer<TOrderKey> myComparer;
            public KeyAndValuesComparer(IComparer<TOrderKey> comparer)
            {
                myComparer = comparer;
            }
            public int Compare(KeyValuePair<TOrderKey, TElement> x, KeyValuePair<TOrderKey, TElement> y)
            {
                return myComparer.Compare(x.Key, y.Key);
            }
        }
    }
}
