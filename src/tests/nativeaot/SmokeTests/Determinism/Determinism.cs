// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

using var baseline = File.OpenRead("baseline.object");
using var compare = File.OpenRead("compare.object");

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        Console.WriteLine($"Baseline size: {baseline.Length}");
        Console.WriteLine($"Compare size: {compare.Length}");

        if (baseline.Length != compare.Length)
        {
            Console.WriteLine("Test Fail: Baseline and Compare have different sizes.");
            return 101;
        }

        long length = baseline.Length;
        for (int i = 0; i < length; i++)
        {
            if (baseline.ReadByte() != compare.ReadByte())
            {
                Console.WriteLine($"Test Fail: Baseline and Compare were different"
                                  + " at byte {i}.");
                return 101;
            }
        }

        // We're not interested in running this, we just want some junk to compile
        if (Environment.GetEnvironmentVariable("Never") == "Ever")
        {
            Delegates.Run();
            Devirtualization.Run();
            Generics.Run();
            Interfaces.Run();
        }

        return 100;
    }
}
