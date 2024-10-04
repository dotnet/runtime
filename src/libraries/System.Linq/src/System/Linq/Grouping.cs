// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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

            private protected override Iterator<TResult> Clone() => new GroupByResultIterator<TSource, TKey, TElement, TResult>(_source, _keySelector, _elementSelector, _resultSelector, _comparer);

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
                _current = _resultSelector(_g.Key, _g);
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

            private protected override Iterator<TResult> Clone() => new GroupByResultIterator<TSource, TKey, TResult>(_source, _keySelector, _resultSelector, _comparer);

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
                _current = _resultSelector(_g.Key, _g);
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

            private protected override Iterator<IGrouping<TKey, TElement>> Clone() => new GroupByIterator<TSource, TKey, TElement>(_source, _keySelector, _elementSelector, _comparer);

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

            private protected override Iterator<IGrouping<TKey, TSource>> Clone() => new GroupByIterator<TSource, TKey>(_source, _keySelector, _comparer);

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
        private GroupingElementArray<TElement> _inlineElements;
        private TElement[]? _elements;
        private int _count;
        internal Grouping<TKey, TElement>? _hashNext;
        internal Grouping<TKey, TElement>? _next;

        internal Grouping(TKey key, int hashCode)
        {
            _key = key;
            _hashCode = hashCode;
        }

        internal void Add(TElement element)
        {
            Span<TElement> destination;

            if (_elements is null)
            {
                destination = _inlineElements;

                if (_count == GroupingElementArray<TElement>.Size)
                {
                    _elements = new TElement[checked(_count * 2)];
                    destination.CopyTo(_elements);
                    destination = _elements;
                }
            }
            else
            {
                if (_elements.Length == _count)
                {
                    Array.Resize(ref _elements, checked(_count * 2));
                }

                destination = _elements;
            }

            destination[_count++] = element;
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            Debug.Assert(_count > 0, "A grouping should only have been created if an element was being added to it.");
            if (_elements is null)
            {
                return new GroupingElementArrayEnumerator<TElement>(_inlineElements, _count);
            }
            else
            {
                return new PartialArrayEnumerator<TElement>(_elements, _count);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public TKey Key => _key;

        int ICollection<TElement>.Count => _count;

        bool ICollection<TElement>.IsReadOnly => true;

        void ICollection<TElement>.Add(TElement item) => ThrowHelper.ThrowNotSupportedException();

        void ICollection<TElement>.Clear() => ThrowHelper.ThrowNotSupportedException();

        bool ICollection<TElement>.Contains(TElement item)
        {
            if (_elements is null)
            {
                return IndexOfInlineElements(item) >= 0;
            }

            return Array.IndexOf(_elements, item) >= 0;
        }

        void ICollection<TElement>.CopyTo(TElement[] array, int arrayIndex) => GetElements().CopyTo(array.AsSpan(arrayIndex));

        bool ICollection<TElement>.Remove(TElement item) => ThrowHelper.ThrowNotSupportedException_Boolean();

        int IList<TElement>.IndexOf(TElement item)
        {
            if (_elements is null)
            {
                return IndexOfInlineElements(item);
            }

            return Array.IndexOf(_elements, item, 0, _count);
        }

        void IList<TElement>.Insert(int index, TElement item) => ThrowHelper.ThrowNotSupportedException();

        void IList<TElement>.RemoveAt(int index) => ThrowHelper.ThrowNotSupportedException();

        TElement IList<TElement>.this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_count)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
                }

                return GetElements()[index];
            }

            set => ThrowHelper.ThrowNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<TElement> GetElements()
        {
            ReadOnlySpan<TElement> buffer = _elements is null ? _inlineElements : _elements;
            return buffer.Slice(0, _count);
        }

        private int IndexOfInlineElements(TElement item)
        {
            ReadOnlySpan<TElement> inlineElements = ((ReadOnlySpan<TElement>)_inlineElements).Slice(0, _count);
            for (int i = 0; i < inlineElements.Length; i++)
            {
                if (EqualityComparer<TElement>.Default.Equals(inlineElements[i], item))
                {
                    return i;
                }
            }

            return -1;
        }

    }

    [InlineArray(GroupingElementArray<int>.Size)]
    internal struct GroupingElementArray<TElement>
    {
        public const int Size = 3;
        private TElement _element0;
    }

    internal sealed class GroupingElementArrayEnumerator<TElement> : IEnumerator<TElement>
    {
        private GroupingElementArray<TElement> _array;
        private int _count;
        private int _index;

        public GroupingElementArrayEnumerator(GroupingElementArray<TElement> array, int count)
        {
            Debug.Assert((uint)_count <= GroupingElementArray<TElement>.Size);
            _array = array;
            _count = count;
            _index = -1;
        }

        public bool MoveNext()
        {
            if (_index + 1 < _count)
            {
                _index++;
                return true;
            }

            return false;
        }

        public TElement Current => _array[_index];

        object? IEnumerator.Current => Current;

        public void Dispose() { }

        public void Reset() => _index = -1;
    }

}
