// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json.Linq;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public partial class MultilevelSharedFxLookup
    {
        [Fact]
        public void TPA_Version_Check_App_Wins()
        {
            string appAssembly;
            string uberAssembly;
            string netcoreAssembly;

            // Apps wins, 9999.0.0.1 vs Uber (existing version) and NetCore (also existing version)
            var fixture = ConfigureAppAndFrameworks("99.0.0.1", null, "7777.0.0", out appAssembly, out uberAssembly, out netcoreAssembly);
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                // Verify final selection in TRUSTED_PLATFORM_ASSEMBLIES
                .And.HaveStdErrContaining($"{appAssembly}{Path.PathSeparator}")
                .And.NotHaveStdErrContaining($"{netcoreAssembly}{Path.PathSeparator}")
                .And.NotHaveStdErrContaining($"{uberAssembly}{Path.PathSeparator}");
        }

        [Theory]
        [InlineData("0.0.0.1", "", "7777.0.0")]          // Uber wins, existing assembly version vs app (0.0.0.1) and NetCore (also existing version)
        [InlineData("99.0.0.1", "99.0.0.1", "7777.0.0")] // Tie case, no roll forward
        [InlineData("99.0.0.1", "99.0.0.1", "7777.0.1")] // Tie case, patch roll forward
        [InlineData("99.0.0.1", "99.0.0.1", "7777.1.0")] // Tie case, minor roll forward
        [InlineData("99.0.0.1", "99.0.0.1", "7778.0.0")] // Tie case, major roll forward
        public void TPA_Version_Check_UberFx_Wins(string appAssemblyVersion, string uberFxAssemblyVersion, string uberProductVersion)
        {
            string appAssembly;
            string uberAssembly;
            string netcoreAssembly;

            var fixture = ConfigureAppAndFrameworks(appAssemblyVersion, uberFxAssemblyVersion, uberProductVersion, out appAssembly, out uberAssembly, out netcoreAssembly);
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX", "2") // Allow major roll forward
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining(Path.Combine(_exeFoundUberFxMessage, uberProductVersion))
                // Verify final selection in TRUSTED_PLATFORM_ASSEMBLIES
                .And.HaveStdErrContaining($"{uberAssembly}{Path.PathSeparator}")
                .And.NotHaveStdErrContaining($"{netcoreAssembly}{Path.PathSeparator}")
                .And.NotHaveStdErrContaining($"{appAssembly}{Path.PathSeparator}");
        }

        [Fact]
        public void TPA_Version_Check_NetCore_Wins()
        {
            string appAssembly;
            string uberAssembly;
            string netcoreAssembly;

            // NetCore wins, existing assembly version vs app (0.0.0.1) and Uber (0.0.0.2)
            var fixture = ConfigureAppAndFrameworks("0.0.0.1", "0.0.0.2", "7777.0.0", out appAssembly, out uberAssembly, out netcoreAssembly);
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll)
                .WorkingDirectory(_currentWorkingDir)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                // Verify final selection in TRUSTED_PLATFORM_ASSEMBLIES
                .And.HaveStdErrContaining($"{netcoreAssembly}{Path.PathSeparator}")
                .And.NotHaveStdErrContaining($"{appAssembly}{Path.PathSeparator}")
                .And.NotHaveStdErrContaining($"{uberAssembly}{Path.PathSeparator}");
        }

        private TestProjectFixture ConfigureAppAndFrameworks(string appAssemblyVersion, string uberFxAssemblyVersion, string uberFxProductVersion, out string appAssembly, out string uberAssembly, out string netcoreAssembly)
        {
            const string fileVersion = "0.0.0.9";
            var fixture = SharedFxLookupPortableAppFixture
                .Copy();

            if (!string.IsNullOrEmpty(uberFxAssemblyVersion))
            {
                // Modify Uber Fx's deps.json
                SharedFramework.CreateUberFrameworkArtifacts(_builtSharedFxDir, _builtSharedUberFxDir, uberFxAssemblyVersion, fileVersion);
            }

            // Set desired version = 7777.0.0
            string runtimeConfig = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.runtimeconfig.json");
            SharedFramework.SetRuntimeConfigJson(runtimeConfig, "7777.0.0", null, useUberFramework: true);

            // Add versions in the exe folder
            SharedFramework.AddAvailableSharedFxVersions(_builtSharedFxDir, _exeSharedFxBaseDir, "9999.0.0");
            SharedFramework.AddAvailableSharedUberFxVersions(_builtSharedUberFxDir, _exeSharedUberFxBaseDir, "9999.0.0", uberFxProductVersion);

            // Copy NetCoreApp's copy of the assembly to the app location
            netcoreAssembly = Path.Combine(_exeSharedFxBaseDir, "9999.0.0", "System.Collections.Immutable.dll");
            appAssembly = Path.Combine(fixture.TestProject.OutputDirectory, "System.Collections.Immutable.dll");
            File.Copy(netcoreAssembly, appAssembly);

            // Modify the app's deps.json to add System.Collections.Immmutable
            string appDepsJson = Path.Combine(fixture.TestProject.OutputDirectory, "SharedFxLookupPortableApp.deps.json");
            JObject versionInfo = new JObject();
            versionInfo.Add(new JProperty("assemblyVersion", appAssemblyVersion));
            versionInfo.Add(new JProperty("fileVersion", fileVersion));
            SharedFramework.AddReferenceToDepsJson(appDepsJson, "SharedFxLookupPortableApp/1.0.0", "System.Collections.Immutable", "1.0.0", versionInfo);

            uberAssembly = Path.Combine(_exeSharedUberFxBaseDir, uberFxProductVersion, "System.Collections.Immutable.dll");

            return fixture;
        }
    }
}
