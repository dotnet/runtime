// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices.JavaScript;

public partial class AppSettingsTest
{
    [JSExport]
    public static void Run()
    {
        // Check file presence in VFS based on application environment
        PrintFileExistence("/appsettings.json");
        PrintFileExistence("/appsettings.Development.json");
        PrintFileExistence("/appsettings.Production.json");
    }

    // Synchronize with AppSettingsTests
    private static void PrintFileExistence(string path) => TestOutput.WriteLine($"'{path}' exists '{File.Exists(path)}'");
}
