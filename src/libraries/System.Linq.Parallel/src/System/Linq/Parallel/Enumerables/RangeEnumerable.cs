// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// RangeEnumerable.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A simple enumerable type that implements the range algorithm. It also supports
    /// partitioning of the indices by implementing an interface that PLINQ recognizes.
    /// </summary>
    internal sealed class RangeEnumerable : ParallelQuery<int>, IParallelPartitionable<int>
    {
        private readonly int _from; // Lowest index to include.
        private readonly int _count; // Number of indices to include.

        //-----------------------------------------------------------------------------------
        // Constructs a new range enumerable object for the specified range.
        //

        internal RangeEnumerable(int from, int count)
            : base(QuerySettings.Empty)
        {
            // Transform the from and to indices into low and highs.
            _from = from;
            _count = count;
        }

        //-----------------------------------------------------------------------------------
        // Retrieves 'count' partitions, each of which uses a non-overlapping set of indices.
        //

        public QueryOperatorEnumerator<int, int>[] GetPartitions(int partitionCount)
        {
            // Calculate a stride size, avoiding overflow if _count is large
            int stride = _count / partitionCount;
            int biggerPartitionCount = _count % partitionCount;

            // Create individual partitions, carefully avoiding overflow
            int doneCount = 0;
            QueryOperatorEnumerator<int, int>[] partitions = new QueryOperatorEnumerator<int, int>[partitionCount];
            for (int i = 0; i < partitionCount; i++)
            {
                int partitionSize = (i < biggerPartitionCount) ? stride + 1 : stride;
                partitions[i] = new RangeEnumerator(
                    unchecked(_from + doneCount),
                    partitionSize,
                    doneCount);
                doneCount += partitionSize;
            }

            return partitions;
        }

        //-----------------------------------------------------------------------------------
        // Basic IEnumerator<T> method implementations.
        //

        public override IEnumerator<int> GetEnumerator()
        {
            return new RangeEnumerator(_from, _count, 0).AsClassicEnumerator();
        }

        //-----------------------------------------------------------------------------------
        // The actual enumerator that walks over the specified range.
        //

        private sealed class RangeEnumerator : QueryOperatorEnumerator<int, int>
        {
            private readonly int _from; // The initial value.
            private readonly int _count; // How many values to yield.
            private readonly int _initialIndex; // The ordinal index of the first value in the range.
            private Shared<int>? _currentCount; // The 0-based index of the current value. [allocate in moveNext to avoid false-sharing]

            //-----------------------------------------------------------------------------------
            // Creates a new enumerator.
            //

            internal RangeEnumerator(int from, int count, int initialIndex)
            {
                _from = from;
                _count = count;
                _initialIndex = initialIndex;
            }

            //-----------------------------------------------------------------------------------
            // Basic enumeration method. This implements the logic to walk the desired
            // range, using the step specified at construction time.
            //

            internal override bool MoveNext(ref int currentElement, ref int currentKey)
            {
                _currentCount ??= new Shared<int>(-1);

                // Calculate the next index and ensure it falls within our range.
                int nextCount = _currentCount.Value + 1;
                if (nextCount < _count)
                {
                    _currentCount.Value = nextCount;
                    currentElement = nextCount + _from;
                    currentKey = nextCount + _initialIndex;
                    return true;
                }

                return false;
            }

            internal override void Reset()
            {
                // We set the current value such that the next addition of step
                // results in the 1st real value in the range.
                _currentCount = null;
            }
        }
    }
}
