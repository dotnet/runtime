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
using System.Buffers;

public class ILStrip : Microsoft.Build.Utilities.Task
{
    [Required]
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
                                                        if (!TrimIndividualMethods)
                                                        {
                                                            if (!StripAssembly(assemblyItem))
                                                                state.Stop();
                                                        }
                                                        else
                                                        {
                                                            if (!TrimMethods(assemblyItem))
                                                                state.Stop();
                                                        }
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

    private bool TrimMethods(ITaskItem assemblyItem)
    {
        string methodTokenFile = assemblyItem.GetMetadata("MethodTokenFile");
        if (!File.Exists(methodTokenFile))
        {
            Log.LogMessage(MessageImportance.Low, $"{methodTokenFile} doesn't exist.");
            return true;
        }

        using (StreamReader sr = new StreamReader(methodTokenFile))
        {
            string? assemblyFilePath = sr.ReadLine();
            if (string.IsNullOrEmpty(assemblyFilePath))
            {
                return true;
            }

            if (!File.Exists(assemblyFilePath))
            {
                Log.LogMessage(MessageImportance.Low, $"{assemblyFilePath} read from {methodTokenFile} doesn't exist.");
                return true;
            }

            string trimmedAssemblyFilePath = ComputeTrimmedAssemblyPath(assemblyFilePath);
            bool isTrimmed = false;

            using (FileStream fs = File.Open(assemblyFilePath, FileMode.Open))
            {
                PEReader peReader = new PEReader(fs, PEStreamOptions.LeaveOpen);
                MetadataReader mr = peReader.GetMetadataReader();

                string guidValue = ComputeGuid(mr);
                string? expectedGuidValue = sr.ReadLine();
                if (!string.Equals(guidValue, expectedGuidValue, StringComparison.OrdinalIgnoreCase))
                {
                    Log.LogMessage(MessageImportance.Low, $"[ILStrip] GUID value of {assemblyFilePath} doesn't match the value listed in {methodTokenFile}.");
                    return true;
                }

                string? line = sr.ReadLine();
                if (!string.IsNullOrEmpty(line))
                {
                    isTrimmed = true;
                    Dictionary<int, int> method_body_uses = ComputeMethodBodyUsage(mr, sr, line);
                    CreateTrimmedAssembly(peReader, trimmedAssemblyFilePath, fs, method_body_uses);
                }
            }
            if (isTrimmed)
            {
                ReplaceAssemblyWithTrimmedOne(assemblyFilePath, trimmedAssemblyFilePath);
            }
        }

        return true;
    }

    private static string ComputeTrimmedAssemblyPath(string assemblyFilePath)
    {
        string? assemblyPath = Path.GetDirectoryName(assemblyFilePath);
        string? assemblyName = Path.GetFileNameWithoutExtension(assemblyFilePath);
        if (string.IsNullOrEmpty(assemblyPath))
        {
            return (assemblyName + "_new.dll");
        }
        else
        {
            return Path.Combine(assemblyPath, (assemblyName + "_new.dll"));
        }
    }

    private static string ComputeGuid(MetadataReader mr)
    {
        GuidHandle mvidHandle = mr.GetModuleDefinition().Mvid;
        Guid mvid = mr.GetGuid(mvidHandle);
        return mvid.ToString();
    }

    private static Dictionary<int, int> ComputeMethodBodyUsage(MetadataReader mr, StreamReader sr, string? line)
    {
        Dictionary<int, int> token_to_rva = new Dictionary<int, int>();
        Dictionary<int, int> method_body_uses = new Dictionary<int, int>();

        foreach (MethodDefinitionHandle mdefh in mr.MethodDefinitions)
        {
            int methodToken = MetadataTokens.GetToken(mr, mdefh);
            MethodDefinition mdef = mr.GetMethodDefinition(mdefh);
            int rva = mdef.RelativeVirtualAddress;

            token_to_rva.Add(methodToken, rva);

            if (method_body_uses.TryGetValue(rva, out var _))
            {
                method_body_uses[rva]++;
            }
            else
            {
                method_body_uses.Add(rva, 1);
            }
        }

        do
        {
            int methodToken2Trim = Convert.ToInt32(line, 16);
            int rva2Trim = token_to_rva[methodToken2Trim];
            method_body_uses[rva2Trim]--;
        } while ((line = sr.ReadLine()) != null);

        return method_body_uses;
    }

    private static void CreateTrimmedAssembly(PEReader peReader, string trimmedAssemblyFilePath, FileStream fs, Dictionary<int, int> method_body_uses)
    {
        using (FileStream os = File.Open(trimmedAssemblyFilePath, FileMode.Create))
        {
            fs.Position = 0;
            MemoryStream memStream = new MemoryStream((int)fs.Length);
            fs.CopyTo(memStream);

            foreach (var kvp in method_body_uses)
            {
                int rva = kvp.Key;
                int count = kvp.Value;
                if (count == 0)
                {
                    int methodSize = ComputeMethodSize(peReader, rva);
                    int actualLoc = ComputeMethodHash(peReader, rva);
                    int headerSize = ComputeMethodHeaderSize(memStream, actualLoc);
                    ZeroOutMethodBody(ref memStream, methodSize, actualLoc, headerSize);
                }
            }

            memStream.Position = 0;
            memStream.CopyTo(os);
        }
    }

    private static int ComputeMethodSize(PEReader peReader, int rva) => peReader.GetMethodBody(rva).Size;

    private static int ComputeMethodHash(PEReader peReader, int rva)
    {
        int sectionIndex = peReader.PEHeaders.GetContainingSectionIndex(rva);
        int relativeOffset = rva - peReader.PEHeaders.SectionHeaders[sectionIndex].VirtualAddress;
        return (peReader.PEHeaders.SectionHeaders[sectionIndex].PointerToRawData + relativeOffset);
    }

    private static int ComputeMethodHeaderSize(MemoryStream memStream, int actualLoc)
    {
        memStream.Position = actualLoc;
        int firstbyte = memStream.ReadByte();
        int headerFlag = firstbyte & 0b11;
        return (headerFlag == 2 ? 1 : 4);
    }

    private static void ZeroOutMethodBody(ref MemoryStream memStream, int methodSize, int actualLoc, int headerSize)
    {
        memStream.Position = actualLoc + headerSize;

        byte[] zeroBuffer;
        zeroBuffer = ArrayPool<byte>.Shared.Rent(methodSize);
        Array.Clear(zeroBuffer, 0, zeroBuffer.Length);
        memStream.Write(zeroBuffer, 0, methodSize - headerSize);
        ArrayPool<byte>.Shared.Return(zeroBuffer);
    }

    private static void ReplaceAssemblyWithTrimmedOne(string assemblyFilePath, string trimmedAssemblyFilePath)
    {
        File.Delete(assemblyFilePath);
        File.Move(trimmedAssemblyFilePath, assemblyFilePath);
    }
}
