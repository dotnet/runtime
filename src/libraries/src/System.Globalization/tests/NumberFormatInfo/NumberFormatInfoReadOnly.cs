// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
