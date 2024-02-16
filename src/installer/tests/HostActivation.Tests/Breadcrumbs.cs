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
    public class Breadcrumbs : IClassFixture<Breadcrumbs.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public Breadcrumbs(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void BreadcrumbThreadFinishes()
        {
            TestContext.BuiltDotNet.Exec(sharedTestState.App.AppDll)
                .EnvironmentVariable(Constants.Breadcrumbs.EnvironmentVariable, sharedTestState.BreadcrumbLocation)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdErrContaining("Done waiting for breadcrumb thread to exit...");
        }

        [Fact]
        public void UnhandledException_BreadcrumbThreadDoesNotFinish()
        {
            TestContext.BuiltDotNet.Exec(sharedTestState.App.AppDll, "throw_exception")
                .EnvironmentVariable(Constants.Breadcrumbs.EnvironmentVariable, sharedTestState.BreadcrumbLocation)
                .EnableTracingAndCaptureOutputs()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining("Unhandled exception.")
                .And.HaveStdErrContaining("System.Exception: Goodbye World")
                .And.NotHaveStdErrContaining("Done waiting for breadcrumb thread to exit...");
        }

        public class SharedTestState : IDisposable
        {
            public TestApp App { get; }
            public string BreadcrumbLocation { get; }

            public SharedTestState()
            {
                App = TestApp.CreateFromBuiltAssets("HelloWorld");
                if (!OperatingSystem.IsWindows())
                {
                    // On non-Windows breadcrumbs are only written if the breadcrumb directory already exists,
                    // so we explicitly create a directory for breadcrumbs
                    BreadcrumbLocation = Path.Combine(
                        App.Location,
                        "opt",
                        "corebreadcrumbs");
                    Directory.CreateDirectory(BreadcrumbLocation);
                }
            }

            public void Dispose()
            {
                App?.Dispose();
            }
        }
    }
}
