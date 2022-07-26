// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Text.Json.Serialization;


public class NetTraceToMibcConverter : ToolTask
{
    /// <summary>
    /// Entries for assemblies referenced in a .nettrace file. Important when you run traces against an executable on a different machine / device
    /// </summary>
    [Required]
    public ITaskItem[] Assemblies { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// NetTrace file to use when invoking dotnet-pgo for
    /// </summary>
    [Required]
    public string NetTracePath { get; set; } = "";

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
    public string? MibcProfilePath { get; set; }

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
            Log.LogError($"{nameof(ToolExe)}='{ToolExe}' must be specified.");
            return false;
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

        if (!File.Exists(NetTracePath))
        {
            Log.LogError($"{nameof(NetTracePath)}='{NetTracePath}' doesn't exist");
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
        var outputMibcPath = Path.Combine(OutputDir, Path.ChangeExtension(Path.GetFileName(NetTracePath), ".mibc"));

        StringBuilder mibcConverterArgsStr = new StringBuilder(string.Empty);
        mibcConverterArgsStr.Append($"create-mibc");
        mibcConverterArgsStr.Append($" --trace {NetTracePath} ");

        foreach (var refAsmItem in Assemblies)
        {
            string? fullPath = refAsmItem.GetMetadata("FullPath");
            mibcConverterArgsStr.Append($" --reference \"{fullPath}\" ");
        }

        mibcConverterArgsStr.Append($" --output {outputMibcPath} ");

        return mibcConverterArgsStr.ToString();
    }
}
