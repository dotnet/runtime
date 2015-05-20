// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // ASCIIEncoding.GetDecoder() 
    public class ASCIIEncodingGetDecoder
    {
        // PosTest1: reference of ASCIIEncoding class is created via default constructor.
        [Fact]
        public void PosTest1()
        {
            ASCIIEncoding ascii;
            Decoder actualDecoder;

            ascii = new ASCIIEncoding();
            actualDecoder = ascii.GetDecoder();
            Assert.NotNull(actualDecoder);
        }
    }
}
