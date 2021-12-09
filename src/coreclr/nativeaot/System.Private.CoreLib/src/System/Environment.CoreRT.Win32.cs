// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System
{
    public static partial class Environment
    {
        [DoesNotReturn]
        private static void ExitRaw() => Interop.Kernel32.ExitProcess(s_latchedExitCode);
    }
}
