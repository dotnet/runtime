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
        public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source)
        {
            return Distinct(source, null);
        }

        public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource> comparer)
        {
            if (source == null) throw Error.ArgumentNull("source");
            return new DistinctIterator<TSource>(source, comparer);
        }

        private sealed class DistinctIterator<TSource> : Iterator<TSource>, IIListProvider<TSource>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly IEqualityComparer<TSource> _comparer;
            private Set<TSource> _set;
            private IEnumerator<TSource> _enumerator;

            public DistinctIterator(IEnumerable<TSource> source, IEqualityComparer<TSource> comparer)
            {
                Debug.Assert(source != null);
                _source = source;
                _comparer = comparer;
            }

            public override Iterator<TSource> Clone()
            {
                return new DistinctIterator<TSource>(_source, _comparer);
            }

            public override bool MoveNext()
            {
                switch (state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        if (!_enumerator.MoveNext())
                        {
                            Dispose();
                            return false;
                        }
                        TSource element = _enumerator.Current;
                        _set = new Set<TSource>(_comparer);
                        _set.Add(element);
                        current = element;
                        state = 2;
                        return true;
                    case 2:
                        while(_enumerator.MoveNext())
                        {
                            element = _enumerator.Current;
                            if (_set.Add(element))
                            {
                                current = element;
                                return true;
                            }
                        }
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
                foreach (TSource element in _source)
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
