// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Resources;
using System.Runtime.InteropServices.JavaScript;

public partial class LibraryInitializerTest
{
    [JSExport]
    public static void Run()
    {
        TestOutput.WriteLine($"LIBRARY_INITIALIZER_TEST = {Environment.GetEnvironmentVariable("LIBRARY_INITIALIZER_TEST")}");
    }
}
