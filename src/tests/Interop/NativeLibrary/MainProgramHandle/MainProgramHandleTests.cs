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
    static MainProgramHandleTests() => NativeLibrary.SetDllImportResolver(typeof(MainProgramHandleTests).Assembly,
        (string libraryName, Assembly asm, DllImportSearchPath? dllImportSearchPath) =>
        {
            if (libraryName == "Self")
            {
                IntPtr handle = NativeLibrary.GetMainProgramHandle();
                Assert.NotEqual(IntPtr.Zero, handle);
                return handle;
            }

            return IntPtr.Zero;
        });

    public static int Main()
    {
        try
        {
            int parentPid = getppid();
            Console.WriteLine("Parent PID is: {0}", parentPid);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }

        return 100;
    }

    [DllImport("Self")]
    private static extern int getppid();
}
