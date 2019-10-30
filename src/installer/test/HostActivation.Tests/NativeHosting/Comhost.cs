// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build.Framework;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    public class Comhost : IClassFixture<Comhost.SharedTestState>
    {
        private readonly SharedTestState sharedState;

        public Comhost(SharedTestState sharedTestState)
        {
            sharedState = sharedTestState;
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(10, true)]
        [InlineData(10, false)]
        public void ActivateClass(int count, bool synchronous)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // COM activation is only supported on Windows
                return;
            }

            string [] args = {
                "comhost",
                synchronous ? "synchronous" : "concurrent",
                $"{count}",
                sharedState.ComHostPath,
                sharedState.ClsidString
            };
            CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.ComLibraryFixture.BuiltDotnet.BinPath)
                .Execute();

            result.Should().Pass()
                .And.HaveStdOutContaining("New instance of Server created");

            for (var i = 1; i <= count; ++i)
            {
                result.Should().HaveStdOutContaining($"Activation of {sharedState.ClsidString} succeeded. {i} of {count}");
            }
        }

        [Fact]
        public void ActivateClass_IgnoreAppLocalHostFxr()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // COM activation is only supported on Windows
                return;
            }

            using (var fixture = sharedState.ComLibraryFixture.Copy())
            {
                File.WriteAllText(Path.Combine(fixture.TestProject.BuiltApp.Location, "hostfxr.dll"), string.Empty);
                var comHostWithAppLocalFxr = Path.Combine(
                    fixture.TestProject.BuiltApp.Location,
                    $"{ fixture.TestProject.AssemblyName }.comhost.dll");

                string[] args = {
                    "comhost",
                    "synchronous",
                    "1",
                    comHostWithAppLocalFxr,
                    sharedState.ClsidString
                    };
                CommandResult result = sharedState.CreateNativeHostCommand(args, fixture.BuiltDotnet.BinPath)
                    .Execute();

                result.Should().Pass()
                    .And.HaveStdOutContaining("New instance of Server created")
                    .And.HaveStdOutContaining($"Activation of {sharedState.ClsidString} succeeded.")
                    .And.HaveStdErrContaining("Using environment variable DOTNET_ROOT");
            }
        }

        [Fact]
        public void ActivateClass_ValidateIErrorInfoResult()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // COM activation is only supported on Windows
                return;
            }

            using (var fixture = sharedState.ComLibraryFixture.Copy())
            {
                string missingRuntimeConfig = Path.Combine(fixture.TestProject.BuiltApp.Location,
                            $"{ fixture.TestProject.AssemblyName }.runtimeconfig.json");

                File.Delete(missingRuntimeConfig);

                var comHost = Path.Combine(
                    fixture.TestProject.BuiltApp.Location,
                    $"{ fixture.TestProject.AssemblyName }.comhost.dll");

                string[] args = {
                    "comhost",
                    "errorinfo",
                    "1",
                    comHost,
                    sharedState.ClsidString
                };
                CommandResult result = sharedState.CreateNativeHostCommand(args, fixture.BuiltDotnet.BinPath)
                    .Execute();

                result.Should().Pass()
                    .And.HaveStdOutContaining($"The specified runtimeconfig.json [{missingRuntimeConfig}] does not exist");
            }
        }

        public class SharedTestState : SharedTestStateBase
        {
            public string ComHostPath { get; }

            public string ClsidString = "{438968CE-5950-4FBC-90B0-E64691350DF5}";
            public TestProjectFixture ComLibraryFixture { get; }

            public SharedTestState()
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // COM activation is only supported on Windows
                    return;
                }

                ComLibraryFixture = new TestProjectFixture("ComLibrary", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .BuildProject();

                // [TODO] BEGIN - Remove once using .NET Core 3.0 to build tests
                ComHostPath = Path.Combine(
                    ComLibraryFixture.TestProject.BuiltApp.Location,
                    $"{ ComLibraryFixture.TestProject.AssemblyName }.comhost.dll");

                File.Copy(Path.Combine(RepoDirectories.CorehostPackages, "comhost.dll"), ComHostPath);

                RuntimeConfig.FromFile(ComLibraryFixture.TestProject.RuntimeConfigJson)
                    .WithFramework(new RuntimeConfig.Framework("Microsoft.NETCore.App", RepoDirectories.MicrosoftNETCoreAppVersion))
                    .Save();

                JObject clsidMap = new JObject()
                {
                    {
                        ClsidString,
                        new JObject() { {"assembly", "ComLibrary" }, {"type", "ComLibrary.Server" } }
                    }
                };
                File.WriteAllText($"{ ComHostPath }.clsidmap", clsidMap.ToString());
                // [TODO] END - Remove once using .NET Core 3.0 to build tests
            }

            protected override void Dispose(bool disposing)
            {
                if (ComLibraryFixture != null)
                    ComLibraryFixture.Dispose();

                base.Dispose(disposing);
            }
        }
    }
}
