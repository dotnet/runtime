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
            private IEnumerator<TSource>? _enumerator;

            public SizeOptIListWhereIterator(IList<TSource> source, Func<TSource, bool> predicate)
            {
                Debug.Assert(source is not null);
                Debug.Assert(predicate is not null);
                _source = source;
                _predicate = predicate;
            }

            private protected override Iterator<TSource> Clone() =>
                new SizeOptIListWhereIterator<TSource>(_source, _predicate);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        _state = 2;
                        goto case 2;
                    case 2:
                        while (_enumerator!.MoveNext())
                        {
                            TSource item = _enumerator.Current;
                            if (_predicate(item))
                            {
                                _current = item;
                                return true;
                            }
                        }

                        Dispose();
                        break;
                }

                return false;
            }

            public override IEnumerable<TSource> Where(Func<TSource, bool> predicate) =>
                new SizeOptIListWhereIterator<TSource>(_source, Utilities.CombinePredicates(_predicate, predicate));

            public override TSource[] ToArray()
            {
                SegmentedArrayBuilder<TSource>.ScratchBuffer scratch = default;
                SegmentedArrayBuilder<TSource> builder = new(scratch);

                foreach (TSource item in _source)
                {
                    if (_predicate(item))
                    {
                        builder.Add(item);
                    }
                }

                TSource[] result = builder.ToArray();
                builder.Dispose();

                return result;
            }

            public override List<TSource> ToList()
            {
                SegmentedArrayBuilder<TSource>.ScratchBuffer scratch = default;
                SegmentedArrayBuilder<TSource> builder = new(scratch);

                foreach (TSource item in _source)
                {
                    if (_predicate(item))
                    {
                        builder.Add(item);
                    }
                }

                List<TSource> result = builder.ToList();
                builder.Dispose();

                return result;
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
