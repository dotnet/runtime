// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;

using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.NET.HostModel.ComHost;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    [PlatformSpecific(TestPlatforms.Windows)] // COM activation is only supported on Windows
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
            string [] args = {
                "comhost",
                synchronous ? "synchronous" : "concurrent",
                $"{count}",
                sharedState.ComHostPath,
                sharedState.ClsidString
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, TestContext.BuiltDotNet.BinPath)
                .Execute();

            result.Should().Pass()
                .And.HaveStdOutContaining("New instance of Server created")
                .And.ExecuteInIsolatedContext(sharedState.ComLibrary.AssemblyName);

            for (var i = 1; i <= count; ++i)
            {
                result.Should().HaveStdOutContaining($"Activation of {sharedState.ClsidString} succeeded. {i} of {count}");
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ActivateClass_ContextConfig(bool inDefaultContext)
        {
            using (var library = sharedState.ComLibrary.Copy())
            {
                var comHost = Path.Combine(library.Location, $"{library.AssemblyName}.comhost.dll");

                RuntimeConfig.FromFile(library.RuntimeConfigJson)
                    .WithProperty("System.Runtime.InteropServices.COM.LoadComponentInDefaultContext", inDefaultContext.ToString())
                    .Save();

                string[] args = {
                    "comhost",
                    "synchronous",
                    "1",
                    comHost,
                    sharedState.ClsidString
                    };
                CommandResult result = sharedState.CreateNativeHostCommand(args, TestContext.BuiltDotNet.BinPath)
                    .Execute();

                result.Should().Pass()
                    .And.HaveStdOutContaining("New instance of Server created")
                    .And.HaveStdOutContaining($"Activation of {sharedState.ClsidString} succeeded.");

                if (inDefaultContext)
                {
                    result.Should().ExecuteInDefaultContext(library.AssemblyName);
                }
                else
                {
                    result.Should().ExecuteInIsolatedContext(library.AssemblyName);
                }
            }
        }

        [Fact]
        public void ActivateClass_IgnoreAppLocalHostFxr()
        {
            using (var library = sharedState.ComLibrary.Copy())
            {
                File.WriteAllText(Path.Combine(library.Location, Binaries.HostFxr.FileName), string.Empty);
                var comHostWithAppLocalFxr = Path.Combine(library.Location, $"{library.AssemblyName}.comhost.dll");

                string[] args = {
                    "comhost",
                    "synchronous",
                    "1",
                    comHostWithAppLocalFxr,
                    sharedState.ClsidString
                };
                CommandResult result = sharedState.CreateNativeHostCommand(args, TestContext.BuiltDotNet.BinPath)
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
            using (var library = sharedState.ComLibrary.Copy())
            {
                File.Delete(library.RuntimeConfigJson);
                var comHost = Path.Combine(library.Location, $"{library.AssemblyName}.comhost.dll");

                string[] args = {
                    "comhost",
                    "errorinfo",
                    "1",
                    comHost,
                    sharedState.ClsidString
                };
                CommandResult result = sharedState.CreateNativeHostCommand(args, TestContext.BuiltDotNet.BinPath)
                    .Execute();

                result.Should().Pass()
                    .And.HaveStdOutContaining($"The specified runtimeconfig.json [{library.RuntimeConfigJson}] does not exist");
            }
        }

        [Fact]
        public void LoadTypeLibraries()
        {
            string[] args = {
                "comhost",
                "typelib",
                "2",
                sharedState.ComHostPath,
                sharedState.ClsidString
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, TestContext.BuiltDotNet.BinPath)
                .Execute();

            result.Should().Pass()
                .And.HaveStdOutContaining("Loading default type library succeeded.")
                .And.HaveStdOutContaining("Loading type library 1 succeeded.")
                .And.HaveStdOutContaining("Loading type library 2 succeeded.");
        }

        public class SharedTestState : SharedTestStateBase
        {
            public string ComHostPath { get; }

            public string ClsidString { get; } = "{438968CE-5950-4FBC-90B0-E64691350DF5}";

            public TestApp ComLibrary { get; }

            public string ClsidMapPath { get; }

            public IReadOnlyDictionary<int, string> TypeLibraries { get; }

            public SharedTestState()
            {
                if (!OperatingSystem.IsWindows())
                {
                    // COM activation is only supported on Windows
                    return;
                }

                ComLibrary = TestApp.CreateFromBuiltAssets("ComLibrary");

                // Create a .clsidmap from the assembly
                ClsidMapPath = Path.Combine(BaseDirectory, $"{ComLibrary.AssemblyName}.clsidmap");
                using (var assemblyStream = new FileStream(ComLibrary.AppDll, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.Read))
                using (var peReader = new System.Reflection.PortableExecutable.PEReader(assemblyStream))
                {
                    if (peReader.HasMetadata)
                    {
                        MetadataReader reader = peReader.GetMetadataReader();
                        ClsidMap.Create(reader, ClsidMapPath);
                    }
                }

                // Include the test type libraries in the ComHost tests.
                TypeLibraries = new Dictionary<int, string>
                {
                    { 1, Path.Combine(RepoDirectoriesProvider.Default.HostTestArtifacts, "Server.tlb") },
                    { 2, Path.Combine(RepoDirectoriesProvider.Default.HostTestArtifacts, "Nested.tlb") }
                };

                // Use the locally built comhost to create a comhost with the embedded .clsidmap and type libraries
                ComHostPath = Path.Combine(ComLibrary.Location, $"{ComLibrary.AssemblyName}.comhost.dll");
                ComHost.Create(
                    Path.Combine(RepoDirectoriesProvider.Default.HostArtifacts, "comhost.dll"),
                    ComHostPath,
                    ClsidMapPath,
                    TypeLibraries);
            }

            protected override void Dispose(bool disposing)
            {
                ComLibrary?.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
