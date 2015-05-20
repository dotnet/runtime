// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // ASCIIEncoding.GetEncoder() 
    public class ASCIIEncodingGetEncoder
    {
        // PosTest1: reference of ASCIIEncoding class is created via default constructor.
        [Fact]
        public void PosTest1()
        {
            ASCIIEncoding ascii;
            Encoder actualEncoder;

            ascii = new ASCIIEncoding();
            actualEncoder = ascii.GetEncoder();
            Assert.NotNull(actualEncoder);
        }
    }
}
