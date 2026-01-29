// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

public class FloatSpecialValuesTests : IDisposable
{
    private string _ilasmFile;
    private string _ildasmFile;

    public FloatSpecialValuesTests()
    {
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

    [Fact]
    public void NaNOutputIsNormalizedAcrossPlatforms()
    {
        Console.WriteLine("NaNOutputIsNormalizedAcrossPlatforms");

        string ilFileName = "FloatSpecialValues.il";
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

        // Verify that NaN values use the normalized format without platform-specific suffixes
        // The hex representation should be followed by "// -nan" without "(ind)" suffix
        var nanFloat64Regex = new Regex(@"float64\(0xFFF8000000000000\)\s+//\s+-nan\s", RegexOptions.Compiled);
        Assert.True(nanFloat64Regex.IsMatch(disasmIl), 
            "NaN float64 should be output as '// -nan' without platform-specific suffixes like '(ind)'");

        // Verify the output does NOT contain the Windows-specific "(ind)" suffix
        Assert.DoesNotContain("-nan(ind)", disasmIl);
        Assert.DoesNotContain("-nan(", disasmIl);
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
