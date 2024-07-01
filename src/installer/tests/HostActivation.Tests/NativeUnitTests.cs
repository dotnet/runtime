// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using FluentAssertions;
using System;
using System.IO;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.DotNet.Cli.Build.Framework;

namespace HostActivation.Tests
{
    public class NativeUnitTests
    {
        [Fact]
        public void Native_Test_Fx_Ver()
        {
            string testPath = Path.Combine(RepoDirectoriesProvider.Default.HostTestArtifacts, Binaries.GetExeName("test_fx_ver"));

            Command testCommand = Command.Create(testPath);
            testCommand
                .Execute()
                .Should()
                .Pass();
        }
    }
}
