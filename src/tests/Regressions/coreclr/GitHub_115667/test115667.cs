// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;
using System.Threading.Tasks;

public interface I0
{
    Task<bool> M8();
}

public struct S1 : I0
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<bool> M8()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        return false;
    }
}

public class Runtime_115667
{
    [Fact]
    public static void TestEntryPoint()
    {
        System.Runtime.Loader.AssemblyLoadContext alc = new CollectibleALC();
        System.Reflection.Assembly asm = alc.LoadFromAssemblyPath(System.Reflection.Assembly.GetExecutingAssembly().Location);
        System.Reflection.MethodInfo mi = asm.GetType(typeof(Runtime_115667).FullName).GetMethod(nameof(MainInner));
        System.Type runtimeTy = asm.GetType(typeof(Runtime).FullName);
        mi.Invoke(null, new object[] { System.Activator.CreateInstance(runtimeTy) });
    }

    public static void MainInner(IRuntime rt)
    {
        bool vr1 = new S1().M8().GetAwaiter().GetResult();
    }
}

public interface IRuntime
{
    void WriteLine<T>(string site, T value);
}

public class Runtime : IRuntime
{
    public void WriteLine<T>(string site, T value) => System.Console.WriteLine(value);
}

public class CollectibleALC : System.Runtime.Loader.AssemblyLoadContext
{
    public CollectibleALC() : base(true)
    {
    }
}
