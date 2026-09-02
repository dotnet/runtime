// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Xunit;
using TestLibrary;

// Repeatedly loads an assembly into a collectible AssemblyLoadContext, invokes a generic
// virtual method on a type from that assembly and unloads the context.
// The results of the generic virtual dispatch are stored in a process-wide cache, which is
// flushed when a collectible context is unloaded. If the flush does not take effect, a stale
// entry could be returned for a new type that happens to reuse the addresses of an unloaded
// one, which results in a hang or a crash.
public class GenericVirtualMethodUnloading
{
    private class TestALC : AssemblyLoadContext
    {
        public TestALC(int id) : base($"GenericVirtualMethod{id}", isCollectible: true)
        {
        }
    }

    [ActiveIssue("https://github.com/dotnet/runtimelab/issues/155: Collectible assemblies", typeof(Utilities), nameof(Utilities.IsNativeAot))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34072", TestRuntimes.Mono)]
    [SkipOnCoreClr("Test polls a fixed number of times for collectible ALCs to be unloaded, which is unreliable under GC stress", RuntimeTestModes.AnyGCStress)]
    [Fact]
    public static void CallGenericVirtualMethodAcrossUnloads()
    {
        string payloadPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "GenericVirtualMethodUnloaded.dll");

        for (int iteration = 0; iteration < 10; iteration++)
        {
            WeakReference context = InvokeAndUnload(payloadPath, iteration);
            for (int collection = 0; context.IsAlive && collection < 10; collection++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Assert.False(context.IsAlive, $"Context {iteration} did not unload.");
            VerifyVirtualDispatchCacheIsEmpty();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference InvokeAndUnload(string payloadPath, int iteration)
    {
        TestALC alc = new TestALC(iteration);
        WeakReference weakAlc = new WeakReference(alc, trackResurrection: false);

        Assembly payload = alc.LoadFromAssemblyPath(payloadPath);
        Type machineType = payload.GetType("DerivedMachine", throwOnError: true);
        Type stateType = payload.GetType("MarkerState", throwOnError: true);
        Type baseType = payload.GetType("Machine", throwOnError: true);

        object machine = Activator.CreateInstance(machineType)!;
        MethodInfo change = baseType.GetMethod("Change")!.MakeGenericMethod(stateType);

        TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => change.Invoke(machine, null));
        Assert.NotNull(ex.InnerException);
        Assert.Equal("ExpectedException", ex.InnerException!.GetType().FullName);

        alc.Unload();
        return weakAlc;
    }

    // Unloading a collectible context flushes the virtual function pointer cache, so that
    // targets belonging to the unloaded context cannot be returned by a later lookup.
    // Check that the cache is indeed empty after the unload.
    private static void VerifyVirtualDispatchCacheIsEmpty()
    {
        Type helpersType = typeof(object).Assembly.GetType("System.Runtime.CompilerServices.VirtualDispatchHelpers");
        if (helpersType == null)
        {
            // The cache is specific to CoreCLR.
            return;
        }

        object cache = helpersType.GetField("s_virtualFunctionPointerCache", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        Array table = (Array)cache.GetType().GetField("_table", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(cache);

        FieldInfo infoField = table.GetType().GetElementType().GetField("_info", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo versionField = infoField.FieldType.GetField("_version", BindingFlags.NonPublic | BindingFlags.Instance);

        // Only validate the flush sentinel table (2 usable entries + element 0 aux data).
        if (table.Length != 3)
            return;
        for (int i = 1; i < table.Length; i++)
        {
            object entryInfo = infoField.GetValue(table.GetValue(i));
            Assert.Equal(0u, (uint)versionField.GetValue(entryInfo));
        }
    }
}
