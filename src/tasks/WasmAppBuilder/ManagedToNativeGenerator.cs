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

    [Output]
    public string[]? FileWrites { get; private set; }

    private static readonly char[] s_charsToReplace = new[] { '.', '-', '+', '<', '>' };

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
        if (ShouldRun(managedAssemblies))
        {
            var pinvoke = new PInvokeTableGenerator(FixupSymbolName, log);
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
                icall.Generate(IcallOutputPath));

            var m2n = new InterpToNativeGenerator(log);
            m2n.Generate(cookies, InterpToNativeOutputPath);

            if (!string.IsNullOrEmpty(CacheFilePath))
                File.WriteAllLines(CacheFilePath, PInvokeModules);
        }

        List<string> fileWritesList = new() { PInvokeOutputPath, InterpToNativeOutputPath };
        if (IcallOutputPath != null)
            fileWritesList.Add(IcallOutputPath);
        if (!string.IsNullOrEmpty(CacheFilePath))
            fileWritesList.Add(CacheFilePath);

        FileWrites = fileWritesList.ToArray();

        string FixupSymbolName(string name)
        {
            if (_symbolNameFixups.TryGetValue(name, out string? fixedName))
                return fixedName;

            UTF8Encoding utf8 = new();
            byte[] bytes = utf8.GetBytes(name);
            StringBuilder sb = new();

            foreach (byte b in bytes)
            {
                if ((b >= (byte)'0' && b <= (byte)'9') ||
                    (b >= (byte)'a' && b <= (byte)'z') ||
                    (b >= (byte)'A' && b <= (byte)'Z') ||
                    (b == (byte)'_'))
                {
                    sb.Append((char)b);
                }
                else if (s_charsToReplace.Contains((char)b))
                {
                    sb.Append('_');
                }
                else
                {
                    sb.Append($"_{b:X}_");
                }
            }

            fixedName = sb.ToString();
            _symbolNameFixups[name] = fixedName;
            return fixedName;
        }

    }

    private bool ShouldRun(IList<string> managedAssemblies)
    {
        if (string.IsNullOrEmpty(CacheFilePath) || !File.Exists(CacheFilePath))
        {
            Log.LogMessage(MessageImportance.Low, $"Running because no cache file found at '{CacheFilePath}'.");
            return true;
        }

        string oldModules = string.Join(",", File.ReadLines(CacheFilePath).OrderBy(l => l));
        string newModules = string.Join(",", PInvokeModules.OrderBy(l => l));
        if (!string.Equals(oldModules, newModules, StringComparison.InvariantCulture))
        {
            Log.LogMessage(MessageImportance.Low, $"Running because the list of pinvoke modules has changed from: {oldModules} to {newModules} .");
            return true;
        }

        // compare against the output files
        // get the timestamp for oldest output file
        DateTime oldestOutputDt = DateTime.MaxValue;
        if (CheckShouldRunBecauseOfOutputFile(IcallOutputPath, ref oldestOutputDt) ||
            CheckShouldRunBecauseOfOutputFile(PInvokeOutputPath, ref oldestOutputDt) ||
            CheckShouldRunBecauseOfOutputFile(InterpToNativeOutputPath, ref oldestOutputDt))
        {
            return true;
        }

        foreach (string asm in managedAssemblies)
        {
            if (File.GetLastWriteTimeUtc(asm) >= oldestOutputDt)
            {
                Log.LogMessage(MessageImportance.Low, $"Running because {asm} is newer than one of the output files.");
                return true;
            }
        }

        Log.LogMessage(MessageImportance.Low, $"Skipping because all of the assemblies are older than the output files.");
        return false;

        bool CheckShouldRunBecauseOfOutputFile(string? path, ref DateTime oldestDt)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (!File.Exists(path))
            {
                Log.LogMessage(MessageImportance.Low, $"Running because output file '{path}' does not exist.");
                return true;
            }
            DateTime utc = File.GetLastWriteTimeUtc(path);
            oldestDt = utc < oldestDt ? utc : oldestDt;
            return false;
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
