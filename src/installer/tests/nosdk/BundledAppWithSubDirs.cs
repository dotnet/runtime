// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using BundleTests;
using BundleTests.Helpers;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.Bundle;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class BundledAppWithSubDirs
    {
        private void RunTheApp(string path, bool selfContained)
        {
            var cmd = Command.Create(path)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut();
            if (!selfContained)
            {
                cmd = cmd
                    .EnvironmentVariable("DOTNET_ROOT", RepoDirectoriesProvider.Default.BuiltDotnet)
                    .EnvironmentVariable("DOTNET_ROOT(x86)", RepoDirectoriesProvider.Default.BuiltDotnet)
                    .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0");
            }
            cmd.Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Wow! We now say hello to the big world and you.");
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_Framework_dependent_App_Run_Succeeds(BundleOptions options)
        {
            using var app = new AppWithSubDirs();

            var singleFile = app.BundleFxDependent(options);

            // Run the bundled app (extract files)
            RunTheApp(singleFile, selfContained: false);

            // Run the bundled app again (reuse extracted files)
            RunTheApp(singleFile, selfContained: false);
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_Self_Contained_NoCompression_App_Run_Succeeds(BundleOptions options)
        {
            using var app = new AppWithSubDirs();
            var singleFile = app.BundleSelfContained(options);

            // Run the bundled app (extract files)
            RunTheApp(singleFile, selfContained: true);

            // Run the bundled app again (reuse extracted files)
            RunTheApp(singleFile, selfContained: true);
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_Self_Contained_Targeting50_App_Run_Succeeds(BundleOptions options)
        {
            using var app = new AppWithSubDirs();
            var singleFile = app.BundleSelfContained(options, new Version(5, 0));

            // Run the bundled app (extract files)
            RunTheApp(singleFile, selfContained: true);

            // Run the bundled app again (reuse extracted files)
            RunTheApp(singleFile, selfContained: true);
        }

        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_Framework_dependent_Targeting50_App_Run_Succeeds(BundleOptions options)
        {
            using var app = new AppWithSubDirs();
            var singleFile = app.BundleFxDependent(options, new Version(5, 0));

            // Run the bundled app (extract files)
            RunTheApp(singleFile, selfContained: false);

            // Run the bundled appuse extracted files)
            RunTheApp(singleFile, selfContained: false);
        }

        [Fact]
        public void Bundled_Self_Contained_Targeting50_WithCompression_Throws()
        {
            using var app = new AppWithSubDirs();
            // compression must be off when targeting 5.0
            var options = BundleOptions.EnableCompression;
            Assert.Throws<ArgumentException>(() => app.BundleFxDependent(options, new Version(5, 0)));
            Assert.Throws<ArgumentException>(() => app.BundleSelfContained(options, new Version(5, 0)));
        }
    }
}
