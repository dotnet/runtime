// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>Appends a value to the end of the sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="element">The value to append to <paramref name="source" />.</param>
        /// <returns>A new sequence that ends with <paramref name="element" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  This method does not modify the elements of the collection. Instead, it creates a copy of the collection with the new element.
        /// ]]></format></remarks>
        /// <example>The following code example demonstrates how to use <see cref="O:Enumerable.Append" /> to append a value to the end of the sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet201":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet201":::</example>
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

        /// <summary>Adds a value to the beginning of the sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="element">The value to prepend to <paramref name="source" />.</param>
        /// <returns>A new sequence that begins with <paramref name="element" />.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  This method does not modify the elements of the collection. Instead, it creates a copy of the collection with the new element.
        /// ]]></format></remarks>
        /// <example>The following code example demonstrates how to use <see cref="O:Enumerable.Prepend" /> to prepend a value to the beginning of the sequence.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Enumerable/CS/enumerable.cs" interactive="try-dotnet-method" id="Snippet202":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Enumerable/VB/Enumerable.vb" id="Snippet202":::</example>
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
