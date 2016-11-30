// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Microsoft.DotNet.Build.Tasks
{
    public class FindBestConfiguration : ConfigurationTask
    {
        private static readonly char[] s_SupportedGroupsSeparator = new[] { '|' };
        private static readonly char[] s_VerticalGroupsSeparator = new[] { '-' };

        [Required]
        public string[] ProjectConfigurations { get; set; }

        [Required]
        public string SelectionGroup { get; set; }

        [Required]
        public string Name { get; set; }

        public bool UseCompileFramework { get; set; }

        public bool AllowCompatibleFramework { get; set; }

        [Output]
        public ITaskItem OutputItem { get; set; }

        public override bool Execute()
        {
            LoadConfiguration();

            var supportedProjectConfigurations = new HashSet<Configuration>(ProjectConfigurations.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => ConfigurationFactory.ParseConfiguration(c)));

            var buildConfiguration = ConfigurationFactory.ParseConfiguration(SelectionGroup);

            var compatibleConfigurations = ConfigurationFactory.GetCompatibleConfigurations(buildConfiguration);

            var bestConfiguration = compatibleConfigurations.FirstOrDefault(c => supportedProjectConfigurations.Contains(c));

            if (bestConfiguration == null)
            {
                Log.LogError($"Could not find any applicable configuration for '{buildConfiguration}' among projectConfigurations {string.Join(", ", supportedProjectConfigurations.Select(c => c.ToString()))}");
                Log.LogMessage(MessageImportance.Low, $"Compatible configurations: {string.Join(", ", compatibleConfigurations.Select(c => c.ToString()))}");
            }
            else
            {
                Log.LogMessage(MessageImportance.Low, $"Chose configuration {bestConfiguration}");
                OutputItem = new TaskItem(Name, (IDictionary)bestConfiguration.GetProperties());
            }

            return !Log.HasLoggedErrors;
        }
    }
}

