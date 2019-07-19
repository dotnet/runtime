// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.CoreSetup.Test;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Packaging.Tests
{
    public class NETStandardTests
    {
        private readonly RepoDirectoriesProvider dirs = new RepoDirectoriesProvider();

        [Fact]
        public void NETStandardTargetingPackIsValid()
        {
            using (var tester = NuGetArtifactTester.Open(
                dirs,
                "NETStandard.Library.Ref"))
            {
                tester.HasOnlyTheseDataFiles(
                    "data/FrameworkList.xml");

                tester.IsTargetingPack();

                // Most artifacts in the repo use the global Major.Minor, this package doesn't. Test
                // this to make sure infra doesn't regress and cause netstandard to lose its special
                // 2.1 version. The versioning difference is because netstandard targeting pack
                // creation doesn't actually belong in Core-Setup: https://github.com/dotnet/standard/issues/1209
                Assert.Equal(
                    (2, 1),
                    (tester.PackageVersion.Major, tester.PackageVersion.Minor));
            }
        }
    }
}
