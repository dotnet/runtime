// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class MockCoreClrSanity : IDisposable
    {
        private readonly DotNetCli DotNet;

        private readonly TestArtifact _dotnetDirArtifact;

        public MockCoreClrSanity()
        {
            _dotnetDirArtifact = new TestArtifact(Path.Combine(TestArtifact.TestArtifactsPath, "mockCoreclrSanity"));

            DotNet = new DotNetBuilder(_dotnetDirArtifact.Location, Path.Combine(TestArtifact.TestArtifactsPath, "sharedFrameworkPublish"), "exe")
                .AddMicrosoftNETCoreAppFrameworkMockCoreClr("9999.0.0")
                .Build();
        }

        public void Dispose()
        {
            _dotnetDirArtifact.Dispose();
        }

        [Fact]
        public void Muxer_ListRuntimes()
        {
            DotNet.Exec("--list-runtimes")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Microsoft.NETCore.App 9999.0.0");
        }

        [Fact]
        public void Muxer_ExecAppSequence()
        {
            var appDll = typeof(MockCoreClrSanity).Assembly.Location;
            char sep = Path.DirectorySeparatorChar;

            DotNet.Exec("--roll-forward-on-no-candidate-fx", "2", appDll, "argumentOne", "arg2")
                .CaptureStdOut()
                .CaptureStdErr()
                .MultilevelLookup(false)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("mock coreclr_initialize() called")
                .And.HaveStdOutContaining("mock property[TRUSTED_PLATFORM_ASSEMBLIES]")
                .And.HaveStdOutContaining($"Microsoft.NETCore.App{sep}9999.0.0{sep}Microsoft.NETCore.App.deps.json")
                .And.HaveStdOutContaining("mock coreclr_execute_assembly() called")
                .And.HaveStdOutContaining("mock argc:2")
                .And.HaveStdOutContaining($"mock managedAssemblyPath:{appDll}")
                .And.HaveStdOutContaining("mock argv[0] = argumentOne")
                .And.HaveStdOutContaining("mock argv[1] = arg2")
                .And.HaveStdOutContaining("mock coreclr_shutdown_2() called");
        }
    }
}
