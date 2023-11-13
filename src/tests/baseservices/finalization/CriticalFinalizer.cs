// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests the weak ordering among normal and critical finalizers: for objects reclaimed by garbage collection
// at the same time, all the noncritical finalizers must be called before any of the critical finalizers.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Threading.Tasks;
using Xunit;

class Normal
{
    public static int Finalized;

    ~Normal() => Finalized++;
}

class Critical : CriticalFinalizerObject
{
    public static int Finalized;
    public static int NormalFinalizedBeforeFirstCritical;

    ~Critical()
    {
        if (++Finalized == 1)
            NormalFinalizedBeforeFirstCritical = Normal.Finalized;
    }
}

public static class CriticalFinalizerTest
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void AllocateObjects(int count)
    {
        var arr = new object[checked(count * 2)];

        Parallel.For(0, count, i =>
        {
            arr[i * 2] = new Normal();
            arr[i * 2 + 1] = new Critical();
        });

        GC.KeepAlive(arr);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        const int Count = 100;

        // Allocate a bunch of Normal and Critical objects, then unroot them
        AllocateObjects(Count);

        // Force a garbage collection and wait until all finalizers are executed
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Check that all Normal objects were finalized before all Critical objects
        int normalFinalized = Normal.Finalized;
        int criticalFinalized = Critical.Finalized;
        int normalFinalizedBeforeFirstCritical = Critical.NormalFinalizedBeforeFirstCritical;

        if (normalFinalized != Count || criticalFinalized != Count || normalFinalizedBeforeFirstCritical != Count)
        {
            Console.WriteLine($"Finalized {normalFinalized} {nameof(Normal)} and {criticalFinalized} {nameof(Critical)} objects.");
            Console.WriteLine($"The first {nameof(Critical)} object was finalized after {normalFinalizedBeforeFirstCritical} {nameof(Normal)} objects.");
            return 101;
        }

        return 100;
    }
}
