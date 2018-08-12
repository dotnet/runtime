// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

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
                for (int i = 0; i != _count; ++i)
                {
                    list.Add(_current);
                }

                return list;
            }

            public int GetCount(bool onlyIfCheap) => _count;

            public IPartition<TResult> Skip(int count)
            {
                if (count >= _count)
                {
                    return EmptyPartition<TResult>.Instance;
                }

                return new RepeatIterator<TResult>(_current, _count - count);
            }

            public IPartition<TResult> Take(int count)
            {
                if (count >= _count)
                {
                    return this;
                }

                return new RepeatIterator<TResult>(_current, count);
            }

            public TResult TryGetElementAt(int index, out bool found)
            {
                if ((uint)index < (uint)_count)
                {
                    found = true;
                    return _current;
                }

                found = false;
                return default(TResult);
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
