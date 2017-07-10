// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) =>
            new GroupedEnumerable<TSource, TKey>(source, keySelector, null);

        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer) =>
            new GroupedEnumerable<TSource, TKey>(source, keySelector, comparer);

        public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) =>
            new GroupedEnumerable<TSource, TKey, TElement>(source, keySelector, elementSelector, null);

        public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer) =>
            new GroupedEnumerable<TSource, TKey, TElement>(source, keySelector, elementSelector, comparer);

        public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector) =>
            new GroupedResultEnumerable<TSource, TKey, TResult>(source, keySelector, resultSelector, null);

        public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector) =>
            new GroupedResultEnumerable<TSource, TKey, TElement, TResult>(source, keySelector, elementSelector, resultSelector, null);

        public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector, IEqualityComparer<TKey> comparer) =>
            new GroupedResultEnumerable<TSource, TKey, TResult>(source, keySelector, resultSelector, comparer);

        public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector, IEqualityComparer<TKey> comparer) =>
            new GroupedResultEnumerable<TSource, TKey, TElement, TResult>(source, keySelector, elementSelector, resultSelector, comparer);
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
    [DebuggerDisplay("Key = {Key}")]
    [DebuggerTypeProxy(typeof(SystemLinq_GroupingDebugView<,>))]
    public class Grouping<TKey, TElement> : IGrouping<TKey, TElement>, IList<TElement>
    {
        internal TKey _key;
        internal int _hashCode;
        internal TElement[] _elements;
        internal int _count;
        internal Grouping<TKey, TElement> _hashNext;
        internal Grouping<TKey, TElement> _next;

        internal Grouping()
        {
        }

        internal void Add(TElement element)
        {
            if (_elements.Length == _count)
            {
                Array.Resize(ref _elements, checked(_count * 2));
            }

            _elements[_count] = element;
            _count++;
        }

        internal void Trim()
        {
            if (_elements.Length != _count)
            {
                Array.Resize(ref _elements, _count);
            }
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
            {
                yield return _elements[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // DDB195907: implement IGrouping<>.Key implicitly
        // so that WPF binding works on this property.
        public TKey Key => _key;

        int ICollection<TElement>.Count => _count;

        bool ICollection<TElement>.IsReadOnly => true;

        void ICollection<TElement>.Add(TElement item) => throw Error.NotSupported();

        void ICollection<TElement>.Clear() => throw Error.NotSupported();

        bool ICollection<TElement>.Contains(TElement item) => Array.IndexOf(_elements, item, 0, _count) >= 0;

        void ICollection<TElement>.CopyTo(TElement[] array, int arrayIndex) =>
            Array.Copy(_elements, 0, array, arrayIndex, _count);

        bool ICollection<TElement>.Remove(TElement item) => throw Error.NotSupported();

        int IList<TElement>.IndexOf(TElement item) => Array.IndexOf(_elements, item, 0, _count);

        void IList<TElement>.Insert(int index, TElement item) => throw Error.NotSupported();

        void IList<TElement>.RemoveAt(int index) => throw Error.NotSupported();

        TElement IList<TElement>.this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                {
                    throw Error.ArgumentOutOfRange(nameof(index));
                }

                return _elements[index];
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
            _source = source ?? throw Error.ArgumentNull(nameof(source));
            _keySelector = keySelector ?? throw Error.ArgumentNull(nameof(keySelector));
            _elementSelector = elementSelector ?? throw Error.ArgumentNull(nameof(elementSelector));
            _comparer = comparer;
            _resultSelector = resultSelector ?? throw Error.ArgumentNull(nameof(resultSelector));
        }

        public IEnumerator<TResult> GetEnumerator()
        {
            Lookup<TKey, TElement> lookup = Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer);
            return lookup.ApplyResultSelector(_resultSelector).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public TResult[] ToArray() =>
            Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).ToArray(_resultSelector);

        public List<TResult> ToList() =>
            Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).ToList(_resultSelector);

        public int GetCount(bool onlyIfCheap) =>
            onlyIfCheap ? -1 : Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).Count;
    }

    internal sealed class GroupedResultEnumerable<TSource, TKey, TResult> : IIListProvider<TResult>
    {
        private readonly IEnumerable<TSource> _source;
        private readonly Func<TSource, TKey> _keySelector;
        private readonly IEqualityComparer<TKey> _comparer;
        private readonly Func<TKey, IEnumerable<TSource>, TResult> _resultSelector;

        public GroupedResultEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            _source = source ?? throw Error.ArgumentNull(nameof(source));
            _keySelector = keySelector ?? throw Error.ArgumentNull(nameof(keySelector));
            _resultSelector = resultSelector ?? throw Error.ArgumentNull(nameof(resultSelector));
            _comparer = comparer;
        }

        public IEnumerator<TResult> GetEnumerator()
        {
            Lookup<TKey, TSource> lookup = Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer);
            return lookup.ApplyResultSelector(_resultSelector).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public TResult[] ToArray() =>
            Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).ToArray(_resultSelector);

        public List<TResult> ToList() =>
            Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).ToList(_resultSelector);

        public int GetCount(bool onlyIfCheap) =>
            onlyIfCheap ? -1 : Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).Count;
    }

    internal sealed class GroupedEnumerable<TSource, TKey, TElement> : IIListProvider<IGrouping<TKey, TElement>>
    {
        private readonly IEnumerable<TSource> _source;
        private readonly Func<TSource, TKey> _keySelector;
        private readonly Func<TSource, TElement> _elementSelector;
        private readonly IEqualityComparer<TKey> _comparer;

        public GroupedEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            _source = source ?? throw Error.ArgumentNull(nameof(source));
            _keySelector = keySelector ?? throw Error.ArgumentNull(nameof(keySelector));
            _elementSelector = elementSelector ?? throw Error.ArgumentNull(nameof(elementSelector));
            _comparer = comparer;
        }

        public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator() =>
            Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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

    internal sealed class GroupedEnumerable<TSource, TKey> : IIListProvider<IGrouping<TKey, TSource>>
    {
        private readonly IEnumerable<TSource> _source;
        private readonly Func<TSource, TKey> _keySelector;
        private readonly IEqualityComparer<TKey> _comparer;

        public GroupedEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            _source = source ?? throw Error.ArgumentNull(nameof(source));
            _keySelector = keySelector ?? throw Error.ArgumentNull(nameof(keySelector));
            _comparer = comparer;
        }

        public IEnumerator<IGrouping<TKey, TSource>> GetEnumerator() =>
            Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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
