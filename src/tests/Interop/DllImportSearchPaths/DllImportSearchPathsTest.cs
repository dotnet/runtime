// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
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

    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void DefaultFlagsAot_Found()
    {
        int sum = NativeLibraryPInvokeAot.Sum_DefaultFlags(1, 2);
        Assert.Equal(3, sum);
    }

    [ConditionalFact(typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
    public static void System32Aot_NotFound()
    {
        Assert.Throws<DllNotFoundException>(() => NativeLibraryPInvokeAot.Sum_System32(1, 2));
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public static void AssemblyDirectory_NoFallback_NotFound()
    {
        string currentDirectory = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = Subdirectory;

            // Library should not be found in the assembly directory and should not fall back to the default OS search
            Assert.Throws<DllNotFoundException>(() => NativeLibraryPInvoke.Sum_Copy(1, 2));
        }
        finally
        {
            Environment.CurrentDirectory = currentDirectory;
        }
    }

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public static void DefaultFlags_Found()
    {
        string currentDirectory = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = Subdirectory;

            // Library should not be found in the assembly directory, but should fall back to the default OS search which includes CWD on Windows
            int sum = NativeLibraryPInvoke.Sum_DefaultFlags(1, 2);
            Assert.Equal(3, sum);
        }
        finally
        {
            Environment.CurrentDirectory = currentDirectory;
        }
    }

    [Fact]
    public static void System32_NotFound()
    {
        string currentDirectory = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = Subdirectory;

            // Library should not be found in System32 (Windows) or default OS search (non-Windows)
            Assert.Throws<DllNotFoundException>(() => NativeLibraryPInvoke.Sum_Copy(1, 2));
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
    internal const string CopyName = $"{NativeLibraryToLoad.Name}-copy";
    internal const string DefaultFlagsName = $"{NativeLibraryToLoad.Name}-default-flags";
    internal const string System32Name = $"{NativeLibraryToLoad.Name}-system32";

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Sum(int a, int b)
        => NativeSum(a, b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Sum_Copy(int a, int b)
        => NativeSum_Copy(a, b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Sum_DefaultFlags(int a, int b)
        => NativeSum_DefaultFlags(a, b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Sum_System32(int a, int b)
        => NativeSum_System32(a, b);

    [DllImport(NativeLibraryToLoad.Name)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
    static extern int NativeSum(int arg1, int arg2);

    [DllImport(CopyName, EntryPoint = nameof(NativeSum))]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
    static extern int NativeSum_Copy(int arg1, int arg2);

    [DllImport(DefaultFlagsName, EntryPoint = nameof(NativeSum))]
    static extern int NativeSum_DefaultFlags(int arg1, int arg2);

    [DllImport(System32Name, EntryPoint = nameof(NativeSum))]
    static extern int NativeSum_System32(int arg1, int arg2);
}

public class NativeLibraryPInvokeAot
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Sum(int a, int b)
        => NativeSum(a, b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Sum_DefaultFlags(int a, int b)
        => NativeSum_DefaultFlags(a, b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Sum_System32(int a, int b)
        => NativeSum_System32(a, b);

    // For NativeAOT, validate the case where the native library is next to the AOT application.
    [DllImport(NativeLibraryToLoad.Name + "-in-native")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
    static extern int NativeSum(int arg1, int arg2);

    [DllImport(NativeLibraryToLoad.Name + "-in-native-default-flags", EntryPoint = nameof(NativeSum))]
    static extern int NativeSum_DefaultFlags(int arg1, int arg2);

    [DllImport(NativeLibraryToLoad.Name + "-in-native-system32", EntryPoint = nameof(NativeSum))]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    static extern int NativeSum_System32(int arg1, int arg2);
}

public class NativeLibraryWithDependency
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Sum(int a, int b)
        => CallDependencySum(a, b);

    // For LoadLibrary on Windows, search flags, like that represented by System32, are incompatible with
    // looking at a specific path (per AssemblyDirectory), so we specify both flags to validate that we do
    // not incorrectly use both when looking in the assembly directory.
    [DllImport(nameof(NativeLibraryWithDependency))]
    [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]
    static extern int CallDependencySum(int a, int b);
}