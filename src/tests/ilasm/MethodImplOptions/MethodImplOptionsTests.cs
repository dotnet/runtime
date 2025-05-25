// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

public class MethodImplOptionsTests : IDisposable
{
    private string _ilasmFile;
    private string _ildasmFile;

    public MethodImplOptionsTests()
    {
        const int Pass = 100;
        const int Fail = 101;

        string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
        if (string.IsNullOrWhiteSpace(coreRoot))
        {
            Console.WriteLine("Environment variable is not set: 'CORE_ROOT'");
            throw new InvalidOperationException("Environment variable is not set: 'CORE_ROOT'");
        }
        if (!Directory.Exists(coreRoot))
        {
            Console.WriteLine($"Did not find CORE_ROOT directory: {coreRoot}");
            throw new InvalidOperationException("Did not find CORE_ROOT directory");
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
            throw new InvalidOperationException("Did not find ilasm or ildasm in CORE_ROOT directory");
        }

        _ilasmFile = ilasmFile;
        _ildasmFile = ildasmFile;
    }

    [Theory]
    [InlineData("AggressiveOptimizationTest", "MiAggressiveOptimization.il", "Main", "aggressiveoptimization")]
    [InlineData("AsyncIdentifierTest", "MiAsUnquotedIdentifier.il", "'async'", "async")]
    [InlineData("RuntimeIdentifierNoInliningTest", "MiAsUnquotedIdentifier.il", "'runtime'", "noinlining")]
    public void RunMethodImplOptionsTest(
        string testName,
        string ilFileName,
        string methodName,
        string ilDisasmAttributeKeyword)
    {
        Console.WriteLine(testName);

        string disasmIlFileName;
        ProcessStartInfo ilasmPsi, ildasmPsi;
        GetIlasmProcessStartInfos(_ilasmFile, _ildasmFile, ilFileName, out disasmIlFileName, out ilasmPsi, out ildasmPsi);

        Process ilasmProcess = Process.Start(ilasmPsi);
        ilasmProcess.WaitForExit();
        Assert.Equal(0, ilasmProcess.ExitCode);

        Process ildasmProcess = Process.Start(ildasmPsi);
        ildasmProcess.WaitForExit();
        Assert.Equal(0, ildasmProcess.ExitCode);

        string disasmIl = File.ReadAllText(disasmIlFileName);
        var findMainAttributeRegex =
            new Regex(
                $@"\bvoid\s+{methodName}\s*\(.*?\).*?\b{ilDisasmAttributeKeyword}\b",
                RegexOptions.Compiled | RegexOptions.Multiline);

        Assert.True(findMainAttributeRegex.IsMatch(disasmIl), $"Attribute '{ilDisasmAttributeKeyword}' did not round-trip through ilasm and ildasm");
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

    public void Dispose() {}
}
