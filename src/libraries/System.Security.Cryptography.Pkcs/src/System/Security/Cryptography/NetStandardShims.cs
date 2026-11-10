// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Security.Cryptography
{
#if !NET && !NETSTANDARD2_1_OR_GREATER
    internal static class CryptographicOperations
    {
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        internal static void ZeroMemory(Span<byte> buffer)
        {
            buffer.Clear();
        }
    }
#endif
}
