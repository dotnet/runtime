// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

public class Runtime_68479
{
    public static int Main()
    {
        return Problem(new Class(), 1, 1) == 1 ? 100 : 101;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Problem(Class a, long b, long c)
    {
        c = 17;
        BlockFwdSub();
        Use(ref a.Field, a.Field, b % c);

        return a.Field;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void BlockFwdSub() { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Use(ref int a, long b, long c) { a = 1; }

    class Class
    {
        public int Field;
    }
}
