// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class IEnumerableWhereIterator<TSource>
        {
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
                        checked
                        {
                            count++;
                        }
                    }
                }

                return count;
            }

            public override TSource[] ToArray()
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

            public override List<TSource> ToList()
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

            public override TSource? TryGetFirst(out bool found)
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

            public override TSource? TryGetLast(out bool found)
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

            public override TSource? TryGetElementAt(int index, out bool found)
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
        }

        internal sealed partial class ArrayWhereIterator<TSource>
        {
            public override int GetCount(bool onlyIfCheap) => GetCount(onlyIfCheap, _source, _predicate);

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

            public override TSource[] ToArray() => ToArray(_source, _predicate);

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

            public override List<TSource> ToList() => ToList(_source, _predicate);

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

            public override TSource? TryGetFirst(out bool found)
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

            public override TSource? TryGetLast(out bool found)
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

            public override TSource? TryGetElementAt(int index, out bool found)
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
        }

        private sealed partial class ListWhereIterator<TSource> : Iterator<TSource>
        {
            public override int GetCount(bool onlyIfCheap) => ArrayWhereIterator<TSource>.GetCount(onlyIfCheap, CollectionsMarshal.AsSpan(_source), _predicate);

            public override TSource[] ToArray() => ArrayWhereIterator<TSource>.ToArray(CollectionsMarshal.AsSpan(_source), _predicate);

            public override List<TSource> ToList() => ArrayWhereIterator<TSource>.ToList(CollectionsMarshal.AsSpan(_source), _predicate);

            public override TSource? TryGetFirst(out bool found)
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

            public override TSource? TryGetLast(out bool found)
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

            public override TSource? TryGetElementAt(int index, out bool found)
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
        }

        private sealed partial class ArrayWhereSelectIterator<TSource, TResult>
        {
            public override int GetCount(bool onlyIfCheap) => GetCount(onlyIfCheap, _source, _predicate, _selector);

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

            public override TResult[] ToArray() => ToArray(_source, _predicate, _selector);

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

            public override List<TResult> ToList() => ToList(_source, _predicate, _selector);

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

            public override TResult? TryGetFirst(out bool found) => TryGetFirst(_source, _predicate, _selector, out found);

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

            public override TResult? TryGetLast(out bool found) => TryGetLast(_source, _predicate, _selector, out found);

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

            public override TResult? TryGetElementAt(int index, out bool found) => TryGetElementAt(_source, _predicate, _selector, index, out found);

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
        }

        private sealed partial class ListWhereSelectIterator<TSource, TResult>
        {
            public override int GetCount(bool onlyIfCheap) => ArrayWhereSelectIterator<TSource, TResult>.GetCount(onlyIfCheap, CollectionsMarshal.AsSpan(_source), _predicate, _selector);

            public override TResult[] ToArray() => ArrayWhereSelectIterator<TSource, TResult>.ToArray(CollectionsMarshal.AsSpan(_source), _predicate, _selector);

            public override List<TResult> ToList() => ArrayWhereSelectIterator<TSource, TResult>.ToList(CollectionsMarshal.AsSpan(_source), _predicate, _selector);

            public override TResult? TryGetElementAt(int index, out bool found) => ArrayWhereSelectIterator<TSource, TResult>.TryGetElementAt(CollectionsMarshal.AsSpan(_source), _predicate, _selector, index, out found);

            public override TResult? TryGetFirst(out bool found) => ArrayWhereSelectIterator<TSource, TResult>.TryGetFirst(CollectionsMarshal.AsSpan(_source), _predicate, _selector, out found);

            public override TResult? TryGetLast(out bool found) => ArrayWhereSelectIterator<TSource, TResult>.TryGetLast(CollectionsMarshal.AsSpan(_source), _predicate, _selector, out found);
        }

        private sealed partial class IEnumerableWhereSelectIterator<TSource, TResult>
        {
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

            public override TResult[] ToArray()
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

            public override List<TResult> ToList()
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

            public override TResult? TryGetFirst(out bool found)
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

            public override TResult? TryGetLast(out bool found)
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

            public override TResult? TryGetElementAt(int index, out bool found)
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
        }
    }
}
