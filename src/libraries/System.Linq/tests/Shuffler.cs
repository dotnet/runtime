// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Tests
{
    public static class Shuffler
    {
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
                Random rnd = _seed.HasValue ? new Random(_seed.GetValueOrDefault()) : new Random();
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
