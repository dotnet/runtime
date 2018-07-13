using FluentAssertions;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.ArgValidation
{
    public class GivenThatICareAboutDotnetArgValidationScenarios : IClassFixture<GivenThatICareAboutDotnetArgValidationScenarios.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public GivenThatICareAboutDotnetArgValidationScenarios(GivenThatICareAboutDotnetArgValidationScenarios.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Muxer_Exec_With_Missing_App_Assembly_Fails()
        {
            var fixture = sharedTestState.PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            string assemblyName = Path.Combine(GetNonexistentAndUnnormalizedPath(), "foo.dll");

            dotnet.Exec("exec", assemblyName)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining($"The application to execute does not exist: '{assemblyName}'");
        }

        [Fact]
        public void Muxer_Exec_With_Missing_App_Assembly_And_Bad_Extension_Fails()
        {
            var fixture = sharedTestState.PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            string assemblyName = Path.Combine(GetNonexistentAndUnnormalizedPath(), "foo.xzy");

            dotnet.Exec("exec", assemblyName)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining($"The application to execute does not exist: '{assemblyName}'");
        }

        [Fact]
        public void Muxer_Exec_With_Bad_Extension_Fails()
        {
            var fixture = sharedTestState.PreviouslyBuiltAndRestoredPortableTestProjectFixture
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
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining($"dotnet exec needs a managed .dll or .exe extension. The application specified was '{assemblyName}'");
        }

        [Fact]
        public void Detect_Missing_Argument_Value()
        {
            var fixture = sharedTestState.PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            dotnet.Exec("--fx-version")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining($"Failed to parse supported options or their values:");
        }

        // Return a non-exisitent path that contains a mix of / and \
        private string GetNonexistentAndUnnormalizedPath()
        {
            return Path.Combine(sharedTestState.PreviouslyBuiltAndRestoredPortableTestProjectFixture.SdkDotnet.BinPath, @"x\y/");
        }

        public class SharedTestState : IDisposable
        {
            public RepoDirectoriesProvider RepoDirectories { get; set; }
            public TestProjectFixture PreviouslyBuiltAndRestoredPortableTestProjectFixture { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                PreviouslyBuiltAndRestoredPortableTestProjectFixture = new TestProjectFixture("PortableApp", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .BuildProject();
            }

            public void Dispose()
            {
                PreviouslyBuiltAndRestoredPortableTestProjectFixture.Dispose();
            }
        }
    }
}
