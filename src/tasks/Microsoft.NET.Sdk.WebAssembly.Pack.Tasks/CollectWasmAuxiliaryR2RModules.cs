// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Sdk.WebAssembly;

public sealed class CollectWasmAuxiliaryR2RModules : Task
{
    [Required]
    public ITaskItem[] PrimaryImages { get; set; }

    public bool AllowMissingFiles { get; set; }

    [Output]
    public ITaskItem[] Modules { get; set; } = Array.Empty<ITaskItem>();

    [Output]
    public ITaskItem[] FileWrites { get; set; } = Array.Empty<ITaskItem>();

    [Output]
    public ITaskItem[] MissingOutputs { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        var modules = new List<ITaskItem>();
        var writes = new List<ITaskItem>();
        var missing = new List<ITaskItem>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (ITaskItem primary in PrimaryImages)
            {
                string path = Path.GetFullPath(primary.ItemSpec);
                string manifest = Path.ChangeExtension(path, ".auxiliary.json");
                if (AllowMissingFiles && !File.Exists(manifest))
                {
                    missing.Add(new TaskItem(manifest));
                    continue;
                }
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(manifest));
                writes.Add(new TaskItem(manifest));
                AddModule(document.RootElement.GetProperty("entrypoints").GetString());
                foreach (JsonElement module in document.RootElement.GetProperty("modules").EnumerateArray())
                    AddModule(module.GetString());

                void AddModule(string? name)
                {
                    if (string.IsNullOrEmpty(name) || name != Path.GetFileName(name) ||
                        name.IndexOfAny(new[] { '/', '\\', ':', '?', '#' }) >= 0 ||
                        name.StartsWith(".", StringComparison.Ordinal) ||
                        !name.EndsWith(".wasm", StringComparison.Ordinal) || !names.Add(name))
                    {
                        throw new InvalidDataException($"Invalid or duplicate auxiliary module filename '{name}' in '{manifest}'.");
                    }
                    string modulePath = Path.Combine(Path.GetDirectoryName(path)!, name);
                    if (!File.Exists(modulePath))
                    {
                        if (!AllowMissingFiles)
                            throw new FileNotFoundException($"Auxiliary ReadyToRun module listed in '{manifest}' does not exist.", modulePath);
                        missing.Add(new TaskItem(modulePath));
                    }

                    var item = new TaskItem(modulePath);
                    item.SetMetadata("ContentRoot", Path.GetDirectoryName(modulePath) + Path.DirectorySeparatorChar);
                    item.SetMetadata("RelativePath", "_framework/" + name);
                    item.SetMetadata("OriginalItemSpec", modulePath);
                    modules.Add(item);
                    writes.Add(new TaskItem(modulePath));
                }
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException or InvalidOperationException or KeyNotFoundException or UnauthorizedAccessException)
        {
            Log.LogErrorFromException(exception, showStackTrace: false);
        }
        Modules = modules.ToArray();
        FileWrites = writes.ToArray();
        MissingOutputs = missing.ToArray();
        return !Log.HasLoggedErrors;
    }
}
