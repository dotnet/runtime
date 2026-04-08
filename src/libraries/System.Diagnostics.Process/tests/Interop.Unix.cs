// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    [DllImport("libc")]
    internal static extern int getsid(int pid);

    [DllImport("libc", SetLastError = true)]
    internal static extern int kill(int pid, int sig);
}
