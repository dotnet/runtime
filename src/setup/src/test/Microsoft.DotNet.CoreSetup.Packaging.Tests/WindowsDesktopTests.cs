// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.CoreSetup.Test;
using System;
using System.Linq;
using System.Xml.Linq;
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

                    tester.IsTargetingPackForPlatform();
                }
                else
                {
                    Assert.Null(tester);
                }
            }
        }

        [Fact]
        public void WindowsDesktopFrameworkListHasClassifications()
        {
            using (var tester = NuGetArtifactTester.OpenOrNull(
                dirs,
                "Microsoft.WindowsDesktop.App.Ref"))
            {
                // Let other test case handle if this is OK.
                if (tester == null)
                {
                    return;
                }

                XDocument fxList = tester.ReadEntryXDocument("data/FrameworkList.xml");
                var files = fxList.Element("FileList").Elements("File").ToArray();

                // Sanity check: did any elements we expect make it into the final file?
                foreach (var attributeName in new[] { "Profile", "ReferencedByDefault" })
                {
                    Assert.True(
                        files.Any(x => !string.IsNullOrEmpty(x.Attribute(attributeName)?.Value)),
                        $"Can't find a non-empty '{attributeName}' attribute in framework list.");
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
