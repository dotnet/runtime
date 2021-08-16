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

                EnumerableHelpers.Copy(_source, array, index, count - 1);

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

                var builder = new SparseArrayBuilder<TSource>(initialize: true);

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

                index = array.Length - 1;
                for (SingleLinkedNode<TSource>? node = _appended; node != null; node = node.Linked)
                {
                    array[index--] = node.Item;
                }

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

            public override List<TSource> ToList()
            {
                int count = GetCount(onlyIfCheap: true);
                List<TSource> list = count == -1 ? new List<TSource>() : new List<TSource>(count);

                for (SingleLinkedNode<TSource>? node = _prepended; node != null; node = node.Linked)
                {
                    list.Add(node.Item);
                }

                list.AddRange(_source);

                if (_appended != null)
                {
                    list.AddRange(_appended.ToArray(_appendCount));
                }

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
