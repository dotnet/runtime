using System;
using System.IO;
using BundleTests.Helpers;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.Bundle;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class SingleFileApiTests : BundleTestBase, IClassFixture<SingleFileApiTests.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public SingleFileApiTests(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void SelfContained_SingleFile_APITests()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var singleFile = BundleSelfContainedApp(fixture);

            Command.Create(singleFile, "fullyqualifiedname codebase appcontext cmdlineargs executing_assembly_location basedirectory")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And.HaveStdOutContaining("FullyQualifiedName: <Unknown>")
                .And.HaveStdOutContaining("Name: <Unknown>")
                .And.HaveStdOutContaining("CodeBase NotSupported")
                .And.NotHaveStdOutContaining("SingleFileApiTests.deps.json")
                .And.NotHaveStdOutContaining("Microsoft.NETCore.App.deps.json")
                // For single-file, Environment.GetCommandLineArgs[0] should return the file path of the host.
                .And.HaveStdOutContaining("Command line args: " + singleFile)
                .And.HaveStdOutContaining("ExecutingAssembly.Location: " + Environment.NewLine)
                .And.HaveStdOutContaining("AppContext.BaseDirectory: " + Path.GetDirectoryName(singleFile));
        }

        [Fact]
        public void SelfContained_NetCoreApp3_CompatMode_SingleFile_APITests()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var singleFile = BundleSelfContainedApp(fixture, BundleOptions.BundleAllContent);
            var extractionBaseDir = BundleHelper.GetExtractionRootDir(fixture);

            Command.Create(singleFile, "fullyqualifiedname codebase appcontext cmdlineargs executing_assembly_location basedirectory")
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable(BundleHelper.DotnetBundleExtractBaseEnvVariable, extractionBaseDir.FullName)
                .Execute()
                .Should()
                .Pass()
                .And.HaveStdOutContaining(Path.DirectorySeparatorChar + "System.Private.CoreLib.dll") // In extraction directory
                .And.HaveStdOutContaining("System.Private.CoreLib.dll") // In extraction directory
                .And.NotHaveStdOutContaining("CodeBase NotSupported") // CodeBase should point to extraction directory
                .And.HaveStdOutContaining("SingleFileApiTests.dll")
                .And.HaveStdOutContaining("SingleFileApiTests.deps.json") // The app's .deps.json should be available
                .And.NotHaveStdOutContaining("Microsoft.NETCore.App.deps.json") // No framework - it's self-contained
                // For single-file, Environment.GetCommandLineArgs[0] should return the file path of the host.
                .And.HaveStdOutContaining("Command line args: " + singleFile)
                .And.HaveStdOutContaining("ExecutingAssembly.Location: " + extractionBaseDir.FullName) // Should point to the app's dll
                .And.HaveStdOutContaining("AppContext.BaseDirectory: " + extractionBaseDir.FullName); // Should point to the extraction directory
        }

        [Fact]
        public void GetCommandLineArgs_0_Non_Bundled_App()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appPath = BundleHelper.GetAppPath(fixture);

            // For non single-file apps, Environment.GetCommandLineArgs[0]
            // should return the file path of the managed entrypoint.
            dotnet.Exec(appPath, "cmdlineargs")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining(appPath);
        }

        public class SharedTestState : SharedTestStateBase, IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }

            public SharedTestState()
            {
                TestFixture = PreparePublishedSelfContainedTestProject("SingleFileApiTests");
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
