// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class WhereEnumerableIterator<TSource> : IPartition<TSource>
        {
            public int GetCount(bool onlyIfCheap)
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
                        checked
                        {
                            count++;
                        }
                    }
                }

                return count;
            }

            public TSource[] ToArray()
            {
                SegmentedArrayBuilder<TSource>.ScratchBuffer scratch = default;
                SegmentedArrayBuilder<TSource> builder = new(scratch);

                Func<TSource, bool> predicate = _predicate;
                foreach (TSource item in _source)
                {
                    if (predicate(item))
                    {
                        builder.Add(item);
                    }
                }

                TSource[] result = builder.ToArray();
                builder.Dispose();

                return result;
            }

            public List<TSource> ToList()
            {
                var list = new List<TSource>();

                Func<TSource, bool> predicate = _predicate;
                foreach (TSource item in _source)
                {
                    if (predicate(item))
                    {
                        list.Add(item);
                    }
                }

                return list;
            }

            public TSource? TryGetFirst(out bool found)
            {
                Func<TSource, bool> predicate = _predicate;

                foreach (TSource item in _source)
                {
                    if (predicate(item))
                    {
                        found = true;
                        return item;
                    }
                }

                found = false;
                return default;
            }

            public TSource? TryGetLast(out bool found)
            {
                using IEnumerator<TSource> e = _source.GetEnumerator();

                if (e.MoveNext())
                {
                    Func<TSource, bool> predicate = _predicate;
                    TSource? last = default;
                    do
                    {
                        TSource current = e.Current;
                        if (predicate(current))
                        {
                            last = current;
                            found = true;

                            while (e.MoveNext())
                            {
                                current = e.Current;
                                if (predicate(current))
                                {
                                    last = current;
                                }
                            }

                            return last;
                        }
                    }
                    while (e.MoveNext());
                }

                found = false;
                return default;
            }

            public TSource? TryGetElementAt(int index, out bool found)
            {
                if (index >= 0)
                {
                    Func<TSource, bool> predicate = _predicate;

                    foreach (TSource item in _source)
                    {
                        if (predicate(item))
                        {
                            if (index == 0)
                            {
                                found = true;
                                return item;
                            }

                            index--;
                        }
                    }
                }

                found = false;
                return default;
            }

            public IPartition<TSource>? Skip(int count) => new EnumerablePartition<TSource>(this, count, -1);

            public IPartition<TSource>? Take(int count) => new EnumerablePartition<TSource>(this, 0, count - 1);
        }

        internal sealed partial class WhereArrayIterator<TSource> : IPartition<TSource>
        {
            public int GetCount(bool onlyIfCheap) => GetCount(onlyIfCheap, _source, _predicate);

            public static int GetCount(bool onlyIfCheap, ReadOnlySpan<TSource> source, Func<TSource, bool> predicate)
            {
                // In case someone uses Count() to force evaluation of
                // the selector, run it provided `onlyIfCheap` is false.

                if (onlyIfCheap)
                {
                    return -1;
                }

                int count = 0;

                foreach (TSource item in source)
                {
                    if (predicate(item))
                    {
                        checked { count++; }
                    }
                }

                return count;
            }

            public TSource[] ToArray() => ToArray(_source, _predicate);

            public static TSource[] ToArray(ReadOnlySpan<TSource> source, Func<TSource, bool> predicate)
            {
                SegmentedArrayBuilder<TSource>.ScratchBuffer scratch = default;
                SegmentedArrayBuilder<TSource> builder = new(scratch);

                foreach (TSource item in source)
                {
                    if (predicate(item))
                    {
                        builder.Add(item);
                    }
                }

                TSource[] result = builder.ToArray();
                builder.Dispose();

                return result;
            }

            public List<TSource> ToList() => ToList(_source, _predicate);

            public static List<TSource> ToList(ReadOnlySpan<TSource> source, Func<TSource, bool> predicate)
            {
                var list = new List<TSource>();

                foreach (TSource item in source)
                {
                    if (predicate(item))
                    {
                        list.Add(item);
                    }
                }

                return list;
            }

            public TSource? TryGetFirst(out bool found)
            {
                Func<TSource, bool> predicate = _predicate;

                foreach (TSource item in _source)
                {
                    if (predicate(item))
                    {
                        found = true;
                        return item;
                    }
                }

                found = false;
                return default;
            }

            public TSource? TryGetLast(out bool found)
            {
                TSource[] source = _source;
                Func<TSource, bool> predicate = _predicate;

                for (int i = source.Length - 1; i >= 0; i--)
                {
                    if (predicate(source[i]))
                    {
                        found = true;
                        return source[i];
                    }
                }

                found = false;
                return default;
            }

            public TSource? TryGetElementAt(int index, out bool found)
            {
                if (index >= 0)
                {
                    Func<TSource, bool> predicate = _predicate;

                    foreach (TSource item in _source)
                    {
                        if (predicate(item))
                        {
                            if (index == 0)
                            {
                                found = true;
                                return item;
                            }

                            index--;
                        }
                    }
                }

                found = false;
                return default;
            }

            public IPartition<TSource>? Skip(int count) => new EnumerablePartition<TSource>(this, count, -1);

            public IPartition<TSource>? Take(int count) => new EnumerablePartition<TSource>(this, 0, count - 1);
        }

        private sealed partial class WhereListIterator<TSource> : Iterator<TSource>, IPartition<TSource>
        {
            public int GetCount(bool onlyIfCheap) => WhereArrayIterator<TSource>.GetCount(onlyIfCheap, CollectionsMarshal.AsSpan(_source), _predicate);

            public TSource[] ToArray() => WhereArrayIterator<TSource>.ToArray(CollectionsMarshal.AsSpan(_source), _predicate);

            public List<TSource> ToList() => WhereArrayIterator<TSource>.ToList(CollectionsMarshal.AsSpan(_source), _predicate);

            public TSource? TryGetFirst(out bool found)
            {
                Func<TSource, bool> predicate = _predicate;

                foreach (TSource item in CollectionsMarshal.AsSpan(_source))
                {
                    if (predicate(item))
                    {
                        found = true;
                        return item;
                    }
                }

                found = false;
                return default;
            }

            public TSource? TryGetLast(out bool found)
            {
                ReadOnlySpan<TSource> source = CollectionsMarshal.AsSpan(_source);
                Func<TSource, bool> predicate = _predicate;

                for (int i = source.Length - 1; i >= 0; i--)
                {
                    if (predicate(source[i]))
                    {
                        found = true;
                        return source[i];
                    }
                }

                found = false;
                return default;
            }

            public TSource? TryGetElementAt(int index, out bool found)
            {
                if (index >= 0)
                {
                    Func<TSource, bool> predicate = _predicate;

                    foreach (TSource item in CollectionsMarshal.AsSpan(_source))
                    {
                        if (predicate(item))
                        {
                            if (index == 0)
                            {
                                found = true;
                                return item;
                            }

                            index--;
                        }
                    }
                }

                found = false;
                return default;
            }

            public IPartition<TSource>? Skip(int count) => new EnumerablePartition<TSource>(this, count, -1);

            public IPartition<TSource>? Take(int count) => new EnumerablePartition<TSource>(this, 0, count - 1);
        }

        private sealed partial class WhereSelectArrayIterator<TSource, TResult> : IPartition<TResult>
        {
            public int GetCount(bool onlyIfCheap) => GetCount(onlyIfCheap, _source, _predicate, _selector);

            public static int GetCount(bool onlyIfCheap, ReadOnlySpan<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
                // In case someone uses Count() to force evaluation of
                // the selector, run it provided `onlyIfCheap` is false.

                if (onlyIfCheap)
                {
                    return -1;
                }

                int count = 0;

                foreach (TSource item in source)
                {
                    if (predicate(item))
                    {
                        selector(item);
                        checked
                        {
                            count++;
                        }
                    }
                }

                return count;
            }

            public TResult[] ToArray() => ToArray(_source, _predicate, _selector);

            public static TResult[] ToArray(ReadOnlySpan<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
                SegmentedArrayBuilder<TResult>.ScratchBuffer scratch = default;
                SegmentedArrayBuilder<TResult> builder = new(scratch);

                foreach (TSource item in source)
                {
                    if (predicate(item))
                    {
                        builder.Add(selector(item));
                    }
                }

                TResult[] result = builder.ToArray();
                builder.Dispose();

                return result;
            }

            public List<TResult> ToList() => ToList(_source, _predicate, _selector);

            public static List<TResult> ToList(ReadOnlySpan<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
            {
                var list = new List<TResult>();

                foreach (TSource item in source)
                {
                    if (predicate(item))
                    {
                        list.Add(selector(item));
                    }
                }

                return list;
            }

            public TResult? TryGetFirst(out bool found) => TryGetFirst(_source, _predicate, _selector, out found);

            public static TResult? TryGetFirst(ReadOnlySpan<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector, out bool found)
            {
                foreach (TSource item in source)
                {
                    if (predicate(item))
                    {
                        found = true;
                        return selector(item);
                    }
                }

                found = false;
                return default;
            }

            public TResult? TryGetLast(out bool found) => TryGetLast(_source, _predicate, _selector, out found);

            public static TResult? TryGetLast(ReadOnlySpan<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector, out bool found)
            {
                for (int i = source.Length - 1; i >= 0; i--)
                {
                    if (predicate(source[i]))
                    {
                        found = true;
                        return selector(source[i]);
                    }
                }

                found = false;
                return default;
            }

            public TResult? TryGetElementAt(int index, out bool found) => TryGetElementAt(_source, _predicate, _selector, index, out found);

            public static TResult? TryGetElementAt(ReadOnlySpan<TSource> source, Func<TSource, bool> predicate, Func<TSource, TResult> selector, int index, out bool found)
            {
                if (index >= 0)
                {
                    foreach (TSource item in source)
                    {
                        if (predicate(item))
                        {
                            if (index == 0)
                            {
                                found = true;
                                return selector(item);
                            }

                            index--;
                        }
                    }
                }

                found = false;
                return default;
            }

            public IPartition<TResult>? Skip(int count) => new EnumerablePartition<TResult>(this, count, -1);

            public IPartition<TResult>? Take(int count) => new EnumerablePartition<TResult>(this, 0, count - 1);
        }

        private sealed partial class WhereSelectListIterator<TSource, TResult> : IPartition<TResult>
        {
            public int GetCount(bool onlyIfCheap) => WhereSelectArrayIterator<TSource, TResult>.GetCount(onlyIfCheap, CollectionsMarshal.AsSpan(_source), _predicate, _selector);

            public TResult[] ToArray() => WhereSelectArrayIterator<TSource, TResult>.ToArray(CollectionsMarshal.AsSpan(_source), _predicate, _selector);

            public List<TResult> ToList() => WhereSelectArrayIterator<TSource, TResult>.ToList(CollectionsMarshal.AsSpan(_source), _predicate, _selector);

            public TResult? TryGetElementAt(int index, out bool found) => WhereSelectArrayIterator<TSource, TResult>.TryGetElementAt(CollectionsMarshal.AsSpan(_source), _predicate, _selector, index, out found);

            public TResult? TryGetFirst(out bool found) => WhereSelectArrayIterator<TSource, TResult>.TryGetFirst(CollectionsMarshal.AsSpan(_source), _predicate, _selector, out found);

            public TResult? TryGetLast(out bool found) => WhereSelectArrayIterator<TSource, TResult>.TryGetLast(CollectionsMarshal.AsSpan(_source), _predicate, _selector, out found);

            public IPartition<TResult>? Skip(int count) => new EnumerablePartition<TResult>(this, count, -1);

            public IPartition<TResult>? Take(int count) => new EnumerablePartition<TResult>(this, 0, count - 1);
        }

        private sealed partial class WhereSelectEnumerableIterator<TSource, TResult> : IPartition<TResult>
        {
            public int GetCount(bool onlyIfCheap)
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
                    if (_predicate(item))
                    {
                        _selector(item);
                        checked
                        {
                            count++;
                        }
                    }
                }

                return count;
            }

            public TResult[] ToArray()
            {
                SegmentedArrayBuilder<TResult>.ScratchBuffer scratch = default;
                SegmentedArrayBuilder<TResult> builder = new(scratch);

                Func<TSource, bool> predicate = _predicate;
                Func<TSource, TResult> selector = _selector;
                foreach (TSource item in _source)
                {
                    if (predicate(item))
                    {
                        builder.Add(selector(item));
                    }
                }

                TResult[] result = builder.ToArray();
                builder.Dispose();

                return result;
            }

            public List<TResult> ToList()
            {
                var list = new List<TResult>();

                Func<TSource, bool> predicate = _predicate;
                Func<TSource, TResult> selector = _selector;
                foreach (TSource item in _source)
                {
                    if (predicate(item))
                    {
                        list.Add(selector(item));
                    }
                }

                return list;
            }

            public TResult? TryGetFirst(out bool found)
            {
                Func<TSource, bool> predicate = _predicate;

                foreach (TSource item in _source)
                {
                    if (predicate(item))
                    {
                        found = true;
                        return _selector(item);
                    }
                }

                found = false;
                return default;
            }

            public TResult? TryGetLast(out bool found)
            {
                using IEnumerator<TSource> e = _source.GetEnumerator();

                if (e.MoveNext())
                {
                    Func<TSource, bool> predicate = _predicate;
                    TSource? last = default;
                    do
                    {
                        TSource current = e.Current;
                        if (predicate(current))
                        {
                            last = current;
                            found = true;

                            while (e.MoveNext())
                            {
                                current = e.Current;
                                if (predicate(current))
                                {
                                    last = current;
                                }
                            }

                            return _selector(last);
                        }
                    }
                    while (e.MoveNext());
                }

                found = false;
                return default;
            }

            public TResult? TryGetElementAt(int index, out bool found)
            {
                if (index >= 0)
                {
                    Func<TSource, bool> predicate = _predicate;

                    foreach (TSource item in _source)
                    {
                        if (predicate(item))
                        {
                            if (index == 0)
                            {
                                found = true;
                                return _selector(item);
                            }

                            index--;
                        }
                    }
                }

                found = false;
                return default;
            }

            public IPartition<TResult>? Skip(int count) => new EnumerablePartition<TResult>(this, count, -1);

            public IPartition<TResult>? Take(int count) => new EnumerablePartition<TResult>(this, 0, count - 1);
        }
    }
}
