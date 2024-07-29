// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

public static class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        // Verify crst levels with GCs triggered during R2R code lookup in the Prestub on the main thread, during which dynamic
        // code from a background thread is deleted at the start of the GC in the main thread

        var t = new Thread(() =>
        {
            for (uint i = 0; ; ++i)
            {
                DynamicMethod dynamicMethod = CreateDynamicMethod($"DynMethod{i}");
                var dynamicMethodDelegate = (Action)dynamicMethod.CreateDelegate(typeof(Action));
                dynamicMethodDelegate();
            }
        });
        t.IsBackground = true;
        t.Start();

        for (int i = 0; i < 100; ++i)
        {
            typeof(Program).InvokeMember(
                $"Func{i}",
                BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                null,
                Array.Empty<object>());
        }
    }

    private static DynamicMethod CreateDynamicMethod(string name)
    {
        var dynamicMethod = new DynamicMethod(name, null, null);
        ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
        ilGenerator.Emit(OpCodes.Ret);
        return dynamicMethod;
    }

    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func0() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func1() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func2() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func3() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func4() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func5() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func6() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func7() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func8() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func9() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func10() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func11() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func12() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func13() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func14() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func15() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func16() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func17() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func18() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func19() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func20() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func21() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func22() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func23() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func24() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func25() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func26() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func27() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func28() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func29() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func30() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func31() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func32() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func33() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func34() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func35() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func36() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func37() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func38() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func39() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func40() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func41() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func42() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func43() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func44() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func45() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func46() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func47() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func48() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func49() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func50() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func51() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func52() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func53() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func54() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func55() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func56() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func57() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func58() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func59() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func60() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func61() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func62() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func63() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func64() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func65() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func66() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func67() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func68() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func69() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func70() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func71() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func72() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func73() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func74() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func75() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func76() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func77() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func78() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func79() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func80() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func81() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func82() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func83() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func84() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func85() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func86() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func87() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func88() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func89() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func90() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func91() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func92() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func93() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func94() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func95() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func96() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func97() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func98() {}
    [MethodImpl(MethodImplOptions.NoInlining)] private static void Func99() {}
}
