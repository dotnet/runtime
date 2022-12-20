// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public sealed class SuppressFinalizeTest {

    private uint size = 0;

    public SuppressFinalizeTest(uint size ) {
        this.size = size;
    }


    public bool RunTests() {

        LargeObject lo;
        try {
            lo = new LargeObject(size, true);
            GC.SuppressFinalize(lo);
        } catch (OutOfMemoryException) {
            Console.WriteLine("Large Memory Machine required");
            return true;
        } catch (Exception e) {
            Console.WriteLine("Unexpected Exception:");
            Console.WriteLine(e);
            return false;
        }
        lo = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        return (LargeObject.FinalizedCount==0);
    }

    public static int Main(string[] args) {
        SuppressFinalizeTest test = new SuppressFinalizeTest(MemCheck.ParseSizeMBAndLimitByAvailableMem(args));

        if (test.RunTests()) {
            Console.WriteLine("Test passed");
            return 100;
        }

        Console.WriteLine("Test failed");
        return 0;
    }
}
