// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.CompilerServices;

namespace System.Diagnostics
{
    internal static class Debug
    {
        [System.Diagnostics.Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                RuntimeImports.RhpFallbackFailFast();
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Assert(bool condition)
        {
            if (!condition)
            {
                RuntimeImports.RhpFallbackFailFast();
            }
        }
    }
}
