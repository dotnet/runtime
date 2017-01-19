// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Build.Tasks
{
    public class FindBestConfiguration : ConfigurationTask
    {
        [Required]
        public string BuildConfiguration { get; set; }

        [Required]
        public string[] BuildConfigurations { get; set; }

        public bool DoNotAllowCompatibleValues { get; set; }

        [Output]
        public ITaskItem BestConfiguration { get; set; }

        public override bool Execute()
        {
            LoadConfiguration();

            var supportedProjectConfigurations = new HashSet<Configuration>(
                BuildConfigurations.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => ConfigurationFactory.ParseConfiguration(c)),
                Configuration.CompatibleComparer);

            var buildConfiguration = ConfigurationFactory.ParseConfiguration(BuildConfiguration);

            var compatibleConfigurations = ConfigurationFactory.GetCompatibleConfigurations(buildConfiguration, DoNotAllowCompatibleValues);

            var bestConfiguration = compatibleConfigurations.FirstOrDefault(c => supportedProjectConfigurations.Contains(c));

            if (bestConfiguration == null)
            {
                Log.LogMessage($"Could not find any applicable configuration for '{buildConfiguration}' among projectConfigurations {string.Join(", ", supportedProjectConfigurations.Select(c => c.ToString()))}");
                Log.LogMessage(MessageImportance.Low, $"Compatible configurations: {string.Join(", ", compatibleConfigurations.Select(c => c.ToString()))}");
            }
            else
            {
                Log.LogMessage(MessageImportance.Low, $"Chose configuration {bestConfiguration}");
                BestConfiguration = new TaskItem(bestConfiguration.ToString(), (IDictionary)bestConfiguration.GetProperties());
            }

            return !Log.HasLoggedErrors;
        }
    }
}

