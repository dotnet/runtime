// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Xunit;

// ALC For AssemblyB
public class AssemblyLoadContextB : AssemblyLoadContext
{
    public AssemblyLoadContextB(string name, bool isCollectible) : base(name, isCollectible)
    {
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name == "AssemblyC")
        {
            Test116953.Log($"AssemblyB is resolving C");
            return Test116953.AssemblyC;
        }

        if (assemblyName.Name == "AssemblyA")
        {
            Test116953.Log($"AssemblyB is resolving A");
            return Test116953.AssemblyA;
        }

        return null;
    }
}

public class Test116953
{
    public static Assembly AssemblyA;
    public static Assembly AssemblyC;

    [Fact]
    public static void TestEntryPoint()
    {
        for (int i = 0; i < 3; i++)
        {
            Log($"Test #{i}");
            RunOneTest();
        }
    }

    private static void RunOneTest()
    {
        // Create three collectible ALCs
        var alcB = new AssemblyLoadContextB("ALC_B", isCollectible: true);
        var alcC = new AssemblyLoadContext("ALC_C", isCollectible: true);
        var alcA = new AssemblyLoadContext("ALC_A", isCollectible: true);

        // Track ALCs with weak references
        WeakReference alcCRef = new WeakReference(alcC, trackResurrection: true);
        WeakReference alcBRef = new WeakReference(alcB, trackResurrection: true);
        WeakReference alcARef = new WeakReference(alcA, trackResurrection: true);

        // Load assembly A
        AssemblyA = LoadAssembly(alcA, "AssemblyA");

        // Load assembly C
        AssemblyC = LoadAssembly(alcC, "AssemblyC");

        // Load assembly B (depends on assemblies A and C)
        Assembly assemblyB = LoadAssembly(alcB, "AssemblyB");
	Log($"AssemblyB: {assemblyB}");

        // Call method in assembly B
        Log("\nTesting call to method in assembly B:");
        Type? typeBClass = assemblyB.GetType("AssemblyB.ClassB");
        if (typeBClass != null)
        {
            object bInstance = Activator.CreateInstance(typeBClass)!;
            string resultB = (string)typeBClass.GetMethod("GetMessage").Invoke(bInstance, null);
            Log($"B method returns: {resultB}");
        }
        else
        {
            Log("AssemblyB.ClassB not found!");
        }

        AssemblyA = null;
        AssemblyC = null;

        // Unload the three ALCs
        Log("\nStarting to unload ALCs...");

        alcA.Unload();
        alcC.Unload();
        alcB.Unload();

        alcA = null;
        alcB = null;
        alcC = null;

        for (int i = 0; i < 100 && (alcARef.IsAlive || alcBRef.IsAlive || alcCRef.IsAlive); i++)
        {
            Thread.Sleep(1);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Log($"ALC_A status: {(alcARef.IsAlive ? "still in memory" : "unloaded")}");
        Log($"ALC_B status: {(alcBRef.IsAlive ? "still in memory" : "unloaded")}");
        Log($"ALC_C status: {(alcCRef.IsAlive ? "still in memory" : "unloaded")}");

        Log("Test completed");
    }

    private static Assembly LoadAssembly(AssemblyLoadContext alc, string assemblyName)
    {
        string assemblyDir = Assembly.GetExecutingAssembly().Location;
        assemblyDir = Path.GetDirectoryName(assemblyDir)!;
        return alc.LoadFromAssemblyPath(Path.Combine(assemblyDir, $"{assemblyName}.dll"));
    }

    public static void Log(string message)
    {
        Console.WriteLine(message);
    }
}
