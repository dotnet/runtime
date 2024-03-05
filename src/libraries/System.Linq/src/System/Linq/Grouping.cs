// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) =>
            GroupBy(source, keySelector, comparer: null);

        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
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
                return [];
            }

            return new GroupByIterator<TSource, TKey>(source, keySelector, comparer);
        }

        public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector) =>
            GroupBy(source, keySelector, elementSelector, comparer: null);

        public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer)
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
                return [];
            }

            return new GroupByIterator<TSource, TKey, TElement>(source, keySelector, elementSelector, comparer);
        }

        public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector) =>
            GroupBy(source, keySelector, resultSelector, comparer: null);

        public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (keySelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
            }

            if (resultSelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
            }

            if (IsEmptyArray(source))
            {
                return [];
            }

            return new GroupByResultIterator<TSource, TKey, TResult>(source, keySelector, resultSelector, comparer);
        }

        public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector) =>
            GroupBy(source, keySelector, elementSelector, resultSelector, comparer: null);

        public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
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

            if (resultSelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
            }

            if (IsEmptyArray(source))
            {
                return [];
            }

            return new GroupByResultIterator<TSource, TKey, TElement, TResult>(source, keySelector, elementSelector, resultSelector, comparer);
        }

        private sealed partial class GroupByResultIterator<TSource, TKey, TElement, TResult> : Iterator<TResult>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly Func<TSource, TKey> _keySelector;
            private readonly Func<TSource, TElement> _elementSelector;
            private readonly IEqualityComparer<TKey>? _comparer;
            private readonly Func<TKey, IEnumerable<TElement>, TResult> _resultSelector;

            private Lookup<TKey, TElement>? _lookup;
            private Grouping<TKey, TElement>? _g;

            public GroupByResultIterator(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
            {
                _source = source;
                _keySelector = keySelector;
                _elementSelector = elementSelector;
                _comparer = comparer;
                _resultSelector = resultSelector;
            }

            public override Iterator<TResult> Clone() => new GroupByResultIterator<TSource, TKey, TElement, TResult>(_source, _keySelector, _elementSelector, _resultSelector, _comparer);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _lookup = Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer);
                        _g = _lookup._lastGrouping;
                        if (_g is not null)
                        {
                            _state = 2;
                            goto ValidItem;
                        }
                        break;

                    case 2:
                        Debug.Assert(_g is not null);
                        Debug.Assert(_lookup is not null);
                        if (_g != _lookup._lastGrouping)
                        {
                            goto ValidItem;
                        }
                        break;
                }

                Dispose();
                return false;

            ValidItem:
                _g = _g._next;
                Debug.Assert(_g is not null);
                _g.Trim();
                _current = _resultSelector(_g.Key, _g._elements);
                return true;
            }
        }

        private sealed partial class GroupByResultIterator<TSource, TKey, TResult> : Iterator<TResult>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly Func<TSource, TKey> _keySelector;
            private readonly IEqualityComparer<TKey>? _comparer;
            private readonly Func<TKey, IEnumerable<TSource>, TResult> _resultSelector;

            private Lookup<TKey, TSource>? _lookup;
            private Grouping<TKey, TSource>? _g;

            public GroupByResultIterator(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
            {
                _source = source;
                _keySelector = keySelector;
                _resultSelector = resultSelector;
                _comparer = comparer;
            }

            public override Iterator<TResult> Clone() => new GroupByResultIterator<TSource, TKey, TResult>(_source, _keySelector, _resultSelector, _comparer);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _lookup = Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer);
                        _g = _lookup._lastGrouping;
                        if (_g is not null)
                        {
                            _state = 2;
                            goto ValidItem;
                        }
                        break;

                    case 2:
                        Debug.Assert(_g is not null);
                        Debug.Assert(_lookup is not null);
                        if (_g != _lookup._lastGrouping)
                        {
                            goto ValidItem;
                        }
                        break;
                }

                Dispose();
                return false;

            ValidItem:
                _g = _g._next;
                Debug.Assert(_g is not null);
                _g.Trim();
                _current = _resultSelector(_g.Key, _g._elements);
                return true;
            }
        }

        private sealed partial class GroupByIterator<TSource, TKey, TElement> : Iterator<IGrouping<TKey, TElement>>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly Func<TSource, TKey> _keySelector;
            private readonly Func<TSource, TElement> _elementSelector;
            private readonly IEqualityComparer<TKey>? _comparer;

            private Lookup<TKey, TElement>? _lookup;
            private Grouping<TKey, TElement>? _g;

            public GroupByIterator(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey>? comparer)
            {
                _source = source;
                _keySelector = keySelector;
                _elementSelector = elementSelector;
                _comparer = comparer;
            }

            public override Iterator<IGrouping<TKey, TElement>> Clone() => new GroupByIterator<TSource, TKey, TElement>(_source, _keySelector, _elementSelector, _comparer);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _lookup = Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer);
                        _g = _lookup._lastGrouping;
                        if (_g is not null)
                        {
                            _state = 2;
                            goto ValidItem;
                        }
                        break;

                    case 2:
                        Debug.Assert(_g is not null);
                        Debug.Assert(_lookup is not null);
                        if (_g != _lookup._lastGrouping)
                        {
                            goto ValidItem;
                        }
                        break;
                }

                Dispose();
                return false;

            ValidItem:
                _g = _g._next;
                Debug.Assert(_g is not null);
                _current = _g;
                return true;
            }
        }

        private sealed partial class GroupByIterator<TSource, TKey> : Iterator<IGrouping<TKey, TSource>>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly Func<TSource, TKey> _keySelector;
            private readonly IEqualityComparer<TKey>? _comparer;

            private Lookup<TKey, TSource>? _lookup;
            private Grouping<TKey, TSource>? _g;

            public GroupByIterator(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
            {
                _source = source;
                _keySelector = keySelector;
                _comparer = comparer;
            }

            public override Iterator<IGrouping<TKey, TSource>> Clone() => new GroupByIterator<TSource, TKey>(_source, _keySelector, _comparer);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _lookup = Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer);
                        _g = _lookup._lastGrouping;
                        if (_g is not null)
                        {
                            _state = 2;
                            goto ValidItem;
                        }
                        break;

                    case 2:
                        Debug.Assert(_g is not null);
                        Debug.Assert(_lookup is not null);
                        if (_g != _lookup._lastGrouping)
                        {
                            goto ValidItem;
                        }
                        break;
                }

                Dispose();
                return false;

                ValidItem:
                _g = _g._next;
                Debug.Assert(_g is not null);
                _current = _g;
                return true;
            }
        }
    }

    public interface IGrouping<out TKey, out TElement> : IEnumerable<TElement>
    {
        TKey Key { get; }
    }

    [DebuggerDisplay("Key = {Key}")]
    [DebuggerTypeProxy(typeof(SystemLinq_GroupingDebugView<,>))]
    internal sealed class Grouping<TKey, TElement> : IGrouping<TKey, TElement>, IList<TElement>
    {
        internal readonly TKey _key;
        internal readonly int _hashCode;
        internal TElement[] _elements;
        internal int _count;
        internal Grouping<TKey, TElement>? _hashNext;
        internal Grouping<TKey, TElement>? _next;

        internal Grouping(TKey key, int hashCode)
        {
            _key = key;
            _hashCode = hashCode;
            _elements = new TElement[1];
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

        public TKey Key => _key;

        int ICollection<TElement>.Count => _count;

        bool ICollection<TElement>.IsReadOnly => true;

        void ICollection<TElement>.Add(TElement item) => ThrowHelper.ThrowNotSupportedException();

        void ICollection<TElement>.Clear() => ThrowHelper.ThrowNotSupportedException();

        bool ICollection<TElement>.Contains(TElement item) => Array.IndexOf(_elements, item, 0, _count) >= 0;

        void ICollection<TElement>.CopyTo(TElement[] array, int arrayIndex) =>
            Array.Copy(_elements, 0, array, arrayIndex, _count);

        bool ICollection<TElement>.Remove(TElement item)
        {
            ThrowHelper.ThrowNotSupportedException();
            return false;
        }

        int IList<TElement>.IndexOf(TElement item) => Array.IndexOf(_elements, item, 0, _count);

        void IList<TElement>.Insert(int index, TElement item) => ThrowHelper.ThrowNotSupportedException();

        void IList<TElement>.RemoveAt(int index) => ThrowHelper.ThrowNotSupportedException();

        TElement IList<TElement>.this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
                }

                return _elements[index];
            }

            set
            {
                ThrowHelper.ThrowNotSupportedException();
            }
        }
    }
}
