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
                    List<TSource>? sample = ShuffleTakeIterator<TSource>.SampleToList(_source, 1, out long totalElementCount);
                    if (sample is not null && index < totalElementCount)
                    {
                        found = true;
                        return sample[0];
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
                return new ShuffleTakeIterator<TSource>(_source, count);
            }
        }

        private sealed partial class ShuffleTakeIterator<TSource> : Iterator<TSource>
        {
            private readonly IEnumerable<TSource> _source;
            private readonly int _takeCount;
            private List<TSource>? _buffer;

            public ShuffleTakeIterator(IEnumerable<TSource> source, int takeCount)
            {
                Debug.Assert(source is not null);
                Debug.Assert(takeCount > 0);

                _source = source;
                _takeCount = takeCount;
            }

            private protected override Iterator<TSource> Clone() => new ShuffleTakeIterator<TSource>(_source, _takeCount);

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
                    List<TSource>? buffer = SampleToList(_source, _takeCount, out _);
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

            public override TSource[] ToArray() => SampleToList(_source, _takeCount, out _)?.ToArray() ?? [];

            public override List<TSource> ToList() => SampleToList(_source, _takeCount, out _) ?? [];

            public override int GetCount(bool onlyIfCheap) =>
                TryGetNonEnumeratedCount(_source, out int count) ? Math.Min(_takeCount, count) :
                !onlyIfCheap ? Math.Min(_takeCount, _source.Take(_takeCount).Count()) :
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
                    List<TSource>? sample = SampleToList(_source, 1, out long totalElementCount);
                    if (sample is not null && index < Math.Min(_takeCount, totalElementCount))
                    {
                        found = true;
                        return sample[0];
                    }
                }

                found = false;
                return default;
            }

            public override Iterator<TSource>? Take(int count) =>
                _takeCount <= count ? this : new ShuffleTakeIterator<TSource>(_source, count);

            /// <summary>Uses reservoir sampling to randomly select <paramref name="takeCount"/> elements from <paramref name="source"/>.</summary>
            internal static List<TSource>? SampleToList(IEnumerable<TSource> source, int takeCount, out long totalElementCount)
            {
                List<TSource>? reservoir = null;

                if (source is IList<TSource> list)
                {
                    int listCount = list.Count;
                    Debug.Assert(listCount > takeCount, "Known listCount <= takeCount should have been handled by Iterator.Take override");

                    reservoir = new(takeCount);

                    // Fill the reservoir with the first takeCount elements from the source.
                    for (int i = 0; i < takeCount; i++)
                    {
                        reservoir.Add(list[i]);
                    }

                    // For each subsequent element in the source, randomly replace an element in the
                    // reservoir with a decreasing probability.
                    for (int i = takeCount; i < listCount; i++)
                    {
                        int r = Random.Shared.Next(i + 1);
                        if (r < takeCount)
                        {
                            reservoir[r] = list[i];
                        }
                    }

                    totalElementCount = listCount;
                }
                else
                {
                    using IEnumerator<TSource> e = source.GetEnumerator();
                    if (e.MoveNext())
                    {
                        // Fill the reservoir with the first takeCount elements from the source.
                        // If we can't fill it, just return what we get.
                        reservoir = new List<TSource>(Math.Min(takeCount, 4)) { e.Current };
                        while (reservoir.Count < takeCount)
                        {
                            if (!e.MoveNext())
                            {
                                totalElementCount = reservoir.Count;
                                goto ReturnReservoir;
                            }

                            reservoir.Add(e.Current);
                        }

                        // For each subsequent element in the source, randomly replace an element in the
                        // reservoir with a decreasing probability.
                        long i = takeCount;
                        while (e.MoveNext())
                        {
                            i++;
                            long r = Random.Shared.NextInt64(i);
                            if (r < takeCount)
                            {
                                reservoir[(int)r] = e.Current;
                            }
                        }

                        totalElementCount = i;
                    }
                    else
                    {
                        totalElementCount = 0;
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
        }
    }
}
