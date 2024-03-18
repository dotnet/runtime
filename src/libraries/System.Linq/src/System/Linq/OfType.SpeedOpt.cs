// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class OfTypeIterator<TResult>
        {
            public override int GetCount(bool onlyIfCheap)
            {
                if (onlyIfCheap)
                {
                    return -1;
                }

                int count = 0;
                foreach (object? item in _source)
                {
                    if (item is TResult)
                    {
                        checked { count++; }
                    }
                }

                return count;
            }

            public override TResult[] ToArray()
            {
                SegmentedArrayBuilder<TResult>.ScratchBuffer scratch = default;
                SegmentedArrayBuilder<TResult> builder = new(scratch);

                foreach (object? item in _source)
                {
                    if (item is TResult castItem)
                    {
                        builder.Add(castItem);
                    }
                }

                TResult[] result = builder.ToArray();
                builder.Dispose();

                return result;
            }

            public override List<TResult> ToList()
            {
                var list = new List<TResult>();

                foreach (object? item in _source)
                {
                    if (item is TResult castItem)
                    {
                        list.Add(castItem);
                    }
                }

                return list;
            }

            public override TResult? TryGetFirst(out bool found)
            {
                foreach (object? item in _source)
                {
                    if (item is TResult castItem)
                    {
                        found = true;
                        return castItem;
                    }
                }

                found = false;
                return default;
            }

            public override TResult? TryGetLast(out bool found)
            {
                IEnumerator e = _source.GetEnumerator();
                try
                {
                    if (e.MoveNext())
                    {
                        do
                        {
                            if (e.Current is TResult last)
                            {
                                found = true;

                                while (e.MoveNext())
                                {
                                    if (e.Current is TResult castCurrent)
                                    {
                                        last = castCurrent;
                                    }
                                }

                                return last;
                            }
                        }
                        while (e.MoveNext());
                    }
                }
                finally
                {
                    (e as IDisposable)?.Dispose();
                }

                found = false;
                return default;
            }

            public override TResult? TryGetElementAt(int index, out bool found)
            {
                if (index >= 0)
                {
                    foreach (object? item in _source)
                    {
                        if (item is TResult castItem)
                        {
                            if (index == 0)
                            {
                                found = true;
                                return castItem;
                            }

                            index--;
                        }
                    }
                }

                found = false;
                return default;
            }

            public override IEnumerable<TResult2> Select<TResult2>(Func<TResult, TResult2> selector)
            {
                // If the source is any generic enumerable of a reference type, which should be the 90% case, it'll covariantly
                // implement IEnumerable<object>, and we can optimize the OfType().Select case by treating the OfType instead like
                // a Where, using the same WhereSelectIterators that are used for Where.Select.
                if (!typeof(TResult).IsValueType && _source is IEnumerable<object> objectSource)
                {
                    // Unsafe.As here is safe because we're only dealing with reference types, and we know by construction that only
                    // TResult instances will be passed in. Using Unsafe.As allows us to avoid an extra closure and delegate allocation.
                    Func<object, TResult2> localSelector =
#if DEBUG
                        o =>
                        {
                            Debug.Assert(o is TResult);
                            return selector((TResult)o);
                        };
#else
                        Unsafe.As<Func<object, TResult2>>(selector);
#endif

                    // We can special-case arrays and IEnumerable to use the corresponding WhereSelectIterators because
                    // they're covariant. It's not worthwhile checking for List<T> to use the ListWhereSelectIterator
                    // because List<> is not covariant.
                    Func<object, bool> isTResult = static o => o is TResult;
                    return objectSource is object[] array ?
                        new ArrayWhereSelectIterator<object, TResult2>(array, isTResult, localSelector) :
                        new IEnumerableWhereSelectIterator<object, TResult2>(objectSource, isTResult, localSelector);
                }

                return base.Select(selector);
            }
        }
    }
}
