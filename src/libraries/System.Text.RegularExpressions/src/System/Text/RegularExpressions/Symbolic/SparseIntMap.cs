// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.RegularExpressions.Symbolic
{
    internal class SparseIntMap<T> where T : struct
    {
        private List<(int, T)> _dense = new();
        private int[] _sparse = new int[16];

        private void Grow(int maxValue)
        {
            int newLength = maxValue + 1;
            if (newLength <= _sparse.Length)
                return;
            newLength = Math.Max(16, Math.Max(2*_sparse.Length, newLength));
            Array.Resize(ref _sparse, newLength);
            // Using GC.AllocateUninitializedArray<int>(newLength) and copying would also be valid
        }

        public void Clear() => _dense.Clear();

        public int Count { get => _dense.Count; }

        public List<(int, T)> Values { get => _dense; }

        public int Find(int key)
        {
            if (key < _sparse.Length)
            {
                int idx = _sparse[key];
                if (idx < _dense.Count)
                {
                    var entry = _dense[idx];
                    Debug.Assert(entry.Item1 < _sparse.Length);
                    if (key == entry.Item1)
                    {
                        return idx;
                    }
                }
            }
            return -1;
        }

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
            _dense.Add((key, default(T)));
            return true;
        }

        public bool Add(int key, T value)
        {
            bool added = Add(key, out int index);
            Update(index, key, value);
            return added;
        }

        public void Update(int index, int key, T value)
        {
            Debug.Assert(0 <= index && index < _dense.Count);
            Debug.Assert(_dense[index].Item1 == key);
            _dense[index] = (key, value);
        }
    }
}
