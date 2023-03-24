// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace System.Collections.Frozen
{
    /// <summary>
    /// Stubs to isolate the frozen collection code from the internal details of ImmutableArray
    /// </summary>
    /// <remarks>
    /// This is intended to make it easier to use the frozen collections in environments/conditions
    /// when only the public API of ImmutableArray is available.
    /// </remarks>
    internal static class ImmutableArrayFactory
    {
        public static ImmutableArray<T> Create<T>(T[] array) => new ImmutableArray<T>(array);
    }
}
