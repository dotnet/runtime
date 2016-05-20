using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public class BuildTargetContext
    {
        private IDictionary<string, BuildTargetResult> _dependencyResults;

        public BuildContext BuildContext { get; }
        public BuildTarget Target { get; }

        public BuildTargetContext(BuildContext buildContext, BuildTarget target, IDictionary<string, BuildTargetResult> dependencyResults)
        {
            BuildContext = buildContext;
            Target = target;
            _dependencyResults = dependencyResults;
        }

        public BuildTargetResult Success()
        {
            return new BuildTargetResult(Target, success: true);
        }

        public BuildTargetResult Failed() => Failed(errorMessage: string.Empty);

        public BuildTargetResult Failed(string errorMessage)
        {
            return new BuildTargetResult(Target, success: false, errorMessage: errorMessage);
        }

        public void Info(string message) => BuildContext.Info(message);
        public void Warn(string message) => BuildContext.Warn(message);
        public void Error(string message) => BuildContext.Error(message);
        public void Verbose(string message) => BuildContext.Verbose(message);
    }
}
