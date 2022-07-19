// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

public class Runtime_66720
{
    public static int Main()
    {
        return Test(0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Test(in short zero)
    {
        // Fill arg stack slot with all ones
        LastArg(0, 0, 0, 0, 0, 0, 0, 0, -1);
        // Bug was that the last arg passed here would write only 16 bits
        // instead of 32 bits
        int last = LastArg(0, 0, 0, 0, 0, 0, 0, 0, zero);
        return last == 0 ? 100 : -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int LastArg(int a, int b, int c, int d, int e, int f, int g, int h, int i)
    {
        return i;
    }
}
