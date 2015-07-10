// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
