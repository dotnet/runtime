// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using static System.Linq.Utilities;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (selector == null)
            {
                throw Error.ArgumentNull(nameof(selector));
            }

            Iterator<TSource> iterator = source as Iterator<TSource>;
            if (iterator != null)
            {
                return iterator.Select(selector);
            }

            IList<TSource> ilist = source as IList<TSource>;
            if (ilist != null)
            {
                TSource[] array = source as TSource[];
                if (array != null)
                {
                    return array.Length == 0 ?
                        EmptyPartition<TResult>.Instance :
                        new SelectArrayIterator<TSource, TResult>(array, selector);
                }

                List<TSource> list = source as List<TSource>;
                if (list != null)
                {
                    return new SelectListIterator<TSource, TResult>(list, selector);
                }

                return new SelectIListIterator<TSource, TResult>(ilist, selector);
            }

            IPartition<TSource> partition = source as IPartition<TSource>;
            if (partition != null)
            {
                return new SelectIPartitionIterator<TSource, TResult>(partition, selector);
            }

            return new SelectEnumerableIterator<TSource, TResult>(source, selector);
        }

        public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (selector == null)
            {
                throw Error.ArgumentNull(nameof(selector));
            }

            return SelectIterator(source, selector);
        }

        private static IEnumerable<TResult> SelectIterator<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            int index = -1;
            foreach (TSource element in source)
            {
                checked
                {
                    index++;
                }

                yield return selector(element, index);
            }
        }

        internal sealed class SelectEnumerableIterator<TSource, TResult> : Iterator<TResult>, IIListProvider<TResult>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly Func<TSource, TResult> _selector;
            private IEnumerator<TSource> _enumerator;

            public SelectEnumerableIterator(IEnumerable<TSource> source, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                _source = source;
                _selector = selector;
            }

            public override Iterator<TResult> Clone()
            {
                return new SelectEnumerableIterator<TSource, TResult>(_source, _selector);
            }

            public override void Dispose()
            {
                if (_enumerator != null)
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
                        _state = 2;
                        goto case 2;
                    case 2:
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

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
            {
                return new SelectEnumerableIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector));
            }

            public TResult[] ToArray()
            {
                var builder = new LargeArrayBuilder<TResult>(initialize: true);
                
                foreach (TSource item in _source)
                {
                    builder.Add(_selector(item));
                }

                return builder.ToArray();
            }

            public List<TResult> ToList() => new List<TResult>(this);

            public int GetCount(bool onlyIfCheap) => onlyIfCheap ? -1 : _source.Count();
        }

        internal sealed class SelectArrayIterator<TSource, TResult> : Iterator<TResult>, IPartition<TResult>
        {
            private readonly TSource[] _source;
            private readonly Func<TSource, TResult> _selector;

            public SelectArrayIterator(TSource[] source, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                Debug.Assert(source.Length > 0); // Caller should check this beforehand and return a cached result
                _source = source;
                _selector = selector;
            }

            public override Iterator<TResult> Clone()
            {
                return new SelectArrayIterator<TSource, TResult>(_source, _selector);
            }

            public override bool MoveNext()
            {
                if (_state < 1 | _state == _source.Length + 1)
                {
                    Dispose();
                    return false;
                }

                int index = _state++ - 1;
                _current = _selector(_source[index]);
                return true;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
            {
                return new SelectArrayIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector));
            }

            public TResult[] ToArray()
            {
                // See assert in constructor.
                // Since _source should never be empty, we don't check for 0/return Array.Empty.
                Debug.Assert(_source.Length > 0);

                var results = new TResult[_source.Length];
                for (int i = 0; i < results.Length; i++)
                {
                    results[i] = _selector(_source[i]);
                }

                return results;
            }

            public List<TResult> ToList()
            {
                TSource[] source = _source;
                var results = new List<TResult>(source.Length);
                for (int i = 0; i < source.Length; i++)
                {
                    results.Add(_selector(source[i]));
                }

                return results;
            }

            public int GetCount(bool onlyIfCheap)
            {
                return _source.Length;
            }

            public IPartition<TResult> Skip(int count)
            {
                Debug.Assert(count > 0);
                if (count >= _source.Length)
                {
                    return EmptyPartition<TResult>.Instance;
                }

                return new SelectListPartitionIterator<TSource, TResult>(_source, _selector, count, int.MaxValue);
            }

            public IPartition<TResult> Take(int count)
            {
                return count >= _source.Length ? (IPartition<TResult>)this : new SelectListPartitionIterator<TSource, TResult>(_source, _selector, 0, count - 1);
            }

            public TResult TryGetElementAt(int index, out bool found)
            {
                if ((uint)index < (uint)_source.Length)
                {
                    found = true;
                    return _selector(_source[index]);
                }

                found = false;
                return default(TResult);
            }

            public TResult TryGetFirst(out bool found)
            {
                Debug.Assert(_source.Length > 0); // See assert in constructor

                found = true;
                return _selector(_source[0]);
            }

            public TResult TryGetLast(out bool found)
            {
                Debug.Assert(_source.Length > 0); // See assert in constructor

                found = true;
                return _selector(_source[_source.Length - 1]);
            }
        }

        internal sealed class SelectListIterator<TSource, TResult> : Iterator<TResult>, IPartition<TResult>
        {
            private readonly List<TSource> _source;
            private readonly Func<TSource, TResult> _selector;
            private List<TSource>.Enumerator _enumerator;

            public SelectListIterator(List<TSource> source, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                _source = source;
                _selector = selector;
            }

            public override Iterator<TResult> Clone()
            {
                return new SelectListIterator<TSource, TResult>(_source, _selector);
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

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
            {
                return new SelectListIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector));
            }

            public TResult[] ToArray()
            {
                int count = _source.Count;
                if (count == 0)
                {
                    return Array.Empty<TResult>();
                }

                var results = new TResult[count];
                for (int i = 0; i < results.Length; i++)
                {
                    results[i] = _selector(_source[i]);
                }

                return results;
            }

            public List<TResult> ToList()
            {
                int count = _source.Count;
                var results = new List<TResult>(count);
                for (int i = 0; i < count; i++)
                {
                    results.Add(_selector(_source[i]));
                }

                return results;
            }

            public int GetCount(bool onlyIfCheap)
            {
                return _source.Count;
            }

            public IPartition<TResult> Skip(int count)
            {
                Debug.Assert(count > 0);
                return new SelectListPartitionIterator<TSource, TResult>(_source, _selector, count, int.MaxValue);
            }

            public IPartition<TResult> Take(int count)
            {
                return new SelectListPartitionIterator<TSource, TResult>(_source, _selector, 0, count - 1);
            }

            public TResult TryGetElementAt(int index, out bool found)
            {
                if ((uint)index < (uint)_source.Count)
                {
                    found = true;
                    return _selector(_source[index]);
                }

                found = false;
                return default(TResult);
            }

            public TResult TryGetFirst(out bool found)
            {
                if (_source.Count != 0)
                {
                    found = true;
                    return _selector(_source[0]);
                }

                found = false;
                return default(TResult);
            }

            public TResult TryGetLast(out bool found)
            {
                int len = _source.Count;
                if (len != 0)
                {
                    found = true;
                    return _selector(_source[len - 1]);
                }

                found = false;
                return default(TResult);
            }
        }

        internal sealed class SelectIListIterator<TSource, TResult> : Iterator<TResult>, IPartition<TResult>
        {
            private readonly IList<TSource> _source;
            private readonly Func<TSource, TResult> _selector;
            private IEnumerator<TSource> _enumerator;

            public SelectIListIterator(IList<TSource> source, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                _source = source;
                _selector = selector;
            }

            public override Iterator<TResult> Clone()
            {
                return new SelectIListIterator<TSource, TResult>(_source, _selector);
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
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }

                base.Dispose();
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
            {
                return new SelectIListIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector));
            }

            public TResult[] ToArray()
            {
                int count = _source.Count;
                if (count == 0)
                {
                    return Array.Empty<TResult>();
                }

                var results = new TResult[count];
                for (int i = 0; i < results.Length; i++)
                {
                    results[i] = _selector(_source[i]);
                }

                return results;
            }

            public List<TResult> ToList()
            {
                int count = _source.Count;
                var results = new List<TResult>(count);
                for (int i = 0; i < count; i++)
                {
                    results.Add(_selector(_source[i]));
                }

                return results;
            }

            public int GetCount(bool onlyIfCheap)
            {
                return _source.Count;
            }

            public IPartition<TResult> Skip(int count)
            {
                Debug.Assert(count > 0);
                return new SelectListPartitionIterator<TSource, TResult>(_source, _selector, count, int.MaxValue);
            }

            public IPartition<TResult> Take(int count)
            {
                return new SelectListPartitionIterator<TSource, TResult>(_source, _selector, 0, count - 1);
            }

            public TResult TryGetElementAt(int index, out bool found)
            {
                if ((uint)index < (uint)_source.Count)
                {
                    found = true;
                    return _selector(_source[index]);
                }

                found = false;
                return default(TResult);
            }

            public TResult TryGetFirst(out bool found)
            {
                if (_source.Count != 0)
                {
                    found = true;
                    return _selector(_source[0]);
                }

                found = false;
                return default(TResult);
            }

            public TResult TryGetLast(out bool found)
            {
                int len = _source.Count;
                if (len != 0)
                {
                    found = true;
                    return _selector(_source[len - 1]);
                }

                found = false;
                return default(TResult);
            }
        }

        internal sealed class SelectIPartitionIterator<TSource, TResult> : Iterator<TResult>, IPartition<TResult>
        {
            private readonly IPartition<TSource> _source;
            private readonly Func<TSource, TResult> _selector;
            private IEnumerator<TSource> _enumerator;

            public SelectIPartitionIterator(IPartition<TSource> source, Func<TSource, TResult> selector)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                _source = source;
                _selector = selector;
            }

            public override Iterator<TResult> Clone()
            {
                return new SelectIPartitionIterator<TSource, TResult>(_source, _selector);
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
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }

                base.Dispose();
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
            {
                return new SelectIPartitionIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector));
            }

            public IPartition<TResult> Skip(int count)
            {
                Debug.Assert(count > 0);
                return new SelectIPartitionIterator<TSource, TResult>(_source.Skip(count), _selector);
            }

            public IPartition<TResult> Take(int count)
            {
                return new SelectIPartitionIterator<TSource, TResult>(_source.Take(count), _selector);
            }

            public TResult TryGetElementAt(int index, out bool found)
            {
                bool sourceFound;
                TSource input = _source.TryGetElementAt(index, out sourceFound);
                found = sourceFound;
                return sourceFound ? _selector(input) : default(TResult);
            }

            public TResult TryGetFirst(out bool found)
            {
                bool sourceFound;
                TSource input = _source.TryGetFirst(out sourceFound);
                found = sourceFound;
                return sourceFound ? _selector(input) : default(TResult);
            }

            public TResult TryGetLast(out bool found)
            {
                bool sourceFound;
                TSource input = _source.TryGetLast(out sourceFound);
                found = sourceFound;
                return sourceFound ? _selector(input) : default(TResult);
            }

            private TResult[] LazyToArray()
            {
                Debug.Assert(_source.GetCount(onlyIfCheap: true) == -1);

                var builder = new LargeArrayBuilder<TResult>(initialize: true);
                foreach (TSource input in _source)
                {
                    builder.Add(_selector(input));
                }
                return builder.ToArray();
            }

            private TResult[] PreallocatingToArray(int count)
            {
                Debug.Assert(count > 0);
                Debug.Assert(count == _source.GetCount(onlyIfCheap: true));

                TResult[] array = new TResult[count];
                int index = 0;
                foreach (TSource input in _source)
                {
                    array[index] = _selector(input);
                    ++index;
                }

                return array;
            }

            public TResult[] ToArray()
            {
                int count = _source.GetCount(onlyIfCheap: true);
                switch (count)
                {
                    case -1:
                        return LazyToArray();
                    case 0:
                        return Array.Empty<TResult>();
                    default:
                        return PreallocatingToArray(count);
                }
            }

            public List<TResult> ToList()
            {
                int count = _source.GetCount(onlyIfCheap: true);
                List<TResult> list;
                switch (count)
                {
                    case -1:
                        list = new List<TResult>();
                        break;
                    case 0:
                        return new List<TResult>();
                    default:
                        list = new List<TResult>(count);
                        break;
                }

                foreach (TSource input in _source)
                {
                    list.Add(_selector(input));
                }

                return list;
            }

            public int GetCount(bool onlyIfCheap)
            {
                return _source.GetCount(onlyIfCheap);
            }
        }

        private sealed class SelectListPartitionIterator<TSource, TResult> : Iterator<TResult>, IPartition<TResult>
        {
            private readonly IList<TSource> _source;
            private readonly Func<TSource, TResult> _selector;
            private readonly int _minIndexInclusive;
            private readonly int _maxIndexInclusive;
            private int _index;

            public SelectListPartitionIterator(IList<TSource> source, Func<TSource, TResult> selector, int minIndexInclusive, int maxIndexInclusive)
            {
                Debug.Assert(source != null);
                Debug.Assert(selector != null);
                Debug.Assert(minIndexInclusive >= 0);
                Debug.Assert(minIndexInclusive <= maxIndexInclusive);
                _source = source;
                _selector = selector;
                _minIndexInclusive = minIndexInclusive;
                _maxIndexInclusive = maxIndexInclusive;
                _index = minIndexInclusive;
            }

            public override Iterator<TResult> Clone()
            {
                return new SelectListPartitionIterator<TSource, TResult>(_source, _selector, _minIndexInclusive, _maxIndexInclusive);
            }

            public override bool MoveNext()
            {
                if ((_state == 1 & _index <= _maxIndexInclusive) && _index < _source.Count)
                {
                    _current = _selector(_source[_index]);
                    ++_index;
                    return true;
                }

                Dispose();
                return false;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
            {
                return new SelectListPartitionIterator<TSource, TResult2>(_source, CombineSelectors(_selector, selector), _minIndexInclusive, _maxIndexInclusive);
            }

            public IPartition<TResult> Skip(int count)
            {
                Debug.Assert(count > 0);
                int minIndex = _minIndexInclusive + count;
                return (uint)minIndex > (uint)_maxIndexInclusive ? EmptyPartition<TResult>.Instance : new SelectListPartitionIterator<TSource, TResult>(_source, _selector, minIndex, _maxIndexInclusive);
            }

            public IPartition<TResult> Take(int count)
            {
                int maxIndex = _minIndexInclusive + count - 1;
                return (uint)maxIndex >= (uint)_maxIndexInclusive ? this : new SelectListPartitionIterator<TSource, TResult>(_source, _selector, _minIndexInclusive, maxIndex);
            }

            public TResult TryGetElementAt(int index, out bool found)
            {
                if ((uint)index <= (uint)(_maxIndexInclusive - _minIndexInclusive) && index < _source.Count - _minIndexInclusive)
                {
                    found = true;
                    return _selector(_source[_minIndexInclusive + index]);
                }

                found = false;
                return default(TResult);
            }

            public TResult TryGetFirst(out bool found)
            {
                if (_source.Count > _minIndexInclusive)
                {
                    found = true;
                    return _selector(_source[_minIndexInclusive]);
                }

                found = false;
                return default(TResult);
            }

            public TResult TryGetLast(out bool found)
            {
                int lastIndex = _source.Count - 1;
                if (lastIndex >= _minIndexInclusive)
                {
                    found = true;
                    return _selector(_source[Math.Min(lastIndex, _maxIndexInclusive)]);
                }

                found = false;
                return default(TResult);
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

            public TResult[] ToArray()
            {
                int count = Count;
                if (count == 0)
                {
                    return Array.Empty<TResult>();
                }

                TResult[] array = new TResult[count];
                for (int i = 0, curIdx = _minIndexInclusive; i != array.Length; ++i, ++curIdx)
                {
                    array[i] = _selector(_source[curIdx]);
                }

                return array;
            }

            public List<TResult> ToList()
            {
                int count = Count;
                if (count == 0)
                {
                    return new List<TResult>();
                }

                List<TResult> list = new List<TResult>(count);
                int end = _minIndexInclusive + count;
                for (int i = _minIndexInclusive; i != end; ++i)
                {
                    list.Add(_selector(_source[i]));
                }

                return list;
            }

            public int GetCount(bool onlyIfCheap)
            {
                return Count;
            }
        }
    }
}
