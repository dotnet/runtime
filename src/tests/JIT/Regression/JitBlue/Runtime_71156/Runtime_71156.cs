// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

public unsafe class Runtime_71156
{
    public static int Main()
    {
        return Problem() ? 101 : 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Problem()
    {
        float a;
        var b = 0xFF000000;
        Unsafe.CopyBlock(&a, &b, 4);

        return *(uint*)&a != 0xFF000000u;
    }
}
