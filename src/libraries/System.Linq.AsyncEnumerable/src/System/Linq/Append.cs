// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Appends a value to the end of the sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values.</param>
        /// <param name="element">The value to append to source.</param>
        /// <returns>A new sequence that ends with element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static IAsyncEnumerable<TSource> Append<TSource>(
            this IAsyncEnumerable<TSource> source,
            TSource element)
        {
            ArgumentNullException.ThrowIfNull(source);

            return source is AppendPrependAsyncIterator<TSource> appendable
                ? appendable.Append(element)
                : new AppendPrepend1AsyncIterator<TSource>(source, element, appending: true);
        }

        /// <summary>
        /// Represents the insertion of one or more items before or after an <see cref="IAsyncEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
        private abstract class AppendPrependAsyncIterator<TSource> : AsyncIterator<TSource>
        {
            protected readonly IAsyncEnumerable<TSource> _source;
            protected IAsyncEnumerator<TSource>? _enumerator;

            protected AppendPrependAsyncIterator(IAsyncEnumerable<TSource> source)
            {
                Debug.Assert(source is not null);
                _source = source;
            }

            protected void GetSourceEnumerator()
            {
                Debug.Assert(_enumerator is null);
                _enumerator = _source.GetAsyncEnumerator(_cancellationToken);
            }

            public abstract AppendPrependAsyncIterator<TSource> Append(TSource item);

            public abstract AppendPrependAsyncIterator<TSource> Prepend(TSource item);

            protected async ValueTask<bool> LoadFromEnumeratorAsync()
            {
                Debug.Assert(_enumerator is not null);
                if (await _enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    _current = _enumerator.Current;
                    return true;
                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }

            public override async ValueTask DisposeAsync()
            {
                if (_enumerator is not null)
                {
                    await _enumerator.DisposeAsync().ConfigureAwait(false);
                    _enumerator = null;
                }

                await base.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Represents the insertion of an item before or after an <see cref="IAsyncEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
        private sealed class AppendPrepend1AsyncIterator<TSource> : AppendPrependAsyncIterator<TSource>
        {
            private readonly TSource _item;
            private readonly bool _appending;

            public AppendPrepend1AsyncIterator(IAsyncEnumerable<TSource> source, TSource item, bool appending)
                : base(source)
            {
                _item = item;
                _appending = appending;
            }

            private protected override AsyncIterator<TSource> Clone() => new AppendPrepend1AsyncIterator<TSource>(_source, _item, _appending);

            public override async ValueTask<bool> MoveNextAsync()
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
                        if (await LoadFromEnumeratorAsync().ConfigureAwait(false))
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

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }

            public override AppendPrependAsyncIterator<TSource> Append(TSource item)
            {
                if (_appending)
                {
                    return new AppendPrependNAsyncIterator<TSource>(_source, null, new SingleLinkedNode<TSource>(_item).Add(item), prependCount: 0, appendCount: 2);
                }
                else
                {
                    return new AppendPrependNAsyncIterator<TSource>(_source, new SingleLinkedNode<TSource>(_item), new SingleLinkedNode<TSource>(item), prependCount: 1, appendCount: 1);
                }
            }

            public override AppendPrependAsyncIterator<TSource> Prepend(TSource item)
            {
                if (_appending)
                {
                    return new AppendPrependNAsyncIterator<TSource>(_source, new SingleLinkedNode<TSource>(item), new SingleLinkedNode<TSource>(_item), prependCount: 1, appendCount: 1);
                }
                else
                {
                    return new AppendPrependNAsyncIterator<TSource>(_source, new SingleLinkedNode<TSource>(_item).Add(item), null, prependCount: 2, appendCount: 0);
                }
            }
        }

        /// <summary>
        /// Represents the insertion of multiple items before or after an <see cref="IAsyncEnumerable{TSource}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
        private sealed class AppendPrependNAsyncIterator<TSource> : AppendPrependAsyncIterator<TSource>
        {
            private readonly SingleLinkedNode<TSource>? _prepended;
            private readonly SingleLinkedNode<TSource>? _appended;
            private readonly int _prependCount;
            private readonly int _appendCount;

            private SingleLinkedNode<TSource>? _node;
            private TSource[]? _appendedArray;
            private int _appendedIndex;

            public AppendPrependNAsyncIterator(IAsyncEnumerable<TSource> source, SingleLinkedNode<TSource>? prepended, SingleLinkedNode<TSource>? appended, int prependCount, int appendCount)
                : base(source)
            {
                Debug.Assert(prepended is not null || appended is not null);
                Debug.Assert(prependCount > 0 || appendCount > 0);
                Debug.Assert(prependCount + appendCount >= 2);
                Debug.Assert((prepended?.GetCount() ?? 0) == prependCount);
                Debug.Assert((appended?.GetCount() ?? 0) == appendCount);

                _prepended = prepended;
                _appended = appended;
                _prependCount = prependCount;
                _appendCount = appendCount;
            }

            private protected override AsyncIterator<TSource> Clone() => new AppendPrependNAsyncIterator<TSource>(_source, _prepended, _appended, _prependCount, _appendCount);

            public override async ValueTask<bool> MoveNextAsync()
            {
                switch (_state)
                {
                    case 1:
                        _node = _prepended;
                        _state = 2;
                        goto case 2;
                    case 2:
                        if (_node is not null)
                        {
                            _current = _node.Item;
                            _node = _node.Linked;
                            return true;
                        }

                        GetSourceEnumerator();
                        _state = 3;
                        goto case 3;
                    case 3:
                        if (await LoadFromEnumeratorAsync().ConfigureAwait(false))
                        {
                            return true;
                        }

                        if (_appended is null)
                        {
                            return false;
                        }

                        // Convert appended items to array to iterate in correct order
                        _appendedArray = _appended.ToArray(_appendCount);
                        _appendedIndex = 0;
                        _state = 4;
                        goto case 4;
                    case 4:
                        if (_appendedIndex < _appendedArray!.Length)
                        {
                            _current = _appendedArray[_appendedIndex++];
                            return true;
                        }
                        break;
                }

                await DisposeAsync().ConfigureAwait(false);
                return false;
            }

            public override AppendPrependAsyncIterator<TSource> Append(TSource item)
            {
                var appended = _appended is not null ? _appended.Add(item) : new SingleLinkedNode<TSource>(item);
                return new AppendPrependNAsyncIterator<TSource>(_source, _prepended, appended, _prependCount, _appendCount + 1);
            }

            public override AppendPrependAsyncIterator<TSource> Prepend(TSource item)
            {
                var prepended = _prepended is not null ? _prepended.Add(item) : new SingleLinkedNode<TSource>(item);
                return new AppendPrependNAsyncIterator<TSource>(_source, prepended, _appended, _prependCount + 1, _appendCount);
            }
        }
    }
}
