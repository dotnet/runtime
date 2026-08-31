// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public sealed class GetTotalMemoryTest {
    private uint size = 0;
    public GetTotalMemoryTest(uint size) {
        this.size = size;
    }


    public bool RunTests() {

        try {
            LargeObject lo = new LargeObject(size);
            long mem  = GC.GetTotalMemory(false);
            long delta = (long)(size*LargeObject.MB)/(long)10;

            if ( (mem - size*LargeObject.MB)> delta) {
                Console.WriteLine("{0} {1} {2}", mem, size*LargeObject.MB, delta);
                return false;
            }

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
        GetTotalMemoryTest test = new GetTotalMemoryTest(MemCheck.ParseSizeMBAndLimitByAvailableMem(args));
        if (test.RunTests()) {
            Console.WriteLine("Test passed");
            return 100;
        }

        Console.WriteLine("Test failed");
        return 0;
    }
}
