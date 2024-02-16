// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using Xunit;

public static class MainProgramHandleTests
{
    private static IntPtr s_handle;

    static MainProgramHandleTests() => NativeLibrary.SetDllImportResolver(typeof(MainProgramHandleTests).Assembly,
        (string libraryName, Assembly asm, DllImportSearchPath? dllImportSearchPath) =>
        {
            if (libraryName == "Self")
            {
                s_handle = NativeLibrary.GetMainProgramHandle();
                Assert.NotEqual(IntPtr.Zero, s_handle);
                return s_handle;
            }

            return IntPtr.Zero;
        });

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            free(s_handle);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }

        return 100;
    }

    [DllImport("Self")]
    private static extern void free(IntPtr arg);
}
