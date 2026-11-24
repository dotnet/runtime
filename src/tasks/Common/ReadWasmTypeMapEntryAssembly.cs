// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.WebAssembly.Build.Tasks;

public sealed class ReadWasmTypeMapEntryAssembly : Task
{
    [Required]
    public string TemplatePath { get; set; } = "";

    [Output]
    public string EntryAssembly { get; private set; } = "";

    public override bool Execute()
    {
        const string EntryAssemblyOption = "System.Runtime.InteropServices.TypeMappingEntryAssembly";

        try
        {
            // A template contains the contents of runtimeOptions, not the runtimeOptions wrapper.
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(TemplatePath),
                new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("configProperties", out JsonElement properties) &&
                properties.ValueKind == JsonValueKind.Object &&
                properties.TryGetProperty(EntryAssemblyOption, out JsonElement entryAssembly))
            {
                if (entryAssembly.ValueKind != JsonValueKind.String)
                {
                    Log.LogError($"The '{EntryAssemblyOption}' property in '{TemplatePath}' must be an assembly name string.");
                    return false;
                }

                EntryAssembly = entryAssembly.GetString()!;
            }
        }
        catch (JsonException ex)
        {
            Log.LogError($"Could not parse runtime configuration template '{TemplatePath}': {ex.Message}");
            return false;
        }

        return true;
    }
}
