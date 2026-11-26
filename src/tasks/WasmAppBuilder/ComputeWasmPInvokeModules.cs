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
public sealed class ComputeWasmPInvokeModules : Task
{
    [Required, NotNull]
    public string? TargetOS { get; set; }

    public bool IncludeCustomModules { get; set; }

    public bool InvariantGlobalization { get; set; }

    public ITaskItem[] FrameworkModules { get; set; } = Array.Empty<ITaskItem>();

    public ITaskItem[] NativeFileReferences { get; set; } = Array.Empty<ITaskItem>();

    public ITaskItem[] NativeLibraries { get; set; } = Array.Empty<ITaskItem>();

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
