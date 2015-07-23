// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.Extensions.Tests
{
    public class EnvironmentNewLine
    {
        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public void Windows_NewLineTest()
        {
            Assert.Equal("\r\n", Environment.NewLine);
        }

        [PlatformSpecific(PlatformID.AnyUnix)]
        [Fact]
        public void Unix_NewLineTest()
        {
            Assert.Equal("\n", Environment.NewLine);
        }
    }
}
