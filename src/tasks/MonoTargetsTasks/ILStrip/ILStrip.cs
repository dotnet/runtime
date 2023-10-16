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
using System.Collections.Concurrent;

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

    /// <summary>
    /// The location to store the trimmed assemblies, when provided.
    /// </summary>
    public string IntermediateOutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Contains the updated list of assemblies comparing to the input variable Assemblies.
    /// Replaced the trimmed ones with their new location.
    ///
    /// Added two metadata for trimmed items:
    /// - UntrimmedAssemblyFilePath
    /// - ILStripped: set to true to indicate this item is trimmed
    /// </summary>
    [Output]
    public ITaskItem[]? UpdatedAssemblies { get; set; }

    private ConcurrentBag<ITaskItem> _updatedAssemblies = new();

    public override bool Execute()
    {
        if (Assemblies.Length == 0)
        {
            throw new ArgumentException($"'{nameof(Assemblies)}' is required.", nameof(Assemblies));
        }

        string trimmedAssemblyFolder = string.Empty;
        if (TrimIndividualMethods)
        {
            if (!string.IsNullOrEmpty(IntermediateOutputPath))
            {
                if (!Directory.Exists(IntermediateOutputPath))
                {
                    Directory.CreateDirectory(IntermediateOutputPath);
                }
            }

            trimmedAssemblyFolder = ComputeTrimmedAssemblyFolderName(IntermediateOutputPath);
            if (!Directory.Exists(trimmedAssemblyFolder))
            {
                Directory.CreateDirectory(trimmedAssemblyFolder);
            }
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
                                                            if (!TrimMethods(assemblyItem, trimmedAssemblyFolder))
                                                                state.Stop();
                                                        }
                                                    });

        if (TrimIndividualMethods)
        {
            UpdatedAssemblies = _updatedAssemblies.ToArray();
        }

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

    private bool TrimMethods(ITaskItem assemblyItem, string trimmedAssemblyFolder)
    {
        ITaskItem newAssmeblyItem = assemblyItem;

        string assemblyFilePathArg = assemblyItem.ItemSpec;
        string methodTokenFile = assemblyItem.GetMetadata("MethodTokenFile");
        if (string.IsNullOrEmpty(methodTokenFile))
        {
            Log.LogError($"Metadata MethodTokenFile of {assemblyItem.ItemSpec} is empty");
            return true;
        }
        if (!File.Exists(methodTokenFile))
        {
            Log.LogError($"{methodTokenFile} doesn't exist.");
            return true;
        }

        using StreamReader sr = new(methodTokenFile);
        string? assemblyFilePath = sr.ReadLine();
        if (string.IsNullOrEmpty(assemblyFilePath))
        {
            Log.LogError($"The first line of {assemblyFilePath} is empty.");
            return true;
        }

        if (!File.Exists(assemblyFilePath))
        {
            Log.LogError($"{assemblyFilePath} read from {methodTokenFile} doesn't exist.");
            return true;
        }

        string trimmedAssemblyFilePath = ComputeTrimmedAssemblyPath(trimmedAssemblyFolder, assemblyFilePath);
        if (File.Exists(trimmedAssemblyFilePath))
        {
            if (IsInputNewerThanOutput(assemblyFilePath, trimmedAssemblyFilePath))
            {
                File.Delete(trimmedAssemblyFilePath);
            }
            else
            {
                return true;
            }
        }

        bool isTrimmed = false;
        using FileStream fs = File.Open(assemblyFilePath, FileMode.Open);
        using PEReader peReader = new(fs, PEStreamOptions.LeaveOpen);
        MetadataReader mr = peReader.GetMetadataReader();
        string actualGuidValue = ComputeGuid(mr);
        string? expectedGuidValue = sr.ReadLine();
        if (!string.Equals(actualGuidValue, expectedGuidValue, StringComparison.OrdinalIgnoreCase))
        {
            Log.LogError($"[ILStrip] GUID value of {assemblyFilePath} doesn't match the value listed in {methodTokenFile}.");
            return true;
        }

        string? line = sr.ReadLine();
        if (!string.IsNullOrEmpty(line))
        {
            isTrimmed = true;
            Dictionary<int, int> methodBodyUses = ComputeMethodBodyUsage(mr, sr, line, methodTokenFile);
            CreateTrimmedAssembly(peReader, trimmedAssemblyFilePath, fs, methodBodyUses);
        }

        if (isTrimmed)
        {
            newAssmeblyItem.ItemSpec = trimmedAssemblyFilePath;
            newAssmeblyItem.SetMetadata("UntrimmedAssemblyFilePath", assemblyFilePathArg);
            newAssmeblyItem.SetMetadata("ILStripped", "true");
        }

        _updatedAssemblies.Add(newAssmeblyItem);
        return true;
    }

    private static string ComputeTrimmedAssemblyFolderName(string IntermediateOutputPath)
    {
        if (string.IsNullOrEmpty(IntermediateOutputPath))
        {
            return "trimmedAssemblies";
        }
        else
        {
            return Path.Combine(IntermediateOutputPath,  "trimmedAssemblies");
        }
    }

    private static string ComputeTrimmedAssemblyPath(string trimmedAssemblyFolder, string assemblyFilePath)
    {
        string? assemblyName = Path.GetFileName(assemblyFilePath);
        return Path.Combine(trimmedAssemblyFolder, assemblyName);
    }

    private static bool IsInputNewerThanOutput(string inFile, string outFile)
    {
        DateTime lastWriteTimeSrc = File.GetLastWriteTimeUtc(inFile);
        DateTime lastWriteTimeDst = File.GetLastWriteTimeUtc(outFile);

        if (lastWriteTimeSrc > lastWriteTimeDst)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private static string ComputeGuid(MetadataReader mr)
    {
        GuidHandle mvidHandle = mr.GetModuleDefinition().Mvid;
        Guid mvid = mr.GetGuid(mvidHandle);
        return mvid.ToString();
    }

    private Dictionary<int, int> ComputeMethodBodyUsage(MetadataReader mr, StreamReader sr, string? line, string methodTokenFile)
    {
        Dictionary<int, int> tokenToRva = new();
        Dictionary<int, int> methodBodyUses = new();

        foreach (MethodDefinitionHandle mdefh in mr.MethodDefinitions)
        {
            int methodToken = MetadataTokens.GetToken(mr, mdefh);
            MethodDefinition mdef = mr.GetMethodDefinition(mdefh);
            int rva = mdef.RelativeVirtualAddress;

            tokenToRva.Add(methodToken, rva);

            if (methodBodyUses.TryGetValue(rva, out var _))
            {
                methodBodyUses[rva]++;
            }
            else
            {
                methodBodyUses.Add(rva, 1);
            }
        }

        do
        {
            int methodToken2Trim = Convert.ToInt32(line, 16);
            if (methodToken2Trim <= 0)
            {
                Log.LogError($"Method token: {line} in {methodTokenFile} is not a valid hex value.");
            }
            if (tokenToRva.TryGetValue(methodToken2Trim, out int rva2Trim))
            {
                methodBodyUses[rva2Trim]--;
            }
            else
            {
                Log.LogError($"Method token: {line} in {methodTokenFile} can't be found within the assembly.");
            }
        } while ((line = sr.ReadLine()) != null);

        return methodBodyUses;
    }

    private void CreateTrimmedAssembly(PEReader peReader, string trimmedAssemblyFilePath, FileStream fs, Dictionary<int, int> methodBodyUses)
    {
        using FileStream os = File.Open(trimmedAssemblyFilePath, FileMode.Create);
        {
            fs.Position = 0;
            MemoryStream memStream = new MemoryStream((int)fs.Length);
            fs.CopyTo(memStream);

            foreach (var kvp in methodBodyUses)
            {
                int rva = kvp.Key;
                int count = kvp.Value;
                if (count == 0)
                {
                    int methodSize = ComputeMethodSize(peReader, rva);
                    int actualLoc = ComputeMethodHash(peReader, rva);
                    int headerSize = ComputeMethodHeaderSize(memStream, actualLoc);
                    if (headerSize == 1) //Set code size to zero for TinyFormat
                        SetCodeSizeToZeroForTiny(ref memStream, actualLoc);
                    ZeroOutMethodBody(ref memStream, methodSize, actualLoc, headerSize);
                }
                else if (count < 0)
                {
                    Log.LogError($"Method usage count is less than zero for rva: {rva}.");
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

    private static void SetCodeSizeToZeroForTiny(ref MemoryStream memStream, int actualLoc)
    {
        memStream.Position = actualLoc;
        byte[] header = {0b10};
        memStream.Write(header, 0, 1);
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
}
