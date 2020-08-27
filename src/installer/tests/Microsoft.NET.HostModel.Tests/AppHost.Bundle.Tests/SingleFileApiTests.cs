using System;
using BundleTests.Helpers;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class SingleFileApiTests : IClassFixture<SingleFileApiTests.SharedTestState>
    {
        private SharedTestState sharedTestState;
        private const string fixtureProjectName = "SingleFileApiTests";

        public SingleFileApiTests(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void FullyQualifiedName()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var singleFile = BundleHelper.BundleApp(fixture);

            Command.Create(singleFile, "fullyqualifiedname")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("FullyQualifiedName: <Unknown>" +
                    Environment.NewLine +
                    "Name: <Unknown>");
        }

        [Fact]
        public void CodeBaseThrows()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var singleFile = BundleHelper.BundleApp(fixture);

            Command.Create(singleFile, "codebase")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("CodeBase NotSupported");
        }

        [Fact]
        public void AppContext_Deps_Files_Bundled_Non_Framework_Dependent()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var singleFile = BundleHelper.BundleApp(fixture);

            Command.Create(singleFile, "appcontext")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .NotHaveStdOutContaining($"{fixtureProjectName}.deps.json")
                .And
                .NotHaveStdOutContaining("Microsoft.NETCore.App.deps.json");
        }

        [Fact]
        public void AppContext_Deps_Files_Bundled_Framework_Dependent()
        {
            var fixture = sharedTestState.FrameworkDependentTestFixture.Copy();
            var singleFile = BundleHelper.BundleApp(fixture);

            Command.Create(singleFile, "appcontext")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .NotHaveStdOutContaining($"{fixtureProjectName}.deps.json")
                .And
                .HaveStdOutContaining("Microsoft.NETCore.App.deps.json");
        }

        [Fact]
        public void GetEnvironmentArgs_0_Returns_Bundled_Executable_Path()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var singleFile = BundleHelper.BundleApp(fixture);

            // For single-file, Environment.GetCommandLineArgs[0]
            // should return the file path of the host.
            Command.Create(singleFile, "cmdlineargs")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining(singleFile);
        }

        [Fact]
        public void GetEnvironmentArgs_0_Non_Bundled_App()
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

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }
            public TestProjectFixture FrameworkDependentTestFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();
                TestFixture = new TestProjectFixture(fixtureProjectName, RepoDirectories);
                TestFixture
                    .EnsureRestoredForRid(TestFixture.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: TestFixture.CurrentRid, outputDirectory: BundleHelper.GetPublishPath(TestFixture));

                FrameworkDependentTestFixture = new TestProjectFixture(fixtureProjectName, RepoDirectories);
                FrameworkDependentTestFixture
                    .EnsureRestoredForRid(FrameworkDependentTestFixture.CurrentRid, RepoDirectories.CorehostPackages)
                    .PublishProject(runtime: FrameworkDependentTestFixture.CurrentRid,
                                    selfContained: false,
                                    outputDirectory: BundleHelper.GetPublishPath(FrameworkDependentTestFixture));
            }

            public void Dispose()
            {
                TestFixture.Dispose();
                FrameworkDependentTestFixture.Dispose();
            }
        }
    }
}
