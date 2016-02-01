// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class StringsTests
    {
        [Fact]
        public void AscWTest()
        {
            Assert.Equal('3', Strings.AscW('3'));

            Assert.Throws<ArgumentException>(() => Strings.AscW(null));
            Assert.Throws<ArgumentException>(() => Strings.AscW(""));

            Assert.Equal('3', Strings.AscW("3"));
            Assert.Equal('3', Strings.AscW("345"));
        }
    }
}
