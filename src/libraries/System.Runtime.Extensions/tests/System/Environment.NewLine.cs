// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.Extensions.Tests
{
    public class EnvironmentNewLine
    {
        [Fact]
        public void NewLineTest()
        {
            //arrange
            string expectedNewLine = Interop.IsWindows ? "\r\n" : "\n";

            //act
            string actualNewLine = Environment.NewLine;

            //assert
            Assert.Equal(expectedNewLine, actualNewLine);
        }
    }




}
