// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Tests
{
    public class UTF7EncodingGetString
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        // PosTest1: start index is zero and count of bytes decoded equals whole length of bytes array
        [Fact]
        public void PosTest1()
        {
            Byte[] bytes = new Byte[] {
                             85,  84,  70,  56,  32,  69, 110,
                             99, 111, 100, 105, 110, 103,  32,
                             69, 120,  97, 109, 112, 108, 101};
            UTF7Encoding utf7 = new UTF7Encoding();
            string str = utf7.GetString(bytes, 0, bytes.Length);
        }

        // PosTest2: start index and count of bytes decoded are both random valid value
        [Fact]
        public void PosTest2()
        {
            int startIndex = 0;
            int count = 0;
            Byte[] bytes = new Byte[] {
                             85,  84,  70,  56,  32,  69, 110,
                             99, 111, 100, 105, 110, 103,  32,
                             69, 120,  97, 109, 112, 108, 101};
            startIndex = _generator.GetInt32(-55) % bytes.Length;
            count = _generator.GetInt32(-55) % (bytes.Length - startIndex) + 1;
            UTF7Encoding utf7 = new UTF7Encoding();
            string str = utf7.GetString(bytes, startIndex, count);
        }
    }
}
