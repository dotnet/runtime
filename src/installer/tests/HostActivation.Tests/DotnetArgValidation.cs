// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.TestUtils;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class DotnetArgValidation : IClassFixture<DotnetArgValidation.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public DotnetArgValidation(DotnetArgValidation.SharedTestState sharedState)
        {
            sharedTestState = sharedState;
        }

        [Fact]
        public void MuxerExec_MissingAppAssembly_Fails()
        {
            string assemblyName = Path.Combine(GetNonexistentAndUnnormalizedPath(), "foo.dll");
            TestContext.BuiltDotNet.Exec("exec", assemblyName)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining($"The application to execute does not exist: '{assemblyName}'");
        }

        [Fact]
        public void MuxerExec_MissingAppAssembly_BadExtension_Fails()
        {
            string assemblyName = Path.Combine(GetNonexistentAndUnnormalizedPath(), "foo.xzy");
            TestContext.BuiltDotNet.Exec("exec", assemblyName)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining($"The application to execute does not exist: '{assemblyName}'");
        }

        [Fact]
        public void MuxerExec_BadExtension_Fails()
        {
            // Get a valid file name, but not exe or dll
            string fxDir = TestContext.BuiltDotNet.GreatestVersionSharedFxPath;
            string assemblyName = Path.Combine(fxDir, "Microsoft.NETCore.App.deps.json");

            TestContext.BuiltDotNet.Exec("exec", assemblyName)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining($"dotnet exec needs a managed .dll or .exe extension. The application specified was '{assemblyName}'");
        }

        [Fact]
        public void MissingArgumentValue_Fails()
        {
            TestContext.BuiltDotNet.Exec("--fx-version")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining($"Failed to parse supported options or their values:");
        }

        [Fact]
        public void InvalidFileOrCommand_NoSDK_ListsPossibleIssues()
        {
            string fileName = "NonExistent";
            TestContext.BuiltDotNet.Exec(fileName)
                .WorkingDirectory(sharedTestState.BaseDirectory.Location)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining($"The application '{fileName}' does not exist")
                .And.FindAnySdk(false);
        }

        [Fact]
        public void DotNetInfo_NoSDK()
        {
            TestContext.BuiltDotNet.Exec("--info")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutMatching($@"Architecture:\s*{TestContext.BuildArchitecture}")
                .And.HaveStdOutMatching($@"RID:\s*{TestContext.BuildRID}");
        }

        [Fact]
        public void DotNetInfo_WithSDK()
        {
            DotNetCli dotnet = new DotNetBuilder(sharedTestState.BaseDirectory.Location, TestContext.BuiltDotNet.BinPath, "withSdk")
                .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("1.0.0")
                .AddMockSDK("1.0.0", "1.0.0")
                .Build();

            dotnet.Exec("--info")
                .WorkingDirectory(sharedTestState.BaseDirectory.Location)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.NotHaveStdOutMatching($@"RID:\s*{TestContext.BuildRID}");
        }

        // Return a non-existent path that contains a mix of / and \
        private string GetNonexistentAndUnnormalizedPath()
        {
            return Path.Combine(TestContext.BuiltDotNet.BinPath, @"x\y/");
        }

        public class SharedTestState : IDisposable
        {
            public TestArtifact BaseDirectory { get; }

            public SharedTestState()
            {
                BaseDirectory = new TestArtifact(SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "argValidation")));

                // Create an empty global.json file
                Directory.CreateDirectory(BaseDirectory.Location);
                GlobalJson.CreateEmpty(BaseDirectory.Location);
            }

            public void Dispose()
            {
                BaseDirectory.Dispose();
            }
        }
    }
}
