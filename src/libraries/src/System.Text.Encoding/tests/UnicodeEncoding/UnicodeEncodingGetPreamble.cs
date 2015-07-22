// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    //System.Text.UnicodeEncoding.GetPreamble()
    public class UnicodeEncodingGetPreamble
    {
        #region Test Logic
        // PosTest1:Invoke the method with bigEndian true and byteOrderMark false
        [Fact]
        public void PosTest1()
        {
            UnicodeEncoding uE = new UnicodeEncoding(true, false);
            Byte[] expectedValue = new Byte[] { };
            Byte[] actualValue;

            actualValue = uE.GetPreamble();
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest2:Invoke the method with bigEndian true and byteOrderMark true
        [Fact]
        public void PosTest2()
        {
            UnicodeEncoding uE = new UnicodeEncoding(true, true);
            Byte[] expectedValue = new Byte[] { 0xfe, 0xff };
            Byte[] actualValue;

            actualValue = uE.GetPreamble();
            Assert.Equal(expectedValue, actualValue);
        }

        // PosTest3:Invoke the method with bigEndian false and byteOrderMark true
        [Fact]
        public void PosTest3()
        {
            UnicodeEncoding uE = new UnicodeEncoding(false, true);
            Byte[] expectedValue = new Byte[] { 0xff, 0xfe };
            Byte[] actualValue;

            actualValue = uE.GetPreamble();
            Assert.Equal(expectedValue, actualValue);
        }
        #endregion
    }
}
