// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Linq
{
    internal abstract class OrderedEnumerable<TElement> : IOrderedEnumerable<TElement>, IPartition<TElement>
    {
        internal IEnumerable<TElement> source;

        private int[] SortedMap(Buffer<TElement> buffer)
        {
            return GetEnumerableSorter().Sort(buffer.items, buffer.count);
        }

        private int[] SortedMap(Buffer<TElement> buffer, int minIdx, int maxIdx)
        {
            return GetEnumerableSorter().Sort(buffer.items, buffer.count, minIdx, maxIdx);
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            Buffer<TElement> buffer = new Buffer<TElement>(source);
            if (buffer.count > 0)
            {
                int[] map = SortedMap(buffer);
                for (int i = 0; i < buffer.count; i++) yield return buffer.items[map[i]];
            }
        }

        public TElement[] ToArray()
        {
            Buffer<TElement> buffer = new Buffer<TElement>(source);

            int count = buffer.count;
            if (count == 0)
            {
                return buffer.items;
            }

            TElement[] array = new TElement[count];
            int[] map = SortedMap(buffer);
            for (int i = 0; i != array.Length; i++) array[i] = buffer.items[map[i]];

            return array;
        }

        public List<TElement> ToList()
        {
            Buffer<TElement> buffer = new Buffer<TElement>(source);
            int count = buffer.count;
            List<TElement> list = new List<TElement>(count);
            if (count > 0)
            {
                int[] map = SortedMap(buffer);
                for (int i = 0; i != count; i++) list.Add(buffer.items[map[i]]);
            }

            return list;
        }

        public int GetCount(bool onlyIfCheap)
        {
            IIListProvider<TElement> listProv = source as IIListProvider<TElement>;
            if (listProv != null) return listProv.GetCount(onlyIfCheap);
            return !onlyIfCheap || source is ICollection<TElement> || source is ICollection ? source.Count() : -1;
        }

        internal IEnumerator<TElement> GetEnumerator(int minIdx, int maxIdx)
        {
            Buffer<TElement> buffer = new Buffer<TElement>(source);
            int count = buffer.count;
            if (count > minIdx)
            {
                if (count <= maxIdx) maxIdx = count - 1;
                if (minIdx == maxIdx) yield return GetEnumerableSorter().ElementAt(buffer.items, count, minIdx);
                else
                {
                    int[] map = SortedMap(buffer, minIdx, maxIdx);
                    while (minIdx <= maxIdx)
                    {
                        yield return buffer.items[map[minIdx]];
                        ++minIdx;
                    }
                }
            }
        }

        internal TElement[] ToArray(int minIdx, int maxIdx)
        {
            Buffer<TElement> buffer = new Buffer<TElement>(source);
            int count = buffer.count;
            if (count <= minIdx) return Array.Empty<TElement>();
            if (count <= maxIdx) maxIdx = count - 1;
            if (minIdx == maxIdx) return new TElement[] { GetEnumerableSorter().ElementAt(buffer.items, count, minIdx) };
            int[] map = SortedMap(buffer, minIdx, maxIdx);
            TElement[] array = new TElement[maxIdx - minIdx + 1];
            int idx = 0;
            while (minIdx <= maxIdx)
            {
                array[idx] = buffer.items[map[minIdx]];
                ++idx;
                ++minIdx;
            }
            return array;
        }

        internal List<TElement> ToList(int minIdx, int maxIdx)
        {
            Buffer<TElement> buffer = new Buffer<TElement>(source);
            int count = buffer.count;
            if (count <= minIdx) return new List<TElement>();
            if (count <= maxIdx) maxIdx = count - 1;
            if (minIdx == maxIdx) return new List<TElement>(1) { GetEnumerableSorter().ElementAt(buffer.items, count, minIdx) };
            int[] map = SortedMap(buffer, minIdx, maxIdx);
            List<TElement> list = new List<TElement>(maxIdx - minIdx + 1);
            while (minIdx <= maxIdx)
            {
                list.Add(buffer.items[map[minIdx]]);
                ++minIdx;
            }
            return list;
        }

        internal int GetCount(int minIdx, int maxIdx, bool onlyIfCheap)
        {
            int count = GetCount(onlyIfCheap);
            if (count <= 0) return count;
            if (count <= minIdx) return 0;
            return (count <= maxIdx? count : maxIdx + 1) - minIdx;
        }

        private EnumerableSorter<TElement> GetEnumerableSorter()
        {
            return GetEnumerableSorter(null);
        }

        internal abstract EnumerableSorter<TElement> GetEnumerableSorter(EnumerableSorter<TElement> next);

        internal CachingComparer<TElement> GetComparer()
        {
            return GetComparer(null);
        }

        internal abstract CachingComparer<TElement> GetComparer(CachingComparer<TElement> childComparer);

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IOrderedEnumerable<TElement> IOrderedEnumerable<TElement>.CreateOrderedEnumerable<TKey>(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending)
        {
            OrderedEnumerable<TElement, TKey> result = new OrderedEnumerable<TElement, TKey>(source, keySelector, comparer, descending);
            result.parent = this;
            return result;
        }

        public IPartition<TElement> Skip(int count)
        {
            return new OrderedPartition<TElement>(this, count, int.MaxValue);
        }

        public IPartition<TElement> Take(int count)
        {
            return new OrderedPartition<TElement>(this, 0, count - 1);
        }

        public bool TryGetElementAt(int index, out TElement result)
        {
            if (index == 0) return TryGetFirst(out result);
            if (index > 0)
            {
                Buffer<TElement> buffer = new Buffer<TElement>(source);
                int count = buffer.count;
                if (index < count)
                {
                    result = GetEnumerableSorter().ElementAt(buffer.items, count, index);
                    return true;
                }
            }
            result = default(TElement);
            return false;
        }

        public TElement ElementAt(int index)
        {
            TElement result;
            if (!TryGetElementAt(index, out result)) throw Error.ArgumentOutOfRange("index");
            return result;
        }

        public TElement ElementAtOrDefault(int index)
        {
            TElement result;
            TryGetElementAt(index, out result);
            return result;
        }

        private bool TryGetFirst(out TElement result)
        {
            CachingComparer<TElement> comparer = GetComparer();
            using (IEnumerator<TElement> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    result = default(TElement);
                    return false;
                }
                TElement value = e.Current;
                comparer.SetElement(value);
                while (e.MoveNext())
                {
                    TElement x = e.Current;
                    if (comparer.Compare(x, true) < 0) value = x;
                }
                result = value;
                return true;
            }
        }

        public TElement FirstOrDefault()
        {
            TElement result;
            TryGetFirst(out result);
            return result;
        }

        public TElement First()
        {
            TElement result;
            if (!TryGetFirst(out result)) throw Error.NoElements();
            return result;
        }

        public TElement First(Func<TElement, bool> predicate)
        {
            CachingComparer<TElement> comparer = GetComparer();
            using (IEnumerator<TElement> e = source.GetEnumerator())
            {
                TElement value;
                do
                {
                    if (!e.MoveNext()) throw Error.NoMatch();
                    value = e.Current;
                } while (!predicate(value));
                comparer.SetElement(value);
                while (e.MoveNext())
                {
                    TElement x = e.Current;
                    if (predicate(x) && comparer.Compare(x, true) < 0) value = x;
                }
                return value;
            }
        }

        public TElement FirstOrDefault(Func<TElement, bool> predicate)
        {
            CachingComparer<TElement> comparer = GetComparer();
            using (IEnumerator<TElement> e = source.GetEnumerator())
            {
                TElement value;
                do
                {
                    if (!e.MoveNext()) return default(TElement);
                    value = e.Current;
                } while (!predicate(value));
                comparer.SetElement(value);
                while (e.MoveNext())
                {
                    TElement x = e.Current;
                    if (predicate(x) && comparer.Compare(x, true) < 0) value = x;
                }
                return value;
            }
        }

        public TElement Last()
        {
            CachingComparer<TElement> comparer = GetComparer();
            using (IEnumerator<TElement> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) throw Error.NoElements();
                TElement value = e.Current;
                comparer.SetElement(value);
                while (e.MoveNext())
                {
                    TElement x = e.Current;
                    if (comparer.Compare(x, false) >= 0) value = x;
                }
                return value;
            }
        }

        public TElement LastOrDefault()
        {
            CachingComparer<TElement> comparer = GetComparer();
            using (IEnumerator<TElement> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) return default(TElement);
                TElement value = e.Current;
                comparer.SetElement(value);
                while (e.MoveNext())
                {
                    TElement x = e.Current;
                    if (comparer.Compare(x, false) >= 0) value = x;
                }
                return value;
            }
        }

        public TElement Last(int minIdx, int maxIdx)
        {
            Buffer<TElement> buffer = new Buffer<TElement>(source);
            int count = buffer.count;
            if (minIdx >= count) throw Error.NoElements();
            if (maxIdx < count - 1) return GetEnumerableSorter().ElementAt(buffer.items, count, maxIdx);
            // If we're here, we want the same results we would have got from
            // Last(), but we've already buffered our source.
            return Last(buffer);
        }

        public TElement LastOrDefault(int minIdx, int maxIdx)
        {
            Buffer<TElement> buffer = new Buffer<TElement>(source);
            int count = buffer.count;
            if (minIdx >= count) return default(TElement);
            if (maxIdx < count - 1) return GetEnumerableSorter().ElementAt(buffer.items, count, maxIdx);
            return Last(buffer);
        }

        private TElement Last(Buffer<TElement> buffer)
        {
            CachingComparer<TElement> comparer = GetComparer();
            TElement[] items = buffer.items;
            int count = buffer.count;
            TElement value = items[0];
            comparer.SetElement(value);
            for (int i = 1; i != count; ++i)
            {
                TElement x = items[i];
                if (comparer.Compare(x, false) >= 0) value = x;
            }
            return value;
        }

        public TElement Last(Func<TElement, bool> predicate)
        {
            CachingComparer<TElement> comparer = GetComparer();
            using (IEnumerator<TElement> e = source.GetEnumerator())
            {
                TElement value;
                do
                {
                    if (!e.MoveNext()) throw Error.NoMatch();
                    value = e.Current;
                } while (!predicate(value));
                comparer.SetElement(value);
                while (e.MoveNext())
                {
                    TElement x = e.Current;
                    if (predicate(x) && comparer.Compare(x, false) >= 0) value = x;
                }
                return value;
            }
        }

        public TElement LastOrDefault(Func<TElement, bool> predicate)
        {
            CachingComparer<TElement> comparer = GetComparer();
            using (IEnumerator<TElement> e = source.GetEnumerator())
            {
                TElement value;
                do
                {
                    if (!e.MoveNext()) return default(TElement);
                    value = e.Current;
                } while (!predicate(value));
                comparer.SetElement(value);
                while (e.MoveNext())
                {
                    TElement x = e.Current;
                    if (predicate(x) && comparer.Compare(x, false) > 0) value = x;
                }
                return value;
            }
        }
    }

    internal sealed class OrderedEnumerable<TElement, TKey> : OrderedEnumerable<TElement>
    {
        internal OrderedEnumerable<TElement> parent;
        internal Func<TElement, TKey> keySelector;
        internal IComparer<TKey> comparer;
        internal bool descending;

        internal OrderedEnumerable(IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (keySelector == null) throw Error.ArgumentNull("keySelector");
            this.source = source;
            this.parent = null;
            this.keySelector = keySelector;
            this.comparer = comparer != null ? comparer : Comparer<TKey>.Default;
            this.descending = descending;
        }

        internal override EnumerableSorter<TElement> GetEnumerableSorter(EnumerableSorter<TElement> next)
        {
            EnumerableSorter<TElement> sorter = new EnumerableSorter<TElement, TKey>(keySelector, comparer, descending, next);
            if (parent != null) sorter = parent.GetEnumerableSorter(sorter);
            return sorter;
        }

        internal override CachingComparer<TElement> GetComparer(CachingComparer<TElement> childComparer)
        {
            CachingComparer<TElement> cmp = childComparer == null
                ? new CachingComparer<TElement, TKey>(keySelector, comparer, descending)
                : new CachingComparerWithChild<TElement, TKey>(keySelector, comparer, descending, childComparer);
            return parent != null ? parent.GetComparer(cmp) : cmp;
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
        protected readonly Func<TElement, TKey> KeySelector;
        protected readonly IComparer<TKey> Comparer;
        protected readonly bool Descending;
        protected TKey LastKey;
        public CachingComparer(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending)
        {
            KeySelector = keySelector;
            Comparer = comparer;
            Descending = descending;
        }
        internal override int Compare(TElement element, bool cacheLower)
        {
            TKey newKey = KeySelector(element);
            int cmp = Descending ? Comparer.Compare(LastKey, newKey) : Comparer.Compare(newKey, LastKey);
            if (cacheLower == cmp < 0) LastKey = newKey;
            return cmp;
        }
        internal override void SetElement(TElement element)
        {
            LastKey = KeySelector(element);
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
            TKey newKey = KeySelector(element);
            int cmp = Descending ? Comparer.Compare(LastKey, newKey) : Comparer.Compare(newKey, LastKey);
            if (cmp == 0) return _child.Compare(element, cacheLower);
            if (cacheLower == cmp < 0)
            {
                LastKey = newKey;
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
        internal abstract void ComputeKeys(TElement[] elements, int count);

        internal abstract int CompareAnyKeys(int index1, int index2);

        private int[] ComputeMap(TElement[] elements, int count)
        {
            ComputeKeys(elements, count);
            int[] map = new int[count];
            for (int i = 0; i < count; i++) map[i] = i;
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
            return elements[QuickSelect(ComputeMap(elements, count), count - 1, idx)];
        }

        private int CompareKeys(int index1, int index2)
        {
            return index1 == index2 ? 0 : CompareAnyKeys(index1, index2);
        }

        private void QuickSort(int[] map, int left, int right)
        {
            do
            {
                int i = left;
                int j = right;
                int x = map[i + ((j - i) >> 1)];
                do
                {
                    while (i < map.Length && CompareKeys(x, map[i]) > 0) i++;
                    while (j >= 0 && CompareKeys(x, map[j]) < 0) j--;
                    if (i > j) break;
                    if (i < j)
                    {
                        int temp = map[i];
                        map[i] = map[j];
                        map[j] = temp;
                    }
                    i++;
                    j--;
                } while (i <= j);
                if (j - left <= right - i)
                {
                    if (left < j) QuickSort(map, left, j);
                    left = i;
                }
                else
                {
                    if (i < right) QuickSort(map, i, right);
                    right = j;
                }
            } while (left < right);
        }

        // Sorts the k elements between minIdx and maxIdx without sorting all elements
        // Time complexity: O(n + k log k) best and average case. O(n^2) worse case.  
        private void PartialQuickSort(int[] map, int left, int right, int minIdx, int maxIdx)
        {
            do
            {
                int i = left;
                int j = right;
                int x = map[i + ((j - i) >> 1)];
                do
                {
                    while (i < map.Length && CompareKeys(x, map[i]) > 0) i++;
                    while (j >= 0 && CompareKeys(x, map[j]) < 0) j--;
                    if (i > j) break;
                    if (i < j)
                    {
                        int temp = map[i];
                        map[i] = map[j];
                        map[j] = temp;
                    }
                    i++;
                    j--;
                } while (i <= j);
                if (minIdx >= i) left = i + 1;
                else if (maxIdx <= j) right = j - 1;
                if (j - left <= right - i)
                {
                    if (left < j) PartialQuickSort(map, left, j, minIdx, maxIdx);
                    left = i;
                }
                else
                {
                    if (i < right) PartialQuickSort(map, i, right, minIdx, maxIdx);
                    right = j;
                }
            } while (left < right);
        }

        // Finds the element that would be at idx if the collection was sorted.
        // Time complexity: O(n) best and average case. O(n^2) worse case.
        private int QuickSelect(int[] map, int right, int idx)
        {
            int left = 0;
            do
            {
                int i = left;
                int j = right;
                int x = map[i + ((j - i) >> 1)];
                do
                {
                    while (i < map.Length && CompareKeys(x, map[i]) > 0) i++;
                    while (j >= 0 && CompareKeys(x, map[j]) < 0) j--;
                    if (i > j) break;
                    if (i < j)
                    {
                        int temp = map[i];
                        map[i] = map[j];
                        map[j] = temp;
                    }
                    i++;
                    j--;
                } while (i <= j);
                if (i <= idx) left = i + 1;
                else right = j - 1;
                if (j - left <= right - i)
                {
                    if (left < j) right = j;
                    left = i;
                }
                else
                {
                    if (i < right) left = i;
                    right = j;
                }
            } while (left < right);
            return map[idx];
        }
    }

    internal sealed class EnumerableSorter<TElement, TKey> : EnumerableSorter<TElement>
    {
        internal Func<TElement, TKey> keySelector;
        internal IComparer<TKey> comparer;
        internal bool descending;
        internal EnumerableSorter<TElement> next;
        internal TKey[] keys;

        internal EnumerableSorter(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending, EnumerableSorter<TElement> next)
        {
            this.keySelector = keySelector;
            this.comparer = comparer;
            this.descending = descending;
            this.next = next;
        }

        internal override void ComputeKeys(TElement[] elements, int count)
        {
            keys = new TKey[count];
            for (int i = 0; i < count; i++) keys[i] = keySelector(elements[i]);
            if (next != null) next.ComputeKeys(elements, count);
        }

        internal override int CompareAnyKeys(int index1, int index2)
        {
            int c = comparer.Compare(keys[index1], keys[index2]);
            if (c == 0)
            {
                if (next == null) return index1 - index2;
                return next.CompareAnyKeys(index1, index2);
            }
            // -c will result in a negative value for int.MinValue (-int.MinValue == int.MinValue).
            // Flipping keys earlier is more likely to trigger something strange in a comparer,
            // particularly as it comes to the sort being stable.
            return (descending != (c > 0)) ? 1 : -1;
        }
    }
}
