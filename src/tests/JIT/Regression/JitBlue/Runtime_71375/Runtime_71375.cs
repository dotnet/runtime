// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_71375
{
    [Fact]
    public static int TestEntryPoint()
    {
        // At the time of writing this test, the calling convention for incoming vector parameters on
        // Windows ARM64 was broken, so only the fact that "Problem" compiled without asserts was
        // checked. If/once the above is fixed, this test should be changed to actually call "Problem".
        RuntimeHelpers.PrepareMethod(typeof(Runtime_71375).GetMethod("Problem").MethodHandle);

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int VarArgs(int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, Vector128<int> splitArg, __arglist) => splitArg.GetElement(0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Problem()
    {
        return VarArgs(0, 0, 0, 0, 0, 0, Vector128<int>.AllBitsSet, __arglist()) != -1;
    }
}
