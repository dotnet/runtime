// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class C1
{
    public sbyte F1;
    public byte F2;
}

public struct S0
{
    public ulong F0;
}

public class Program
{
    public static IRuntime s_rt;
    public static bool s_25;
    public static short[] s_42;
    public static S0[][] s_43 = new S0[][]{new S0[]{new S0()}};
    [Fact]
    public static int TestEntryPoint()
    {
        CollectibleALC alc = new CollectibleALC();
        System.Reflection.Assembly asm = alc.LoadFromAssemblyPath(System.Reflection.Assembly.GetExecutingAssembly().Location);
        System.Reflection.MethodInfo mi = asm.GetType(typeof(Program).FullName).GetMethod(nameof(MainInner));
        System.Type runtimeTy = asm.GetType(typeof(Runtime).FullName);
        mi.Invoke(null, new object[]{System.Activator.CreateInstance(runtimeTy)});
        return 100;
    }

#pragma warning disable xUnit1013
    public static void MainInner(IRuntime rt)
    {
        s_rt = rt;
        var vr3 = new C1();
        C1 vr8 = new C1();
        bool vr10 = vr8.F2 == vr8.F2;
        M6(vr3, vr10);
    }
#pragma warning restore xUnit1013

    internal static void M60(ref sbyte arg1, ref short[] arg2)
    {
    }

    internal static void M6(C1 argThis, bool arg0)
    {
        arg0 = s_25;
        M60(ref argThis.F1, ref s_42);
        if (arg0 && arg0)
        {
            throw new Exception();
        }
    }
}

public interface IRuntime
{
}

public class Runtime : IRuntime
{
}

public class CollectibleALC : System.Runtime.Loader.AssemblyLoadContext
{
    public CollectibleALC(): base(true)
    {
    }
}
