// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

// a large object that resurrects itself
public sealed class LargeObject2 {

    private byte[][] data;

    public const long GB = 1024*1024*1024;

    public LargeObject2(uint sizeInGB)
    {
        data = new byte[sizeInGB][];
        for (int i=0; i<sizeInGB; i++) {
            data[i] = new byte[GB];
        }

    }

    ~LargeObject2() {
        FinalizerTest.LO2 = this;
    }

}

// allocates a large object on the finalizer thread
public sealed class FinalizerObject {
    uint size = 0;

    public FinalizerObject(uint sizeInGB)
    {
        size = sizeInGB;
    }

    ~FinalizerObject() {

        LargeObject lo =null;

        try {
            lo = new LargeObject(size);
        } catch (OutOfMemoryException) {
            Console.WriteLine("OOM");
            return;
        } catch (Exception e) {
            Console.WriteLine("Unexpected Exception");
            Console.WriteLine(e);
            return;
        }

        if (lo!=null)
            FinalizerTest.ObjectSize = lo.Size;
        GC.KeepAlive(lo);
    }
}


public sealed class FinalizerTest {

    public static LargeObject2 LO2 = null;
    public static long ObjectSize = 0;

    private uint size = 0;
    private int numTests = 0;


    public FinalizerTest(uint size) {
        this.size = size;
    }

    bool ressurectionTest() {
        numTests++;

        try {
            new LargeObject2(size);
        } catch (OutOfMemoryException) {
            Console.WriteLine("Large Memory Machine required");
            return false;
        } catch (Exception e) {
            Console.WriteLine("Unexpected Exception");
            Console.WriteLine(e);
            return false;
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        if (LO2 != null) {
            Console.WriteLine("ressurectionTest passed");
            LO2 = null;
            return true;
        }
        Console.WriteLine("ressurectionTest failed");
        return false;

    }


    bool allocateInFinalizerTest() {
        numTests++;

        try {
            new FinalizerObject(size);
        } catch (OutOfMemoryException) {
            Console.WriteLine("Large Memory Machine required");
            return false;
        } catch (Exception e) {
            Console.WriteLine("Unexpected Exception");
            Console.WriteLine(e);
            return false;
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        if (ObjectSize == size*LargeObject.GB) {
            Console.WriteLine("allocateInFinalizerTest passed");
            return true;
        }
        Console.WriteLine("{0} {1}", ObjectSize, size*LargeObject.GB);
        Console.WriteLine("allocateInFinalizerTest failed");
        return false;

    }

    public bool RunTests() {

        int numPassed = 0;

        if (allocateInFinalizerTest() ) {
            numPassed++;
        }

        if (ressurectionTest() ) {
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

        FinalizerTest test = new FinalizerTest(size);


        if (test.RunTests()) {
            Console.WriteLine("Test passed");
            return 100;
        }

        Console.WriteLine("Test failed");
        return 0;
    }
}

