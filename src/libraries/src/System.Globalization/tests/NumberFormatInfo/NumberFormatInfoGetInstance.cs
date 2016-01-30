// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
