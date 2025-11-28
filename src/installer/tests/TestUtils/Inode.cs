// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.DotNet.Cli.Build.Framework;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test
{
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    public static class Inode
    {
        public static string GetInode(string path)
        {
            var firstls = Command.Create("/bin/ls", "-li", path)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute();
            firstls.Should().Pass();
            var firstInode = firstls.StdOut.Split(' ')[0];
            return firstInode;
        }
    }
}
