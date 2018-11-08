// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Threading;

namespace System.Reflection.TypeLoading.Ecma
{
    /// <summary>
    /// Thread-safe interning table for objects that map 1-1 with ECMA tokens.
    /// 
    /// The key type is hard-coded to EntityHandle.
    /// The "T" type is the value type (e.g. RoTypeDefinition objects)
    /// The "C" type is an optional context value passed through the factory methods (so we don't to allocate a closure each time.)
    /// </summary>
    internal sealed class MetadataTable<T, C>
        where T : class
    {
        private readonly T[] _table;

        public MetadataTable(int count)
        {
            Count = count;
            _table = new T[count];
        }

        public T GetOrAdd(EntityHandle handle, C context, Func<EntityHandle, C, T> factory)
        {
            Debug.Assert(!handle.IsNil);
            Debug.Assert(factory != null);

            int index = handle.GetToken().GetTokenRowNumber() - 1;
            T[] table = _table;
            T result = Volatile.Read(ref table[index]);
            if (result != null)
                return result;
            T newValue = factory(handle, context);
            return Interlocked.CompareExchange(ref table[index], newValue, null) ?? newValue;
        }

        public int Count { get; }

        /// <summary>
        /// Return a read-only enumeration of the table (safe to hand back to app code.)
        /// </summary>
        public IEnumerable<T> EnumerateValues(int skip = 0)
        {
            for (int i = skip; i < _table.Length; i++)
            {
                yield return _table[i];
            }
        }

        /// <summary>
        /// Return a newly allocated array containing the contents (safe to hand back to app code.)
        /// </summary>
        public TOut[] ToArray<TOut>(int skip = 0)
        {
            TOut[] newArray = new TOut[Count - skip];
            Array.Copy(_table, skip, newArray, 0, newArray.Length);
            return newArray;
        }
    }
}
