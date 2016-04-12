// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class UTF7EncodingGetBytes2
    {
        // PosTest1: Verify method GetBytes(string,Int32,Int32,Byte[],Int32) with non-null chars
        [Fact]
        public void PosTest1()
        {
            Byte[] bytes;
            String chars = "UTF7 Encoding Example";
            UTF7Encoding UTF7 = new UTF7Encoding();
            int byteCount = chars.Length;
            bytes = new Byte[byteCount];
            int bytesEncodedCount = UTF7.GetBytes(chars, 1, 2, bytes, 0);
        }

        // PosTest2: Verify method GetBytes(String,Int32,Int32,Byte[],Int32) with null chars
        [Fact]
        public void PosTest2()
        {
            Byte[] bytes;
            String chars = "";
            UTF7Encoding UTF7 = new UTF7Encoding();
            int byteCount = chars.Length;
            bytes = new Byte[byteCount];
            int bytesEncodedCount = UTF7.GetBytes(chars, 0, byteCount, bytes, 0);
            Assert.Equal(0, bytesEncodedCount);
        }
    }
}
