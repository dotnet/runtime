// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return new GroupedEnumerable<TSource, TKey>(source, keySelector, null);
        }

        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            return new GroupedEnumerable<TSource, TKey>(source, keySelector, comparer);
        }

        public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            return new GroupedEnumerable<TSource, TKey, TElement>(source, keySelector, elementSelector, null);
        }

        public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            return new GroupedEnumerable<TSource, TKey, TElement>(source, keySelector, elementSelector, comparer);
        }

        public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector)
        {
            return new GroupedResultEnumerable<TSource, TKey, TResult>(source, keySelector, resultSelector, null);
        }

        public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
        {
            return new GroupedResultEnumerable<TSource, TKey, TElement, TResult>(source, keySelector, elementSelector, resultSelector, null);
        }

        public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            return new GroupedResultEnumerable<TSource, TKey, TResult>(source, keySelector, resultSelector, comparer);
        }

        public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            return new GroupedResultEnumerable<TSource, TKey, TElement, TResult>(source, keySelector, elementSelector, resultSelector, comparer);
        }
    }

    public interface IGrouping<out TKey, out TElement> : IEnumerable<TElement>
    {
        TKey Key { get; }
    }

    // It is (unfortunately) common to databind directly to Grouping.Key.
    // Because of this, we have to declare this internal type public so that we
    // can mark the Key property for public reflection.
    //
    // To limit the damage, the toolchain makes this type appear in a hidden assembly.
    // (This is also why it is no longer a nested type of Lookup<,>).
    //
    public class Grouping<TKey, TElement> : IGrouping<TKey, TElement>, IList<TElement>
    {
        internal TKey key;
        internal int hashCode;
        internal TElement[] elements;
        internal int count;
        internal Grouping<TKey, TElement> hashNext;
        internal Grouping<TKey, TElement> next;

        internal Grouping()
        {
        }

        internal void Add(TElement element)
        {
            if (elements.Length == count) Array.Resize(ref elements, checked(count * 2));
            elements[count] = element;
            count++;
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            for (int i = 0; i < count; i++) yield return elements[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // DDB195907: implement IGrouping<>.Key implicitly
        // so that WPF binding works on this property.
        public TKey Key
        {
            get { return key; }
        }

        int ICollection<TElement>.Count
        {
            get { return count; }
        }

        bool ICollection<TElement>.IsReadOnly
        {
            get { return true; }
        }

        void ICollection<TElement>.Add(TElement item)
        {
            throw Error.NotSupported();
        }

        void ICollection<TElement>.Clear()
        {
            throw Error.NotSupported();
        }

        bool ICollection<TElement>.Contains(TElement item)
        {
            return Array.IndexOf(elements, item, 0, count) >= 0;
        }

        void ICollection<TElement>.CopyTo(TElement[] array, int arrayIndex)
        {
            Array.Copy(elements, 0, array, arrayIndex, count);
        }

        bool ICollection<TElement>.Remove(TElement item)
        {
            throw Error.NotSupported();
        }

        int IList<TElement>.IndexOf(TElement item)
        {
            return Array.IndexOf(elements, item, 0, count);
        }

        void IList<TElement>.Insert(int index, TElement item)
        {
            throw Error.NotSupported();
        }

        void IList<TElement>.RemoveAt(int index)
        {
            throw Error.NotSupported();
        }

        TElement IList<TElement>.this[int index]
        {
            get
            {
                if (index < 0 || index >= count) throw Error.ArgumentOutOfRange("index");
                return elements[index];
            }
            set
            {
                throw Error.NotSupported();
            }
        }
    }

    internal sealed class GroupedResultEnumerable<TSource, TKey, TElement, TResult> : IIListProvider<TResult>
    {
        private readonly IEnumerable<TSource> _source;
        private readonly Func<TSource, TKey> _keySelector;
        private readonly Func<TSource, TElement> _elementSelector;
        private readonly IEqualityComparer<TKey> _comparer;
        private readonly Func<TKey, IEnumerable<TElement>, TResult> _resultSelector;

        public GroupedResultEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (keySelector == null) throw Error.ArgumentNull("keySelector");
            if (elementSelector == null) throw Error.ArgumentNull("elementSelector");
            if (resultSelector == null) throw Error.ArgumentNull("resultSelector");
            _source = source;
            _keySelector = keySelector;
            _elementSelector = elementSelector;
            _comparer = comparer;
            _resultSelector = resultSelector;
        }

        public IEnumerator<TResult> GetEnumerator()
        {
            Lookup<TKey, TElement> lookup = Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer);
            return lookup.ApplyResultSelector(_resultSelector).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public TResult[] ToArray()
        {
            return Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).ToArray(_resultSelector);
        }

        public List<TResult> ToList()
        {
            return Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).ToList(_resultSelector);
        }

        public int GetCount(bool onlyIfCheap)
        {
            return onlyIfCheap ? -1 : Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).Count;
        }
    }

    internal sealed class GroupedResultEnumerable<TSource, TKey, TResult> : IIListProvider<TResult>
    {
        private readonly IEnumerable<TSource> _source;
        private readonly Func<TSource, TKey> _keySelector;
        private readonly IEqualityComparer<TKey> _comparer;
        private readonly Func<TKey, IEnumerable<TSource>, TResult> _resultSelector;

        public GroupedResultEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (keySelector == null) throw Error.ArgumentNull("keySelector");
            if (resultSelector == null) throw Error.ArgumentNull("resultSelector");
            _source = source;
            _keySelector = keySelector;
            _resultSelector = resultSelector;
            _comparer = comparer;
        }

        public IEnumerator<TResult> GetEnumerator()
        {
            Lookup<TKey, TSource> lookup = Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer);
            return lookup.ApplyResultSelector(_resultSelector).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public TResult[] ToArray()
        {
            return Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).ToArray(_resultSelector);
        }

        public List<TResult> ToList()
        {
            return Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).ToList(_resultSelector);
        }

        public int GetCount(bool onlyIfCheap)
        {
            return onlyIfCheap ? -1 : Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).Count;
        }
    }

    internal sealed class GroupedEnumerable<TSource, TKey, TElement> : IIListProvider<IGrouping<TKey, TElement>>
    {
        private readonly IEnumerable<TSource> _source;
        private readonly Func<TSource, TKey> _keySelector;
        private readonly Func<TSource, TElement> _elementSelector;
        private readonly IEqualityComparer<TKey> _comparer;

        public GroupedEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (keySelector == null) throw Error.ArgumentNull("keySelector");
            if (elementSelector == null) throw Error.ArgumentNull("elementSelector");
            _source = source;
            _keySelector = keySelector;
            _elementSelector = elementSelector;
            _comparer = comparer;
        }

        public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
        {
            return Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

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

        public int GetCount(bool onlyIfCheap)
        {
            return onlyIfCheap ? -1 : Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).Count;
        }
    }

    internal sealed class GroupedEnumerable<TSource, TKey> : IIListProvider<IGrouping<TKey, TSource>>
    {
        private readonly IEnumerable<TSource> _source;
        private readonly Func<TSource, TKey> _keySelector;
        private readonly IEqualityComparer<TKey> _comparer;

        public GroupedEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null) throw Error.ArgumentNull("source");
            if (keySelector == null) throw Error.ArgumentNull("keySelector");
            _source = source;
            _keySelector = keySelector;
            _comparer = comparer;
        }

        public IEnumerator<IGrouping<TKey, TSource>> GetEnumerator()
        {
            return Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

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

        public int GetCount(bool onlyIfCheap)
        {
            return onlyIfCheap ? -1 : Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).Count;
        }
    }
}
