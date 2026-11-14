// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;

public sealed class MonoRuntimeComponentManifestReadTask : Microsoft.Build.Utilities.Task
{
    private JsonModelRoot? _json;

    [Required]
    public string? JsonFilePath { get; set; }

    [Output]
    public ITaskItem[]? _MonoRuntimeComponentSharedLibExt => _json?.GetItems(nameof(_MonoRuntimeComponentSharedLibExt));

    [Output]
    public ITaskItem[]? _MonoRuntimeComponentStaticLibExt => _json?.GetItems(nameof(_MonoRuntimeComponentStaticLibExt));

    [Output]
    public ITaskItem[]? _MonoRuntimeComponentLinking => _json?.GetItems(nameof(_MonoRuntimeComponentLinking));

    [Output]
    public ITaskItem[]? _MonoRuntimeAvailableComponents => _json?.GetItems(nameof(_MonoRuntimeAvailableComponents));

    public override bool Execute()
    {
        return JsonToItemsReader.TryRead(JsonFilePath, Log, out _json);
    }
}

public sealed class ReadWasmProps : Microsoft.Build.Utilities.Task
{
    private JsonModelRoot? _json;

    [Required]
    public string? JsonFilePath { get; set; }

    [Output]
    public ITaskItem[]? EmccProperties => _json?.GetItems(nameof(EmccProperties));

    [Output]
    public ITaskItem[]? WasmOptConfigurationFlags => _json?.GetItems(nameof(WasmOptConfigurationFlags));

    [Output]
    public ITaskItem[]? EmccDefaultExportedFunctions => _json?.GetItems(nameof(EmccDefaultExportedFunctions));

    [Output]
    public ITaskItem[]? EmccDefaultExportedRuntimeMethods => _json?.GetItems(nameof(EmccDefaultExportedRuntimeMethods));

    [Output]
    public ITaskItem[]? PropertiesThatTriggerRelinking => _json?.GetItems(nameof(PropertiesThatTriggerRelinking));

    public override bool Execute()
    {
        return JsonToItemsReader.TryRead(JsonFilePath, Log, out _json);
    }
}
