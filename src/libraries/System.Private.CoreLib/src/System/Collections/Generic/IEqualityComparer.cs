// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic
{
    // The generic IEqualityComparer interface implements methods to check if two objects are equal
    // and generate Hashcode for an object.
    // It is used in Dictionary class.
    public interface IEqualityComparer<in T>
    {
        bool Equals(T? x, T? y);
        int GetHashCode([DisallowNull] T obj);
    }
}
