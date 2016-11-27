// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TSource> Reverse<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            return new ReverseIterator<TSource>(source);
        }

        /// <summary>
        /// An iterator that yields the items of an <see cref="IEnumerable{TSource}"/> in reverse.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
        private sealed class ReverseIterator<TSource> : Iterator<TSource>, IIListProvider<TSource>
        {
            private readonly IEnumerable<TSource> _source;
            private TSource[] _buffer;

            public ReverseIterator(IEnumerable<TSource> source)
            {
                Debug.Assert(source != null);
                _source = source;
            }

            public override Iterator<TSource> Clone()
            {
                return new ReverseIterator<TSource>(_source);
            }

            public override bool MoveNext()
            {
                if (_state - 2 <= -2)
                {
                    Dispose();
                    return false;
                }

                switch (_state)
                {
                    case 1:
                        Buffer<TSource> buffer = new Buffer<TSource>(_source);
                        _buffer = buffer._items;
                        _state = buffer._count + 2;
                        goto default;
                    default:
                        int index = _state - 3;
                        if (index != -1)
                        {
                            _current = _buffer[index];
                            --_state;
                            return true;
                        }

                        break;
                }

                Dispose();
                return false;
            }

            public override void Dispose()
            {
                _buffer = null; // Just in case this ends up being long-lived, allow the memory to be reclaimed.
                base.Dispose();
            }

            public TSource[] ToArray()
            {
                TSource[] array = _source.ToArray();

                // Array.Reverse() involves boxing for non-primitive value types, but
                // checking that has its own cost, so just use this approach for all types.
                for (int i = 0, j = array.Length - 1; i < j; ++i, --j)
                {
                    TSource temp = array[i];
                    array[i] = array[j];
                    array[j] = temp;
                }

                return array;
            }

            public List<TSource> ToList()
            {
                List<TSource> list = _source.ToList();
                list.Reverse();
                return list;
            }

            public int GetCount(bool onlyIfCheap)
            {
                if (onlyIfCheap)
                {
                    IIListProvider<TSource> listProv = _source as IIListProvider<TSource>;
                    if (listProv != null)
                    {
                        return listProv.GetCount(onlyIfCheap: true);
                    }

                    if (!(_source is ICollection<TSource>) && !(_source is ICollection))
                    {
                        return -1;
                    }
                }

                return _source.Count();
            }
        }
    }
}
