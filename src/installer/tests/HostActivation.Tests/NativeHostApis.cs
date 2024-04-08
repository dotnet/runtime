// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.TestUtils;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class NativeHostApis : IClassFixture<NativeHostApis.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public NativeHostApis(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        private class ApiNames
        {
            public const string hostfxr_get_available_sdks = nameof(hostfxr_get_available_sdks);
            public const string hostfxr_resolve_sdk2 = nameof(hostfxr_resolve_sdk2);
            public const string hostfxr_get_dotnet_environment_info = nameof(hostfxr_get_dotnet_environment_info);
        }

        internal sealed class SdkAndFrameworkFixture : IDisposable
        {
            private readonly TestArtifact _artifact;

            public string EmptyGlobalJsonDir => Path.Combine(_artifact.Location, "wd");

            public string ExeDir => Path.Combine(_artifact.Location, "ed");
            public string LocalSdkDir => Path.Combine(ExeDir, "sdk");
            public string LocalFrameworksDir => Path.Combine(ExeDir, "shared");
            public string[] LocalSdks = new[] { "0.1.2", "5.6.7-preview", "1.2.3" };
            public List<(string fwName, string[] fwVersions)> LocalFrameworks =
                new List<(string fwName, string[] fwVersions)>()
                {
                    ("HostFxr.Test.B", new[] { "4.0.0", "5.6.7-A" }),
                    ("HostFxr.Test.C", new[] { "3.0.0" })
                };

            public string ProgramFiles => Path.Combine(_artifact.Location, "pf");
            public string ProgramFilesGlobalSdkDir => Path.Combine(ProgramFiles, "dotnet", "sdk");
            public string ProgramFilesGlobalFrameworksDir => Path.Combine(ProgramFiles, "dotnet", "shared");
            public string[] ProgramFilesGlobalSdks = new[] { "4.5.6", "1.2.3", "2.3.4-preview" };
            public List<(string fwName, string[] fwVersions)> ProgramFilesGlobalFrameworks =
                new List<(string fwName, string[] fwVersions)>()
                {
                    ("HostFxr.Test.A", new[] { "1.2.3", "3.0.0" }),
                    ("HostFxr.Test.B", new[] { "5.6.7-A" })
                };

            public string SelfRegistered => Path.Combine(_artifact.Location, "sr");
            public string SelfRegisteredGlobalSdkDir => Path.Combine(SelfRegistered, "sdk");
            public string[] SelfRegisteredGlobalSdks = new[] { "3.0.0", "15.1.4-preview", "5.6.7" };

            public SdkAndFrameworkFixture()
            {
                _artifact = TestArtifact.Create(nameof(SdkAndFrameworkFixture));

                Directory.CreateDirectory(EmptyGlobalJsonDir);

                // start with an empty global.json, it will be ignored, but prevent one lying on disk
                // on a given machine from impacting the test.
                GlobalJson.CreateEmpty(EmptyGlobalJsonDir);

                foreach (string sdk in ProgramFilesGlobalSdks)
                {
                    AddSdkDirectory(ProgramFilesGlobalSdkDir, sdk);
                }
                foreach (string sdk in SelfRegisteredGlobalSdks)
                {
                    AddSdkDirectory(SelfRegisteredGlobalSdkDir, sdk);
                }
                foreach (string sdk in LocalSdks)
                {
                    AddSdkDirectory(LocalSdkDir, sdk);
                }

                // Empty SDK directory - this should not be recognized as a valid SDK directory
                Directory.CreateDirectory(Path.Combine(LocalSdkDir, "9.9.9"));

                foreach ((string fwName, string[] fwVersions) in ProgramFilesGlobalFrameworks)
                {
                    foreach (string fwVersion in fwVersions)
                        AddFrameworkDirectory(ProgramFilesGlobalFrameworksDir, fwName, fwVersion);
                }
                foreach ((string fwName, string[] fwVersions) in LocalFrameworks)
                {
                    foreach (string fwVersion in fwVersions)
                        AddFrameworkDirectory(LocalFrameworksDir, fwName, fwVersion);

                    // Empty framework directory - this should not be recognized as a valid framework directory
                    Directory.CreateDirectory(Path.Combine(LocalFrameworksDir, fwName, "9.9.9"));
                }

                static void AddSdkDirectory(string sdkDir, string version)
                {
                    string versionDir = Path.Combine(sdkDir, version);
                    Directory.CreateDirectory(versionDir);
                    File.WriteAllText(Path.Combine(versionDir, "dotnet.dll"), string.Empty);
                }

                static void AddFrameworkDirectory(string frameworkDir, string name, string version)
                {
                    string versionDir = Path.Combine(frameworkDir, name, version);
                    Directory.CreateDirectory(versionDir);
                    File.WriteAllText(Path.Combine(versionDir, $"{name}.deps.json"), string.Empty);
                }
            }

            public void Dispose()
            {
                _artifact.Dispose();
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // The test setup only works on Windows (and MLL was Windows-only anyway)
        public void Hostfxr_get_available_sdks_with_multilevel_lookup()
        {
            // Starting with .NET 7, multi-level lookup is completely disabled for hostfxr API calls.
            // This test is still valuable to validate that it is in fact disabled
            var f = sharedTestState.SdkAndFrameworkFixture;
            string expectedList = string.Join(';', new[]
            {
                Path.Combine(f.LocalSdkDir, "0.1.2"),
                Path.Combine(f.LocalSdkDir, "1.2.3"),
                Path.Combine(f.LocalSdkDir, "5.6.7-preview"),
            });

            string api = ApiNames.hostfxr_get_available_sdks;
            sharedTestState.TestBehaviorEnabledDotNet.Exec(sharedTestState.HostApiInvokerApp.AppDll, api, f.ExeDir)
                .EnvironmentVariable("TEST_MULTILEVEL_LOOKUP_PROGRAM_FILES", f.ProgramFiles)
                .EnvironmentVariable("TEST_MULTILEVEL_LOOKUP_SELF_REGISTERED", f.SelfRegistered)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.ReturnStatusCode(api, Constants.ErrorCode.Success)
                .And.HaveStdOutContaining($"{api} sdks:[{expectedList}]");
        }

        [Fact]
        public void Hostfxr_get_available_sdks()
        {
            // Get SDKs sorted by ascending version

            var f = sharedTestState.SdkAndFrameworkFixture;
            string expectedList = string.Join(';', new[]
            {
                 Path.Combine(f.LocalSdkDir, "0.1.2"),
                 Path.Combine(f.LocalSdkDir, "1.2.3"),
                 Path.Combine(f.LocalSdkDir, "5.6.7-preview"),
            });

            string api = ApiNames.hostfxr_get_available_sdks;
            TestContext.BuiltDotNet.Exec(sharedTestState.HostApiInvokerApp.AppDll, api, f.ExeDir)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.ReturnStatusCode(api, Constants.ErrorCode.Success)
                .And.HaveStdOutContaining($"{api} sdks:[{expectedList}]");
        }

        [Fact]
        public void Hostfxr_resolve_sdk2_without_global_json_or_flags()
        {
            // with no global.json and no flags, pick latest SDK

            var f = sharedTestState.SdkAndFrameworkFixture;
            string expectedData = string.Join(';', new[]
            {
                ("resolved_sdk_dir", Path.Combine(f.LocalSdkDir, "5.6.7-preview")),
            });

            string api = ApiNames.hostfxr_resolve_sdk2;
            TestContext.BuiltDotNet.Exec(sharedTestState.HostApiInvokerApp.AppDll, api, f.ExeDir, f.EmptyGlobalJsonDir, "0")
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.ReturnStatusCode(api, Constants.ErrorCode.Success)
                .And.HaveStdOutContaining($"{api} data:[{expectedData}]");
        }

        [Fact]
        public void Hostfxr_resolve_sdk2_without_global_json_and_disallowing_previews()
        {
            // Without global.json and disallowing previews, pick latest non-preview

            var f = sharedTestState.SdkAndFrameworkFixture;
            string expectedData = string.Join(';', new[]
            {
                ("resolved_sdk_dir", Path.Combine(f.LocalSdkDir, "1.2.3"))
            });

            string api = ApiNames.hostfxr_resolve_sdk2;
            TestContext.BuiltDotNet.Exec(sharedTestState.HostApiInvokerApp.AppDll, api, f.ExeDir, f.EmptyGlobalJsonDir, "disallow_prerelease")
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.ReturnStatusCode(api, Constants.ErrorCode.Success)
                .And.HaveStdOutContaining($"{api} data:[{expectedData}]");
        }

        [Fact]
        public void Hostfxr_resolve_sdk2_with_global_json_and_disallowing_previews()
        {
            // With global.json specifying a preview, roll forward to preview
            // since flag has no impact if global.json specifies a preview.
            // Also check that global.json that impacted resolution is reported.

            var f = sharedTestState.SdkAndFrameworkFixture;
            using (TestArtifact workingDir = TestArtifact.Create(nameof(workingDir)))
            {
                string requestedVersion = "5.6.6-preview";
                string globalJson = GlobalJson.CreateWithVersion(workingDir.Location, requestedVersion);
                string expectedData = string.Join(';', new[]
                {
                    ("resolved_sdk_dir", Path.Combine(f.LocalSdkDir, "5.6.7-preview")),
                    ("global_json_path", globalJson),
                    ("requested_version", requestedVersion),
                });

                string api = ApiNames.hostfxr_resolve_sdk2;
                TestContext.BuiltDotNet.Exec(sharedTestState.HostApiInvokerApp.AppDll, api, f.ExeDir, workingDir.Location, "disallow_prerelease")
                    .EnableTracingAndCaptureOutputs()
                    .Execute()
                    .Should().Pass()
                    .And.ReturnStatusCode(api, Constants.ErrorCode.Success)
                    .And.HaveStdOutContaining($"{api} data:[{expectedData}]");
            }
        }

        [Fact]
        public void Hostfxr_corehost_set_error_writer_test()
        {
            TestContext.BuiltDotNet.Exec(sharedTestState.HostApiInvokerApp.AppDll, "Test_hostfxr_set_error_writer")
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass();
        }

        [Fact]
        public void Hostfxr_get_dotnet_environment_info_dotnet_root_only()
        {
            var f = sharedTestState.SdkAndFrameworkFixture;
            string expectedSdkVersions = string.Join(";", new[]
            {
                "0.1.2",
                "1.2.3",
                "5.6.7-preview"
            });

            string expectedSdkPaths = string.Join(';', new[]
            {
                 Path.Combine(f.LocalSdkDir, "0.1.2"),
                 Path.Combine(f.LocalSdkDir, "1.2.3"),
                 Path.Combine(f.LocalSdkDir, "5.6.7-preview"),
            });

            string expectedFrameworkNames = string.Join(';', new[]
            {
                "HostFxr.Test.B",
                "HostFxr.Test.B",
                "HostFxr.Test.C"
            });

            string expectedFrameworkVersions = string.Join(';', new[]
            {
                "4.0.0",
                "5.6.7-A",
                "3.0.0"
            });

            string expectedFrameworkPaths = string.Join(';', new[]
            {
                Path.Combine(f.LocalFrameworksDir, "HostFxr.Test.B"),
                Path.Combine(f.LocalFrameworksDir, "HostFxr.Test.B"),
                Path.Combine(f.LocalFrameworksDir, "HostFxr.Test.C")
            });

            string api = ApiNames.hostfxr_get_dotnet_environment_info;
            TestContext.BuiltDotNet.Exec(sharedTestState.HostApiInvokerApp.AppDll, api, f.ExeDir)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.ReturnStatusCode(api, Constants.ErrorCode.Success)
                .And.HaveStdOutContaining($"{api} sdk versions:[{expectedSdkVersions}]")
                .And.HaveStdOutContaining($"{api} sdk paths:[{expectedSdkPaths}]")
                .And.HaveStdOutContaining($"{api} framework names:[{expectedFrameworkNames}]")
                .And.HaveStdOutContaining($"{api} framework versions:[{expectedFrameworkVersions}]")
                .And.HaveStdOutContaining($"{api} framework paths:[{expectedFrameworkPaths}]");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // The test setup only works on Windows (and MLL was Windows-only anyway)
        public void Hostfxr_get_dotnet_environment_info_with_multilevel_lookup_with_dotnet_root()
        {
            var f = sharedTestState.SdkAndFrameworkFixture;
            string expectedSdkVersions = string.Join(';', new[]
            {
                "0.1.2",
                "1.2.3",
                "5.6.7-preview",
            });

            string expectedSdkPaths = string.Join(';', new[]
            {
                Path.Combine(f.LocalSdkDir, "0.1.2"),
                Path.Combine(f.LocalSdkDir, "1.2.3"),
                Path.Combine(f.LocalSdkDir, "5.6.7-preview"),
            });

            string expectedFrameworkNames = string.Join(';', new[]
            {
                "HostFxr.Test.B",
                "HostFxr.Test.B",
                "HostFxr.Test.C"
            });

            string expectedFrameworkVersions = string.Join(';', new[]
            {
                "4.0.0",
                "5.6.7-A",
                "3.0.0"
            });

            string expectedFrameworkPaths = string.Join(';', new[]
            {
                Path.Combine(f.LocalFrameworksDir, "HostFxr.Test.B"),
                Path.Combine(f.LocalFrameworksDir, "HostFxr.Test.B"),
                Path.Combine(f.LocalFrameworksDir, "HostFxr.Test.C")
            });

            string api = ApiNames.hostfxr_get_dotnet_environment_info;
            sharedTestState.TestBehaviorEnabledDotNet.Exec(sharedTestState.HostApiInvokerApp.AppDll, new[] { api, f.ExeDir })
                .EnvironmentVariable("TEST_MULTILEVEL_LOOKUP_PROGRAM_FILES", f.ProgramFiles)
                .EnvironmentVariable("TEST_MULTILEVEL_LOOKUP_SELF_REGISTERED", f.SelfRegistered)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.ReturnStatusCode(api, Constants.ErrorCode.Success)
                .And.HaveStdOutContaining($"{api} sdk versions:[{expectedSdkVersions}]")
                .And.HaveStdOutContaining($"{api} sdk paths:[{expectedSdkPaths}]")
                .And.HaveStdOutContaining($"{api} framework names:[{expectedFrameworkNames}]")
                .And.HaveStdOutContaining($"{api} framework versions:[{expectedFrameworkVersions}]")
                .And.HaveStdOutContaining($"{api} framework paths:[{expectedFrameworkPaths}]");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // The test setup only works on Windows (and MLL was Windows-only anyway)
        public void Hostfxr_get_dotnet_environment_info_with_multilevel_lookup_only()
        {
            var f = sharedTestState.SdkAndFrameworkFixture;

            // Multi-level lookup is completely disabled on 7+
            // The test runs the API with the dotnet root directory set to a location which doesn't have any SDKs or frameworks
            string api = ApiNames.hostfxr_get_dotnet_environment_info;
            sharedTestState.TestBehaviorEnabledDotNet.Exec(sharedTestState.HostApiInvokerApp.AppDll, api, sharedTestState.HostApiInvokerApp.Location)
                .EnvironmentVariable("TEST_MULTILEVEL_LOOKUP_PROGRAM_FILES", f.ProgramFiles)
                .EnvironmentVariable("TEST_MULTILEVEL_LOOKUP_SELF_REGISTERED", f.SelfRegistered)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.ReturnStatusCode(api, Constants.ErrorCode.Success)
                .And.HaveStdOutContaining($"{api} sdk versions:[]")
                .And.HaveStdOutContaining($"{api} sdk paths:[]")
                .And.HaveStdOutContaining($"{api} framework names:[]")
                .And.HaveStdOutContaining($"{api} framework versions:[]")
                .And.HaveStdOutContaining($"{api} framework paths:[]");
        }

        [Fact]
        public void Hostfxr_get_dotnet_environment_info_global_install_path()
        {
            string api = ApiNames.hostfxr_get_dotnet_environment_info;
            TestContext.BuiltDotNet.Exec(sharedTestState.HostApiInvokerApp.AppDll, api)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.ReturnStatusCode(api, Constants.ErrorCode.Success);
        }

        [Fact]
        public void Hostfxr_get_dotnet_environment_info_result_is_nullptr_fails()
        {
            string api = ApiNames.hostfxr_get_dotnet_environment_info;
            TestContext.BuiltDotNet.Exec(sharedTestState.HostApiInvokerApp.AppDll, api, "test_invalid_result_ptr")
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.ReturnStatusCode(api, Constants.ErrorCode.InvalidArgFailure)
                .And.HaveStdErrContaining($"{api} received an invalid argument: result should not be null.");
        }

        [Fact]
        public void Hostfxr_get_dotnet_environment_info_reserved_is_not_nullptr_fails()
        {
            string api = ApiNames.hostfxr_get_dotnet_environment_info;
            TestContext.BuiltDotNet.Exec(sharedTestState.HostApiInvokerApp.AppDll, api, "test_invalid_reserved_ptr")
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.ReturnStatusCode(api, Constants.ErrorCode.InvalidArgFailure)
                .And.HaveStdErrContaining($"{api} received an invalid argument: reserved should be null.");
        }

        [Fact]
        public void Hostpolicy_corehost_set_error_writer_test()
        {
            TestContext.BuiltDotNet.Exec(sharedTestState.HostApiInvokerApp.AppDll, "Test_corehost_set_error_writer")
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass();
        }

        [Fact]
        public void HostRuntimeContract_get_runtime_property()
        {
            TestApp app = sharedTestState.HostApiInvokerApp;
            TestContext.BuiltDotNet.Exec(app.AppDll, "host_runtime_contract.get_runtime_property", "APP_CONTEXT_BASE_DIRECTORY", "RUNTIME_IDENTIFIER", "DOES_NOT_EXIST", "ENTRY_ASSEMBLY_NAME")
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining($"APP_CONTEXT_BASE_DIRECTORY = {Path.GetDirectoryName(app.AppDll)}")
                .And.HaveStdOutContaining($"RUNTIME_IDENTIFIER = {TestContext.BuildRID}")
                .And.HaveStdOutContaining($"DOES_NOT_EXIST = <none>")
                .And.HaveStdOutContaining($"ENTRY_ASSEMBLY_NAME = {app.AssemblyName}");
        }

        [Fact]
        public void HostRuntimeContract_bundle_probe()
        {
            TestContext.BuiltDotNet.Exec(sharedTestState.HostApiInvokerApp.AppDll, "host_runtime_contract.bundle_probe", "APP_CONTEXT_BASE_DIRECTORY", "RUNTIME_IDENTIFIER", "DOES_NOT_EXIST", "ENTRY_ASSEMBLY_NAME")
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("host_runtime_contract.bundle_probe is not set");
        }

        public class SharedTestState : IDisposable
        {
            public TestApp HostApiInvokerApp { get; }

            public DotNetCli TestBehaviorEnabledDotNet { get; }
            private readonly TestArtifact copiedDotnet;

            internal SdkAndFrameworkFixture SdkAndFrameworkFixture { get; }

            public SharedTestState()
            {
                HostApiInvokerApp = TestApp.CreateFromBuiltAssets("HostApiInvokerApp");

                if (!OperatingSystem.IsWindows())
                {
                    // On non-Windows, we can't just P/Invoke to already loaded hostfxr, so copy it next to the app dll.
                    File.Copy(Binaries.HostFxr.FilePath, Path.Combine(HostApiInvokerApp.Location, Binaries.HostFxr.FileName));
                }

                // Make a copy of the built .NET, as we will enable test-only behaviour
                copiedDotnet = TestArtifact.CreateFromCopy(nameof(NativeHostApis), TestContext.BuiltDotNet.BinPath);
                TestBehaviorEnabledDotNet = new DotNetCli(copiedDotnet.Location);

                // Enable test-only behavior for the copied .NET. We don't bother disabling the behaviour later,
                // as we just delete the entire copy after the tests run.
                _ = TestOnlyProductBehavior.Enable(TestBehaviorEnabledDotNet.GreatestVersionHostFxrFilePath);

                SdkAndFrameworkFixture = new SdkAndFrameworkFixture();
            }

            public void Dispose()
            {
                HostApiInvokerApp?.Dispose();
                copiedDotnet.Dispose();
                SdkAndFrameworkFixture.Dispose();
            }
        }
    }

    public static class HostApisCommandResultExtensions
    {
        public static AndConstraint<CommandResultAssertions> ReturnStatusCode(this CommandResultAssertions assertion, string apiName, int statusCode)
        {
            return statusCode == Constants.ErrorCode.Success
                ? assertion.HaveStdOutContaining($"{apiName}:Success")
                : assertion.HaveStdOutContaining($"{apiName}:Fail[0x{statusCode:x}]");
        }
    }
}
