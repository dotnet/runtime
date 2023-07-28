// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

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
            return true;
        } catch (LargeException) {
            return true;
        } catch (Exception e) {
            Console.WriteLine("Unexpected Exception");
            Console.WriteLine(e.ToString());
            return false;
        }
    }

    public static int Main(string[] args) {
        LargeExceptionTest test = new LargeExceptionTest(MemCheck.ParseSizeMBAndLimitByAvailableMem(args));

        if (test.RunTests()) {
            Console.WriteLine("Test passed");
            return 100;
        }

        Console.WriteLine("Test failed");
        return 0;
    }
}

