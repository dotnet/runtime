// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    //System.Text.UnicodeEncoding.GetHashCode()
    public class UnicodeEncodingGetHashCode
    {
        #region Test Logic
        [Fact]
        public void PosTest1()
        {
            int expectedValue;
            int actualValue;
            UnicodeEncoding uE1 = new UnicodeEncoding();
            UnicodeEncoding uE2 = new UnicodeEncoding();

            expectedValue = uE1.GetHashCode();
            actualValue = uE2.GetHashCode();

            Assert.Equal(expectedValue, actualValue);
        }
        #endregion
    }
}
