// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

public class Program
{
    public static int CompareDLLs(string folder1, string folder2)
    {
        int result = 100;

        string superIlcFolder1 = Directory.GetDirectories(folder1, "CPAOT*").First();
        string superIlcFolder2 = Directory.GetDirectories(folder2, "CPAOT*").First();

        // Check for files that failed compilation with one of the seeds but not the other
        HashSet<string> uniqueFilenames = new HashSet<string>(Directory.GetFiles(superIlcFolder1, "*.dll").Select(Path.GetFileName));
        uniqueFilenames.SymmetricExceptWith(Directory.GetFiles(superIlcFolder2, "*.dll").Select(Path.GetFileName));
        foreach (string uniqueFilename in uniqueFilenames)
        {
            Console.WriteLine($"{uniqueFilename} was found in only one of the output folders.");
            result = 1;
        }

        foreach (string filename in Directory.GetFiles(superIlcFolder1, "*.dll").Select(Path.GetFileName))
        {
            if (uniqueFilenames.Contains(filename))
                continue;

            byte[] file1 = File.ReadAllBytes(Path.Combine(superIlcFolder1, Path.GetFileName(filename)));
            byte[] file2 = File.ReadAllBytes(Path.Combine(superIlcFolder2, Path.GetFileName(filename)));

            if (file1.Length != file2.Length)
            {
                Console.WriteLine(filename);
                Console.WriteLine($"Expected ReadyToRun'd files to be identical but they have different sizes ({file1.Length} and {file2.Length})");
                result = 1;
                continue;
            }

            int byteDifferentCount = 0;
            for (int i = 0; i < file1.Length; ++i)
            {
                if (file1[i] != file2[i])
                {
                    ++byteDifferentCount;
                }
            }

            if (byteDifferentCount > 0)
            {
                result = 1;
                Console.WriteLine($"Error: Found {byteDifferentCount} different bytes in {filename}");
                continue;
            }

            Console.WriteLine($"Files of length {file1.Length} were identical.");
        }
        return result;
    }

    public static string OSExeSuffix(string path) => (OperatingSystem.IsWindows() ? path + ".exe" : path);

    private static void PrepareCompilationInputFolder(string coreRootFolder, string compilationInputFolder)
    {
        if (Directory.Exists(compilationInputFolder))
        {
            Directory.Delete(compilationInputFolder, true);
        }
        Directory.CreateDirectory(compilationInputFolder);

        CopyDeterminismTestAssembly(coreRootFolder, compilationInputFolder, "System.Private.CoreLib.dll");
    }

    private static void CopyDeterminismTestAssembly(string coreRootFolder, string compilationInputFolder, string fileName)
    {
        File.Copy(Path.Combine(coreRootFolder, fileName), Path.Combine(compilationInputFolder, fileName));
    }

    public static bool CompileWithSeed(int seed, string coreRootPath, string compilationInputFolder, string outDir)
    {
        string superIlcPath = Path.Combine(coreRootPath, "R2RTest", "R2RTest.dll");
        string coreRunPath = Path.Combine(coreRootPath, OSExeSuffix("corerun"));

        Console.WriteLine($"================================== Compiling with seed {seed} ==================================");
        Environment.SetEnvironmentVariable("CoreRT_DeterminismSeed", seed.ToString());
        if (Directory.Exists(outDir))
        {
            Directory.Delete(outDir, true);
        }
        Directory.CreateDirectory(outDir);
        ProcessStartInfo processStartInfo = new ProcessStartInfo(coreRunPath, $"{superIlcPath} compile-directory -cr {coreRootPath} -in {compilationInputFolder} --nojit --noexe --large-bubble --release --nocleanup -out {outDir}");
        var process = Process.Start(processStartInfo);
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            Console.WriteLine($"Compilation failed. {processStartInfo.FileName} {processStartInfo.Arguments} failed with exit code {process.ExitCode}");
        }
        return 0 == process.ExitCode;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        string coreRootPath = Environment.GetEnvironmentVariable("CORE_ROOT");
        string compilationInputFolder = "TestAssemblies";
        PrepareCompilationInputFolder(coreRootPath, compilationInputFolder);
        if (!CompileWithSeed(1, coreRootPath, compilationInputFolder, "seed1"))
            return 1;
        if (!CompileWithSeed(2, coreRootPath, compilationInputFolder, "seed2"))
            return 1;
        return CompareDLLs("seed1", "seed2");
    }
}
