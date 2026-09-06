// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NETCore.Platforms
{
    public class UpdateRuntimeIdentifierGraph : Task
    {
        [Required]
        public string? InputFile { get; set; }

        [Required]
        public string? OutputFile { get; set; }

        // ItemSpec should be a RID, and "Imports" metadata should be a semicolon-separated list of RIDs that the ItemSpec RID imports
        [Required]
        public ITaskItem[]? AdditionalRuntimeIdentifiers { get; set; }

        public override bool Execute()
        {
            JsonNode? json;
            using (FileStream stream = File.OpenRead(InputFile!))
            {
                json = JsonNode.Parse(stream);
            }

            JsonObject runtimes = json!["runtimes"]!.AsObject();
            foreach (ITaskItem rid in AdditionalRuntimeIdentifiers!)
            {
                // Skip the RID if it's already in the graph
                if (runtimes.ContainsKey(rid.ItemSpec))
                {
                    continue;
                }

                string[] importedRids = rid.GetMetadata("Imports").Split(';');
                runtimes.Add(rid.ItemSpec, new JsonObject { ["#import"] = new JsonArray([.. importedRids]) });
            }

            using FileStream streamWriter = File.Create(OutputFile!);
            using Utf8JsonWriter jsonWriter = new(streamWriter, new JsonWriterOptions { Indented = true });
            json!.WriteTo(jsonWriter);

            return true;
        }
    }
}
