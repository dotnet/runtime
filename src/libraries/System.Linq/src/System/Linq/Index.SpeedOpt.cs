// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using static System.Linq.Utilities;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class IEnumerableIndexIterator<TSource> : Iterator<(int Index, TSource Item)>
        {
            private readonly IEnumerable<TSource> _source;
            private int _index;
            private IEnumerator<TSource>? _enumerator;

            public IEnumerableIndexIterator(IEnumerable<TSource> source)
            {
                Debug.Assert(source is not null);
                _source = source;
            }

            private protected override Iterator<(int Index, TSource Item)> Clone() =>
                new IEnumerableIndexIterator<TSource>(_source);

            public override void Dispose()
            {
                if (_enumerator is not null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }

                base.Dispose();
            }

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        _index = -1;
                        _state = 2;
                        goto case 2;
                    case 2:
                        Debug.Assert(_enumerator is not null);

                        if (_enumerator.MoveNext())
                        {
                            _current = (checked(++_index), _enumerator.Current);
                            return true;
                        }

                        Dispose();
                        break;
                }

                return false;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<(int Index, TSource Item), TResult2> selector) =>
                new IEnumerableSelect2Iterator<TSource, TResult2>(_source, CombineSelectors((TSource x, int i) => (i, x), selector));

            public override (int Index, TSource Item)[] ToArray()
            {
                int index = -1;

                if (_source.TryGetNonEnumeratedCount(out int known))
                {
                    var array = new (int Index, TSource Item)[known];

                    foreach (TSource item in _source)
                    {
                        array[checked(++index)] = (index, item);
                    }

                    return array;
                }

                SegmentedArrayBuilder<(int Index, TSource Item)>.ScratchBuffer scratch = default;
                SegmentedArrayBuilder<(int Index, TSource Item)> builder = new(scratch);

                foreach (TSource item in _source)
                {
                    builder.Add((checked(++index), item));
                }

                (int Index, TSource Item)[] result = builder.ToArray();
                builder.Dispose();
                return result;
            }

            public override List<(int Index, TSource Item)> ToList()
            {
                List<(int Index, TSource Item)> list = _source.TryGetNonEnumeratedCount(out int known) ? new(known) : [];
                int index = -1;

                foreach (TSource item in _source)
                {
                    list.Add((checked(++index), item));
                }

                return list;
            }

            public override int GetCount(bool onlyIfCheap)
            {
                // In case someone uses Count() to force evaluation of
                // the selector, run it provided `onlyIfCheap` is false.
                if (onlyIfCheap)
                {
                    return _source.TryGetNonEnumeratedCount(out int known) ? known : -1;
                }

                int count = 0;

                foreach (TSource item in _source)
                {
                    checked
                    {
                        count++;
                    }
                }

                return count;
            }

            public override (int Index, TSource Item) TryGetElementAt(int index, out bool found)
            {
                if (_source is Iterator<TSource> iterator)
                {
                    return iterator.TryGetElementAt(index, out found) is var element && found ? (0, element!) : default;
                }

                if (index >= 0)
                {
                    IEnumerator<TSource> e = _source.GetEnumerator();
                    int enumeratorIndex = -1;

                    try
                    {
                        while (e.MoveNext())
                        {
                            if (index == 0)
                            {
                                found = true;
                                return (checked(++enumeratorIndex), e.Current);
                            }

                            index--;
                        }
                    }
                    finally
                    {
                        (e as IDisposable)?.Dispose();
                    }
                }

                found = false;
                return default;
            }

            public override (int Index, TSource Item) TryGetFirst(out bool found)
            {
                if (_source is Iterator<TSource> iterator)
                {
                    return iterator.TryGetFirst(out found) is var first && found ? (0, first!) : default;
                }

                using IEnumerator<TSource> e = _source.GetEnumerator();

                if (e.MoveNext())
                {
                    found = true;
                    return (0, e.Current);
                }

                found = false;
                return default;
            }

            public override (int Index, TSource Item) TryGetLast(out bool found)
            {
                if (_source is Iterator<TSource> iterator && iterator.GetCount(true) is not -1 and var count)
                {
                    return iterator.TryGetLast(out found) is var last && found ? (count - 1, last!) : default;
                }

                using IEnumerator<TSource> e = _source.GetEnumerator();

                if (e.MoveNext())
                {
                    found = true;
                    TSource lastElement = e.Current;
                    int lastIndex = -1;

                    while (e.MoveNext())
                    {
                        lastElement = e.Current;
                        lastIndex++;
                    }

                    return (lastIndex, lastElement);
                }

                found = false;
                return default;
            }
        }
    }
}
