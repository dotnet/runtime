// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build;
using System;
using System.IO;
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
            sharedTestState.BuiltDotNet.Exec("exec", assemblyName)
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
            sharedTestState.BuiltDotNet.Exec("exec", assemblyName)
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
            string fxDir = Path.Combine(sharedTestState.RepoDirectories.DotnetSDK, "shared", "Microsoft.NETCore.App");
            fxDir = new DirectoryInfo(fxDir).GetDirectories()[0].FullName;
            string assemblyName = Path.Combine(fxDir, "Microsoft.NETCore.App.deps.json");

            sharedTestState.BuiltDotNet.Exec("exec", assemblyName)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining($"dotnet exec needs a managed .dll or .exe extension. The application specified was '{assemblyName}'");
        }

        [Fact]
        public void MissingArgumentValue_Fails()
        {
            sharedTestState.BuiltDotNet.Exec("--fx-version")
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
            sharedTestState.BuiltDotNet.Exec(fileName)
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
            sharedTestState.BuiltDotNet.Exec("--info")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutMatching($@"Architecture:\s*{RepoDirectoriesProvider.Default.BuildArchitecture}")
                .And.HaveStdOutMatching($@"RID:\s*{RepoDirectoriesProvider.Default.BuildRID}");
        }

        [Fact]
        public void DotNetInfo_WithSDK()
        {
            DotNetCli dotnet = new DotNetBuilder(sharedTestState.BaseDirectory.Location, RepoDirectoriesProvider.Default.BuiltDotnet, "withSdk")
                .AddMicrosoftNETCoreAppFrameworkMockHostPolicy("1.0.0")
                .AddMockSDK("1.0.0", "1.0.0")
                .Build();

            dotnet.Exec("--info")
                .WorkingDirectory(sharedTestState.BaseDirectory.Location)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.NotHaveStdOutMatching($@"RID:\s*{RepoDirectoriesProvider.Default.BuildRID}");
        }

        // Return a non-existent path that contains a mix of / and \
        private string GetNonexistentAndUnnormalizedPath()
        {
            return Path.Combine(sharedTestState.RepoDirectories.DotnetSDK, @"x\y/");
        }

        public class SharedTestState : IDisposable
        {
            public RepoDirectoriesProvider RepoDirectories { get; }

            public DotNetCli BuiltDotNet { get; }
            public TestArtifact BaseDirectory { get; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();
                BuiltDotNet = new DotNetCli(RepoDirectories.BuiltDotnet);

                BaseDirectory = new TestArtifact(SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "argValidation")));

                // Create an empty global.json file
                Directory.CreateDirectory(BaseDirectory.Location);
                File.WriteAllText(Path.Combine(BaseDirectory.Location, "global.json"), "{}");
            }

            public void Dispose()
            {
                BaseDirectory.Dispose();
            }
        }
    }
}
