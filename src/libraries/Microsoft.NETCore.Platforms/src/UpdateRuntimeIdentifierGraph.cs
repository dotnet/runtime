// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            JToken json;

            using (StreamReader streamReader = File.OpenText(InputFile!))
            using (JsonTextReader jsonReader = new JsonTextReader(streamReader))
            {
                json = JObject.ReadFrom(jsonReader);
            }

            JObject runtimes = (JObject)json["runtimes"]!;
            foreach (ITaskItem rid in AdditionalRuntimeIdentifiers!)
            {
                // Skip the RID if it's already in the graph
                if (runtimes.ContainsKey(rid.ItemSpec))
                {
                    continue;
                }

                string[] importedRids = rid.GetMetadata("Imports").Split(';');
                runtimes.Add(rid.ItemSpec, new JObject(new JProperty("#import", new JArray(importedRids))));
            }

            using StreamWriter streamWriter = File.CreateText(OutputFile!);
            using JsonTextWriter jsonWriter = new(streamWriter) { Formatting = Formatting.Indented };
            json.WriteTo(jsonWriter);

            return true;
        }
    }
}
