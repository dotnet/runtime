// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

internal class Program
{
    public static bool IsTimestampByte(int i)
    {
        return i >= 136 && i < 140;
    }

    public static int CompareDLLs(string folder1, string folder2)
    {
        int result = 100;
        foreach (string filepath1 in Directory.EnumerateFiles(folder1, "*.dll"))
        {
            byte[] file1 = File.ReadAllBytes(filepath1);
            byte[] file2 = File.ReadAllBytes(Path.Combine(folder2, Path.GetFileName(filepath1)));

            if (file1.Length != file2.Length)
            {
                Console.WriteLine(filepath1);
                Console.WriteLine($"Expected ReadyToRun'd files to be identical but they have different sizes ({file1.Length} and {file2.Length})");
                result = 1;
            }

            for (int i = 0; i < file1.Length; ++i)
            {
                if (file1[i] != file2[i] && !IsTimestampByte(i))
                {
                    Console.WriteLine(filepath1);
                    Console.WriteLine($"Difference at non-timestamp byte {i}");
                    result = 1;
                }
            }

            Console.WriteLine($"Files of length {file1.Length} were identical.");
        }
        return result;
    }

    public static string OSExeSuffix(string path) => (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? path + ".exe" : path);

    public static void CompileWithSeed(int seed, string outDir)
    {
        string coreRootPath = Environment.GetEnvironmentVariable("Core_Root");
        string superIlcPath = Path.Combine(coreRootPath, "ReadyToRun.SuperIlc", OSExeSuffix("ReadyToRun.SuperIlc"));


        Environment.SetEnvironmentVariable("CoreRT_DeterminismSeed", seed.ToString());
        Directory.CreateDirectory(outDir);
        ProcessStartInfo processStartInfo = new ProcessStartInfo(superIlcPath, $"compile-directory -cr {coreRootPath} -in {coreRootPath} --nojit --noexe --large-bubble --release --nocleanup -out {outDir}");
        Process.Start(processStartInfo).WaitForExit();
    }

    public static int Main()
    {
        CompileWithSeed(1, "seed1");
        CompileWithSeed(2, "seed2");
        return CompareDLLs("seed1", "seed2");
    }
}
