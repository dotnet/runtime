using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class BuildPlatformsAttribute : TargetConditionAttribute
    {
        private IEnumerable<BuildPlatform> _buildPlatforms;
        private string _version;

        public BuildPlatformsAttribute(params BuildPlatform[] platforms)
        {
            if (platforms == null)
            {
                throw new ArgumentNullException(nameof(platforms));
            }

            _buildPlatforms = platforms;
        }

        public BuildPlatformsAttribute(BuildPlatform platform, string version)
        {
            _buildPlatforms = new BuildPlatform[] { platform };
            _version = version;
        }

        public override bool EvaluateCondition()
        {
            foreach (var platform in _buildPlatforms)
            {
                if (CurrentPlatform.IsPlatform(platform, _version))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
