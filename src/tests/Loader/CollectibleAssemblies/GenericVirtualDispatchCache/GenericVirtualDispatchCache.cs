// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Xunit;

public class GenericVirtualDispatchCacheTest
{
    [Fact]
    public static void TestEntryPoint()
    {
        string payloadPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "GenericVirtualDispatchPayload.dll");

        for (int iteration = 1; iteration <= 100; iteration++)
        {
            WeakReference context = InvokeAndRelease(payloadPath, iteration);

            for (int collection = 0; context.IsAlive && collection < 10; collection++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            Assert.False(context.IsAlive, $"Context {iteration} did not unload.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference InvokeAndRelease(string payloadPath, int iteration)
    {
        AssemblyLoadContext context = new($"GenericVirtualDispatchCache.{iteration}", isCollectible: true);
        WeakReference weak = new(context, trackResurrection: false);
        Assembly payload = context.LoadFromAssemblyPath(payloadPath);
        Type machineType = payload.GetType("GenericVirtualDispatchPayload.DerivedMachine", throwOnError: true)!;
        Type stateType = payload.GetType("GenericVirtualDispatchPayload.MarkerState", throwOnError: true)!;
        Type baseType = payload.GetType("GenericVirtualDispatchPayload.Machine", throwOnError: true)!;
        object machine = Activator.CreateInstance(machineType)!;
        MethodInfo change = baseType.GetMethod("Change")!.MakeGenericMethod(stateType);

        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => change.Invoke(machine, null));
        Assert.Equal("GenericVirtualDispatchPayload.ExpectedException", exception.InnerException?.GetType().FullName);

        change = null!;
        machine = null!;
        baseType = null!;
        stateType = null!;
        machineType = null!;
        payload = null!;
        context.Unload();
        return weak;
    }
}
