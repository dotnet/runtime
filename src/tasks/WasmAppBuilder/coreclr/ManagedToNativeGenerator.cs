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
using WasmAppBuilder;

namespace Microsoft.WebAssembly.Build.Tasks;

public class ManagedToNativeGenerator : Task
{
    [Required]
    public string[] Assemblies { get; set; } = Array.Empty<string>();

    public string? RuntimeIcallTableFile { get; set; }

    public string? IcallOutputPath { get; set; }

    [Required, NotNull]
    public string[]? PInvokeModules { get; set; }

    [Required, NotNull]
    public string? PInvokeOutputPath { get; set; }

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

    // WASM-TODO:
    // add missing signatures temporarily
    // part is for runtime tests and delegates
    // active issue https://github.com/dotnet/runtime/issues/121222
    private static readonly string[] missingCookies =
                    [
                        "d",
                        "dii",
                        "f",
                        "id",
                        "idi",
                        "if",
                        "iff",
                        "iid",
                        "iif",
                        "iifiif",
                        "iiiiiiiiiiiiiiiiii",
                        "iin",
                        "iinini",
                        "iinn",
                        "il",
                        "lii",
                        "ll",
                        "lli",
                        "n",
                        "ni",
                        "nii",
                        "niii",
                        "nn",
                        "nni",
                        "vd",
                        "vf",
                        "viiiiiii",
                        "viin",
                        "vin",
                        "vinni",
                        "iinini",
                    ];

    private void ExecuteInternal(LogAdapter log)
    {
        Dictionary<string, string> _symbolNameFixups = new();
        List<string> managedAssemblies = FilterOutUnmanagedBinaries(Assemblies);
        var pinvoke = new PInvokeTableGenerator(FixupSymbolName, log, IsLibraryMode);
        var icall = new IcallTableGenerator(RuntimeIcallTableFile, FixupSymbolName, log);

        var resolver = new PathAssemblyResolver(managedAssemblies);
        using var mlc = new MetadataLoadContext(resolver, "System.Private.CoreLib");
        foreach (string asmPath in managedAssemblies)
        {
            log.LogMessage(MessageImportance.Low, $"Loading {asmPath} to scan for pinvokes, and icalls");
            Assembly asm = mlc.LoadFromAssemblyPath(asmPath);
            pinvoke.ScanAssembly(asm);
            icall.ScanAssembly(asm);
        }

        IEnumerable<string> cookies = Enumerable.Concat(
            pinvoke.Generate(PInvokeModules, PInvokeOutputPath),
            Enumerable.Concat(icall.Generate(IcallOutputPath),
            missingCookies));

        var m2n = new InterpToNativeGenerator(log);
        m2n.Generate(cookies, InterpToNativeOutputPath);

        if (!string.IsNullOrEmpty(CacheFilePath))
            File.WriteAllLines(CacheFilePath, PInvokeModules);

        List<string> fileWritesList = new() { PInvokeOutputPath, InterpToNativeOutputPath };
        if (!string.IsNullOrEmpty(IcallOutputPath))
            fileWritesList.Add(IcallOutputPath);
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
