// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_116212
{
    [Fact]
    public static void Test() => Problem(1, null);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SideEffect(int x = 0) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Problem(int a, object? o)
    {
        if (o == null)
        {
            if (a == 0)
            {
                SideEffect();
            }

            if (a == 0)
            {
                o = new object();
            }
        }

        object? oo = o;

        if (oo != null)
        {
            SideEffect(oo.GetHashCode());
            return true;
        }

        return false;
    }
}
