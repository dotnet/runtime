// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class AppendPrepend1Iterator<TSource>
        {
            private TSource[] LazyToArray()
            {
                Debug.Assert(GetCount(onlyIfCheap: true) == -1);
                TSource[] result;

                if (_source is ICollection<TSource> c)
                {
                    // Allocate an array of the exact size needed. We have a collection
                    // with an additional item either before it or after it; copy them
                    // all to the new array appropriately.
                    result = new TSource[c.Count + 1];
                    if (_appending)
                    {
                        c.CopyTo(result, 0);
                        result[^1] = _item;
                    }
                    else
                    {
                        c.CopyTo(result, 1);
                        result[0] = _item;
                    }
                }
                else
                {
                    SegmentedArrayBuilder<TSource>.ScratchBuffer scratch = default;
                    SegmentedArrayBuilder<TSource> builder = new(scratch);
                    if (_appending)
                    {
                        builder.AddNonICollectionRange(_source);
                        builder.Add(_item);
                    }
                    else
                    {
                        builder.Add(_item);
                        builder.AddNonICollectionRange(_source);
                    }

                    result = builder.ToArray();
                    builder.Dispose();
                }

                return result;
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

                if (_source is ICollection<TSource> collection)
                {
                    collection.CopyTo(array, index);
                }
                else
                {
                    foreach (TSource item in _source)
                    {
                        array[index++] = item;
                    }
                }

                if (_appending)
                {
                    array[^1] = _item;
                }

                return array;
            }

            public override List<TSource> ToList()
            {
                int count = GetCount(onlyIfCheap: true);

                if (count == 1)
                {
                    // If GetCount returns 1, then _source is empty and only _item should be returned
                    return new List<TSource>(1) { _item };
                }

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
                if (_source is Iterator<TSource> iterator)
                {
                    int count = iterator.GetCount(onlyIfCheap);
                    return count == -1 ? -1 : count + 1;
                }

                return !onlyIfCheap || _source is ICollection<TSource> ? _source.Count() + 1 : -1;
            }

            public override TSource? TryGetFirst(out bool found)
            {
                if (_appending)
                {
                    TSource? first = _source.TryGetFirst(out found);
                    if (found)
                    {
                        return first;
                    }
                }

                found = true;
                return _item;
            }

            public override TSource? TryGetLast(out bool found)
            {
                if (!_appending)
                {
                    TSource? last = _source.TryGetLast(out found);
                    if (found)
                    {
                        return last;
                    }
                }

                found = true;
                return _item;
            }

            public override TSource? TryGetElementAt(int index, out bool found)
            {
                if (!_appending)
                {
                    if (index == 0)
                    {
                        found = true;
                        return _item;
                    }

                    index--;
                    return
                        _source is Iterator<TSource> iterator ? iterator.TryGetElementAt(index, out found) :
                        TryGetElementAtNonIterator(_source, index, out found);
                }

                return base.TryGetElementAt(index, out found);
            }
        }

        private sealed partial class AppendPrependN<TSource>
        {
            private TSource[] LazyToArray()
            {
                Debug.Assert(GetCount(onlyIfCheap: true) == -1);

                if (_source is ICollection<TSource> c)
                {
                    var result = new TSource[checked(_prependCount + c.Count + _appendCount)];

                    _prepended?.Fill(result);
                    c.CopyTo(result, _prependCount);
                    _appended?.FillReversed(result);

                    return result;
                }
                else
                {
                    // Create the new builder with the prepended content and source content. Then
                    // build the resulting array with enough space to also hold any appended content,
                    // and write the appended content directly into the resulting array.
                    SegmentedArrayBuilder<TSource>.ScratchBuffer scratch = default;
                    SegmentedArrayBuilder<TSource> builder = new(scratch);
                    for (SingleLinkedNode<TSource>? node = _prepended; node is not null; node = node.Linked)
                    {
                        builder.Add(node.Item);
                    }
                    builder.AddNonICollectionRange(_source);

                    TSource[] result = builder.ToArray(_appendCount);
                    builder.Dispose();

                    _appended?.FillReversed(result);
                    return result;
                }
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
                for (SingleLinkedNode<TSource>? node = _prepended; node is not null; node = node.Linked)
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
                for (SingleLinkedNode<TSource>? node = _appended; node is not null; node = node.Linked)
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

                _prepended?.Fill(SetCountAndGetSpan(list, _prependCount));

                list.AddRange(_source);

                _appended?.FillReversed(SetCountAndGetSpan(list, list.Count + _appendCount));

                return list;
            }

            public override int GetCount(bool onlyIfCheap)
            {
                if (_source is Iterator<TSource> iterator)
                {
                    int count = iterator.GetCount(onlyIfCheap);
                    return count == -1 ? -1 : count + _appendCount + _prependCount;
                }

                return !onlyIfCheap || _source is ICollection<TSource> ? _source.Count() + _appendCount + _prependCount : -1;
            }
        }
    }
}
