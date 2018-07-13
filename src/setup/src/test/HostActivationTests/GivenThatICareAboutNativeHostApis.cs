// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using FluentAssertions;
using Microsoft.DotNet.CoreSetup.Test;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHostApis
{
    public class GivenThatICareAboutNativeHostApis : IClassFixture<GivenThatICareAboutNativeHostApis.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public GivenThatICareAboutNativeHostApis(GivenThatICareAboutNativeHostApis.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Muxer_activation_of_Publish_Output_Portable_DLL_hostfxr_get_native_search_directories_Succeeds()
        {
            // Currently the native API is used only on Windows, although it has been manually tested on Unix.
            // Limit OS here to avoid issues with DllImport not being able to find the shared library.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

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
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("hostfxr_get_native_search_directories:Success")
                .And
                .HaveStdOutContaining("hostfxr_get_native_search_directories buffer:[" + dotnet.GreatestVersionSharedFxPath);
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
