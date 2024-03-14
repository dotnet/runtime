// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class GroupByResultIterator<TSource, TKey, TElement, TResult>
        {
            public override TResult[] ToArray() =>
                Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).ToArray(_resultSelector);

            public override List<TResult> ToList() =>
                Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).ToList(_resultSelector);

            public override int GetCount(bool onlyIfCheap) =>
                onlyIfCheap ? -1 : Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).Count;
        }

        private sealed partial class GroupByResultIterator<TSource, TKey, TResult>
        {
            public override TResult[] ToArray() =>
                Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).ToArray(_resultSelector);

            public override List<TResult> ToList() =>
                Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).ToList(_resultSelector);

            public override int GetCount(bool onlyIfCheap) =>
                onlyIfCheap ? -1 : Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).Count;
        }

        private sealed partial class GroupByIterator<TSource, TKey, TElement>
        {
            public override IGrouping<TKey, TElement>[] ToArray() =>
                Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).ToArray();

            public override List<IGrouping<TKey, TElement>> ToList() =>
                Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).ToList();

            public override int GetCount(bool onlyIfCheap) =>
                onlyIfCheap ? -1 : Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).Count;
        }

        private sealed partial class GroupByIterator<TSource, TKey>
        {
            public override IGrouping<TKey, TSource>[] ToArray() =>
                Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).ToArray();

            public override List<IGrouping<TKey, TSource>> ToList() =>
                Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).ToList();

            public override int GetCount(bool onlyIfCheap) =>
                onlyIfCheap ? -1 : Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).Count;
        }
    }
}
