// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.DotNet.CoreSetup.Test;
using Xunit;

namespace Microsoft.NET.HostModel.Tests
{
    // https://github.com/dotnet/runtime/issues/88465
    public class AppWithUnknownLanguageResource : IClassFixture<AppWithUnknownLanguageResource.SharedTestState>
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

            fixture.TestProject.BuiltApp.CreateAppHost();
        }

        public class SharedTestState : IDisposable
        {
            public RepoDirectoriesProvider RepoDirectories { get; set; }
            public TestProjectFixture TestFixture { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();
                var testFixture = new TestProjectFixture("AppWithUnknownLanguageResource", RepoDirectories);
                testFixture.EnsureRestored().BuildProject();
                TestFixture = testFixture;
            }

            public void Dispose()
            {
                TestFixture.Dispose();
            }
        }
    }
}
