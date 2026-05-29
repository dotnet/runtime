// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) =>
            ToLookup(source, keySelector, comparer: null);

        public static ILookup<TKey, TSource> ToLookup<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (keySelector is null)
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
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (keySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            if (elementSelector is null)
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
        // Null keys are not supported by Dictionary, so they are handled separately via _nullKeyGrouping.
#pragma warning disable CS8714 // Nullability of type argument doesn't match 'notnull' constraint.
        private readonly Dictionary<TKey, Grouping<TKey, TElement>> _groupings;
#pragma warning restore CS8714
        // True when a custom comparer was supplied. The default comparer never considers null equal to a
        // non-null key, but a custom comparer might, so null keys must then be routed through the comparer.
        private readonly bool _customComparer;
        private Grouping<TKey, TElement>? _nullKeyGrouping;
        internal Grouping<TKey, TElement>? _lastGrouping;
        private int _count;

        internal static Lookup<TKey, TElement> Create<TSource>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer)
        {
            Debug.Assert(source is not null);
            Debug.Assert(keySelector is not null);
            Debug.Assert(elementSelector is not null);

            var lookup = new CollectionLookup<TKey, TElement>(comparer);
            foreach (TSource item in source)
            {
                lookup.GetGrouping(keySelector(item), create: true)!.Add(elementSelector(item));
            }

            return lookup;
        }

        internal static Lookup<TKey, TElement> Create(IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            Debug.Assert(source is not null);
            Debug.Assert(keySelector is not null);

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
                if (key is not null)
                {
                    lookup.GetGrouping(key, create: true)!.Add(item);
                }
            }

            return lookup;
        }

        private protected Lookup(IEqualityComparer<TKey>? comparer)
        {
#pragma warning disable CS8714 // Nullability of type argument doesn't match 'notnull' constraint.
            _groupings = new Dictionary<TKey, Grouping<TKey, TElement>>(comparer);
#pragma warning restore CS8714
            // Even if a comparer was supplied, it might still be the default; only a non-default comparer
            // can equate null with a non-null key, so only then must null keys be routed through it.
            _customComparer = comparer is not null && !comparer.Equals(EqualityComparer<TKey>.Default);
        }

        public int Count => _count;

        public IEnumerable<TElement> this[TKey key] => GetGrouping(key, create: false) ?? Enumerable.Empty<TElement>();

        public bool Contains(TKey key) => GetGrouping(key, create: false) is not null;

        public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
        {
            Grouping<TKey, TElement>? g = _lastGrouping;
            if (g is not null)
            {
                do
                {
                    g = g._next;

                    Debug.Assert(g is not null);
                    yield return g;
                }
                while (g != _lastGrouping);
            }
        }

        internal List<TResult> ToList<TResult>(Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
        {
            List<TResult> list = new List<TResult>(_count);
            Grouping<TKey, TElement>? g = _lastGrouping;
            if (g is not null)
            {
                Span<TResult> span = Enumerable.SetCountAndGetSpan(list, _count);
                int index = 0;
                do
                {
                    g = g._next;

                    Debug.Assert(g is not null);
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
            if (g is not null)
            {
                do
                {
                    g = g._next;

                    Debug.Assert(g is not null);
                    g.Trim();
                    yield return resultSelector(g._key, g._elements);
                }
                while (g != _lastGrouping);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal Grouping<TKey, TElement>? GetGrouping(TKey key, bool create)
        {
            // Dictionary<TKey, TValue> does not support null keys, so they are tracked separately.
            if (key is null)
            {
                return GetNullKeyGrouping(create);
            }

            // A custom comparer may consider a non-null key equal to null. The historical implementation
            // hashed null to 0 and routed equality through the comparer, so a non-null key merged with the
            // null grouping only when its own hash code (masked to non-negative) was also 0. Preserve that
            // exact behavior so this remains non-breaking, while never passing null to a comparer's GetHashCode.
            if (_nullKeyGrouping is not null && _customComparer)
            {
                IEqualityComparer<TKey> comparer = _groupings.Comparer;
                if ((comparer.GetHashCode(key) & 0x7FFFFFFF) == 0 && comparer.Equals(default, key))
                {
                    return _nullKeyGrouping;
                }
            }

            if (create)
            {
#pragma warning disable CS8714 // Nullability of type argument doesn't match 'notnull' constraint. The null case is handled above.
                ref Grouping<TKey, TElement>? grouping = ref CollectionsMarshal.GetValueRefOrAddDefault(_groupings, key, out _);
#pragma warning restore CS8714
                return grouping ??= CreateGrouping(key);
            }

            _groupings.TryGetValue(key, out Grouping<TKey, TElement>? g);
            return g;
        }

        private Grouping<TKey, TElement>? GetNullKeyGrouping(bool create)
        {
            Grouping<TKey, TElement>? nullGrouping = _nullKeyGrouping;
            if (nullGrouping is not null)
            {
                return nullGrouping;
            }

            // A custom comparer may already have grouped a null-equivalent non-null key. Old behavior only
            // merged keys whose hash code was 0, so look for such a grouping before creating a new one.
            if (_customComparer)
            {
                nullGrouping = FindNullEquivalentGrouping();
            }

            if (create)
            {
                nullGrouping ??= CreateGrouping(default!);
                _nullKeyGrouping = nullGrouping;
            }

            return nullGrouping;
        }

        private Grouping<TKey, TElement>? FindNullEquivalentGrouping()
        {
            IEqualityComparer<TKey> comparer = _groupings.Comparer;
            Grouping<TKey, TElement>? g = _lastGrouping;
            if (g is not null)
            {
                do
                {
                    g = g._next!;
                    TKey existingKey = g._key;
                    if (existingKey is not null &&
                        (comparer.GetHashCode(existingKey) & 0x7FFFFFFF) == 0 &&
                        comparer.Equals(existingKey, default))
                    {
                        return g;
                    }
                }
                while (g != _lastGrouping);
            }

            return null;
        }

        private Grouping<TKey, TElement> CreateGrouping(TKey key)
        {
            Grouping<TKey, TElement> g = new Grouping<TKey, TElement>(key);
            if (_lastGrouping is null)
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
            if (g is not null)
            {
                do
                {
                    g = g._next;
                    Debug.Assert(g is not null);

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
