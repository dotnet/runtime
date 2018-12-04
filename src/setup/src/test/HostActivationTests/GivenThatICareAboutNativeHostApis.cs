// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Cli.Build;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHostApis
{
    public class GivenThatICareAboutNativeHostApis : IClassFixture<GivenThatICareAboutNativeHostApis.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public GivenThatICareAboutNativeHostApis(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Muxer_activation_of_Publish_Output_Portable_DLL_hostfxr_get_native_search_directories_Succeeds()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var dotnetLocation = Path.Combine(dotnet.BinPath, $"dotnet{fixture.ExeExtension}");
            string[] args =
            {
                "hostfxr_get_native_search_directories",
                dotnetLocation,
                appDll
            };

            dotnet.Exec(appDll, args)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("hostfxr_get_native_search_directories:Success")
                .And
                .HaveStdOutContaining("hostfxr_get_native_search_directories buffer:[" + dotnet.GreatestVersionSharedFxPath);
        }

        // This test also validates that hostfxr_set_error_writer captures errors
        // from hostfxr itself.
        [Fact]
        public void Hostfxr_get_native_search_directories_invalid_buffer()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();

            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, "Test_hostfxr_get_native_search_directories_invalid_buffer")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And.HaveStdOutContaining($"hostfxr reported errors:")
                .And.HaveStdOutContaining("hostfxr_get_native_search_directories received an invalid argument.");
        }

        // This test also validates that hostfxr_set_error_writer propagates the custom writer
        // to the hostpolicy.dll for the duration of those calls.
        [Fact]
        public void Muxer_hostfxr_get_native_search_directories_with_invalid_deps()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();
            var appFixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;

            // Corrupt the .deps.json by appending } to it (malformed json)
            File.WriteAllText(
                appFixture.TestProject.DepsJson,
                File.ReadAllLines(appFixture.TestProject.DepsJson) + "}");

            var dotnetLocation = Path.Combine(dotnet.BinPath, $"dotnet{fixture.ExeExtension}");
            string[] args =
            {
                "hostfxr_get_native_search_directories",
                dotnetLocation,
                appFixture.TestProject.AppDll
            };

            dotnet.Exec(fixture.TestProject.AppDll, args)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And.HaveStdOutContaining("hostfxr_get_native_search_directories:Fail[2147516555]") // StatusCode::ResolverInitFailure
                .And.HaveStdOutContaining("hostfxr reported errors:")
                .And.HaveStdOutContaining($"A JSON parsing exception occurred in [{appFixture.TestProject.DepsJson}]: * Line 1, Column 2 Syntax error: Malformed token")
                .And.HaveStdOutContaining($"Error initializing the dependency resolver: An error occurred while parsing: {appFixture.TestProject.DepsJson}");
        }

        [Fact]
        public void Breadcrumb_thread_finishes_when_app_closes_normally()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll)
                .EnvironmentVariable("CORE_BREADCRUMBS", sharedTestState.BreadcrumbLocation)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World")
                .And
                .HaveStdErrContaining("Waiting for breadcrumb thread to exit...");
        }

        [Fact]
        public void Breadcrumb_thread_does_not_finish_when_app_has_unhandled_exception()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppWithExceptionProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll)
                .EnvironmentVariable("CORE_BREADCRUMBS", sharedTestState.BreadcrumbLocation)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("Unhandled Exception: System.Exception: Goodbye World")
                .And
                // The breadcrumb thread does not wait since destructors are not called when an exception is thrown.
                // However, destructors will be called when the caller (such as a custom host) is compiled with SEH Exceptions (/EHa) and has a try\catch.
                // Todo: add a native host test app so we can verify this behavior.
                .NotHaveStdErrContaining("Waiting for breadcrumb thread to exit...");
        }

        private class SdkResolutionFixture
        {
            private readonly TestProjectFixture _fixture;

            public DotNetCli Dotnet => _fixture.BuiltDotnet;
            public string AppDll => _fixture.TestProject.AppDll;
            public string ExeDir => Path.Combine(_fixture.TestProject.ProjectDirectory, "ed");
            public string ProgramFiles => Path.Combine(ExeDir, "pf");
            public string WorkingDir => Path.Combine(_fixture.TestProject.ProjectDirectory, "wd");
            public string GlobalSdkDir => Path.Combine(ProgramFiles, "dotnet", "sdk");
            public string LocalSdkDir => Path.Combine(ExeDir, "sdk");
            public string GlobalJson => Path.Combine(WorkingDir, "global.json");
            public string[] GlobalSdks = new[] { "4.5.6", "1.2.3", "2.3.4-preview" };
            public string[] LocalSdks = new[] { "0.1.2", "5.6.7-preview", "1.2.3" };

            public SdkResolutionFixture(SharedTestState state)
            {
                _fixture = state.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();

                Directory.CreateDirectory(WorkingDir);

                // start with an empty global.json, it will be ignored, but prevent one lying on disk 
                // on a given machine from impacting the test.
                File.WriteAllText(GlobalJson, "{}");

                foreach (string sdk in GlobalSdks)
                {
                    Directory.CreateDirectory(Path.Combine(GlobalSdkDir, sdk));
                }

                foreach (string sdk in LocalSdks)
                {
                    Directory.CreateDirectory(Path.Combine(LocalSdkDir, sdk));
                }
            } 
        }

        [Fact]
        public void Hostfxr_get_available_sdks_with_multilevel_lookup()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
            {
                // multilevel lookup is not supported on non-Windows
                return;
            }
            
            var f = new SdkResolutionFixture(sharedTestState);

            // With multi-level lookup (windows onnly): get local and global sdks sorted by ascending version,
            // with global sdk coming before local sdk when versions are equal
            string expectedList = string.Join(';', new[]
            {
                Path.Combine(f.LocalSdkDir, "0.1.2"),
                Path.Combine(f.GlobalSdkDir, "1.2.3"),
                Path.Combine(f.LocalSdkDir, "1.2.3"),
                Path.Combine(f.GlobalSdkDir, "2.3.4-preview"),
                Path.Combine(f.GlobalSdkDir, "4.5.6"),
                Path.Combine(f.LocalSdkDir, "5.6.7-preview"),
            });

            f.Dotnet.Exec(f.AppDll, new[] { "hostfxr_get_available_sdks", f.ExeDir })
                .EnvironmentVariable("TEST_MULTILEVEL_LOOKUP_PROGRAM_FILES", f.ProgramFiles)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("hostfxr_get_available_sdks:Success")
                .And
                .HaveStdOutContaining($"hostfxr_get_available_sdks sdks:[{expectedList}]");
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
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("hostfxr_get_available_sdks:Success")
                .And
                .HaveStdOutContaining($"hostfxr_get_available_sdks sdks:[{expectedList}]");
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
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("hostfxr_resolve_sdk2:Success")
                .And
                .HaveStdOutContaining($"hostfxr_resolve_sdk2 data:[{expectedData}]");
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
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("hostfxr_resolve_sdk2:Success")
                .And
                .HaveStdOutContaining($"hostfxr_resolve_sdk2 data:[{expectedData}]");
        }

        [Fact]
        public void Hostfxr_resolve_sdk2_with_global_json_and_disallowing_previews()
        {
            // With global.json specifying a preview, roll forward to preview 
            // since flag has no impact if global.json specifies a preview.
            // Also check that global.json that impacted resolution is reported.

            var f = new SdkResolutionFixture(sharedTestState);

            File.WriteAllText(f.GlobalJson, "{ \"sdk\": { \"version\": \"5.6.6-preview\" } }");
            string expectedData = string.Join(';', new[]
            {
                ("resolved_sdk_dir", Path.Combine(f.LocalSdkDir, "5.6.7-preview")),
                ("global_json_path", f.GlobalJson),
            });

            f.Dotnet.Exec(f.AppDll, new[] { "hostfxr_resolve_sdk2", f.ExeDir, f.WorkingDir, "disallow_prerelease" })
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("hostfxr_resolve_sdk2:Success")
                .And
                .HaveStdOutContaining($"hostfxr_resolve_sdk2 data:[{expectedData}]");
        }

        [Fact]
        public void Hostfxr_corehost_set_error_writer_test()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();

            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, "Test_hostfxr_set_error_writer")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass();
        }

        [Fact]
        public void Hostpolicy_corehost_set_error_writer_test()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Copy();

            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll, "Test_corehost_set_error_writer")
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass();
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture PreviouslyPublishedAndRestoredPortableApiTestProjectFixture { get; set; }
            public TestProjectFixture PreviouslyPublishedAndRestoredPortableAppProjectFixture { get; set; }
            public TestProjectFixture PreviouslyPublishedAndRestoredPortableAppWithExceptionProjectFixture { get; set; }
            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public string BreadcrumbLocation { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                PreviouslyPublishedAndRestoredPortableApiTestProjectFixture = new TestProjectFixture("HostApiInvokerApp", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .BuildProject();

                PreviouslyPublishedAndRestoredPortableAppProjectFixture = new TestProjectFixture("PortableApp", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();

                PreviouslyPublishedAndRestoredPortableAppWithExceptionProjectFixture = new TestProjectFixture("PortableAppWithException", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    BreadcrumbLocation = Path.Combine(
                        PreviouslyPublishedAndRestoredPortableAppWithExceptionProjectFixture.TestProject.OutputDirectory,
                        "opt",
                        "corebreadcrumbs");
                    Directory.CreateDirectory(BreadcrumbLocation);

                    // On non-Windows, we can't just P/Invoke to already loaded hostfxr, so copy it next to the app dll.
                    var fixture = PreviouslyPublishedAndRestoredPortableApiTestProjectFixture;
                    var hostfxr = Path.Combine(
                        fixture.BuiltDotnet.GreatestVersionHostFxrPath, 
                        $"{fixture.SharedLibraryPrefix}hostfxr{fixture.SharedLibraryExtension}");

                    File.Copy(
                        hostfxr, 
                        Path.GetDirectoryName(fixture.TestProject.AppDll));
                }
            }

            public void Dispose()
            {
                PreviouslyPublishedAndRestoredPortableApiTestProjectFixture.Dispose();
                PreviouslyPublishedAndRestoredPortableAppProjectFixture.Dispose();
                PreviouslyPublishedAndRestoredPortableAppWithExceptionProjectFixture.Dispose();
            }
        }
    }
}
