// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

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
                LargeArrayBuilder<TSource> builder = new();

                foreach (TSource item in _source)
                {
                    if (_predicate(item))
                    {
                        builder.Add(item);
                    }
                }

                return builder.ToArray();
            }

            public List<TSource> ToList()
            {
                var list = new List<TSource>();

                foreach (TSource item in _source)
                {
                    if (_predicate(item))
                    {
                        list.Add(item);
                    }
                }

                return list;
            }
        }

        internal sealed partial class WhereArrayIterator<TSource> : IIListProvider<TSource>
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
                var builder = new LargeArrayBuilder<TSource>(_source.Length);

                foreach (TSource item in _source)
                {
                    if (_predicate(item))
                    {
                        builder.Add(item);
                    }
                }

                return builder.ToArray();
            }

            public List<TSource> ToList()
            {
                var list = new List<TSource>();

                foreach (TSource item in _source)
                {
                    if (_predicate(item))
                    {
                        list.Add(item);
                    }
                }

                return list;
            }
        }

        private sealed partial class WhereListIterator<TSource> : Iterator<TSource>, IIListProvider<TSource>
        {
            public int GetCount(bool onlyIfCheap)
            {
                if (onlyIfCheap)
                {
                    return -1;
                }

                int count = 0;

                for (int i = 0; i < _source.Count; i++)
                {
                    TSource item = _source[i];
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
                var builder = new LargeArrayBuilder<TSource>(_source.Count);

                for (int i = 0; i < _source.Count; i++)
                {
                    TSource item = _source[i];
                    if (_predicate(item))
                    {
                        builder.Add(item);
                    }
                }

                return builder.ToArray();
            }

            public List<TSource> ToList()
            {
                var list = new List<TSource>();

                for (int i = 0; i < _source.Count; i++)
                {
                    TSource item = _source[i];
                    if (_predicate(item))
                    {
                        list.Add(item);
                    }
                }

                return list;
            }
        }

        private sealed partial class WhereSelectArrayIterator<TSource, TResult> : IIListProvider<TResult>
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
                var builder = new LargeArrayBuilder<TResult>(_source.Length);

                foreach (TSource item in _source)
                {
                    if (_predicate(item))
                    {
                        builder.Add(_selector(item));
                    }
                }

                return builder.ToArray();
            }

            public List<TResult> ToList()
            {
                var list = new List<TResult>();

                foreach (TSource item in _source)
                {
                    if (_predicate(item))
                    {
                        list.Add(_selector(item));
                    }
                }

                return list;
            }
        }

        private sealed partial class WhereSelectListIterator<TSource, TResult> : IIListProvider<TResult>
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

                for (int i = 0; i < _source.Count; i++)
                {
                    TSource item = _source[i];
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
                var builder = new LargeArrayBuilder<TResult>(_source.Count);

                for (int i = 0; i < _source.Count; i++)
                {
                    TSource item = _source[i];
                    if (_predicate(item))
                    {
                        builder.Add(_selector(item));
                    }
                }

                return builder.ToArray();
            }

            public List<TResult> ToList()
            {
                var list = new List<TResult>();

                for (int i = 0; i < _source.Count; i++)
                {
                    TSource item = _source[i];
                    if (_predicate(item))
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
                LargeArrayBuilder<TResult> builder = new();

                foreach (TSource item in _source)
                {
                    if (_predicate(item))
                    {
                        builder.Add(_selector(item));
                    }
                }

                return builder.ToArray();
            }

            public List<TResult> ToList()
            {
                var list = new List<TResult>();

                foreach (TSource item in _source)
                {
                    if (_predicate(item))
                    {
                        list.Add(_selector(item));
                    }
                }

                return list;
            }
        }
    }
}
