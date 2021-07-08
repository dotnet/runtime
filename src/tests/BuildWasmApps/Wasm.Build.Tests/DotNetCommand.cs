// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Wasm.Build.Tests
{
    public class DotNetCommand : ToolCommand
    {
        private BuildEnvironment _buildEnvironment;

        public DotNetCommand(BuildEnvironment buildEnv) : base(buildEnv.DotNet)
        {
            _buildEnvironment = buildEnv;
            WithEnvironmentVariables(buildEnv.EnvVars);
        }

        protected override string GetFullArgs(params string[] args)
            => $"{_buildEnvironment.DefaultBuildArgs} {string.Join(" ", args)}";
    }
}
