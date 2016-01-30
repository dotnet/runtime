// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoReadOnly
    {
        // PosTest1: Verify method ReadOnly
        [Fact]
        public void TestReadOnly()
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            NumberFormatInfo nfiReadOnly = NumberFormatInfo.ReadOnly(nfi);
            Assert.True(nfiReadOnly.IsReadOnly);
        }

        // NegTest1: ArgumentNullException is not thrown
        [Fact]
        public void TestNullArgument()
        {
            NumberFormatInfo nfi = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                NumberFormatInfo nfiReadOnly = NumberFormatInfo.ReadOnly(nfi);
            });
        }
    }
}
