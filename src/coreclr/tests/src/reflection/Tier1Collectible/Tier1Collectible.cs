// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

public static class Program
{
    private static int Main()
    {
        const int Pass = 100, Fail = 1;

        PromoteToTier1AndRun(() =>
        {
            CollectibleTestIteration();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForPendingFinalizers();
        });

        return Pass;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CollectibleTestIteration()
    {
        GetCollectible().Hello();
    }

    private static int s_collectibleIndex = 0;

    public interface IHelloWorld
    {
        void Hello();
    }

    private static IHelloWorld GetCollectible()
    {
        int collectibleIndex = s_collectibleIndex++;

        AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("CollectibleAssembly" + collectibleIndex),
            AssemblyBuilderAccess.RunAndCollect);

        ModuleBuilder mb = ab.DefineDynamicModule("CollectibleModule" + collectibleIndex);

        TypeBuilder tb = mb.DefineType("CollectibleHelloType" + collectibleIndex, TypeAttributes.Public);
        tb.AddInterfaceImplementation(typeof(IHelloWorld));
        tb.DefineDefaultConstructor(MethodAttributes.Public);

        MethodBuilder methb = tb.DefineMethod("Hello", MethodAttributes.Public | MethodAttributes.Virtual);
        methb.GetILGenerator().Emit(OpCodes.Ret);

        Type helloType = tb.CreateTypeInfo().UnderlyingSystemType;
        return (IHelloWorld)Activator.CreateInstance(helloType);
    }

    private static void PromoteToTier1AndRun(Action action)
    {
        // Call the method once to register a call for call counting
        action();

        // Allow time for call counting to begin
        Thread.Sleep(500);

        // Call the method enough times to trigger tier 1 promotion
        for (int i = 0; i < 100; i++)
        {
            action();
        }

        // Allow time for the method to be jitted at tier 1
        Thread.Sleep(500);

        // Run the tier 1 code
        action();
    }
}
