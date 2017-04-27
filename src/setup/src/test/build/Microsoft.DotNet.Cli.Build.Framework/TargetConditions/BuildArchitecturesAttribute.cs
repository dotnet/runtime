using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class BuildArchitecturesAttribute : TargetConditionAttribute
    {
        private IEnumerable<BuildArchitecture> _buildArchitectures;

        public BuildArchitecturesAttribute(params BuildArchitecture[] architectures)
        {
            if (architectures == null)
            {
                throw new ArgumentNullException(nameof(architectures));
            }

            _buildArchitectures = architectures;
        }

        public override bool EvaluateCondition()
        {
            var currentArchitecture = CurrentArchitecture.Current;

            if (currentArchitecture == default(BuildArchitecture))
            {
                throw new Exception("Unrecognized Architecture");
            }

            foreach (var architecture in _buildArchitectures)
            {
                if (architecture == currentArchitecture)
                {
                    return true;
                }
            }

            return false;
        }
    }
}