using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class BuildPlatformsAttribute : TargetConditionAttribute
    {
        private IEnumerable<BuildPlatform> _buildPlatforms;

        public BuildPlatformsAttribute(params BuildPlatform[] platforms)
        {
            if (platforms == null)
            {
                throw new ArgumentNullException(nameof(platforms));
            }

            _buildPlatforms = platforms;
        }

        public override bool EvaluateCondition()
        {
            foreach (var platform in _buildPlatforms)
            {
                if (CurrentPlatform.IsPlatform(platform))
                {
                    return true;
                }
            }

            return false;
        }
    }
}