// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;


namespace Microsoft.DotNet.HotReload.Utils.Generator.OutputSummary;

public class OutputSummary {
    [JsonPropertyName("deltas")]
    public Delta[]? Deltas {get; init;}

    [JsonExtensionData]
    public System.Collections.Generic.Dictionary<string, object>? Extra {get; init;}

    [JsonConstructor]
    public OutputSummary(Delta[]? deltas) {
        Deltas = deltas;
    }
}

public class Delta {
    [JsonPropertyName("assembly")]
    public string? Assembly {get; init;}
    [JsonPropertyName("metadata")]
    public string? Metadata {get; init;}
    [JsonPropertyName("il")]
    public string? IL {get; init;}
    [JsonPropertyName("pdb")]
    public string? Pdb {get; init;}

    [JsonConstructor]
    public Delta (string? assembly, string? metadata, string? il, string? pdb) {
        Assembly = assembly;
        Metadata = metadata;
        IL = il;
        Pdb = pdb;
    }

    [JsonExtensionData]
    public System.Collections.Generic.Dictionary<string, object>? Extra {get; set;}
}
