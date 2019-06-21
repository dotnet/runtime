// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class DotnetArgValidation : IClassFixture<DotnetArgValidation.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public DotnetArgValidation(DotnetArgValidation.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Muxer_Exec_With_Missing_App_Assembly_Fails()
        {
            var fixture = sharedTestState.PortableAppFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            string assemblyName = Path.Combine(GetNonexistentAndUnnormalizedPath(), "foo.dll");

            dotnet.Exec("exec", assemblyName)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining($"The application to execute does not exist: '{assemblyName}'");
        }

        [Fact]
        public void Muxer_Exec_With_Missing_App_Assembly_And_Bad_Extension_Fails()
        {
            var fixture = sharedTestState.PortableAppFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            string assemblyName = Path.Combine(GetNonexistentAndUnnormalizedPath(), "foo.xzy");

            dotnet.Exec("exec", assemblyName)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining($"The application to execute does not exist: '{assemblyName}'");
        }

        [Fact]
        public void Muxer_Exec_With_Bad_Extension_Fails()
        {
            var fixture = sharedTestState.PortableAppFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            // Get a valid file name, but not exe or dll
            string fxDir = Path.Combine(fixture.SdkDotnet.BinPath, "shared", "Microsoft.NETCore.App");
            fxDir = new DirectoryInfo(fxDir).GetDirectories()[0].FullName;
            string assemblyName = Path.Combine(fxDir, "Microsoft.NETCore.App.deps.json");

            dotnet.Exec("exec", assemblyName)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining($"dotnet exec needs a managed .dll or .exe extension. The application specified was '{assemblyName}'");
        }

        [Fact]
        public void Detect_Missing_Argument_Value()
        {
            var fixture = sharedTestState.PortableAppFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            dotnet.Exec("--fx-version")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining($"Failed to parse supported options or their values:");
        }

        // Return a non-exisitent path that contains a mix of / and \
        private string GetNonexistentAndUnnormalizedPath()
        {
            return Path.Combine(sharedTestState.PortableAppFixture.SdkDotnet.BinPath, @"x\y/");
        }

        public class SharedTestState : IDisposable
        {
            public RepoDirectoriesProvider RepoDirectories { get; }
            public TestProjectFixture PortableAppFixture { get; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                PortableAppFixture = new TestProjectFixture("PortableApp", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .BuildProject();
            }

            public void Dispose()
            {
                PortableAppFixture.Dispose();
            }
        }
    }
}
