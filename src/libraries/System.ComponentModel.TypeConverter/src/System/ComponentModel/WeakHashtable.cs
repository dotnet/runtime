// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a hashtable-like collection that stores keys and values using weak references.
    /// This class is a thin wrapper around <see cref="ConditionalWeakTable{TKey, TValue}"/>
    /// and is especially useful for associating data with objects from unloadable assemblies.
    /// </summary>
    internal sealed class WeakHashtable : IEnumerable<KeyValuePair<object, object?>>
    {
        private readonly ConditionalWeakTable<object, object?> _hashtable = new ConditionalWeakTable<object, object?>();

        public object? this[object key]
        {
            get => _hashtable.TryGetValue(key, out object? value) ? value : null;
            set => _hashtable.AddOrUpdate(key, value);
        }

        public bool ContainsKey(object key) => _hashtable.TryGetValue(key, out object? _);

        public void Remove(object key) => _hashtable.Remove(key);

        public IEnumerator<KeyValuePair<object, object?>> GetEnumerator() => ((IEnumerable<KeyValuePair<object, object?>>)_hashtable).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<object, object?>>)_hashtable).GetEnumerator();
    }
}
