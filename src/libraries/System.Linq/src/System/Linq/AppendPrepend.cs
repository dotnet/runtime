// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                throw Error.ArgumentNull(nameof(source));
            }

            AppendPrependIterator<TSource> appendable = source as AppendPrependIterator<TSource>;
            if (appendable != null)
            {
                return appendable.Append(element);
            }

            return new AppendPrepend1Iterator<TSource>(source, element, true);
        }

        public static IEnumerable<TSource> Prepend<TSource>(this IEnumerable<TSource> source, TSource element)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            AppendPrependIterator<TSource> appendable = source as AppendPrependIterator<TSource>;
            if (appendable != null)
            {
                return appendable.Prepend(element);
            }

            return new AppendPrepend1Iterator<TSource>(source, element, false);
        }

        private abstract class AppendPrependIterator<TSource> : Iterator<TSource>, IIListProvider<TSource>
        {
            protected readonly IEnumerable<TSource> _source;
            protected IEnumerator<TSource> _enumerator;

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

            public abstract TSource[] ToArray();

            public abstract List<TSource> ToList();

            public abstract int GetCount(bool onlyIfCheap);
        }

        private class AppendPrepend1Iterator<TSource> : AppendPrependIterator<TSource>
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
                    return new AppendPrependN<TSource>(_source, null, new SingleLinkedNode<TSource>(_item, item));
                }
                else
                {
                    return new AppendPrependN<TSource>(_source, new SingleLinkedNode<TSource>(_item), new SingleLinkedNode<TSource>(item));
                }
            }

            public override AppendPrependIterator<TSource> Prepend(TSource item)
            {
                if (_appending)
                {
                    return new AppendPrependN<TSource>(_source, new SingleLinkedNode<TSource>(item), new SingleLinkedNode<TSource>(_item));
                }
                else
                {
                    return new AppendPrependN<TSource>(_source, new SingleLinkedNode<TSource>(_item, item), null);
                }
            }

            private TSource[] LazyToArray()
            {
                Debug.Assert(GetCount(onlyIfCheap: true) == -1);

                var builder = new LargeArrayBuilder<TSource>(initialize: true);
                
                if (!_appending)
                {
                    builder.SlowAdd(_item);
                }

                builder.AddRange(_source);

                if (_appending)
                {
                    builder.SlowAdd(_item);
                }

                return builder.ToArray();
            }

            public override TSource[] ToArray()
            {
                int count = GetCount(onlyIfCheap: true);
                if (count == -1)
                {
                    return LazyToArray();
                }

                TSource[] array = new TSource[count];
                int index;
                if (_appending)
                {
                    index = 0;
                }
                else
                {
                    array[0] = _item;
                    index = 1;
                }

                ICollection<TSource> sourceCollection = _source as ICollection<TSource>;
                if (sourceCollection != null)
                {
                    sourceCollection.CopyTo(array, index);
                }
                else
                {
                    foreach (TSource item in _source)
                    {
                        array[index] = item;
                        ++index;
                    }
                }

                if (_appending)
                {
                    array[array.Length - 1] = _item;
                }

                return array;
            }

            public override List<TSource> ToList()
            {
                int count = GetCount(onlyIfCheap: true);
                List<TSource> list = count == -1 ? new List<TSource>() : new List<TSource>(count);
                if (!_appending)
                {
                    list.Add(_item);
                }

                list.AddRange(_source);
                if (_appending)
                {
                    list.Add(_item);
                }

                return list;
            }

            public override int GetCount(bool onlyIfCheap)
            {
                IIListProvider<TSource> listProv = _source as IIListProvider<TSource>;
                if (listProv != null)
                {
                    int count = listProv.GetCount(onlyIfCheap);
                    return count == -1 ? -1 : count + 1;
                }

                return !onlyIfCheap || _source is ICollection<TSource> ? _source.Count() + 1 : -1;
            }
        }

        private sealed class SingleLinkedNode<TSource>
        {
            public SingleLinkedNode(TSource first, TSource second)
            {
                Linked = new SingleLinkedNode<TSource>(first);
                Item = second;
                Count = 2;
            }

            public SingleLinkedNode(TSource item)
            {
                Item = item;
                Count = 1;
            }

            private SingleLinkedNode(SingleLinkedNode<TSource> linked, TSource item)
            {
                Debug.Assert(linked != null);
                Linked = linked;
                Item = item;
                Count = linked.Count + 1;
            }

            public TSource Item { get; }

            public SingleLinkedNode<TSource> Linked { get; }

            public int Count { get; }

            public SingleLinkedNode<TSource> Add(TSource item) => new SingleLinkedNode<TSource>(this, item);

            public IEnumerator<TSource> GetEnumerator()
            {
                return ((IEnumerable<TSource>)ToArray()).GetEnumerator();
            }

            public TSource[] ToArray()
            {
                TSource[] array = new TSource[Count];
                int index = Count;
                for (SingleLinkedNode<TSource> node = this; node != null; node = node.Linked)
                {
                    --index;
                    array[index] = node.Item;
                }

                Debug.Assert(index == 0);
                return array;
            }
        }

        private class AppendPrependN<TSource> : AppendPrependIterator<TSource>
        {
            private readonly SingleLinkedNode<TSource> _prepended;
            private readonly SingleLinkedNode<TSource> _appended;
            private SingleLinkedNode<TSource> _node;

            public AppendPrependN(IEnumerable<TSource> source, SingleLinkedNode<TSource> prepended, SingleLinkedNode<TSource> appended)
                : base(source)
            {
                Debug.Assert(prepended != null || appended != null);
                _prepended = prepended;
                _appended = appended;
            }

            public override Iterator<TSource> Clone() => new AppendPrependN<TSource>(_source, _prepended, _appended);

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

                        _enumerator = _appended.GetEnumerator();
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
                return new AppendPrependN<TSource>(_source, _prepended, _appended != null ? _appended.Add(item) : new SingleLinkedNode<TSource>(item));
            }

            public override AppendPrependIterator<TSource> Prepend(TSource item)
            {
                return new AppendPrependN<TSource>(_source, _prepended != null ? _prepended.Add(item) : new SingleLinkedNode<TSource>(item), _appended);
            }

            private TSource[] LazyToArray()
            {
                Debug.Assert(GetCount(onlyIfCheap: true) == -1);

                var builder = new LargeArrayBuilder<TSource>(initialize: true);

                for (SingleLinkedNode<TSource> node = _prepended; node != null; node = node.Linked)
                {
                    builder.Add(node.Item);
                }

                builder.AddRange(_source);

                if (_appended != null)
                {
                    foreach (TSource item in _appended.ToArray())
                    {
                        builder.Add(item);
                    }
                }

                return builder.ToArray();
            }

            public override TSource[] ToArray()
            {
                int count = GetCount(onlyIfCheap: true);
                if (count == -1)
                {
                    return LazyToArray();
                }

                TSource[] array = new TSource[count];
                int index = 0;
                for (SingleLinkedNode<TSource> node = _prepended; node != null; node = node.Linked)
                {
                    array[index] = node.Item;
                    ++index;
                }

                ICollection<TSource> sourceCollection = _source as ICollection<TSource>;
                if (sourceCollection != null)
                {
                    sourceCollection.CopyTo(array, index);
                }
                else
                {
                    foreach (TSource item in _source)
                    {
                        array[index] = item;
                        ++index;
                    }
                }

                index = array.Length;
                for (SingleLinkedNode<TSource> node = _appended; node != null; node = node.Linked)
                {
                    --index;
                    array[index] = node.Item;
                }

                return array;
            }

            public override List<TSource> ToList()
            {
                int count = GetCount(onlyIfCheap: true);
                List<TSource> list = count == -1 ? new List<TSource>() : new List<TSource>(count);
                for (SingleLinkedNode<TSource> node = _prepended; node != null; node = node.Linked)
                {
                    list.Add(node.Item);
                }

                list.AddRange(_source);
                if (_appended != null)
                {
                    IEnumerator<TSource> e = _appended.GetEnumerator();
                    while (e.MoveNext())
                    {
                        list.Add(e.Current);
                    }
                }

                return list;
            }

            public override int GetCount(bool onlyIfCheap)
            {
                IIListProvider<TSource> listProv = _source as IIListProvider<TSource>;
                if (listProv != null)
                {
                    int count = listProv.GetCount(onlyIfCheap);
                    return count == -1 ? -1 : count + (_appended == null ? 0 : _appended.Count) + (_prepended == null ? 0 : _prepended.Count);
                }

                return !onlyIfCheap || _source is ICollection<TSource> ? _source.Count() + (_appended == null ? 0 : _appended.Count) + (_prepended == null ? 0 : _prepended.Count) : -1;
            }
        }
    }
}
