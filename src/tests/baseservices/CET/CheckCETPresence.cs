// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

static class Program
{
    public static int Main()
    {
        Console.WriteLine("Checking whether codeflow enforcement technology (CET) is active");
        // TODO: perform the actual CET check
        // For now return failure to let us see that the test has actually run.
        return 101;
    }
}
