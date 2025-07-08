// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.DotNet.TestUtils;
using Xunit;

namespace HostActivation.Tests
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
                .Execute()
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
                .Execute()
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
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining($"dotnet exec needs a managed .dll or .exe extension. The application specified was '{assemblyName}'");
        }

        [Fact]
        public void MissingArgumentValue_Fails()
        {
            TestContext.BuiltDotNet.Exec("--fx-version")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining($"Failed to parse supported options or their values:");
        }

        [Fact]
        public void File_ExistsNoDirectorySeparator_RoutesToSDK()
        {
            // Create a file named "build" in the current directory to simulate a file that happens to match the name of a command
            string buildFile = Path.Combine(sharedTestState.BaseDirectory.Location, "build");
            File.WriteAllText(buildFile, string.Empty);

            // Test that "dotnet build" still routes to SDK, not to the file
            TestContext.BuiltDotNet.Exec("build")
                .WorkingDirectory(sharedTestState.BaseDirectory.Location)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining("The command could not be loaded, possibly because:") // This is a generic error for when we can't tell what exactly the user intended to do
                .And.HaveStdErrContaining("Resolving SDKs");
        }

        [Fact]
        public void RelativePathToNonManagedFile_ShowsSpecificError()
        {
            // Test relative path with directory separator
            Directory.CreateDirectory(Path.Combine(sharedTestState.BaseDirectory.Location, "subdir"));
            string testFile = Path.Combine(sharedTestState.BaseDirectory.Location, "subdir", "test.json");
            File.WriteAllText(testFile, "{}");

            string relativePath = Path.GetRelativePath(sharedTestState.BaseDirectory.Location, testFile);
            TestContext.BuiltDotNet.Exec(relativePath)
                .WorkingDirectory(sharedTestState.BaseDirectory.Location)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining($"The application '{relativePath}' is not a managed .dll.");
        }

        [Fact]
        public void InvalidFileOrCommand_NoSDK_ListsPossibleIssues()
        {
            string fileName = "NonExistent";
            TestContext.BuiltDotNet.Exec(fileName)
                .WorkingDirectory(sharedTestState.BaseDirectory.Location)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining($"The application '{fileName}' does not exist")
                .And.FindAnySdk(false);
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
                BaseDirectory = TestArtifact.Create("argValidation");

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
