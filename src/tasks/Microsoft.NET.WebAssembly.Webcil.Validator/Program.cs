// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Collections.Immutable;
using System.Reflection.PortableExecutable;
using Microsoft.NET.WebAssembly.Webcil;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: Microsoft.NET.WebAssembly.Webcil.Validator <path-to-wasm-file>");
    return 1;
}

string filePath = args[0];

if (!File.Exists(filePath))
{
    Console.Error.WriteLine($"Error: File not found: {filePath}");
    return 1;
}

try
{
    using FileStream stream = File.OpenRead(filePath);
    using var reader = new WebcilReader(stream, filePath);

    Console.WriteLine($"File: {filePath}");
    Console.WriteLine("Webcil header: valid");

    MetadataReader metadata = reader.GetMetadataReader();
    Console.WriteLine("Metadata: valid");

    if (metadata.IsAssembly)
    {
        AssemblyDefinition assemblyDef = metadata.GetAssemblyDefinition();
        Console.WriteLine($"Assembly name: {metadata.GetString(assemblyDef.Name)}");
        Console.WriteLine($"Assembly version: {assemblyDef.Version}");
    }

    ImmutableArray<DebugDirectoryEntry> debugEntries = reader.ReadDebugDirectory();
    Console.WriteLine($"Debug directory entries: {debugEntries.Length}");

    foreach (DebugDirectoryEntry entry in debugEntries)
    {
        Console.WriteLine($"  - Location: {entry.DataRelativeVirtualAddress}, Size: {entry.DataSize}, Type: {entry.Type}, Version: {entry.MajorVersion}.{entry.MinorVersion}");
    }

    Console.WriteLine("Validation succeeded.");
    return 0;
}
catch (BadImageFormatException ex)
{
    Console.Error.WriteLine($"Validation failed: {ex.Message}");
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Unexpected error: {ex.GetType().Name}: {ex.Message}");
    return 1;
}
