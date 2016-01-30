// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
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
