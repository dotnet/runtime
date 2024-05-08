// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class BundleLocalizedApp : IClassFixture<BundleLocalizedApp.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundleLocalizedApp(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Bundled_Localized_App_Run_Succeeds()
        {
            var singleFile = sharedTestState.App.Bundle();
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("[kn-IN]! [ta-IN]! [default]!");
        }

        public class SharedTestState : IDisposable
        {
            public SingleFileTestApp App { get; set; }

            public SharedTestState()
            {
                App = SingleFileTestApp.CreateSelfContained("LocalizedApp");
            }

            public void Dispose()
            {
                App?.Dispose();
            }
        }
    }
}
