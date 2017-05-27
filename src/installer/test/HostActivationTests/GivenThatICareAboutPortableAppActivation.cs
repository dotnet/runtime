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

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.PortableApp
{
    public class GivenThatICareAboutPortableAppActivation
    {
        private static TestProjectFixture PreviouslyBuiltAndRestoredPortableTestProjectFixture { get; set; }
        private static TestProjectFixture PreviouslyPublishedAndRestoredPortableTestProjectFixture { get; set; }
        private static RepoDirectoriesProvider RepoDirectories { get; set; }

        static GivenThatICareAboutPortableAppActivation()
        {
            RepoDirectories = new RepoDirectoriesProvider();

            PreviouslyBuiltAndRestoredPortableTestProjectFixture = new TestProjectFixture("PortableApp", RepoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages)
                .BuildProject();

            PreviouslyPublishedAndRestoredPortableTestProjectFixture = new TestProjectFixture("PortableApp", RepoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages)
                .PublishProject();
        }

        [Fact]
        public void Muxer_activation_of_Build_Output_Portable_DLL_with_DepsJson_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
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
        public void Muxer_activation_of_Apps_with_AltDirectorySeparatorChar()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll.Replace(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);

            dotnet.Exec(appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");
        }
        [Fact]
        public void Muxer_Exec_activation_of_Build_Output_Portable_DLL_with_DepsJson_Local_and_RuntimeConfig_Remote_Without_AdditionalProbingPath_Fails()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            MoveRuntimeConfigToSubdirectory(fixture);

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;
            var runtimeConfig = fixture.TestProject.RuntimeConfigJson;
            
            dotnet.Exec("exec", "--runtimeconfig", runtimeConfig, appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute(fExpectedToFail:true)
                .Should()
                .Fail();
        }

        [Fact]
        public void Muxer_Exec_activation_of_Build_Output_Portable_DLL_with_DepsJson_Local_and_RuntimeConfig_Remote_With_AdditionalProbingPath_Succeeds()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();

            MoveRuntimeConfigToSubdirectory(fixture);

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;
            var runtimeConfig = fixture.TestProject.RuntimeConfigJson;
            var additionalProbingPath = RepoDirectories.NugetPackages;

            dotnet.Exec(
                    "exec",
                    "--runtimeconfig", runtimeConfig,
                    "--additionalprobingpath", additionalProbingPath,
                    appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void Muxer_Activation_With_Templated_AdditionalProbingPath_Succeeds()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                .Copy();
            
            var store_path = CreateAStore(fixture);
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var destRuntimeDevConfig = fixture.TestProject.RuntimeDevConfigJson;
            if (File.Exists(destRuntimeDevConfig))
            {
                File.Delete(destRuntimeDevConfig);
            }

            var additionalProbingPath = store_path + "/|arch|/|tfm|";

            dotnet.Exec(
                    "exec",
                    "--additionalprobingpath", additionalProbingPath,
                    appDll)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World")
                .And
                .HaveStdErrContaining($"Adding tpa entry: {Path.Combine(store_path,fixture.RepoDirProvider.BuildArchitecture, fixture.Framework)}");
        }

        [Fact]
        public void Muxer_Exec_activation_of_Build_Output_Portable_DLL_with_DepsJson_Remote_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = PreviouslyBuiltAndRestoredPortableTestProjectFixture
                 .Copy();

            MoveDepsJsonToSubdirectory(fixture);

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;
            var depsJson = fixture.TestProject.DepsJson;

            dotnet.Exec("exec", "--depsfile", depsJson, appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");
            
        }

        [Fact]
        public void Muxer_activation_of_Publish_Output_Portable_DLL_with_DepsJson_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = PreviouslyPublishedAndRestoredPortableTestProjectFixture
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
        public void Muxer_Exec_activation_of_Publish_Output_Portable_DLL_with_DepsJson_Local_and_RuntimeConfig_Remote_Succeeds()
        {
            var fixture = PreviouslyPublishedAndRestoredPortableTestProjectFixture
                .Copy();

            MoveRuntimeConfigToSubdirectory(fixture);

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;
            var runtimeConfig = fixture.TestProject.RuntimeConfigJson;

            dotnet.Exec("exec", "--runtimeconfig", runtimeConfig, appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void Muxer_Exec_activation_of_Publish_Output_Portable_DLL_with_DepsJson_Remote_and_RuntimeConfig_Local_Fails()
        {
            var fixture = PreviouslyPublishedAndRestoredPortableTestProjectFixture
                 .Copy();

            MoveDepsJsonToSubdirectory(fixture);

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;
            var depsJson = fixture.TestProject.DepsJson;

            dotnet.Exec("exec", "--depsfile", depsJson, appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute(fExpectedToFail:true)
                .Should()
                .Fail();
        }

        private void MoveDepsJsonToSubdirectory(TestProjectFixture testProjectFixture)
        {
            var subdirectory = Path.Combine(testProjectFixture.TestProject.ProjectDirectory, "d");
            if (!Directory.Exists(subdirectory))
            {
                Directory.CreateDirectory(subdirectory);
            }

            var destDepsJson = Path.Combine(subdirectory, Path.GetFileName(testProjectFixture.TestProject.DepsJson));

            if (File.Exists(destDepsJson))
            {
                File.Delete(destDepsJson);
            }
            File.Move(testProjectFixture.TestProject.DepsJson, destDepsJson);

            testProjectFixture.TestProject.DepsJson = destDepsJson;
        }

        private void MoveRuntimeConfigToSubdirectory(TestProjectFixture testProjectFixture)
        {
            var subdirectory = Path.Combine(testProjectFixture.TestProject.ProjectDirectory, "r");
            if (!Directory.Exists(subdirectory))
            {
                Directory.CreateDirectory(subdirectory);
            }

            var destRuntimeConfig = Path.Combine(subdirectory, Path.GetFileName(testProjectFixture.TestProject.RuntimeConfigJson));

            if (File.Exists(destRuntimeConfig))
            {
                File.Delete(destRuntimeConfig);
            }
            File.Move(testProjectFixture.TestProject.RuntimeConfigJson, destRuntimeConfig);

            testProjectFixture.TestProject.RuntimeConfigJson = destRuntimeConfig;
        }

         private string CreateAStore(TestProjectFixture testProjectFixture)
        {
            var storeoutputDirectory = Path.Combine(testProjectFixture.TestProject.ProjectDirectory, "store");
            if (!Directory.Exists(storeoutputDirectory))
            {
                Directory.CreateDirectory(storeoutputDirectory);
            }

            testProjectFixture.StoreProject(outputDirectory :storeoutputDirectory);
            
            return storeoutputDirectory;
        }
    }
}
