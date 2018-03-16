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
    public class GivenThatICareAboutNativeHostApis
    {
        private static TestProjectFixture PreviouslyBuiltAndRestoredPortableTestProjectFixture { get; set; }
        private static TestProjectFixture PreviouslyPublishedAndRestoredPortableTestProjectFixture { get; set; }
        private static RepoDirectoriesProvider RepoDirectories { get; set; }

        static GivenThatICareAboutNativeHostApis()
        {
            RepoDirectories = new RepoDirectoriesProvider();

            PreviouslyBuiltAndRestoredPortableTestProjectFixture = new TestProjectFixture("HostApiInvokerApp", RepoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages)
                .BuildProject();

            PreviouslyPublishedAndRestoredPortableTestProjectFixture = new TestProjectFixture("HostApiInvokerApp", RepoDirectories)
                .EnsureRestored(RepoDirectories.CorehostPackages)
                .PublishProject();
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

            var fixture = PreviouslyPublishedAndRestoredPortableTestProjectFixture.Copy();
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
    }
}
