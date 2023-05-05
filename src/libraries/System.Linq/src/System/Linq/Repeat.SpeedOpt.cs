// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class RepeatIterator<TResult> : IPartition<TResult>
        {
            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new SelectIPartitionIterator<TResult, TResult2>(this, selector);

            public TResult[] ToArray()
            {
                TResult[] array = new TResult[_count];
                if (_current != null)
                {
                    Array.Fill(array, _current);
                }

                return array;
            }

            public List<TResult> ToList()
            {
                List<TResult> list = new List<TResult>(_count);
                SetCountAndGetSpan(list, _count).Fill(_current);

                return list;
            }

            public int GetCount(bool onlyIfCheap) => _count;

            public IPartition<TResult> Skip(int count)
            {
                Debug.Assert(count > 0);

                if (count >= _count)
                {
                    return EmptyPartition<TResult>.Instance;
                }

                return new RepeatIterator<TResult>(_current, _count - count);
            }

            public IPartition<TResult> Take(int count)
            {
                Debug.Assert(count > 0);

                if (count >= _count)
                {
                    return this;
                }

                return new RepeatIterator<TResult>(_current, count);
            }

            public TResult? TryGetElementAt(int index, out bool found)
            {
                if ((uint)index < (uint)_count)
                {
                    found = true;
                    return _current;
                }

                found = false;
                return default;
            }

            public TResult TryGetFirst(out bool found)
            {
                found = true;
                return _current;
            }

            public TResult TryGetLast(out bool found)
            {
                found = true;
                return _current;
            }
        }
    }
}
