// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class Environment_IsPrivilegedProcess
    {
        [Fact]
        public void TestIsPrivilegedProcess()
        {
            Assert.Equal(AdminHelpers.IsProcessElevated(), Environment.IsPrivilegedProcess);
        }
    }
}
