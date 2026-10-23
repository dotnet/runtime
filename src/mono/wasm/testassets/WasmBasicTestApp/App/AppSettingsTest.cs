// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices.JavaScript;

#nullable enable

public partial class AppSettingsTest
{
    [JSExport]
    public static void Run()
    {
        const string browserVirtualAppBase = "/"; // keep in sync other places that define browserVirtualAppBase

        // Check file presence in VFS based on application environment
        PrintFileExistence(browserVirtualAppBase + "appsettings.json");
        PrintFileExistence(browserVirtualAppBase + "appsettings.Development.json");
        PrintFileExistence(browserVirtualAppBase + "appsettings.Production.json");

        string[] files = GetAppSettingsFiles();
        TestOutput.WriteLine($"Appsettings files: {string.Join(",", files)}");
        foreach (string file in files)
        {
            byte[]? bytes = GetAppSettingsBytes(file);
            if (bytes is null || !bytes.AsSpan().SequenceEqual(File.ReadAllBytes(file)))
                throw new InvalidOperationException($"Appsettings bytes do not match VFS for '{file}'.");

        }

        if (GetAppSettingsBytes("appsettings.Missing.json") is not null)
            throw new InvalidOperationException("Missing appsettings should not return bytes.");

        File.WriteAllBytes("appsettings.Empty.json", Array.Empty<byte>());
        if (GetAppSettingsBytes("appsettings.Empty.json") is not { Length: 0 })
            throw new InvalidOperationException("Empty appsettings should return an empty array.");

        File.WriteAllBytes("appsettings.json", new byte[] { 1, 2, 3 });
        if (!GetAppSettingsBytes("appsettings.json").AsSpan().SequenceEqual(new byte[] { 1, 2, 3 }))
            throw new InvalidOperationException("Appsettings bytes do not reflect VFS updates.");

        try
        {
            GetAppSettingsBytes("/");
            throw new InvalidOperationException("Reading a directory should fail.");
        }
        catch (JSException)
        {
        }

        TestOutput.WriteLine("Appsettings byte API passed");
    }

    [JSImport("INTERNAL.getAppSettingsFiles")]
    private static partial string[] GetAppSettingsFiles();

    [JSImport("INTERNAL.getAppSettingsBytes")]
    private static partial byte[]? GetAppSettingsBytes(string fileName);

    // Synchronize with AppSettingsTests
    private static void PrintFileExistence(string path) => TestOutput.WriteLine($"'{path}' exists '{File.Exists(path)}'");
}
