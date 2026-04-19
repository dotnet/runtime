// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.WebAssembly.Build.Tasks.CoreClr;

public class ManagedToNativeGenerator : Task
{
    [Required]
    public string[] Assemblies { get; set; } = Array.Empty<string>();

    [Required, NotNull]
    public string[]? PInvokeModules { get; set; }

    [Required, NotNull]
    public string? PInvokeOutputPath { get; set; }

    [Required, NotNull]
    public string? ReversePInvokeOutputPath { get; set; }

    [Required, NotNull]
    public string? InterpToNativeOutputPath { get; set; }
    public string? CacheFilePath { get; set; }

    public bool IsLibraryMode { get; set; }

    [Output]
    public string[]? FileWrites { get; private set; }

    public override bool Execute()
    {
        if (Assemblies!.Length == 0)
        {
            Log.LogError($"{nameof(ManagedToNativeGenerator)}.{nameof(Assemblies)} cannot be empty");
            return false;
        }

        if (PInvokeModules!.Length == 0)
        {
            Log.LogError($"{nameof(ManagedToNativeGenerator)}.{nameof(PInvokeModules)} cannot be empty");
            return false;
        }

        try
        {
            var logAdapter = new LogAdapter(Log);
            ExecuteInternal(logAdapter);
            return !Log.HasLoggedErrors;
        }
        catch (LogAsErrorException e)
        {
            Log.LogError(e.Message);
            return false;
        }
    }

    private void ExecuteInternal(LogAdapter log)
    {
        Dictionary<string, string> _symbolNameFixups = new();
        List<string> managedAssemblies = FilterOutUnmanagedBinaries(Assemblies);
        var pinvoke = new PInvokeTableGenerator(FixupSymbolName, log, IsLibraryMode);
        var internalCallCollector = new InternalCallSignatureCollector(log);

        var resolver = new PathAssemblyResolver(managedAssemblies);
        using var mlc = new MetadataLoadContext(resolver, "System.Private.CoreLib");
        foreach (string asmPath in managedAssemblies)
        {
            log.LogMessage(MessageImportance.Low, $"Loading {asmPath} to scan for pinvokes and InternalCall methods");
            Assembly asm = mlc.LoadFromAssemblyPath(asmPath);
            pinvoke.ScanAssembly(asm);

            if (asmPath.Contains("System.Private.CoreLib", StringComparison.OrdinalIgnoreCase))
            {
                // Only scan System.Private.CoreLib, as all used InternalCall methods should be defined there,
                // and scanning all assemblies can be expensive, and can trigger failures which should be avoided.
                // System.Private.CoreLib is tested such that this should never fail on that binary.
                internalCallCollector.ScanAssembly(asm);
            }
        }

        // Pregenerated signatures for commonly used shapes used by R2R code to reduce duplication in generated R2R binaries.
        // The signatures should be in the form of a string where the first character represents the return type and the
        // following characters represent the argument types. The type characters should match those used by the
        // SignatureMapper.CharToNativeType method.
        string[] pregeneratedInterpreterToNativeSignatures =
        {
            "ip",
            "iip",
            "iiip",
            "iiiip",
            "vip",
            "viip",
        };

        IEnumerable<string> cookies = pinvoke.Generate(PInvokeModules, PInvokeOutputPath, ReversePInvokeOutputPath);
        cookies = cookies.Concat(internalCallCollector.GetSignatures());
        cookies = cookies.Concat(pregeneratedInterpreterToNativeSignatures);

        var m2n = new InterpToNativeGenerator(log);
        m2n.Generate(cookies, InterpToNativeOutputPath);

        if (!string.IsNullOrEmpty(CacheFilePath))
            File.WriteAllLines(CacheFilePath, PInvokeModules, Encoding.UTF8);

        List<string> fileWritesList = new() { PInvokeOutputPath, InterpToNativeOutputPath };
        if (!string.IsNullOrEmpty(CacheFilePath))
            fileWritesList.Add(CacheFilePath);

        FileWrites = fileWritesList.ToArray();

        string FixupSymbolName(string name)
        {
            if (_symbolNameFixups.TryGetValue(name, out string? fixedName))
                return fixedName;

            fixedName = Utils.FixupSymbolName(name);
            _symbolNameFixups[name] = fixedName;
            return fixedName;
        }
    }

    private List<string> FilterOutUnmanagedBinaries(string[] assemblies)
    {
        List<string> managedAssemblies = new(assemblies.Length);
        foreach (string asmPath in Assemblies)
        {
            if (!File.Exists(asmPath))
                throw new LogAsErrorException($"Cannot find assembly {asmPath}");

            try
            {
                if (!Utils.IsManagedAssembly(asmPath))
                {
                    Log.LogMessage(MessageImportance.Low, $"Skipping unmanaged {asmPath}.");
                    continue;
                }
            }
            catch (Exception ex)
            {
                Log.LogMessage(MessageImportance.Low, $"Failed to read assembly {asmPath}: {ex}");
                throw new LogAsErrorException($"Failed to read assembly {asmPath}: {ex.Message}");
            }

            managedAssemblies.Add(asmPath);
        }

        return managedAssemblies;
    }
}
