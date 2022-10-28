// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Immutable
{
    internal sealed class IFrozenOrdinalStringDictionaryDebugView<TValue>
    {
        private readonly IFrozenDictionary<string, TValue> _dict;

        public IFrozenOrdinalStringDictionaryDebugView(IFrozenDictionary<string, TValue> dictionary)
        {
            _dict = dictionary;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<string, TValue>[] Items => new List<KeyValuePair<string, TValue>>(_dict).ToArray();
    }
}
