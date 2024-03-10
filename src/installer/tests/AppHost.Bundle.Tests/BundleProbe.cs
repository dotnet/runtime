// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class BundleProbe : IClassFixture<BundleProbe.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundleProbe(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        private void SingleFileApp_ProbeFiles()
        {
            SingleFileTestApp app = sharedTestState.App;
            string singleFile = app.Bundle();

            (string Path, bool ShouldBeFound)[] itemsToProbe = new[]
            {
                ($"{app.AppName}.dll", true),
                ($"{app.AppName}.runtimeconfig.json", true),
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

        public class SharedTestState : IDisposable
        {
            public SingleFileTestApp App { get; set; }

            public SharedTestState()
            {
                App = SingleFileTestApp.CreateSelfContained("HostApiInvokerApp");
            }

            public void Dispose()
            {
                App?.Dispose();
            }
        }
    }
}
