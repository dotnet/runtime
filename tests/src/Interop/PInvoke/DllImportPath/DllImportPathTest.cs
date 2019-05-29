// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

class Test
{
    private const string RelativeSubdirectoryName = "RelativeNative";
    private const string PathEnvSubdirectoryName = "Subdirectory";
    private const string PathEnvFileName = "MovedNativeLib";

#if PLATFORM_WINDOWS
    private const string RelativePath1 = @".\RelativeNative\..\DllImportPath_Relative";
    private const string RelativePath3 = @"..\DllImportPathTest\DllImportPath_Relative";
#else
    private const string RelativePath1 =  @"./RelativeNative/../libDllImportPath_Relative";
    private const string RelativePath3 = @"../DllImportPathTest/libDllImportPath_Relative";
#endif

    private const string UnicodeFileName = "DllImportPath_Unicode✔";

    [DllImport(@"DllImportPath_Local", CharSet = CharSet.Unicode, EntryPoint = "MarshalStringPointer_InOut")]
    private static extern bool MarshalStringPointer_InOut_Local1([In, Out]ref string strManaged);

    [DllImport(@".\DllImportPath_Local", CharSet = CharSet.Unicode, EntryPoint = "MarshalStringPointer_InOut")]
    private static extern bool MarshalStringPointer_InOut_Local2([In, Out]ref string strManaged);

    [DllImport(@"DllImportPath.Local.dll", CharSet = CharSet.Unicode, EntryPoint = "MarshalStringPointer_InOut")]
    private static extern bool MarshalStringPointer_InOut_LocalWithDot1([In, Out]ref string strManaged);

    [DllImport(@".\DllImportPath.Local.dll", CharSet = CharSet.Unicode, EntryPoint = "MarshalStringPointer_InOut")]
    private static extern bool MarshalStringPointer_InOut_LocalWithDot2([In, Out]ref string strManaged);

    [DllImport(RelativePath1, CharSet = CharSet.Unicode, EntryPoint = "MarshalStringPointer_InOut")]
    private static extern bool MarshalStringPointer_InOut_Relative1([In, Out]ref string strManaged);

    [DllImport(@"..\DllImportPathTest\DllImportPath_Relative.dll", CharSet = CharSet.Unicode, EntryPoint = "MarshalStringPointer_InOut")]
    private static extern bool MarshalStringPointer_InOut_Relative2([In, Out]ref string strManaged);

    [DllImport(RelativePath3, CharSet = CharSet.Unicode, EntryPoint = "MarshalStringPointer_InOut")]
    private static extern bool MarshalStringPointer_InOut_Relative3([In, Out]ref string strManaged);

    [DllImport(@".\..\DllImportPathTest\DllImportPath_Relative.dll", CharSet = CharSet.Unicode, EntryPoint = "MarshalStringPointer_InOut")]
    private static extern bool MarshalStringPointer_InOut_Relative4([In, Out]ref string strManaged);

    [DllImport(UnicodeFileName, CharSet = CharSet.Unicode, EntryPoint = "MarshalStringPointer_InOut")]
    private static extern bool MarshalStringPointer_InOut_Unicode([In, Out]ref string strManaged);
    
    [DllImport(PathEnvFileName, CharSet = CharSet.Unicode, EntryPoint = "MarshalStringPointer_InOut")]
    private static extern bool MarshalStringPointer_InOut_PathEnv([In, Out]ref string strManaged);

