// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// Sorting.cs
//
// Support for sorting.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace System.Linq.Parallel
{
    //---------------------------------------------------------------------------------------
    // The sort helper abstraction hides the implementation of our parallel merge sort.  See
    // comments below for more details.  In summary, there will be one sort helper per
    // partition.  Each will, in parallel read the whole key/value set from its input,
    // perform a local sort on this data, and then cooperatively merge with other concurrent
    // tasks to generate a single sorted output.  The local sort step is done using a simple
    // quick-sort algorithm.  Then we use a log(p) reduction to perform merges in parallel;
    // during each round of merges, half of the threads will stop doing work and may return.
    // At the end, one thread will remain and it holds the final sorted output.
    //

    internal abstract class SortHelper<TInputOutput>
    {
        internal abstract TInputOutput[] Sort();
    }

    internal class SortHelper<TInputOutput, TKey> : SortHelper<TInputOutput>, IDisposable
    {
        private QueryOperatorEnumerator<TInputOutput, TKey> _source; // The data source from which to pull data.
        private int _partitionCount; // The partition count.
        private int _partitionIndex; // This helper's index.

        // This data is shared among all partitions.
        private QueryTaskGroupState _groupState; // To communicate status, e.g. cancellation.
        private int[][] _sharedIndices; // Shared set of indices used during sorting.
        private GrowingArray<TKey>[] _sharedKeys; // Shared keys with which to compare elements.
        private TInputOutput[][] _sharedValues; // The actual values used for comparisons.
        private Barrier[][] _sharedBarriers; // A matrix of barriers used for synchronizing during merges.
        private OrdinalIndexState _indexState; // State of the order index
        private IComparer<TKey> _keyComparer; // Comparer for the order keys

        //---------------------------------------------------------------------------------------
        // Creates a single sort helper object.  This is marked private to ensure the only
        // snippet of code that creates one is the factory, since creating many implies some
        // implementation detail in terms of dependencies which other places in the codebase
        // shouldn't need to worry about.
        //

        private SortHelper(QueryOperatorEnumerator<TInputOutput, TKey> source, int partitionCount, int partitionIndex,
            QueryTaskGroupState groupState, int[][] sharedIndices,
            OrdinalIndexState indexState, IComparer<TKey> keyComparer,
            GrowingArray<TKey>[] sharedkeys, TInputOutput[][] sharedValues, Barrier[][] sharedBarriers)
        {
            Debug.Assert(source != null);
            Debug.Assert(groupState != null);
            Debug.Assert(sharedIndices != null);
            Debug.Assert(sharedkeys != null);
            Debug.Assert(sharedValues != null);
            Debug.Assert(sharedBarriers != null);
            Debug.Assert(sharedIndices.Length <= sharedkeys.Length);
            Debug.Assert(sharedIndices.Length == sharedValues.Length);
            // Multi-dim arrays are simulated using jagged arrays.
            // Because of that, when phaseCount == 0, we end up with an empty sharedBarrier array.
            // Since there are no cases when phaseCount == 0 where we actually access the sharedBarriers, I am removing this check.
            // Debug.Assert(sharedIndices.Length == sharedBarriers[0].Length);

            _source = source;
            _partitionCount = partitionCount;
            _partitionIndex = partitionIndex;
            _groupState = groupState;
            _sharedIndices = sharedIndices;
            _indexState = indexState;
            _keyComparer = keyComparer;
            _sharedKeys = sharedkeys;
            _sharedValues = sharedValues;
            _sharedBarriers = sharedBarriers;

            Debug.Assert(_sharedKeys.Length >= _sharedValues.Length);
        }

        //---------------------------------------------------------------------------------------
        // Factory method to create a bunch of sort helpers that are all related.  Once created,
        // these helpers must all run concurrently with one another.
        //
        // Arguments:
        //     partitions    - the input data partitions to be sorted
        //     groupState    - common state used for communication (e.g. cancellation)
        //
        // Return Value:
        //     An array of helpers, one for each partition.
        //

        internal static SortHelper<TInputOutput, TKey>[] GenerateSortHelpers(
            PartitionedStream<TInputOutput, TKey> partitions, QueryTaskGroupState groupState)
        {
            int degreeOfParallelism = partitions.PartitionCount;
            SortHelper<TInputOutput, TKey>[] helpers = new SortHelper<TInputOutput, TKey>[degreeOfParallelism];

            // Calculate the next highest power of two greater than or equal to the DOP.
            // Also, calculate phaseCount = log2(degreeOfParallelismPow2)
            int degreeOfParallelismPow2 = 1, phaseCount = 0;
            while (degreeOfParallelismPow2 < degreeOfParallelism)
            {
                phaseCount++;
                degreeOfParallelismPow2 <<= 1;
            }

            // Initialize shared objects used during sorting.
            int[][] sharedIndices = new int[degreeOfParallelism][];
            GrowingArray<TKey>[] sharedKeys = new GrowingArray<TKey>[degreeOfParallelism];
            TInputOutput[][] sharedValues = new TInputOutput[degreeOfParallelism][];
            // Note that it is possible that phaseCount is 0.
            Barrier[][] sharedBarriers = JaggedArray<Barrier>.Allocate(phaseCount, degreeOfParallelism);

            if (degreeOfParallelism > 1)
            {
                // Initialize the barriers we need.  Due to the logarithmic reduction, we don't
                // need to populate the whole matrix.
                int offset = 1;
                for (int i = 0; i < sharedBarriers.Length; i++)
                {
                    // We have jagged arrays.
                    for (int j = 0; j < sharedBarriers[i].Length; j++)
                    {
                        // As the phases increase, the barriers required become more and more sparse.
                        if ((j % offset) == 0)
                        {
                            sharedBarriers[i][j] = new Barrier(2);
                        }
                    }
                    offset *= 2;
                }
            }

            // Lastly populate the array of sort helpers.
            for (int i = 0; i < degreeOfParallelism; i++)
            {
                helpers[i] = new SortHelper<TInputOutput, TKey>(
                    partitions[i], degreeOfParallelism, i,
                    groupState, sharedIndices,
                    partitions.OrdinalIndexState, partitions.KeyComparer,
                    sharedKeys, sharedValues, sharedBarriers);
            }

            return helpers;
        }

        //---------------------------------------------------------------------------------------
        // Disposes of this sort helper's expensive state.
        //

        public void Dispose()
        {
            // We only dispose of the barriers when the 1st partition finishes.  That's because
            // all others depend on the shared barriers, so we can't get rid of them eagerly.
            if (_partitionIndex == 0)
            {
                for (int i = 0; i < _sharedBarriers.Length; i++)
                {
                    for (int j = 0; j < _sharedBarriers[i].Length; j++)
                    {
                        Barrier b = _sharedBarriers[i][j];
                        if (b != null)
                        {
                            b.Dispose();
                        }
                    }
                }
            }
        }

        //---------------------------------------------------------------------------------------
        // Sorts the data, possibly returning a result.
        //
        // Notes:
        //     This method makes some pretty fundamental assumptions about what concurrency
        //     exists in the system.  Namely, it assumes all SortHelpers are running in
        //     parallel.  If they aren't Sort will end up waiting for certain events that
        //     will never happen -- i.e. we will deadlock.
        //

        internal override TInputOutput[] Sort()
        {
            // Step 1.  Accumulate this partitions' worth of input.
            GrowingArray<TKey> sourceKeys = null;
            List<TInputOutput> sourceValues = null;

            BuildKeysFromSource(ref sourceKeys, ref sourceValues);

            Debug.Assert(sourceValues != null, "values weren't populated");
            Debug.Assert(sourceKeys != null, "keys weren't populated");

            // Step 2.  Locally sort this partition's key indices in-place.
            QuickSortIndicesInPlace(sourceKeys, sourceValues, _indexState);

            // Step 3. Enter into the merging phases, each separated by several barriers.
            if (_partitionCount > 1)
            {
                // We only need to merge if there is more than 1 partition.
                MergeSortCooperatively();
            }

            return _sharedValues[_partitionIndex];
        }

        //-----------------------------------------------------------------------------------
        // Generates a list of values and keys from the data source.  After calling this,
        // the keys and values lists will be populated; each key at index i corresponds to
        // the value at index i in the other list.
        //
        // Notes:
        //    Should only be called once per sort helper.
        //

        private void BuildKeysFromSource(ref GrowingArray<TKey> keys, ref List<TInputOutput> values)
        {
            values = new List<TInputOutput>();

            // Enumerate the whole input set, generating a key set in the process.
            CancellationToken cancelToken = _groupState.CancellationState.MergedCancellationToken;
            try
            {
                TInputOutput current = default(TInputOutput);
                TKey currentKey = default(TKey);
                bool hadNext = _source.MoveNext(ref current, ref currentKey);

                if (keys == null)
                {
                    keys = new GrowingArray<TKey>();
                }

                if (hadNext)
                {
                    int i = 0;
                    do
                    {
                        if ((i++ & CancellationState.POLL_INTERVAL) == 0)
                            CancellationState.ThrowIfCanceled(cancelToken);

                        // Accumulate the keys and values so that we can sort them in a moment.
                        keys.Add(currentKey);
                        values.Add(current);
                    }
                    while (_source.MoveNext(ref current, ref currentKey));
                }
            }
            finally
            {
                _source.Dispose();
            }
        }

        //-----------------------------------------------------------------------------------
        // Produces a list of indices and sorts them in place using a local sort.
        //
        // Notes:
        //     Each element in the indices array is an index which refers to an element in
        //     the key/value array.  After calling this routine, the indices will be ordered
        //     such that the keys they refer to are in ascending or descending order,
        //     according to the sort criteria used.
        //

        private void QuickSortIndicesInPlace(GrowingArray<TKey> keys, List<TInputOutput> values, OrdinalIndexState ordinalIndexState)
        {
            Debug.Assert(keys != null);
            Debug.Assert(values != null);
            Debug.Assert(keys.Count == values.Count);

            // Generate a list of keys in forward order.  We will sort them in a moment.
            int[] indices = new int[values.Count];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = i;
            }

            // Now sort the indices in place.
            if (indices.Length > 1
                && ordinalIndexState.IsWorseThan(OrdinalIndexState.Increasing))
            {
                QuickSort(0, indices.Length - 1, keys.InternalArray, indices, _groupState.CancellationState.MergedCancellationToken);
            }

            if (_partitionCount == 1)
            {
                // If there is only one partition, we will produce the final value set now,
                // since there will be no merge afterward (which is where we usually do this).
                TInputOutput[] sortedValues = new TInputOutput[values.Count];
                for (int i = 0; i < indices.Length; i++)
                {
                    sortedValues[i] = values[indices[i]];
                }
                _sharedValues[_partitionIndex] = sortedValues;
            }
            else
            {
                // Otherwise, a merge will happen.  Generate the shared data structures.
                _sharedIndices[_partitionIndex] = indices;
                _sharedKeys[_partitionIndex] = keys;
                _sharedValues[_partitionIndex] = new TInputOutput[values.Count];

                // Copy local structures to shared space.
                values.CopyTo(_sharedValues[_partitionIndex]);
            }
        }

        //-----------------------------------------------------------------------------------
        // Works cooperatively with other concurrent sort helpers to produce a final sorted
        // output list of data.  Here is an overview of the algorithm used.
        //
        // During each phase, we must communicate with a partner task.  As a simple
        // illustration, imagine we have 8 partitions (P=8), numbered 0-7.  There will be
        // Log2(O)+2 phases (separated by barriers), where O is the next power of two greater
        // than or equal to P, in the sort operation:
        //
        //     Pairs:   (P = 8)
        //        phase=L:     [0][1] [2][3] [4][5] [6][7]
        //        phase=0:     [0,1]  [2,3]  [4,5]  [6,7]
        //        phase=1:     [0,2]         [4,6]
        //        phase=2:     [0,4]
        //        phase=M:     [0]
        //
        // During phase L, each partition locally sorts its data.  Then, at each subsequent
        // phase in the logarithmic reduction, two partitions are paired together and cooperate
        // to accomplish a portion of the merge.  The left one then goes on to choose another
        // partner, in the next phase, and the right one exits.  And so on, until phase M, when
        // there is just one partition left (the 0th), which is when it may publish the final
        // output from the sort operation.
        //
        // Notice we mentioned rounding up to the next power of two when determining the number
        // of phases.  Values of P which aren't powers of 2 are slightly problematic, because
        // they create a load imbalance in one of the partitions and heighten the depth of the
        // logarithmic tree.  As an illustration, imagine this case:
        //
        //     Pairs:   (P = 5)
        //        phase=L:    [0][1] [2][3] [4]
        //        phase=0:    [0,1]  [2,3]  [4,X]  [X,X]
        //        phase=1:    [0,2]         [4,X]
        //        phase=2:    [0,4]
        //        phase=M:    [0]
        //
        // Partition #4 in this example performs its local sort during phase L, but then has nothing
        // to do during phases 0 and 2.  (I.e. it has nobody to merge with.)  Only during phase 2
        // does it then resume work and help phase 2 perform its merge.  This is modeled a bit like
        // there were actually 8 partitions, which is the next power of two greater than or equal to
        // 5.  This example was chosen as an extreme case of imbalance.  We stall a processor (the 5th)
        // for two complete phases.  If P = 6 or 7, the problem would not be nearly so bad, but if
        // P = 9, the last partition would stall for yet another phase (and so on for every power of
        // two boundary).  We handle these, cases, but note that an overabundance of them will probably
        // negatively impact speedups.
        //

        private void MergeSortCooperatively()
        {
            CancellationToken cancelToken = _groupState.CancellationState.MergedCancellationToken;

            int phaseCount = _sharedBarriers.Length;
            for (int phase = 0; phase < phaseCount; phase++)
            {
                bool isLastPhase = (phase == (phaseCount - 1));

                // Calculate our partner for this phase and the next.
                int partnerIndex = ComputePartnerIndex(phase);

                // If we have a partner (see above for non power of 2 cases and why the index returned might
                // be out of bounds), we will coordinate with the partner to produce the merged output.
                if (partnerIndex < _partitionCount)
                {
                    // Cache references to our local data.
                    int[] myIndices = _sharedIndices[_partitionIndex];
                    GrowingArray<TKey> myKeys = _sharedKeys[_partitionIndex];
                    TKey[] myKeysArr = myKeys.InternalArray;

                    TInputOutput[] myValues = _sharedValues[_partitionIndex];


                    // First we must rendezvous with our merge partner so we know the previous sort
                    // and merge phase has been completed.  By convention, we always use the left-most
                    // partner's barrier for this; all that matters is that both uses the same.
                    _sharedBarriers[phase][Math.Min(_partitionIndex, partnerIndex)].SignalAndWait(cancelToken);

                    // Grab the two sorted inputs and then merge them cooperatively into one list.  One
                    // worker merges from left-to-right until it's placed elements up to the half-way
                    // point, and the other worker does the same, but only from right-to-left.
                    if (_partitionIndex < partnerIndex)
                    {
                        // Before moving on to the actual merge, the left-most partition will allocate data
                        // to hold the merged indices and key/value pairs.

                        // First, remember a copy of all of the partner's lists.
                        int[] rightIndices = _sharedIndices[partnerIndex];
                        TKey[] rightKeys = _sharedKeys[partnerIndex].InternalArray;
                        TInputOutput[] rightValues = _sharedValues[partnerIndex];

                        // We copy the our own items into the right's (overwriting its values) so that it can
                        // retrieve them after the barrier.  This is an exchange operation.
                        _sharedIndices[partnerIndex] = myIndices;
                        _sharedKeys[partnerIndex] = myKeys;
                        _sharedValues[partnerIndex] = myValues;

                        int leftCount = myValues.Length;
                        int rightCount = rightValues.Length;
                        int totalCount = leftCount + rightCount;

                        // Now allocate the lists into which the merged data will go.  Share this
                        // with the other thread so that it can place data into it as well.
                        int[] mergedIndices = null;
                        TInputOutput[] mergedValues = new TInputOutput[totalCount];

                        // Only on the last phase do we need to remember indices and keys.
                        if (!isLastPhase)
                        {
                            mergedIndices = new int[totalCount];
                        }

                        // Publish our newly allocated merged data structures.
                        _sharedIndices[_partitionIndex] = mergedIndices;
                        _sharedKeys[_partitionIndex] = myKeys;
                        _sharedValues[_partitionIndex] = mergedValues;

                        Debug.Assert(myKeysArr != null);

                        _sharedBarriers[phase][_partitionIndex].SignalAndWait(cancelToken);

                        // Merge the left half into the shared merged space.  This is a normal merge sort with
                        // the caveat that we stop merging once we reach the half-way point (since our partner
                        // is doing the same for the right half).  Note that during the last phase we only
                        // copy the values and not the indices or keys.
                        int m = (totalCount + 1) / 2;
                        int i = 0, j0 = 0, j1 = 0;
                        while (i < m)
                        {
                            if ((i & CancellationState.POLL_INTERVAL) == 0)
                                CancellationState.ThrowIfCanceled(cancelToken);

                            if (j0 < leftCount && (j1 >= rightCount ||
                                                   _keyComparer.Compare(myKeysArr[myIndices[j0]],
                                                                         rightKeys[rightIndices[j1]]) <= 0))
                            {
                                if (isLastPhase)
                                {
                                    mergedValues[i] = myValues[myIndices[j0]];
                                }
                                else
                                {
                                    mergedIndices[i] = myIndices[j0];
                                }
                                j0++;
                            }
                            else
                            {
                                if (isLastPhase)
                                {
                                    mergedValues[i] = rightValues[rightIndices[j1]];
                                }
                                else
                                {
                                    mergedIndices[i] = leftCount + rightIndices[j1];
                                }
                                j1++;
                            }
                            i++;
                        }

                        // If it's not the last phase, we just bulk propagate the keys and values.
                        if (!isLastPhase && leftCount > 0)
                        {
                            Array.Copy(myValues, 0, mergedValues, 0, leftCount);
                        }

                        // And now just wait for the second half.  We never reuse the same barrier across multiple
                        // phases, so we can always dispose of it when we wake up.
                        _sharedBarriers[phase][_partitionIndex].SignalAndWait(cancelToken);
                    }
                    else
                    {
                        // Wait for the other partition to allocate the shared data.
                        _sharedBarriers[phase][partnerIndex].SignalAndWait(cancelToken);

                        // After the barrier, the other partition will have made two things available to us:
                        // (1) its own indices, keys, and values, stored in the cell that used to hold our data,
                        // and (2) the arrays into which merged data will go, stored in its shared array cells.  
                        // We will snag references to all of these things.
                        int[] leftIndices = _sharedIndices[_partitionIndex];
                        TKey[] leftKeys = _sharedKeys[_partitionIndex].InternalArray;
                        TInputOutput[] leftValues = _sharedValues[_partitionIndex];
                        int[] mergedIndices = _sharedIndices[partnerIndex];
                        GrowingArray<TKey> mergedKeys = _sharedKeys[partnerIndex];
                        TInputOutput[] mergedValues = _sharedValues[partnerIndex];

                        Debug.Assert(leftValues != null);
                        Debug.Assert(leftKeys != null);

                        int leftCount = leftValues.Length;
                        int rightCount = myValues.Length;
                        int totalCount = leftCount + rightCount;

                        // Merge the right half into the shared merged space.  This is a normal merge sort with
                        // the caveat that we stop merging once we reach the half-way point (since our partner
                        // is doing the same for the left half).  Note that during the last phase we only
                        // copy the values and not the indices or keys.
                        int m = (totalCount + 1) / 2;
                        int i = totalCount - 1, j0 = leftCount - 1, j1 = rightCount - 1;
                        while (i >= m)
                        {
                            if ((i & CancellationState.POLL_INTERVAL) == 0)
                                CancellationState.ThrowIfCanceled(cancelToken);

                            if (j0 >= 0 && (j1 < 0 ||
                                            _keyComparer.Compare(leftKeys[leftIndices[j0]],
                                                                  myKeysArr[myIndices[j1]]) > 0))
                            {
                                if (isLastPhase)
                                {
                                    mergedValues[i] = leftValues[leftIndices[j0]];
                                }
                                else
                                {
                                    mergedIndices[i] = leftIndices[j0];
                                }
                                j0--;
                            }
                            else
                            {
                                if (isLastPhase)
                                {
                                    mergedValues[i] = myValues[myIndices[j1]];
                                }
                                else
                                {
                                    mergedIndices[i] = leftCount + myIndices[j1];
                                }
                                j1--;
                            }
                            i--;
                        }

                        // If it's not the last phase, we just bulk propagate the keys and values.
                        if (!isLastPhase && myValues.Length > 0)
                        {
                            mergedKeys.CopyFrom(myKeysArr, myValues.Length);
                            Array.Copy(myValues, 0, mergedValues, leftCount, myValues.Length);
                        }

                        // Wait for our partner to finish copying too.
                        _sharedBarriers[phase][partnerIndex].SignalAndWait(cancelToken);

                        // Now the greater of the two partners can leave, it's done.
                        break;
                    }
                }
            }
        }

        //---------------------------------------------------------------------------------------
        // Computes our partner index given the logarithmic reduction algorithm specified above.
        //

        private int ComputePartnerIndex(int phase)
        {
            int offset = 1 << phase;
            return _partitionIndex + ((_partitionIndex % (offset * 2)) == 0 ? offset : -offset);
        }

        //---------------------------------------------------------------------------------------
        // Sort algorithm used to sort key/value lists. After this has been called, the indices
        // will have been placed in sorted order based on the keys provided.
        //

        private void QuickSort(int left, int right, TKey[] keys, int[] indices, CancellationToken cancelToken)
        {
            Debug.Assert(keys != null, "need a non-null keyset");
            Debug.Assert(keys.Length >= indices.Length);
            Debug.Assert(left <= right);
            Debug.Assert(0 <= left && left < keys.Length);
            Debug.Assert(0 <= right && right < keys.Length);

            // cancellation check.
            // only test for intervals that are wider than so many items, else this test is 
            // relatively expensive compared to the work being performed.
            if (right - left > CancellationState.POLL_INTERVAL)
                CancellationState.ThrowIfCanceled(cancelToken);

            do
            {
                int i = left;
                int j = right;
                int pivot = indices[i + ((j - i) >> 1)];
                TKey pivotKey = keys[pivot];

                do
                {
                    while (_keyComparer.Compare(keys[indices[i]], pivotKey) < 0) i++;
                    while (_keyComparer.Compare(keys[indices[j]], pivotKey) > 0) j--;

                    Debug.Assert(i >= left && j <= right, "(i>=left && j<=right) sort failed - bogus IComparer?");

                    if (i > j)
                    {
                        break;
                    }

                    if (i < j)
                    {
                        // Swap the indices.
                        int tmp = indices[i];
                        indices[i] = indices[j];
                        indices[j] = tmp;
                    }

                    i++;
                    j--;
                }
                while (i <= j);

                if (j - left <= right - i)
                {
                    if (left < j)
                    {
                        QuickSort(left, j, keys, indices, cancelToken);
                    }
                    left = i;
                }
                else
                {
                    if (i < right)
                    {
                        QuickSort(i, right, keys, indices, cancelToken);
                    }
                    right = j;
                }
            }
            while (left < right);
        }
    }
}
