// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> Repeat<TResult>(TResult element, int count)
        {
            if (count < 0)
            {
                throw Error.ArgumentOutOfRange(nameof(count));
            }

            if (count == 0)
            {
                return EmptyPartition<TResult>.Instance;
            }

            return new RepeatIterator<TResult>(element, count);
        }

        /// <summary>
        /// An iterator that yields the same item multiple times. 
        /// </summary>
        /// <typeparam name="TResult">The type of the item.</typeparam>
        private sealed class RepeatIterator<TResult> : Iterator<TResult>, IPartition<TResult>
        {
            private readonly int _count;

            public RepeatIterator(TResult element, int count)
            {
                Debug.Assert(count > 0);
                _current = element;
                _count = count;
            }

            public override Iterator<TResult> Clone()
            {
                return new RepeatIterator<TResult>(_current, _count);
            }

            public override void Dispose()
            {
                // Don't let base.Dispose wipe Current.
                _state = -1;
            }

            public override bool MoveNext()
            {
                // Having a separate field for the number of sent items would be more readable.
                // However, we save it into _state with a bias to minimize field size of the iterator.
                int sent = _state - 1;

                // We can't have sent a negative number of items, obviously. However, if this iterator
                // was illegally casted to IEnumerator without GetEnumerator being called, or if we've
                // already been disposed, then `sent` will be negative.
                if (sent >= 0 && sent != _count)
                {
                    ++_state;
                    return true;
                }

                Dispose();
                return false;
            }

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
