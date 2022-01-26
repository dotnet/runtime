// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

public class Runtime_64161
{
    // Just make sure it doesn't assert in Checked mode and does not return 0
    public static int Main() => Test() != 0 ? 100 : 101;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe ulong Test() => *(ulong*)(delegate*<void>)&VoidFunc;

    private static void VoidFunc() {}
}
