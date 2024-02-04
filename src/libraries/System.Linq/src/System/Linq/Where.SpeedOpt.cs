// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class WhereEnumerableIterator<TSource> : IIListProvider<TSource>
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
        }

        internal sealed partial class WhereArrayIterator<TSource> : IIListProvider<TSource>
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
        }

        private sealed partial class WhereListIterator<TSource> : Iterator<TSource>, IIListProvider<TSource>
        {
            public int GetCount(bool onlyIfCheap) => WhereArrayIterator<TSource>.GetCount(onlyIfCheap, CollectionsMarshal.AsSpan(_source), _predicate);

            public TSource[] ToArray() => WhereArrayIterator<TSource>.ToArray(CollectionsMarshal.AsSpan(_source), _predicate);

            public List<TSource> ToList()
            {
                var list = new List<TSource>();

                Func<TSource, bool> predicate = _predicate;
                foreach (TSource item in CollectionsMarshal.AsSpan(_source))
                {
                    if (predicate(item))
                    {
                        list.Add(item);
                    }
                }

                return list;
            }
        }

        private sealed partial class WhereSelectArrayIterator<TSource, TResult> : IIListProvider<TResult>
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

            public List<TResult> ToList()
            {
                var list = new List<TResult>();

                Func<TSource, bool> predicate = _predicate;
                foreach (TSource item in _source)
                {
                    if (predicate(item))
                    {
                        list.Add(_selector(item));
                    }
                }

                return list;
            }
        }

        private sealed partial class WhereSelectListIterator<TSource, TResult> : IIListProvider<TResult>
        {
            public int GetCount(bool onlyIfCheap) => WhereSelectArrayIterator<TSource, TResult>.GetCount(onlyIfCheap, CollectionsMarshal.AsSpan(_source), _predicate, _selector);

            public TResult[] ToArray() => WhereSelectArrayIterator<TSource, TResult>.ToArray(CollectionsMarshal.AsSpan(_source), _predicate, _selector);

            public List<TResult> ToList()
            {
                var list = new List<TResult>();

                Func<TSource, bool> predicate = _predicate;
                foreach (TSource item in CollectionsMarshal.AsSpan(_source))
                {
                    if (predicate(item))
                    {
                        list.Add(_selector(item));
                    }
                }

                return list;
            }
        }

        private sealed partial class WhereSelectEnumerableIterator<TSource, TResult> : IIListProvider<TResult>
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
        }
    }
}
