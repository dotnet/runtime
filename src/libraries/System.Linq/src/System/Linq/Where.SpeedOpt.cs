// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    /// <summary>Provides a set of <see langword="static" /> (<see langword="Shared" /> in Visual Basic) methods for querying objects that implement <see cref="System.Collections.Generic.IEnumerable{T}" />.</summary>
    /// <remarks>The methods in this class provide an implementation of the standard query operators for querying data sources that implement <see cref="System.Collections.Generic.IEnumerable{T}" />. The standard query operators are general purpose methods that follow the LINQ pattern and enable you to express traversal, filter, and projection operations over data in any .NET-based programming language.
    /// The majority of the methods in this class are defined as extension methods that extend <see cref="System.Collections.Generic.IEnumerable{T}" />. This means they can be called like an instance method on any object that implements <see cref="System.Collections.Generic.IEnumerable{T}" />.
    /// Methods that are used in a query that returns a sequence of values do not consume the target data until the query object is enumerated. This is known as deferred execution. Methods that are used in a query that returns a singleton value execute and consume the target data immediately.</remarks>
    /// <related type="Article" href="https://msdn.microsoft.com/library/24cda21e-8af8-4632-b519-c404a839b9b2">Standard Query Operators Overview</related>
    /// <related type="Article" href="/dotnet/csharp/programming-guide/classes-and-structs/extension-methods">Extension Methods (C# Programming Guide)</related>
    /// <related type="Article" href="/dotnet/visual-basic/programming-guide/language-features/procedures/extension-methods">Extension Methods (Visual Basic)</related>
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
                var builder = new LargeArrayBuilder<TSource>(initialize: true);

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
                var builder = new LargeArrayBuilder<TResult>(initialize: true);

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
