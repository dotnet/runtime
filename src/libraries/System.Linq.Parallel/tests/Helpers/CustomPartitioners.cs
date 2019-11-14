// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel.Tests
{
    // This class is meant to be used with partitioners that do load balancing, but _not_ buffering.
    // Feeding it a buffering partitioner will result in incorrect results!
    internal sealed class StrictPartitioner<T> : Partitioner<T>
    {
        private Partitioner<T> _source;
        private int _count;

        public StrictPartitioner(Partitioner<T> source, int count)
        {
            _source = source;
            _count = count;
        }

        public override IList<IEnumerator<T>> GetPartitions(int partitionCount)
        {
            return _source.GetPartitions(partitionCount).Select((partition, index) => new StrictPartitionerEnumerator(partition, partitionCount, index, _count)).Cast<IEnumerator<T>>().ToList();
        }

        private class StrictPartitionerEnumerator : IEnumerator<T>
        {
            private IEnumerator<T> _partition;
            private int _elements;

            public StrictPartitionerEnumerator(IEnumerator<T> partition, int partitionCount, int index, int count)
            {
                _partition = partition;
                _elements = Math.Min((count - 1) / partitionCount + 1, count - index * ((count - 1) / partitionCount + 1));
            }

            public T Current
            {
                get
                {
                    if (_elements >= 0)
                    {
                        return _partition.Current;
                    }
                    throw new InvalidOperationException("Out of elements");
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public void Dispose()
            {
                _partition.Dispose();
            }

            public bool MoveNext()
            {
                if (_elements > 0)
                {
                    _elements--;
                    return _partition.MoveNext();
                }
                return false;
            }

            public void Reset()
            {
                _partition.Reset();
            }
        }
    }

    internal sealed class RangeOrderablePartitioner : OrderablePartitioner<int>
    {
        private readonly int _start;
        private readonly int _count;
        private readonly bool _keysOrderedInEachPartition;
        private readonly bool _keysNormalized;

        public RangeOrderablePartitioner(int start, int count, bool keysOrderedInEachPartition, bool keysNormalized)
            : base(keysOrderedInEachPartition, false, keysNormalized)
        {
            _start = start;
            _count = count;
            _keysOrderedInEachPartition = keysOrderedInEachPartition;
            _keysNormalized = keysNormalized;
        }

        public override IList<IEnumerator<KeyValuePair<long, int>>> GetOrderablePartitions(int partitionCount)
        {
            IEnumerator<KeyValuePair<long, int>>[] partitions = new IEnumerator<KeyValuePair<long, int>>[partitionCount];
            int partitionSize = Math.Max(1, (int)Math.Ceiling(_count / (double)partitionCount));
            for (int i = 0; i < partitionCount; i++)
            {
                int start = partitionSize * i;
                int count = Math.Max(0, Math.Min(_count - start, partitionSize));
                partitions[i] = Enumerable.Range(start, count).Select(elemIndex =>
                {
                    if (!_keysOrderedInEachPartition)
                    {
                        elemIndex = _count - 1 - elemIndex;
                    }
                    long key = _keysNormalized ? elemIndex : (elemIndex * 2);
                    return new KeyValuePair<long, int>(key, _start + elemIndex);
                }).GetEnumerator();
            }
            return partitions;
        }

        public override IEnumerable<KeyValuePair<long, int>> GetOrderableDynamicPartitions()
        {
            return new RangeDynamicPartitions(_start, _count, _keysOrderedInEachPartition, _keysNormalized);
        }

        private sealed class RangeDynamicPartitions : IEnumerable<KeyValuePair<long, int>>
        {
            private readonly int _start;
            private readonly int _count;
            private readonly bool _keysOrderedInEachPartition;
            private readonly bool _keysNormalized;
            private int _pos = 0;

            internal RangeDynamicPartitions(int start, int count, bool keysOrderedInEachPartition, bool keysNormalized)
            {
                _start = start;
                _count = count;
                _keysOrderedInEachPartition = keysOrderedInEachPartition;
                _keysNormalized = keysNormalized;
            }

            public IEnumerator<KeyValuePair<long, int>> GetEnumerator()
            {
                while (true)
                {
                    int elemIndex = Interlocked.Increment(ref _pos) - 1;
                    if (elemIndex >= _count)
                    {
                        yield break;
                    }

                    if (!_keysOrderedInEachPartition)
                    {
                        elemIndex = _count - 1 - elemIndex;
                    }
                    long key = _keysNormalized ? elemIndex : (elemIndex * 2);

                    yield return new KeyValuePair<long, int>(key, _start + elemIndex);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
