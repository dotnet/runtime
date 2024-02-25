// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        internal sealed partial class GroupByResultIterator<TSource, TKey, TElement, TResult> : IIListProvider<TResult>
        {
            public TResult[] ToArray() =>
                Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).ToArray(_resultSelector);

            public List<TResult> ToList() =>
                Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).ToList(_resultSelector);

            public int GetCount(bool onlyIfCheap) =>
                onlyIfCheap ? -1 : Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).Count;
        }

        internal sealed partial class GroupByResultIterator<TSource, TKey, TResult> : IIListProvider<TResult>
        {
            public TResult[] ToArray() =>
                Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).ToArray(_resultSelector);

            public List<TResult> ToList() =>
                Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).ToList(_resultSelector);

            public int GetCount(bool onlyIfCheap) =>
                onlyIfCheap ? -1 : Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).Count;
        }

        internal sealed partial class GroupByIterator<TSource, TKey, TElement> : IIListProvider<IGrouping<TKey, TElement>>
        {
            public IGrouping<TKey, TElement>[] ToArray() =>
                Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).ToArray();

            public List<IGrouping<TKey, TElement>> ToList() =>
                Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).ToList();

            public int GetCount(bool onlyIfCheap) =>
                onlyIfCheap ? -1 : Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).Count;
        }

        internal sealed partial class GroupByIterator<TSource, TKey> : IIListProvider<IGrouping<TKey, TSource>>
        {
            public IGrouping<TKey, TSource>[] ToArray() =>
                Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).ToArray();

            public List<IGrouping<TKey, TSource>> ToList() =>
                Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).ToList();

            public int GetCount(bool onlyIfCheap) =>
                onlyIfCheap ? -1 : Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).Count;
        }
    }
}
