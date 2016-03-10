// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

public sealed class CollectTest {

    private int numTests = 0;
    public uint size = 0;

    private bool collectLargeObject(int gen) {
        numTests++;
        LargeObject lo;
        try {
            lo = new LargeObject(size, true);
        } catch (OutOfMemoryException) {
            Console.WriteLine("Large Memory Machine required");
            return false;
        } catch (Exception e) {
            Console.WriteLine("Unexpected Exception:");
            Console.WriteLine(e);
            return false;
        }
        lo = null;
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

        uint size = 0;
        try {
            size = UInt32.Parse(args[0]);
        } catch (Exception e) {
            if ( (e is IndexOutOfRangeException) || (e is FormatException) || (e is OverflowException) ) {
                Console.WriteLine("args: uint - number of GB to allocate");
                return 0;
            }
            throw;
        }

        CollectTest test = new CollectTest();
        test.size = size;

        if (test.RunTests()) {
            Console.WriteLine("Test passed");
            return 100;
        }

        Console.WriteLine("Test failed");
        return 0;
    }
}
