// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILCompiler.Reflection.ReadyToRun;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    /// <summary>
    /// A simple key-value map that does not support updates and that supports range queries.
    /// </summary>
    /// <typeparam name="TValue">The type of values.</typeparam>
    public class KeyValueMap<TKey, TValue> where TKey : IComparable<TKey>
    {
        // Native offsets in order
        private TKey[] _keys;
        private TValue[] _values;

        public KeyValueMap(TKey[] keys, TValue[] values)
        {
            Trace.Assert(keys.Length == values.Length);

            _keys = keys;
            _values = values;
        }

        // Find last index of a key that is smaller than the specified input key.
        private int LookupIndex(TKey key)
        {
            int index = Array.BinarySearch(_keys, key);
            if (index < 0)
                index = ~index - 1;

            // If rva is before first binary search will return ~0 so index will be -1.
            if (index < 0)
                return -1;

            return index;
        }

        public bool TryLookup(TKey key, out TValue value)
        {
            int index = LookupIndex(key);
            if (index == -1)
            {
                value = default;
                return false;
            }

            value = _values[index];
            return true;
        }

        public IEnumerable<TValue> LookupRange(TKey min, TKey max)
        {
            Debug.Assert(min.CompareTo(max) <= 0);

            int start = LookupIndex(min);
            if (start < 0)
                start = 0;

            int end = LookupIndex(max);
            if (end < 0)
                yield break;

            for (int i = start; i <= end; i++)
                yield return _values[i];
        }
    }
}
