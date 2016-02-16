// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TSource> Append<TSource>(this IEnumerable<TSource> source, TSource element)
        {
            if (source == null) throw Error.ArgumentNull("source");
            return AppendIterator(source, element);
        }

        private static IEnumerable<TSource> AppendIterator<TSource>(IEnumerable<TSource> source, TSource element)
        {
            foreach (TSource e1 in source) yield return e1;
            yield return element;
        }

        public static IEnumerable<TSource> Prepend<TSource>(this IEnumerable<TSource> source, TSource element)
        {
            if (source == null) throw Error.ArgumentNull("source");
            return PrependIterator(source, element);
        }

        private static IEnumerable<TSource> PrependIterator<TSource>(IEnumerable<TSource> source, TSource element)
        {
            yield return element;
            foreach (TSource e1 in source) yield return e1;
        }

        public static IEnumerable<TSource> Concat<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            if (first == null) throw Error.ArgumentNull("first");
            if (second == null) throw Error.ArgumentNull("second");

            var concatFirst = first as ConcatIterator<TSource>;
            return concatFirst != null ?
                UpgradeConcatIterator(concatFirst, second) :
                new Concat2Iterator<TSource>(first, second);
        }

        private static ConcatIterator<TSource> UpgradeConcatIterator<TSource>(ConcatIterator<TSource> first, IEnumerable<TSource> second)
        {
            Debug.Assert(first is Concat2Iterator<TSource> || first is Concat3Iterator<TSource> || first is ConcatNIterator<TSource>);
            Debug.Assert(second != null);

            // Are we upgrading from two sources to three?
            Concat2Iterator<TSource> two = first as Concat2Iterator<TSource>;
            if (two != null)
            {
                return new Concat3Iterator<TSource>(two._first, two._second, second);
            }

            // From three to four?
            Concat3Iterator<TSource> three = first as Concat3Iterator<TSource>;
            if (three != null)
            {
                return new ConcatNIterator<TSource>(three._first, three._second, three._third, second);
            }

            // From four+ to one more
            const int MaxLengthForConcatArray = 64; // arbitrary limit on array size to avoid lots of large array allocations
            ConcatNIterator<TSource> n = (ConcatNIterator<TSource>)first;
            if (n._sources.Length < MaxLengthForConcatArray)
            {
                var sources = new IEnumerable<TSource>[n._sources.Length + 1];
                Array.Copy(n._sources, 0, sources, 0, n._sources.Length);
                sources[n._sources.Length] = second;
                return new ConcatNIterator<TSource>(sources);
            }

            // Fall back to using the normal two-source concat
            return new Concat2Iterator<TSource>(first, second);
        }

        private sealed class Concat2Iterator<TSource> : ConcatIterator<TSource>
        {
            internal readonly IEnumerable<TSource> _first;
            internal readonly IEnumerable<TSource> _second;

            internal Concat2Iterator(IEnumerable<TSource> first, IEnumerable<TSource> second)
            {
                Debug.Assert(first != null && second != null);
                _first = first;
                _second = second;
            }

            public override Iterator<TSource> Clone()
            {
                return new Concat2Iterator<TSource>(_first, _second);
            }

            protected override IEnumerable<TSource> GetEnumerable(int index)
            {
                switch (index)
                {
                    case 0: return _first;
                    case 1: return _second;
                    default: return null;
                }
            }
        }

        private sealed class Concat3Iterator<TSource> : ConcatIterator<TSource>
        {
            internal readonly IEnumerable<TSource> _first;
            internal readonly IEnumerable<TSource> _second;
            internal readonly IEnumerable<TSource> _third;

            internal Concat3Iterator(IEnumerable<TSource> first, IEnumerable<TSource> second, IEnumerable<TSource> third)
            {
                Debug.Assert(first != null && second != null && third != null);
                _first = first;
                _second = second;
                _third = third;
            }

            public override Iterator<TSource> Clone()
            {
                return new Concat3Iterator<TSource>(_first, _second, _third);
            }

            protected override IEnumerable<TSource> GetEnumerable(int index)
            {
                switch (index)
                {
                    case 0: return _first;
                    case 1: return _second;
                    case 2: return _third;
                    default: return null;
                }
            }
        }

        private sealed class ConcatNIterator<TSource> : ConcatIterator<TSource>
        {
            internal readonly IEnumerable<TSource>[] _sources;

            internal ConcatNIterator(params IEnumerable<TSource>[] sources)
            {
                Debug.Assert(sources != null);
                Debug.Assert(sources.All(s => s != null));
                Debug.Assert(sources.Length > 3, "Should be using Concat2Iterator or Concat3Iterator");
                _sources = sources;
            }

            public override Iterator<TSource> Clone()
            {
                return new ConcatNIterator<TSource>(_sources);
            }

            protected override IEnumerable<TSource> GetEnumerable(int index)
            {
                return index < _sources.Length ? _sources[index] : null;
            }
        }

        private abstract class ConcatIterator<TSource> : Iterator<TSource>, IIListProvider<TSource>
        {
            private IEnumerator<TSource> _enumerator;

            public override void Dispose()
            {
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }
                base.Dispose();
            }

            protected abstract IEnumerable<TSource> GetEnumerable(int index);

            public override bool MoveNext()
            {
                if (state == 1)
                {
                    _enumerator = GetEnumerable(0).GetEnumerator();
                    state = 2;
                }

                if (state > 1)
                {
                    while (true)
                    {
                        if (_enumerator.MoveNext())
                        {
                            current = _enumerator.Current;
                            return true;
                        }

                        Debug.Assert(state < int.MaxValue);
                        IEnumerable<TSource> next = GetEnumerable(state++ - 1);
                        if (next != null)
                        {
                            _enumerator.Dispose();
                            _enumerator = next.GetEnumerator();
                            continue;
                        }

                        Dispose();
                        break;
                    }
                }

                return false;
            }

            public TSource[] ToArray()
            {
                return EnumerableHelpers.ToArray(this);
            }

            public List<TSource> ToList()
            {
                var list = new List<TSource>();
                for (int i = 0; ; i++)
                {
                    IEnumerable<TSource> source = GetEnumerable(i);
                    if (source == null) break;

                    list.AddRange(source);
                }
                return list;
            }

            public int GetCount(bool onlyIfCheap)
            {
                int count = 0;
                for (int i = 0; ; i++)
                {
                    IEnumerable<TSource> source = GetEnumerable(i);
                    if (source == null) break;

                    ICollection<TSource> c = source as ICollection<TSource>;
                    checked
                    {
                        if (c == null)
                        {
                            if (onlyIfCheap) return -1;
                            count += source.Count();
                        }
                        else
                        {
                            count += c.Count;
                        }
                    }
                }
                return count;
            }
        }
    }
}
