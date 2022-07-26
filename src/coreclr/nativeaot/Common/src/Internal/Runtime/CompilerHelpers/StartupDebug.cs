// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// <see cref="System.Diagnostics.Debug"/> replacement for the startup path.
    /// It's not safe to use the full-blown Debug class during startup because big chunks
    /// of the managed execution environment are not initialized yet.
    /// </summary>
    internal static class StartupDebug
    {
        [Conditional("DEBUG")]
        public static void Assert([DoesNotReturnIf(false)] bool condition)
        {
            if (!condition)
                unsafe { *(int*)0 = 0; }
        }
    }
}
