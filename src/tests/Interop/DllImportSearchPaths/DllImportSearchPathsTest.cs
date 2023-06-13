// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

public class DllImportSearchPathsTest
{
    private static string Subdirectory => Path.Combine(NativeLibraryToLoad.GetDirectory(), "subdirectory");
    private static bool CanLoadAssemblyInSubdirectory =>
        !TestLibrary.Utilities.IsMonoLLVMFULLAOT &&
        !OperatingSystem.IsAndroid() &&
        !OperatingSystem.IsIOS() &&
        !OperatingSystem.IsTvOS() &&
        !OperatingSystem.IsBrowser();

    static int Main(string[] args)
    {
        try
        {
            AssemblyDirectory_NotFound();
            if (CanLoadAssemblyInSubdirectory)
                AssemblyDirectory_Found();

            if (OperatingSystem.IsWindows())
                AssemblyDirectory_Fallback_Found();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }

        return 100;
    }

    public static void AssemblyDirectory_NotFound()
    {
        // Library should not be found in the assembly directory
        Assert.Throws<DllNotFoundException>(() => NativeLibraryPInvoke.Sum(1, 2));
    }

    public static void AssemblyDirectory_Found()
    {
        // Library should be found in the assembly directory
        var assembly = Assembly.LoadFile(Path.Combine(Subdirectory, $"{nameof(DllImportSearchPathsTest)}.dll"));
        var type = assembly.GetType(nameof(NativeLibraryPInvoke));
        var method = type.GetMethod(nameof(NativeLibraryPInvoke.Sum));

        int sum = (int)method.Invoke(null, new object[] { 1, 2 });
        Assert.Equal(3, sum);
    }

    public static void AssemblyDirectory_Fallback_Found()
    {
        string currentDirectory = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = Subdirectory;

            // Library should not be found in the assembly directory, but should fall back to the default OS search which includes CWD on Windows
            int sum = NativeLibraryPInvoke.Sum(1, 2);
            Assert.Equal(3, sum);
        }
        finally
        {
            Environment.CurrentDirectory = currentDirectory;
        }
    }
}

public class NativeLibraryPInvoke
{
    public static int Sum(int a, int b)
    {
        return NativeSum(a, b);
    }

    [DllImport(NativeLibraryToLoad.Name)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
    static extern int NativeSum(int arg1, int arg2);
}
