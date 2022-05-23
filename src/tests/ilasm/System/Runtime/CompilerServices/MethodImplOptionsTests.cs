// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

public static class MethodImplOptionsTests
{
    public static int Main()
    {
        const int Pass = 100;
        const int Fail = 101;

        string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
        if (string.IsNullOrWhiteSpace(coreRoot))
        {
            Console.WriteLine("Environment variable is not set: 'CORE_ROOT'");
            return Fail;
        }
        if (!Directory.Exists(coreRoot))
        {
            Console.WriteLine($"Did not find CORE_ROOT directory: {coreRoot}");
            return Fail;
        }

        var nativeExeExtensions = new string[] { string.Empty, ".exe" };
        bool found = false;
        string ilasmFile = null;
        string ildasmFile = null;
        foreach (string nativeExeExtension in nativeExeExtensions)
        {
            ilasmFile = Path.Combine(coreRoot, $"ilasm{nativeExeExtension}");
            ildasmFile = Path.Combine(coreRoot, $"ildasm{nativeExeExtension}");
            if (File.Exists(ilasmFile) && File.Exists(ildasmFile))
            {
                found = true;
                break;
            }
        }
        if (!found)
        {
            Console.WriteLine($"Did not find ilasm or ildasm in CORE_ROOT directory: {coreRoot}");
            return Fail;
        }

        bool allPassed = true;
        allPassed &=
            RunMethodImplOptionsTest(
                ilasmFile,
                ildasmFile,
                "AggressiveOptimizationTest",
                "MiAggressiveOptimization.il",
                "aggressiveoptimization");
        return allPassed ? Pass : Fail;
    }

    private static bool RunMethodImplOptionsTest(
        string ilasmFile,
        string ildasmFile,
        string testName,
        string ilFileName,
        string ilDisasmAttributeKeyword)
    {
        Console.WriteLine(testName);

        try
        {
            string disasmIlFileName;
            ProcessStartInfo ilasmPsi, ildasmPsi;
            GetIlasmProcessStartInfos(ilasmFile, ildasmFile, ilFileName, out disasmIlFileName, out ilasmPsi, out ildasmPsi);

            Process ilasmProcess = Process.Start(ilasmPsi);
            ilasmProcess.WaitForExit();
            if (ilasmProcess.ExitCode != 0)
            {
                Console.WriteLine($"ilasm failed with exit code: {ilasmProcess.ExitCode}");
                return false;
            }

            Process ildasmProcess = Process.Start(ildasmPsi);
            ildasmProcess.WaitForExit();
            if (ildasmProcess.ExitCode != 0)
            {
                Console.WriteLine($"ildasm failed with exit code: {ildasmProcess.ExitCode}");
                return false;
            }

            string disasmIl = File.ReadAllText(disasmIlFileName);
            var findMainAttributeRegex =
                new Regex(
                    @"\bvoid\s+Main\s*\(\s*\).*?\b" + ilDisasmAttributeKeyword + @"\b",
                    RegexOptions.Compiled | RegexOptions.Multiline);
            if (!findMainAttributeRegex.IsMatch(disasmIl))
            {
                Console.WriteLine($"Attribute '{ilDisasmAttributeKeyword}' did not round-trip through ilasm and ildasm");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
        return true;
    }

    private static void GetIlasmProcessStartInfos(
        string ilasmFile,
        string ildasmFile,
        string ilFileName,
        out string disasmIlFileName,
        out ProcessStartInfo ilasmPsi,
        out ProcessStartInfo ildasmPsi)
    {
        if (!File.Exists(ilFileName))
        {
            throw new FileNotFoundException(
                $"Did not find '{ilFileName}' in working directory '{Environment.CurrentDirectory}'");
        }

        string currentDirectory = Environment.CurrentDirectory;

        ilasmPsi = new ProcessStartInfo();
        ilasmPsi.UseShellExecute = false;
        ilasmPsi.WorkingDirectory = currentDirectory;
        ilasmPsi.FileName = ilasmFile;
        string asmDllFileName = $"{Path.GetFileNameWithoutExtension(ilFileName)}.dll";
        ilasmPsi.Arguments =
            $"-nologo -dll -optimize -output={asmDllFileName} {ilFileName}";

        ildasmPsi = new ProcessStartInfo();
        ildasmPsi.UseShellExecute = false;
        ildasmPsi.WorkingDirectory = currentDirectory;
        ildasmPsi.FileName = ildasmFile;
        disasmIlFileName = $"{Path.GetFileNameWithoutExtension(ilFileName)}_dis{Path.GetExtension(ilFileName)}";
        ildasmPsi.Arguments = $"-out={disasmIlFileName} {asmDllFileName}";
    }
}
