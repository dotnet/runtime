// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Warning: Do not port this code to Desktop!

// Desktop has its own implementation.
// The difference between those implementations is caused by randomized hashing feature.
// On Desktop randomized hashing for strings is disabled by default (it is on only for collections)
// On CoreClr platforms randomized hashing is enabled by default meaning that we can use standard
// string.GetHashCode implementation.

using System;
using System.Collections.Generic;

namespace System.Xml
{
    // The SecureStringHasher implements IEqualityComparer for strings and therefore can be used in generic IDictionary.
    internal class SecureStringHasher : IEqualityComparer<String>
    {
        public bool Equals(String x, String y)
        {
            return String.Equals(x, y, StringComparison.Ordinal);
        }

        public int GetHashCode(String key)
        {
            return key.GetHashCode();
        }
    }
}