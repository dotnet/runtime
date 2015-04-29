// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Diagnostics.Contracts.Tests
{
    public class ValueTests
    {
        [Fact]
        public static void OldValueReturnsDefault()
        {
            Assert.Equal(0, Contract.OldValue<int>(42));
            Assert.Null(Contract.OldValue<string>("test"));
        }

        [Fact]
        public static void ResultReturnsDefault()
        {
            Assert.Equal(0, Contract.Result<int>());
            Assert.Null(Contract.Result<string>());
        }

        [Fact]
        public static void ValueAtReturnReturnsDefault()
        {
            int intValue = 42;
            Assert.Equal(0, Contract.ValueAtReturn(out intValue));
            Assert.Equal(0, intValue);

            string strValue = "test";
            Assert.Null(Contract.ValueAtReturn(out strValue));
            Assert.Null(strValue);
        }

    }
}
