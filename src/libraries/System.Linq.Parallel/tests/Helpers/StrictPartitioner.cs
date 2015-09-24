// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace System.Linq.Parallel.Tests
{
    // This class is meant to be used with partitioners that do load balancing, but _not_ buffering.
    // Feeding it a buffering partitioner will result in incorrect results!
    internal class StrictPartitioner<T> : Partitioner<T>
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
}
