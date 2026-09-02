// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.WebAssembly.Build.Tasks.CoreClr;

// Scans managed assemblies for P/Invokes and validates that every referenced native module is
// either statically linked (allow-listed or resolved through the lib-prefix fallback), imported
// via [WasmImportLinkage], "*" or QCall. Any other module produces WASM0066. Unlike
// ManagedToNativeGenerator this emits no C code and no tables -- it reuses the same scanner purely
// to guard a built runtime pack against foreign P/Invokes that survived trimming.
public class PInvokeModuleValidator : Task
{
    [Required]
    public string[] Assemblies { get; set; } = Array.Empty<string>();

    [Required, NotNull]
    public string[]? PInvokeModules { get; set; }

    public string[] IgnoredPInvokeModules { get; set; } = Array.Empty<string>();

    public string TargetOS { get; set; } = "browser";

    private static readonly string[] s_knownTargetOSes = new[] { "browser", "wasi" };

    public override bool Execute()
    {
        if (Assemblies.Length == 0)
        {
            Log.LogError($"{nameof(PInvokeModuleValidator)}.{nameof(Assemblies)} cannot be empty");
            return false;
        }

        if (PInvokeModules!.Length == 0)
        {
            Log.LogError($"{nameof(PInvokeModuleValidator)}.{nameof(PInvokeModules)} cannot be empty");
            return false;
        }

        if (string.IsNullOrWhiteSpace(TargetOS))
        {
            Log.LogError($"{nameof(PInvokeModuleValidator)}.{nameof(TargetOS)} cannot be empty; expected one of: {string.Join(", ", s_knownTargetOSes)}");
            return false;
        }

        TargetOS = TargetOS.Trim().ToLowerInvariant();
        if (Array.IndexOf(s_knownTargetOSes, TargetOS) < 0)
        {
            Log.LogError($"{nameof(PInvokeModuleValidator)}.{nameof(TargetOS)} '{TargetOS}' is not recognized; expected one of: {string.Join(", ", s_knownTargetOSes)}");
            return false;
        }

        try
        {
            var log = new LogAdapter(Log);
            ExecuteInternal(log);
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
        List<string> managedAssemblies = FilterOutUnmanagedBinaries(Assemblies);

        // The symbol-name fixup is only consumed when emitting C tables, which the validator never
        // does, so identity is sufficient here.
        var pinvoke = new PInvokeTableGenerator(static name => name, log, isLibraryMode: false, TargetOS);

        var resolver = new PathAssemblyResolver(managedAssemblies);
        using var mlc = new MetadataLoadContext(resolver, "System.Private.CoreLib");
        foreach (string asmPath in managedAssemblies)
        {
            log.LogMessage(MessageImportance.Low, $"Loading {asmPath} to validate pinvoke modules");
            Assembly asm = mlc.LoadFromAssemblyPath(asmPath);
            pinvoke.ScanAssembly(asm);
        }

        pinvoke.ValidateModules(PInvokeModules, IgnoredPInvokeModules);
    }

    private List<string> FilterOutUnmanagedBinaries(string[] assemblies)
    {
        List<string> managedAssemblies = new(assemblies.Length);
        foreach (string asmPath in assemblies)
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
