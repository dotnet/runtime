// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using FluentAssertions;
using System;
using System.IO;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.NET.HostModel.AppHost;
using Microsoft.NET.HostModel.Bundle;
using System.Diagnostics;

namespace HostActivation.Tests
{
    public class MachOHostSigningTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public void SignedAppHostRuns()
        {
            using var testDirectory = TestArtifact.Create(nameof(SignedAppHostRuns));
            var testAppHostPath = Path.Combine(testDirectory.Location, Path.GetFileName(Binaries.AppHost.FilePath));
            File.Copy(Binaries.AppHost.FilePath, testAppHostPath);
            long preRemovalSize = new FileInfo(testAppHostPath).Length;
            string signedHostPath = testAppHostPath + ".signed";

            HostWriter.CreateAppHost(testAppHostPath, signedHostPath, testAppHostPath + ".dll", enableMacOSCodeSign: true);

            var executedCommand = Command.Create(testAppHostPath)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute();
            executedCommand.Should().ExitWith(Constants.ErrorCode.AppHostExeNotBoundFailure);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public void SigningAppHostPreservesEntitlements()
        {
            using var testDirectory = TestArtifact.Create(nameof(SignedAppHostRuns));
            var testAppHostPath = Path.Combine(testDirectory.Location, Path.GetFileName(Binaries.AppHost.FilePath));
            File.Copy(Binaries.AppHost.FilePath, testAppHostPath);
            long preRemovalSize = new FileInfo(testAppHostPath).Length;
            string signedHostPath = testAppHostPath + ".signed";

            HostWriter.CreateAppHost(testAppHostPath, signedHostPath, testAppHostPath + ".dll", enableMacOSCodeSign: true);

            SignatureHelpers.HasEntitlements(testAppHostPath).Should().BeTrue();
            SignatureHelpers.HasEntitlements(signedHostPath).Should().BeTrue();
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.OSX)]
        public void BundledAppHostHasEntitlements()
        {
            using var testDirectory = TestArtifact.Create(nameof(BundledAppHostHasEntitlements));
            var testAppHostPath = Path.Combine(testDirectory.Location, Path.GetFileName(Binaries.SingleFileHost.FilePath));
            File.Copy(Binaries.SingleFileHost.FilePath, testAppHostPath);
            long preRemovalSize = new FileInfo(testAppHostPath).Length;
            string signedHostPath = testAppHostPath + ".signed";

            HostWriter.CreateAppHost(testAppHostPath, signedHostPath, testAppHostPath + ".dll", enableMacOSCodeSign: true);
            var bundlePath = new Bundler(Path.GetFileName(signedHostPath), testAppHostPath + ".bundle").GenerateBundle([new(signedHostPath, Path.GetFileName(signedHostPath))]);


            SignatureHelpers.HasEntitlements(testAppHostPath).Should().BeTrue();
            SignatureHelpers.HasEntitlements(bundlePath).Should().BeTrue();
        }
    }
}
