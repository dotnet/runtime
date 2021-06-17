// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// RepeatEnumerable.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A simple enumerable type that implements the repeat algorithm. It also supports
    /// partitioning of the count space by implementing an interface that PLINQ recognizes.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    internal sealed class RepeatEnumerable<TResult> : ParallelQuery<TResult>, IParallelPartitionable<TResult>
    {
        private readonly TResult _element; // Element value to repeat.
        private readonly int _count; // Count of element values.

        //-----------------------------------------------------------------------------------
        // Constructs a new repeat enumerable object for the repeat operation.
        //

        internal RepeatEnumerable(TResult element, int count)
            : base(QuerySettings.Empty)
        {
            Debug.Assert(count >= 0, "count not within range (must be >= 0)");
            _element = element;
            _count = count;
        }

        //-----------------------------------------------------------------------------------
        // Retrieves 'count' partitions, dividing the total count by the partition count,
        // and having each partition produce a certain number of repeated elements.
        //

        public QueryOperatorEnumerator<TResult, int>[] GetPartitions(int partitionCount)
        {
            // Calculate a stride size.
            int stride = (_count + partitionCount - 1) / partitionCount;

            // Now generate the actual enumerators. Each produces 'stride' elements, except
            // for the last partition which may produce fewer (if '_count' isn't evenly
            // divisible by 'partitionCount').
            QueryOperatorEnumerator<TResult, int>[] partitions = new QueryOperatorEnumerator<TResult, int>[partitionCount];
            for (int i = 0, offset = 0; i < partitionCount; i++, offset += stride)
            {
                if ((offset + stride) > _count)
                {
                    partitions[i] = new RepeatEnumerator(_element, offset < _count ? _count - offset : 0, offset);
                }
                else
                {
                    partitions[i] = new RepeatEnumerator(_element, stride, offset);
                }
            }

            return partitions;
        }

        //-----------------------------------------------------------------------------------
        // Basic IEnumerator<T> method implementations.
        //

        public override IEnumerator<TResult> GetEnumerator()
        {
            return new RepeatEnumerator(_element, _count, 0).AsClassicEnumerator();
        }

        //-----------------------------------------------------------------------------------
        // The actual enumerator that produces a set of repeated elements.
        //

        private sealed class RepeatEnumerator : QueryOperatorEnumerator<TResult, int>
        {
            private readonly TResult _element; // The element to repeat.
            private readonly int _count; // The number of times to repeat it.
            private readonly int _indexOffset; // Our index offset.
            private Shared<int>? _currentIndex; // The number of times we have already repeated it. [allocate in moveNext to avoid false-sharing]

            //-----------------------------------------------------------------------------------
            // Creates a new enumerator.
            //

            internal RepeatEnumerator(TResult element, int count, int indexOffset)
            {
                _element = element;
                _count = count;
                _indexOffset = indexOffset;
            }

            //-----------------------------------------------------------------------------------
            // Basic IEnumerator<T> methods. These produce the repeating sequence..
            //

            internal override bool MoveNext([MaybeNullWhen(false), AllowNull] ref TResult currentElement, ref int currentKey)
            {
                if (_currentIndex == null)
                    _currentIndex = new Shared<int>(-1);

                if (_currentIndex.Value < (_count - 1))
                {
                    ++_currentIndex.Value;
                    currentElement = _element;
                    currentKey = _currentIndex.Value + _indexOffset;
                    return true;
                }

                return false;
            }

            internal override void Reset()
            {
                _currentIndex = null;
            }
        }
    }
}
