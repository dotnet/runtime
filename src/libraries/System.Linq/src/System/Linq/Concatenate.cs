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
                concatFirst.Concat(second) :
                new Concat2Iterator<TSource>(first, second);
        }

        private sealed class Concat2Iterator<TSource> : ConcatIterator<TSource>
        {
            private readonly IEnumerable<TSource> _first;
            private readonly IEnumerable<TSource> _second;

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

            internal override ConcatIterator<TSource> Concat(IEnumerable<TSource> next)
            {
                return new Concat3Iterator<TSource>(_first, _second, next);
            }

            internal override IEnumerable<TSource> GetEnumerable(int index)
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
            private readonly IEnumerable<TSource> _first;
            private readonly IEnumerable<TSource> _second;
            private readonly IEnumerable<TSource> _third;

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

            internal override ConcatIterator<TSource> Concat(IEnumerable<TSource> next)
            {
                return new ConcatNIterator<TSource>(this, next, 3);
            }

            internal override IEnumerable<TSource> GetEnumerable(int index)
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
            // To handle chains of >= 4 sources, we chain the concat iterators together and allow
            // GetEnumerable to fetch enumerables from the previous sources.  This means that rather
            // than each MoveNext/Current calls having to traverse all of the previous sources, we
            // only have to traverse all of the previous sources once per chained enumerable.  An
            // alternative would be to use an array to store all of the enumerables, but this has
            // a much better memory profile and without much additional run-time cost.

            private readonly ConcatIterator<TSource> _previousConcat;
            private readonly IEnumerable<TSource> _next;
            private readonly int _nextIndex;

            internal ConcatNIterator(ConcatIterator<TSource> previousConcat, IEnumerable<TSource> next, int nextIndex)
            {
                Debug.Assert(previousConcat != null);
                Debug.Assert(next != null);
                Debug.Assert(nextIndex > 0);
                _previousConcat = previousConcat;
                _next = next;
                _nextIndex = nextIndex;
            }

            public override Iterator<TSource> Clone()
            {
                return new ConcatNIterator<TSource>(_previousConcat, _next, _nextIndex);
            }

            internal override ConcatIterator<TSource> Concat(IEnumerable<TSource> next)
            {
                return new ConcatNIterator<TSource>(this, next, checked(_nextIndex + 1));
            }

            internal override IEnumerable<TSource> GetEnumerable(int index)
            {
                if (index > _nextIndex)
                {
                    return null;
                }

                // Walk back through the chain of ConcatNIterators looking for the one
                // that has its _nextIndex equal to index.  If we don't find one, then it
                // must be prior to any of them, so call GetEnumerable on the previous
                // Concat3/2Iterator.  This avoids a deep recursive call chain.
                ConcatNIterator<TSource> current = this;
                while (true)
                {
                    if (index == current._nextIndex)
                    {
                        return current._next;
                    }

                    ConcatNIterator<TSource> prevN = current._previousConcat as ConcatNIterator<TSource>;
                    if (prevN != null)
                    {
                        current = prevN;
                        continue;
                    }

                    return current._previousConcat.GetEnumerable(index);
                }
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

            internal abstract IEnumerable<TSource> GetEnumerable(int index);

            internal abstract ConcatIterator<TSource> Concat(IEnumerable<TSource> next);

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
                if (onlyIfCheap) return -1;

                int count = 0;
                for (int i = 0; ; i++)
                {
                    IEnumerable<TSource> source = GetEnumerable(i);
                    if (source == null) break;
                    checked { count += source.Count(); }
                }
                return count;
            }
        }
    }
}
