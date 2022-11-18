// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

// a large object that resurrects itself
public sealed class LargeObject2 {

    private byte[][] data;

    public const long MB = 1024*1024;

    public LargeObject2(uint sizeInMB)
    {
        data = new byte[sizeInMB][];
        for (int i=0; i<sizeInMB; i++) {
            data[i] = new byte[MB];
        }

    }

    ~LargeObject2() {
        FinalizerTest.LO2 = this;
    }

}

// allocates a large object on the finalizer thread
public sealed class FinalizerObject {
    uint size = 0;

    public FinalizerObject(uint sizeInMB)
    {
        size = sizeInMB;
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
            Console.WriteLine(e.ToString());
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

    public LargeObject2 TempObject;

    private uint size = 0;
    private int numTests = 0;


    public FinalizerTest(uint size) {
        this.size = size;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public void CreateLargeObject() {
        TempObject = new LargeObject2(size);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public void DestroyLargeObject() {
        TempObject = null;
    }

    bool resurrectionTest() {
        numTests++;

        try {
            CreateLargeObject();
            DestroyLargeObject();
        } catch (OutOfMemoryException) {
            Console.WriteLine("Large Memory Machine required");
            return true;
        } catch (Exception e) {
            Console.WriteLine("Unexpected Exception");
            Console.WriteLine(e.ToString());
            return false;
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        if (LO2 != null) {
            Console.WriteLine("resurrectionTest passed");
            LO2 = null;
            return true;
        }
        Console.WriteLine("resurrectionTest failed");
        return false;

    }


    bool allocateInFinalizerTest() {
        numTests++;

        try {
            new FinalizerObject(size);
        } catch (OutOfMemoryException) {
            Console.WriteLine("Large Memory Machine required");
            return true;
        } catch (Exception e) {
            Console.WriteLine("Unexpected Exception");
            Console.WriteLine(e.ToString());
            return false;
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        if (ObjectSize == size*LargeObject.MB) {
            Console.WriteLine("allocateInFinalizerTest passed");
            return true;
        }
        Console.WriteLine("{0} {1}", ObjectSize, size*LargeObject.MB);
        Console.WriteLine("allocateInFinalizerTest failed");
        return false;

    }

    public bool RunTests() {

        int numPassed = 0;

        if (allocateInFinalizerTest() ) {
            numPassed++;
        }

        if (resurrectionTest() ) {
            numPassed++;
        }

        return (numTests==numPassed);
    }


    public static int Main(string[] args) {
        FinalizerTest test = new FinalizerTest(MemCheck.ParseSizeMBAndLimitByAvailableMem(args));

        if (test.RunTests()) {
            Console.WriteLine("Test passed");
            return 100;
        }

        Console.WriteLine("Test failed");
        return 0;
    }
}

