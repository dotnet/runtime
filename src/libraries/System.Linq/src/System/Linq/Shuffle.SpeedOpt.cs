// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Linq
{
    public static partial class Enumerable
    {
        private sealed partial class ShuffleIterator<TSource>
        {
            public override TSource[] ToArray()
            {
                TSource[] array = _source.ToArray();
                Random.Shared.Shuffle(array);
                return array;
            }

            public override List<TSource> ToList()
            {
                List<TSource> list = _source.ToList();
                Random.Shared.Shuffle(CollectionsMarshal.AsSpan(list));
                return list;
            }

            public override int GetCount(bool onlyIfCheap) =>
                !onlyIfCheap ? _source.Count() :
                TryGetNonEnumeratedCount(_source, out int count) ? count :
                -1;

            public override TSource? TryGetFirst(out bool found) =>
                TryGetElementAt(0, out found);

            public override TSource? TryGetLast(out bool found) =>
                TryGetElementAt(0, out found);

            public override TSource? TryGetElementAt(int index, out bool found)
            {
                if (_source is Iterator<TSource> iterator &&
                    iterator.GetCount(onlyIfCheap: true) is int iteratorCount &&
                    iteratorCount >= 0)
                {
                    if ((uint)index < (uint)iteratorCount)
                    {
                        return iterator.TryGetElementAt(Random.Shared.Next(0, iteratorCount), out found);
                    }
                }
                else if (_source is IList<TSource> list)
                {
                    int listCount = list.Count;
                    if ((uint)index < (uint)listCount)
                    {
                        found = true;
                        return list[Random.Shared.Next(0, listCount)];
                    }
                }
                else if (index >= 0)
                {
                    TSource[] array = _source.ToArray();
                    if (index < array.Length)
                    {
                        found = true;
                        return array[Random.Shared.Next(0, array.Length)];
                    }
                }

                found = false;
                return default;
            }

            public override Iterator<TSource>? Take(int count)
            {
                // If the source is known to have fewer elements than count, we're best off just using the default implementation.
                if (_source.TryGetNonEnumeratedCount(out int sourceCount) && sourceCount <= count)
                {
                    return base.Take(count);
                }

                // Otherwise, we either don't know how many elements are in the source, or we know it's more than count.
                // Try to optimize by using reservoir sampling to get a random sample of count elements.
                return new TakeShuffleIterator<TSource>(_source, count);
            }
        }

        private sealed partial class TakeShuffleIterator<TSource> : Iterator<TSource>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly int _takeCount;
            private List<TSource>? _buffer;

            public TakeShuffleIterator(IEnumerable<TSource> source, int takeCount)
            {
                Debug.Assert(source is not null);
                Debug.Assert(takeCount > 0);

                _source = source;
                _takeCount = takeCount;
            }

            private protected override Iterator<TSource> Clone() => new TakeShuffleIterator<TSource>(_source, _takeCount);

            public override bool MoveNext()
            {
                int state = _state;

            Initialized:
                if (state > 1)
                {
                    List<TSource>? buffer = _buffer;
                    Debug.Assert(buffer is not null);

                    int i = state - 2;
                    if (i < buffer.Count)
                    {
                        _current = buffer[i];
                        _state++;
                        return true;
                    }
                }
                else if (state == 1)
                {
                    List<TSource>? buffer = SampleToList(_source, _takeCount);
                    if (buffer is not null)
                    {
                        _buffer = buffer;
                        _state = state = 2;
                        goto Initialized;
                    }
                }

                Dispose();
                return false;
            }

            public override void Dispose()
            {
                _buffer = null;
                base.Dispose();
            }

            /// <summary>Uses reservoir sampling to randomly select <paramref name="takeCount"/> elements from <paramref name="source"/>.</summary>
            private static List<TSource>? SampleToList(IEnumerable<TSource> source, int takeCount)
            {
                List<TSource>? reservoir = null;

                if (source is IList<TSource> list)
                {
                    int listCount = list.Count;
                    Debug.Assert(listCount > takeCount, "Known listCount <= takeCount should have been handled by Iterator.Take override");

                    reservoir = new(takeCount);
                    for (int i = 0; i < listCount; i++)
                    {
                        if (i < takeCount)
                        {
                            // Fill the reservoir with the first takeCount elements from the source.
                            reservoir.Add(list[i]);
                        }
                        else
                        {
                            // For each subsequent element in the source, randomly replace an element in the
                            // reservoir with a decreasing probability.
                            int r = Random.Shared.Next(i + 1);
                            if (r < takeCount)
                            {
                                reservoir[r] = list[i];
                            }
                        }
                    }
                }
                else
                {
                    using IEnumerator<TSource> e = source.GetEnumerator();
                    if (e.MoveNext())
                    {
                        reservoir = [e.Current];

                        // Fill the reservoir with the first takeCount elements from the source.
                        // If we can't fill it, just return what we get.
                        while (reservoir.Count < takeCount)
                        {
                            if (!e.MoveNext())
                            {
                                goto ReturnReservoir;
                            }

                            reservoir.Add(e.Current);
                        }

                        // For each subsequent element in the source, randomly replace an element in the
                        // reservoir with a decreasing probability.
                        long totalElementsSeen = reservoir.Count;
                        while (e.MoveNext())
                        {
                            long r = Random.Shared.NextInt64(totalElementsSeen);
                            if (r < reservoir.Count)
                            {
                                reservoir[(int)r] = e.Current;
                            }
                        }
                    }
                }

            ReturnReservoir:
                if (reservoir is not null)
                {
                    // Ensure that elements in the reservoir are in random order. The sampling helped
                    // to ensure we got a uniform distribution from the source into the reservoir, but
                    // it didn't randomize the order of the reservoir itself; this is especially relevant
                    // to the elements initially added into the reservoir.
                    Random.Shared.Shuffle(CollectionsMarshal.AsSpan(reservoir));
                }

                return reservoir;
            }

            public override TSource[] ToArray() => SampleToList(_source, _takeCount)?.ToArray() ?? [];

            public override List<TSource> ToList() => SampleToList(_source, _takeCount) ?? [];

            public override int GetCount(bool onlyIfCheap) =>
                !onlyIfCheap ? Math.Min(_takeCount, _source.Count()) :
                TryGetNonEnumeratedCount(_source, out int count) ? Math.Min(_takeCount, count) :
                -1;

            public override TSource? TryGetFirst(out bool found) =>
                TryGetElementAt(0, out found);

            public override TSource? TryGetLast(out bool found) =>
                TryGetElementAt(0, out found);

            public override TSource? TryGetElementAt(int index, out bool found)
            {
                if (_source is Iterator<TSource> iterator &&
                    iterator.GetCount(onlyIfCheap: true) is int iteratorCount &&
                    iteratorCount >= 0)
                {
                    if ((uint)index < (uint)Math.Min(_takeCount, iteratorCount))
                    {
                        return iterator.TryGetElementAt(Random.Shared.Next(0, iteratorCount), out found);
                    }
                }
                else if (_source is IList<TSource> list)
                {
                    int count = list.Count;
                    if ((uint)index < (uint)Math.Min(_takeCount, count))
                    {
                        found = true;
                        return list[Random.Shared.Next(0, count)];
                    }
                }
                else if (index >= 0)
                {
                    TSource[] array = _source.ToArray();
                    if (index < Math.Min(_takeCount, array.Length))
                    {
                        found = true;
                        return array[Random.Shared.Next(0, array.Length)];
                    }
                }

                found = false;
                return default;
            }

            public override Iterator<TSource>? Take(int count) =>
                _takeCount <= count ? this : new TakeShuffleIterator<TSource>(_source, count);
        }
    }
}
