// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// HashJoinQueryOperatorEnumerator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// This enumerator implements the hash-join algorithm as noted earlier.
    ///
    /// Assumptions:
    ///     This enumerator type won't work properly at all if the analysis engine didn't
    ///     ensure a proper hash-partition. We expect inner and outer elements with equal
    ///     keys are ALWAYS in the same partition. If they aren't (e.g. if the analysis is
    ///     busted) we'll silently drop items on the floor. :(
    ///
    ///
    ///  This is the enumerator class for two operators:
    ///   - Join
    ///   - GroupJoin
    /// </summary>
    /// <typeparam name="TLeftInput"></typeparam>
    /// <typeparam name="TLeftKey"></typeparam>
    /// <typeparam name="TRightInput"></typeparam>
    /// <typeparam name="TRightKey"></typeparam>
    /// <typeparam name="THashKey"></typeparam>
    /// <typeparam name="TOutput"></typeparam>
    /// <typeparam name="TOutputKey"></typeparam>
    internal sealed class HashJoinQueryOperatorEnumerator<TLeftInput, TLeftKey, TRightInput, TRightKey, THashKey, TOutput, TOutputKey>
        : QueryOperatorEnumerator<TOutput, TOutputKey>
    {
        private readonly QueryOperatorEnumerator<Pair<TLeftInput, THashKey>, TLeftKey> _leftSource; // Left (outer) data source. For probing.
        private readonly HashLookupBuilder<TRightInput, TRightKey, THashKey> _rightLookupBuilder; // Right (inner) data source. For building.
        private readonly Func<TLeftInput, TRightInput, TOutput> _resultSelector; // Result selector.
        private readonly HashJoinOutputKeyBuilder<TLeftKey, TRightKey, TOutputKey> _outputKeyBuilder;
        private readonly CancellationToken _cancellationToken;
        private Mutables? _mutables;

        private sealed class Mutables
        {
            internal TLeftInput _currentLeft = default!; // The current matching left element.
            internal TLeftKey _currentLeftKey = default!; // The current index of the matching left element.
            internal HashJoinHashLookup<THashKey, TRightInput, TRightKey>? _rightHashLookup; // The hash lookup.
            internal ListChunk<Pair<TRightInput, TRightKey>>? _currentRightMatches; // Current right matches (if any).
            internal int _currentRightMatchesIndex; // Current index in the set of right matches.
            internal int _outputLoopCount;
        }

        //---------------------------------------------------------------------------------------
        // Instantiates a new hash-join enumerator.
        //

        internal HashJoinQueryOperatorEnumerator(
            QueryOperatorEnumerator<Pair<TLeftInput, THashKey>, TLeftKey> leftSource,
            HashLookupBuilder<TRightInput, TRightKey, THashKey> rightLookupBuilder,
            Func<TLeftInput, TRightInput, TOutput> resultSelector,
            HashJoinOutputKeyBuilder<TLeftKey, TRightKey, TOutputKey> outputKeyBuilder,
            CancellationToken cancellationToken)
        {
            Debug.Assert(leftSource != null);
            Debug.Assert(rightLookupBuilder != null);
            Debug.Assert(resultSelector != null);
            Debug.Assert(outputKeyBuilder != null);

            _leftSource = leftSource;
            _rightLookupBuilder = rightLookupBuilder;
            _resultSelector = resultSelector;
            _outputKeyBuilder = outputKeyBuilder;
            _cancellationToken = cancellationToken;
        }

        //---------------------------------------------------------------------------------------
        // MoveNext implements all the hash-join logic noted earlier. When it is called first, it
        // will execute the entire inner query tree, and build a hash-table lookup. This is the
        // Building phase. Then for the first call and all subsequent calls to MoveNext, we will
        // incrementally perform the Probing phase. We'll keep getting elements from the outer
        // data source, looking into the hash-table we built, and enumerating the full results.
        //
        // This routine supports both inner and outer (group) joins. An outer join will yield a
        // (possibly empty) list of matching elements from the inner instead of one-at-a-time,
        // as we do for inner joins.
        //

        internal override bool MoveNext([MaybeNullWhen(false), AllowNull] ref TOutput currentElement, [AllowNull] ref TOutputKey currentKey)
        {
            Debug.Assert(_resultSelector != null, "expected a compiled result selector");
            Debug.Assert(_leftSource != null);
            Debug.Assert(_rightLookupBuilder != null);

            // BUILD phase: If we haven't built the hash-table yet, create that first.
            Mutables? mutables = _mutables;
            if (mutables == null)
            {
                mutables = _mutables = new Mutables();
                mutables._rightHashLookup = _rightLookupBuilder.BuildHashLookup(_cancellationToken);
            }

            // PROBE phase: So long as the source has a next element, return the match.
            ListChunk<Pair<TRightInput, TRightKey>>? currentRightChunk = mutables._currentRightMatches;
            if (currentRightChunk != null && mutables._currentRightMatchesIndex == currentRightChunk.Count)
            {
                mutables._currentRightMatches = currentRightChunk.Next;
                mutables._currentRightMatchesIndex = 0;
            }

            if (mutables._currentRightMatches == null)
            {
                // We have to look up the next list of matches in the hash-table.
                Pair<TLeftInput, THashKey> leftPair = default(Pair<TLeftInput, THashKey>);
                TLeftKey leftKey = default(TLeftKey)!;
                while (_leftSource.MoveNext(ref leftPair, ref leftKey))
                {
                    if ((mutables._outputLoopCount++ & CancellationState.POLL_INTERVAL) == 0)
                        _cancellationToken.ThrowIfCancellationRequested();

                    // Find the match in the hash table.
                    HashLookupValueList<TRightInput, TRightKey> matchValue = default(HashLookupValueList<TRightInput, TRightKey>);
                    TLeftInput leftElement = leftPair.First;
                    THashKey leftHashKey = leftPair.Second;

                    // Ignore null keys.
                    if (leftHashKey != null)
                    {
                        Debug.Assert(mutables._rightHashLookup != null);
                        if (mutables._rightHashLookup.TryGetValue(leftHashKey, ref matchValue))
                        {
                            // We found a new match. We remember the list in case there are multiple
                            // values under this same key -- the next iteration will pick them up.
                            mutables._currentRightMatches = matchValue.Tail;
                            Debug.Assert(mutables._currentRightMatches == null || mutables._currentRightMatches.Count > 0,
                                            "we were expecting that the list would be either null or empty");
                            mutables._currentRightMatchesIndex = 0;

                            // Yield the value.
                            currentElement = _resultSelector(leftElement, matchValue.Head.First);
                            currentKey = _outputKeyBuilder.Combine(leftKey, matchValue.Head.Second);

                            // If there is a list of matches, remember the left values for next time.
                            if (matchValue.Tail != null)
                            {
                                mutables._currentLeft = leftElement;
                                mutables._currentLeftKey = leftKey;
                            }

                            return true;
                        }
                    }
                }

                // If we've reached the end of the data source, we're done.
                return false;
            }

            // Produce the next element.
            Debug.Assert(mutables._currentRightMatches != null);
            Debug.Assert(0 <= mutables._currentRightMatchesIndex && mutables._currentRightMatchesIndex < mutables._currentRightMatches.Count);

            Pair<TRightInput, TRightKey> rightMatch = mutables._currentRightMatches._chunk[mutables._currentRightMatchesIndex];

            currentElement = _resultSelector(mutables._currentLeft, rightMatch.First);
            currentKey = _outputKeyBuilder.Combine(mutables._currentLeftKey, rightMatch.Second);

            mutables._currentRightMatchesIndex++;

            return true;
        }

        protected override void Dispose(bool disposing)
        {
            Debug.Assert(_leftSource != null && _rightLookupBuilder != null);
            _leftSource.Dispose();
            _rightLookupBuilder.Dispose();
        }
    }

    /// <summary>
    /// A class to create output order keys from the left and right keys.
    /// </summary>
    /// <typeparam name="TLeftKey"></typeparam>
    /// <typeparam name="TRightKey"></typeparam>
    /// <typeparam name="TOutputKey"></typeparam>
    internal abstract class HashJoinOutputKeyBuilder<TLeftKey, TRightKey, TOutputKey>
    {
        public abstract TOutputKey Combine(TLeftKey leftKey, TRightKey rightKey);
    }

    /// <summary>
    /// A key builder that simply returns the left key, ignoring the right key.
    ///
    /// Used when the right source is unordered.
    /// </summary>
    /// <typeparam name="TLeftKey"></typeparam>
    /// <typeparam name="TRightKey"></typeparam>
    internal sealed class LeftKeyOutputKeyBuilder<TLeftKey, TRightKey> : HashJoinOutputKeyBuilder<TLeftKey, TRightKey, TLeftKey>
    {
        public override TLeftKey Combine(TLeftKey leftKey, TRightKey rightKey)
        {
            return leftKey;
        }
    }

    /// <summary>
    /// A key builder that simply returns a left key, right key pair.
    ///
    /// Used when the right source is ordered.
    /// </summary>
    /// <typeparam name="TLeftKey"></typeparam>
    /// <typeparam name="TRightKey"></typeparam>
    internal sealed class PairOutputKeyBuilder<TLeftKey, TRightKey> : HashJoinOutputKeyBuilder<TLeftKey, TRightKey, Pair<TLeftKey, TRightKey>>
    {
        public override Pair<TLeftKey, TRightKey> Combine(TLeftKey leftKey, TRightKey rightKey)
        {
            return new Pair<TLeftKey, TRightKey>(leftKey, rightKey);
        }
    }

    /// <summary>
    /// Class to build a HashJoinHashLookup of right elements for use in HashJoin operations.
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    /// <typeparam name="TOrderKey"></typeparam>
    /// <typeparam name="THashKey"></typeparam>
    internal abstract class HashLookupBuilder<TElement, TOrderKey, THashKey>
    {
        public abstract HashJoinHashLookup<THashKey, TElement, TOrderKey> BuildHashLookup(CancellationToken cancellationToken);

        protected static void BuildBaseHashLookup<TBaseBuilder, TBaseElement, TBaseOrderKey>(
            QueryOperatorEnumerator<Pair<TBaseElement, THashKey>, TBaseOrderKey> dataSource,
            TBaseBuilder baseHashBuilder,
            CancellationToken cancellationToken) where TBaseBuilder : IBaseHashBuilder<TBaseElement, TBaseOrderKey>
        {
            Debug.Assert(dataSource != null);

#if DEBUG
            int hashLookupCount = 0;
            int hashKeyCollisions = 0;
#endif

            Pair<TBaseElement, THashKey> currentPair = default(Pair<TBaseElement, THashKey>);
            TBaseOrderKey orderKey = default(TBaseOrderKey)!;
            int i = 0;
            while (dataSource.MoveNext(ref currentPair, ref orderKey))
            {
                if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                    cancellationToken.ThrowIfCancellationRequested();

                TBaseElement element = currentPair.First;
                THashKey hashKey = currentPair.Second;

                // We ignore null keys.
                if (hashKey != null)
                {
#if DEBUG
                    hashLookupCount++;
#endif

                    if (baseHashBuilder.Add(hashKey, element, orderKey))
                    {
#if DEBUG
                        hashKeyCollisions++;
#endif
                    }
                }
            }

#if DEBUG
            TraceHelpers.TraceInfo("HashLookupBuilder::BuildBaseHashLookup - built hash table [count = {0}, collisions = {1}]",
                hashLookupCount, hashKeyCollisions);
#endif
        }

        // Standard implementation of the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Used in BuildBaseHashLookup to translate from data in dataSource to data to be used
        /// by the HashJoin operator.
        /// </summary>
        /// <typeparam name="TBaseElement"></typeparam>
        /// <typeparam name="TBaseOrderKey"></typeparam>
        protected interface IBaseHashBuilder<TBaseElement, TBaseOrderKey>
        {
            // adds the value to the base HashLookup.
            // returns true if the addition is a hash collision
            bool Add(THashKey hashKey, TBaseElement element, TBaseOrderKey orderKey);
        }
    }

    /// <summary>
    /// A wrapper for the HashLookup returned by HashLookupBuilder.
    ///
    /// This will allow for providing a default if there is no value in the base lookup.
    /// </summary>
    /// <typeparam name="THashKey"></typeparam>
    /// <typeparam name="TElement"></typeparam>
    /// <typeparam name="TOrderKey"></typeparam>
    internal abstract class HashJoinHashLookup<THashKey, TElement, TOrderKey>
    {
        // get the current value if it exists.
        public abstract bool TryGetValue(THashKey key, ref HashLookupValueList<TElement, TOrderKey> value);
    }

    /// <summary>
    /// A list to handle one or more right elements of a join operation.
    ///
    /// It optimizes for 1 to 1 joins by only allocating heap space lazily
    /// once a second value is added.
    ///
    /// This is built in the HashLookupBuilder classes and consumed by the
    /// HashJoinQueryOperatorEnumerator.
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    /// <typeparam name="TOrderKey"></typeparam>
    internal struct HashLookupValueList<TElement, TOrderKey>
    {
        internal Pair<TElement, TOrderKey> Head
        {
            get
            {
                return _head;
            }
        }
        private readonly Pair<TElement, TOrderKey> _head;

        internal ListChunk<Pair<TElement, TOrderKey>>? Tail
        {
            get
            {
                return _tail;
            }
        }
        private ListChunk<Pair<TElement, TOrderKey>>? _tail;

        private const int INITIAL_CHUNK_SIZE = 2;

        // constructor used to build a new list.
        internal HashLookupValueList(TElement firstValue, TOrderKey firstOrderKey)
        {
            _head = CreatePair(firstValue, firstOrderKey);
            _tail = null;
        }

        /// <summary>
        /// Adds a value/ordering key pair to the list.
        /// </summary>
        /// <param name="value">value to add</param>
        /// <param name="orderKey">ordering key</param>
        /// <returns>if true, the internal memory has changed</returns>
        /// <remarks>
        /// As this is a value type, if the internal memory changes,
        /// then the changes need to be reflected (to a HashLookup, for example)
        /// as necessary
        /// </remarks>
        internal bool Add(TElement value, TOrderKey orderKey)
        {
            bool requiresMemoryChange = (_tail == null);

            if (requiresMemoryChange)
            {
                _tail = new ListChunk<Pair<TElement, TOrderKey>>(INITIAL_CHUNK_SIZE);
            }
            _tail!.Add(CreatePair(value, orderKey));

            return requiresMemoryChange;
        }

        private static Pair<TElement, TOrderKey> CreatePair(TElement value, TOrderKey orderKey)
        {
            return new Pair<TElement, TOrderKey>(value, orderKey);
        }
    }
}
