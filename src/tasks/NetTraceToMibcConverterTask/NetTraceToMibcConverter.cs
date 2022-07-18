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

public class NetTraceToMibcConverter : Microsoft.Build.Utilities.Task
{

    /// <summary>
    /// Path to the mibc converter tool
    /// </summary>
    public string? MibcConverterBinaryPath { get; set; }

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
    /// File used to track hashes of assemblies, to act as a cache
    /// Output files don't get written, if they haven't changed
    /// </summary>
    public string? CacheFilePath { get; set; }

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

    private bool ProcessAndValidateArguments()
    {
        if (!File.Exists(MibcConverterBinaryPath))
        {
            Log.LogError($"{nameof(MibcConverterBinaryPath)}='{MibcConverterBinaryPath}' doesn't exist.");
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

    public override bool Execute()
    {
        try
        {
            return ExecuteInternal();
        }
        catch (LogAsErrorException laee)
        {
            Log.LogError(laee.Message);
            return false;
        }
    }

    private bool ExecuteInternal()
    {
        if (!ProcessAndValidateArguments())
            return false;

        if (!ProcessNettrace(NetTracePath))
            return false;

        return !Log.HasLoggedErrors;
    }

    private bool ProcessNettrace(string netTraceFile)
    {
        var outputMibcPath = Path.Combine(OutputDir, Path.ChangeExtension(Path.GetFileName(netTraceFile), ".mibc"));

        StringBuilder pgoArgsStr = new StringBuilder(string.Empty);
        pgoArgsStr.Append($"create-mibc");
        pgoArgsStr.Append($" --trace {netTraceFile} ");
        foreach (var refAsmItem in Assemblies)
        {
            string? fullPath = refAsmItem.GetMetadata("FullPath");
            pgoArgsStr.Append($" --reference \"{fullPath}\" ");
        }
        pgoArgsStr.Append($" --output {outputMibcPath} ");
        (int exitCode, string output) = Utils.TryRunProcess(Log,
                                                            MibcConverterBinaryPath!,
                                                            pgoArgsStr.ToString());

        if (exitCode != 0)
        {
            Log.LogError($"dotnet-pgo({MibcConverterBinaryPath}) failed for {netTraceFile}:{output}");
            return false;
        }

        MibcProfilePath = outputMibcPath;
        Log.LogMessage(MessageImportance.Low, $"Generated {outputMibcPath} from {MibcConverterBinaryPath}");
        return true;
    }
}
