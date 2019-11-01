// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            return false;
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

        uint sizeInMB = 0;
        try {
            sizeInMB = UInt32.Parse(args[0]);
        } catch (Exception e) {
            if ( (e is IndexOutOfRangeException) || (e is FormatException) || (e is OverflowException) ) {
                Console.WriteLine("args: uint - number of MB to allocate");
                return 0;
            }
            throw;
        }

        int availableMem = MemCheck.GetPhysicalMem();
        if (availableMem != -1 && availableMem < sizeInMB){
            sizeInMB = (uint)(availableMem > 300 ? 300 : (availableMem / 2));
            Console.WriteLine("Not enough memory. Allocating " + sizeInMB + "MB instead.");
        }

        CollectTest test = new CollectTest();
        test.size = sizeInMB;

        if (test.RunTests()) {
            Console.WriteLine("Test passed");
            return 100;
        }

        Console.WriteLine("Test failed");
        return 0;
    }
}
