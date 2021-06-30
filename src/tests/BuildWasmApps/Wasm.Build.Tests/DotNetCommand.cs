// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
