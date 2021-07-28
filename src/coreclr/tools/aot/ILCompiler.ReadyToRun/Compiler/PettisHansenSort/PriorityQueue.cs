// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILCompiler.PettisHansenSort
{
    public class PriorityQueue<T>
    {
        private const int DefaultCapacity = 4;

        private static readonly T[] s_emptyArray = new T[0];

        private readonly IComparer<T> _comparer;
        private T[] _items;

        public PriorityQueue(int capacity = 0, IComparer<T> comparer = null)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            _items = capacity == 0 ? s_emptyArray : new T[capacity];
            _comparer = comparer ?? Comparer<T>.Default;
        }

        public T this[int index]
        {
            get
            {
                if (index < Count)
                    return _items[index];

                throw new IndexOutOfRangeException();
            }
        }

        public int Capacity
        {
            get { return _items.Length; }
            set
            {
                if (value < Count)
                    throw new ArgumentOutOfRangeException(nameof(value));

                Resize(value);
            }
        }

        public int Count { get; private set; }

        public void Add(T item)
        {
            if (Count == Capacity)
                Expand();

            BubbleUp(Count, item);
            Count++;
        }

        public T ExtractMax()
        {
            if (Count <= 0)
                throw new InvalidOperationException("The priority queue is empty");

            T min = _items[0];
            Count--;
            if (Count > 0)
            {
                // Move last one to root
                BubbleDown(0, _items[Count]);
            }

            _items[Count] = default(T);
            return min;
        }

        public void Replace(int index, T newValue)
        {
            if (index < 0 || index >= Count)
                throw new IndexOutOfRangeException();

            int comparison = _comparer.Compare(newValue, _items[index]);

            // If the element we are replacing with is larger than what was there,
            // it might also be larger than the parent. In that case we should bubble up.
            // Otherwise it might be smaller than children; bubble down in that case.
            if (comparison > 0)
                BubbleUp(index, newValue);
            else if (comparison < 0)
                BubbleDown(index, newValue);
        }

        private void Expand()
        {
            int newCapacity = Math.Max(Count * 2, DefaultCapacity);
            Resize(newCapacity);
        }

        private void Resize(int newCapacity)
        {
            if (newCapacity == _items.Length)
                return;

            Debug.Assert(newCapacity > _items.Length);

            if (newCapacity == 0)
            {
                _items = s_emptyArray;
                return;
            }

            Array.Resize(ref _items, newCapacity);
        }

        private void BubbleUp(int index, T value)
        {
            // Parent is always at floor((index - 1) / 2)
            while (index != 0)
            {
                int parent = (index - 1) / 2;

                if (_comparer.Compare(value, _items[parent]) <= 0)
                    break; // Item is smaller than parent

                _items[index] = _items[parent];
                index = parent;
            }

            _items[index] = value;
        }

        private void BubbleDown(int index, T value)
        {
            // Children are at index * 2 + 1 and index * 2 + 2
            while (true)
            {
                int left = index * 2 + 1;
                if (left >= Count)
                    break;

                int right = left + 1;
                int largestChild = left;

                if (right < Count && _comparer.Compare(_items[right], _items[left]) > 0)
                    largestChild = right;

                if (_comparer.Compare(value, _items[largestChild]) >= 0)
                    break; // Item is larger than both children

                // Child is larger than item
                _items[index] = _items[largestChild];
                index = largestChild;
            }

            _items[index] = value;
        }
    }
}
