// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

public class MixedModeTests
{
    public static bool IsX86OrX64 => RuntimeInformation.ProcessArchitecture is Architecture.X86 or Architecture.X64;

    // ILASM has no ARM64 native export stub.
    [ConditionalTheory(nameof(IsX86OrX64))]
    [InlineData(false)]
    [InlineData(true)]
    public static unsafe void FixupTables(bool autoExports)
    {
        Assembly assembly = LoadAssembly(autoExports ? "AutoExports" : "VTableFixups");
        nint module = Marshal.GetHINSTANCE(assembly.ManifestModule);
        var twice = (delegate* unmanaged[Stdcall]<int, int>)NativeLibrary.GetExport(module, "Twice");
        var triple = (delegate* unmanaged[Stdcall]<int, int>)NativeLibrary.GetExport(module, "Triple");

        // Exercise both the first-call prestubs and the initialized entry points.
        for (int i = 0; i < 2; i++)
        {
            Assert.Equal(74, twice(37));
            Assert.Equal(111, triple(37));

            if (!autoExports)
            {
                Type type = assembly.GetType("VTableFixups", throwOnError: true)!;
                Func<int, nint> getTable = type.GetMethod("GetTable")!.CreateDelegate<Func<int, nint>>();
                nint* managed = (nint*)getTable(0);
                nint* unmanaged = (nint*)getTable(1);
                nint* retained = (nint*)getTable(2);

                Assert.Equal(47, ((delegate* managed<int, int>)managed[0])(37));
                Assert.Equal(17, ((delegate* managed<int, int>)managed[1])(37));
                Assert.Equal(74, ((delegate* unmanaged[Stdcall]<int, int>)unmanaged[0])(37));
                Assert.Equal(111, ((delegate* unmanaged[Stdcall]<int, int>)unmanaged[1])(37));
                Assert.Equal(137, ((delegate* unmanaged[Stdcall]<int, int>)retained[0])(37));
            }
        }
    }

    [Theory]
    [InlineData("DataPointers")]
    [InlineData("ExecutableData")]
    public static void Data(string name)
    {
        Type type = LoadAssembly(name).GetType(name, throwOnError: true)!;
        Func<int> run = type.GetMethod("Run")!.CreateDelegate<Func<int>>();
        Assert.Equal(100, run());
    }

    private static Assembly LoadAssembly(string name)
    {
        return Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, name + RuntimeInformation.ProcessArchitecture + ".dll"));
    }
}
