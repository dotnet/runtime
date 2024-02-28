// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class StaticHost
    {
        [Fact]
        private void NotMarkedAsBundle_RunSelfContainedApp()
        {
            using (TestApp app = TestApp.CreateFromBuiltAssets("HelloWorld"))
            {
                app.PopulateSelfContained(TestApp.MockedComponent.None);
                app.CreateSingleFileHost();

                Command.Create(app.AppExe)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining("Hello World");
            }
        }
    }
}
