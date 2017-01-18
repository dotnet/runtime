using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;

namespace Microsoft.DotNet.Build.Tasks
{
    public class ValidateVSConfigurations : BuildTask
    {
        [Required]
        public ITaskItem[] ProjectsToValidate { get; set; }

        private const string CONFIGURATION_PROPS = "Configurations.props";
        private const string VS_CONFIG_REGEX = @"'\$\(Configuration\)\|\$\(Platform\)' == '(.*?)'";

        public override bool Execute()
        {
            bool matchesConfigs = true;
            //Parallellise this task to make faster
            foreach (var item in ProjectsToValidate)
            {
                string projLocation = item.ItemSpec;
                string projConfigurationLocation = Path.Combine(Path.GetDirectoryName(projLocation), CONFIGURATION_PROPS);
                if (File.Exists(projConfigurationLocation))
                {
                    string projContents = File.ReadAllText(projLocation);
                    string projConfigurationContents = File.ReadAllText(projConfigurationLocation);

                    var matchCollection = Regex.Matches(projContents, VS_CONFIG_REGEX);
                    var configCollection = Regex.Matches(projConfigurationContents,
                        "<BuildConfigurations>(.*?)</BuildConfigurations>", RegexOptions.Singleline);

                    string[] configs = null;
                    HashSet<string> vsConfigs = new HashSet<string>();
                    foreach (Match configGroup in configCollection)
                    {
                        configs = configGroup.Groups[1].Value.Replace("\r\n", "").Trim().Split(new char[]{ ';' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                    foreach (var config in configs)
                    {
                        string str = config + "_Debug|AnyCPU";
                        string str1 = config + "_Release|AnyCPU";
                        vsConfigs.Add(str);
                        vsConfigs.Add(str1);
                    }

                    foreach (Match match in matchCollection)
                    {
                        string configFromProj = match.Groups[1].Value;
                        if (!vsConfigs.Contains(configFromProj))
                        {
                            matchesConfigs = false;
                            break;
                        }
                    }
                }
                if (!matchesConfigs)
                {
                    Log.LogError($"{item} configurations does not match it's Configurations.props.");
                    break;
                }
            }
            return matchesConfigs;
        }
    }
}
