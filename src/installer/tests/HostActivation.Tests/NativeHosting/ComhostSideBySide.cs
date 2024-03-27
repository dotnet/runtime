// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection.Metadata;

using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.NET.HostModel.ComHost;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    [PlatformSpecific(TestPlatforms.Windows)] // COM activation is only supported on Windows
    public class ComhostSideBySide : IClassFixture<ComhostSideBySide.SharedTestState>
    {
        private readonly SharedTestState sharedState;

        public ComhostSideBySide(SharedTestState sharedTestState)
        {
            sharedState = sharedTestState;
        }

        [Fact]
        public void ActivateClass()
        {
            string [] args = {
                "activation",
                sharedState.ClsidString
            };

            CommandResult result = Command.Create(sharedState.ComSxsPath, args)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(TestContext.BuiltDotNet.BinPath)
                .MultilevelLookup(false)
                .Execute();

            result.Should().Pass()
                .And.HaveStdOutContaining("New instance of Server created");
        }

        [Fact]
        public void LocateEmbeddedTlb()
        {
            string [] args = {
                "typelib_lookup",
                sharedState.TypeLibId
            };

            CommandResult result = Command.Create(sharedState.ComSxsPath, args)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(TestContext.BuiltDotNet.BinPath)
                .MultilevelLookup(false)
                .Execute();

            result.Should().Pass()
                .And.HaveStdOutContaining("Located type library by typeid.");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ManagedHost(bool selfContained)
        {
            string [] args = {
                "comhost",
                sharedState.ClsidString
            };
            TestApp app = selfContained ? sharedState.ManagedHost_SelfContained : sharedState.ManagedHost_FrameworkDependent;
            CommandResult result = Command.Create(app.AppExe, args)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(TestContext.BuiltDotNet.BinPath)
                .MultilevelLookup(false)
                .Execute();

            result.Should().Pass()
                .And.HaveStdOutContaining("New instance of Server created")
                .And.HaveStdOutContaining($"Activation of {sharedState.ClsidString} succeeded.")
                .And.ExecuteSelfContained(selfContained);
        }

        public class SharedTestState : Comhost.SharedTestState
        {
            public string TypeLibId { get; } = "{20151109-a0e8-46ae-b28e-8ff2c0e72166}";

            public string ComSxsPath { get; }

            public TestApp ManagedHost_FrameworkDependent { get; }
            public TestApp ManagedHost_SelfContained { get; }

            public SharedTestState()
            {
                if (!OperatingSystem.IsWindows())
                {
                    // COM activation is only supported on Windows
                    return;
                }

                string comsxsDirectory = BaseDirectory;
                string regFreeManifestName = $"{ ComLibrary.AssemblyName }.X.manifest";
                string regFreeManifestPath = Path.Combine(comsxsDirectory, regFreeManifestName);
                using (var assemblyStream = new FileStream(ComLibrary.AppDll, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.Read))
                using (var peReader = new System.Reflection.PortableExecutable.PEReader(assemblyStream))
                {
                    if (peReader.HasMetadata)
                    {
                        MetadataReader reader = peReader.GetMetadataReader();
                        RegFreeComManifest.CreateManifestFromClsidmap(
                            ComLibrary.AssemblyName,
                            Path.GetFileName(ComHostPath),
                            reader.GetAssemblyDefinition().Version.ToString(),
                            ClsidMapPath,
                            regFreeManifestPath,
                            TypeLibraries
                        );
                    }
                }

                string comsxsName = Binaries.GetExeFileNameForCurrentPlatform("comsxs");
                ComSxsPath = Path.Combine(comsxsDirectory, comsxsName);
                File.Copy(
                    Path.Combine(RepoDirectoriesProvider.Default.HostTestArtifacts, comsxsName),
                    ComSxsPath);

                ManagedHost_FrameworkDependent = TestApp.CreateFromBuiltAssets("RegFreeCom");
                ManagedHost_FrameworkDependent.CreateAppHost();
                File.Copy(regFreeManifestPath, Path.Combine(ManagedHost_FrameworkDependent.Location, regFreeManifestName));

                ManagedHost_SelfContained = TestApp.CreateFromBuiltAssets("RegFreeCom");
                ManagedHost_SelfContained.PopulateSelfContained(TestApp.MockedComponent.None);
                ManagedHost_FrameworkDependent.CreateAppHost();
                File.Copy(regFreeManifestPath, Path.Combine(ManagedHost_SelfContained.Location, regFreeManifestName));

                // Copy the ComLibrary output and comhost to the ComSxS and ManagedHost directories
                string[] toCopy = {
                    ComLibrary.AppDll,
                    ComLibrary.DepsJson,
                    ComLibrary.RuntimeConfigJson,
                    ComHostPath,
                };
                foreach (string filePath in toCopy)
                {
                    File.Copy(filePath, Path.Combine(comsxsDirectory, Path.GetFileName(filePath)));
                    File.Copy(filePath, Path.Combine(ManagedHost_FrameworkDependent.Location, Path.GetFileName(filePath)));
                    File.Copy(filePath, Path.Combine(ManagedHost_SelfContained.Location, Path.GetFileName(filePath)));
                }
            }

            protected override void Dispose(bool disposing)
            {
                ManagedHost_FrameworkDependent?.Dispose();
                ManagedHost_SelfContained?.Dispose();

                base.Dispose(disposing);
            }
        }
    }
}
