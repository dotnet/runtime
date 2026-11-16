// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.WebAssembly.Build.Tasks;

/// <summary>
/// Computes the native modules available to WebAssembly P/Invoke resolution.
/// </summary>
/// <remarks>
/// The outputs are <c>--directpinvoke</c> entries: a module name, or <c>module!entrypoint</c> for a
/// single entry point. With <see cref="IncludeCustomModules"/>, <see cref="DirectPInvokes"/> and
/// <see cref="DirectPInvokeLists"/> supply app entries with the same meaning as the NativeAOT
/// <c>DirectPInvoke</c> and <c>DirectPInvokeList</c> items.
/// </remarks>
public sealed class ComputeWasmPInvokeModules : Task
{
    [Required, NotNull]
    public string? TargetOS { get; set; }

    public bool IncludeCustomModules { get; set; }

    public bool InvariantGlobalization { get; set; }

    public ITaskItem[] FrameworkModules { get; set; } = Array.Empty<ITaskItem>();

    public ITaskItem[] NativeFileReferences { get; set; } = Array.Empty<ITaskItem>();

    public ITaskItem[] NativeLibraries { get; set; } = Array.Empty<ITaskItem>();

    public ITaskItem[] DirectPInvokes { get; set; } = Array.Empty<ITaskItem>();

    public ITaskItem[] DirectPInvokeLists { get; set; } = Array.Empty<ITaskItem>();

    [Output]
    public ITaskItem[] Modules { get; private set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        string platformMetadata = TargetOS switch
        {
            "browser" => "Browser",
            "wasi" => "Wasi",
            _ => string.Empty,
        };

        if (platformMetadata.Length == 0)
        {
            Log.LogError($"Unsupported WebAssembly target OS '{TargetOS}'.");
            return false;
        }

        List<ITaskItem> modules = new();
        HashSet<string> seenModules = new(StringComparer.Ordinal);

        if (IncludeCustomModules)
        {
            AddNativeModules(NativeFileReferences, requireExisting: false);
            AddNativeModules(NativeLibraries, requireExisting: true);

            foreach (ITaskItem directPInvoke in DirectPInvokes)
                AddDirectPInvoke(directPInvoke.ItemSpec);

            foreach (ITaskItem list in DirectPInvokeLists)
            {
                string path = list.GetMetadata("FullPath");
                if (!File.Exists(path))
                {
                    Log.LogError($"DirectPInvokeList file '{path}' does not exist.");
                    return false;
                }

                foreach (string line in File.ReadLines(path))
                    AddDirectPInvoke(line);
            }
        }

        foreach (ITaskItem module in FrameworkModules)
        {
            if (!string.Equals(module.GetMetadata(platformMetadata), "true", StringComparison.OrdinalIgnoreCase))
                continue;

            string moduleName = module.ItemSpec;
            if (InvariantGlobalization && moduleName == "System.Globalization.Native")
                continue;

            AddModule(moduleName);
        }

        Modules = modules.ToArray();
        return true;

        void AddDirectPInvoke(string entry)
        {
            entry = entry.Trim();
            if (entry.Length != 0 && !entry.StartsWith("#", StringComparison.Ordinal))
                AddModule(entry);
        }

        void AddNativeModules(ITaskItem[] nativeItems, bool requireExisting)
        {
            foreach (ITaskItem nativeItem in nativeItems)
            {
                if (string.Equals(nativeItem.GetMetadata("ScanForPInvokes"), "false", StringComparison.OrdinalIgnoreCase))
                    continue;

                string path = nativeItem.GetMetadata("FullPath");
                if (requireExisting && !File.Exists(path))
                    continue;

                string extension = Path.GetExtension(path);
                if (extension is not (".a" or ".o" or ".c" or ".cpp"))
                    continue;

                string moduleName = Path.GetFileNameWithoutExtension(path);
                if (moduleName.StartsWith("lib", StringComparison.Ordinal) && moduleName.Length > 3)
                    moduleName = moduleName.Substring(3);

                AddModule(moduleName);
            }
        }

        void AddModule(string moduleName)
        {
            if (seenModules.Add(moduleName))
                modules.Add(new TaskItem(moduleName));
        }
    }
}
