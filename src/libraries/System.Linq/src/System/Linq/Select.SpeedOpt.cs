// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static System.Linq.Utilities;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class IEnumerableSelectIterator<TSource, TResult>
        {
            public override TResult[] ToArray()
            {
                SegmentedArrayBuilder<TResult>.ScratchBuffer scratch = default;
                SegmentedArrayBuilder<TResult> builder = new(scratch);

                Func<TSource, TResult> selector = _selector;
                foreach (TSource item in _source)
                {
                    builder.Add(selector(item));
                }

                TResult[] result = builder.ToArray();
                builder.Dispose();

                return result;
            }

            public override List<TResult> ToList()
            {
                var list = new List<TResult>();

                Func<TSource, TResult> selector = _selector;
                foreach (TSource item in _source)
                {
                    list.Add(selector(item));
                }

                return list;
            }

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

            public override TResult? TryGetElementAt(int index, out bool found)
            {
                if (index >= 0)
                {
                    IEnumerator<TSource> e = _source.GetEnumerator();
                    try
                    {
                        while (e.MoveNext())
                        {
                            if (index == 0)
                            {
                                found = true;
                                return _selector(e.Current);
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

            public override TResult? TryGetFirst(out bool found)
            {
                using IEnumerator<TSource> e = _source.GetEnumerator();
                if (e.MoveNext())
                {
                    found = true;
                    return _selector(e.Current);
                }

                found = false;
                return default;
            }

            public override TResult? TryGetLast(out bool found)
            {
                using IEnumerator<TSource> e = _source.GetEnumerator();

                if (e.MoveNext())
                {
                    found = true;
                    TSource last = e.Current;

                    while (e.MoveNext())
                    {
                        last = e.Current;
                    }

                    return _selector(last);
                }

                found = false;
                return default;
            }
        }

        private sealed partial class ArraySelectIterator<TSource, TResult>
        {
            public override TResult[] ToArray()
            {
                // See assert in constructor.
                // Since _source should never be empty, we don't check for 0/return Array.Empty.
                TSource[] source = _source;
                Debug.Assert(source.Length > 0);

                var results = new TResult[source.Length];
                Fill(source, results, _selector);

                return results;
            }

            public override List<TResult> ToList()
            {
                TSource[] source = _source;
                Debug.Assert(source.Length > 0);

                var results = new List<TResult>(source.Length);
                Fill(source, SetCountAndGetSpan(results, source.Length), _selector);

                return results;
            }

            private static void Fill(ReadOnlySpan<TSource> source, Span<TResult> destination, Func<TSource, TResult> func)
            {
                for (int i = 0; i < destination.Length; i++)
                {
                    destination[i] = func(source[i]);
                }
            }

            public override int GetCount(bool onlyIfCheap)
            {
                // In case someone uses Count() to force evaluation of
                // the selector, run it provided `onlyIfCheap` is false.

                if (!onlyIfCheap)
                {
                    foreach (TSource item in _source)
                    {
                        _selector(item);
                    }
                }

                return _source.Length;
            }

            public override Iterator<TResult>? Skip(int count)
            {
                Debug.Assert(count > 0);
                if (count >= _source.Length)
                {
                    return null;
                }

                return new IListSkipTakeSelectIterator<TSource, TResult>(_source, _selector, count, int.MaxValue);
            }

            public override Iterator<TResult> Take(int count)
            {
                Debug.Assert(count > 0);
                return count >= _source.Length ?
                    this :
                    new IListSkipTakeSelectIterator<TSource, TResult>(_source, _selector, 0, count - 1);
            }

            public override TResult? TryGetElementAt(int index, out bool found)
            {
                TSource[] source = _source;
                if ((uint)index < (uint)source.Length)
                {
                    found = true;
                    return _selector(source[index]);
                }

                found = false;
                return default;
            }

            public override TResult TryGetFirst(out bool found)
            {
                Debug.Assert(_source.Length > 0); // See assert in constructor

                found = true;
                return _selector(_source[0]);
            }

            public override TResult TryGetLast(out bool found)
            {
                Debug.Assert(_source.Length > 0); // See assert in constructor

                found = true;
                return _selector(_source[^1]);
            }
        }

        private sealed partial class RangeSelectIterator<TResult> : Iterator<TResult>
        {
            private readonly int _start;
            private readonly int _end;
            private readonly Func<int, TResult> _selector;

            public RangeSelectIterator(int start, int end, Func<int, TResult> selector)
            {
                Debug.Assert(start < end);
                Debug.Assert((uint)(end - start) <= (uint)int.MaxValue);
                Debug.Assert(selector is not null);

                _start = start;
                _end = end;
                _selector = selector;
            }

            public override Iterator<TResult> Clone() =>
                new RangeSelectIterator<TResult>(_start, _end, _selector);

            public override bool MoveNext()
            {
                if (_state < 1 || _state == (_end - _start + 1))
                {
                    Dispose();
                    return false;
                }

                int index = _state++ - 1;
                Debug.Assert(_start < _end - index);
                _current = _selector(_start + index);
                return true;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new RangeSelectIterator<TResult2>(_start, _end, CombineSelectors(_selector, selector));

            public override TResult[] ToArray()
            {
                var results = new TResult[_end - _start];
                Fill(results, _start, _selector);

                return results;
            }

            public override List<TResult> ToList()
            {
                var results = new List<TResult>(_end - _start);
                Fill(SetCountAndGetSpan(results, _end - _start), _start, _selector);

                return results;
            }

            private static void Fill(Span<TResult> results, int start, Func<int, TResult> func)
            {
                for (int i = 0; i < results.Length; i++, start++)
                {
                    results[i] = func(start);
                }
            }

            public override int GetCount(bool onlyIfCheap)
            {
                // In case someone uses Count() to force evaluation of the selector,
                // run it provided `onlyIfCheap` is false.
                if (!onlyIfCheap)
                {
                    for (int i = _start; i != _end; i++)
                    {
                        _selector(i);
                    }
                }

                return _end - _start;
            }

            public override Iterator<TResult>? Skip(int count)
            {
                Debug.Assert(count > 0);

                if (count >= (_end - _start))
                {
                    return null;
                }

                return new RangeSelectIterator<TResult>(_start + count, _end, _selector);
            }

            public override Iterator<TResult> Take(int count)
            {
                Debug.Assert(count > 0);

                if (count >= (_end - _start))
                {
                    return this;
                }

                return new RangeSelectIterator<TResult>(_start, _start + count, _selector);
            }

            public override TResult? TryGetElementAt(int index, out bool found)
            {
                if ((uint)index < (uint)(_end - _start))
                {
                    found = true;
                    return _selector(_start + index);
                }

                found = false;
                return default;
            }

            public override TResult TryGetFirst(out bool found)
            {
                Debug.Assert(_end > _start);
                found = true;
                return _selector(_start);
            }

            public override TResult TryGetLast(out bool found)
            {
                Debug.Assert(_end > _start);
                found = true;
                return _selector(_end - 1);
            }
        }

        private sealed partial class ListSelectIterator<TSource, TResult>
        {
            public override TResult[] ToArray()
            {
                ReadOnlySpan<TSource> source = CollectionsMarshal.AsSpan(_source);
                if (source.Length == 0)
                {
                    return [];
                }

                var results = new TResult[source.Length];
                Fill(source, results, _selector);

                return results;
            }

            public override List<TResult> ToList()
            {
                ReadOnlySpan<TSource> source = CollectionsMarshal.AsSpan(_source);

                var results = new List<TResult>(source.Length);
                Fill(source, SetCountAndGetSpan(results, source.Length), _selector);

                return results;
            }

            private static void Fill(ReadOnlySpan<TSource> source, Span<TResult> destination, Func<TSource, TResult> func)
            {
                for (int i = 0; i < destination.Length; i++)
                {
                    destination[i] = func(source[i]);
                }
            }

            public override int GetCount(bool onlyIfCheap)
            {
                // In case someone uses Count() to force evaluation of
                // the selector, run it provided `onlyIfCheap` is false.

                int count = _source.Count;

                if (!onlyIfCheap)
                {
                    for (int i = 0; i < count; i++)
                    {
                        _selector(_source[i]);
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

            public override TResult? TryGetElementAt(int index, out bool found)
            {
                if ((uint)index < (uint)_source.Count)
                {
                    found = true;
                    return _selector(_source[index]);
                }

                found = false;
                return default;
            }

            public override TResult? TryGetFirst(out bool found)
            {
                if (_source.Count != 0)
                {
                    found = true;
                    return _selector(_source[0]);
                }

                found = false;
                return default;
            }

            public override TResult? TryGetLast(out bool found)
            {
                int len = _source.Count;
                if (len != 0)
                {
                    found = true;
                    return _selector(_source[len - 1]);
                }

                found = false;
                return default;
            }
        }

        private sealed partial class IListSelectIterator<TSource, TResult>
        {
            public override TResult[] ToArray()
            {
                int count = _source.Count;
                if (count == 0)
                {
                    return [];
                }

                var results = new TResult[count];
                Fill(_source, results, _selector);

                return results;
            }

            public override List<TResult> ToList()
            {
                IList<TSource> source = _source;
                int count = _source.Count;

                var results = new List<TResult>(count);
                Fill(source, SetCountAndGetSpan(results, count), _selector);

                return results;
            }

            private static void Fill(IList<TSource> source, Span<TResult> results, Func<TSource, TResult> func)
            {
                for (int i = 0; i < results.Length; i++)
                {
                    results[i] = func(source[i]);
                }
            }

            public override int GetCount(bool onlyIfCheap)
            {
                // In case someone uses Count() to force evaluation of
                // the selector, run it provided `onlyIfCheap` is false.

                int count = _source.Count;

                if (!onlyIfCheap)
                {
                    for (int i = 0; i < count; i++)
                    {
                        _selector(_source[i]);
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

            public override TResult? TryGetElementAt(int index, out bool found)
            {
                if ((uint)index < (uint)_source.Count)
                {
                    found = true;
                    return _selector(_source[index]);
                }

                found = false;
                return default;
            }

            public override TResult? TryGetFirst(out bool found)
            {
                if (_source.Count != 0)
                {
                    found = true;
                    return _selector(_source[0]);
                }

                found = false;
                return default;
            }

            public override TResult? TryGetLast(out bool found)
            {
                int len = _source.Count;
                if (len != 0)
                {
                    found = true;
                    return _selector(_source[len - 1]);
                }

                found = false;
                return default;
            }
        }

        /// <summary>
        /// An iterator that maps each item of an <see cref="Iterator{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source elements.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        private sealed class IteratorSelectIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly Iterator<TSource> _source;
            private readonly Func<TSource, TResult> _selector;
            private IEnumerator<TSource>? _enumerator;

            public IteratorSelectIterator(Iterator<TSource> source, Func<TSource, TResult> selector)
            {
                Debug.Assert(source is not null);
                Debug.Assert(selector is not null);
                _source = source;
                _selector = selector;
            }

            public override Iterator<TResult> Clone() =>
                new IteratorSelectIterator<TSource, TResult>(_source, _selector);

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

            public override void Dispose()
            {
                if (_enumerator is not null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }

                base.Dispose();
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new IteratorSelectIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector));

            public override Iterator<TResult>? Skip(int count)
            {
                Debug.Assert(count > 0);
                Iterator<TSource>? source = _source.Skip(count);
                return source is null ? null : new IteratorSelectIterator<TSource, TResult>(source, _selector);
            }

            public override Iterator<TResult>? Take(int count)
            {
                Debug.Assert(count > 0);
                Iterator<TSource>? source = _source.Take(count);
                return source is null ? null : new IteratorSelectIterator<TSource, TResult>(source, _selector);
            }

            public override TResult? TryGetElementAt(int index, out bool found)
            {
                bool sourceFound;
                TSource? input = _source.TryGetElementAt(index, out sourceFound);
                found = sourceFound;
                return sourceFound ? _selector(input!) : default!;
            }

            public override TResult? TryGetFirst(out bool found)
            {
                bool sourceFound;
                TSource? input = _source.TryGetFirst(out sourceFound);
                found = sourceFound;
                return sourceFound ? _selector(input!) : default!;
            }

            public override TResult? TryGetLast(out bool found)
            {
                bool sourceFound;
                TSource? input = _source.TryGetLast(out sourceFound);
                found = sourceFound;
                return sourceFound ? _selector(input!) : default!;
            }

            private TResult[] LazyToArray()
            {
                Debug.Assert(_source.GetCount(onlyIfCheap: true) == -1);

                SegmentedArrayBuilder<TResult>.ScratchBuffer scratch = default;
                SegmentedArrayBuilder<TResult> builder = new(scratch);

                Func<TSource, TResult> selector = _selector;
                foreach (TSource input in _source)
                {
                    builder.Add(selector(input));
                }

                TResult[] result = builder.ToArray();
                builder.Dispose();

                return result;
            }

            private TResult[] PreallocatingToArray(int count)
            {
                Debug.Assert(count > 0);
                Debug.Assert(count == _source.GetCount(onlyIfCheap: true));

                TResult[] array = new TResult[count];
                Fill(_source, array, _selector);
                return array;
            }

            public override TResult[] ToArray()
            {
                int count = _source.GetCount(onlyIfCheap: true);
                return count switch
                {
                    -1 => LazyToArray(),
                    0 => [],
                    _ => PreallocatingToArray(count),
                };
            }

            public override List<TResult> ToList()
            {
                int count = _source.GetCount(onlyIfCheap: true);
                List<TResult> list;
                switch (count)
                {
                    case -1:
                        list = new List<TResult>();
                        foreach (TSource input in _source)
                        {
                            list.Add(_selector(input));
                        }
                        break;
                    case 0:
                        list = new List<TResult>();
                        break;
                    default:
                        list = new List<TResult>(count);
                        Fill(_source, SetCountAndGetSpan(list, count), _selector);
                        break;
                }

                return list;
            }

            private static void Fill(Iterator<TSource> source, Span<TResult> results, Func<TSource, TResult> func)
            {
                int index = 0;
                foreach (TSource item in source)
                {
                    results[index] = func(item);
                    ++index;
                }

                Debug.Assert(index == results.Length, "All list elements were not initialized.");
            }

            public override int GetCount(bool onlyIfCheap)
            {
                if (!onlyIfCheap)
                {
                    // In case someone uses Count() to force evaluation of
                    // the selector, run it provided `onlyIfCheap` is false.

                    int count = 0;

                    foreach (TSource item in _source)
                    {
                        _selector(item);
                        checked { count++; }
                    }

                    return count;
                }

                return _source.GetCount(onlyIfCheap);
            }
        }

        /// <summary>
        /// An iterator that maps each item of part of an <see cref="IList{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source list.</typeparam>
        /// <typeparam name="TResult">The type of the mapped items.</typeparam>
        [DebuggerDisplay("Count = {Count}")]
        private sealed class IListSkipTakeSelectIterator<TSource, TResult> : Iterator<TResult>
        {
            private readonly IList<TSource> _source;
            private readonly Func<TSource, TResult> _selector;
            private readonly int _minIndexInclusive;
            private readonly int _maxIndexInclusive;

            public IListSkipTakeSelectIterator(IList<TSource> source, Func<TSource, TResult> selector, int minIndexInclusive, int maxIndexInclusive)
            {
                Debug.Assert(source is not null);
                Debug.Assert(selector is not null);
                Debug.Assert(minIndexInclusive >= 0);
                Debug.Assert(minIndexInclusive <= maxIndexInclusive);
                _source = source;
                _selector = selector;
                _minIndexInclusive = minIndexInclusive;
                _maxIndexInclusive = maxIndexInclusive;
            }

            public override Iterator<TResult> Clone() =>
                new IListSkipTakeSelectIterator<TSource, TResult>(_source, _selector, _minIndexInclusive, _maxIndexInclusive);

            public override bool MoveNext()
            {
                // _state - 1 represents the zero-based index into the list.
                // Having a separate field for the index would be more readable. However, we save it
                // into _state with a bias to minimize field size of the iterator.
                int index = _state - 1;
                if ((uint)index <= (uint)(_maxIndexInclusive - _minIndexInclusive) && index < _source.Count - _minIndexInclusive)
                {
                    _current = _selector(_source[_minIndexInclusive + index]);
                    ++_state;
                    return true;
                }

                Dispose();
                return false;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector) =>
                new IListSkipTakeSelectIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector), _minIndexInclusive, _maxIndexInclusive);

            public override Iterator<TResult>? Skip(int count)
            {
                Debug.Assert(count > 0);
                int minIndex = _minIndexInclusive + count;
                return (uint)minIndex > (uint)_maxIndexInclusive ? null : new IListSkipTakeSelectIterator<TSource, TResult>(_source, _selector, minIndex, _maxIndexInclusive);
            }

            public override Iterator<TResult> Take(int count)
            {
                Debug.Assert(count > 0);
                int maxIndex = _minIndexInclusive + count - 1;
                return (uint)maxIndex >= (uint)_maxIndexInclusive ? this : new IListSkipTakeSelectIterator<TSource, TResult>(_source, _selector, _minIndexInclusive, maxIndex);
            }

            public override TResult? TryGetElementAt(int index, out bool found)
            {
                if ((uint)index <= (uint)(_maxIndexInclusive - _minIndexInclusive) && index < _source.Count - _minIndexInclusive)
                {
                    found = true;
                    return _selector(_source[_minIndexInclusive + index]);
                }

                found = false;
                return default;
            }

            public override TResult? TryGetFirst(out bool found)
            {
                if (_source.Count > _minIndexInclusive)
                {
                    found = true;
                    return _selector(_source[_minIndexInclusive]);
                }

                found = false;
                return default;
            }

            public override TResult? TryGetLast(out bool found)
            {
                int lastIndex = _source.Count - 1;
                if (lastIndex >= _minIndexInclusive)
                {
                    found = true;
                    return _selector(_source[Math.Min(lastIndex, _maxIndexInclusive)]);
                }

                found = false;
                return default;
            }

            private int Count
            {
                get
                {
                    int count = _source.Count;
                    if (count <= _minIndexInclusive)
                    {
                        return 0;
                    }

                    return Math.Min(count - 1, _maxIndexInclusive) - _minIndexInclusive + 1;
                }
            }

            public override TResult[] ToArray()
            {
                int count = Count;
                if (count == 0)
                {
                    return [];
                }

                TResult[] array = new TResult[count];
                Fill(_source, array, _selector, _minIndexInclusive);

                return array;
            }

            public override List<TResult> ToList()
            {
                int count = Count;
                if (count == 0)
                {
                    return new List<TResult>();
                }

                List<TResult> list = new List<TResult>(count);
                Fill(_source, SetCountAndGetSpan(list, count), _selector, _minIndexInclusive);

                return list;
            }

            private static void Fill(IList<TSource> source, Span<TResult> destination, Func<TSource, TResult> func, int sourceIndex)
            {
                for (int i = 0; i < destination.Length; i++, sourceIndex++)
                {
                    destination[i] = func(source[sourceIndex]);
                }
            }

            public override int GetCount(bool onlyIfCheap)
            {
                // In case someone uses Count() to force evaluation of
                // the selector, run it provided `onlyIfCheap` is false.

                int count = Count;

                if (!onlyIfCheap)
                {
                    int end = _minIndexInclusive + count;
                    for (int i = _minIndexInclusive; i != end; ++i)
                    {
                        _selector(_source[i]);
                    }
                }

                return count;
            }
        }
    }
}
