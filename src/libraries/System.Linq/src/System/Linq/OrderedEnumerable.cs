// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private abstract partial class OrderedIterator<TElement> : Iterator<TElement>, IOrderedEnumerable<TElement>
        {
            internal readonly IEnumerable<TElement> _source;

            protected OrderedIterator(IEnumerable<TElement> source) => _source = source;

            private protected int[] SortedMap(TElement[] buffer) => GetEnumerableSorter().Sort(buffer, buffer.Length);

            internal int[] SortedMap(TElement[] buffer, int minIdx, int maxIdx) =>
                GetEnumerableSorter().Sort(buffer, buffer.Length, minIdx, maxIdx);

            internal abstract EnumerableSorter<TElement> GetEnumerableSorter(EnumerableSorter<TElement>? next = null);

            internal abstract CachingComparer<TElement> GetComparer(CachingComparer<TElement>? childComparer = null);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            IOrderedEnumerable<TElement> IOrderedEnumerable<TElement>.CreateOrderedEnumerable<TKey>(Func<TElement, TKey> keySelector, IComparer<TKey>? comparer, bool descending) =>
                new OrderedIterator<TElement, TKey>(_source, keySelector, comparer, @descending, this);

            public TElement? TryGetLast(Func<TElement, bool> predicate, out bool found)
            {
                CachingComparer<TElement> comparer = GetComparer();
                using IEnumerator<TElement> e = _source.GetEnumerator();
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

        private sealed partial class OrderedIterator<TElement, TKey> : OrderedIterator<TElement>
        {
            private readonly OrderedIterator<TElement>? _parent;
            private readonly Func<TElement, TKey> _keySelector;
            private readonly IComparer<TKey> _comparer;
            private readonly bool _descending;
            private TElement[]? _buffer;
            private int[]? _map;

            internal OrderedIterator(IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IComparer<TKey>? comparer, bool descending, OrderedIterator<TElement>? parent) :
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

            private protected override Iterator<TElement> Clone() => new OrderedIterator<TElement, TKey>(_source, _keySelector, _comparer, _descending, _parent);

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

                EnumerableSorter<TElement> sorter;

                if (next is null)
                {
                    sorter = new EnumerableSorter<TElement, TKey>(_keySelector, comparer, _descending, next);
                }
                else
                {
                    sorter = next.CreateWithChild(_keySelector, comparer, _descending);
                }

                if (_parent is not null)
                {
                    sorter = _parent.GetEnumerableSorter(sorter);
                }

                return sorter;
            }

            internal override CachingComparer<TElement> GetComparer(CachingComparer<TElement>? childComparer)
            {
                CachingComparer<TElement> cmp = childComparer is null
                    ? new CachingComparer<TElement, TKey>(_keySelector, _comparer, _descending)
                    : new CachingComparerWithChild<TElement, TKey>(_keySelector, _comparer, _descending, childComparer);
                return _parent is not null ? _parent.GetComparer(cmp) : cmp;
            }

            public override bool MoveNext()
            {
                int state = _state;

            Initialized:
                if (state > 1)
                {
                    Debug.Assert(_buffer is not null);
                    Debug.Assert(_map is not null);
                    Debug.Assert(_map.Length == _buffer.Length);

                    int[] map = _map;
                    int i = state - 2;
                    if ((uint)i < (uint)map.Length)
                    {
                        _current = _buffer[map[i]];
                        _state++;
                        return true;
                    }
                }
                else if (state == 1)
                {
                    TElement[] buffer = _source.ToArray();
                    if (buffer.Length != 0)
                    {
                        _map = SortedMap(buffer);
                        _buffer = buffer;
                        _state = state = 2;
                        goto Initialized;
                    }
                }

                Dispose();
                return false;
            }

            public override void Dispose()
            {
                _buffer = null;
                _map = null;
                base.Dispose();
            }
        }

        /// <summary>An ordered enumerable used by Order/OrderDescending for Ts that are bitwise indistinguishable for any considered equal.</summary>
        private sealed partial class ImplicitlyStableOrderedIterator<TElement> : OrderedIterator<TElement>
        {
            private readonly bool _descending;
            private TElement[]? _buffer;

            public ImplicitlyStableOrderedIterator(IEnumerable<TElement> source, bool descending) : base(source)
            {
                Debug.Assert(TypeIsImplicitlyStable<TElement>());

                if (source is null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
                }

                _descending = descending;
            }

            private protected override Iterator<TElement> Clone() => new ImplicitlyStableOrderedIterator<TElement>(_source, _descending);

            internal override CachingComparer<TElement> GetComparer(CachingComparer<TElement>? childComparer) =>
                childComparer is null ?
                    new CachingComparer<TElement, TElement>(EnumerableSorter<TElement>.IdentityFunc, Comparer<TElement>.Default, _descending) :
                    new CachingComparerWithChild<TElement, TElement>(EnumerableSorter<TElement>.IdentityFunc, Comparer<TElement>.Default, _descending, childComparer);

            internal override EnumerableSorter<TElement> GetEnumerableSorter(EnumerableSorter<TElement>? next) =>
                new EnumerableSorter<TElement, TElement>(EnumerableSorter<TElement>.IdentityFunc, Comparer<TElement>.Default, _descending, next);

            public override bool MoveNext()
            {
                int state = _state;
                TElement[]? buffer;

            Initialized:
                if (state > 1)
                {
                    buffer = _buffer;
                    Debug.Assert(buffer is not null);

                    int i = state - 2;
                    if ((uint)i < (uint)buffer.Length)
                    {
                        _current = buffer[i];
                        _state++;
                        return true;
                    }
                }
                else if (state == 1)
                {
                    buffer = _source.ToArray();
                    if (buffer.Length != 0)
                    {
                        Sort(buffer, _descending);
                        _buffer = buffer;
                        _state = state = 2;
                        goto Initialized;
                    }
                }

                Dispose();
                return false;
            }

            public override void Dispose()
            {
                _buffer = null;
                base.Dispose();
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
        private abstract class CachingComparer<TElement>
        {
            internal abstract int Compare(TElement element, bool cacheLower);

            internal abstract void SetElement(TElement element);
        }

        private class CachingComparer<TElement, TKey> : CachingComparer<TElement>
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

        private sealed class CachingComparerWithChild<TElement, TKey> : CachingComparer<TElement, TKey>
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

        private abstract class EnumerableSorter<TElement>
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
                FillIncrementing(map, 0);
                return map;
            }

            internal abstract EnumerableSorter<TElement> CreateWithChild<TChildKey>(Func<TElement, TChildKey> keySelector, IComparer<TChildKey> comparer, bool descending);

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

        private sealed class EnumerableSorter<TElement, TKey> : EnumerableSorter<TElement>
        {
            private readonly Func<TElement, TKey> _keySelector;
            private readonly IComparer<TKey> _comparer;
            private readonly bool _descending;
            private readonly EnumerableSorter<TElement>? _next;
            private TKey[]? _keys;
            private readonly byte _packingUsedSize;

            internal EnumerableSorter(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending, EnumerableSorter<TElement>? next)
            {
                _keySelector = keySelector;
                _comparer = comparer;
                _descending = descending;
                _next = next;
            }

            private EnumerableSorter(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending, EnumerableSorter<TElement>? next, byte packingUsedSize)
            {
                _keySelector = keySelector;
                _comparer = comparer;
                _descending = descending;
                _next = next;
                _packingUsedSize = packingUsedSize;
            }

            internal override EnumerableSorter<TElement> CreateWithChild<TChildKey>(Func<TElement, TChildKey> keySelector, IComparer<TChildKey> comparer, bool descending)
            {
                EnumerableSorter<TElement> sorter;

                if (TryPackKeys(keySelector, comparer, descending, this, out var packedSorter))
                {
                    sorter = packedSorter;
                }
                else
                {
                    sorter = new EnumerableSorter<TElement, TChildKey>(keySelector, comparer, descending, this);
                }

                return sorter;
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
                Debug.Assert(keys is not null);

                int c = _comparer.Compare(keys[index1], keys[index2]);
                if (c == 0)
                {
                    if (_next is null)
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
                Debug.Assert(keys is not null);

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
                Debug.Assert(keys is not null);

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

            private static bool TryPackKeys<TKey1, TKey2>(Func<TElement, TKey1> keySelector, IComparer<TKey1> comparer, bool descending, EnumerableSorter<TElement, TKey2> next, [NotNullWhen(true)] out EnumerableSorter<TElement>? packedSorter)
            {
                bool key1typeIsSigned;
                bool key2typeIsSigned;
                int key1typeSize;
                int key2typeSize;

                if (!TryGetPackingTypeData<TKey1>(out key1typeIsSigned, out key1typeSize) ||
                    !TryGetPackingTypeData<TKey2>(out key2typeIsSigned, out key2typeSize) ||
                    comparer != Comparer<TKey1>.Default || next._comparer != Comparer<TKey2>.Default)
                {
                    packedSorter = null;
                    return false;
                }

                int packingSize = next._packingUsedSize == 0 ? key2typeSize : next._packingUsedSize;

                byte totalSize = (byte)(key1typeSize + packingSize);

                int keyPadding = BitConverter.IsLittleEndian ? packingSize : key1typeSize;

                if (totalSize <= sizeof(uint))
                {
                    uint toggle = 0U;
                    if (key1typeIsSigned)
                    {
                        toggle |= 1U << ((totalSize * 8) - 1);
                    }
                    if (key2typeIsSigned)
                    {
                        toggle |= 1U << ((key2typeSize * 8) - 1);
                    }
                    packedSorter = CreatePacked<uint, TKey1, TKey2>(
                        highKeySelector: keySelector,
                        lowKeySelector: next._keySelector,
                        keyPadding: keyPadding,
                        totalSize: totalSize,
                        key1IsDescending: descending,
                        key2IsDescending: next._descending,
                        toggleSignBits: toggle,
                        next: next?._next
                    );
                    return true;
                }

                if (totalSize <= sizeof(ulong))
                {
                    ulong toggle = 0UL;
                    if (key1typeIsSigned)
                    {
                        toggle |= 1UL << ((totalSize * 8) - 1);
                    }
                    if (key2typeIsSigned)
                    {
                        toggle |= 1UL << ((key2typeSize * 8) - 1);
                    }
                    packedSorter = CreatePacked<ulong, TKey1, TKey2>(
                        highKeySelector: keySelector,
                        lowKeySelector: next._keySelector,
                        keyPadding: keyPadding,
                        totalSize: totalSize,
                        key1IsDescending: descending,
                        key2IsDescending: next._descending,
                        toggleSignBits: toggle,
                        next: next?._next
                    );
                    return true;
                }

                packedSorter = null;
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool TryGetPackingTypeData<T>(out bool isSigned, out int size)
            {
                if (typeof(T) == typeof(sbyte))
                {
                    isSigned = true;
                    size = sizeof(sbyte);
                    return true;
                }
                if (typeof(T) == typeof(short))
                {
                    isSigned = true;
                    size = sizeof(short);
                    return true;
                }
                if (typeof(T) == typeof(int))
                {
                    isSigned = true;
                    size = sizeof(int);
                    return true;
                }
                if (typeof(T) == typeof(nint))
                {
                    isSigned = true;
                    size = Unsafe.SizeOf<nint>();
                    return true;
                }
                if (typeof(T) == typeof(long))
                {
                    isSigned = true;
                    size = sizeof(long);
                    return true;
                }

                if (typeof(T) == typeof(byte))
                {
                    isSigned = false;
                    size = sizeof(byte);
                    return true;
                }
                if (typeof(T) == typeof(ushort))
                {
                    isSigned = false;
                    size = sizeof(ushort);
                    return true;
                }
                if (typeof(T) == typeof(uint))
                {
                    isSigned = false;
                    size = sizeof(uint);
                    return true;
                }
                if (typeof(T) == typeof(nuint))
                {
                    isSigned = false;
                    size = Unsafe.SizeOf<nuint>();
                    return true;
                }
                if (typeof(T) == typeof(ulong))
                {
                    isSigned = false;
                    size = sizeof(ulong);
                    return true;
                }

                if (typeof(T) == typeof(char))
                {
                    isSigned = false;
                    size = sizeof(char);
                    return true;
                }

                if (typeof(T) == typeof(bool))
                {
                    isSigned = false;
                    size = sizeof(bool);
                    return true;
                }

                isSigned = false;
                size = default;

                return false;
            }

            private static EnumerableSorter<TElement, TNewKey> CreatePacked<TNewKey, TKey1, TKey2>(
                Func<TElement, TKey1> highKeySelector,
                Func<TElement, TKey2> lowKeySelector,
                int keyPadding,
                byte totalSize,
                bool key1IsDescending,
                bool key2IsDescending,
                TNewKey toggleSignBits,
                EnumerableSorter<TElement>? next
            )
                where TNewKey : IBitwiseOperators<TNewKey, TNewKey, TNewKey>, IShiftOperators<TNewKey, int, TNewKey>, IUnsignedNumber<TNewKey>
            {
                // see github.com/dotnet/runtime/issues/120785 for more information
                if (key1IsDescending != key2IsDescending)
                {
                    // make the parent order not apply to the child
                    TNewKey toggleLowKeyOrder = (TNewKey.One << keyPadding * 8) - TNewKey.One;
                    toggleSignBits ^= toggleLowKeyOrder;
                }

                if (toggleSignBits == TNewKey.Zero)
                {
                    //result ^= 0 does nothing
                    return new EnumerableSorter<TElement, TNewKey>(x =>
                    {
                        TKey1 highKey = highKeySelector(x);
                        TKey2 lowKey = lowKeySelector(x);

                        TNewKey result = default!;

                        ref byte resultByteRef = ref Unsafe.As<TNewKey, byte>(ref result);

                        if (BitConverter.IsLittleEndian)
                        {
                            Unsafe.WriteUnaligned(ref resultByteRef, lowKey);

                            ref byte dest = ref Unsafe.Add(ref resultByteRef, keyPadding);
                            Unsafe.WriteUnaligned(ref dest, highKey);
                        }
                        else
                        {
                            Unsafe.WriteUnaligned(ref resultByteRef, highKey);

                            ref byte dest = ref Unsafe.Add(ref resultByteRef, keyPadding);
                            Unsafe.WriteUnaligned(ref dest, lowKey);
                        }

                        return result;
                    }, Comparer<TNewKey>.Default, key1IsDescending, next, totalSize);
                }

                return new EnumerableSorter<TElement, TNewKey>(x =>
                {
                    // highKey = 11111111
                    TKey1 highKey = highKeySelector(x);
                    // lowKey = 01111000
                    TKey2 lowKey = lowKeySelector(x);

                    // result = 00000000_00000000
                    TNewKey result = default!;

                    ref byte resultByteRef = ref Unsafe.As<TNewKey, byte>(ref result);

                    if (BitConverter.IsLittleEndian)
                    {
                        // WriteUnaligned will write the bits in the low end of result
                        Unsafe.WriteUnaligned(ref resultByteRef, lowKey);
                        // result = 00000000_01111000
                        //                   |--lo--|

                        // now we want to skip the size of the lowKey writted in the result
                        ref byte dest = ref Unsafe.Add(ref resultByteRef, keyPadding);
                        // |--hi--| |--lo--|
                        // 00000000_01111000
                        //        ^ now we are here

                        // Write the highKey after lowKey
                        Unsafe.WriteUnaligned(ref dest, highKey);
                        // result = 11111111_01111000
                        //          |--hi--| |--lo--|
                        // toggle the sign bit, to safe convert to unsigned
                        // (sbyte)0 in binary 00000000 will be 10000000, now (sbyte)0 is in the middle
                        // basically doing this:
                        //                  Middle
                        // MinValue|----------|----------|MaxValue
                        //                    0
                    }
                    else
                    {
                        Unsafe.WriteUnaligned(ref resultByteRef, highKey);

                        ref byte dest = ref Unsafe.Add(ref resultByteRef, keyPadding);
                        Unsafe.WriteUnaligned(ref dest, lowKey);
                    }

                    result ^= toggleSignBits;

                    return result;
                }, Comparer<TNewKey>.Default, key1IsDescending, next, totalSize);
            }
        }
    }
}
