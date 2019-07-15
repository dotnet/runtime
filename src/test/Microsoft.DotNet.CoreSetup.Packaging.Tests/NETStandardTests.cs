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
            }
        }
    }
}
