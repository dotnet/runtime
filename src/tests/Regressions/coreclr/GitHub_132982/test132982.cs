// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// VirtualCallStubManager::~VirtualCallStubManager held CrstStubDispatchCache for the whole
// destructor and then deleted its loader heaps, which take CrstExecutableAllocatorLock. Both are
// level 0, so a checked build asserted:
//
//   Crst Level violation: Can't take level 0 lock CrstExecutableAllocatorLock
//   because you already holding level 0 lock CrstStubDispatchCache
//
// Reaching the assert requires three things to line up:
//
//  1. A collectible LoaderAllocator is reclaimed, which is what runs the destructor.
//  2. W^X double mapping is enabled (the default outside riscv64), because
//     ExecutableAllocator::ReleaseWorker only takes its lock in that mode.
//  3. One of the manager's LoaderHeaps owns a block it actually releases. A collectible manager
//     gets a single page for cache_entry_heap out of the LoaderAllocator's VSD initial block, and
//     that page is not released by the heap; only pages the heap reserves itself are. So the work
//     done inside the collectible context has to push cache_entry_heap past its first page.
//
// A ResolveCacheElem is allocated per distinct (MethodTable, DispatchToken) pair, so the test
// creates 16 * 16 instantiations of Shape<T1, T2> and calls 8 interface methods on each.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Xunit;

public interface IShape
{
    int M0();
    int M1();
    int M2();
    int M3();
    int M4();
    int M5();
    int M6();
    int M7();
}

public abstract class ShapeBase : IShape
{
    public virtual int M0() => 0;
    public virtual int M1() => 1;
    public virtual int M2() => 2;
    public virtual int M3() => 3;
    public virtual int M4() => 4;
    public virtual int M5() => 5;
    public virtual int M6() => 6;
    public virtual int M7() => 7;
}

// Each closed instantiation gets its own MethodTable, owned by whichever LoaderAllocator owns this
// assembly - the collectible one when the assembly is loaded into a collectible context.
public sealed class Shape<T1, T2> : ShapeBase
{
}

// Invoked by reflection from inside the collectible AssemblyLoadContext so that the interface call
// sites, and therefore the virtual stub dispatch stubs and resolve cache entries they create,
// belong to the collectible LoaderAllocator.
public static class Dispatcher
{
    private static readonly Type[] s_typeArguments =
    {
        typeof(bool), typeof(byte), typeof(sbyte), typeof(char),
        typeof(short), typeof(ushort), typeof(int), typeof(uint),
        typeof(long), typeof(ulong), typeof(float), typeof(double),
        typeof(decimal), typeof(string), typeof(object), typeof(DateTime),
    };

    public static int Run()
    {
        List<IShape> shapes = CreateShapes();
        int sum = 0;

        for (int iteration = 0; iteration < 2; iteration++)
        {
            foreach (IShape shape in shapes)
            {
                sum += D0(shape);
                sum += D1(shape);
                sum += D2(shape);
                sum += D3(shape);
                sum += D4(shape);
                sum += D5(shape);
                sum += D6(shape);
                sum += D7(shape);
            }
        }

        return sum;
    }

    private static List<IShape> CreateShapes()
    {
        var shapes = new List<IShape>(s_typeArguments.Length * s_typeArguments.Length);
        Type definition = typeof(Shape<,>);

        foreach (Type first in s_typeArguments)
        {
            foreach (Type second in s_typeArguments)
            {
                shapes.Add((IShape)Activator.CreateInstance(definition.MakeGenericType(first, second)));
            }
        }

        return shapes;
    }

    // Eight separate megamorphic call sites, one per dispatch token. Each (MethodTable, token) pair
    // the resolver sees costs one ResolveCacheElem out of cache_entry_heap.
    [MethodImpl(MethodImplOptions.NoInlining)] private static int D0(IShape shape) => shape.M0();
    [MethodImpl(MethodImplOptions.NoInlining)] private static int D1(IShape shape) => shape.M1();
    [MethodImpl(MethodImplOptions.NoInlining)] private static int D2(IShape shape) => shape.M2();
    [MethodImpl(MethodImplOptions.NoInlining)] private static int D3(IShape shape) => shape.M3();
    [MethodImpl(MethodImplOptions.NoInlining)] private static int D4(IShape shape) => shape.M4();
    [MethodImpl(MethodImplOptions.NoInlining)] private static int D5(IShape shape) => shape.M5();
    [MethodImpl(MethodImplOptions.NoInlining)] private static int D6(IShape shape) => shape.M6();
    [MethodImpl(MethodImplOptions.NoInlining)] private static int D7(IShape shape) => shape.M7();
}

public class Test132982
{
    [SkipOnCoreClr("Depends on the collectible VSD cache entry heap growing past its first page, which GC stress perturbs.", RuntimeTestModes.AnyGCStress)]
    [Fact]
    public static void UnloadingCollectibleContextDoesNotViolateLockOrder()
    {
        WeakReference contextRef = LoadRunAndUnload();

        // A generous bound rather than the measured minimum: on a workstation-GC checked build the
        // context dies on the second forced gen2 collection, but unload timing is not a contract
        // and varies with GC mode.
        const int MaxCollections = 10;

        for (int i = 0; contextRef.IsAlive && i < MaxCollections; i++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();
        }

        Assert.False(contextRef.IsAlive, $"AssemblyLoadContext was not collected after {MaxCollections} forced gen2 collections.");

        // LoaderAllocator::GCLoaderAllocators, and therefore ~VirtualCallStubManager, can run one
        // collection after the context object itself dies.
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
        GC.WaitForPendingFinalizers();
    }

    // Kept in its own non-inlined frame so that no stack slot keeps the context, or anything loaded
    // into it, alive once it returns.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference LoadRunAndUnload()
    {
        var context = new AssemblyLoadContext("Collectible132982", isCollectible: true);

        Assembly assembly = context.LoadFromAssemblyPath(typeof(Test132982).Assembly.Location);
        MethodInfo run = assembly.GetType(nameof(Dispatcher), throwOnError: true)
                                 .GetMethod(nameof(Dispatcher.Run), BindingFlags.Public | BindingFlags.Static);
        const int ExpectedDispatchSum = 14_336;
        Assert.Equal(ExpectedDispatchSum, (int)run.Invoke(null, null));

        var contextRef = new WeakReference(context, trackResurrection: true);
        context.Unload();
        return contextRef;
    }
}
