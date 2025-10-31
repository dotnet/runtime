// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices.JavaScript;

public partial class FilesToIncludeInFileSystemTest
{
    [JSExport]
    public static void Run()
    {
        // Check file presence in VFS based on application environment
        PrintFileExistence("/myfiles/Vfs1.txt");
        PrintFileExistence("/myfiles/Vfs2.txt");
        PrintFileExistence("/subdir/subsubdir/Vfs3.txt");
    }

    // Synchronize with FilesToIncludeInFileSystemTests
    private static void PrintFileExistence(string path) => TestOutput.WriteLine($"'{path}' exists '{File.Exists(path)}' with content '{File.ReadAllText(path)}'");
}
