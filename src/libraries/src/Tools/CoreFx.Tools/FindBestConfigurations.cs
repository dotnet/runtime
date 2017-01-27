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
    public class FindBestConfigurations : ConfigurationTask
    {
        [Required]
        public ITaskItem[] Configurations { get; set; }

        [Required]
        public string[] SupportedConfigurations { get; set; }

        public bool DoNotAllowCompatibleValues { get; set; }

        [Output]
        public ITaskItem[] BestConfigurations { get; set; }

        public override bool Execute()
        {
            LoadConfiguration();

            var supportedProjectConfigurations = new HashSet<Configuration>(
                SupportedConfigurations.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => ConfigurationFactory.ParseConfiguration(c)),
                Configuration.CompatibleComparer);

            var bestConfigurations = new List<ITaskItem>();

            foreach (var configurationItem in Configurations)
            {
                var buildConfiguration = ConfigurationFactory.ParseConfiguration(configurationItem.ItemSpec);

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
                    var bestConfigurationItem = new TaskItem(bestConfiguration.ToString(), (IDictionary)bestConfiguration.GetProperties());

                    // preserve metadata on the configuration that selected this
                    configurationItem.CopyMetadataTo(bestConfigurationItem);

                    // preserve the configuration that selected this
                    bestConfigurationItem.SetMetadata("BuildConfiguration", configurationItem.ItemSpec);

                    bestConfigurations.Add(bestConfigurationItem);
                }
            }

            BestConfigurations = bestConfigurations.ToArray();

            return !Log.HasLoggedErrors;
        }
    }
}

