// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// GrowingArray.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A growing array. Unlike List{T}, it makes the internal array available to its user.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class GrowingArray<T>
    {
        private T[] _array;
        private int _count;
        private const int DEFAULT_ARRAY_SIZE = 1024;

        internal GrowingArray()
        {
            _array = new T[DEFAULT_ARRAY_SIZE];
            _count = 0;
        }

        //---------------------------------------------------------------------------------------
        // Returns the internal array representing the list. Note that the array may be larger
        // than necessary to hold all elements in the list.
        //

        internal T[] InternalArray
        {
            get { return _array; }
        }

        internal int Count
        {
            get { return _count; }
        }

        internal void Add(T element)
        {
            if (_count >= _array.Length)
            {
                GrowArray(2 * _array.Length);
            }
            _array[_count++] = element;
        }

        private void GrowArray(int newSize)
        {
            Debug.Assert(newSize > _array.Length);

            T[] array2 = new T[newSize];
            _array.CopyTo(array2, 0);
            _array = array2;
        }

        internal void CopyFrom(T[] otherArray, int otherCount)
        {
            // Ensure there is just enough room for both.
            if (_count + otherCount > _array.Length)
            {
                GrowArray(_count + otherCount);
            }

            // And now just blit the keys directly.
            Array.Copy(otherArray, 0, _array, _count, otherCount);
            _count += otherCount;
        }
    }
}
