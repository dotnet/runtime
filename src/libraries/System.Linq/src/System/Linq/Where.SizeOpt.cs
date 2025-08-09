// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class SizeOptIListWhereIterator<TSource> : Iterator<TSource>
        {
            private readonly IList<TSource> _source;
            private readonly Func<TSource, bool> _predicate;

            public SizeOptIListWhereIterator(IList<TSource> source, Func<TSource, bool> predicate)
            {
                Debug.Assert(source is not null && source.Count > 0);
                Debug.Assert(predicate is not null);
                _source = source;
                _predicate = predicate;
            }

            private protected override Iterator<TSource> Clone() =>
                new SizeOptIListWhereIterator<TSource>(_source, _predicate);

            public override bool MoveNext()
            {
                int index = _state - 1;
                IList<TSource> source = _source;

                while ((uint)index < (uint)source.Count)
                {
                    TSource item = source[index];
                    index = _state++;
                    if (_predicate(item))
                    {
                        _current = item;
                        return true;
                    }
                }

                Dispose();
                return false;
            }

            public override IEnumerable<TSource> Where(Func<TSource, bool> predicate) =>
                new SizeOptIListWhereIterator<TSource>(_source, Utilities.CombinePredicates(_predicate, predicate));

            public override TSource[] ToArray()
            {
                var array = new TSource[_source.Count];
                int count = 0;

                foreach (TSource item in _source)
                {
                    if (_predicate(item))
                    {
                        array[count++] = item;
                    }
                }

                Array.Resize(ref array, count);
                return array;
            }

            public override List<TSource> ToList()
            {
                var list = new List<TSource>(_source.Count);
                foreach (TSource item in _source)
                {
                    if (_predicate(item))
                    {
                        list.Add(item);
                    }
                }
                return list;
            }

            public override int GetCount(bool onlyIfCheap)
            {
                if (onlyIfCheap)
                {
                    return -1;
                }

                int count = 0;
                foreach (TSource item in _source)
                {
                    if (_predicate(item))
                    {
                        checked { count++; }
                    }
                }
                return count;
            }
        }
    }
}
