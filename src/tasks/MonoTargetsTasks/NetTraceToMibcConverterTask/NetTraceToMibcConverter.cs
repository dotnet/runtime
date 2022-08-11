// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Text.Json.Serialization;

#nullable enable

public class NetTraceToMibcConverter : ToolTask
{
    /// <summary>
    /// List of all assemblies referenced in a .nettrace file. Important when you run traces against an executable on a different machine / device
    /// </summary>
    [Required]
    public ITaskItem[] Assemblies { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Path to .nettrace file which should be converted to .mibc
    /// </summary>
    [Required]
    public string NetTraceFilePath { get; set; } = "";

    /// <summary>
    /// Directory where the mibc file will be placed
    /// </summary>
    [NotNull]
    [Required]
    public string? OutputDir { get; set; }

    /// <summary>
    /// The path to the mibc file generated from the converter.
    /// </summary>
    [Output]
    public string MibcFilePath { get; set; } = "";

    public override string ToolExe { get; set; } = "";

    protected override string ToolName { get; } = "NetTraceToMibcConverter";

    protected override string GenerateFullPathToTool()
    {
        return ToolPath;
    }

    protected override bool ValidateParameters()
    {
        if (string.IsNullOrEmpty(ToolPath))
        {
            Log.LogError($"{nameof(ToolPath)}='{ToolPath}' must be specified.");
            return false;
        }

        if (string.IsNullOrEmpty(ToolExe))
        {
            ToolExe = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? "dotnet-pgo.exe" : "dotnet-pgo";
        }

        string mibcConverterBinaryPath = Path.Combine(ToolPath, ToolExe);

        if (!File.Exists(mibcConverterBinaryPath))
        {
            Log.LogError($"{nameof(mibcConverterBinaryPath)}='{mibcConverterBinaryPath}' doesn't exist.");
            return false;
        }

        if (Assemblies.Length == 0)
        {
            Log.LogError($"'{nameof(Assemblies)}' is required.");
            return false;
        }

        if (!File.Exists(NetTraceFilePath))
        {
            Log.LogError($"{nameof(NetTraceFilePath)}='{NetTraceFilePath}' doesn't exist");
            return false;
        }

        foreach (var asmItem in Assemblies)
        {
            string? fullPath = asmItem.GetMetadata("FullPath");
            if (!File.Exists(fullPath))
                throw new LogAsErrorException($"Could not find {fullPath} to AOT");
        }

        return !Log.HasLoggedErrors;
    }

    protected override string GenerateCommandLineCommands()
    {
        MibcFilePath = Path.Combine(OutputDir, Path.ChangeExtension(Path.GetFileName(NetTraceFilePath), ".mibc"));

        StringBuilder mibcConverterArgsStr = new StringBuilder("create-mibc");
        mibcConverterArgsStr.Append($" --trace \"{NetTraceFilePath}\" ");

        foreach (var refAsmItem in Assemblies)
        {
            string? fullPath = refAsmItem.GetMetadata("FullPath");
            mibcConverterArgsStr.Append($" --reference \"{fullPath}\" ");
        }

        mibcConverterArgsStr.Append($" --output \"{MibcFilePath}\"");

        return mibcConverterArgsStr.ToString();
    }
}
