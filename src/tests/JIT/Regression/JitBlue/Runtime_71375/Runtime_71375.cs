// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_71375
{
    [Fact]
    public static void TestEntryPoint()
    {
        // At the time of writing this test, the calling convention for incoming vector parameters on
        // Windows ARM64 was broken, so only the fact that "Problem" compiled without asserts was
        // checked. If/once the above is fixed, this test should be changed to actually call "Problem".
        RuntimeHelpers.PrepareMethod(typeof(Runtime_71375).GetMethod("Problem").MethodHandle);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int VarArgs(int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, Vector128<int> splitArg, __arglist) => splitArg.GetElement(0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Problem()
    {
        return VarArgs(0, 0, 0, 0, 0, 0, Vector128<int>.AllBitsSet, __arglist()) != -1;
    }

    [Fact]
    public static int TestEntryPoint2()
    {
        if (Case2())
            return 101;
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int VarArgs2(int arg1, int arg2, int arg3, int arg4, int arg5, Vector128<int> vecArg, __arglist) => vecArg.GetElement(0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Case2()
    {
        // vector is not split: it is passed in registers x6, x7
        return VarArgs2(0, 0, 0, 0, 0, Vector128<int>.AllBitsSet, __arglist()) != -1;
    }

    [Fact]
    public static int TestEntryPoint3()
    {
        if (Case3())
            return 101;
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int VarArgs3(int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, int arg7, Vector128<int> vecArg, __arglist) => vecArg.GetElement(0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Case3()
    {
        // vector is not split: it is passed entirely on the stack
        return VarArgs3(0, 0, 0, 0, 0, 0, 0, Vector128<int>.AllBitsSet, __arglist()) != -1;
    }

    [Fact]
    public static int TestEntryPoint4()
    {
        if (Case4())
            return 101;
        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int VarArgs4(int arg1, object arg2, int arg3, object arg4, int arg5, object arg6, Vector128<int> splitArg, __arglist) => splitArg.GetElement(0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Case4()
    {
        // Spit the vector but also pass some object types so the GC needs to know about them.
        return VarArgs4(0, new object(), 0, new object(), 0, new object(), Vector128<int>.AllBitsSet, __arglist(new object(), 1, new object())) != -1;
    }
}
