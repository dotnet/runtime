// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private abstract partial class AppendPrependIterator<TSource> : IIListProvider<TSource>
        {
            public abstract TSource[] ToArray();

            public abstract List<TSource> ToList();

            public abstract int GetCount(bool onlyIfCheap);
        }

        private sealed partial class AppendPrepend1Iterator<TSource>
        {
            private TSource[] LazyToArray()
            {
                Debug.Assert(GetCount(onlyIfCheap: true) == -1);

                LargeArrayBuilder<TSource> builder = new();

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

                EnumerableHelpers.Copy(_source, array, index, count - 1);

                if (_appending)
                {
                    array[array.Length - 1] = _item;
                }

                return array;
            }

            private List<TSource> LazyToList()
            {
                Debug.Assert(GetCount(onlyIfCheap: true) == -1);

                var list = new List<TSource>();
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

            public override List<TSource> ToList()
            {
                int count = GetCount(onlyIfCheap: true);
                if (count == -1)
                {
                    return LazyToList();
                }

                if (count == 1)
                {
                    // If GetCount returns 1, then _source is empty and only _item should be returned
                    return ToSingleItemList(_item);
                }

                var list = new List<TSource>(count);
                Span<TSource> span = SetCountAndGetSpan(list, count);
                int index;
                if (_appending)
                {
                    index = 0;
                }
                else
                {
                    span[0] = _item;
                    index = 1;
                }

                foreach (var item in _source)
                {
                    span[index] = item;
                    ++index;
                }

                if (_appending)
                {
                    span[index] = _item;
                    ++index; // For the Debug.Assert, should be elided in release mode
                }

                Debug.Assert(index == span.Length, "All list elements were not initialized.");
                return list;
            }

            public override int GetCount(bool onlyIfCheap)
            {
                if (_source is IIListProvider<TSource> listProv)
                {
                    int count = listProv.GetCount(onlyIfCheap);
                    return count == -1 ? -1 : count + 1;
                }

                return !onlyIfCheap || _source is ICollection<TSource> ? _source.Count() + 1 : -1;
            }
        }

        private sealed partial class AppendPrependN<TSource>
        {
            private TSource[] LazyToArray()
            {
                Debug.Assert(GetCount(onlyIfCheap: true) == -1);

                SparseArrayBuilder<TSource> builder = new();

                if (_prepended != null)
                {
                    builder.Reserve(_prependCount);
                }

                builder.AddRange(_source);

                if (_appended != null)
                {
                    builder.Reserve(_appendCount);
                }

                TSource[] array = builder.ToArray();

                int index = 0;
                for (SingleLinkedNode<TSource>? node = _prepended; node != null; node = node.Linked)
                {
                    array[index++] = node.Item;
                }

                _appended?.Fill(array.AsSpan(^_appendCount));

                return array;
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
                for (SingleLinkedNode<TSource>? node = _prepended; node != null; node = node.Linked)
                {
                    array[index] = node.Item;
                    ++index;
                }

                if (_source is ICollection<TSource> sourceCollection)
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
                for (SingleLinkedNode<TSource>? node = _appended; node != null; node = node.Linked)
                {
                    --index;
                    array[index] = node.Item;
                }

                return array;
            }

            private List<TSource> LazyToList()
            {
                Debug.Assert(GetCount(onlyIfCheap: true) == -1);

                var list = new List<TSource>();

                if (_prepended != null)
                {
                    Span<TSource> span = SetCountAndGetSpan(list, _prependCount);
                    int index = 0;
                    for (SingleLinkedNode<TSource>? node = _prepended; node != null; node = node.Linked)
                    {
                        span[index] = node.Item;
                        ++index;
                    }
                }

                list.AddRange(_source);

                if (_appended != null)
                {
                    int index = list.Count;
                    Span<TSource> span = SetCountAndGetSpan(list, index + _appendCount);
                    _appended.Fill(span[index..]);
                }

                return list;
            }

            public override List<TSource> ToList()
            {
                int count = GetCount(onlyIfCheap: true);
                if (count == -1)
                {
                    return LazyToList();
                }

                var list = new List<TSource>(count);
                Span<TSource> span = SetCountAndGetSpan(list, count);
                int index = 0;

                for (SingleLinkedNode<TSource>? node = _prepended; node != null; node = node.Linked)
                {
                    list[index] = node.Item;
                    ++index;
                }

                foreach (var item in _source)
                {
                    span[index] = item;
                    ++index;
                }

                if (_appended != null)
                {
                    _appended.Fill(span[index..]);
                    index += _appendCount; // For the Debug.Assert, should be elided in release mode
                }

                Debug.Assert(index == span.Length, "All list elements were not initialized.");
                return list;
            }

            public override int GetCount(bool onlyIfCheap)
            {
                if (_source is IIListProvider<TSource> listProv)
                {
                    int count = listProv.GetCount(onlyIfCheap);
                    return count == -1 ? -1 : count + _appendCount + _prependCount;
                }

                return !onlyIfCheap || _source is ICollection<TSource> ? _source.Count() + _appendCount + _prependCount : -1;
            }
        }
    }
}
