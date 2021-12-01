// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.IO;
using System.Linq;
using BundleTests.Helpers;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.Bundle;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class NetCoreApp3CompatModeTests : BundleTestBase, IClassFixture<SingleFileSharedState>
    {
        private SingleFileSharedState sharedTestState;

        public NetCoreApp3CompatModeTests(SingleFileSharedState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Bundle_Is_Extracted()
        {
            var fixture = sharedTestState.TestFixture.Copy();
            BundleOptions options = BundleOptions.BundleAllContent;
            Bundler bundler = BundleSelfContainedApp(fixture, out string singleFile, options);
            var extractionBaseDir = BundleHelper.GetExtractionRootDir(fixture);

            Command.Create(singleFile, "executing_assembly_location trusted_platform_assemblies assembly_location System.Console")
                .CaptureStdOut()
                .CaptureStdErr()
                .EnvironmentVariable(BundleHelper.DotnetBundleExtractBaseEnvVariable, extractionBaseDir.FullName)
                .Execute()
                .Should()
                .Pass()
                // Validate that the main assembly is running from disk (and not from bundle)
                .And.HaveStdOutContaining("ExecutingAssembly.Location: " + extractionBaseDir.FullName)
                // Validate that TPA contains at least one framework assembly from the extraction directory
                .And.HaveStdOutContaining("System.Runtime.dll")
                // Validate that framework assembly is actually loaded from the extraction directory
                .And.HaveStdOutContaining("System.Console location: " + extractionBaseDir.FullName);

            var extractionDir = BundleHelper.GetExtractionDir(fixture, bundler);
            var bundleFiles = BundleHelper.GetBundleDir(fixture).GetFiles().Select(file => file.Name).ToArray();
            var publishedFiles = Directory.GetFiles(BundleHelper.GetPublishPath(fixture), searchPattern: "*", searchOption: SearchOption.AllDirectories)
                .Select(file => Path.GetFileName(file))
                .Except(bundleFiles)
                .ToArray();
            extractionDir.Should().HaveFiles(publishedFiles);
        }
    }
}
