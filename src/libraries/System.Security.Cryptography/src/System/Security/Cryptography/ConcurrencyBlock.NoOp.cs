// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The shape needs to match the block version.
#pragma warning disable CA1822
#pragma warning disable IDE0060

namespace System.Security.Cryptography
{
    internal struct ConcurrencyBlock
    {
        internal static Scope Enter(ref ConcurrencyBlock block) => default;

        internal ref struct Scope
        {
            internal void Dispose()
            {
            }
        }
    }
}
