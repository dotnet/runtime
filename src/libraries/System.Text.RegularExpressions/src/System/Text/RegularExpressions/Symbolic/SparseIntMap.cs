// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>An insertion-ordered map that supports small int keys.</summary>
    /// <remarks>Uses a sparse array of the same size as the space of keys for efficient lookups.</remarks>
    internal sealed class SparseIntMap<T> where T : struct
    {
        private const int InitialCapacity = 16;
        private readonly List<KeyValuePair<int, T>> _dense = new();
        private int[] _sparse = new int[InitialCapacity];

        private void Grow(int maxValue)
        {
            int newLength = maxValue + 1;
            Debug.Assert(newLength > _sparse.Length);
            // At least double the size to amortize memory allocations
            newLength = Math.Max(2 * _sparse.Length, newLength);
            Array.Resize(ref _sparse, newLength);
            // Using GC.AllocateUninitializedArray<int>(newLength) and copying would also be valid
        }

        /// <summary>Remove all entries. Does not decrease capacity.</summary>
        public void Clear() => _dense.Clear();

        /// <summary>Number of entries in the map.</summary>
        public int Count => _dense.Count;

        /// <summary>Get the internal list of entries. Do not modify.</summary>
        public List<KeyValuePair<int, T>> Values => _dense;

        /// <summary>Return the index of the entry with the key, or -1 if none exists.</summary>
        public int Find(int key)
        {
            int[] sparse = _sparse;
            if ((uint)key < (uint)sparse.Length)
            {
                List<KeyValuePair<int, T>> dense = _dense;
                int idx = sparse[key];
                if (idx < dense.Count)
                {
                    int entryKey = dense[idx].Key;
                    Debug.Assert(entryKey < _sparse.Length);
                    if (key == entryKey)
                    {
                        return idx;
                    }
                }
            }

            return -1;
        }

        /// <summary>Find or add an entry matching the key.</summary>
        /// <param name="key">the key to find or add</param>
        /// <param name="index">will be set to the index of the matching entry</param>
        /// <returns>whether a new entry was added</returns>
        public bool Add(int key, out int index)
        {
            index = Find(key);
            if (index >= 0)
            {
                return false;
            }
            if (key >= _sparse.Length)
            {
                Grow(key);
            }
            index = _dense.Count;
            _sparse[key] = index;
            _dense.Add(new KeyValuePair<int, T>(key, default(T)));
            return true;
        }

        /// <summary>Find or add an entry matching a key and then set the value of the entry.</summary>
        public bool Add(int key, T value)
        {
            bool added = Add(key, out int index);
            Update(index, key, value);
            return added;
        }

        /// <summary>Set the value of the entry at the index.</summary>
        public void Update(int index, int key, T value)
        {
            Debug.Assert(0 <= index && index < _dense.Count);
            Debug.Assert(_dense[index].Key == key);
            _dense[index] = new KeyValuePair<int, T>(key, value);
        }
    }
}
