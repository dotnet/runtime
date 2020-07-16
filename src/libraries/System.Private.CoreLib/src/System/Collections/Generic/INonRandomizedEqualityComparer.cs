// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections.Generic
{
    internal interface INonRandomizedEqualityComparer<in T> : IEqualityComparer<T>
    {
        IEqualityComparer<T> GetComparerForSerialization() => GetRandomizedComparer();

        IEqualityComparer<T> GetRandomizedComparer();
    }
}
