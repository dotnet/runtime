// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        byte[] file1 = File.ReadAllBytes("crossgen2smoke1.ildll");
        byte[] file2 = File.ReadAllBytes("crossgen2smoke2.ildll");
        if (file1.Length != file2.Length)
        {
            Console.WriteLine("Expected R2R'd files to be identical but they have different sizes.");
            return 1;
        }

        for (int i = 0; i < file1.Length; ++i)
        {
            if (file1[i] != file2[i])
            {
                Console.WriteLine($"Difference at byte {i}");
                return 1;
            }
        }

        Console.WriteLine($"Files of length {file1.Length} were identical.");
        return 100;
    }
}
