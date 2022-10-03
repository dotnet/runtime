// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public sealed class KeepAliveTest {

    private uint size = 0;

    public KeepAliveTest(uint size) {
        this.size = size;
    }

    public bool RunTests() {

       try {
            LargeObject lo = new LargeObject(size);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            if (lo == null)
                return false;
            GC.KeepAlive(lo);

        } catch (OutOfMemoryException) {
            Console.WriteLine("Large Memory Machine required");
            return true;
        } catch (Exception e) {
            Console.WriteLine("Unexpected Exception:");
            Console.WriteLine(e);
            return false;
        }

        return true;
    }


    public static int Main(string[] args) {
        KeepAliveTest test = new KeepAliveTest(MemCheck.ParseSizeMBAndLimitByAvailableMem(args));

        if (test.RunTests()) {
            Console.WriteLine("Test passed");
            return 100;
        }

        Console.WriteLine("Test failed");
        return 0;
    }
}
