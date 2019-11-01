// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            return false;
        } catch (Exception e) {
            Console.WriteLine("Unexpected Exception:");
            Console.WriteLine(e);
            return false;
        }

        return true;

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

        GetTotalMemoryTest test = new GetTotalMemoryTest(sizeInMB);
        if (test.RunTests()) {
            Console.WriteLine("Test passed");
            return 100;
        }

        Console.WriteLine("Test failed");
        return 0;
    }
}
