// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class SyntaxCheckTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData(" ", false)]
        [InlineData("machineName\\", false)]
        [InlineData("machineName", true)]
        [InlineData("  machineName  ", true)]
        public void CheckMachineName_Invoke_ReturnsExpected(string value, bool expected)
        {
            Assert.Equal(expected, SyntaxCheck.CheckMachineName(value));
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData(" ", false)]
        [InlineData("path", false)]
        [InlineData("\\path", false)]
        [InlineData("\\\\", true)]
        [InlineData("\\\\path", true)]
        [InlineData("  \\\\path", true)]
        public void CheckPath_Invoke_ReturnsExpected(string value, bool expected)
        {
            Assert.Equal(expected, SyntaxCheck.CheckPath(value));
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData(" ", false)]
        [InlineData("path", false)]
        public void CheckRootedPath_Invoke_ReturnsExpected(string value, bool expected)
        {
            Assert.Equal(expected, SyntaxCheck.CheckRootedPath(value));
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData("\\path", true)]
        public void CheckRootedPath_Windows_ReturnsExpected(string value, bool expected)
        {
            Assert.Equal(expected, SyntaxCheck.CheckRootedPath(value));
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [InlineData("/path", true)]
        public void CheckRootedPath_Unix_ReturnsExpected(string value, bool expected)
        {
            Assert.Equal(expected, SyntaxCheck.CheckRootedPath(value));
        }
    }
}
