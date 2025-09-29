// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using FluentAssertions;
using System;
using System.IO;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.NET.HostModel.AppHost;
using Microsoft.NET.HostModel.MachO.CodeSign.Tests;
using Microsoft.NET.HostModel.Bundle;

namespace HostActivation.Tests
{
    public class MachOHostSigningTests
    {
        [PlatformSpecificFact(TestPlatforms.OSX)]
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
    }
}
