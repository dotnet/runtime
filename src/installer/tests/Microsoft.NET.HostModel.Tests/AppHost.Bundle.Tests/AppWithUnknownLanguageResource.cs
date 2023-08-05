// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Xunit;

namespace AppHost.Bundle.Tests
{
    // https://github.com/dotnet/runtime/issues/88465
    public class AppWithUnknownLanguageResource : BundleTestBase, IClassFixture<AppWithUnknownLanguageResource.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public AppWithUnknownLanguageResource(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        private void Can_Build_App_With_Resource_With_Unknown_Language()
        {
            var fixture = sharedTestState.TestFixture.Copy();

            var appExe = UseSingleFileSelfContainedHost(fixture);

            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");
        }

        public class SharedTestState : SharedTestStateBase, IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }

            public SharedTestState()
            {
                TestFixture = PreparePublishedSelfContainedTestProject("AppWithUnknownLanguageResource");
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
