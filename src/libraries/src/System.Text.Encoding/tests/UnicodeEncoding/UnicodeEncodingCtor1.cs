// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class UnicodeEncodingCtor
    {
        [Fact]
        public void PosTest1()
        {
            UnicodeEncoding expectedValue = new UnicodeEncoding(false, true);
            UnicodeEncoding actualValue;
            actualValue = new UnicodeEncoding();
            Assert.Equal(expectedValue, actualValue);
        }
    }
}