    static bool DllExistsOnLocalPath()
    {
        string strManaged = "Managed";
        string native = " Native";

        Console.WriteLine("[Calling MarshalStringPointer_InOut_Local1].");
        string strPara1 = strManaged;
        if (!MarshalStringPointer_InOut_Local1(ref strPara1))
        {
            Console.WriteLine("Return value is wrong");
            return false;
        }

        if (native != strPara1)
        {
            Console.WriteLine("The passed string is wrong");
            return false;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.WriteLine("[Calling MarshalStringPointer_InOut_Local2]");
            string strPara2 = strManaged;
            if (!MarshalStringPointer_InOut_Local2(ref strPara2))
            {
                Console.WriteLine("Return value is wrong");
                return false;
            }

            if (native != strPara2)
            {
                Console.WriteLine("The passed string is wrong");
                return false;
            }

            Console.WriteLine("[Calling MarshalStringPointer_InOut_LocalWithDot1]");
            string strPara3 = strManaged;
            if (!MarshalStringPointer_InOut_LocalWithDot1(ref strPara3))
            {
                Console.WriteLine("Return value is wrong");
                return false;
            }

            if (native != strPara3)
            {
                Console.WriteLine("The passed string is wrong");
                return false;
            }

            Console.WriteLine("[Calling MarshalStringPointer_InOut_LocalWithDot2]");
            string strPara4 = strManaged;
            if (!MarshalStringPointer_InOut_LocalWithDot2(ref strPara4))
            {
                Console.WriteLine("Return value is wrong");
                return false;
            }

            if (native != strPara4)
            {
                Console.WriteLine("The passed string is wrong");
                return false;
            }
        }

        return true;
    }

    static bool DllExistsOnRelativePath()
    {
        string strManaged = "Managed";
        string native = " Native";

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // We need to ensure that the subdirectory exists for off-Windows.
        {        
            var currentDirectory = Directory.GetCurrentDirectory();
            var info = new DirectoryInfo(currentDirectory);
            info.CreateSubdirectory(RelativeSubdirectoryName);
        }

        Console.WriteLine("[Calling MarshalStringPointer_InOut_Relative1]");
        string strPara5 = strManaged;
        if (!MarshalStringPointer_InOut_Relative1(ref strPara5))
        {
            Console.WriteLine("Return value is wrong");
            return false;
        }

        if (native != strPara5)
        {
            Console.WriteLine("The passed string is wrong");
            return false;
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.WriteLine("[Calling MarshalStringPointer_InOut_Relative2]");
            string strPara6 = strManaged;
            if (!MarshalStringPointer_InOut_Relative2(ref strPara6))
            {
                Console.WriteLine("Return value is wrong");
                return false;
            }

            if (native != strPara6)
            {
                Console.WriteLine("The passed string is wrong");
                return false;
            }
        }
        
        Console.WriteLine("[Calling MarshalStringPointer_InOut_Relative3]");
        string strPara7 = strManaged;
        if (!MarshalStringPointer_InOut_Relative3(ref strPara7))
        {
            Console.WriteLine("Return value is wrong");
            return false;
        }

        if (native != strPara7)
        {
            Console.WriteLine("The passed string is wrong");
            return false;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.WriteLine("[Calling MarshalStringPointer_InOut_Relative4]");
            string strPara8 = strManaged;
            if (!MarshalStringPointer_InOut_Relative4(ref strPara8))
            {
                Console.WriteLine("Return value is wrong");
                return false;
            }
        
            if (native != strPara8)
            {
                Console.WriteLine("The passed string is wrong");
                return false;
            }
        }

        return true;
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

    static bool DllExistsOnPathEnv()
    {
        SetupPathEnvTest();

        string managed = "Managed";
        string native = " Native";

        Console.WriteLine("[Calling MarshalStringPointer_InOut_PathEnv]");
        if (!MarshalStringPointer_InOut_PathEnv(ref managed))
        {
            Console.WriteLine("Return value is wrong");
            return false;
        }

        if (native != managed)
        {
            Console.WriteLine($"The passed string is wrong. Expected {native} got {managed}.");
            return false;
        }

        return true;
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

    static bool DllExistsUnicode()
    {
        SetupUnicodeTest();

        string managed = "Managed";
        string native = " Native";
        
        Console.WriteLine("[Calling MarshalStringPointer_InOut_Unicode]");
        if (!MarshalStringPointer_InOut_Unicode(ref managed))
        {
            Console.WriteLine("Return value is wrong");
            return false;
        }

        if (native != managed)
        {
            Console.WriteLine("The passed string is wrong");
            return false;
        }

        return true;
    }

    public static int Main(string[] args)
    {
        bool success = true;

        success = success && DllExistsOnLocalPath();
        success = success && DllExistsOnRelativePath();

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) // This test fails due to a bug in OSX 10.12 combined with the weird way that HFS+ handles unicode file names
        {
            success = success && DllExistsUnicode();
        }
        success = success && DllExistsOnPathEnv();
        
        return success ? 100 : 101;
    }
}
