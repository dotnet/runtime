// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using CilStrip.Mono.Cecil;
using CilStrip.Mono.Cecil.Binary;
using CilStrip.Mono.Cecil.Cil;
using CilStrip.Mono.Cecil.Metadata;

public class ILStrip : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Assemblies to be stripped.
    /// The assemblies will be modified in place if OutputPath metadata is not set.
    /// </summary>
    [Required]
    public ITaskItem[] Assemblies { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Disable parallel stripping
    /// </summary>
    public bool DisableParallelStripping { get; set; }

    public override bool Execute()
    {
        if (Assemblies.Length == 0)
        {
            throw new ArgumentException($"'{nameof(Assemblies)}' is required.", nameof(Assemblies));
        }

        int allowedParallelism = DisableParallelStripping ? 1 : Math.Min(Assemblies.Length, Environment.ProcessorCount);
        if (BuildEngine is IBuildEngine9 be9)
            allowedParallelism = be9.RequestCores(allowedParallelism);
        ParallelLoopResult result = Parallel.ForEach(Assemblies,
                                                     new ParallelOptions { MaxDegreeOfParallelism = allowedParallelism },
                                                     (assemblyItem, state) =>
                                                     {
                                                         if (!StripAssembly(assemblyItem))
                                                             state.Stop();
                                                     });

        if (!result.IsCompleted && !Log.HasLoggedErrors)
        {
            Log.LogError("Unknown failure occurred while IL stripping assemblies. Check logs to get more details.");
        }

        return !Log.HasLoggedErrors;
    }

    private bool StripAssembly(ITaskItem assemblyItem)
    {
        string assemblyFile = assemblyItem.ItemSpec;
        var outputPath = assemblyItem.GetMetadata("OutputPath");
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = assemblyFile;
            Log.LogMessage(MessageImportance.Low, $"[ILStrip] {assemblyFile}");
        }
        else
        {
            Log.LogMessage(MessageImportance.Low, $"[ILStrip] {assemblyFile} to {outputPath}");
        }

        try
        {
            AssemblyStripper.AssemblyStripper.StripAssembly (assemblyFile, outputPath);
        }
        catch (Exception ex)
        {
            Log.LogMessage(MessageImportance.Low, ex.ToString());
            Log.LogError($"ILStrip failed for {assemblyFile}: {ex.Message}");
            return false;
        }

        return true;
    }

}
