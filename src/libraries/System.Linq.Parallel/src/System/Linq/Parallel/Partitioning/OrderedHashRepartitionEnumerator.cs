// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// OrderedHashRepartitionEnumerator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq.Parallel
{
    /// <summary>
    /// This enumerator handles the actual coordination among partitions required to
    /// accomplish the repartitioning operation, as explained above.  In addition to that,
    /// it tracks order keys so that order preservation can flow through the enumerator.
    /// </summary>
    /// <typeparam name="TInputOutput">The kind of elements.</typeparam>
    /// <typeparam name="THashKey">The key used to distribute elements.</typeparam>
    /// <typeparam name="TOrderKey">The kind of keys found in the source.</typeparam>
    internal sealed class OrderedHashRepartitionEnumerator<TInputOutput, THashKey, TOrderKey> : QueryOperatorEnumerator<Pair<TInputOutput, THashKey>, TOrderKey>
    {
        private const int ENUMERATION_NOT_STARTED = -1; // Sentinel to note we haven't begun enumerating yet.

        private readonly int _partitionCount; // The number of partitions.
        private readonly int _partitionIndex; // Our unique partition index.
        private readonly Func<TInputOutput, THashKey>? _keySelector; // A key-selector function.
        private readonly HashRepartitionStream<TInputOutput, THashKey, TOrderKey> _repartitionStream; // A repartitioning stream.
        private readonly ListChunk<Pair<TInputOutput, THashKey>>[][] _valueExchangeMatrix; // Matrix to do inter-task communication of values.
        private readonly ListChunk<TOrderKey>[][] _keyExchangeMatrix; // Matrix to do inter-task communication of order keys.
        private readonly QueryOperatorEnumerator<TInputOutput, TOrderKey> _source; // The immediate source of data.
        private CountdownEvent _barrier; // Used to signal and wait for repartitions to complete.
        private readonly CancellationToken _cancellationToken; // A token for canceling the process.
        private Mutables? _mutables; // Mutable fields for this enumerator.

        private sealed class Mutables
        {
            internal int _currentBufferIndex; // Current buffer index.
            internal ListChunk<Pair<TInputOutput, THashKey>>? _currentBuffer; // The buffer we're currently enumerating.
            internal ListChunk<TOrderKey>? _currentKeyBuffer; // The buffer we're currently enumerating.
            internal int _currentIndex; // Current index into the buffer.

            internal Mutables()
            {
                _currentBufferIndex = ENUMERATION_NOT_STARTED;
            }
        }

        //---------------------------------------------------------------------------------------
        // Creates a new repartitioning enumerator.
        //
        // Arguments:
        //     source            - the data stream from which to pull elements
        //     useOrdinalOrderPreservation - whether order preservation is required
        //     partitionCount    - total number of partitions
        //     partitionIndex    - this operator's unique partition index
        //     repartitionStream - the stream object to use for partition selection
        //     barrier           - a latch used to signal task completion
        //     buffers           - a set of buffers for inter-task communication
        //

        internal OrderedHashRepartitionEnumerator(
            QueryOperatorEnumerator<TInputOutput, TOrderKey> source, int partitionCount, int partitionIndex,
            Func<TInputOutput, THashKey>? keySelector, OrderedHashRepartitionStream<TInputOutput, THashKey, TOrderKey> repartitionStream, CountdownEvent barrier,
            ListChunk<Pair<TInputOutput, THashKey>>[][] valueExchangeMatrix, ListChunk<TOrderKey>[][] keyExchangeMatrix, CancellationToken cancellationToken)
        {
            Debug.Assert(source != null);
            Debug.Assert(keySelector != null || typeof(THashKey) == typeof(NoKeyMemoizationRequired));
            Debug.Assert(repartitionStream != null);
            Debug.Assert(barrier != null);
            Debug.Assert(valueExchangeMatrix != null);
            Debug.Assert(valueExchangeMatrix.GetLength(0) == partitionCount, "expected square matrix of buffers (NxN)");
            Debug.Assert(partitionCount > 0 && valueExchangeMatrix[0].Length == partitionCount, "expected square matrix of buffers (NxN)");
            Debug.Assert(0 <= partitionIndex && partitionIndex < partitionCount);

            _source = source;
            _partitionCount = partitionCount;
            _partitionIndex = partitionIndex;
            _keySelector = keySelector;
            _repartitionStream = repartitionStream;
            _barrier = barrier;
            _valueExchangeMatrix = valueExchangeMatrix;
            _keyExchangeMatrix = keyExchangeMatrix;
            _cancellationToken = cancellationToken;
        }

        //---------------------------------------------------------------------------------------
        // Retrieves the next element from this partition.  All repartitioning operators across
        // all partitions cooperate in a barrier-style algorithm.  The first time an element is
        // requested, the repartitioning operator will enter the 1st phase: during this phase, it
        // scans its entire input and compute the destination partition for each element.  During
        // the 2nd phase, each partition scans the elements found by all other partitions for
        // it, and yield this to callers.  The only synchronization required is the barrier itself
        // -- all other parts of this algorithm are synchronization-free.
        //
        // Notes: One rather large penalty that this algorithm incurs is higher memory usage and a
        // larger time-to-first-element latency, at least compared with our old implementation; this
        // happens because all input elements must be fetched before we can produce a single output
        // element.  In many cases this isn't too terrible: e.g. a GroupBy requires this to occur
        // anyway, so having the repartitioning operator do so isn't complicating matters much at all.
        //

        internal override bool MoveNext(ref Pair<TInputOutput, THashKey> currentElement, [AllowNull] ref TOrderKey currentKey)
        {
            if (_partitionCount == 1)
            {
                TInputOutput current = default(TInputOutput)!;

                // If there's only one partition, no need to do any sort of exchanges.
                if (_source.MoveNext(ref current!, ref currentKey))
                {
                    currentElement = new Pair<TInputOutput, THashKey>(
                        current, _keySelector == null ? default! : _keySelector(current));
                    return true;
                }

                return false;
            }

            Debug.Assert(!ParallelEnumerable.SinglePartitionMode);

            Mutables mutables = _mutables ??= new Mutables();

            // If we haven't enumerated the source yet, do that now.  This is the first phase
            // of a two-phase barrier style operation.
            if (mutables._currentBufferIndex == ENUMERATION_NOT_STARTED)
            {
                EnumerateAndRedistributeElements();
                Debug.Assert(mutables._currentBufferIndex != ENUMERATION_NOT_STARTED);
            }

            // Once we've enumerated our contents, we can then go back and walk the buffers that belong
            // to the current partition.  This is phase two.  Note that we slyly move on to the first step
            // of phase two before actually waiting for other partitions.  That's because we can enumerate
            // the buffer we wrote to above, as already noted.
            while (mutables._currentBufferIndex < _partitionCount)
            {
                // If the queue is non-null and still has elements, yield them.
                if (mutables._currentBuffer != null)
                {
                    Debug.Assert(mutables._currentKeyBuffer != null);

                    if (++mutables._currentIndex < mutables._currentBuffer.Count)
                    {
                        // Return the current element.
                        currentElement = mutables._currentBuffer._chunk[mutables._currentIndex];
                        Debug.Assert(mutables._currentKeyBuffer != null, "expected same # of buffers/key-buffers");
                        currentKey = mutables._currentKeyBuffer._chunk[mutables._currentIndex];
                        return true;
                    }
                    else
                    {
                        // If the chunk is empty, advance to the next one (if any).
                        mutables._currentIndex = ENUMERATION_NOT_STARTED;
                        mutables._currentBuffer = mutables._currentBuffer.Next;
                        mutables._currentKeyBuffer = mutables._currentKeyBuffer.Next;
                        Debug.Assert(mutables._currentBuffer == null || mutables._currentBuffer.Count > 0);
                        Debug.Assert((mutables._currentBuffer == null) == (mutables._currentKeyBuffer == null));
                        Debug.Assert(mutables._currentBuffer == null || mutables._currentBuffer.Count == mutables._currentKeyBuffer!.Count);
                        continue; // Go back around and invoke this same logic.
                    }
                }

                // We're done with the current partition.  Slightly different logic depending on whether
                // we're on our own buffer or one that somebody else found for us.
                if (mutables._currentBufferIndex == _partitionIndex)
                {
                    // We now need to wait at the barrier, in case some other threads aren't done.
                    // Once we wake up, we reset our index and will increment it immediately after.
                    _barrier.Wait(_cancellationToken);
                    mutables._currentBufferIndex = ENUMERATION_NOT_STARTED;
                }

                // Advance to the next buffer.
                mutables._currentBufferIndex++;
                mutables._currentIndex = ENUMERATION_NOT_STARTED;

                if (mutables._currentBufferIndex == _partitionIndex)
                {
                    // Skip our current buffer (since we already enumerated it).
                    mutables._currentBufferIndex++;
                }

                // Assuming we're within bounds, retrieve the next buffer object.
                if (mutables._currentBufferIndex < _partitionCount)
                {
                    mutables._currentBuffer = _valueExchangeMatrix[mutables._currentBufferIndex][_partitionIndex];
                    mutables._currentKeyBuffer = _keyExchangeMatrix[mutables._currentBufferIndex][_partitionIndex];
                }
            }

            // We're done. No more buffers to enumerate.
            return false;
        }

        //---------------------------------------------------------------------------------------
        // Called when this enumerator is first enumerated; it must walk through the source
        // and redistribute elements to their slot in the exchange matrix.
        //

        private void EnumerateAndRedistributeElements()
        {
            Mutables? mutables = _mutables;
            Debug.Assert(mutables != null);

            ListChunk<Pair<TInputOutput, THashKey>>[] privateBuffers = new ListChunk<Pair<TInputOutput, THashKey>>[_partitionCount];
            ListChunk<TOrderKey>[] privateKeyBuffers = new ListChunk<TOrderKey>[_partitionCount];

            TInputOutput element = default(TInputOutput)!;
            TOrderKey key = default(TOrderKey)!;
            int loopCount = 0;
            while (_source.MoveNext(ref element!, ref key))
            {
                if ((loopCount++ & CancellationState.POLL_INTERVAL) == 0)
                    _cancellationToken.ThrowIfCancellationRequested();

                // Calculate the element's destination partition index, placing it into the
                // appropriate buffer from which partitions will later enumerate.
                int destinationIndex;
                THashKey elementHashKey = default(THashKey)!;
                if (_keySelector != null)
                {
                    elementHashKey = _keySelector(element);
                    destinationIndex = _repartitionStream.GetHashCode(elementHashKey) % _partitionCount;
                }
                else
                {
                    Debug.Assert(typeof(THashKey) == typeof(NoKeyMemoizationRequired));
                    destinationIndex = _repartitionStream.GetHashCode(element) % _partitionCount;
                }

                Debug.Assert(0 <= destinationIndex && destinationIndex < _partitionCount,
                                "destination partition outside of the legal range of partitions");

                // Get the buffer for the destination partition, lazily allocating if needed.  We maintain
                // this list in our own private cache so that we avoid accessing shared memory locations
                // too much.  In the original implementation, we'd access the buffer in the matrix ([N,M],
                // where N is the current partition and M is the destination), but some rudimentary
                // performance profiling indicates copying at the end performs better.
                ListChunk<Pair<TInputOutput, THashKey>> buffer = privateBuffers[destinationIndex];
                ListChunk<TOrderKey> keyBuffer = privateKeyBuffers[destinationIndex];
                if (buffer == null)
                {
                    const int INITIAL_PRIVATE_BUFFER_SIZE = 128;
                    Debug.Assert(keyBuffer == null);
                    privateBuffers[destinationIndex] = buffer = new ListChunk<Pair<TInputOutput, THashKey>>(INITIAL_PRIVATE_BUFFER_SIZE);
                    privateKeyBuffers[destinationIndex] = keyBuffer = new ListChunk<TOrderKey>(INITIAL_PRIVATE_BUFFER_SIZE);
                }

                buffer.Add(new Pair<TInputOutput, THashKey>(element, elementHashKey));
                keyBuffer.Add(key);
            }

            // Copy the local buffers to the shared space and then signal to other threads that
            // we are done.  We can then immediately move on to enumerating the elements we found
            // for the current partition before waiting at the barrier.  If we found a lot, we will
            // hopefully never have to physically wait.
            for (int i = 0; i < _partitionCount; i++)
            {
                _valueExchangeMatrix[_partitionIndex][i] = privateBuffers[i];
                _keyExchangeMatrix[_partitionIndex][i] = privateKeyBuffers[i];
            }

            _barrier.Signal();

            // Begin at our own buffer.
            mutables._currentBufferIndex = _partitionIndex;
            mutables._currentBuffer = privateBuffers[_partitionIndex];
            mutables._currentKeyBuffer = privateKeyBuffers[_partitionIndex];
            mutables._currentIndex = ENUMERATION_NOT_STARTED;
        }

        protected override void Dispose(bool disposing)
        {
            if (_barrier != null)
            {
                // Since this enumerator is being disposed, we will decrement the barrier,
                // in case other enumerators will wait on the barrier.
                if (_mutables == null || (_mutables._currentBufferIndex == ENUMERATION_NOT_STARTED))
                {
                    _barrier.Signal();
                    _barrier = null!;
                }

                _source.Dispose();
            }
        }
    }
}
