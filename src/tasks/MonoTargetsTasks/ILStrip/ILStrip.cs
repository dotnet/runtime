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

        if (DisableParallelStripping)
        {
            foreach (var assemblyItem in Assemblies)
            {
                if (!StripAssembly(assemblyItem))
                    return !Log.HasLoggedErrors;
            }
        }
        else
        {
            Parallel.ForEach(Assemblies,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                assemblyItem => StripAssembly(assemblyItem));
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
