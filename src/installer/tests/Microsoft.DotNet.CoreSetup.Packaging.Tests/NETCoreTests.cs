// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.CoreSetup.Test;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Packaging.Tests
{
    public class NETCoreTests
    {
        private readonly RepoDirectoriesProvider dirs = new RepoDirectoriesProvider();

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
                $"Microsoft.NETCore.App.Host.{TestContext.BuildRID}"))
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
                $"Microsoft.NETCore.App.Runtime.{TestContext.BuildRID}"))
            {
                tester.IsRuntimePack();
            }
        }
    }
}
