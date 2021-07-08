// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class BlazorWasmTests : BuildTestBase
    {
        public BlazorWasmTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [ConditionalFact(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        public void PublishTemplateProject()
        {
            InitPaths("id");
            if (Directory.Exists(_projectDir))
                Directory.Delete(_projectDir, recursive: true);
            Directory.CreateDirectory(_projectDir);
            Directory.CreateDirectory(Path.Combine(_projectDir, ".nuget"));

            File.Copy(Path.Combine(BuildEnvironment.TestDataPath, "nuget6.config"), Path.Combine(_projectDir, "nuget.config"));
            File.Copy(Path.Combine(BuildEnvironment.TestDataPath, "Blazor.Directory.Build.props"), Path.Combine(_projectDir, "Directory.Build.props"));
            File.Copy(Path.Combine(BuildEnvironment.TestDataPath, "Blazor.Directory.Build.targets"), Path.Combine(_projectDir, "Directory.Build.targets"));

            new DotNetCommand(s_buildEnv)
                    .WithWorkingDirectory(_projectDir)
                    .ExecuteWithCapturedOutput("new blazorwasm")
                    .EnsureSuccessful();

            new DotNetCommand(s_buildEnv)
                    .WithWorkingDirectory(_projectDir)
                    .ExecuteWithCapturedOutput("publish -bl -p:RunAOTCompilation=true")
                    .EnsureSuccessful();

            //TODO: validate the build somehow?
            // compare dotnet.wasm?
            // playwright?
        }
    }
}
