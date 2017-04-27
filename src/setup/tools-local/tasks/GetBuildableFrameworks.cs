// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Build.Framework;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Microsoft.Build.Utilities;

namespace Microsoft.DotNet.Build.Tasks
{
    /// <summary>
    /// This target opens the json files and extracts the frameworks that are buildable
    /// according to the current OSGroup.
    /// In short it removes net45 and similar from *nix systems.
    /// The output is an ItemGroup that can be batch called when executing 'dotnet build'
    /// </summary>
    public class GetBuildArgsByFrameworks : BuildTask
    {
        [Required]
        public ITaskItem[] ProjectJsonPaths { get; set; }
        [Required]
        public string OSGroup { get; set; }
        [Output]
        public ITaskItem[] BuildArgs { get; set; }
        public override bool Execute()
        {
            List<string> args = new List<string>();
            foreach (var projectJsonPath in ProjectJsonPaths)
            {
                using (TextReader projectFileReader = File.OpenText(projectJsonPath.ItemSpec))
                {

                    var projectJsonReader = new JsonTextReader(projectFileReader);
                    var serializer = new JsonSerializer();
                    var project = serializer.Deserialize<JObject>(projectJsonReader);
                    var dir = Path.GetDirectoryName(projectJsonPath.ItemSpec);
                    var frameworksSection = project.Value<JObject>("frameworks");
                    foreach (var framework in frameworksSection.Properties())
                    {
                        if (OSGroup == "Windows_NT"
                            || framework.Name.StartsWith("netstandard")
                            || framework.Name.StartsWith("netcoreapp"))
                        {
                            args.Add($"--framework {framework.Name} {dir}");
                        }
                    }
                }
            }

            BuildArgs = new ITaskItem[args.Count];
            for (int i = 0; i < BuildArgs.Length; i++)
            {
                BuildArgs[i] = new TaskItem(args[i]);
            }

            return true;
        }
    }
}
