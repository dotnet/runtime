// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Microsoft.DotNet.CoreSetup.Test;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Packaging.Tests
{
    public class NETCoreTests
    {
        private readonly RepoDirectoriesProvider dirs = RepoDirectoriesProvider.Default;

        [Fact]
        public void NETCoreTargetingPackIsValid()
        {
            using (var tester = NuGetArtifactTester.OpenOrNull(
                dirs,
                "Microsoft.NETCore.App.Ref"))
            {
                // Allow no targeting pack in case this is a servicing build.
                // This condition should be tightened: https://github.com/dotnet/runtime/issues/3836
                if (tester == null)
                {
                    return;
                }

                tester.IsTargetingPackForPlatform();
                tester.HasOnlyTheseDataFiles(
                    "data/FrameworkList.xml",
                    "data/PackageOverrides.txt",
                    "data/PlatformManifest.txt");
            }
        }

        [Fact]
        public void NETCoreAppHostPackIsValid()
        {
            using (var tester = NuGetArtifactTester.Open(
                dirs,
                "Microsoft.NETCore.App.Host",
                $"Microsoft.NETCore.App.Host.{HostTestContext.BuildRID}"))
            {
                tester.IsAppHostPack();
            }
        }

        [Fact]
        public void NETCoreRuntimePackIsValid()
        {
            using (var tester = NuGetArtifactTester.Open(
                dirs,
                "Microsoft.NETCore.App.Runtime",
                $"Microsoft.NETCore.App.Runtime.{HostTestContext.BuildRID}"))
            {
                tester.IsRuntimePack();

                XElement coreLib = Assert.Single(
                    tester.ReadEntryXDocument("data/RuntimeList.xml").Root.Elements("File"),
                    file => (string)file.Attribute("AssemblyName") == "System.Private.CoreLib");
                bool isCoreCLR = HostTestContext.RuntimeFlavor == "CoreCLR";
                string coreLibDirectory = isCoreCLR ? $"lib/{HostTestContext.Tfm}" : "native";
                Assert.Equal(isCoreCLR ? "Managed" : "Native", (string)coreLib.Attribute("Type"));
                Assert.Equal(
                    $"runtimes/{HostTestContext.BuildRID}/{coreLibDirectory}/System.Private.CoreLib.dll",
                    (string)coreLib.Attribute("Path"));
            }
        }
    }
}
