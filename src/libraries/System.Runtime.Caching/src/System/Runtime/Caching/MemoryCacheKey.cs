// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Caching
{
    internal class MemoryCacheKey
    {
        private string _key;
        private int _hash;

        internal int Hash { get { return _hash; } }
        internal string Key { get { return _key; } }

        internal MemoryCacheKey(string key)
        {
            _key = key;
            _hash = key.GetHashCode();
        }
    }
}
