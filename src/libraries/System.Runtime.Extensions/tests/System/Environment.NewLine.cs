// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class EnvironmentNewLine
    {
        [PlatformSpecific(TestPlatforms.Windows)]  // NewLine character on Windows
        [Fact]
        public void Windows_NewLineTest()
        {
            Assert.Equal("\r\n", Environment.NewLine);
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]  // NewLine character on Unix
        [Fact]
        public void Unix_NewLineTest()
        {
            Assert.Equal("\n", Environment.NewLine);
        }
    }
}
