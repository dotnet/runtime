// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Wasm.Build.Tests
{
    public class DotNetCommand : ToolCommand
    {
        private BuildEnvironment _buildEnvironment;
        private bool _useDefaultArgs;

        public DotNetCommand(BuildEnvironment buildEnv, bool useDefaultArgs=true) : base(buildEnv.DotNet)
        {
            _buildEnvironment = buildEnv;
            _useDefaultArgs = useDefaultArgs;
            WithEnvironmentVariables(buildEnv.EnvVars);
        }

        protected override string GetFullArgs(params string[] args)
            => _useDefaultArgs
                    ? $"{string.Join(" ", args)} {_buildEnvironment.DefaultBuildArgs}"
                    : string.Join(" ", args);
    }
}
