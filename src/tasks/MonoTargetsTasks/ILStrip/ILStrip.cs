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
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

public class ILStrip : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Assemblies to be stripped.
    /// The assemblies will be modified in place if OutputPath metadata is not set.
    /// </summary>
    public ITaskItem[] Assemblies { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Disable parallel stripping
    /// </summary>
    public bool DisableParallelStripping { get; set; }

    /// <summary>
    /// Enable the feature of trimming indiviual methods
    /// </summary>
    public bool TrimIndividualMethods { get; set; }

    /// <summary>
    /// Methods to be trimmed, identified by method token
    /// </summary>
    public ITaskItem[] MethodTokenFiles { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Directory where all the assemblies are stored
    /// </summary>
    public string AssemblyPath { get; set; } = "";

    public override bool Execute()
    {
        if (!TrimIndividualMethods)
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
        else
        {
            if (MethodTokenFiles.Length == 0)
            {
                throw new ArgumentException($"'{nameof(MethodTokenFiles)}' is required.", nameof(MethodTokenFiles));
            }

            if (!Directory.Exists(AssemblyPath))
            {
                throw new ArgumentException($"'{nameof(AssemblyPath)}' needs to be a valid path.", nameof(AssemblyPath));
            }

            int allowedParallelism = DisableParallelStripping ? 1 : Math.Min(MethodTokenFiles.Length, Environment.ProcessorCount);
            if (BuildEngine is IBuildEngine9 be9)
                allowedParallelism = be9.RequestCores(allowedParallelism);
            ParallelLoopResult result = Parallel.ForEach(MethodTokenFiles,
                                                        new ParallelOptions { MaxDegreeOfParallelism = allowedParallelism },
                                                        (methodTokenFileItem, state) =>
                                                        {
                                                            if (!TrimMethods(methodTokenFileItem, AssemblyPath))
                                                                state.Stop();
                                                        });

            if (!result.IsCompleted && !Log.HasLoggedErrors)
            {
                Log.LogError("Unknown failure occurred while IL stripping assemblies. Check logs to get more details.");
            }

            return !Log.HasLoggedErrors;
        }
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

    private bool TrimMethods(ITaskItem methodTokenFileItem, string assemblyPath)
    {
        string methodTokenFile = methodTokenFileItem.ItemSpec;
        if (!File.Exists(methodTokenFile))
        {
            Log.LogMessage(MessageImportance.Low, $"[ILStrip] {methodTokenFile} doesn't exit.");
            return true;
        }
        string[] log = File.ReadAllLines(methodTokenFile);
        if (log.Length <= 1)
        {
            // Frist line is assembly name
            Log.LogMessage(MessageImportance.Low, $"[ILStrip] {methodTokenFile} doesn't contain any compiled method token.");
            return true;
        }
        string assemblyName = log[0];
        string assemblyFilePath = Path.Combine(assemblyPath, (assemblyName + ".dll"));
        string trimmedAssemblyFilePath = Path.Combine(assemblyPath, (assemblyName + "_new.dll"));
        if (!File.Exists(assemblyFilePath))
        {
            Log.LogMessage(MessageImportance.Low, $"[ILStrip] {assemblyFilePath} doesn't exit.");
            return true;
        }

        using (FileStream fs = File.Open(assemblyFilePath, FileMode.Open),
                os = File.Open(trimmedAssemblyFilePath, FileMode.Create))
        {
            MemoryStream memStream = new MemoryStream((int)fs.Length);
            fs.CopyTo(memStream);

            fs.Position = 0;
            PEReader peReader = new PEReader(fs, PEStreamOptions.LeaveOpen);
            MetadataReader mr = peReader.GetMetadataReader();

            Dictionary<int, int> token_to_rva = new Dictionary<int, int>();
            Dictionary<int, int> method_body_uses = new Dictionary<int, int>();

            foreach (MethodDefinitionHandle mdefh in mr.MethodDefinitions)
            {
                int methodToken = MetadataTokens.GetToken(mr, mdefh);
                MethodDefinition mdef = mr.GetMethodDefinition(mdefh);
                int rva = mdef.RelativeVirtualAddress;

                token_to_rva.Add(methodToken, rva);

                if (method_body_uses.TryGetValue(rva, out var count))
                {
                    method_body_uses[rva]++;
                }
                else
                {
                    method_body_uses.Add(rva, 1);
                }
            }

            for (int i = 1; i < log.Length; i++)
            {
                int methodToken2Trim = Convert.ToInt32(log[i]);
                int rva2Trim = token_to_rva[methodToken2Trim];
                method_body_uses[rva2Trim]--;
            }

            foreach (var kvp in method_body_uses)
            {
                int rva = kvp.Key;
                int count = kvp.Value;
                if (count == 0)
                {
                    MethodBodyBlock mb = peReader.GetMethodBody(rva);
                    int methodSize = mb.Size;
                    int sectionIndex = peReader.PEHeaders.GetContainingSectionIndex(rva);
                    int relativeOffset = rva - peReader.PEHeaders.SectionHeaders[sectionIndex].VirtualAddress;
                    int actualLoc = peReader.PEHeaders.SectionHeaders[sectionIndex].PointerToRawData + relativeOffset;

                    byte[] zeroBuffer = new byte[methodSize];
                    for (int i = 0; i < methodSize; i++)
                    {
                        zeroBuffer[i] = 0x0;
                    }

                    memStream.Position = actualLoc;
                    int firstbyte = memStream.ReadByte();
                    int headerFlag = firstbyte & 0b11;
                    int headerSize = headerFlag == 2 ? 1 : 4;

                    memStream.Position = actualLoc + headerSize;
                    memStream.Write(zeroBuffer, 0, methodSize - headerSize);
                }
            }
            memStream.Position = 0;
            memStream.CopyTo(os);
        }

        return true;
    }
}
