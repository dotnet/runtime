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
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // COM activation is only supported on Windows
                return;
            }

            string [] args = {
                "activation",
                sharedState.ClsidString
            };

            CommandResult result = Command.Create(sharedState.ComSxsPath, args)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(sharedState.ComLibraryFixture.BuiltDotnet.BinPath)
                .MultilevelLookup(false)
                .Execute();

            result.Should().Pass()
                .And.HaveStdOutContaining("New instance of Server created");
        }

        [Fact]
        public void LocateEmbeddedTlb()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // COM activation is only supported on Windows
                return;
            }

            string [] args = {
                "typelib_lookup",
                sharedState.TypeLibId
            };

            CommandResult result = Command.Create(sharedState.ComSxsPath, args)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(sharedState.ComLibraryFixture.BuiltDotnet.BinPath)
                .MultilevelLookup(false)
                .Execute();

            result.Should().Pass()
                .And.HaveStdOutContaining("Located type library by typeid.");
        }

        public class SharedTestState : Comhost.SharedTestState
        {
            public string TypeLibId { get; } = "{20151109-a0e8-46ae-b28e-8ff2c0e72166}";

            public string ComSxsPath { get; }

            public SharedTestState()
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // COM activation is only supported on Windows
                    return;
                }

                using (var assemblyStream = new FileStream(ComLibraryFixture.TestProject.AppDll, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.Read))
                using (var peReader = new System.Reflection.PortableExecutable.PEReader(assemblyStream))
                {
                    if (peReader.HasMetadata)
                    {
                        string regFreeManifestPath = Path.Combine(BaseDirectory, $"{ ComLibraryFixture.TestProject.AssemblyName }.X.manifest");

                        MetadataReader reader = peReader.GetMetadataReader();
                        RegFreeComManifest.CreateManifestFromClsidmap(
                            ComLibraryFixture.TestProject.AssemblyName,
                            Path.GetFileName(ComHostPath),
                            reader.GetAssemblyDefinition().Version.ToString(),
                            ClsidMapPath,
                            regFreeManifestPath,
                            TypeLibraries
                        );
                    }
                }

                string testDirectoryPath = Path.GetDirectoryName(NativeHostPath);
                string comsxsName = RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform("comsxs");
                ComSxsPath = Path.Combine(testDirectoryPath, comsxsName);
                File.Copy(
                    Path.Combine(RepoDirectories.Artifacts, "corehost_test", comsxsName),
                    ComSxsPath);
                File.Copy(
                    ComHostPath,
                    Path.Combine(testDirectoryPath, Path.GetFileName(ComHostPath)));
                File.Copy(
                    ComLibraryFixture.TestProject.AppDll,
                    Path.Combine(testDirectoryPath, Path.GetFileName(ComLibraryFixture.TestProject.AppDll)));
                File.Copy(
                    ComLibraryFixture.TestProject.DepsJson,
                    Path.Combine(testDirectoryPath, Path.GetFileName(ComLibraryFixture.TestProject.DepsJson)));
                File.Copy(
                    ComLibraryFixture.TestProject.RuntimeConfigJson,
                    Path.Combine(testDirectoryPath, Path.GetFileName(ComLibraryFixture.TestProject.RuntimeConfigJson)));
            }
        }
    }
}
