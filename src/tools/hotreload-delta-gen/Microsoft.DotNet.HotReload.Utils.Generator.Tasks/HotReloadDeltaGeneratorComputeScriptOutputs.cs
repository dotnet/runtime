// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;


namespace Microsoft.DotNet.HotReload.Utils.Generator.Tasks;

/// Given a DeltaScript, counts the number of elements and returns items for the .dmeta, .dil, and .dpdb
/// files that the Generator would produce.
public class HotReloadDeltaGeneratorComputeScriptOutputs : Microsoft.Build.Utilities.Task
{
    /// The name of the assembly produced by the current project
    [Required]
    public string BaseAssemblyName { get; set; }
    /// The name of the json delta script
    [Required]
    public string DeltaScript {get; set; }


    /// The generated delta outputs
    ///  Each item has a DeltaOutputType metadata with a value of "dmeta", "dil", "dpdb" or "updateHandlerJson"
    ///    indicating what kind of delta output it is.
    [Output]
    public ITaskItem[] DeltaOutputs { get; set; }

    /// The (relative to the script file) sources that comprise the changes.
    ///  Each item has a DeltaForBaseline metadata that has the name of the baseline source file
    [Output]
    public ITaskItem[] DeltaSources { get; set; }

    public HotReloadDeltaGeneratorComputeScriptOutputs()
    {
        BaseAssemblyName = string.Empty;
        DeltaScript = string.Empty;
        DeltaOutputs = Array.Empty<ITaskItem>();
        DeltaSources = Array.Empty<ITaskItem>();
    }

    enum DeltaOutputType {
        dmeta,
        dil,
        dpdb,
        updateHandlerJson,
    }

    public override bool Execute()
    {
        if (!System.IO.File.Exists(DeltaScript))
        {
            Log.LogError("Hot reload delta script {0} does not exist", DeltaScript);
            return false;
        }
        string baseAssemblyName = BaseAssemblyName;
        Script.Json.Script? json;
        try
        {
            json = Parse(DeltaScript).Result;
            if (json?.Changes == null) {
                Log.LogError("Hot reload delta script had no 'changes' array");
                return false;
            }
        }
        catch (JsonException exn)
        {
            Log.LogErrorFromException(exn, showStackTrace: true);
            return false;
        }

        DeltaOutputs = ComputeOutputs (baseAssemblyName, json.Changes.Length);
        DeltaSources = ComputeSources (json.Changes);
        return true;
    }

    private static ITaskItem[] ComputeOutputs (string baseAssemblyName, int count)
    {
        const string deltaOutputTypeMetadata = "DeltaOutputType";
        DeltaOutputType[] outputTypes = new DeltaOutputType[] {
            DeltaOutputType.dmeta,
            DeltaOutputType.dil,
            DeltaOutputType.dpdb,
            DeltaOutputType.updateHandlerJson,
        };
        int itemsPerRev = outputTypes.Length;
        ITaskItem[] result = new TaskItem[itemsPerRev*count];
        for (int i = 0; i < count; ++i)
        {
            int rev = 1+i;
            foreach (var outputType in outputTypes)
            {
                int index = i*itemsPerRev + (int)outputType;
                string name = NameForOutput(baseAssemblyName, rev, outputType);
                result[index] = new TaskItem(name, new Dictionary<string,string> { { deltaOutputTypeMetadata, outputType.ToString() } });
            }
        }
        return result;
    }

    private static string NameForOutput(string baseName, int rev, DeltaOutputType t)
    {
        string ext = t switch {
            DeltaOutputType.dmeta => "dmeta",
            DeltaOutputType.dil => "dil",
            DeltaOutputType.dpdb => "dpdb",
            DeltaOutputType.updateHandlerJson => "handler.json",
            _ => throw new Exception("unexpected")
        };
        return $"{baseName}.{rev}.{ext}";
    }

    private static ITaskItem[] ComputeSources (Script.Json.Change[] changes)
    {
        var count = changes.Length;
        ITaskItem[] result = new TaskItem[changes.Length];
        for (int i = 0; i < count; ++i)
        {
            // Just return the "update" documents.  The baseline document should already be a <Compile> item in the project
            result[i] = new TaskItem(changes[i].Update, new Dictionary<string,string> {  { "DeltaForBaseline", changes[i].Document} });
        }
        return result;
    }

    public static async Task<Script.Json.Script?> Parse(string scriptPath, CancellationToken ct = default)
    {
        using var stream = System.IO.File.OpenRead(scriptPath);
        var options = new JsonSerializerOptions {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
        };
        var json = await JsonSerializer.DeserializeAsync<Script.Json.Script>(stream, options: options, cancellationToken: ct);
        return json;
    }

}
