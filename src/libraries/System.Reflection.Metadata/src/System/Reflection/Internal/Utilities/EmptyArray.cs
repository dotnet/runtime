// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.Internal
{
    internal static class EmptyArray<T>
    {
#pragma warning disable CA1825 // Array.Empty<T>() doesn't exist in all configurations
        internal static readonly T[] Instance = new T[0];
#pragma warning restore CA1825
    }
}
