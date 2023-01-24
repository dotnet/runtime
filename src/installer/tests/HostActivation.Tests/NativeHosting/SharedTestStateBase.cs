// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    public class SharedTestStateBase : IDisposable
    {
        public string BaseDirectory { get; }
        public string NativeHostPath { get; }
        public string NethostPath { get; }
        public RepoDirectoriesProvider RepoDirectories { get; }

        private readonly TestArtifact _baseDirArtifact;

        public SharedTestStateBase()
        {
            BaseDirectory = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "nativeHosting"));
            _baseDirArtifact = new TestArtifact(BaseDirectory);
            Directory.CreateDirectory(BaseDirectory);

            string nativeHostName = RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform("nativehost");
            NativeHostPath = Path.Combine(BaseDirectory, nativeHostName);

            // Copy over native host
            RepoDirectories = new RepoDirectoriesProvider();
            File.Copy(Path.Combine(RepoDirectories.Artifacts, "corehost_test", nativeHostName), NativeHostPath);

            // Copy nethost next to native host
            // This is done even for tests not directly using nethost because nativehost consumes nethost in the more
            // user-friendly way of linking against nethost (instead of dlopen/LoadLibrary and dlsym/GetProcAddress).
            // On Windows, we can delay load through a linker option, but on other platforms load is required on start.
            string nethostName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("nethost");
            NethostPath = Path.Combine(Path.GetDirectoryName(NativeHostPath), nethostName);
            File.Copy(
                Path.Combine(RepoDirectories.HostArtifacts, nethostName),
                NethostPath);
        }

        public Command CreateNativeHostCommand(IEnumerable<string> args, string dotNetRoot)
        {
            return Command.Create(NativeHostPath, args)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(dotNetRoot)
                .MultilevelLookup(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _baseDirArtifact.Dispose();
            }
        }
    }
}
