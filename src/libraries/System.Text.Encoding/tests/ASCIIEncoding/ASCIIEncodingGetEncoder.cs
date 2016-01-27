// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
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
