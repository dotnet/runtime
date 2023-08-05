// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using BundleTests.Helpers;
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

            UseSingleFileSelfContainedHost(fixture);
        }

        public class SharedTestState : SharedTestStateBase, IDisposable
        {
            public TestProjectFixture TestFixture { get; set; }

            public SharedTestState()
            {
                var testFixture = new TestProjectFixture("AppWithUnknownLanguageResource", RepoDirectories);
                testFixture.EnsureRestoredForRid(testFixture.CurrentRid)
                    .PublishProject(outputDirectory: BundleHelper.GetPublishPath(testFixture));
                TestFixture = testFixture;
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
