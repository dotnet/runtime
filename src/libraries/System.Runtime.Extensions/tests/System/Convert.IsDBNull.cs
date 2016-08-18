// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Tests
{
    public class IsDBNullTests
    {
#if netstandard17
        [Fact]
        public static void SimpleTest()
        {
            Assert.Equal(true, Convert.IsDBNull(Convert.DBNull));
            Assert.Equal(false, Convert.IsDBNull(4));
            Assert.Equal(false, Convert.IsDBNull(true));
            Assert.Equal(false, Convert.IsDBNull('x'));
            Assert.Equal(false, Convert.IsDBNull(1.1));
        }
#endif
    }
}
