// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

internal class Program
{
    public static bool IsTimestampByte(int i)
    {
        return i >= 136 && i < 140;
    }

    public static int CompareDLLs(string folder1, string folder2)
    {
        foreach (string filepath1 in Directory.EnumerateFiles(folder1, "*.dll"))
        {
            byte[] file1 = File.ReadAllBytes(filepath1);
            byte[] file2 = File.ReadAllBytes(Path.Combine(folder2, Path.GetFileName(filepath1)));

            if (file1.Length != file2.Length)
            {
                Console.WriteLine(filepath1);
                Console.WriteLine("Expected Crossgen2'd files to be identical but they have different sizes.");
                return 2;
            }

            for (int i = 0; i < file1.Length; ++i)
            {
                if (file1[i] != file2[i] && !IsTimestampByte(i))
                {
                    Console.WriteLine(filepath1);
                    Console.WriteLine($"Difference at non-timestamp byte {i}");
                    return 1;
                }
            }

            Console.WriteLine($"Files of length {file1.Length} were identical.");
        }
        return 100;
    }

    public static int Main()
    {
        return CompareDLLs("seed1", "seed2");
    }
}
