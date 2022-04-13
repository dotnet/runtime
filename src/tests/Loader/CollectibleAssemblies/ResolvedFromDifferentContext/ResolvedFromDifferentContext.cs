// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// When an assembly A1 from an unloadable AssemblyLoadContext 1 is referenced
// by another assembly A2 from an unloadable AssemblyLoadContext 2, e.g.
// when a class from A2 implements an interface from A1 and there are no
// instances of types from A1 and the managed assembly type and the related
// AssemblyLoadContext is gone too, the AssemblyLoadContext 1 cannot
// get collected, it would result in later crashes, null references
// etc.

using System;
using System.Runtime.Loader;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.IO;

class TestAssemblyLoadContext : AssemblyLoadContext
{
    WeakReference interfaceAssemblyRef = null;

    public TestAssemblyLoadContext(string name, bool isCollectible) : base(name,  isCollectible)
    {
    }

    public WeakReference InterfaceAssemblyRef { get { return interfaceAssemblyRef; } }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name == "TestInterface")
        {
            AssemblyLoadContext alc1 = new AssemblyLoadContext("Dependencies", true);
            Console.WriteLine($"Loading TestInterface by alc {alc1} for alc {this}");
            Assembly a = alc1.LoadFromAssemblyPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\TestInterface\TestInterface.dll"));
            interfaceAssemblyRef = new WeakReference(a);
            return a;
        }

        return null;
    }
}

class Test
{
    static AssemblyLoadContext alc1 = null;
    static WeakReference interfaceAssemblyRef = null;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Assembly LoadUsingResolvingEvent()
    {
        alc1 = new AssemblyLoadContext("Dependencies", true);
        AssemblyLoadContext alc2 = new AssemblyLoadContext("Test1", true);
        alc2.Resolving += Alc2_Resolving;
        Assembly assembly = alc2.LoadFromAssemblyPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\TestClass\TestClass.dll"));

        Type t = assembly.GetType("TestClass.Class");
        Console.WriteLine($"Type {t} obtained");

        MethodInfo mi = t.GetMethod("MainTest");
        Console.WriteLine($"Method {mi} obtained");

        alc1 = null;
        Console.WriteLine("Load done");

        return assembly;
    }

    private static Assembly Alc2_Resolving(AssemblyLoadContext arg1, AssemblyName arg2)
    {
        Console.WriteLine($"Resolving event by alc {alc1} for alc {arg1}");
        if (alc1 != null && arg2.Name == "TestInterface")
        {
            Console.WriteLine($"Loading TestInterface by alc {alc1} for alc {arg1}");
            Assembly a = alc1.LoadFromAssemblyPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\TestInterface\TestInterface.dll"));
            interfaceAssemblyRef = new WeakReference(a);
            return a;
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Assembly LoadUsingLoadOverride()
    {
        TestAssemblyLoadContext alc2 = new TestAssemblyLoadContext("Test2", true);
        Assembly assembly = alc2.LoadFromAssemblyPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\TestClass\TestClass.dll"));

        Type t = assembly.GetType("TestClass.Class");
        
        Console.WriteLine($"Load done, type {t} obtained");

        interfaceAssemblyRef = alc2.InterfaceAssemblyRef;

        return assembly;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference TestDependencies(string testCase)
    {
        Assembly assembly;

        if (testCase == "ResolvingEvent")
        {
            assembly = LoadUsingResolvingEvent();
        }
        else
        {
            assembly = LoadUsingLoadOverride();
        }

        for (int i = 0; interfaceAssemblyRef.IsAlive && (i < 10); i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Type t = assembly.GetType("TestClass.Class");
        Type[] ifaces = t.GetInterfaces();
        ConstructorInfo ctor = t.GetConstructor(new Type[] { });
        object instance = ctor.Invoke(new object[0]);
        MethodInfo mi = t.GetMethod("MainTest");
        mi.Invoke(instance, null);

        return new WeakReference(assembly);
    }

    public static int TestFullUnload(string testCase)
    {
        Console.WriteLine($"Running test case {testCase}");

        WeakReference assemblyRef = TestDependencies(testCase);
        if (assemblyRef == null)
        {
            return 101;
        }

        for (int i = 0; assemblyRef.IsAlive && (i < 10); i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        if (assemblyRef.IsAlive)
        {
            Console.WriteLine("Failed to unload alc2");
            return 102;
        }

        if (interfaceAssemblyRef.IsAlive)
        {
            Console.WriteLine("Failed to unload alc1");
            return 103;
        }

        Console.WriteLine();
        return 100;
         
    }

    public static int Main(string[] args)
    {
        int status = 100;
        foreach (string testCase in new string[] {"LoadOverride", "ResolvingEvent"})
        {
            status = TestFullUnload(testCase);
        }

        return status;
    }
}
