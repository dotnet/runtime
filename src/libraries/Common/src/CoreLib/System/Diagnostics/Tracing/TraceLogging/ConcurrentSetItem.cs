// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// TraceLogging: Abstract base class that must be inherited by items in a
    /// ConcurrentSet.
    /// </summary>
    /// <typeparam name="KeyType">Type of the set's key.</typeparam>
    /// <typeparam name="ItemType">Type of the derived class.</typeparam>
    internal abstract class ConcurrentSetItem<KeyType, ItemType>
        where ItemType : ConcurrentSetItem<KeyType, ItemType>
    {
        public abstract int Compare(ItemType other);
        public abstract int Compare(KeyType key);
    }
}
