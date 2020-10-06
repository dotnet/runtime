// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Build.Tasks
{
    public class ChangeEntryPointLibraryName : BuildTask
    {
        [Required]
        public string DepsFile { get; set; }

        public string NewName { get; set; }

        public override bool Execute()
        {
            JToken deps;
            using (var file = File.OpenText(DepsFile))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                deps = JObject.ReadFrom(reader);
            }

            if (deps == null) return false;

            string version = string.Empty;
            foreach (JProperty target in deps["targets"])
            {
                var targetLibrary = target.Value.Children<JProperty>().FirstOrDefault();
                if (targetLibrary == null)
                {
                    continue;
                }
                version = targetLibrary.Name.Substring(targetLibrary.Name.IndexOf('/') + 1);
                if (string.IsNullOrEmpty(NewName))
                {
                    targetLibrary.Remove();
                }
                else
                {
                    targetLibrary.Replace(new JProperty(NewName + '/' + version, targetLibrary.Value));
                }
            }
            if (!string.IsNullOrEmpty(version))
            {
                var library = deps["libraries"].Children<JProperty>().First();
                if (string.IsNullOrEmpty(NewName))
                {
                    library.Remove();
                }
                else
                {
                    library.Replace(new JProperty(NewName + '/' + version, library.Value));
                }
                using (var file = File.CreateText(DepsFile))
                using (var writer = new JsonTextWriter(file) { Formatting = Formatting.Indented })
                {
                    deps.WriteTo(writer);
                }
            }

            return true;
        }
    }
}
