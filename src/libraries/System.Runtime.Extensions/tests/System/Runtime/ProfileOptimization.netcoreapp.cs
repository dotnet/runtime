// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Runtime.Tests
{
    public class ProfileOptimizationTest : FileCleanupTestBase
    {
        // Active issue https://github.com/dotnet/corefx/issues/31792
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotRedHatFamily6))]
        public void ProfileOptimization_CheckFileExists()
        {
            string profileFile = GetTestFileName();

            RemoteExecutor.Invoke((_profileFile) =>
            {
                // tracking down why test sporadically fails on RedHat69
                // write to the file first to check permissions
                // See https://github.com/dotnet/corefx/issues/31792
                File.WriteAllText(_profileFile, "42");

                // Verify this write succeeded
                Assert.True(File.Exists(_profileFile), $"'{_profileFile}' does not exist");
                Assert.True(new FileInfo(_profileFile).Length > 0, $"'{_profileFile}' is empty");

                // Delete the file and verify the delete
                File.Delete(_profileFile);
                Assert.True(!File.Exists(_profileFile), $"'{_profileFile} ought to not exist now");

                // Perform the test work
                ProfileOptimization.SetProfileRoot(Path.GetDirectoryName(_profileFile));
                ProfileOptimization.StartProfile(Path.GetFileName(_profileFile));

            }, profileFile).Dispose();

            // profileFile should deterministically exist now -- if not, wait 5 seconds
            bool existed = File.Exists(profileFile);
            if (!existed)
            {
                Thread.Sleep(5000);
            }

            Assert.True(File.Exists(profileFile), $"'{profileFile}' does not exist");
            Assert.True(new FileInfo(profileFile).Length > 0, $"'{profileFile}' is empty");

            Assert.True(existed, $"'{profileFile}' did not immediately exist, but did exist 5 seconds later");
        }
    }
}
