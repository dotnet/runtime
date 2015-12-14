// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace GCPerfTestFramework.Metrics.Builders
{
    internal class CircularBuffer<T> : IEnumerable<T>
        where T : class
    {
        private int StartIndex, AfterEndIndex, Size;
        private T[] Items;
        public CircularBuffer(int size)
        {
            if (size < 1)
                throw new ArgumentException("size");

            StartIndex = 0;
            AfterEndIndex = 0;
            Size = size + 1;
            Items = new T[Size];
        }

        public void Add(T item)
        {
            if (Next(AfterEndIndex) == StartIndex)
            {
                Items[StartIndex] = null;
                StartIndex = Next(StartIndex);
            }
            Items[AfterEndIndex] = item;
            AfterEndIndex = Next(AfterEndIndex);
        }

        private int Next(int i)
        {
            return (i == Size - 1) ? 0 : i + 1;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = StartIndex; i != AfterEndIndex; i = Next(i))
            {
                yield return Items[i];
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
