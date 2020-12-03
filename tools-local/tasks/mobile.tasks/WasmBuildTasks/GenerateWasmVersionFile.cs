// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

// GenerateWasmVersionFile is needed because emcc --version
// outputs too much and we only need the first line.
public class GenerateWasmVersionFile : Task
{
    [Required]
    public string? SourceFile { get; set; }

    [Required]
    public string? VersionFile { get; set; }

    public override bool Execute()
    {
        string version = File.ReadLines(SourceFile!).First();
        File.WriteAllText(VersionFile!, version);

        return true;
    }
}
