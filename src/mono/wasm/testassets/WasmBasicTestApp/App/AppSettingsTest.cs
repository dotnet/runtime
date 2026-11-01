// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices.JavaScript;

#nullable enable

public partial class AppSettingsTest
{
    [JSExport]
    public static void Run(bool expectVfs, bool inMemory)
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
            if (bytes is null || (expectVfs && !bytes.AsSpan().SequenceEqual(File.ReadAllBytes(file))))
                throw new InvalidOperationException($"Unexpected appsettings bytes for '{file}'.");
            if (file == "appsettings.Empty.json" && bytes.Length != 0)
                throw new InvalidOperationException("Downloaded empty settings should return an empty array.");
        }

        if (GetAppSettingsBytes("appsettings.Missing.json") is not null)
            throw new InvalidOperationException("Missing appsettings should not return bytes.");

        File.WriteAllBytes("appsettings.VfsOnly.json", Array.Empty<byte>());
        if (!inMemory && GetAppSettingsBytes("appsettings.VfsOnly.json") is not { Length: 0 })
            throw new InvalidOperationException("Empty appsettings should return an empty array.");
        if (inMemory && GetAppSettingsBytes("appsettings.VfsOnly.json") is not null)
            throw new InvalidOperationException("The appsettings map should not read arbitrary VFS files.");

        byte[]? originalBytes = GetAppSettingsBytes("appsettings.json");
        File.WriteAllBytes("appsettings.json", new byte[] { 1, 2, 3 });
        byte[]? expectedBytes = inMemory ? originalBytes : new byte[] { 1, 2, 3 };
        if (!GetAppSettingsBytes("appsettings.json").AsSpan().SequenceEqual(expectedBytes))
            throw new InvalidOperationException("Unexpected appsettings bytes after a VFS update.");

        if (inMemory)
        {
            if (GetAppSettingsBytes("/") is not null)
                throw new InvalidOperationException("Unknown appsettings should not return bytes.");
        }
        else
        {
            try
            {
                GetAppSettingsBytes("/");
                throw new InvalidOperationException("Reading a directory should fail.");
            }
            catch (JSException)
            {
            }
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
