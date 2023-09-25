// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using BundleTests.Helpers;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.NET.HostModel.Tests
{
    public class BundleAndRun : IClassFixture<BundleAndRun.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public BundleAndRun(BundleAndRun.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        private void RunTheApp(string path, bool selfContained)
        {
            Command.Create(path)
                .CaptureStdErr()
                .CaptureStdOut()
                .DotNetRoot(selfContained ? null : RepoDirectoriesProvider.Default.BuiltDotnet)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World!");
        }

        private string MakeUniversalBinary(string path, Architecture architecture)
        {
            string fatApp = path + ".fat";
            string arch = architecture == Architecture.Arm64 ? "arm64" : "x86_64";

            // We will create a universal binary with just one arch slice and run it.
            // It is enough for testing purposes. The code that finds the releavant slice
            // would work the same regardless if there is 1, 2, 3 or more slices.
            Command.Create("lipo", $"-create -arch {arch} {path} -output {fatApp}")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass();

            return fatApp;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        private void RunApp(bool selfContained)
        {
            // Bundle to a single-file
            string singleFile = selfContained
                ? sharedTestState.SelfContainedApp.Bundle()
                : sharedTestState.FrameworkDependentApp.Bundle();

            // Run the bundled app
            RunTheApp(singleFile, selfContained);

            if (OperatingSystem.IsMacOS())
            {
                string fatApp = MakeUniversalBinary(singleFile, RuntimeInformation.OSArchitecture);

                // Run the fat app
                RunTheApp(fatApp, selfContained);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AdditionalContentAfterBundleMetadata(bool selfContained)
        {
            string singleFile = selfContained
                ? sharedTestState.SelfContainedApp.Bundle()
                : sharedTestState.FrameworkDependentApp.Bundle();

            using (var file = File.OpenWrite(singleFile))
            {
                file.Position = file.Length;
                var blob = Encoding.UTF8.GetBytes("Mock signature at the end of the bundle");
                file.Write(blob, 0, blob.Length);
            }

            RunTheApp(singleFile, selfContained);
        }

        public class SharedTestState : IDisposable
        {
            public SingleFileTestApp FrameworkDependentApp { get; }
            public SingleFileTestApp SelfContainedApp { get; }

            public SharedTestState()
            {
                FrameworkDependentApp = SingleFileTestApp.CreateFrameworkDependent("HelloWorld");
                SelfContainedApp = SingleFileTestApp.CreateSelfContained("HelloWorld");
            }

            public void Dispose()
            {
                FrameworkDependentApp.Dispose();
                SelfContainedApp.Dispose();
            }
        }
    }
}
