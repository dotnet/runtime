// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Build.Framework;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
                string dir = Path.GetDirectoryName(projectJsonPath.ItemSpec);
                string text = File.ReadAllText(projectJsonPath.ItemSpec);
                Match match = Regex.Match(text, "<TargetFrameworks>(.*)</TargetFrameworks>");
                if (match.Groups.Count == 2)
                {
                    string[] tfms = match.Groups[1].Value.Split(';');
                    foreach (string framework in tfms)
                    {
                        Console.WriteLine("OSGroup: " + OSGroup);
                        if (OSGroup == "Windows_NT"
                            || framework.StartsWith("netstandard")
                            || framework.StartsWith("netcoreapp"))
                        {
                            args.Add($"--framework {framework} {projectJsonPath}");
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
