// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    internal abstract partial class OrderedEnumerable<TElement> : IOrderedEnumerable<TElement>
    {
        internal IEnumerable<TElement> _source;

        protected OrderedEnumerable(IEnumerable<TElement> source) => _source = source;

        private int[] SortedMap(TElement[] buffer) => GetEnumerableSorter().Sort(buffer, buffer.Length);

        private int[] SortedMap(TElement[] buffer, int minIdx, int maxIdx) =>
            GetEnumerableSorter().Sort(buffer, buffer.Length, minIdx, maxIdx);

        public virtual IEnumerator<TElement> GetEnumerator()
        {
            TElement[] buffer = _source.ToArray();
            if (buffer.Length > 0)
            {
                int[] map = SortedMap(buffer);
                for (int i = 0; i < buffer.Length; i++)
                {
                    yield return buffer[map[i]];
                }
            }
        }

        internal IEnumerator<TElement> GetEnumerator(int minIdx, int maxIdx)
        {
            TElement[] buffer = _source.ToArray();
            int count = buffer.Length;
            if (count > minIdx)
            {
                if (count <= maxIdx)
                {
                    maxIdx = count - 1;
                }

                if (minIdx == maxIdx)
                {
                    yield return GetEnumerableSorter().ElementAt(buffer, count, minIdx);
                }
                else
                {
                    int[] map = SortedMap(buffer, minIdx, maxIdx);
                    while (minIdx <= maxIdx)
                    {
                        yield return buffer[map[minIdx]];
                        ++minIdx;
                    }
                }
            }
        }

        private EnumerableSorter<TElement> GetEnumerableSorter() => GetEnumerableSorter(null);

        internal abstract EnumerableSorter<TElement> GetEnumerableSorter(EnumerableSorter<TElement>? next);

        internal abstract CachingComparer<TElement> GetComparer(CachingComparer<TElement>? childComparer = null);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IOrderedEnumerable<TElement> IOrderedEnumerable<TElement>.CreateOrderedEnumerable<TKey>(Func<TElement, TKey> keySelector, IComparer<TKey>? comparer, bool descending) =>
            new OrderedEnumerable<TElement, TKey>(_source, keySelector, comparer, @descending, this);

        public TElement? TryGetLast(Func<TElement, bool> predicate, out bool found)
        {
            CachingComparer<TElement> comparer = GetComparer();
            using (IEnumerator<TElement> e = _source.GetEnumerator())
            {
                TElement value;
                do
                {
                    if (!e.MoveNext())
                    {
                        found = false;
                        return default;
                    }

                    value = e.Current;
                }
                while (!predicate(value));

                comparer.SetElement(value);
                while (e.MoveNext())
                {
                    TElement x = e.Current;
                    if (predicate(x) && comparer.Compare(x, false) >= 0)
                    {
                        value = x;
                    }
                }

                found = true;
                return value;
            }
        }
    }

    internal sealed partial class OrderedEnumerable<TElement, TKey> : OrderedEnumerable<TElement>
    {
        private readonly OrderedEnumerable<TElement>? _parent;
        private readonly Func<TElement, TKey> _keySelector;
        private readonly IComparer<TKey> _comparer;
        private readonly bool _descending;

        internal OrderedEnumerable(IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IComparer<TKey>? comparer, bool descending, OrderedEnumerable<TElement>? parent) :
            base(source)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }
            if (keySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            _parent = parent;
            _keySelector = keySelector;
            _comparer = comparer ?? Comparer<TKey>.Default;
            _descending = descending;
        }

        internal override EnumerableSorter<TElement> GetEnumerableSorter(EnumerableSorter<TElement>? next)
        {
            // Special case the common use of string with default comparer. Comparer<string>.Default checks the
            // thread's Culture on each call which is an overhead which is not required, because we are about to
            // do a sort which remains on the current thread (and EnumerableSorter is not used afterwards).
            IComparer<TKey> comparer = _comparer;
            if (typeof(TKey) == typeof(string) && comparer == Comparer<string>.Default)
            {
                comparer = (IComparer<TKey>)StringComparer.CurrentCulture;
            }

            EnumerableSorter<TElement> sorter = new EnumerableSorter<TElement, TKey>(_keySelector, comparer, _descending, next);
            if (_parent != null)
            {
                sorter = _parent.GetEnumerableSorter(sorter);
            }

            return sorter;
        }

        internal override CachingComparer<TElement> GetComparer(CachingComparer<TElement>? childComparer)
        {
            CachingComparer<TElement> cmp = childComparer == null
                ? new CachingComparer<TElement, TKey>(_keySelector, _comparer, _descending)
                : new CachingComparerWithChild<TElement, TKey>(_keySelector, _comparer, _descending, childComparer);
            return _parent != null ? _parent.GetComparer(cmp) : cmp;
        }
    }

    /// <summary>An ordered enumerable used by Order/OrderDescending for Ts that are bitwise indistinguishable for any considered equal.</summary>
    internal sealed partial class OrderedImplicitlyStableEnumerable<TElement> : OrderedEnumerable<TElement>
    {
        private readonly bool _descending;

        public OrderedImplicitlyStableEnumerable(IEnumerable<TElement> source, bool descending) : base(source)
        {
            Debug.Assert(Enumerable.TypeIsImplicitlyStable<TElement>());

            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            _descending = descending;
        }

        internal override CachingComparer<TElement> GetComparer(CachingComparer<TElement>? childComparer) =>
            childComparer == null ?
                new CachingComparer<TElement, TElement>(EnumerableSorter<TElement>.IdentityFunc, Comparer<TElement>.Default, _descending) :
                new CachingComparerWithChild<TElement, TElement>(EnumerableSorter<TElement>.IdentityFunc, Comparer<TElement>.Default, _descending, childComparer);

        internal override EnumerableSorter<TElement> GetEnumerableSorter(EnumerableSorter<TElement>? next) =>
            new EnumerableSorter<TElement, TElement>(EnumerableSorter<TElement>.IdentityFunc, Comparer<TElement>.Default, _descending, next);

        public override IEnumerator<TElement> GetEnumerator()
        {
            TElement[] buffer = _source.ToArray();
            if (buffer.Length > 0)
            {
                Sort(buffer, _descending);
                for (int i = 0; i < buffer.Length; i++)
                {
                    yield return buffer[i];
                }
            }
        }

        private static void Sort(Span<TElement> span, bool descending)
        {
            if (descending)
            {
                span.Sort(static (a, b) => Comparer<TElement>.Default.Compare(b, a));
            }
            else
            {
                span.Sort();
            }
        }
    }

    // A comparer that chains comparisons, and pushes through the last element found to be
    // lower or higher (depending on use), so as to represent the sort of comparisons
    // done by OrderBy().ThenBy() combinations.
    internal abstract class CachingComparer<TElement>
    {
        internal abstract int Compare(TElement element, bool cacheLower);

        internal abstract void SetElement(TElement element);
    }

    internal class CachingComparer<TElement, TKey> : CachingComparer<TElement>
    {
        protected readonly Func<TElement, TKey> _keySelector;
        protected readonly IComparer<TKey> _comparer;
        protected readonly bool _descending;
        protected TKey? _lastKey;

        public CachingComparer(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending)
        {
            _keySelector = keySelector;
            _comparer = comparer;
            _descending = descending;
        }

        internal override int Compare(TElement element, bool cacheLower)
        {
            TKey newKey = _keySelector(element);
            int cmp = _descending ? _comparer.Compare(_lastKey, newKey) : _comparer.Compare(newKey, _lastKey);
            if (cacheLower == cmp < 0)
            {
                _lastKey = newKey;
            }

            return cmp;
        }

        internal override void SetElement(TElement element)
        {
            _lastKey = _keySelector(element);
        }
    }

    internal sealed class CachingComparerWithChild<TElement, TKey> : CachingComparer<TElement, TKey>
    {
        private readonly CachingComparer<TElement> _child;

        public CachingComparerWithChild(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending, CachingComparer<TElement> child)
            : base(keySelector, comparer, descending)
        {
            _child = child;
        }

        internal override int Compare(TElement element, bool cacheLower)
        {
            TKey newKey = _keySelector(element);
            int cmp = _descending ? _comparer.Compare(_lastKey, newKey) : _comparer.Compare(newKey, _lastKey);
            if (cmp == 0)
            {
                return _child.Compare(element, cacheLower);
            }

            if (cacheLower == cmp < 0)
            {
                _lastKey = newKey;
                _child.SetElement(element);
            }

            return cmp;
        }

        internal override void SetElement(TElement element)
        {
            base.SetElement(element);
            _child.SetElement(element);
        }
    }

    internal abstract class EnumerableSorter<TElement>
    {
        /// <summary>Function that returns its input unmodified.</summary>
        /// <remarks>
        /// Used for reference equality in order to avoid unnecessary computation when a caller
        /// can benefit from knowing that the produced value is identical to the input.
        /// </remarks>
        internal static readonly Func<TElement, TElement> IdentityFunc = e => e;

        internal abstract void ComputeKeys(TElement[] elements, int count);

        internal abstract int CompareAnyKeys(int index1, int index2);

        private int[] ComputeMap(TElement[] elements, int count)
        {
            ComputeKeys(elements, count);
            int[] map = new int[count];
            for (int i = 0; i < map.Length; i++)
            {
                map[i] = i;
            }

            return map;
        }

        internal int[] Sort(TElement[] elements, int count)
        {
            int[] map = ComputeMap(elements, count);
            QuickSort(map, 0, count - 1);
            return map;
        }

        internal int[] Sort(TElement[] elements, int count, int minIdx, int maxIdx)
        {
            int[] map = ComputeMap(elements, count);
            PartialQuickSort(map, 0, count - 1, minIdx, maxIdx);
            return map;
        }

        internal TElement ElementAt(TElement[] elements, int count, int idx)
        {
            int[] map = ComputeMap(elements, count);
            return idx == 0 ?
                elements[Min(map, count)] :
                elements[QuickSelect(map, count - 1, idx)];
        }

        protected abstract void QuickSort(int[] map, int left, int right);

        // Sorts the k elements between minIdx and maxIdx without sorting all elements
        // Time complexity: O(n + k log k) best and average case. O(n^2) worse case.
        protected abstract void PartialQuickSort(int[] map, int left, int right, int minIdx, int maxIdx);

        // Finds the element that would be at idx if the collection was sorted.
        // Time complexity: O(n) best and average case. O(n^2) worse case.
        protected abstract int QuickSelect(int[] map, int right, int idx);

        protected abstract int Min(int[] map, int count);
    }

    internal sealed class EnumerableSorter<TElement, TKey> : EnumerableSorter<TElement>
    {
        private readonly Func<TElement, TKey> _keySelector;
        private readonly IComparer<TKey> _comparer;
        private readonly bool _descending;
        private readonly EnumerableSorter<TElement>? _next;
        private TKey[]? _keys;

        internal EnumerableSorter(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending, EnumerableSorter<TElement>? next)
        {
            _keySelector = keySelector;
            _comparer = comparer;
            _descending = descending;
            _next = next;
        }

        internal override void ComputeKeys(TElement[] elements, int count)
        {
            Func<TElement, TKey> keySelector = _keySelector;
            if (!ReferenceEquals(keySelector, IdentityFunc))
            {
                var keys = new TKey[count];
                for (int i = 0; i < keys.Length; i++)
                {
                    keys[i] = keySelector(elements[i]);
                }
                _keys = keys;
            }
            else
            {
                // The key selector is our known identity function, which means we don't
                // need to invoke the key selector for every element.  Further, we can just
                // use the original array as the keys (even if count is smaller, as the additional
                // values will just be ignored).
                Debug.Assert(typeof(TKey) == typeof(TElement));
                _keys = (TKey[])(object)elements;
            }

            _next?.ComputeKeys(elements, count);
        }

        internal override int CompareAnyKeys(int index1, int index2)
        {
            TKey[]? keys = _keys;
            Debug.Assert(keys != null);

            int c = _comparer.Compare(keys[index1], keys[index2]);
            if (c == 0)
            {
                if (_next == null)
                {
                    return index1 - index2; // ensure stability of sort
                }

                return _next.CompareAnyKeys(index1, index2);
            }

            // -c will result in a negative value for int.MinValue (-int.MinValue == int.MinValue).
            // Flipping keys earlier is more likely to trigger something strange in a comparer,
            // particularly as it comes to the sort being stable.
            return (_descending != (c > 0)) ? 1 : -1;
        }

        private int CompareAnyKeys_DefaultComparer_NoNext_Ascending(int index1, int index2)
        {
            Debug.Assert(typeof(TKey).IsValueType);
            Debug.Assert(_comparer == Comparer<TKey>.Default);
            Debug.Assert(_next is null);
            Debug.Assert(!_descending);

            TKey[]? keys = _keys;
            Debug.Assert(keys != null);

            int c = Comparer<TKey>.Default.Compare(keys[index1], keys[index2]);
            return
                c == 0 ? index1 - index2 : // ensure stability of sort
                c;
        }

        private int CompareAnyKeys_DefaultComparer_NoNext_Descending(int index1, int index2)
        {
            Debug.Assert(typeof(TKey).IsValueType);
            Debug.Assert(_comparer == Comparer<TKey>.Default);
            Debug.Assert(_next is null);
            Debug.Assert(_descending);

            TKey[]? keys = _keys;
            Debug.Assert(keys != null);

            int c = Comparer<TKey>.Default.Compare(keys[index2], keys[index1]);
            return
                c == 0 ? index1 - index2 : // ensure stability of sort
                c;
        }

        private int CompareKeys(int index1, int index2) => index1 == index2 ? 0 : CompareAnyKeys(index1, index2);

        protected override void QuickSort(int[] keys, int lo, int hi)
        {
            Comparison<int> comparison;

            if (typeof(TKey).IsValueType && _next is null && _comparer == Comparer<TKey>.Default)
            {
                // We can use Comparer<TKey>.Default.Compare and benefit from devirtualization and inlining.
                // We can also avoid extra steps to check whether we need to deal with a subsequent tie breaker (_next).
                if (!_descending)
                {
                    comparison = CompareAnyKeys_DefaultComparer_NoNext_Ascending;
                }
                else
                {
                    comparison = CompareAnyKeys_DefaultComparer_NoNext_Descending;
                }
            }
            else
            {
                comparison = CompareAnyKeys;
            }

            new Span<int>(keys, lo, hi - lo + 1).Sort(comparison);
        }

        // Sorts the k elements between minIdx and maxIdx without sorting all elements
        // Time complexity: O(n + k log k) best and average case. O(n^2) worse case.
        protected override void PartialQuickSort(int[] map, int left, int right, int minIdx, int maxIdx)
        {
            do
            {
                int i = left;
                int j = right;
                int x = map[i + ((j - i) >> 1)];
                do
                {
                    while (i < map.Length && CompareKeys(x, map[i]) > 0)
                    {
                        i++;
                    }

                    while (j >= 0 && CompareKeys(x, map[j]) < 0)
                    {
                        j--;
                    }

                    if (i > j)
                    {
                        break;
                    }

                    if (i < j)
                    {
                        int temp = map[i];
                        map[i] = map[j];
                        map[j] = temp;
                    }

                    i++;
                    j--;
                }
                while (i <= j);

                if (minIdx >= i)
                {
                    left = i + 1;
                }
                else if (maxIdx <= j)
                {
                    right = j - 1;
                }

                if (j - left <= right - i)
                {
                    if (left < j)
                    {
                        PartialQuickSort(map, left, j, minIdx, maxIdx);
                    }

                    left = i;
                }
                else
                {
                    if (i < right)
                    {
                        PartialQuickSort(map, i, right, minIdx, maxIdx);
                    }

                    right = j;
                }
            }
            while (left < right);
        }

        // Finds the element that would be at idx if the collection was sorted.
        // Time complexity: O(n) best and average case. O(n^2) worse case.
        protected override int QuickSelect(int[] map, int right, int idx)
        {
            int left = 0;
            do
            {
                int i = left;
                int j = right;
                int x = map[i + ((j - i) >> 1)];
                do
                {
                    while (i < map.Length && CompareKeys(x, map[i]) > 0)
                    {
                        i++;
                    }

                    while (j >= 0 && CompareKeys(x, map[j]) < 0)
                    {
                        j--;
                    }

                    if (i > j)
                    {
                        break;
                    }

                    if (i < j)
                    {
                        int temp = map[i];
                        map[i] = map[j];
                        map[j] = temp;
                    }

                    i++;
                    j--;
                }
                while (i <= j);

                if (i <= idx)
                {
                    left = i + 1;
                }
                else
                {
                    right = j - 1;
                }

                if (j - left <= right - i)
                {
                    if (left < j)
                    {
                        right = j;
                    }

                    left = i;
                }
                else
                {
                    if (i < right)
                    {
                        left = i;
                    }

                    right = j;
                }
            }
            while (left < right);

            return map[idx];
        }

        protected override int Min(int[] map, int count)
        {
            int index = 0;
            for (int i = 1; i < count; i++)
            {
                if (CompareKeys(map[i], map[index]) < 0)
                {
                    index = i;
                }
            }
            return map[index];
        }
    }
}
