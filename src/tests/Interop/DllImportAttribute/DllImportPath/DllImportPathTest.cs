// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public class Test
{
    private const string RelativeSubdirectoryName = "RelativeNative";
    private const string PathEnvSubdirectoryName = "Subdirectory";
    private const string PathEnvFileName = "MovedNativeLib";

    private const string RelativePath1Windows = @".\RelativeNative\..\DllImportPath_Relative";
    private const string RelativePath3Windows = @"..\DllImportPathTest\DllImportPath_Relative";

    private const string RelativePath1Unix =  @"./RelativeNative/../libDllImportPath_Relative";
    private const string RelativePath3Unix = @"../DllImportPathTest/libDllImportPath_Relative";

    private const string UnicodeFileName = "DllImportPath_Unicode\u2714";

    [DllImport(@"DllImportPath_Local", EntryPoint = "GetZero")]
    private static extern int GetZero_Local1();

    [DllImport(@".\DllImportPath_Local", EntryPoint = "GetZero")]
    private static extern int GetZero_Local2();

    [DllImport(@"DllImportPath.Local.dll", EntryPoint = "GetZero")]
    private static extern int GetZero_LocalWithDot1();

    [DllImport(@".\DllImportPath.Local.dll", EntryPoint = "GetZero")]
    private static extern int GetZero_LocalWithDot2();

    [DllImport(RelativePath1Windows, EntryPoint = "GetZero")]
    private static extern int GetZero_Relative1Windows();

    [DllImport(RelativePath1Unix, EntryPoint = "GetZero")]
    private static extern int GetZero_Relative1Unix();

    [DllImport(@"..\DllImportPathTest\DllImportPath_Relative.dll", EntryPoint = "GetZero")]
    private static extern int GetZero_Relative2();

    [DllImport(RelativePath3Windows, EntryPoint = "GetZero")]
    private static extern int GetZero_Relative3Windows();

    [DllImport(RelativePath3Unix, EntryPoint = "GetZero")]
    private static extern int GetZero_Relative3Unix();

    [DllImport(@".\..\DllImportPathTest\DllImportPath_Relative.dll", EntryPoint = "GetZero")]
    private static extern int GetZero_Relative4();

    [DllImport(UnicodeFileName, EntryPoint = "GetZero")]
    private static extern int GetZero_Unicode();

    [DllImport(PathEnvFileName, EntryPoint = "GetZero")]
    private static extern int GetZero_PathEnv();

    [DllImport("DllImportPath_ExeFile.exe", EntryPoint = "GetZero")]
    private static extern int GetZero_Exe();

    static void TestNativeLibraryProbingOnLocalPath()
    {
        string strManaged = "Managed";
        string native = " Native";

        GetZero_Local1();

        if (OperatingSystem.IsWindows())
        {
            GetZero_Local2();

            GetZero_LocalWithDot1();

            GetZero_LocalWithDot2();
        }
    }

    static void TestNativeLibraryProbingOnRelativePath()
    {
        string strManaged = "Managed";
        string native = " Native";
        bool isWindows = OperatingSystem.IsWindows();

        if (!isWindows) // We need to ensure that the subdirectory exists for off-Windows.
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var info = new DirectoryInfo(currentDirectory);
            info.CreateSubdirectory(RelativeSubdirectoryName);
        }

        if (isWindows)
        {
            GetZero_Relative1Windows();
        }
        else
        {
            GetZero_Relative1Unix();
        }

        if (OperatingSystem.IsWindows())
        {
            GetZero_Relative2();
        }

        if (isWindows)
        {
            GetZero_Relative3Windows();
        }
        else
        {
            GetZero_Relative3Unix();
        }

        if (OperatingSystem.IsWindows())
        {
            GetZero_Relative4();
        }
    }

    private static void SetupPathEnvTest()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var info = new DirectoryInfo(currentDirectory);
        var subDirectory = info.CreateSubdirectory(PathEnvSubdirectoryName);

        var file = info.EnumerateFiles("*DllImportPath_PathEnv*", SearchOption.TopDirectoryOnly).FirstOrDefault();

        var newFileLocation = Path.Combine(subDirectory.FullName, file.Name);

        file.CopyTo(Path.Combine(subDirectory.FullName, PathEnvFileName + file.Extension), true);

        Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + $";{subDirectory.FullName}");
    }

    static void TestNativeLibraryProbingOnPathEnv()
    {
        SetupPathEnvTest();

        GetZero_PathEnv();
    }

    private static void SetupUnicodeTest()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var info = new DirectoryInfo(currentDirectory);

        var file = info.EnumerateFiles("*DllImportPath_Local*", SearchOption.TopDirectoryOnly)
                        .FirstOrDefault(localFile =>
                            localFile.Extension == ".dll"
                            || localFile.Extension == ".so"
                            || localFile.Extension == ".dylib");

        var unicodeFileLocation = file.FullName.Replace("DllImportPath_Local", UnicodeFileName);

        file.CopyTo(unicodeFileLocation, true);
    }

    static void TestNativeLibraryProbingUnicode()
    {
        SetupUnicodeTest();

        GetZero_Unicode();
    }

    static void TestNativeExeProbing()
    {
        GetZero_Exe();
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            TestNativeLibraryProbingOnLocalPath();
            TestNativeLibraryProbingOnRelativePath();
            if (!OperatingSystem.IsMacOS()) // This test fails due to a bug in OSX 10.12 combined with the weird way that HFS+ handles unicode file names
            {
                TestNativeLibraryProbingUnicode();
            }
            // Setting LD_LIBRARY_PATH/DYLD_LIBRARY_PATH may not be allowed on Mac
            if (!OperatingSystem.IsMacOS())
            {
                TestNativeLibraryProbingOnPathEnv();
            }
            if (OperatingSystem.IsWindows())
            {
                TestNativeExeProbing();
            }
        }
        catch (System.Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return 101;
        }
        return 100;
    }
}
