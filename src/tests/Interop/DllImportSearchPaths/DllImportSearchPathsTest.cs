// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

public class DllImportSearchPathsTest : IDisposable
{
    private static string Subdirectory => Path.Combine(AppContext.BaseDirectory, "subdirectory");

    [Fact]
    public void AssemblyDirectory_NotFound()
    {
        // Library should not be found in the assembly directory
        Assert.Throws<DllNotFoundException>(() => NativeLibraryPInvoke.Sum(1, 2));

        string currentDirectory = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = Subdirectory;

            // Library should not be found in the assembly directory and should not fall back to the current directory
            Assert.Throws<DllNotFoundException>(() => NativeLibraryPInvoke.Sum(1, 2));
        }
        finally
        {
            Environment.CurrentDirectory = currentDirectory;
        }
    }

    [Fact]
    public void AssemblyDirectory_Found()
    {
        // Library should be found in the assembly directory
        var assembly = Assembly.LoadFile(Path.Combine(Subdirectory, $"{nameof(DllImportSearchPathsTest)}.dll"));
        var type = assembly.GetType(nameof(NativeLibraryPInvoke));
        var method = type.GetMethod(nameof(NativeLibraryPInvoke.Sum));

        int sum = (int)method.Invoke(null, new object[] { 1, 2 });
        Assert.Equal(3, sum);
    }

    public void Dispose() { }
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
