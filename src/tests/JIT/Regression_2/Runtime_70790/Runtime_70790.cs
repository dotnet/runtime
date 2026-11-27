// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_70790
{
    private static readonly nint s_intType = typeof(int).TypeHandle.Value;

    [Fact]
    public static int TestEntryPoint()
    {
        RuntimeHelpers.RunClassConstructor(typeof(Runtime_70790).TypeHandle);

        object a = 1u;
        object b = 2u;
        if (Problem(a, b))
        {
            return 101;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Problem(object a, object b)
    {
        if (a.GetType() == typeof(int))
        {
            return true;
        }

        JitUse(b.GetType() == typeof(int));
        JitUse(s_intType - 300);

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void JitUse<T>(T arg) { }
}
