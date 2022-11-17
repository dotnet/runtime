// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public sealed class ReRegisterForFinalizeTest {
    private LargeObject lo;
    private uint size = 0;

    public ReRegisterForFinalizeTest(uint size ) {
        this.size = size;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public void CreateLargeObject() {
        lo = new LargeObject(size, true);
        GC.ReRegisterForFinalize(lo);
    }
    
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public void DestroyLargeObject() {
        lo = null;
    }

    public bool RunTests() {
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
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        return (LargeObject.FinalizedCount==2);
    }

    public static int Main(string[] args) {
        ReRegisterForFinalizeTest test = new ReRegisterForFinalizeTest(MemCheck.ParseSizeMBAndLimitByAvailableMem(args));

        if (test.RunTests()) {
            Console.WriteLine("Test passed");
            return 100;
        }

        Console.WriteLine("Test failed");
        return 0;
    }
}
