// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

using Microsoft.DotNet.Cli.Build.Framework;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    [PlatformSpecific(TestPlatforms.Windows)] // IJW is only supported on Windows
    public class Ijwhost : IClassFixture<Ijwhost.SharedTestState>
    {
        private readonly SharedTestState sharedState;

        public Ijwhost(SharedTestState sharedTestState)
        {
            sharedState = sharedTestState;
        }

        [Fact]
        public void LoadLibrary()
        {
            string [] args = {
                "ijwhost",
                sharedState.IjwLibraryPath,
                "NativeEntryPoint"
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.RepoDirectories.BuiltDotnet)
                .Execute();

            result.Should().Pass()
                .And.HaveStdOutContaining("NativeEntryPoint: calling managed class")
                .And.HaveStdOutContaining("AssemblyLoadContext = \"IsolatedComponentLoadContext");
        }

        public class SharedTestState : SharedTestStateBase
        {
            public string IjwLibraryPath { get; }

            public SharedTestState()
            {
                string folder = Path.Combine(BaseDirectory, "ijw");
                Directory.CreateDirectory(folder);

                // Copy over ijwhost
                string ijwhostName = "ijwhost.dll";
                File.Copy(Path.Combine(RepoDirectories.HostArtifacts, ijwhostName), Path.Combine(folder, ijwhostName));

                // Copy over the C++/CLI test library
                string ijwLibraryName = "ijw.dll";
                IjwLibraryPath = Path.Combine(folder, ijwLibraryName);
                File.Copy(Path.Combine(RepoDirectories.Artifacts, "corehost_test", ijwLibraryName), IjwLibraryPath);

                // Create a runtimeconfig.json for the C++/CLI test library
                new RuntimeConfig(Path.Combine(folder, "ijw.runtimeconfig.json"))
                    .WithFramework(new RuntimeConfig.Framework(Constants.MicrosoftNETCoreApp, RepoDirectories.MicrosoftNETCoreAppVersion))
                    .Save();
            }
        }
    }
}
