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
    public class BundledAppWithSubDirs : IClassFixture<BundledAppWithSubDirs.SharedTestState>
    {
        private readonly SharedTestState sharedState;

        public BundledAppWithSubDirs(SharedTestState fixture)
        {
            sharedState = fixture;
        }

        private void RunTheApp(string path, bool selfContained)
        {
            var cmd = Command.Create(path)
                .EnableTracingAndCaptureOutputs();
            if (!selfContained)
            {
                cmd = cmd.DotNetRoot(RepoDirectoriesProvider.Default.BuiltDotnet);
            }

            cmd.Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Wow! We now say hello to the big world and you.");
        }

        [InlineData(BundleOptions.None)]
        [InlineData(BundleOptions.BundleNativeBinaries)]
        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_Framework_dependent_App_Run_Succeeds(BundleOptions options)
        {
            var app = sharedState.FrameworkDependentApp;

            var singleFile = app.Bundle(options);

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
            var app = sharedState.SelfContainedApp;
            var singleFile = app.Bundle(options);

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
            var app = sharedState.SelfContainedApp;
            var singleFile = app.Bundle(options, new Version(5, 0));

            // Run the bundled app (extract files)
            RunTheApp(singleFile, selfContained: true);

            // Run the bundled app again (reuse extracted files)
            RunTheApp(singleFile, selfContained: true);
        }

        [InlineData(BundleOptions.BundleAllContent)]
        [Theory]
        public void Bundled_Framework_dependent_Targeting50_App_Run_Succeeds(BundleOptions options)
        {
            var app = sharedState.FrameworkDependentApp;
            var singleFile = app.Bundle(options, new Version(5, 0));

            // Run the bundled app (extract files)
            RunTheApp(singleFile, selfContained: false);

            // Run the bundled appuse extracted files)
            RunTheApp(singleFile, selfContained: false);
        }

        [Fact]
        public void Bundled_Self_Contained_Targeting50_WithCompression_Throws()
        {
            // compression must be off when targeting 5.0
            var options = BundleOptions.EnableCompression;
            Assert.Throws<ArgumentException>(() => sharedState.FrameworkDependentApp.Bundle(options, new Version(5, 0)));
            Assert.Throws<ArgumentException>(() => sharedState.SelfContainedApp.Bundle(options, new Version(5, 0)));
        }

        public class SharedTestState : IDisposable
        {
            internal AppWithSubDirs FrameworkDependentApp { get; }
            internal AppWithSubDirs SelfContainedApp { get; }

            public SharedTestState()
            {
                FrameworkDependentApp = AppWithSubDirs.CreateFrameworkDependent();
                SelfContainedApp = AppWithSubDirs.CreateSelfContained();
            }

            public void Dispose()
            {
                FrameworkDependentApp.Dispose();
                SelfContainedApp.Dispose();
            }
        }
    }
}
