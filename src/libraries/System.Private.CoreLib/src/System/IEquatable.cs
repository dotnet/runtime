// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System
{
    public interface IEquatable<T> // invariant due to questionable semantics around equality and inheritance
    {
        bool Equals(T? other);
    }
}
