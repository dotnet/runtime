// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;

public class Runtime_71375
{
    public static int Main()
    {
        return Problem() ? 101 : 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int VarArgs(int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, Vector128<int> splitArg, __arglist) => splitArg.GetElement(0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Problem()
    {
        return VarArgs(0, 0, 0, 0, 0, 0, Vector128<int>.AllBitsSet, __arglist()) != -1;
    }
}
