// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TSource> Union<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            return Union(first, second, null);
        }

        public static IEnumerable<TSource> Union<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
        {
            if (first == null) throw Error.ArgumentNull("first");
            if (second == null) throw Error.ArgumentNull("second");
            return new UnionIterator<TSource>(first, second, comparer);
        }

        private sealed class UnionIterator<TSource> : Iterator<TSource>, IIListProvider<TSource>
        {
            private readonly IEnumerable<TSource> _first;
            private readonly IEnumerable<TSource> _second;
            private readonly IEqualityComparer<TSource> _comparer;
            private Set<TSource> _set;
            private IEnumerator<TSource> _enumerator;

            public UnionIterator(IEnumerable<TSource> first, IEnumerable<TSource> second, IEqualityComparer<TSource> comparer)
            {
                Debug.Assert(first != null);
                Debug.Assert(second != null);
                _first = first;
                _second = second;
                _comparer = comparer;
            }

            public override Iterator<TSource> Clone()
            {
                return new UnionIterator<TSource>(_first, _second, _comparer);
            }

            private bool GetNext()
            {
                while (_enumerator.MoveNext())
                {
                    TSource element = _enumerator.Current;
                    if (_set.Add(element))
                    {
                        current = element;
                        return true;
                    }
                }

                return false;
            }

            public override bool MoveNext()
            {
                switch(state)
                {
                    case 1:
                        _enumerator = _first.GetEnumerator();
                        if (_enumerator.MoveNext())
                        {
                            state = 2;
                        }
                        else
                        {
                            _enumerator.Dispose();
                            _enumerator = _second.GetEnumerator();
                            if (!_enumerator.MoveNext())
                            {
                                Dispose();
                                return false;
                            }
                            state = 3;
                        }
                        TSource element = _enumerator.Current;
                        _set = new Set<TSource>(_comparer);
                        _set.Add(element);
                        current = element;
                        return true;
                    case 2:
                        if (GetNext())
                            return true;
                        _enumerator.Dispose();
                        _enumerator = _second.GetEnumerator();
                        state = 3;
                        goto case 3;
                    case 3:
                        if (GetNext())
                            return true;
                        break;
                }

                Dispose();
                return false;
            }

            public override void Dispose()
            {
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                    _set = null;
                }

                base.Dispose();
            }

            private Set<TSource> FillSet()
            {
                Set<TSource> set = new Set<TSource>(_comparer);
                foreach (TSource element in _first)
                    set.Add(element);
                foreach (TSource element in _second)
                    set.Add(element);
                return set;
            }

            public TSource[] ToArray()
            {
                return FillSet().ToArray();
            }

            public List<TSource> ToList()
            {
                return FillSet().ToList();
            }

            public int GetCount(bool onlyIfCheap)
            {
                return onlyIfCheap ? -1 : FillSet().Count;
            }
        }
    }
}
