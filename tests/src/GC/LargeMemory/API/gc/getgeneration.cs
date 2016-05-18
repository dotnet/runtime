// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;


public sealed class GetGenerationTest {

    public uint size = 0;
    private int numTests=0;

    private bool getGenerationWR() {
        numTests++;
        int gen = -1;

        try {
            gen = GC.GetGeneration(new WeakReference(new LargeObject(size)));
        } catch (OutOfMemoryException) {
            Console.WriteLine("Large Memory Machine required");
            return false;
        } catch (Exception e) {
            Console.WriteLine("Unexpected Exception:");
            Console.WriteLine(e);
            return false;
        }

        if (gen==GC.MaxGeneration) {
            Console.WriteLine("getGenerationWR passed");
            return true;
        }

        Console.WriteLine(gen);
        Console.WriteLine("getGenerationWR failed");
        return false;
    }

    private bool getGeneration() {
        numTests++;

        int gen = -1;

        try {
            LargeObject lo = new LargeObject(size);
            gen = GC.GetGeneration(lo);

        } catch (OutOfMemoryException) {
            Console.WriteLine("Large Memory Machine required");
            return false;
        } catch (Exception e) {
            Console.WriteLine("Unexpected Exception:");
            Console.WriteLine(e);
            return false;
        }

        if (gen==GC.MaxGeneration) {
            Console.WriteLine("getGeneration passed");
            return true;
        }

        Console.WriteLine(gen);
        Console.WriteLine("getGeneration failed");
        return false;
    }

    public bool RunTests() {
        int numPassed = 0;

        if (getGeneration()) {
            numPassed++;
        }

        if (getGenerationWR()) {
            numPassed++;
        }


        return (numPassed==numTests);
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

        GetGenerationTest test = new GetGenerationTest();
        test.size = sizeInMB;
        if (test.RunTests()) {
            Console.WriteLine("Test passed");
            return 100;
        }

        Console.WriteLine("Test failed");
        return 0;
    }
}
