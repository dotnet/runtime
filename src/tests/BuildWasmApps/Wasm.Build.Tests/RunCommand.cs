// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit.Abstractions;

namespace Wasm.Build.Tests;

public class RunCommand : DotNetCommand
{
    public RunCommand(BuildEnvironment buildEnv, ITestOutputHelper _testOutput, string label="") : base(buildEnv, _testOutput, false, label)
    {
        WithEnvironmentVariable("DOTNET_ROOT", buildEnv.DotNet);
        WithEnvironmentVariable("DOTNET_INSTALL_DIR", Path.GetDirectoryName(buildEnv.DotNet)!);
        WithEnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0");
        WithEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1");
    }
}
