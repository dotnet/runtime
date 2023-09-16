// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using BundleTests.Helpers;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class BundleProbe : BundleTestBase, IClassFixture<BundleProbe.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundleProbe(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        private void NonSingleFileApp_NoProbe()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            string appExe = BundleHelper.GetHostPath(fixture);

            Command.Create(appExe, "host_runtime_contract.bundle_probe")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("host_runtime_contract.bundle_probe is not set");
        }

        [Fact]
        private void SingleFileApp_ProbeFiles()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            string singleFile = BundleSelfContainedApp(fixture);

            (string Path, bool ShouldBeFound)[] itemsToProbe = new[]
            {
                ($"{fixture.TestProject.AssemblyName}.dll", true),
                ($"{fixture.TestProject.AssemblyName}.runtimeconfig.json", true),
                ("System.Private.CoreLib.dll", true),
                ("hostpolicy.dll", false),
                ("--", false),
                (string.Empty, false),
            };

            var result = Command.Create(singleFile, $"host_runtime_contract.bundle_probe {string.Join(" ", itemsToProbe.Select(i => i.Path))}")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute();

            result.Should().Pass();
            foreach (var item in itemsToProbe)
            {
                result.Should().HaveStdOutContaining($"{item.Path} - found = {item.ShouldBeFound}");
            }
        }

        public class SharedTestState : SharedTestStateBase, IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }

            public SharedTestState()
            {
                TestFixture = PreparePublishedSelfContainedTestProject("HostApiInvokerApp");
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
