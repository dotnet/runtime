// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) =>
            ToLookup(source, keySelector, comparer: null);

        public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (keySelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            if (IsEmptyArray(source))
            {
                return EmptyLookup<TKey, TSource>.Instance;
            }

            return Lookup<TKey, TSource>.Create(source, keySelector, comparer);
        }

        public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) =>
            ToLookup(source, keySelector, elementSelector, comparer: null);

        public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (keySelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            if (elementSelector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementSelector);
            }

            if (IsEmptyArray(source))
            {
                return EmptyLookup<TKey, TElement>.Instance;
            }

            return Lookup<TKey, TElement>.Create(source, keySelector, elementSelector, comparer);
        }
    }

    public interface ILookup<TKey, TElement> : IEnumerable<IGrouping<TKey, TElement>>
    {
        int Count { get; }

        IEnumerable<TElement> this[TKey key] { get; }

        bool Contains(TKey key);
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(SystemLinq_LookupDebugView<,>))]
    public partial class Lookup<TKey, TElement> : ILookup<TKey, TElement>
    {
        private readonly IEqualityComparer<TKey> _comparer;
        private Grouping<TKey, TElement>[] _groupings;
        private protected Grouping<TKey, TElement>? _lastGrouping;
        private int _count;

        internal static Lookup<TKey, TElement> Create<TSource>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer)
        {
            Debug.Assert(source != null);
            Debug.Assert(keySelector != null);
            Debug.Assert(elementSelector != null);

            var lookup = new CollectionLookup<TKey, TElement>(comparer);
            foreach (TSource item in source)
            {
                lookup.GetGrouping(keySelector(item), create: true)!.Add(elementSelector(item));
            }

            return lookup;
        }

        internal static Lookup<TKey, TElement> Create(IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            Debug.Assert(source != null);
            Debug.Assert(keySelector != null);

            var lookup = new CollectionLookup<TKey, TElement>(comparer);
            foreach (TElement item in source)
            {
                lookup.GetGrouping(keySelector(item), create: true)!.Add(item);
            }

            return lookup;
        }

        internal static Lookup<TKey, TElement> CreateForJoin(IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            var lookup = new CollectionLookup<TKey, TElement>(comparer);
            foreach (TElement item in source)
            {
                TKey key = keySelector(item);
                if (key != null)
                {
                    lookup.GetGrouping(key, create: true)!.Add(item);
                }
            }

            return lookup;
        }

        private protected Lookup(IEqualityComparer<TKey>? comparer)
        {
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
            _groupings = new Grouping<TKey, TElement>[7];
        }

        public int Count => _count;

        public IEnumerable<TElement> this[TKey key] => GetGrouping(key, create: false) ?? Enumerable.Empty<TElement>();

        public bool Contains(TKey key) => GetGrouping(key, create: false) != null;

        public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
        {
            Grouping<TKey, TElement>? g = _lastGrouping;
            if (g != null)
            {
                do
                {
                    g = g._next;

                    Debug.Assert(g != null);
                    yield return g;
                }
                while (g != _lastGrouping);
            }
        }

        internal List<TResult> ToList<TResult>(Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
        {
            List<TResult> list = new List<TResult>(_count);
            Grouping<TKey, TElement>? g = _lastGrouping;
            if (g != null)
            {
                Span<TResult> span = Enumerable.SetCountAndGetSpan(list, _count);
                int index = 0;
                do
                {
                    g = g._next;

                    Debug.Assert(g != null);
                    g.Trim();
                    span[index] = resultSelector(g._key, g._elements);
                    ++index;
                }
                while (g != _lastGrouping);

                Debug.Assert(index == _count, "All list elements were not initialized.");
            }

            return list;
        }

        public IEnumerable<TResult> ApplyResultSelector<TResult>(Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
        {
            Grouping<TKey, TElement>? g = _lastGrouping;
            if (g != null)
            {
                do
                {
                    g = g._next;

                    Debug.Assert(g != null);
                    g.Trim();
                    yield return resultSelector(g._key, g._elements);
                }
                while (g != _lastGrouping);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private int InternalGetHashCode(TKey key)
        {
            // Handle comparer implementations that throw when passed null
            return (key == null) ? 0 : _comparer.GetHashCode(key) & 0x7FFFFFFF;
        }

        internal Grouping<TKey, TElement>? GetGrouping(TKey key, bool create)
        {
            int hashCode = InternalGetHashCode(key);
            for (Grouping<TKey, TElement>? g = _groupings[(uint)hashCode % _groupings.Length]; g != null; g = g._hashNext)
            {
                if (g._hashCode == hashCode && _comparer.Equals(g._key, key))
                {
                    return g;
                }
            }

            if (create)
            {
                if (_count == _groupings.Length)
                {
                    Resize();
                }

                int index = hashCode % _groupings.Length;
                Grouping<TKey, TElement> g = new Grouping<TKey, TElement>(key, hashCode);
                g._hashNext = _groupings[index];
                _groupings[index] = g;
                if (_lastGrouping == null)
                {
                    g._next = g;
                }
                else
                {
                    g._next = _lastGrouping._next;
                    _lastGrouping._next = g;
                }

                _lastGrouping = g;
                _count++;
                return g;
            }

            return null;
        }

        private void Resize()
        {
            int newSize = checked((_count * 2) + 1);
            Grouping<TKey, TElement>[] newGroupings = new Grouping<TKey, TElement>[newSize];
            Grouping<TKey, TElement> g = _lastGrouping!;
            do
            {
                g = g._next!;
                int index = g._hashCode % newSize;
                g._hashNext = newGroupings[index];
                newGroupings[index] = g;
            }
            while (g != _lastGrouping);

            _groupings = newGroupings;
        }
    }

    internal sealed class CollectionLookup<TKey, TElement> : Lookup<TKey, TElement>, ICollection<IGrouping<TKey, TElement>>, IReadOnlyCollection<IGrouping<TKey, TElement>>
    {
        internal CollectionLookup(IEqualityComparer<TKey>? comparer) : base(comparer) { }

        void ICollection<IGrouping<TKey, TElement>>.CopyTo(IGrouping<TKey, TElement>[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
            ArgumentOutOfRangeException.ThrowIfLessThan(array.Length - arrayIndex, Count, nameof(arrayIndex));

            Grouping<TKey, TElement>? g = _lastGrouping;
            if (g != null)
            {
                do
                {
                    g = g._next;
                    Debug.Assert(g != null);

                    array[arrayIndex] = g;
                    ++arrayIndex;
                }
                while (g != _lastGrouping);
            }
        }

        bool ICollection<IGrouping<TKey, TElement>>.Contains(IGrouping<TKey, TElement> item)
        {
            ArgumentNullException.ThrowIfNull(item);
            return GetGrouping(item.Key, create: false) is { } grouping && grouping == item;
        }

        bool ICollection<IGrouping<TKey, TElement>>.IsReadOnly => true;
        void ICollection<IGrouping<TKey, TElement>>.Add(IGrouping<TKey, TElement> item) => throw new NotSupportedException();
        void ICollection<IGrouping<TKey, TElement>>.Clear() => throw new NotSupportedException();
        bool ICollection<IGrouping<TKey, TElement>>.Remove(IGrouping<TKey, TElement> item) => throw new NotSupportedException();
    }

    [DebuggerDisplay("Count = 0")]
    [DebuggerTypeProxy(typeof(SystemLinq_LookupDebugView<,>))]
    internal sealed class EmptyLookup<TKey, TElement> : ILookup<TKey, TElement>, ICollection<IGrouping<TKey, TElement>>, IReadOnlyCollection<IGrouping<TKey, TElement>>
    {
        public static readonly EmptyLookup<TKey, TElement> Instance = new();

        public IEnumerable<TElement> this[TKey key] => [];
        public int Count => 0;

        public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator() => Enumerable.Empty<IGrouping<TKey, TElement>>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Contains(TKey key) => false;
        public bool Contains(IGrouping<TKey, TElement> item) => false;
        public void CopyTo(IGrouping<TKey, TElement>[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        }

        public bool IsReadOnly => true;
        public void Add(IGrouping<TKey, TElement> item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Remove(IGrouping<TKey, TElement> item) => throw new NotSupportedException();
    }
}
