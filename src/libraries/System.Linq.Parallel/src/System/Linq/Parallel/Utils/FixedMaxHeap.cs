// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// FixedMaxHeap.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Very simple heap data structure, of fixed size.
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    internal class FixedMaxHeap<TElement>
    {
        private TElement[] _elements; // Element array.
        private int _count; // Current count.
        private IComparer<TElement> _comparer; // Element comparison routine.

        //-----------------------------------------------------------------------------------
        // Create a new heap with the specified maximum size.
        //

        internal FixedMaxHeap(int maximumSize)
            : this(maximumSize, Util.GetDefaultComparer<TElement>())
        {
        }

        internal FixedMaxHeap(int maximumSize, IComparer<TElement> comparer)
        {
            Debug.Assert(comparer != null);

            _elements = new TElement[maximumSize];
            _comparer = comparer;
        }

        //-----------------------------------------------------------------------------------
        // Retrieve the count (i.e. how many elements are in the heap).
        //

        internal int Count
        {
            get { return _count; }
        }

        //-----------------------------------------------------------------------------------
        // Retrieve the size (i.e. the maximum size of the heap).
        //

        internal int Size
        {
            get { return _elements.Length; }
        }

        //-----------------------------------------------------------------------------------
        // Get the current maximum value in the max-heap.
        //
        // Note: The heap stores the maximumSize smallest elements that were inserted.
        // So, if the heap is full, the value returned is the maximumSize-th smallest
        // element that was inserted into the heap.
        //

        internal TElement MaxValue
        {
            get
            {
                if (_count == 0)
                {
                    throw new InvalidOperationException(SR.NoElements);
                }

                // The maximum element is in the 0th position.
                return _elements[0];
            }
        }


        //-----------------------------------------------------------------------------------
        // Removes all elements from the heap.
        //

        internal void Clear()
        {
            _count = 0;
        }

        //-----------------------------------------------------------------------------------
        // Inserts the new element, maintaining the heap property.
        //
        // Return Value:
        //     If the element is greater than the current max element, this function returns
        //     false without modifying the heap. Otherwise, it returns true.
        //

        internal bool Insert(TElement e)
        {
            if (_count < _elements.Length)
            {
                // There is room. We can add it and then max-heapify.
                _elements[_count] = e;
                _count++;
                HeapifyLastLeaf();
                return true;
            }
            else
            {
                // No more room. The element might not even fit in the heap. The check
                // is simple: if it's greater than the maximum element, then it can't be
                // inserted. Otherwise, we replace the head with it and reheapify.
                if (_comparer.Compare(e, _elements[0]) < 0)
                {
                    _elements[0] = e;
                    HeapifyRoot();
                    return true;
                }

                return false;
            }
        }

        //-----------------------------------------------------------------------------------
        // Replaces the maximum value in the heap with the user-provided value, and restores
        // the heap property.
        //

        internal void ReplaceMax(TElement newValue)
        {
            Debug.Assert(_count > 0);
            _elements[0] = newValue;
            HeapifyRoot();
        }

        //-----------------------------------------------------------------------------------
        // Removes the maximum value from the heap, and restores the heap property.
        //

        internal void RemoveMax()
        {
            Debug.Assert(_count > 0);
            _count--;

            if (_count > 0)
            {
                _elements[0] = _elements[_count];
                HeapifyRoot();
            }
        }

        //-----------------------------------------------------------------------------------
        // Private helpers to swap elements, and to reheapify starting from the root or
        // from a leaf element, depending on what is needed.
        //

        private void Swap(int i, int j)
        {
            TElement tmpElement = _elements[i];
            _elements[i] = _elements[j];
            _elements[j] = tmpElement;
        }

        private void HeapifyRoot()
        {
            // We are heapifying from the head of the list.
            int i = 0;
            int n = _count;

            while (i < n)
            {
                // Calculate the current child node indexes.
                int n0 = ((i + 1) * 2) - 1;
                int n1 = n0 + 1;

                if (n0 < n && _comparer.Compare(_elements[i], _elements[n0]) < 0)
                {
                    // We have to select the bigger of the two subtrees, and float
                    // the current element down. This maintains the max-heap property.
                    if (n1 < n && _comparer.Compare(_elements[n0], _elements[n1]) < 0)
                    {
                        Swap(i, n1);
                        i = n1;
                    }
                    else
                    {
                        Swap(i, n0);
                        i = n0;
                    }
                }
                else if (n1 < n && _comparer.Compare(_elements[i], _elements[n1]) < 0)
                {
                    // Float down the "right" subtree. We needn't compare this subtree
                    // to the "left", because if the element was smaller than that, the
                    // first if statement's predicate would have evaluated to true.
                    Swap(i, n1);
                    i = n1;
                }
                else
                {
                    // Else, the current key is in its final position. Break out
                    // of the current loop and return.
                    break;
                }
            }
        }

        private void HeapifyLastLeaf()
        {
            int i = _count - 1;
            while (i > 0)
            {
                int j = ((i + 1) / 2) - 1;

                if (_comparer.Compare(_elements[i], _elements[j]) > 0)
                {
                    Swap(i, j);
                    i = j;
                }
                else
                {
                    break;
                }
            }
        }
    }
}
