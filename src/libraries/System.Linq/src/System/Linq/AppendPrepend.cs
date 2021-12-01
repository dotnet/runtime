// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TSource> Append<TSource>(this IEnumerable<TSource> source, TSource element)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return source is AppendPrependIterator<TSource> appendable
                ? appendable.Append(element)
                : new AppendPrepend1Iterator<TSource>(source, element, appending: true);
        }

        public static IEnumerable<TSource> Prepend<TSource>(this IEnumerable<TSource> source, TSource element)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return source is AppendPrependIterator<TSource> appendable
                ? appendable.Prepend(element)
                : new AppendPrepend1Iterator<TSource>(source, element, appending: false);
        }

        /// <summary>
        /// Represents the insertion of one or more items before or after an <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
        private abstract partial class AppendPrependIterator<TSource> : Iterator<TSource>
        {
            protected readonly IEnumerable<TSource> _source;
            protected IEnumerator<TSource>? _enumerator;

            protected AppendPrependIterator(IEnumerable<TSource> source)
            {
                Debug.Assert(source != null);
                _source = source;
            }

            protected void GetSourceEnumerator()
            {
                Debug.Assert(_enumerator == null);
                _enumerator = _source.GetEnumerator();
            }

            public abstract AppendPrependIterator<TSource> Append(TSource item);

            public abstract AppendPrependIterator<TSource> Prepend(TSource item);

            protected bool LoadFromEnumerator()
            {
                Debug.Assert(_enumerator != null);
                if (_enumerator.MoveNext())
                {
                    _current = _enumerator.Current;
                    return true;
                }

                Dispose();
                return false;
            }

            public override void Dispose()
            {
                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }

                base.Dispose();
            }
        }

        /// <summary>
        /// Represents the insertion of an item before or after an <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
        private sealed partial class AppendPrepend1Iterator<TSource> : AppendPrependIterator<TSource>
        {
            private readonly TSource _item;
            private readonly bool _appending;

            public AppendPrepend1Iterator(IEnumerable<TSource> source, TSource item, bool appending)
                : base(source)
            {
                _item = item;
                _appending = appending;
            }

            public override Iterator<TSource> Clone() => new AppendPrepend1Iterator<TSource>(_source, _item, _appending);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _state = 2;
                        if (!_appending)
                        {
                            _current = _item;
                            return true;
                        }

                        goto case 2;
                    case 2:
                        GetSourceEnumerator();
                        _state = 3;
                        goto case 3;
                    case 3:
                        if (LoadFromEnumerator())
                        {
                            return true;
                        }

                        if (_appending)
                        {
                            _current = _item;
                            return true;
                        }

                        break;
                }

                Dispose();
                return false;
            }

            public override AppendPrependIterator<TSource> Append(TSource item)
            {
                if (_appending)
                {
                    return new AppendPrependN<TSource>(_source, null, new SingleLinkedNode<TSource>(_item).Add(item), prependCount: 0, appendCount: 2);
                }
                else
                {
                    return new AppendPrependN<TSource>(_source, new SingleLinkedNode<TSource>(_item), new SingleLinkedNode<TSource>(item), prependCount: 1, appendCount: 1);
                }
            }

            public override AppendPrependIterator<TSource> Prepend(TSource item)
            {
                if (_appending)
                {
                    return new AppendPrependN<TSource>(_source, new SingleLinkedNode<TSource>(item), new SingleLinkedNode<TSource>(_item), prependCount: 1, appendCount: 1);
                }
                else
                {
                    return new AppendPrependN<TSource>(_source, new SingleLinkedNode<TSource>(_item).Add(item), null, prependCount: 2, appendCount: 0);
                }
            }
        }

        /// <summary>
        /// Represents the insertion of multiple items before or after an <see cref="IEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
        private sealed partial class AppendPrependN<TSource> : AppendPrependIterator<TSource>
        {
            private readonly SingleLinkedNode<TSource>? _prepended;
            private readonly SingleLinkedNode<TSource>? _appended;
            private readonly int _prependCount;
            private readonly int _appendCount;
            private SingleLinkedNode<TSource>? _node;

            public AppendPrependN(IEnumerable<TSource> source, SingleLinkedNode<TSource>? prepended, SingleLinkedNode<TSource>? appended, int prependCount, int appendCount)
                : base(source)
            {
                Debug.Assert(prepended != null || appended != null);
                Debug.Assert(prependCount > 0 || appendCount > 0);
                Debug.Assert(prependCount + appendCount >= 2);
                Debug.Assert((prepended?.GetCount() ?? 0) == prependCount);
                Debug.Assert((appended?.GetCount() ?? 0) == appendCount);

                _prepended = prepended;
                _appended = appended;
                _prependCount = prependCount;
                _appendCount = appendCount;
            }

            public override Iterator<TSource> Clone() => new AppendPrependN<TSource>(_source, _prepended, _appended, _prependCount, _appendCount);

            public override bool MoveNext()
            {
                switch (_state)
                {
                    case 1:
                        _node = _prepended;
                        _state = 2;
                        goto case 2;
                    case 2:
                        if (_node != null)
                        {
                            _current = _node.Item;
                            _node = _node.Linked;
                            return true;
                        }

                        GetSourceEnumerator();
                        _state = 3;
                        goto case 3;
                    case 3:
                        if (LoadFromEnumerator())
                        {
                            return true;
                        }

                        if (_appended == null)
                        {
                            return false;
                        }

                        _enumerator = ((IEnumerable<TSource>)_appended.ToArray(_appendCount)).GetEnumerator();
                        _state = 4;
                        goto case 4;
                    case 4:
                        return LoadFromEnumerator();
                }

                Dispose();
                return false;
            }

            public override AppendPrependIterator<TSource> Append(TSource item)
            {
                var appended = _appended != null ? _appended.Add(item) : new SingleLinkedNode<TSource>(item);
                return new AppendPrependN<TSource>(_source, _prepended, appended, _prependCount, _appendCount + 1);
            }

            public override AppendPrependIterator<TSource> Prepend(TSource item)
            {
                var prepended = _prepended != null ? _prepended.Add(item) : new SingleLinkedNode<TSource>(item);
                return new AppendPrependN<TSource>(_source, prepended, _appended, _prependCount + 1, _appendCount);
            }
        }
    }
}
