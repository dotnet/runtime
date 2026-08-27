// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1416 // Interop is only supported on Windows; this file is compiled only for Windows.

using System.Diagnostics.CodeAnalysis;

namespace System
{
    public static partial class Environment
    {
        [DoesNotReturn]
        private static void ExitRaw() => Interop.Kernel32.ExitProcess(s_latchedExitCode);
    }
}
