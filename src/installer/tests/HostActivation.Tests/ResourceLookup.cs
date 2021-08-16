// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class ResourceLookup : IClassFixture<ResourceLookup.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public ResourceLookup(ResourceLookup.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Muxer_activation_of_Build_Output_Resource_DLL_with_DepsJson_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = sharedTestState.ResourceLookupFixture_Built
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");

            dotnet.Exec("exec", appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }
        
        [Fact]
        public void Muxer_activation_of_Publish_Output_ResourceLookup_DLL_with_DepsJson_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = sharedTestState.ResourceLookupFixture_Published
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");

            dotnet.Exec("exec", appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture ResourceLookupFixture_Built { get; }
            public TestProjectFixture ResourceLookupFixture_Published { get; }
            public RepoDirectoriesProvider RepoDirectories { get; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                ResourceLookupFixture_Built = new TestProjectFixture("ResourceLookup", RepoDirectories)
                    .EnsureRestored()
                    .BuildProject();

                ResourceLookupFixture_Published = new TestProjectFixture("ResourceLookup", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();
            }

            public void Dispose()
            {
                ResourceLookupFixture_Built.Dispose();
                ResourceLookupFixture_Published.Dispose();
            }
        }
    }
}
