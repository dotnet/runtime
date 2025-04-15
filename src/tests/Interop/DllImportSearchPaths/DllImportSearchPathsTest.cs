// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public class DllImportSearchPathsTest
{
    private static string Subdirectory => Path.Combine(NativeLibraryToLoad.GetDirectory(), "subdirectory");

    [Fact]
    public static void AssemblyDirectory_NotFound()
    {
        // Library should not be found in the assembly directory
        Assert.Throws<DllNotFoundException>(() => NativeLibraryPInvoke.Sum(1, 2));
    }

    public static bool CanLoadAssemblyInSubdirectory =>
        !TestLibrary.Utilities.IsNativeAot &&
        !TestLibrary.PlatformDetection.IsMonoFULLAOT &&
        !OperatingSystem.IsAndroid() &&
        !OperatingSystem.IsIOS() &&
        !OperatingSystem.IsTvOS() &&
        !OperatingSystem.IsBrowser() &&
        !OperatingSystem.IsWasi();

    [ConditionalFact(nameof(CanLoadAssemblyInSubdirectory))]
    public static void AssemblyDirectory_InMemory_NotFound()
    {
        byte[] bytes = File.ReadAllBytes(Path.Combine(Subdirectory, $"{nameof(DllImportSearchPathsTest)}.dll"));
        Assembly assembly = Assembly.Load(bytes);
        var type = assembly.GetType(nameof(NativeLibraryPInvoke));
        var method = type.GetMethod(nameof(NativeLibraryPInvoke.Sum));

        Exception ex = Assert.Throws<TargetInvocationException>(() =>method.Invoke(null, new object[] { 1, 2 }));
        Assert.Equal(typeof(DllNotFoundException), ex.InnerException.GetType());
    }

    [ConditionalFact(nameof(CanLoadAssemblyInSubdirectory))]
    public static void AssemblyDirectory_Found()
    {
        // Library should be found in the assembly directory
        var assembly = Assembly.LoadFile(Path.Combine(Subdirectory, $"{nameof(DllImportSearchPathsTest)}.dll"));
        var type = assembly.GetType(nameof(NativeLibraryPInvoke));
        var method = type.GetMethod(nameof(NativeLibraryPInvoke.Sum));

        int sum = (int)method.Invoke(null, new object[] { 1, 2 });
        Assert.Equal(3, sum);
    }

    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void AssemblyDirectoryAot_Found()
    {
        int sum = NativeLibraryPInvokeAot.Sum(1, 2);
        Assert.Equal(3, sum);
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
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

    [ConditionalFact(nameof(CanLoadAssemblyInSubdirectory))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public static void AssemblyDirectory_SearchFlags_WithDependency_Found()
    {
        // Library and its dependency should be found in the assembly directory.
        var assembly = Assembly.LoadFile(Path.Combine(Subdirectory, $"{nameof(DllImportSearchPathsTest)}.dll"));
        var type = assembly.GetType(nameof(NativeLibraryWithDependency));
        var method = type.GetMethod(nameof(NativeLibraryWithDependency.Sum));

        int sum = (int)method.Invoke(null, new object[] { 1, 2 });
        Assert.Equal(3, sum);
        Console.WriteLine("NativeLibraryWithDependency.Sum returned {0}", sum);
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

public class NativeLibraryPInvokeAot
{
    public static int Sum(int a, int b)
    {
        return NativeSum(a, b);
    }

    // For NativeAOT, validate the case where the native library is next to the AOT application.
    // The passing of DllImportSearchPath.System32 is done to ensure on Windows the runtime won't fallback
    // and try to search the application directory by default.
    [DllImport(NativeLibraryToLoad.Name + "-in-native")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    static extern int NativeSum(int arg1, int arg2);
}

public class NativeLibraryWithDependency
{
    public static int Sum(int a, int b)
    {
        return CallDependencySum(a, b);
    }

    // For LoadLibrary on Windows, search flags, like that represented by System32, are incompatible with
    // looking at a specific path (per AssemblyDirectory), so we specify both flags to validate that we do
    // not incorrectly use both when looking in the assembly directory.
    [DllImport(nameof(NativeLibraryWithDependency))]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    static extern int CallDependencySum(int a, int b);
}