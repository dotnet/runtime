using System.Threading.Tasks;

namespace Wasm.Build.Tests
{
    public class DotNetCommand : Command
    {
        private BuildEnvironment _buildEnvironment;
        public DotNetCommand(BuildEnvironment buildEnv) : base(buildEnv.DotNet)
        {
            _buildEnvironment = buildEnv;
            WithEnvironmentVariables(buildEnv.EnvVars);
            WithExtraArgs(buildEnv.DefaultBuildArgs);
        }
    }
}
