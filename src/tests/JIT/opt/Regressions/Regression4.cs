// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    public static IRuntime s_rt;
    public static int s_58;
    public static uint s_66;
    public static byte[] s_126 = new byte[] { 0 };
    [Fact]
    public static int TestEntryPoint()
    {
        CollectibleALC alc = new CollectibleALC();
        System.Reflection.Assembly asm = alc.LoadFromAssemblyPath(System.Reflection.Assembly.GetExecutingAssembly().Location);
        System.Reflection.MethodInfo mi = asm.GetType(typeof(Program).FullName).GetMethod(nameof(MainInner));
        System.Type runtimeTy = asm.GetType(typeof(Runtime).FullName);
        int count = (int)mi.Invoke(null, new object[] { System.Activator.CreateInstance(runtimeTy) });

        if (count != 2)
            return 0;

        return 100;
    }

    public static int MainInner(IRuntime rt)
    {
        s_rt = rt;
        var vr3 = s_126[0];
        M59(vr3, false, ref s_66);

        return s_rt.Count;
    }

    internal static void M59(byte arg2, bool arg3, ref uint arg4)
    {
        for (int var0 = 0; var0 < 2; var0++)
        {
            byte var3 = arg2;
            arg3 = 1 <= s_58;
            s_rt.WriteLine("c_480", var3);
            if ((arg3 || arg3))
            {
                short vr12 = default(short);
                arg4 = (uint)vr12;
                int vr13 = default(int);
                s_rt.WriteLine("c_439", vr13);
            }
        }
    }
}

public interface IRuntime
{
    int Count { get; }
    void WriteLine<T>(string site, T value);
}

public class Runtime : IRuntime
{
    public int Count { get; set; }
    public void WriteLine<T>(string site, T value) => Count++;
}

public class CollectibleALC : System.Runtime.Loader.AssemblyLoadContext
{
    public CollectibleALC() : base(true)
    {
    }
}
