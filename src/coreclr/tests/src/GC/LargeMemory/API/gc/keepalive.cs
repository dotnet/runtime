// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            return false;
        } catch (Exception e) {
            Console.WriteLine("Unexpected Exception:");
            Console.WriteLine(e);
            return false;
        }

        return true;
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


        KeepAliveTest test = new KeepAliveTest(size);

        if (test.RunTests()) {
            Console.WriteLine("Test passed");
            return 100;
        }

        Console.WriteLine("Test failed");
        return 0;
    }
}
