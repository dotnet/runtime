// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Xunit;
using FluentAssertions;
using Microsoft.DotNet.CoreSetup.Test;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.ResourceLookup
{
    public class GivenThatICareAboutResourceLookup : IClassFixture<GivenThatICareAboutResourceLookup.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public GivenThatICareAboutResourceLookup(GivenThatICareAboutResourceLookup.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Muxer_activation_of_Build_Output_Resource_DLL_with_DepsJson_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = sharedTestState.PreviouslyBuiltAndRestoredResourceLookupTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");

            dotnet.Exec("exec", appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");
        }
        
        [Fact]
        public void Muxer_activation_of_Publish_Output_ResourceLookup_DLL_with_DepsJson_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredResourceLookupTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");

            dotnet.Exec("exec", appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture PreviouslyBuiltAndRestoredResourceLookupTestProjectFixture { get; set; }
            public TestProjectFixture PreviouslyPublishedAndRestoredResourceLookupTestProjectFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                PreviouslyBuiltAndRestoredResourceLookupTestProjectFixture = new TestProjectFixture("ResourceLookup", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .BuildProject();

                PreviouslyPublishedAndRestoredResourceLookupTestProjectFixture = new TestProjectFixture("ResourceLookup", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();
            }

            public void Dispose()
            {
                PreviouslyBuiltAndRestoredResourceLookupTestProjectFixture.Dispose();
                PreviouslyPublishedAndRestoredResourceLookupTestProjectFixture.Dispose();
            }
        }
    }
}
