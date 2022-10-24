// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class Environment_IsPrivilegedProcess
    {
        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support geteuid")]
        public void TestIsPrivilegedProcess()
        {
            if (AdminHelpers.IsProcessElevated())
            {
                Assert.True(Environment.IsPrivilegedProcess);
            }
            else
            {
                Assert.False(Environment.IsPrivilegedProcess);
            }
        }
    }
}
