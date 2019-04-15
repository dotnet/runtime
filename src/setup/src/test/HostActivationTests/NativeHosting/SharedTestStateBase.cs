// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    public class SharedTestStateBase : IDisposable
    {
        public string BaseDirectory { get; }
        public string NativeHostPath { get; }
        public RepoDirectoriesProvider RepoDirectories { get; }

        public SharedTestStateBase()
        {
            BaseDirectory = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "nativeHosting"));
            Directory.CreateDirectory(BaseDirectory);

            string nativeHostName = RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform("nativehost");
            NativeHostPath = Path.Combine(BaseDirectory, nativeHostName);

            // Copy over native host
            RepoDirectories = new RepoDirectoriesProvider();
            File.Copy(Path.Combine(RepoDirectories.Artifacts, "corehost_test", nativeHostName), NativeHostPath);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!TestArtifact.PreserveTestRuns() && Directory.Exists(BaseDirectory))
            {
                Directory.Delete(BaseDirectory, true);
            }
        }
    }
}
