// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Collections
{
    public interface IStructuralEquatable
    {
        bool Equals(object? other, IEqualityComparer comparer);
        int GetHashCode(IEqualityComparer comparer);
    }
}
