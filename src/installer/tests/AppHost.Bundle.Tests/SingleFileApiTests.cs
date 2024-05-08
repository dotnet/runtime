using System;
using System.IO;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.Bundle;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class SingleFileApiTests : IClassFixture<SingleFileApiTests.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public SingleFileApiTests(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void SelfContained()
        {
            string singleFile = sharedTestState.BundledAppPath;
            Command.Create(singleFile, "fullyqualifiedname codebase appcontext cmdlineargs executing_assembly_location basedirectory")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("FullyQualifiedName: <Unknown>")
                .And.HaveStdOutContaining("Name: <Unknown>")
                .And.HaveStdOutContaining("CodeBase NotSupported")
                .And.NotHaveStdOutContaining("SingleFileApiTests.deps.json")
                .And.NotHaveStdOutContaining("Microsoft.NETCore.App.deps.json")
                // For single-file, Environment.GetCommandLineArgs[0] should return the file path of the host.
                .And.HaveStdOutContaining($"Command line args: {singleFile}")
                .And.HaveStdOutContaining($"ExecutingAssembly.Location: {Environment.NewLine}")
                .And.HaveStdOutContaining($"AppContext.BaseDirectory: {Path.GetDirectoryName(singleFile)}");
        }

        [Fact]
        public void SelfContained_BundleAllContent()
        {
            SingleFileTestApp app = sharedTestState.App;
            string singleFile = app.Bundle(BundleOptions.BundleAllContent, out Manifest manifest);
            string extractionRoot = app.GetNewExtractionRootPath();
            string extractionDir = app.GetExtractionDir(extractionRoot, manifest).FullName;

            Command.Create(singleFile, "fullyqualifiedname codebase appcontext cmdlineargs executing_assembly_location basedirectory trusted_platform_assemblies assembly_location System.Console")
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable(Constants.BundleExtractBase.EnvironmentVariable, extractionRoot)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining($"FullyQualifiedName: {Path.Combine(extractionDir, "System.Private.CoreLib.dll")}")
                .And.HaveStdOutContaining("Name: System.Private.CoreLib.dll")
                .And.NotHaveStdOutContaining("CodeBase NotSupported") // CodeBase should point to extraction directory
                .And.HaveStdOutContaining("SingleFileApiTests.dll")
                .And.HaveStdOutContaining("SingleFileApiTests.deps.json") // The app's .deps.json should be available
                .And.NotHaveStdOutContaining("Microsoft.NETCore.App.deps.json") // No framework - it's self-contained
                // For single-file, Environment.GetCommandLineArgs[0] should return the file path of the host.
                .And.HaveStdOutContaining($"Command line args: {singleFile}")
                .And.HaveStdOutContaining($"ExecutingAssembly.Location: {extractionDir}") // Should point to the extracted app's dll
                .And.HaveStdOutContaining($"AppContext.BaseDirectory: {extractionDir}")
                .And.HaveStdOutContaining(Path.Combine(extractionDir, "System.Runtime.dll")) // TPA should contain extracted framework assembly
                .And.HaveStdOutContaining("System.Console location: " + extractionDir); // System.Console should be from extracted location
        }

        [Fact]
        public void NativeSearchDirectories()
        {
            string singleFile = sharedTestState.BundledAppPath;
            string extractionRoot = sharedTestState.App.GetNewExtractionRootPath();
            string bundleDir = Directory.GetParent(singleFile).FullName;

            // If we don't extract anything to disk, the extraction dir shouldn't
            // appear in the native search dirs.
            Command.Create(singleFile, "native_search_dirs")
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable(Constants.BundleExtractBase.EnvironmentVariable, extractionRoot)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining(bundleDir)
                .And.NotHaveStdOutContaining(extractionRoot);
        }

        [Fact]
        public void NativeSearchDirectories_WithExtraction()
        {
            SingleFileTestApp app = sharedTestState.App;
            string singleFile = app.Bundle(BundleOptions.BundleNativeBinaries, out Manifest manifest);

            string extractionRoot = app.GetNewExtractionRootPath();
            string extractionDir = app.GetExtractionDir(extractionRoot, manifest).FullName;
            string bundleDir = Directory.GetParent(singleFile).FullName;

            Command.Create(singleFile, "native_search_dirs")
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable(Constants.BundleExtractBase.EnvironmentVariable, extractionRoot)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining(extractionDir)
                .And.HaveStdOutContaining(bundleDir);
        }

        public class SharedTestState : IDisposable
        {
            public SingleFileTestApp App { get; set; }
            public string BundledAppPath { get; }

            public SharedTestState()
            {
                App = SingleFileTestApp.CreateSelfContained("SingleFileApiTests");

                // Copy over mockcoreclr so that the app will have a native binary
                File.Copy(Binaries.CoreClr.MockPath, Path.Combine(App.NonBundledLocation, Binaries.CoreClr.MockName));

                // Create a bundled app that can be used by multiple tests
                BundledAppPath = App.Bundle(BundleOptions.None);
            }

            public void Dispose()
            {
                App?.Dispose();
            }
        }
    }
}
