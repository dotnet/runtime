using System;
using System.IO;
using System.Text.RegularExpressions;
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
            string extractionDir = BundleHelper.GetExtractionRootDir(fixture).Name;
            string bundleDir = BundleHelper.GetBundleDir(fixture).FullName;

            Command.Create(singleFile, "fullyqualifiedname codebase appcontext cmdlineargs executing_assembly_location basedirectory native_search_dirs")
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
                .And.HaveStdOutContaining("AppContext.BaseDirectory: " + Path.GetDirectoryName(singleFile))
                // If we don't extract anything to disk, the extraction dir shouldn't
                // appear in the native search dirs.
                .And.HaveStdOutMatching($"NATIVE_DLL_SEARCH_DIRECTORIES: .*{Regex.Escape(bundleDir)}")
                .And.NotHaveStdOutContaining(extractionDir);
        }

        [Fact]
        public void SelfContained_NetCoreApp3_CompatMode_SingleFile_APITests()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            var singleFile = BundleSelfContainedApp(fixture, BundleOptions.BundleAllContent);
            string bundleDir = BundleHelper.GetBundleDir(fixture).FullName;
            string extractionDir = BundleHelper.GetExtractionRootDir(fixture).FullName;

            Command.Create(singleFile, "fullyqualifiedname codebase appcontext cmdlineargs executing_assembly_location basedirectory native_search_dirs")
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable(BundleHelper.DotnetBundleExtractBaseEnvVariable, extractionDir)
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
                .And.HaveStdOutContaining("ExecutingAssembly.Location: " + extractionDir) // Should point to the app's dll
                .And.HaveStdOutContaining("AppContext.BaseDirectory: " + extractionDir) // Should point to the extraction directory
                // In extraction mode, we should have both dirs
                .And.HaveStdOutMatching($"NATIVE_DLL_SEARCH_DIRECTORIES: .*{Regex.Escape(extractionDir)}.*{Regex.Escape(bundleDir)}");
        }

        public class SharedTestState : SharedTestStateBase, IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }

            public SharedTestState()
            {
                // We include mockcoreclr in our project to test native binaries extraction.
                string mockCoreClrPath = Path.Combine(RepoDirectories.Artifacts, "corehost_test",
                    RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("mockcoreclr"));
                TestFixture = PreparePublishedSelfContainedTestProject("SingleFileApiTests", $"/p:AddFile={mockCoreClrPath}");
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
