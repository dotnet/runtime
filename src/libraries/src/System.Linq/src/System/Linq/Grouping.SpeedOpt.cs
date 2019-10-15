// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq
{
    internal sealed partial class GroupedResultEnumerable<TSource, TKey, TElement, TResult> : IIListProvider<TResult>
    {
        public TResult[] ToArray() =>
            Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).ToArray(_resultSelector);

        public List<TResult> ToList() =>
            Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).ToList(_resultSelector);

        public int GetCount(bool onlyIfCheap) =>
            onlyIfCheap ? -1 : Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).Count;
    }

    internal sealed partial class GroupedResultEnumerable<TSource, TKey, TResult> : IIListProvider<TResult>
    {
        public TResult[] ToArray() =>
            Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).ToArray(_resultSelector);

        public List<TResult> ToList() =>
            Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).ToList(_resultSelector);

        public int GetCount(bool onlyIfCheap) =>
            onlyIfCheap ? -1 : Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).Count;
    }

    internal sealed partial class GroupedEnumerable<TSource, TKey, TElement> : IIListProvider<IGrouping<TKey, TElement>>
    {
        public IGrouping<TKey, TElement>[] ToArray()
        {
            IIListProvider<IGrouping<TKey, TElement>> lookup = Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer);
            return lookup.ToArray();
        }

        public List<IGrouping<TKey, TElement>> ToList()
        {
            IIListProvider<IGrouping<TKey, TElement>> lookup = Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer);
            return lookup.ToList();
        }

        public int GetCount(bool onlyIfCheap) =>
            onlyIfCheap ? -1 : Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).Count;
    }

    internal sealed partial class GroupedEnumerable<TSource, TKey> : IIListProvider<IGrouping<TKey, TSource>>
    {
        public IGrouping<TKey, TSource>[] ToArray()
        {
            IIListProvider<IGrouping<TKey, TSource>> lookup = Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer);
            return lookup.ToArray();
        }

        public List<IGrouping<TKey, TSource>> ToList()
        {
            IIListProvider<IGrouping<TKey, TSource>> lookup = Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer);
            return lookup.ToList();
        }

        public int GetCount(bool onlyIfCheap) =>
            onlyIfCheap ? -1 : Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).Count;
    }
}
