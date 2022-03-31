// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Tests
{
    public static class Shuffler
    {
        public static T[] Shuffle<T>(T[] array)
        {
            var r = new Random(42);
            int i = array.Length;
            while (i > 1)
            {
                int j = r.Next(i--);
                (array[i], array[j]) = (array[j], array[i]);
            }
            return array;
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, int seed)
        {
            return new ShuffledEnumerable<T>(source, seed);
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            return new ShuffledEnumerable<T>(source);
        }

        private class ShuffledEnumerable<T> : IEnumerable<T>
        {
            private IEnumerable<T> _source;
            private int? _seed;

            public ShuffledEnumerable(IEnumerable<T> source)
            {
                _source = source;
            }

            public ShuffledEnumerable(IEnumerable<T> source, int seed)
                : this(source)
            {
                _seed = seed;
            }

            public IEnumerator<T> GetEnumerator()
            {
                Random rnd = new Random(_seed.HasValue ? _seed.GetValueOrDefault() : 42);
                T[] array = _source.ToArray();
                int count = array.Length;
                for (int i = array.Length - 1; i > 0; --i)
                {
                    int j = rnd.Next(0, i + 1);
                    if (i != j)
                    {
                        T swapped = array[i];
                        array[i] = array[j];
                        array[j] = swapped;
                    }
                }
                return ((IEnumerable<T>)array).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
