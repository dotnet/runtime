// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.NET.HostModel.ComHost;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    public class Comhost : IClassFixture<Comhost.SharedTestState>
    {
        private readonly SharedTestState sharedState;

        public Comhost(SharedTestState sharedTestState)
        {
            sharedState = sharedTestState;
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(10, true)]
        [InlineData(10, false)]
        public void ActivateClass(int count, bool synchronous)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // COM activation is only supported on Windows
                return;
            }

            string [] args = {
                "comhost",
                synchronous ? "synchronous" : "concurrent",
                $"{count}",
                sharedState.ComHostPath,
                sharedState.ClsidString
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.ComLibraryFixture.BuiltDotnet.BinPath)
                .Execute();

            result.Should().Pass()
                .And.HaveStdOutContaining("New instance of Server created");

            for (var i = 1; i <= count; ++i)
            {
                result.Should().HaveStdOutContaining($"Activation of {sharedState.ClsidString} succeeded. {i} of {count}");
            }
        }

        [Fact]
        public void ActivateClass_IgnoreAppLocalHostFxr()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // COM activation is only supported on Windows
                return;
            }

            using (var fixture = sharedState.ComLibraryFixture.Copy())
            {
                File.WriteAllText(Path.Combine(fixture.TestProject.BuiltApp.Location, "hostfxr.dll"), string.Empty);
                var comHostWithAppLocalFxr = Path.Combine(
                    fixture.TestProject.BuiltApp.Location,
                    $"{ fixture.TestProject.AssemblyName }.comhost.dll");

                string[] args = {
                    "comhost",
                    "synchronous",
                    "1",
                    comHostWithAppLocalFxr,
                    sharedState.ClsidString
                    };
                CommandResult result = sharedState.CreateNativeHostCommand(args, fixture.BuiltDotnet.BinPath)
                    .Execute();

                result.Should().Pass()
                    .And.HaveStdOutContaining("New instance of Server created")
                    .And.HaveStdOutContaining($"Activation of {sharedState.ClsidString} succeeded.")
                    .And.HaveStdErrContaining("Using environment variable DOTNET_ROOT");
            }
        }

        [Fact]
        public void ActivateClass_ValidateIErrorInfoResult()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // COM activation is only supported on Windows
                return;
            }

            using (var fixture = sharedState.ComLibraryFixture.Copy())
            {
                string missingRuntimeConfig = Path.Combine(fixture.TestProject.BuiltApp.Location,
                            $"{ fixture.TestProject.AssemblyName }.runtimeconfig.json");

                File.Delete(missingRuntimeConfig);

                var comHost = Path.Combine(
                    fixture.TestProject.BuiltApp.Location,
                    $"{ fixture.TestProject.AssemblyName }.comhost.dll");

                string[] args = {
                    "comhost",
                    "errorinfo",
                    "1",
                    comHost,
                    sharedState.ClsidString
                };
                CommandResult result = sharedState.CreateNativeHostCommand(args, fixture.BuiltDotnet.BinPath)
                    .Execute();

                result.Should().Pass()
                    .And.HaveStdOutContaining($"The specified runtimeconfig.json [{missingRuntimeConfig}] does not exist");
            }
        }

        [Fact]
        public void LoadTypeLibraries()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // COM activation is only supported on Windows
                return;
            }

            using (var fixture = sharedState.ComLibraryFixture.Copy())
            {
                var comHost = Path.Combine(
                    fixture.TestProject.BuiltApp.Location,
                    $"{ fixture.TestProject.AssemblyName }.comhost.dll");

                string[] args = {
                    "comhost",
                    "typelib",
                    "2",
                    comHost,
                    sharedState.ClsidString
                };
                CommandResult result = sharedState.CreateNativeHostCommand(args, fixture.BuiltDotnet.BinPath)
                    .Execute();

                result.Should().Pass()
                    .And.HaveStdOutContaining("Loading default type library succeeded.")
                    .And.HaveStdOutContaining("Loading type library 1 succeeded.")
                    .And.HaveStdOutContaining("Loading type library 2 succeeded.");
            }
        }

        public class SharedTestState : SharedTestStateBase
        {
            public string ComHostPath { get; }

            public string ClsidString = "{438968CE-5950-4FBC-90B0-E64691350DF5}";
            public TestProjectFixture ComLibraryFixture { get; }

            public SharedTestState()
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // COM activation is only supported on Windows
                    return;
                }

                ComLibraryFixture = new TestProjectFixture("ComLibrary", RepoDirectories)
                    .EnsureRestored()
                    .BuildProject();

                // Create a .clsidmap from the assembly
                string clsidMapPath = Path.Combine(BaseDirectory, $"{ ComLibraryFixture.TestProject.AssemblyName }.clsidmap");
                using (var assemblyStream = new FileStream(ComLibraryFixture.TestProject.AppDll, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.Read))
                using (var peReader = new System.Reflection.PortableExecutable.PEReader(assemblyStream))
                {
                    if (peReader.HasMetadata)
                    {
                        MetadataReader reader = peReader.GetMetadataReader();
                        ClsidMap.Create(reader, clsidMapPath);
                    }
                }

                // Use the locally built comhost to create a comhost with the embedded .clsidmap
                ComHostPath = Path.Combine(
                    ComLibraryFixture.TestProject.BuiltApp.Location,
                    $"{ ComLibraryFixture.TestProject.AssemblyName }.comhost.dll");

                // Include the test type libraries in the ComHost tests.
                var typeLibraries = new Dictionary<int, string>
                {
                    { 1, Path.Combine(RepoDirectories.Artifacts, "corehost_test", "Server.tlb") },
                    { 2, Path.Combine(RepoDirectories.Artifacts, "corehost_test", "Nested.tlb") }
                };

                ComHost.Create(
                    Path.Combine(RepoDirectories.HostArtifacts, "comhost.dll"),
                    ComHostPath,
                    clsidMapPath,
                    typeLibraries);
            }

            protected override void Dispose(bool disposing)
            {
                if (ComLibraryFixture != null)
                    ComLibraryFixture.Dispose();

                base.Dispose(disposing);
            }
        }
    }
}
