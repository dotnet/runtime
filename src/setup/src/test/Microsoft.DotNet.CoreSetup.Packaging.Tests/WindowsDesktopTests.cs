// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.CoreSetup.Test;
using System.Linq;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Packaging.Tests
{
    public class WindowsDesktopTests
    {
        private readonly RepoDirectoriesProvider dirs = new RepoDirectoriesProvider();

        [Fact]
        public void WindowsDesktopTargetingPackIsValid()
        {
            // Use "OrNull" variant to get null if this nupkg doesn't exist. WindowsDesktop is only
            // built on officially supported platforms.
            using (var tester = NuGetArtifactTester.OpenOrNull(
                dirs,
                "Microsoft.WindowsDesktop.App.Ref"))
            {
                if (CurrentRidShouldCreateNupkg)
                {
                    Assert.NotNull(tester);

                    tester.HasOnlyTheseDataFiles(
                        "data/FrameworkList.xml",
                        "data/PlatformManifest.txt");

                    tester.IsTargetingPack();
                    tester.HasGoodPlatformManifest();
                }
                else
                {
                    Assert.Null(tester);
                }
            }
        }

        [Fact]
        public void WindowsDesktopRuntimePackIsValid()
        {
            using (var tester = NuGetArtifactTester.OpenOrNull(
                dirs,
                "Microsoft.WindowsDesktop.App.Runtime",
                $"Microsoft.WindowsDesktop.App.Runtime.{dirs.BuildRID}"))
            {
                if (CurrentRidShouldCreateNupkg)
                {
                    Assert.NotNull(tester);

                    tester.IsRuntimePack();
                }
                else
                {
                    Assert.Null(tester);
                }
            }
        }

        private bool CurrentRidShouldCreateNupkg =>
            new[]
            {
                "win-x64",
                "win-x86"
            }.Contains(dirs.BuildRID);
    }
}
