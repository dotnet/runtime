// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

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

        private class SdkResolutionFixture
        {
            private readonly TestApp _app;

            public DotNetCli Dotnet { get; }
            public string AppDll => _app.AppDll;
            public string ExeDir => Path.Combine(_app.Location, "ed");
            public string ProgramFiles => Path.Combine(ExeDir, "pf");
            public string SelfRegistered => Path.Combine(ExeDir, "sr");
            public string WorkingDir => Path.Combine(_app.Location, "wd");
            public string ProgramFilesGlobalSdkDir => Path.Combine(ProgramFiles, "dotnet", "sdk");
            public string ProgramFilesGlobalFrameworksDir => Path.Combine(ProgramFiles, "dotnet", "shared");
            public string SelfRegisteredGlobalSdkDir => Path.Combine(SelfRegistered, "sdk");
            public string LocalSdkDir => Path.Combine(ExeDir, "sdk");
            public string LocalFrameworksDir => Path.Combine(ExeDir, "shared");
            public string[] ProgramFilesGlobalSdks = new[] { "4.5.6", "1.2.3", "2.3.4-preview" };
            public List<(string fwName, string[] fwVersions)> ProgramFilesGlobalFrameworks =
                new List<(string fwName, string[] fwVersions)>()
                {
                    ("HostFxr.Test.A", new[] { "1.2.3", "3.0.0" }),
                    ("HostFxr.Test.B", new[] { "5.6.7-A" })
                };
            public string[] SelfRegisteredGlobalSdks = new[] { "3.0.0", "15.1.4-preview", "5.6.7" };
            public string[] LocalSdks = new[] { "0.1.2", "5.6.7-preview", "1.2.3" };
            public List<(string fwName, string[] fwVersions)> LocalFrameworks =
                new List<(string fwName, string[] fwVersions)>()
                {
                    ("HostFxr.Test.B", new[] { "4.0.0", "5.6.7-A" }),
                    ("HostFxr.Test.C", new[] { "3.0.0" })
                };

            public SdkResolutionFixture(SharedTestState state)
            {
                Dotnet = TestContext.BuiltDotNet;

                _app = state.HostApiInvokerApp.Copy();

                Directory.CreateDirectory(WorkingDir);

                // start with an empty global.json, it will be ignored, but prevent one lying on disk
                // on a given machine from impacting the test.
                GlobalJson.CreateEmpty(WorkingDir);

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
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // The test setup only works on Windows (and MLL was Windows-only anyway)
        public void Hostfxr_get_available_sdks_with_multilevel_lookup()
        {
            var f = new SdkResolutionFixture(sharedTestState);

            // Starting with .NET 7, multi-level lookup is completely disabled for hostfxr API calls.
            // This test is still valuable to validate that it is in fact disabled
            string expectedList = string.Join(';', new[]
            {
                Path.Combine(f.LocalSdkDir, "0.1.2"),
                Path.Combine(f.LocalSdkDir, "1.2.3"),
                Path.Combine(f.LocalSdkDir, "5.6.7-preview"),
            });

            using (TestOnlyProductBehavior.Enable(f.Dotnet.GreatestVersionHostFxrFilePath))
            {
                f.Dotnet.Exec(f.AppDll, new[] { "hostfxr_get_available_sdks", f.ExeDir })
                    .EnvironmentVariable("TEST_MULTILEVEL_LOOKUP_PROGRAM_FILES", f.ProgramFiles)
                    .EnvironmentVariable("TEST_MULTILEVEL_LOOKUP_SELF_REGISTERED", f.SelfRegistered)
                    .CaptureStdOut()
                    .CaptureStdErr()
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining("hostfxr_get_available_sdks:Success")
                    .And.HaveStdOutContaining($"hostfxr_get_available_sdks sdks:[{expectedList}]");
            }
        }

        [Fact]
        public void Hostfxr_get_available_sdks_without_multilevel_lookup()
        {
            // Without multi-level lookup: get only sdks sorted by ascending version

            var f = new SdkResolutionFixture(sharedTestState);

            string expectedList = string.Join(';', new[]
            {
                 Path.Combine(f.LocalSdkDir, "0.1.2"),
                 Path.Combine(f.LocalSdkDir, "1.2.3"),
                 Path.Combine(f.LocalSdkDir, "5.6.7-preview"),
            });

            f.Dotnet.Exec(f.AppDll, new[] { "hostfxr_get_available_sdks", f.ExeDir })
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("hostfxr_get_available_sdks:Success")
                .And.HaveStdOutContaining($"hostfxr_get_available_sdks sdks:[{expectedList}]");
        }

        [Fact]
        public void Hostfxr_resolve_sdk2_without_global_json_or_flags()
        {
            // with no global.json and no flags, pick latest SDK

            var f = new SdkResolutionFixture(sharedTestState);

            string expectedData = string.Join(';', new[]
            {
                ("resolved_sdk_dir", Path.Combine(f.LocalSdkDir, "5.6.7-preview")),
            });

            f.Dotnet.Exec(f.AppDll, new[] { "hostfxr_resolve_sdk2", f.ExeDir, f.WorkingDir, "0" })
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("hostfxr_resolve_sdk2:Success")
                .And.HaveStdOutContaining($"hostfxr_resolve_sdk2 data:[{expectedData}]");
        }

        [Fact]
        public void Hostfxr_resolve_sdk2_without_global_json_and_disallowing_previews()
        {
            // Without global.json and disallowing previews, pick latest non-preview

            var f = new SdkResolutionFixture(sharedTestState);

            string expectedData = string.Join(';', new[]
            {
                ("resolved_sdk_dir", Path.Combine(f.LocalSdkDir, "1.2.3"))
            });

            f.Dotnet.Exec(f.AppDll, new[] { "hostfxr_resolve_sdk2", f.ExeDir, f.WorkingDir, "disallow_prerelease" })
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("hostfxr_resolve_sdk2:Success")
                .And.HaveStdOutContaining($"hostfxr_resolve_sdk2 data:[{expectedData}]");
        }

        [Fact]
        public void Hostfxr_resolve_sdk2_with_global_json_and_disallowing_previews()
        {
            // With global.json specifying a preview, roll forward to preview
            // since flag has no impact if global.json specifies a preview.
            // Also check that global.json that impacted resolution is reported.

            var f = new SdkResolutionFixture(sharedTestState);

            string requestedVersion = "5.6.6-preview";
            string globalJson = GlobalJson.CreateWithVersion(f.WorkingDir, requestedVersion);
            string expectedData = string.Join(';', new[]
            {
                ("resolved_sdk_dir", Path.Combine(f.LocalSdkDir, "5.6.7-preview")),
                ("global_json_path", globalJson),
                ("requested_version", requestedVersion),
            });

            f.Dotnet.Exec(f.AppDll, new[] { "hostfxr_resolve_sdk2", f.ExeDir, f.WorkingDir, "disallow_prerelease" })
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("hostfxr_resolve_sdk2:Success")
                .And.HaveStdOutContaining($"hostfxr_resolve_sdk2 data:[{expectedData}]");
        }

        [Fact]
        public void Hostfxr_corehost_set_error_writer_test()
        {
            TestContext.BuiltDotNet.Exec(sharedTestState.HostApiInvokerApp.AppDll, "Test_hostfxr_set_error_writer")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass();
        }

        [Fact]
        public void Hostfxr_get_dotnet_environment_info_dotnet_root_only()
        {
            var f = new SdkResolutionFixture(sharedTestState);
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

            f.Dotnet.Exec(f.AppDll, new[] { "hostfxr_get_dotnet_environment_info", f.ExeDir })
            .CaptureStdOut()
            .CaptureStdErr()
            .Execute()
            .Should().Pass()
            .And.HaveStdOutContaining("hostfxr_get_dotnet_environment_info:Success")
            .And.HaveStdOutContaining($"hostfxr_get_dotnet_environment_info sdk versions:[{expectedSdkVersions}]")
            .And.HaveStdOutContaining($"hostfxr_get_dotnet_environment_info sdk paths:[{expectedSdkPaths}]")
            .And.HaveStdOutContaining($"hostfxr_get_dotnet_environment_info framework names:[{expectedFrameworkNames}]")
            .And.HaveStdOutContaining($"hostfxr_get_dotnet_environment_info framework versions:[{expectedFrameworkVersions}]")
            .And.HaveStdOutContaining($"hostfxr_get_dotnet_environment_info framework paths:[{expectedFrameworkPaths}]");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // The test setup only works on Windows (and MLL was Windows-only anyway)
        public void Hostfxr_get_dotnet_environment_info_with_multilevel_lookup_with_dotnet_root()
        {
            var f = new SdkResolutionFixture(sharedTestState);
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

            using (TestOnlyProductBehavior.Enable(f.Dotnet.GreatestVersionHostFxrFilePath))
            {
                f.Dotnet.Exec(f.AppDll, new[] { "hostfxr_get_dotnet_environment_info", f.ExeDir })
                .EnvironmentVariable("TEST_MULTILEVEL_LOOKUP_PROGRAM_FILES", f.ProgramFiles)
                .EnvironmentVariable("TEST_MULTILEVEL_LOOKUP_SELF_REGISTERED", f.SelfRegistered)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("hostfxr_get_dotnet_environment_info:Success")
                .And.HaveStdOutContaining($"hostfxr_get_dotnet_environment_info sdk versions:[{expectedSdkVersions}]")
                .And.HaveStdOutContaining($"hostfxr_get_dotnet_environment_info sdk paths:[{expectedSdkPaths}]")
                .And.HaveStdOutContaining($"hostfxr_get_dotnet_environment_info framework names:[{expectedFrameworkNames}]")
                .And.HaveStdOutContaining($"hostfxr_get_dotnet_environment_info framework versions:[{expectedFrameworkVersions}]")
                .And.HaveStdOutContaining($"hostfxr_get_dotnet_environment_info framework paths:[{expectedFrameworkPaths}]");
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // The test setup only works on Windows (and MLL was Windows-only anyway)
        public void Hostfxr_get_dotnet_environment_info_with_multilevel_lookup_only()
        {
            var f = new SdkResolutionFixture(sharedTestState);

            // Multi-level lookup is completely disabled on 7+
            // The test runs the API with the dotnet root directory set to a location which doesn't have any SDKs or frameworks
            using (TestOnlyProductBehavior.Enable(f.Dotnet.GreatestVersionHostFxrFilePath))
            {
                // We pass f.WorkingDir so that we don't resolve dotnet_dir to the global installation
                // in the native side.
                f.Dotnet.Exec(f.AppDll, new[] { "hostfxr_get_dotnet_environment_info", f.WorkingDir })
                .EnvironmentVariable("TEST_MULTILEVEL_LOOKUP_PROGRAM_FILES", f.ProgramFiles)
                .EnvironmentVariable("TEST_MULTILEVEL_LOOKUP_SELF_REGISTERED", f.SelfRegistered)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("hostfxr_get_dotnet_environment_info:Success")
                .And.HaveStdOutContaining($"hostfxr_get_dotnet_environment_info sdk versions:[]")
                .And.HaveStdOutContaining($"hostfxr_get_dotnet_environment_info sdk paths:[]")
                .And.HaveStdOutContaining($"hostfxr_get_dotnet_environment_info framework names:[]")
                .And.HaveStdOutContaining($"hostfxr_get_dotnet_environment_info framework versions:[]")
                .And.HaveStdOutContaining($"hostfxr_get_dotnet_environment_info framework paths:[]");
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // The test setup only works on Windows (and MLL was Windows-only anyway)
        public void Hostfxr_get_dotnet_environment_info_with_multilevel_lookup_only_self_register_program_files()
        {
            var f = new SdkResolutionFixture(sharedTestState);

            using (TestOnlyProductBehavior.Enable(f.Dotnet.GreatestVersionHostFxrFilePath))
            {
                // We pass f.WorkingDir so that we don't resolve dotnet_dir to the global installation
                // in the native side.
                f.Dotnet.Exec(f.AppDll, new[] { "hostfxr_get_dotnet_environment_info", f.WorkingDir })
                .EnvironmentVariable("TEST_MULTILEVEL_LOOKUP_PROGRAM_FILES", f.ProgramFiles)
                // Test with a self-registered path the same as ProgramFiles, with a trailing slash.  Expect this to be de-duped
                .EnvironmentVariable("TEST_MULTILEVEL_LOOKUP_SELF_REGISTERED", Path.Combine(f.ProgramFiles, "dotnet") + Path.DirectorySeparatorChar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("hostfxr_get_dotnet_environment_info:Success")
                .And.HaveStdOutContaining($"hostfxr_get_dotnet_environment_info framework names:[]")
                .And.HaveStdOutContaining($"hostfxr_get_dotnet_environment_info framework versions:[]")
                .And.HaveStdOutContaining($"hostfxr_get_dotnet_environment_info framework paths:[]");
            }
        }

        [Fact]
        public void Hostfxr_get_dotnet_environment_info_global_install_path()
        {
            var f = new SdkResolutionFixture(sharedTestState);

            f.Dotnet.Exec(f.AppDll, new[] { "hostfxr_get_dotnet_environment_info" })
            .CaptureStdOut()
            .CaptureStdErr()
            .Execute()
            .Should().Pass()
            .And.HaveStdOutContaining("hostfxr_get_dotnet_environment_info:Success");
        }

        [Fact]
        public void Hostfxr_get_dotnet_environment_info_result_is_nullptr_fails()
        {
            var f = new SdkResolutionFixture(sharedTestState);

            f.Dotnet.Exec(f.AppDll, new[] { "hostfxr_get_dotnet_environment_info", "test_invalid_result_ptr" })
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                // 0x80008081 (InvalidArgFailure)
                .And.HaveStdOutContaining("hostfxr_get_dotnet_environment_info:Fail[-2147450751]")
                .And.HaveStdErrContaining("hostfxr_get_dotnet_environment_info received an invalid argument: result should not be null.");
        }

        [Fact]
        public void Hostfxr_get_dotnet_environment_info_reserved_is_not_nullptr_fails()
        {
            var f = new SdkResolutionFixture(sharedTestState);

            f.Dotnet.Exec(f.AppDll, new[] { "hostfxr_get_dotnet_environment_info", "test_invalid_reserved_ptr" })
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                // 0x80008081 (InvalidArgFailure)
                .And.HaveStdOutContaining("hostfxr_get_dotnet_environment_info:Fail[-2147450751]")
                .And.HaveStdErrContaining("hostfxr_get_dotnet_environment_info received an invalid argument: reserved should be null.");
        }

        [Fact]
        public void Hostpolicy_corehost_set_error_writer_test()
        {
            TestContext.BuiltDotNet.Exec(sharedTestState.HostApiInvokerApp.AppDll, "Test_corehost_set_error_writer")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass();
        }

        [Fact]
        public void HostRuntimeContract_get_runtime_property()
        {
            TestApp app = sharedTestState.HostApiInvokerApp;
            TestContext.BuiltDotNet.Exec(app.AppDll, "host_runtime_contract.get_runtime_property", "APP_CONTEXT_BASE_DIRECTORY", "RUNTIME_IDENTIFIER", "DOES_NOT_EXIST", "ENTRY_ASSEMBLY_NAME")
                .CaptureStdOut()
                .CaptureStdErr()
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
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("host_runtime_contract.bundle_probe is not set");
        }

        public class SharedTestState : IDisposable
        {
            public TestApp HostApiInvokerApp { get; }

            public SharedTestState()
            {
                HostApiInvokerApp = TestApp.CreateFromBuiltAssets("HostApiInvokerApp");

                if (!OperatingSystem.IsWindows())
                {
                    // On non-Windows, we can't just P/Invoke to already loaded hostfxr, so copy it next to the app dll.
                    File.Copy(Binaries.HostFxr.FilePath, Path.Combine(HostApiInvokerApp.Location, Binaries.HostFxr.FileName));
                }
            }

            public void Dispose()
            {
                HostApiInvokerApp?.Dispose();
            }
        }
    }
}
