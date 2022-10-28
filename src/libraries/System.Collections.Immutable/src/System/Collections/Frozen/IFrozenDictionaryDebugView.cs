// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Immutable
{
    internal sealed class IFrozenDictionaryDebugView<TKey, TValue>
        where TKey : notnull
    {
        private readonly IFrozenDictionary<TKey, TValue> _dict;

        public IFrozenDictionaryDebugView(IFrozenDictionary<TKey, TValue> dictionary)
        {
            _dict = dictionary;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<TKey, TValue>[] Items => new List<KeyValuePair<TKey, TValue>>(_dict).ToArray();
    }
}
