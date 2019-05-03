// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class PortableAppActivation : IClassFixture<PortableAppActivation.SharedTestState>
    {
        private readonly SharedTestState sharedTestState;

        public PortableAppActivation(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Muxer_activation_of_Build_Output_Portable_DLL_with_DepsJson_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture_Built
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
        public void Muxer_activation_of_Build_Output_Portable_DLL_with_DepsJson_having_Assembly_with_Different_File_Extension_Fails()
        {
            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            // Change *.dll to *.exe
            var appDll = fixture.TestProject.AppDll;
            var appExe = appDll.Replace(".dll", ".exe");
            File.Copy(appDll, appExe);
            File.Delete(appDll);

            dotnet.Exec("exec", appExe)
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining("has already been found but with a different file extension");
        }

        [Fact]
        public void Muxer_activation_of_Apps_with_AltDirectorySeparatorChar()
        {
            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            dotnet.Exec(appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }
        [Fact]
        public void Muxer_Exec_activation_of_Build_Output_Portable_DLL_with_DepsJson_Local_and_RuntimeConfig_Remote_Without_AdditionalProbingPath_Fails()
        {
            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            var runtimeConfig = MoveRuntimeConfigToSubdirectory(fixture);

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec("exec", "--runtimeconfig", runtimeConfig, appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute(fExpectedToFail: true)
                .Should().Fail();
        }

        [Fact]
        public void Muxer_Exec_activation_of_Build_Output_Portable_DLL_with_DepsJson_Local_and_RuntimeConfig_Remote_With_AdditionalProbingPath_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            var runtimeConfig = MoveRuntimeConfigToSubdirectory(fixture);

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;
            var additionalProbingPath = sharedTestState.RepoDirectories.NugetPackages;

            dotnet.Exec(
                    "exec",
                    "--runtimeconfig", runtimeConfig,
                    "--additionalprobingpath", additionalProbingPath,
                    appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void Muxer_Activation_With_Templated_AdditionalProbingPath_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture_Built
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
                .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdErrContaining($"Adding tpa entry: {Path.Combine(store_path, fixture.RepoDirProvider.BuildArchitecture, fixture.Framework)}");
        }

        [Fact]
        public void Muxer_Exec_activation_of_Build_Output_Portable_DLL_with_DepsJson_Remote_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            var depsJson = MoveDepsJsonToSubdirectory(fixture);

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec("exec", "--depsfile", depsJson, appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");

        }

        [Fact]
        public void Muxer_activation_of_Publish_Output_Portable_DLL_with_DepsJson_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture_Published
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
        public void Muxer_Exec_activation_of_Publish_Output_Portable_DLL_with_DepsJson_Local_and_RuntimeConfig_Remote_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture_Published
                .Copy();

            var runtimeConfig = MoveRuntimeConfigToSubdirectory(fixture);

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec("exec", "--runtimeconfig", runtimeConfig, appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void Muxer_Exec_activation_of_Publish_Output_Portable_DLL_with_DepsJson_Remote_and_RuntimeConfig_Local_Fails()
        {
            var fixture = sharedTestState.PortableAppFixture_Published
                .Copy();

            var depsJson = MoveDepsJsonToSubdirectory(fixture);

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec("exec", "--depsfile", depsJson, appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute(fExpectedToFail: true)
                .Should().Fail();
        }

        [Fact]
        public void Framework_Dependent_AppHost_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture_Published
                .Copy();

            // Since SDK doesn't support building framework dependent apphost yet, emulate that behavior
            // by creating the executable from apphost.exe
            var appExe = fixture.TestProject.AppExe;
            var appDllName = Path.GetFileName(fixture.TestProject.AppDll);

            string hostExeName = RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform("apphost");
            string builtAppHost = Path.Combine(sharedTestState.RepoDirectories.HostArtifacts, hostExeName);
            string appDir = Path.GetDirectoryName(appExe);
            string appDirHostExe = Path.Combine(appDir, hostExeName);

            // Make a copy of apphost first, replace hash and overwrite app.exe, rather than
            // overwrite app.exe and edit in place, because the file is opened as "write" for
            // the replacement -- the test fails with ETXTBSY (exit code: 26) in Linux when
            // executing a file opened in "write" mode.
            File.Copy(builtAppHost, appDirHostExe, true);
            using (var sha256 = SHA256.Create())
            {
                // Replace the hash with the managed DLL name.
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes("foobar"));
                var hashStr = BitConverter.ToString(hash).Replace("-", "").ToLower();
                AppHostExtensions.SearchAndReplace(appDirHostExe, Encoding.UTF8.GetBytes(hashStr), Encoding.UTF8.GetBytes(appDllName), true);
            }
            File.Copy(appDirHostExe, appExe, true);

            // Get the framework location that was built
            string builtDotnet = fixture.BuiltDotnet.BinPath;

            // Verify running with the default working directory
            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnvironmentVariable("DOTNET_ROOT", builtDotnet)
                .EnvironmentVariable("DOTNET_ROOT(x86)", builtDotnet)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining($"Framework Version:{sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion}");


            // Verify running from within the working directory
            Command.Create(appExe)
                .WorkingDirectory(fixture.TestProject.OutputDirectory)
                .EnvironmentVariable("DOTNET_ROOT", builtDotnet)
                .EnvironmentVariable("DOTNET_ROOT(x86)", builtDotnet)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining($"Framework Version:{sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion}");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Framework_Dependent_AppHost_From_Global_Location_Succeeds(bool useRegisteredLocation)
        {
            var fixture = sharedTestState.PortableAppFixture_Published
                .Copy();

            // Since SDK doesn't support building framework dependent apphost yet, emulate that behavior
            // by creating the executable from apphost.exe
            var appExe = fixture.TestProject.AppExe;
            var appDllName = Path.GetFileName(fixture.TestProject.AppDll);

            string hostExeName = RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform("apphost");
            string builtAppHost = Path.Combine(sharedTestState.RepoDirectories.HostArtifacts, hostExeName);
            string appDir = Path.GetDirectoryName(appExe);
            string appDirHostExe = Path.Combine(appDir, hostExeName);

            // Make a copy of apphost first, replace hash and overwrite app.exe, rather than
            // overwrite app.exe and edit in place, because the file is opened as "write" for
            // the replacement -- the test fails with ETXTBSY (exit code: 26) in Linux when
            // executing a file opened in "write" mode.
            File.Copy(builtAppHost, appDirHostExe, true);
            using (var sha256 = SHA256.Create())
            {
                // Replace the hash with the managed DLL name.
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes("foobar"));
                var hashStr = BitConverter.ToString(hash).Replace("-", "").ToLower();
                AppHostExtensions.SearchAndReplace(appDirHostExe, Encoding.UTF8.GetBytes(hashStr), Encoding.UTF8.GetBytes(appDllName), true);
            }
            File.Copy(appDirHostExe, appExe, true);

            // Get the framework location that was built
            string builtDotnet = fixture.BuiltDotnet.BinPath;

            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride())
            {
                string architecture = fixture.CurrentRid.Split('-')[1];
                if (useRegisteredLocation)
                {
                    registeredInstallLocationOverride.SetInstallLocation(builtDotnet, architecture);
                }

                // Verify running with the default working directory
                Command.Create(appExe)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                    .EnvironmentVariable(Constants.TestOnlyEnvironmentVariables.DefaultInstallPath, useRegisteredLocation ? null : builtDotnet)
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining("Hello World")
                    .And.HaveStdOutContaining($"Framework Version:{sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion}");

                // Verify running from within the working directory
                Command.Create(appExe)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .WorkingDirectory(fixture.TestProject.OutputDirectory)
                    .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                    .EnvironmentVariable(Constants.TestOnlyEnvironmentVariables.DefaultInstallPath, useRegisteredLocation ? null : builtDotnet)
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining("Hello World")
                    .And.HaveStdOutContaining($"Framework Version:{sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion}");
            }
        }

        [Fact]
        public void ComputedTPADoesntEndWithPathSeparator()
        {
            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrMatching($"Property TRUSTED_PLATFORM_ASSEMBLIES = .*[^{Path.PathSeparator}]$", System.Text.RegularExpressions.RegexOptions.Multiline);
        }


        private string MoveDepsJsonToSubdirectory(TestProjectFixture testProjectFixture)
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

            return destDepsJson;
        }

        private string MoveRuntimeConfigToSubdirectory(TestProjectFixture testProjectFixture)
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

            return destRuntimeConfig;
        }

        private string CreateAStore(TestProjectFixture testProjectFixture)
        {
            var storeoutputDirectory = Path.Combine(testProjectFixture.TestProject.ProjectDirectory, "store");
            if (!Directory.Exists(storeoutputDirectory))
            {
                Directory.CreateDirectory(storeoutputDirectory);
            }

            testProjectFixture.StoreProject(outputDirectory: storeoutputDirectory);

            return storeoutputDirectory;
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture PortableAppFixture_Built { get; }
            public TestProjectFixture PortableAppFixture_Published { get; }
            public RepoDirectoriesProvider RepoDirectories { get; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                PortableAppFixture_Built = new TestProjectFixture("PortableApp", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .BuildProject();

                PortableAppFixture_Published = new TestProjectFixture("PortableApp", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();
            }

            public void Dispose()
            {
                PortableAppFixture_Built.Dispose();
                PortableAppFixture_Published.Dispose();
            }
        }
    }
}
