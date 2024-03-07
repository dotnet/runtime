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

        private readonly TestArtifact _baseDirArtifact;

        public SharedTestStateBase()
        {
            _baseDirArtifact = TestArtifact.Create("nativeHosting");
            BaseDirectory = _baseDirArtifact.Location;

            string nativeHostName = Binaries.GetExeFileNameForCurrentPlatform("nativehost");
            NativeHostPath = Path.Combine(BaseDirectory, nativeHostName);

            // Copy over native host
            File.Copy(Path.Combine(RepoDirectoriesProvider.Default.HostTestArtifacts, nativeHostName), NativeHostPath);

            // Copy nethost next to native host
            // This is done even for tests not directly using nethost because nativehost consumes nethost in the more
            // user-friendly way of linking against nethost (instead of dlopen/LoadLibrary and dlsym/GetProcAddress).
            // On Windows, we can delay load through a linker option, but on other platforms load is required on start.
            NethostPath = Path.Combine(Path.GetDirectoryName(NativeHostPath), Binaries.NetHost.FileName);
            File.Copy(
                Binaries.NetHost.FilePath,
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
