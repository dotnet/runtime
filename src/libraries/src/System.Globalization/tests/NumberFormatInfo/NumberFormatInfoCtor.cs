// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoCtor
    {
        // PosTest1: Verify Ctor
        [Fact]
        public void TestCtor()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            Assert.NotNull(nfi);
        }
    }
}
