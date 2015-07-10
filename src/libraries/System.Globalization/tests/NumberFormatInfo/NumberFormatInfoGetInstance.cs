// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoGetInstance
    {
        // PosTest1: Verify method GetInstance
        [Fact]
        public void TestGetInstanceNotNull()
        {
            CultureInfo ci = new CultureInfo("fr-FR");
            NumberFormatInfo nfi = NumberFormatInfo.GetInstance(ci);
            Assert.NotNull(nfi);
        }
    }
}
