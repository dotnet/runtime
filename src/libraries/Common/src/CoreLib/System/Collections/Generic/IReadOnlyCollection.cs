// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    // Provides a read-only, covariant view of a generic list.
    public interface IReadOnlyCollection<out T> : IEnumerable<T>
    {
        int Count { get; }
    }
}
