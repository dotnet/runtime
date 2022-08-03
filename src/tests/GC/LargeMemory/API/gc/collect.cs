// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public sealed class CollectTest {
    private LargeObject lo;
    private int numTests = 0;
    public uint size = 0;
    
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public void CreateLargeObject() {
        lo = new LargeObject(size, true);
    }
    
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public void DestroyLargeObject() {
        lo = null;
    }

    private bool collectLargeObject(int gen) {
        numTests++;
        try {
            CreateLargeObject();
        } catch (OutOfMemoryException) {
            Console.WriteLine("Large Memory Machine required");
            return true;
        } catch (Exception e) {
            Console.WriteLine("Unexpected Exception:");
            Console.WriteLine(e);
            return false;
        }
        
        DestroyLargeObject();
        GC.Collect(gen);
        GC.WaitForPendingFinalizers();
        GC.Collect(gen);

        if (LargeObject.FinalizedCount>0) {
            Console.WriteLine("collectLargeObject {0} passed", gen);
            return true;
        }

        Console.WriteLine("collectLargeObject {0} failed", gen);
        return false;
    }

    public bool RunTests() {
        int numPassed = 0;

        if (collectLargeObject(0)) {
            numPassed++;
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        if (collectLargeObject(2)) {
            numPassed++;
        }


        return (numTests==numPassed);
    }

    public static int Main(string[] args) {
        CollectTest test = new CollectTest();
        test.size = MemCheck.ParseSizeMBAndLimitByAvailableMem(args);

        if (test.RunTests()) {
            Console.WriteLine("Test passed");
            return 100;
        }

        Console.WriteLine("Test failed");
        return 0;
    }
}
