// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Security.Cryptography
{
    internal struct ConcurrencyBlock
    {
        internal static Scope Enter(ref ConcurrencyBlock block)
        {
            _ = block;
            return default;
        }

        internal ref struct Scope
        {
#pragma warning disable CA1822 // Member can be marked static
            internal void Dispose()
            {
            }
#pragma warning restore CA1822
        }
    }
}
