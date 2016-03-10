// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

public sealed class LargeException : Exception
{
// disabling unused variable warning
#pragma warning disable 0414
    LargeObject lo;
#pragma warning restore 0414

    public LargeException(uint size) {
        lo = new LargeObject(size);
    }
}


public sealed class LargeExceptionTest {

    private uint size = 0;
    public LargeExceptionTest(uint size) {
        this.size = size;
    }

    public bool RunTests() {

        try {
            throw new LargeException(size);
        } catch (OutOfMemoryException) {
            Console.WriteLine("Large Memory Machine required");
            return false;
        } catch (LargeException) {
            return true;
        } catch (Exception e) {
            Console.WriteLine("Unexpected Exception");
            Console.WriteLine(e);
            return false;
        }

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

        LargeExceptionTest test = new LargeExceptionTest(size);


        if (test.RunTests()) {
            Console.WriteLine("Test passed");
            return 100;
        }

        Console.WriteLine("Test failed");
        return 0;
    }
}

