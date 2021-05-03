// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Runtime.Tests
{
    public class ProfileOptimizationTest : FileCleanupTestBase
    {
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31853", TestRuntimes.Mono)]
        public void ProfileOptimization_CheckFileExists(bool stopProfile)
        {
            string profileFile = GetTestFileName();

            RemoteExecutor.Invoke((_profileFile, _stopProfile) =>
            {
                // Perform the test work
                ProfileOptimization.SetProfileRoot(Path.GetDirectoryName(_profileFile));
                ProfileOptimization.StartProfile(Path.GetFileName(_profileFile));

                if (bool.Parse(_stopProfile))
                {
                    ProfileOptimization.StartProfile(null);
                    CheckProfileFileExists(_profileFile);
                }

            }, profileFile, stopProfile.ToString()).Dispose();

            CheckProfileFileExists(profileFile);
        }

        static void CheckProfileFileExists(string profileFile)
        {
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
