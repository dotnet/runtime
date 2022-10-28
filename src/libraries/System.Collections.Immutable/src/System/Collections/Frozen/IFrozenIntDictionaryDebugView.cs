// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Immutable
{
    internal sealed class IFrozenIntDictionaryDebugView<TValue>
    {
        private readonly IFrozenDictionary<int, TValue> _dict;

        public IFrozenIntDictionaryDebugView(IFrozenDictionary<int, TValue> dictionary)
        {
            _dict = dictionary;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<int, TValue>[] Items => new List<KeyValuePair<int, TValue>>(_dict).ToArray();
    }
}
