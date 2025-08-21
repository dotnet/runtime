// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed class SizeOptIListSelectIterator<TSource, TResult>(IList<TSource> _source, Func<TSource, TResult> _selector)
            : Iterator<TResult>
        {
            private IEnumerator<TSource>? _enumerator;

            public override int GetCount(bool onlyIfCheap)
            {
                // In case someone uses Count() to force evaluation of
                // the selector, run it provided `onlyIfCheap` is false.

                if (onlyIfCheap)
                {
                    return -1;
                }

                int count = 0;

                foreach (TSource item in _source)
                {
                    _selector(item);
                    checked
                    {
                        count++;
                    }
                }

                return count;
            }

            public override Iterator<TResult> Skip(int count)
            {
                Debug.Assert(count > 0);
                return new IListSkipTakeSelectIterator<TSource, TResult>(_source, _selector, count, int.MaxValue);
            }

            public override Iterator<TResult> Take(int count)
            {
                Debug.Assert(count > 0);
                return new IListSkipTakeSelectIterator<TSource, TResult>(_source, _selector, 0, count - 1);
            }

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _enumerator = _source.GetEnumerator();
                        _state = 2;
                        goto case 2;
                    case 2:
                        Debug.Assert(_enumerator is not null);
                        if (_enumerator.MoveNext())
                        {
                            _current = _selector(_enumerator.Current);
                            return true;
                        }

                        Dispose();
                        break;
                }

                return false;
            }

            public override TResult[] ToArray()
            {
                TResult[] array = new TResult[_source.Count];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = _selector(_source[i]);
                }
                return array;
            }

            public override List<TResult> ToList()
            {
                List<TResult> list = new List<TResult>(_source.Count);
                for (int i = 0; i < list.Count; i++)
                {
                    list.Add(_selector(_source[i]));
                }
                return list;
            }

            private protected override Iterator<TResult> Clone()
                => new SizeOptIListSelectIterator<TSource, TResult>(_source, _selector);
        }
    }
}
